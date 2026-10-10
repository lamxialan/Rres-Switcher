using System;
using System.Diagnostics;
using System.IO;

namespace RRes.Core.Valorant
{
    public static class ValorantLauncher
    {
        private const string ProtocolUri = "riotclient://launch/valorant";

        /// <summary>
        /// Meluncurkan game VALORANT melalui protokol resmi Riot Client atau eksekusi binary langsung.
        /// </summary>
        public static (bool Success, string Message) LaunchGame()
        {
            // 1. Coba luncurkan via protokol resmi Windows riotclient://
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = ProtocolUri,
                    UseShellExecute = true
                };
                Process.Start(psi);
                return (true, "Meluncurkan VALORANT via Riot Client...");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes Protocol Launch Warning]: {ex.Message}");
            }

            // 2. Fallback: Eksekusi langsung RiotClientServices.exe
            string[] potentialPaths = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Riot Games", "Riot Client", "RiotClientServices.exe"),
                @"C:\Riot Games\Riot Client\RiotClientServices.exe",
                @"D:\Riot Games\Riot Client\RiotClientServices.exe"
            };

            foreach (var path in potentialPaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "--launch-product=valorant --launch-patchline=live",
                            UseShellExecute = true
                        };
                        Process.Start(psi);
                        return (true, "Meluncurkan VALORANT via RiotClientServices.exe...");
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[RRes Direct Launch Warning]: {ex.Message}");
                    }
                }
            }

            return (false, "Gagal meluncurkan VALORANT. Pastikan Riot Client terpasang.");
        }
    }
}
