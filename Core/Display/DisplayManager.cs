using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RRes.Models;

namespace RRes.Core.Display
{
    public class DisplayManager
    {
        private NativeMethods.DEVMODE _initialDevMode;
        private bool _hasInitialDevMode;
        private readonly Dictionary<string, NativeMethods.DEVMODE> _initialDevModes = new(StringComparer.OrdinalIgnoreCase);

        public ResolutionInfo InitialResolution { get; private set; } = new ResolutionInfo(1920, 1080, 60);

        public DisplayManager()
        {
            CaptureInitialResolution();
        }

        /// <summary>
        /// Mengambil daftar semua monitor aktif yang terpasang pada komputer.
        /// </summary>
        public List<MonitorInfo> GetConnectedMonitors()
        {
            var monitors = new List<MonitorInfo>();
            try
            {
                var dev = new NativeMethods.DISPLAY_DEVICE();
                dev.cb = Marshal.SizeOf(dev);
                uint devNum = 0;

                while (NativeMethods.EnumDisplayDevices(null, devNum, ref dev, 0))
                {
                    if ((dev.StateFlags & NativeMethods.DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0)
                    {
                        var mon = new NativeMethods.DISPLAY_DEVICE();
                        mon.cb = Marshal.SizeOf(mon);
                        string monName = "";
                        string monId = "";

                        if (NativeMethods.EnumDisplayDevices(dev.DeviceName, 0, ref mon, 0))
                        {
                            monName = mon.DeviceString?.Trim() ?? "";
                            monId = mon.DeviceID?.Trim() ?? "";
                        }

                        bool isPrimary = (dev.StateFlags & NativeMethods.DISPLAY_DEVICE_PRIMARY_DEVICE) != 0;

                        monitors.Add(new MonitorInfo
                        {
                            Index = (int)devNum,
                            DeviceName = dev.DeviceName ?? string.Empty,
                            DeviceString = dev.DeviceString ?? string.Empty,
                            MonitorName = monName,
                            DeviceId = monId,
                            IsPrimary = isPrimary
                        });
                    }

                    devNum++;
                    dev = new NativeMethods.DISPLAY_DEVICE();
                    dev.cb = Marshal.SizeOf(dev);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes GetConnectedMonitors Warning]: {ex.Message}");
            }

            if (monitors.Count == 0)
            {
                monitors.Add(new MonitorInfo
                {
                    Index = 0,
                    DeviceName = @"\\.\DISPLAY1",
                    DeviceString = "Default Adapter",
                    MonitorName = "Generic Monitor",
                    IsPrimary = true
                });
            }

            return monitors;
        }

        /// <summary>
        /// Mengambil dan menyimpan resolusi native/awal saat aplikasi pertama kali dijalankan.
        /// </summary>
        public void CaptureInitialResolution(string? deviceName = null)
        {
            string key = string.IsNullOrEmpty(deviceName) ? "PRIMARY" : deviceName;
            try
            {
                var dm = CreateDefaultDevMode();
                if (NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_REGISTRY_SETTINGS, ref dm) ||
                    NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
                {
                    _initialDevModes[key] = dm;
                    if (string.IsNullOrEmpty(deviceName) || key.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase))
                    {
                        _initialDevMode = dm;
                        _hasInitialDevMode = true;
                        InitialResolution = new ResolutionInfo((int)dm.dmPelsWidth, (int)dm.dmPelsHeight, (int)dm.dmDisplayFrequency);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes DisplayManager Capture Warning]: {ex.Message}");
            }

            if (string.IsNullOrEmpty(deviceName))
            {
                InitialResolution = new ResolutionInfo(1920, 1080, 60);
            }
        }

        /// <summary>
        /// Mengambil resolusi layar monitor saat ini secara real-time.
        /// </summary>
        public ResolutionInfo GetCurrentResolution(string? deviceName = null)
        {
            try
            {
                var dm = CreateDefaultDevMode();
                if (NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
                {
                    return new ResolutionInfo((int)dm.dmPelsWidth, (int)dm.dmPelsHeight, (int)dm.dmDisplayFrequency);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes DisplayManager GetCurrent Warning]: {ex.Message}");
            }
            return InitialResolution;
        }

        /// <summary>
        /// Mengambil daftar semua refresh rate yang didukung oleh monitor tertentu.
        /// </summary>
        public List<int> GetSupportedRefreshRates(int width = 0, int height = 0, string? deviceName = null)
        {
            var rates = new HashSet<int>();
            try
            {
                var dm = CreateDefaultDevMode();
                int modeIndex = 0;

                while (NativeMethods.EnumDisplaySettings(deviceName, modeIndex, ref dm))
                {
                    if (dm.dmDisplayFrequency > 0)
                    {
                        if (width > 0 && height > 0)
                        {
                            if (dm.dmPelsWidth == width && dm.dmPelsHeight == height)
                            {
                                rates.Add((int)dm.dmDisplayFrequency);
                            }
                        }
                        else
                        {
                            rates.Add((int)dm.dmDisplayFrequency);
                        }
                    }
                    modeIndex++;
                }

                if (rates.Count == 0 && (width > 0 || height > 0))
                {
                    return GetSupportedRefreshRates(0, 0, deviceName);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes EnumDisplaySettings Warning]: {ex.Message}");
            }

            var cur = GetCurrentResolution(deviceName);
            if (cur.RefreshRate > 0)
            {
                rates.Add(cur.RefreshRate);
            }

            return rates.OrderByDescending(r => r).ToList();
        }

        /// <summary>
        /// Mengambil refresh rate tertinggi yang didukung monitor untuk mengunci frekuensi (mencegah turun ke 60Hz).
        /// </summary>
        public int GetMaxSupportedRefreshRate(int width = 0, int height = 0, string? deviceName = null)
        {
            var supported = GetSupportedRefreshRates(width, height, deviceName);
            if (supported.Count > 0)
            {
                return supported.Max();
            }
            var cur = GetCurrentResolution(deviceName);
            return cur.RefreshRate > 0 ? cur.RefreshRate : 60;
        }

        /// <summary>
        /// Mengubah resolusi desktop ke target pilihan secara instan dengan penguncian refresh rate aktif.
        /// </summary>
        public (bool Success, string Message) SetResolution(int width, int height, int refreshRate = 0, string? deviceName = null)
        {
            try
            {
                var dm = CreateDefaultDevMode();
                if (!NativeMethods.EnumDisplaySettings(deviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
                {
                    return (false, "Gagal membaca konfigurasi tampilan monitor saat ini.");
                }

                // Catat initial jika belum ada
                string key = string.IsNullOrEmpty(deviceName) ? "PRIMARY" : deviceName;
                if (!_initialDevModes.ContainsKey(key))
                {
                    _initialDevModes[key] = dm;
                }

                // Tentukan target refresh rate terbaik agar tidak turun ke 60Hz
                int targetHz = refreshRate;
                if (targetHz <= 0)
                {
                    targetHz = GetMaxSupportedRefreshRate(width, height, deviceName);
                }

                dm.dmPelsWidth = (uint)width;
                dm.dmPelsHeight = (uint)height;
                dm.dmFields = NativeMethods.DM_PELSWIDTH | NativeMethods.DM_PELSHEIGHT | NativeMethods.DM_DISPLAYFREQUENCY;
                dm.dmDisplayFrequency = (uint)targetHz;

                // 1. Tes mode resolusi dengan target refresh rate
                int testResult = string.IsNullOrEmpty(deviceName)
                    ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_TEST)
                    : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_TEST, IntPtr.Zero);

                // 2. Jika target frekuensi gagal, coba fallback ke refresh rate lain yang didukung dalam urutan menurun
                if (testResult != NativeMethods.DISP_CHANGE_SUCCESSFUL)
                {
                    var fallbackRates = GetSupportedRefreshRates(width, height, deviceName).Where(r => r != targetHz).ToList();
                    bool foundWorking = false;

                    foreach (var rate in fallbackRates)
                    {
                        dm.dmDisplayFrequency = (uint)rate;
                        int retryTest = string.IsNullOrEmpty(deviceName)
                            ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_TEST)
                            : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_TEST, IntPtr.Zero);

                        if (retryTest == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                        {
                            targetHz = rate;
                            foundWorking = true;
                            break;
                        }
                    }

                    if (!foundWorking)
                    {
                        // Coba tanpa memaksakan dmDisplayFrequency sebagai langkah penyelamatan terakhir
                        dm.dmFields = NativeMethods.DM_PELSWIDTH | NativeMethods.DM_PELSHEIGHT;
                        int noFreqTest = string.IsNullOrEmpty(deviceName)
                            ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_TEST)
                            : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_TEST, IntPtr.Zero);

                        if (noFreqTest != NativeMethods.DISP_CHANGE_SUCCESSFUL)
                        {
                            return (false, $"Mode tampilan {width}x{height} tidak didukung oleh monitor/GPU.");
                        }
                    }
                }

                // 3. Terapkan perubahan ke registry display secara bersih dengan CDS_UPDATEREGISTRY
                int applyResult = string.IsNullOrEmpty(deviceName)
                    ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_UPDATEREGISTRY)
                    : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY, IntPtr.Zero);

