using System;
using DiscordRPC;
using DiscordRPC.Logging;

namespace RRes.Core.Watcher
{
    public class DiscordRpcService : IDisposable
    {
        // Discord Application Client ID khusus untuk utilitas RRes Valorant Companion
        private const string DefaultClientId = "1224508930472480838";

        private readonly object _syncLock = new();
        private DiscordRpcClient? _client;
        private bool _isInitialized;
        private bool _isEnabled = true;
        private DateTime? _gameStartTime;

        public bool IsEnabled
        {
            get
            {
                lock (_syncLock)
                {
                    return _isEnabled;
                }
            }
            set
            {
                lock (_syncLock)
                {
                    _isEnabled = value;
                    if (!_isEnabled)
                    {
                        ClearPresenceInternal();
                    }
                }
            }
        }

        public bool IsClientReady
        {
            get
            {
                lock (_syncLock)
                {
                    return _client != null && _client.IsInitialized;
                }
            }
        }

        public void Initialize(string? clientId = null)
        {
            lock (_syncLock)
            {
                // Guard clause: jika dinonaktifkan atau sudah diinisialisasi, lewati
                if (!_isEnabled || _isInitialized) return;

                try
                {
                    string id = string.IsNullOrWhiteSpace(clientId) ? DefaultClientId : clientId;
                    _client = new DiscordRpcClient(id)
                    {
                        Logger = new ConsoleLogger { Level = LogLevel.Warning },
                        SkipIdenticalPresence = true
                    };

                    _client.OnReady += (sender, msg) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Connected as {msg.User.Username}");
                    };

                    _client.OnError += (sender, msg) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Error: {msg.Message}");
                    };

                    _client.OnConnectionFailed += (sender, msg) =>
                    {
                        System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Connection Failed: Pipe {msg.FailedPipe}");
                    };

                    _client.Initialize();
                    _isInitialized = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Inisialisasi gagal: {ex.Message}");
                    _isInitialized = false;
                }
            }
        }

        /// <summary>
        /// Mengupdate Rich Presence Discord saat game Valorant aktif.
        /// Guard clause: Jangan kirim paket pipe apa pun jika IsEnabled == false.
        /// </summary>
        public void UpdateGamePresence(string resolutionText, string tweaksStateText = "RRes Competitive Tweaks Active")
        {
            lock (_syncLock)
            {
                // Guard clause: Jangan kirim apapun jika fitur dimatikan
                if (!_isEnabled) return;

                try
                {
                    if (_client == null || !_isInitialized)
                    {
                        // Inisialisasi on-demand
                        string id = DefaultClientId;
                        _client = new DiscordRpcClient(id)
                        {
                            Logger = new ConsoleLogger { Level = LogLevel.Warning },
                            SkipIdenticalPresence = true
                        };
                        _client.Initialize();
                        _isInitialized = true;
                    }

                    if (_client == null || !_isInitialized) return;

                    if (!_gameStartTime.HasValue)
                    {
                        _gameStartTime = DateTime.UtcNow;
                    }

                    _client.SetPresence(new RichPresence
                    {
                        Details = string.IsNullOrWhiteSpace(resolutionText) ? "Playing VALORANT (Stretched)" : resolutionText,
                        State = tweaksStateText,
                        Timestamps = new Timestamps(_gameStartTime.Value),
                        Assets = new Assets
                        {
                            LargeImageKey = "logo_valorant",
                            LargeImageText = "VALORANT via RRes Utility",
                            SmallImageKey = "rres_logo",
                            SmallImageText = "RRes Pro Display & Tweaks"
                        }
                    });
                }
                catch (Exception ex)
                {
                    // Aman dari crash jika Discord client ditutup paksa
                    System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Gagal update presence (Discord closed/pipe error): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Membersihkan Rich Presence saat game ditutup atau toggle dimatikan.
        /// </summary>
        public void ClearPresence()
        {
            lock (_syncLock)
            {
                ClearPresenceInternal();
            }
        }

        public void Disable()
        {
            IsEnabled = false;
        }

        public void Enable()
        {
            IsEnabled = true;
        }

        private void ClearPresenceInternal()
        {
            try
            {
                _gameStartTime = null;
                if (_client != null && _client.IsInitialized)
                {
                    _client.ClearPresence();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DiscordRPC] Gagal clear presence: {ex.Message}");
            }
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                try
                {
                    ClearPresenceInternal();
                    _client?.Dispose();
                    _client = null;
                    _isInitialized = false;
                }
                catch { }
            }
        }
    }
}
