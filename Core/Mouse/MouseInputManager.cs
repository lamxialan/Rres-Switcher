using System;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace RRes.Core.Mouse
{
    public class MouseInputManager
    {
        private const uint SPI_GETMOUSE = 0x0003;
        private const uint SPI_SETMOUSE = 0x0004;
        private const uint SPIF_UPDATEINIFILE = 0x0001;
        private const uint SPIF_SENDCHANGE = 0x0002;

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, [In, Out] int[] pvParam, uint fWinIni);

        /// <summary>
        /// Memeriksa apakah fitur 'Enhance Pointer Precision' (Mouse Acceleration) Windows sedang dinonaktifkan.
        /// </summary>
        public bool IsMouseAccelerationDisabled()
        {
            try
            {
                int[] mouseParams = new int[3];
                if (SystemParametersInfo(SPI_GETMOUSE, 0, mouseParams, 0))
                {
                    // mouseParams[2] bernilai 0 jika akselerasi dinonaktifkan
                    if (mouseParams[2] == 0)
                    {
                        return true;
                    }
                }

                // Cek cadangan registry Control Panel\Mouse
                using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", false);
                if (key != null)
                {
                    var speed = key.GetValue("MouseSpeed")?.ToString();
                    return speed == "0";
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes MouseInputManager Check Warning]: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Mengatur akselerasi mouse Windows (Enhance Pointer Precision).
        /// disable = true akan mematikan akselerasi agar menghasilkan input raw 1:1 murni.
        /// </summary>
        public (bool Success, string Message) SetMouseAcceleration(bool disable)
        {
            try
            {
                int[] mouseParams = new int[3];

                if (disable)
                {
                    // 1:1 Raw input murni tanpa kurva akselerasi
                    mouseParams[0] = 0; // MouseThreshold1
                    mouseParams[1] = 0; // MouseThreshold2
                    mouseParams[2] = 0; // MouseSpeed (0 = disabled)
                }
                else
                {
                    // Default standar Windows dengan akselerasi aktif
                    mouseParams[0] = 6;
                    mouseParams[1] = 10;
                    mouseParams[2] = 1;
                }

                bool apiResult = SystemParametersInfo(
                    SPI_SETMOUSE,
                    0,
                    mouseParams,
                    SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

                // Tulis langsung ke registry agar tersimpan permanen setelah restart
                using (var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Mouse", true))
                {
                    if (key != null)
                    {
                        if (disable)
                        {
                            key.SetValue("MouseSpeed", "0");
                            key.SetValue("MouseThreshold1", "0");
                            key.SetValue("MouseThreshold2", "0");
                        }
                        else
                        {
                            key.SetValue("MouseSpeed", "1");
                            key.SetValue("MouseThreshold1", "6");
                            key.SetValue("MouseThreshold2", "10");
                        }
                    }
                }

                if (apiResult)
                {
                    return (true, disable
                        ? "Windows Mouse Acceleration berhasil dinonaktifkan (1:1 Raw Input aktif)."
                        : "Windows Mouse Acceleration diaktifkan.");
                }
                else
                {
                    return (false, "Gagal mengubah parameter mouse via SystemParametersInfo.");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Kesalahan saat mengatur mouse precision: {ex.Message}");
            }
        }
    }
}