                // Jika pemanggilan dengan deviceName mengembalikan gagal, coba langsung dengan ChangeDisplaySettings global
                if (applyResult != NativeMethods.DISP_CHANGE_SUCCESSFUL && !string.IsNullOrEmpty(deviceName))
                {
                    applyResult = NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_UPDATEREGISTRY);
                }

                if (applyResult == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                {
                    // Pastikan broadcast WM_DISPLAYCHANGE dikirimkan ke seluruh aplikasi & desktop Windows
                    NativeMethods.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
                    return (true, $"Resolusi desktop berhasil diubah ke {width}x{height} @ {targetHz}Hz (Registry Updated).");
                }
                else if (applyResult == NativeMethods.DISP_CHANGE_RESTART)
                {
                    return (false, "Perubahan resolusi memerlukan restart komputer.");
                }
                else
                {
                    // Fallback ke mode fullscreen dinamis jika update registry ditolak
                    int fallbackResult = string.IsNullOrEmpty(deviceName)
                        ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_FULLSCREEN)
                        : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_FULLSCREEN, IntPtr.Zero);

                    if (fallbackResult == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                    {
                        return (true, $"Resolusi desktop berhasil diterapkan (Mode Dinamis {width}x{height} @ {targetHz}Hz).");
                    }
                    return (false, $"Gagal mengubah resolusi (Kode error Windows: {applyResult}).");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Terjadi kesalahan saat mengubah resolusi: {ex.Message}");
            }
        }

        /// <summary>
        /// Mengembalikan resolusi ke pengaturan awal (revert to native).
        /// </summary>
        public (bool Success, string Message) RevertToNative(string? deviceName = null)
        {
            try
            {
                string key = string.IsNullOrEmpty(deviceName) ? "PRIMARY" : deviceName;
                if (_initialDevModes.TryGetValue(key, out var initDm))
                {
                    var dm = initDm;
                    dm.dmFields |= NativeMethods.DM_DISPLAYFREQUENCY;
                    int result = string.IsNullOrEmpty(deviceName)
                        ? NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_UPDATEREGISTRY)
                        : NativeMethods.ChangeDisplaySettingsEx(deviceName, ref dm, IntPtr.Zero, NativeMethods.CDS_UPDATEREGISTRY, IntPtr.Zero);

                    if (result != NativeMethods.DISP_CHANGE_SUCCESSFUL && !string.IsNullOrEmpty(deviceName))
                    {
                        result = NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_UPDATEREGISTRY);
                    }

                    if (result == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                    {
                        NativeMethods.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
                        return (true, $"Resolusi berhasil dikembalikan ke native ({initDm.dmPelsWidth}x{initDm.dmPelsHeight} @ {initDm.dmDisplayFrequency}Hz).");
                    }
                }
                else if (_hasInitialDevMode && string.IsNullOrEmpty(deviceName))
                {
                    var dm = _initialDevMode;
                    dm.dmFields |= NativeMethods.DM_DISPLAYFREQUENCY;
                    int result = NativeMethods.ChangeDisplaySettings(ref dm, NativeMethods.CDS_UPDATEREGISTRY);
                    if (result == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                    {
                        NativeMethods.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
                        return (true, $"Resolusi berhasil dikembalikan ke native ({_initialDevMode.dmPelsWidth}x{_initialDevMode.dmPelsHeight} @ {_initialDevMode.dmDisplayFrequency}Hz).");
                    }
                }

                // Fallback ke reset default Windows
                int resetResult = NativeMethods.ChangeDisplaySettingsEx(deviceName, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
                NativeMethods.ChangeDisplaySettingsEx(null, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero);
                if (resetResult == NativeMethods.DISP_CHANGE_SUCCESSFUL)
                {
                    return (true, "Resolusi berhasil direset ke pengaturan bawaan Windows.");
                }

                return (false, $"Gagal mereset resolusi (Kode error: {resetResult}).");
            }
            catch (Exception ex)
            {
                return (false, $"Kesalahan saat mengembalikan resolusi: {ex.Message}");
            }
        }

        private static NativeMethods.DEVMODE CreateDefaultDevMode()
        {
            var dm = new NativeMethods.DEVMODE();
            dm.dmDeviceName = string.Empty;
            dm.dmFormName = string.Empty;
            dm.dmSize = (ushort)Marshal.SizeOf(typeof(NativeMethods.DEVMODE));
            return dm;
        }
    }
}
