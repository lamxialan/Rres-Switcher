using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RRes.Core.Valorant
{
    public class RiotProcessManager
    {
        private static readonly string[] RiotProcessNames = new[]
        {
            "VALORANT",
            "VALORANT-Win64-Shipping",
            "RiotClientServices",
            "RiotClientUx",
            "RiotClientCrashHandler",
            "UnrealCEFSubProcess"
        };

        /// <summary>
        /// Menghentikan seluruh proses Valorant dan Riot Client secara paksa
        /// </summary>
        public async Task<(bool Success, int KilledCount, string Message)> KillAllRiotProcessesAsync()
        {
            return await Task.Run(() =>
            {
                int killed = 0;
                try
                {
                    foreach (var name in RiotProcessNames)
                    {
                        var processes = Process.GetProcessesByName(name);
                        foreach (var proc in processes)
                        {
                            try
                            {
                                proc.Kill(true);
                                killed++;
                            }
                            catch
                            {
                                // Jika proteksi akses, coba lewat taskkill
                            }
                            finally
                            {
                                proc.Dispose();
                            }
                        }
                    }

                    // Fallback taskkill jika ada proses zombie tertahan
                    foreach (var name in RiotProcessNames)
                    {
                        try
                        {
                            using var taskkill = Process.Start(new ProcessStartInfo
                            {
                                FileName = "taskkill.exe",
                                Arguments = $"/F /T /IM {name}.exe",
                                CreateNoWindow = true,
                                UseShellExecute = false
                            });
                            taskkill?.WaitForExit(1000);
                        }
                        catch
                        {
                            // Abaikan jika tidak ditemukan
                        }
                    }

                    return (true, killed, $"Berhasil menutup seluruh proses Riot & Valorant ({killed} proses dihentikan).");
                }
                catch (Exception ex)
                {
                    return (false, killed, $"Gagal menghentikan proses: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Me-restart Windows Service Vanguard Anti-Cheat (vgc) secara asinkron
        /// </summary>
        public async Task<(bool Success, string Message)> RestartVanguardServiceAsync()
        {
            return await Task.Run(async () =>
            {
                try
                {
                    // 1. Coba hentikan vgc
                    var stopResult = await ExecuteScCommandAsync("stop vgc");
                    
                    // Tunggu sesaat hingga service benar-benar berhenti
                    await Task.Delay(2500);

                    // 2. Jalankan kembali vgc
                    var startResult = await ExecuteScCommandAsync("start vgc");

                    if (startResult.ExitCode == 0 || startResult.Output.Contains("PENDING") || startResult.Output.Contains("RUNNING"))
                    {
                        return (true, "Vanguard Service (vgc) berhasil direstart & berjalan aktif.");
                    }

                    // Jika gagal karena hak akses atau error lain
                    if (startResult.Output.Contains("Access is denied") || startResult.ExitCode == 5)
                    {
                        return (false, "Akses ditolak: Jalankan RRes sebagai Administrator untuk mengontrol Service vgc.");
                    }

                    return (false, $"Hasil restart vgc: {startResult.Output.Trim()}");
                }
                catch (Exception ex)
                {
                    return (false, $"Gagal me-restart service Vanguard: {ex.Message}");
                }
            });
        }

        private static async Task<(int ExitCode, string Output)> ExecuteScCommandAsync(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return (-1, "Gagal memulai sc.exe");

                string output = await proc.StandardOutput.ReadToEndAsync();
                string error = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();

                return (proc.ExitCode, string.IsNullOrWhiteSpace(output) ? error : output);
            }
            catch (Exception ex)
            {
                return (-1, ex.Message);
            }
        }
    }
}
