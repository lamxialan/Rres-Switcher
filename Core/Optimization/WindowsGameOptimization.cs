using System;
using System.Diagnostics;
using Microsoft.Win32;

namespace RRes.Core.Optimization
{
    public class WindowsGameOptimization
    {
        /// <summary>
        /// Memeriksa apakah fitur Game Bar & Game DVR Windows sedang dimatikan (Anti-Stutter mode aktif).
        /// </summary>
        public bool IsGameDvrDisabled()
        {
            try
            {
                // Cek HKCU\System\GameConfigStore -> GameDVR_Enabled
                using var configStoreKey = Registry.CurrentUser.OpenSubKey(@"System\GameConfigStore", false);
                if (configStoreKey != null)
                {
                    var val = configStoreKey.GetValue("GameDVR_Enabled");
                    if (val is int intVal && intVal == 0)
                    {
                        return true;
                    }
                }

                // Cek HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR -> AppCaptureEnabled
                using var gameDvrKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", false);
                if (gameDvrKey != null)
                {
                    var val = gameDvrKey.GetValue("AppCaptureEnabled");
                    if (val is int intVal && intVal == 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes GameDVR Check Warning]: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Mengatur penonaktifan Game Bar & DVR untuk mencegah micro-stuttering dan input delay.
        /// </summary>
        public (bool Success, string Message) SetGameDvrDisabled(bool disable)
        {
            try
            {
                int targetValue = disable ? 0 : 1;

                // 1. Tulis ke HKCU\System\GameConfigStore
                using (var configStoreKey = Registry.CurrentUser.CreateSubKey(@"System\GameConfigStore", true))
                {
                    configStoreKey?.SetValue("GameDVR_Enabled", targetValue, RegistryValueKind.DWord);
                    if (disable)
                    {
                        configStoreKey?.SetValue("GameDVR_FSEBehaviorMode", 2, RegistryValueKind.DWord);
                    }
                }

                // 2. Tulis ke HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR
                using (var gameDvrKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR", true))
                {
                    gameDvrKey?.SetValue("AppCaptureEnabled", targetValue, RegistryValueKind.DWord);
                }

                // 3. Coba tulis ke HKLM Policies jika memiliki akses hak istimewa (opsional)
                try
                {
                    using var policyKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\GameDVR", true);
                    policyKey?.SetValue("AllowGameDVR", targetValue, RegistryValueKind.DWord);
                }
                catch
                {
                    // Abaikan jika non-admin
                }

                return (true, disable
                    ? "Windows Game Bar & DVR berhasil dimatikan (Anti-Stutter aktif)."
                    : "Windows Game Bar & DVR dikembalikan ke default.");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal mengatur registri Game DVR: {ex.Message}");
            }
        }

        /// <summary>
        /// Membersihkan DNS Resolver Cache Windows guna mereset rute jaringan game (ipconfig /flushdns).
        /// </summary>
        public (bool Success, string Message) FlushDnsCache()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(3000);
                return (true, "DNS Resolver Cache berhasil dibersihkan (Flush DNS sukses).");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal membersihkan DNS cache: {ex.Message}");
            }
        }

        /// <summary>
        /// Membuka GPU Control Panel (NVIDIA / AMD / Intel) atau Windows Display Settings
        /// </summary>
        public (bool Success, string Message) OpenGpuControlPanel()
        {
            // 1. Coba NVIDIA Control Panel (Store App atau Classic Client)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:AppsFolder\\NVIDIACorp.NVIDIAControlPanel_56jybvy8sckqj!NVIDIACorp.NVIDIAControlPanel",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return (true, "Membuka NVIDIA Control Panel...");
            }
            catch { }

            string[] classicPaths = new[]
            {
                @"C:\Program Files\NVIDIA Corporation\Control Panel Client\nvcplui.exe",
                @"C:\Program Files\AMD\CNext\CNext\RadeonSoftware.exe"
            };

            foreach (var path in classicPaths)
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
                        return (true, $"Membuka {System.IO.Path.GetFileNameWithoutExtension(path)}...");
                    }
                    catch { }
                }
            }

            // 2. Fallback ke Windows Display Settings
            return OpenWindowsDisplaySettings();
        }

        /// <summary>
        /// Membuka menu Pengaturan Tampilan Windows (ms-settings:display)
        /// </summary>
        public (bool Success, string Message) OpenWindowsDisplaySettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:display",
                    UseShellExecute = true
                });
                return (true, "Membuka Windows Display Settings...");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal membuka Windows Display Settings: {ex.Message}");
            }
        }
    }
}
