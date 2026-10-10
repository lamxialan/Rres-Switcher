using System;
using System.Diagnostics;

namespace RRes.Core.Utils
{
    public static class WindowsAudioHelper
    {
        /// <summary>
        /// Membuka dialog Control Panel Sound Windows klasik (mmsys.cpl) untuk mengakses konfigurasi device & tab Enhancements.
        /// </summary>
        public static (bool Success, string Message) OpenClassicSoundControlPanel()
        {
            try
            {
                // Coba buka mmsys.cpl langsung
                var psi = new ProcessStartInfo
                {
                    FileName = "rundll32.exe",
                    Arguments = "shell32.dll,Control_RunDLL mmsys.cpl,,0",
                    UseShellExecute = true
                };
                Process.Start(psi);
                return (true, "Membuka Windows Sound Control Panel (mmsys.cpl)...");
            }
            catch (Exception ex)
            {
                try
                {
                    // Fallback alternatif
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "control.exe",
                        Arguments = "mmsys.cpl sounds",
                        UseShellExecute = true
                    });
                    return (true, "Membuka Sound Control Panel via control.exe...");
                }
                catch
                {
                    return (false, $"Gagal membuka Sound Control Panel: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Membuka aplikasi Pengaturan Suara Windows Modern (ms-settings:sound).
        /// </summary>
        public static (bool Success, string Message) OpenModernSoundSettings()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "ms-settings:sound",
                    UseShellExecute = true
                });
                return (true, "Membuka Windows Modern Sound Settings...");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal membuka Modern Sound Settings: {ex.Message}");
            }
        }
    }
}
