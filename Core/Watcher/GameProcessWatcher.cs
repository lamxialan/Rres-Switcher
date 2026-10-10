using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RRes.Core.Watcher
{
    public class GameProcessWatcher : IDisposable
    {
        private readonly string[] _targetProcessNames = new[] { "VALORANT-Win64-Shipping", "VALORANT" };
        private CancellationTokenSource? _cts;
        private Task? _watcherTask;
        private bool _isGameRunning;
        private int _lastProcessId;
        private bool _priorityApplied;

        public bool IsAutoRevertEnabled { get; set; } = true;
        public bool IsHighPriorityEnabled { get; set; } = true;

        public bool IsGameRunning => _isGameRunning;

        public event Action<bool>? GameStateChanged; // true: game launched, false: game closed
        public event Action? AutoRevertTriggered;
        public event Action<string, string>? StatusNotification; // (message, type)

        public void Start()
        {
            if (_watcherTask != null && !_watcherTask.IsCompleted)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _watcherTask = Task.Run(() => WatcherLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task WatcherLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var gameProcess = FindValorantProcess();

                    if (gameProcess != null && !gameProcess.HasExited)
                    {
                        // Game terdeteksi sedang berjalan
                        if (!_isGameRunning || _lastProcessId != gameProcess.Id)
                        {
                            _isGameRunning = true;
                            _lastProcessId = gameProcess.Id;
                            _priorityApplied = false;

                            GameStateChanged?.Invoke(true);
                            StatusNotification?.Invoke("VALORANT terdeteksi aktif. Watcher memantau proses...", "Info");
                        }

                        // Optimasi Process Priority jika opsi aktif
                        if (IsHighPriorityEnabled && !_priorityApplied)
                        {
                            ApplyHighPriority(gameProcess);
                        }
                    }
                    else
                    {
                        // Game tidak berjalan
                        if (_isGameRunning)
                        {
                            _isGameRunning = false;
                            _lastProcessId = 0;
                            _priorityApplied = false;

                            GameStateChanged?.Invoke(false);

                            // Jalankan auto-revert jika fitur diaktifkan
                            if (IsAutoRevertEnabled)
                            {
                                AutoRevertTriggered?.Invoke();
                                StatusNotification?.Invoke("VALORANT telah ditutup. Resolusi otomatis dikembalikan ke Native!", "Success");
                            }
                            else
                            {
                                StatusNotification?.Invoke("VALORANT ditutup.", "Info");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[GameProcessWatcher Exception]: {ex.Message}");
                }

                try
                {
                    await Task.Delay(1500, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private Process? FindValorantProcess()
        {
            foreach (var name in _targetProcessNames)
            {
                try
                {
                    var processes = Process.GetProcessesByName(name);
                    if (processes.Length > 0)
                    {
                        var proc = processes.FirstOrDefault(p => !p.HasExited);
                        if (proc != null)
                        {
                            return proc;
                        }
                    }
                }
                catch
                {
                    // Abaikan exception saat query proses
                }
            }
            return null;
        }

        private void ApplyHighPriority(Process process)
        {
            try
            {
                if (process.PriorityClass != ProcessPriorityClass.High)
                {
                    process.PriorityClass = ProcessPriorityClass.High;
                    _priorityApplied = true;
                    StatusNotification?.Invoke("Prioritas proses VALORANT berhasil ditingkatkan ke High!", "Success");
                }
                else
                {
                    _priorityApplied = true;
                }
            }
            catch (Exception ex)
            {
                // Jika akses ditolak oleh proteksi Vanguard/Windows, coba AboveNormal sebagai fallback
                try
                {
                    if (process.PriorityClass != ProcessPriorityClass.AboveNormal)
                    {
                        process.PriorityClass = ProcessPriorityClass.AboveNormal;
                        _priorityApplied = true;
                        StatusNotification?.Invoke("Prioritas proses VALORANT diatur ke AboveNormal (High dibatasi oleh sistem).", "Info");
                    }
                    else
                    {
                        _priorityApplied = true;
                    }
                }
                catch
                {
                    _priorityApplied = true;
                    Console.Error.WriteLine($"[RRes Priority Warning]: Tidak dapat mengubah prioritas proses (akses dibatasi Windows/Vanguard): {ex.Message}");
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
