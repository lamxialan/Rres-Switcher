using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RRes.Core.Display;
using RRes.Core.Mouse;
using RRes.Core.Network;
using RRes.Core.Optimization;
using RRes.Core.Utils;
using RRes.Core.Valorant;
using RRes.Core.Watcher;
using RRes.Models;

namespace RRes.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly DisplayManager _displayManager;
        private readonly ValorantConfigManager _configManager;
        private readonly GameProcessWatcher _watcher;
        private readonly MouseInputManager _mouseManager;
        private readonly RiotPingService _pingService;
        private readonly WindowsGameOptimization _gameOpt;
        private readonly RiotProcessManager _riotProcessManager;
        private readonly GitHubUpdateService _updateService;
        private readonly RawAccelService _rawAccelService;
        private readonly ValorantCleanerService _cleanerService;
        private readonly CrosshairVaultService _crosshairService;
        private readonly DiscordRpcService _discordRpcService;
        private readonly AppSettingsService _settingsService;

        // Storage Cleaner State
        private bool _isCleaning;
        private string _cleanerStatusText = "Siap membersihkan log & crash dump";
        private string _scannedJunkSize = "--";
        private int _scannedJunkFilesCount;

        // Crosshair Vault State
        private CrosshairProfile? _selectedCrosshair;
        private string _newCrosshairName = string.Empty;
        private string _newCrosshairCode = string.Empty;
        private string _newCrosshairAuthor = string.Empty;
        private string _crosshairFeedbackText = string.Empty;
        private bool _isCrosshairFeedbackVisible;

        // Discord Rich Presence State
        private bool _isDiscordRpcEnabled = true;

        // Network Diagnostics State
        private bool _isNetworkDiagnosticsRunning;
        private NetworkQualityResult? _sgQuality;
        private NetworkQualityResult? _tokyoQuality;
        private NetworkQualityResult? _hkQuality;
        private string _networkDiagnosticsStatusText = "Tekan 'Uji Jaringan Lengkap' untuk evaluasi Jitter & Packet Loss";

        private ResolutionPreset? _selectedPreset;
        private RefreshRateOption? _selectedRefreshRate;
        private bool _isCustomResolution;
        private int _customWidth = 1280;
        private int _customHeight = 960;
        private int _customRefreshRate = 144;
        private MonitorInfo? _selectedMonitor;


        private bool _isReadOnlyChecked = true;
        private bool _isBackupChecked = true;
        private bool _syncDesktopResolution = true;
        private bool _applyToAllProfiles = true;
        private bool _isAutoRevertOnGameExit = true;
        private bool _isHighPriorityEnabled = true;
        private bool _disableMouseAcceleration = true;
        private bool _disableGameDvr;
        private bool _syncSensitivityToConfig = true;
        private int _mouseDpi = 800;
        private double _inGameSens = 0.35;
        private double _scopedSensMultiplier = 1.0;
        private string _yawCompensationFeedbackText = "";
        private bool _isYawCompensationApplied;
        private string _scopedSensFeedbackText = "";
        private bool _isScopedSensApplied;
        private bool _isRestartingVanguard;
        private bool _isKillingProcesses;
        private bool _isCheckingUpdate;
        private string _updateStatusMessage = "Versi saat ini: v1.0.0 (Klik untuk periksa)";
        private string _updateStatusColor = "#8B949E";
        private bool _hasUpdateAvailable;
        private string _latestVersionString = "";
        private string _latestReleaseUrl = "https://github.com/muhbr/RRes/releases";
        private bool _isMinimizeToTrayEnabled = true;

        // Raw Accel Companion State
        private bool _isRawAccelInstalled;
        private RawAccelCurvePreset? _selectedRawAccelPreset;
        private bool _autoSyncRawAccelYaw = true;
        private string _rawAccelStatusText = "Belum diperiksa";
        private string _rawAccelConfigDisplayPath = "";
        private bool _isRawAccelDriverActive;
        private string _rawAccelStatusColor = "#6B7280";
        private string _rawAccelFeedbackText = "";
        private bool _isRawAccelFeedbackVisible;
        private double _rawAccelCurrentYawX = 1.0;
        private double _rawAccelCurrentYawY = 1.0;

        private bool _isGameRunning;
        private string _gameStatusBadge = "Game Offline";
        private string _serverPingText = "SG Server: -- ms";
        private string _serverPingColor = "#10B981";
        private string _tokyoPingText = "Tokyo: -- ms";
        private string _tokyoPingColor = "#10B981";
        private string _hkPingText = "HK Server: -- ms";
        private string _hkPingColor = "#10B981";
        private bool _isPinging;
        private int _selectedNavIndex = 0;
        private bool _isSidebarExpanded = true;
        private ValorantProfile? _selectedProfile;
        private string _statusMessage = "Siap untuk menerapkan resolusi & grafik.";
        private string _statusType = "Info"; // Info, Success, Error, Warning
        private string _currentDesktopResolution = "-";
        private string _nativeDesktopResolution = "-";
        private bool _isBusy;

        // Delegasi System Tray & Notifikasi
        public Action? RequestMinimizeToTrayAction { get; set; }
        public Action<string, string>? RequestShowNotificationAction { get; set; }

        public ObservableCollection<ResolutionPreset> Presets { get; } = new();
        public ObservableCollection<ResolutionPreset> FilteredPresets { get; } = new();
        public ObservableCollection<string> RatioFilters { get; } = new() { "Semua", "4:3", "16:9", "16:10", "5:4" };
        public ObservableCollection<ValorantProfile> Profiles { get; } = new();
        public ObservableCollection<RefreshRateOption> RefreshRateOptions { get; } = new();
        public ObservableCollection<MonitorInfo> Monitors { get; } = new();
        public ObservableCollection<string> ActivityLogs { get; } = new();

        private string _selectedRatioFilter = "Semua";
        public string SelectedRatioFilter
        {
            get => _selectedRatioFilter;
            set
            {
                if (SetProperty(ref _selectedRatioFilter, value))
                {
                    UpdateFilteredPresets();
                    OnPropertyChanged(nameof(IsRatioSemua));
                    OnPropertyChanged(nameof(IsRatio43));
                    OnPropertyChanged(nameof(IsRatio169));
                    OnPropertyChanged(nameof(IsRatio1610));
                    OnPropertyChanged(nameof(IsRatio54));
                }
            }
        }

        public bool IsRatioSemua => string.Equals(SelectedRatioFilter, "Semua", StringComparison.OrdinalIgnoreCase);
        public bool IsRatio43 => string.Equals(SelectedRatioFilter, "4:3", StringComparison.OrdinalIgnoreCase);
        public bool IsRatio169 => string.Equals(SelectedRatioFilter, "16:9", StringComparison.OrdinalIgnoreCase);
        public bool IsRatio1610 => string.Equals(SelectedRatioFilter, "16:10", StringComparison.OrdinalIgnoreCase);
        public bool IsRatio54 => string.Equals(SelectedRatioFilter, "5:4", StringComparison.OrdinalIgnoreCase);

        public void UpdateFilteredPresets()
        {
            FilteredPresets.Clear();
            var filtered = string.Equals(_selectedRatioFilter, "Semua", StringComparison.OrdinalIgnoreCase)
                ? (IEnumerable<ResolutionPreset>)Presets
                : Presets.Where(p => string.Equals(p.AspectRatio, _selectedRatioFilter, StringComparison.OrdinalIgnoreCase));

            foreach (var p in filtered)
            {
                FilteredPresets.Add(p);
            }
        }

        public ResolutionPreset? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (SetProperty(ref _selectedPreset, value))
                {
                    foreach (var p in Presets)
                    {
                        p.IsSelected = (p == value);
                    }

                    if (value != null && value.Width > 0 && value.Height > 0)
                    {
                        UpdateRefreshRatesForPreset(value.Width, value.Height);
                        UpdateRawAccelYawDisplay();
                    }
                }
            }
        }

        public RefreshRateOption? SelectedRefreshRate
        {
            get => _selectedRefreshRate;
            set => SetProperty(ref _selectedRefreshRate, value);
        }

        public bool IsCustomResolution
        {
            get => _isCustomResolution;
            set
            {
                if (SetProperty(ref _isCustomResolution, value))
                {
                    if (value)
                    {
                        UpdateRefreshRatesForPreset(_customWidth, _customHeight);
                    }
                    else if (SelectedPreset != null)
                    {
                        UpdateRefreshRatesForPreset(SelectedPreset.Width, SelectedPreset.Height);
                    }
                }
            }
        }

        public int CustomWidth
        {
            get => _customWidth;
            set
            {
                if (SetProperty(ref _customWidth, value) && _isCustomResolution && value > 0 && _customHeight > 0)
                {
                    UpdateRefreshRatesForPreset(value, _customHeight);
                }
            }
        }

        public int CustomHeight
        {
            get => _customHeight;
            set
            {
                if (SetProperty(ref _customHeight, value) && _isCustomResolution && _customWidth > 0 && value > 0)
                {
                    UpdateRefreshRatesForPreset(_customWidth, value);
                }
            }
        }

        public int CustomRefreshRate
        {
            get => _customRefreshRate;
            set => SetProperty(ref _customRefreshRate, value);
        }

        public MonitorInfo? SelectedMonitor
        {
            get => _selectedMonitor;
            set
            {
                if (SetProperty(ref _selectedMonitor, value) && value != null)
                {
                    _displayManager.CaptureInitialResolution(value.DeviceName);
                    UpdateCurrentResolutionDisplay();
                    int w = _isCustomResolution ? _customWidth : (SelectedPreset?.Width ?? 1280);
                    int h = _isCustomResolution ? _customHeight : (SelectedPreset?.Height ?? 960);
                    UpdateRefreshRatesForPreset(w, h);
                }
            }
        }


        public bool IsReadOnlyChecked
        {
            get => _isReadOnlyChecked;
            set => SetProperty(ref _isReadOnlyChecked, value);
        }

        public bool IsBackupChecked
        {
            get => _isBackupChecked;
            set => SetProperty(ref _isBackupChecked, value);
        }

        public bool SyncDesktopResolution
        {
            get => _syncDesktopResolution;
            set => SetProperty(ref _syncDesktopResolution, value);
        }

        public bool ApplyToAllProfiles
        {
            get => _applyToAllProfiles;
            set => SetProperty(ref _applyToAllProfiles, value);
        }

        public bool IsAutoRevertOnGameExit
        {
            get => _isAutoRevertOnGameExit;
            set
            {
                if (SetProperty(ref _isAutoRevertOnGameExit, value))
                {
                    _watcher.IsAutoRevertEnabled = value;
                }
            }
        }

        public bool IsHighPriorityEnabled
        {
            get => _isHighPriorityEnabled;
            set
            {
                if (SetProperty(ref _isHighPriorityEnabled, value))
                {
                    _watcher.IsHighPriorityEnabled = value;
                }
            }
        }

        public bool DisableMouseAcceleration
        {
            get => _disableMouseAcceleration;
            set
            {
                if (SetProperty(ref _disableMouseAcceleration, value))
                {
                    var (ok, msg) = _mouseManager.SetMouseAcceleration(value);
                    if (ok)
                    {
                        StatusMessage = msg;
                        StatusType = "Info";
                    }
                }
            }
        }

        public bool DisableGameDvr
        {
            get => _disableGameDvr;
            set
            {
                if (SetProperty(ref _disableGameDvr, value))
                {
                    var (ok, msg) = _gameOpt.SetGameDvrDisabled(value);
                    if (ok)
                    {
                        StatusMessage = msg;
                        StatusType = "Info";
                    }
                }
            }
        }

        public bool SyncSensitivityToConfig
        {
            get => _syncSensitivityToConfig;
            set => SetProperty(ref _syncSensitivityToConfig, value);
        }

        public int MouseDpi
        {
            get => _mouseDpi;
            set
            {
                if (value > 0 && SetProperty(ref _mouseDpi, value))
                {
                    NotifyCalculatedSensProperties();
                }
            }
        }

        public double InGameSens
        {
            get => _inGameSens;
            set
            {
                if (value >= 0 && SetProperty(ref _inGameSens, Math.Round(value, 4)))
                {
                    NotifyCalculatedSensProperties();
                }
            }
        }

        public double CurrentEdpi => Math.Round(_mouseDpi * _inGameSens, 1);

        public string EdpiCategory
        {
            get
            {
                double edpi = CurrentEdpi;
                if (edpi < 200) return "Low Sens (Arm Aim)";
                if (edpi <= 320) return "Pro Standard (Balanced)";
                if (edpi <= 450) return "Mid-High Sens";
                return "High Sens (Wrist Aim)";
            }
        }

        public double CompensatedSens43 => Math.Round(_inGameSens * 0.75, 3);
        public double CompensatedSens54 => Math.Round(_inGameSens * 0.703125, 3);

        public double ScopedSensMultiplier
        {
            get => _scopedSensMultiplier;
            set
            {
                if (value >= 0 && SetProperty(ref _scopedSensMultiplier, Math.Round(value, 3)))
                {
                    OnPropertyChanged(nameof(RecommendedScopedSens43));
                    OnPropertyChanged(nameof(RecommendedScopedSens54));
                    OnPropertyChanged(nameof(RecommendedScopedSensFocal));
                    OnPropertyChanged(nameof(EffectiveScopedEdpi));
                    OnPropertyChanged(nameof(OperatorZoom1Edpi));
                    OnPropertyChanged(nameof(OperatorZoom2Edpi));
                }
            }
        }

        public double RecommendedScopedSens43 => Math.Round(_scopedSensMultiplier * 0.75, 3);
        public double RecommendedScopedSens54 => Math.Round(_scopedSensMultiplier * 0.703, 3);
        public double RecommendedScopedSensFocal => Math.Round(_scopedSensMultiplier * 0.847, 3);
        public double EffectiveScopedEdpi => Math.Round(CurrentEdpi * _scopedSensMultiplier, 1);
        public double OperatorZoom1Edpi => Math.Round(CurrentEdpi * _scopedSensMultiplier / 2.5, 1);
        public double OperatorZoom2Edpi => Math.Round(CurrentEdpi * _scopedSensMultiplier / 5.0, 1);

        public string YawCompensationFeedbackText
        {
            get => _yawCompensationFeedbackText;
            set => SetProperty(ref _yawCompensationFeedbackText, value);
        }

        public bool IsYawCompensationApplied
        {
            get => _isYawCompensationApplied;
            set => SetProperty(ref _isYawCompensationApplied, value);
        }

        public string ScopedSensFeedbackText
        {
            get => _scopedSensFeedbackText;
            set => SetProperty(ref _scopedSensFeedbackText, value);
        }

        public bool IsScopedSensApplied
        {
            get => _isScopedSensApplied;
            set => SetProperty(ref _isScopedSensApplied, value);
        }

        // ==========================================
        // RAW ACCEL COMPANION PROPERTIES
        // ==========================================
        public IReadOnlyList<RawAccelCurvePreset> RawAccelPresets => _rawAccelService.CurvePresets;

        public RawAccelCurvePreset? SelectedRawAccelPreset
        {
            get => _selectedRawAccelPreset;
            set
            {
                if (SetProperty(ref _selectedRawAccelPreset, value))
                {
                    OnPropertyChanged(nameof(RawAccelPresetDescription));
                }
            }
        }

        public string RawAccelPresetDescription => _selectedRawAccelPreset?.Description ?? "Pilih preset kurva akselerasi.";

        public bool AutoSyncRawAccelYaw
        {
            get => _autoSyncRawAccelYaw;
            set
            {
                if (SetProperty(ref _autoSyncRawAccelYaw, value))
                {
                    UpdateRawAccelYawDisplay();
                }
            }
        }

        public bool IsRawAccelInstalled
        {
            get => _isRawAccelInstalled;
            private set
            {
                if (SetProperty(ref _isRawAccelInstalled, value))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public string RawAccelStatusText
        {
            get => _rawAccelStatusText;
            private set => SetProperty(ref _rawAccelStatusText, value);
        }

        public bool IsRawAccelDriverActive
        {
            get => _isRawAccelDriverActive;
            private set => SetProperty(ref _isRawAccelDriverActive, value);
        }

        public string RawAccelStatusColor
        {
            get => _rawAccelStatusColor;
            private set => SetProperty(ref _rawAccelStatusColor, value);
        }

        public string RawAccelConfigDisplayPath
        {
            get => _rawAccelConfigDisplayPath;
            private set => SetProperty(ref _rawAccelConfigDisplayPath, value);
        }

        public string RawAccelFeedbackText
        {
            get => _rawAccelFeedbackText;
            private set => SetProperty(ref _rawAccelFeedbackText, value);
        }

        public bool IsRawAccelFeedbackVisible
        {
            get => _isRawAccelFeedbackVisible;
            private set => SetProperty(ref _isRawAccelFeedbackVisible, value);
        }

        public double RawAccelCurrentYawX
        {
            get => _rawAccelCurrentYawX;
            private set
            {
                if (SetProperty(ref _rawAccelCurrentYawX, value))
                {
                    OnPropertyChanged(nameof(RawAccelYawInfoText));
                }
            }
        }

        public double RawAccelCurrentYawY
        {
            get => _rawAccelCurrentYawY;
            private set
            {
                if (SetProperty(ref _rawAccelCurrentYawY, value))
                {
                    OnPropertyChanged(nameof(RawAccelYawInfoText));
                }
            }
        }

        public string RawAccelYawInfoText => $"Domain Weight: X={RawAccelCurrentYawX:F3}, Y={RawAccelCurrentYawY:F3}";

        public bool IsRestartingVanguard
        {
            get => _isRestartingVanguard;
            set => SetProperty(ref _isRestartingVanguard, value);
        }

        public bool IsKillingProcesses
        {
            get => _isKillingProcesses;
            set => SetProperty(ref _isKillingProcesses, value);
        }

        public bool IsCheckingUpdate
        {
            get => _isCheckingUpdate;
            set => SetProperty(ref _isCheckingUpdate, value);
        }

        public string UpdateStatusMessage
        {
            get => _updateStatusMessage;
            set => SetProperty(ref _updateStatusMessage, value);
        }

        public string UpdateStatusColor
        {
            get => _updateStatusColor;
            set => SetProperty(ref _updateStatusColor, value);
        }

        public bool HasUpdateAvailable
        {
            get => _hasUpdateAvailable;
            set => SetProperty(ref _hasUpdateAvailable, value);
        }

        public string LatestVersionString
        {
            get => _latestVersionString;
            set => SetProperty(ref _latestVersionString, value);
        }

        public string LatestReleaseUrl
        {
            get => _latestReleaseUrl;
            set => SetProperty(ref _latestReleaseUrl, value);
        }

        public bool IsMinimizeToTrayEnabled
        {
            get => _isMinimizeToTrayEnabled;
            set => SetProperty(ref _isMinimizeToTrayEnabled, value);
        }

        // ==========================================
        // FEATURE 1: STORAGE & CACHE CLEANER
        // ==========================================
        public bool IsCleaning
        {
            get => _isCleaning;
            private set => SetProperty(ref _isCleaning, value);
        }

        public string CleanerStatusText
        {
            get => _cleanerStatusText;
            private set => SetProperty(ref _cleanerStatusText, value);
        }

        public string ScannedJunkSize
        {
            get => _scannedJunkSize;
            private set => SetProperty(ref _scannedJunkSize, value);
        }

        public int ScannedJunkFilesCount
        {
            get => _scannedJunkFilesCount;
            private set => SetProperty(ref _scannedJunkFilesCount, value);
        }

        // ==========================================
        // FEATURE 2: CROSSHAIR PROFILE VAULT
        // ==========================================
        public ObservableCollection<CrosshairProfile> CrosshairProfiles { get; } = new();

        public CrosshairProfile? SelectedCrosshair
        {
            get => _selectedCrosshair;
            set => SetProperty(ref _selectedCrosshair, value);
        }

        public string NewCrosshairName
        {
            get => _newCrosshairName;
            set => SetProperty(ref _newCrosshairName, value);
        }

        public string NewCrosshairCode
        {
            get => _newCrosshairCode;
            set => SetProperty(ref _newCrosshairCode, value);
        }

        public string NewCrosshairAuthor
        {
            get => _newCrosshairAuthor;
            set => SetProperty(ref _newCrosshairAuthor, value);
        }

        public string CrosshairFeedbackText
        {
            get => _crosshairFeedbackText;
            private set => SetProperty(ref _crosshairFeedbackText, value);
        }

        public bool IsCrosshairFeedbackVisible
        {
            get => _isCrosshairFeedbackVisible;
            private set => SetProperty(ref _isCrosshairFeedbackVisible, value);
        }

        // ==========================================
        // FEATURE 4: DISCORD RICH PRESENCE
        // ==========================================
        public bool IsDiscordRpcEnabled
        {
            get => _isDiscordRpcEnabled;
            set
            {
                if (SetProperty(ref _isDiscordRpcEnabled, value))
                {
                    OnPropertyChanged(nameof(DiscordRpcStatusBadgeText));
                    OnPropertyChanged(nameof(DiscordRpcStatusBadgeColor));
                    OnPropertyChanged(nameof(DiscordRpcStatusBadgeBg));
                    OnIsDiscordRpcEnabledChanged(value);
                }
            }
        }

        public string DiscordRpcStatusBadgeText => IsDiscordRpcEnabled ? "[AKTIF]" : "[NONAKTIF]";
        public string DiscordRpcStatusBadgeColor => IsDiscordRpcEnabled ? "#10B981" : "#6B7280";
        public string DiscordRpcStatusBadgeBg => IsDiscordRpcEnabled ? "#064E3B" : "#1F242E";

        // ==========================================
        // FEATURE 5: RIOT DIRECT PACKET LOSS & JITTER
        // ==========================================
        public bool IsNetworkDiagnosticsRunning
        {
            get => _isNetworkDiagnosticsRunning;
            private set => SetProperty(ref _isNetworkDiagnosticsRunning, value);
        }

        public NetworkQualityResult? SgQuality
        {
            get => _sgQuality;
            private set => SetProperty(ref _sgQuality, value);
        }

        public NetworkQualityResult? TokyoQuality
        {
            get => _tokyoQuality;
            private set => SetProperty(ref _tokyoQuality, value);
        }

        public NetworkQualityResult? HkQuality
        {
            get => _hkQuality;
            private set => SetProperty(ref _hkQuality, value);
        }

        public string NetworkDiagnosticsStatusText
        {
            get => _networkDiagnosticsStatusText;
            private set => SetProperty(ref _networkDiagnosticsStatusText, value);
        }

        public void AddLog(string message)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                ActivityLogs.Insert(0, $"[{timestamp}] {message}");
                while (ActivityLogs.Count > 100)
                {
                    ActivityLogs.RemoveAt(ActivityLogs.Count - 1);
                }
            });
        }

        public bool IsGameRunning
        {
            get => _isGameRunning;
            private set => SetProperty(ref _isGameRunning, value);
        }

        public string GameStatusBadge
        {
            get => _gameStatusBadge;
            private set => SetProperty(ref _gameStatusBadge, value);
        }

        public string ServerPingText
        {
            get => _serverPingText;
            private set => SetProperty(ref _serverPingText, value);
        }

        public string ServerPingColor
        {
            get => _serverPingColor;
            private set => SetProperty(ref _serverPingColor, value);
        }

        public string TokyoPingText
        {
            get => _tokyoPingText;
            private set => SetProperty(ref _tokyoPingText, value);
        }

        public string TokyoPingColor
        {
            get => _tokyoPingColor;
            private set => SetProperty(ref _tokyoPingColor, value);
        }

        public string HkPingText
        {
            get => _hkPingText;
            private set => SetProperty(ref _hkPingText, value);
        }

        public string HkPingColor
        {
            get => _hkPingColor;
            private set => SetProperty(ref _hkPingColor, value);
        }

        public bool IsPinging
        {
            get => _isPinging;
            private set => SetProperty(ref _isPinging, value);
        }

        public int SelectedNavIndex
        {
            get => _selectedNavIndex;
            set
            {
                if (SetProperty(ref _selectedNavIndex, value))
                {
                    OnPropertyChanged(nameof(IsDisplayTab));
                    OnPropertyChanged(nameof(IsMouseTab));
                    OnPropertyChanged(nameof(IsNetworkTab));
                    OnPropertyChanged(nameof(IsSettingsTab));
                    OnPropertyChanged(nameof(IsAboutTab));
                    OnPropertyChanged(nameof(CurrentNavTitle));
                    OnPropertyChanged(nameof(CurrentNavSubtitle));
                }
            }
        }

        public bool IsDisplayTab => SelectedNavIndex == 0;
        public bool IsMouseTab => SelectedNavIndex == 1;
        public bool IsNetworkTab => SelectedNavIndex == 2;
        public bool IsSettingsTab => SelectedNavIndex == 3;
        public bool IsAboutTab => SelectedNavIndex == 4;

        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set
            {
                if (SetProperty(ref _isSidebarExpanded, value))
                {
                    OnPropertyChanged(nameof(SidebarWidth));
                }
            }
        }

        public double SidebarWidth => IsSidebarExpanded ? 190 : 58;

        public string CurrentNavTitle => SelectedNavIndex switch
        {
            0 => "DISPLAY & RESOLUTION",
            1 => "AIM & MOUSE PRECISION",
            2 => "NETWORK & PERFORMANCE TWEAKS",
            3 => "SETTINGS & TAMPILAN",
            4 => "ABOUT RRES",
            _ => "RRES"
        };

        public string CurrentNavSubtitle => SelectedNavIndex switch
        {
            0 => "Resolusi stretched, refresh rate monitor, dan profil config Valorant",
            1 => "Kalkulator eDPI, rasio yaw stretched, integrasi Raw Accel, dan optimasi mouse sensor",
            2 => "Cek latensi server Riot AP, anti-stutter registry, dan DNS resolver",
            3 => "Kustomisasi tema warna antarmuka, jenis font, dan ukuran font aplikasi",
            4 => "Informasi utilitas, status profil config, dan kredit",
            _ => ""
        };

        // ==========================================
        // THEME & UI/UX SETTINGS PROPERTIES
        // ==========================================
        private string _selectedThemeMode = "Dark";
        private string _selectedAccentColor = "#FF4655";
        private string _selectedFontFamily = "Segoe UI Variable";
        private double _baseFontSize = 12.5;

        public string SelectedThemeMode
        {
            get => _selectedThemeMode;
            set
            {
                if (SetProperty(ref _selectedThemeMode, value))
                {
                    OnPropertyChanged(nameof(IsThemeDark));
                    OnPropertyChanged(nameof(IsThemeOled));
                    OnPropertyChanged(nameof(IsThemeMidnight));
                    OnPropertyChanged(nameof(IsThemeLight));
                    ApplyThemeSettings();
                    SaveAppSettings();
                }
            }
        }

        public bool IsThemeDark => string.Equals(SelectedThemeMode, "Dark", StringComparison.OrdinalIgnoreCase);
        public bool IsThemeOled => string.Equals(SelectedThemeMode, "OLED", StringComparison.OrdinalIgnoreCase);
        public bool IsThemeMidnight => string.Equals(SelectedThemeMode, "Midnight", StringComparison.OrdinalIgnoreCase);
        public bool IsThemeLight => string.Equals(SelectedThemeMode, "Light", StringComparison.OrdinalIgnoreCase);

        public string SelectedAccentColor
        {
            get => _selectedAccentColor;
            set
            {
                if (SetProperty(ref _selectedAccentColor, value))
                {
                    ApplyThemeSettings();
                    SaveAppSettings();
                }
            }
        }

        public string SelectedFontFamily
        {
            get => _selectedFontFamily;
            set
            {
                if (SetProperty(ref _selectedFontFamily, value))
                {
                    ApplyThemeSettings();
                    SaveAppSettings();
                }
            }
        }

        public double BaseFontSize
        {
            get => _baseFontSize;
            set
            {
                double clamped = Math.Clamp(Math.Round(value, 1), 11.0, 16.0);
                if (SetProperty(ref _baseFontSize, clamped))
                {
                    ApplyThemeSettings();
                    SaveAppSettings();
                }
            }
        }

        public IReadOnlyList<string> AvailableFonts { get; } = new List<string>
        {
            "Segoe UI Variable",
            "Segoe UI",
            "Inter",
            "Roboto",
            "Poppins",
            "JetBrains Mono",
            "Arial"
        };

        private void ApplyThemeSettings()
        {
            RRes.Core.Utils.ThemeManager.ApplyTheme(SelectedThemeMode, SelectedAccentColor, SelectedFontFamily, BaseFontSize);
        }

        public ValorantProfile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value) && value != null)
                {
                    if (value.CurrentSensitivity > 0)
                    {
                        InGameSens = value.CurrentSensitivity;
                    }
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string StatusType
        {
            get => _statusType;
            set => SetProperty(ref _statusType, value);
        }

        public string CurrentDesktopResolution
        {
            get => _currentDesktopResolution;
            set => SetProperty(ref _currentDesktopResolution, value);
        }

        public string NativeDesktopResolution
        {
            get => _nativeDesktopResolution;
            set => SetProperty(ref _nativeDesktopResolution, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Commands
        public ICommand ApplyResolutionCommand { get; }
        public ICommand ApplyAndLaunchCommand { get; }
        public ICommand QuickLaunchCommand { get; }
        public ICommand RevertToNativeCommand { get; }

        public ICommand RefreshProfilesCommand { get; }
        public ICommand OpenConfigFolderCommand { get; }
        public ICommand OpenSelectedProfileFolderCommand { get; }
        public ICommand SetDpiPresetCommand { get; }
        public ICommand Apply43CompensationCommand { get; }
        public ICommand Apply54CompensationCommand { get; }
        public ICommand ApplyScopedSensPresetCommand { get; }
        public ICommand SelectPresetCommand { get; }
        public ICommand RefreshPingCommand { get; }
        public ICommand ShowGpuScalingGuideCommand { get; }
        public ICommand OpenGpuControlPanelCommand { get; }
        public ICommand OpenWindowsDisplaySettingsCommand { get; }
        public ICommand ToggleSidebarCommand { get; }
        public ICommand SelectNavTabCommand { get; }
        public ICommand FlushDnsCommand { get; }
        public ICommand OpenProjectUrlCommand { get; }

        // Raw Accel Commands
        public ICommand ApplyRawAccelCommand { get; }
        public ICommand SelectRawAccelPresetCommand { get; }
        public ICommand OpenRawAccelFolderCommand { get; }
        public ICommand RefreshRawAccelCommand { get; }
        public ICommand OpenInstallRawAccelCommand { get; }

        // New Feature Commands
        public ICommand RestartVanguardCommand { get; }
        public ICommand KillRiotProcessesCommand { get; }
        public ICommand EmergencyResetAllCommand { get; }
        public ICommand ClearLogsCommand { get; }
        public ICommand CheckForUpdatesCommand { get; }
        public ICommand OpenLatestReleaseUrlCommand { get; }
        public ICommand MinimizeToTrayCommand { get; }

        // Feature 1: Cleaner Commands
        public ICommand CleanValorantStorageCommand { get; }
        public ICommand ScanValorantStorageCommand { get; }

        // Feature 2: Crosshair Vault Commands
        public ICommand CopyCrosshairCodeCommand { get; }
        public ICommand SaveNewCrosshairCommand { get; }
        public ICommand DeleteCrosshairCommand { get; }
        public ICommand RefreshCrosshairsCommand { get; }

        // Feature 3: Audio Helper Commands
        public ICommand OpenClassicSoundControlPanelCommand { get; }
        public ICommand OpenModernSoundSettingsCommand { get; }

        // Feature 5: Network Diagnostics Commands
        public ICommand RunDeepNetworkTestCommand { get; }

        // Settings & Ratio Filter Commands
        public ICommand SelectThemeCommand { get; }
        public ICommand SelectAccentColorCommand { get; }
        public ICommand ResetThemeSettingsCommand { get; }
        public ICommand SelectRatioFilterCommand { get; }

        public MainViewModel()
        {
            _displayManager = new DisplayManager();
            _configManager = new ValorantConfigManager();
            _mouseManager = new MouseInputManager();
            _pingService = new RiotPingService();
            _gameOpt = new WindowsGameOptimization();
            _riotProcessManager = new RiotProcessManager();
            _updateService = new GitHubUpdateService();
            _rawAccelService = new RawAccelService();
            _cleanerService = new ValorantCleanerService();
            _crosshairService = new CrosshairVaultService();
            _discordRpcService = new DiscordRpcService();
            _settingsService = new AppSettingsService();

            var loadedSettings = _settingsService.LoadSettings();
            _isDiscordRpcEnabled = loadedSettings.IsDiscordRpcEnabled;
            _discordRpcService.IsEnabled = _isDiscordRpcEnabled;
            if (_isDiscordRpcEnabled)
            {
                _discordRpcService.Initialize();
            }

            // Muat tema & font tersimpan
            _selectedThemeMode = !string.IsNullOrWhiteSpace(loadedSettings.ThemeMode) ? loadedSettings.ThemeMode : "Dark";
            _selectedAccentColor = !string.IsNullOrWhiteSpace(loadedSettings.AccentColor) ? loadedSettings.AccentColor : "#FF4655";
            _selectedFontFamily = !string.IsNullOrWhiteSpace(loadedSettings.FontFamily) ? loadedSettings.FontFamily : "Segoe UI Variable";
            _baseFontSize = loadedSettings.BaseFontSize >= 11.0 && loadedSettings.BaseFontSize <= 16.0 ? loadedSettings.BaseFontSize : 12.5;
            ApplyThemeSettings();

            _selectedRawAccelPreset = _rawAccelService.CurvePresets.FirstOrDefault(p => p.Type == RawAccelCurveType.Natural) ?? _rawAccelService.CurvePresets[0];
            UpdateRawAccelStatus();

            _watcher = new GameProcessWatcher
            {
                IsAutoRevertEnabled = _isAutoRevertOnGameExit,
                IsHighPriorityEnabled = _isHighPriorityEnabled
            };

            _disableMouseAcceleration = _mouseManager.IsMouseAccelerationDisabled();
            _disableGameDvr = _gameOpt.IsGameDvrDisabled();

            _watcher.GameStateChanged += OnWatcherGameStateChanged;
            _watcher.AutoRevertTriggered += OnWatcherAutoRevert;
            _watcher.StatusNotification += OnWatcherStatusNotification;

            bool CanApply() => !IsBusy && ((!IsCustomResolution && SelectedPreset != null) || (IsCustomResolution && CustomWidth >= 640 && CustomHeight >= 480));

            ApplyResolutionCommand = new RelayCommand(ExecuteApplyResolution, CanApply);
            ApplyAndLaunchCommand = new RelayCommand(ExecuteApplyAndLaunch, CanApply);
            QuickLaunchCommand = new RelayCommand(ExecuteQuickLaunch, () => !IsBusy);
            RevertToNativeCommand = new RelayCommand(ExecuteRevertToNative, () => !IsBusy);

            RefreshProfilesCommand = new RelayCommand(LoadProfiles);
            OpenConfigFolderCommand = new RelayCommand(ExecuteOpenConfigFolder);
            OpenSelectedProfileFolderCommand = new RelayCommand(ExecuteOpenSelectedProfileFolder);

            RefreshPingCommand = new RelayCommand(async () => await CheckServerPingAsync(), () => !IsPinging);
            ShowGpuScalingGuideCommand = new RelayCommand(ExecuteShowGpuGuide);
            OpenGpuControlPanelCommand = new RelayCommand(ExecuteOpenGpuControlPanel);
            OpenWindowsDisplaySettingsCommand = new RelayCommand(ExecuteOpenWindowsDisplaySettings);

            ToggleSidebarCommand = new RelayCommand(() => IsSidebarExpanded = !IsSidebarExpanded);
            SelectNavTabCommand = new RelayCommand(param =>
            {
                if (param != null && int.TryParse(param.ToString(), out int idx))
                {
                    SelectedNavIndex = idx;
                }
            });

            SelectThemeCommand = new RelayCommand(param =>
            {
                if (param != null)
                {
                    SelectedThemeMode = param.ToString()!;
                    StatusMessage = $"Tema diubah ke: {SelectedThemeMode} Mode";
                    StatusType = "Success";
                    AddLog($"Tema antarmuka diubah ke {SelectedThemeMode}.");
                }
            });

            SelectAccentColorCommand = new RelayCommand(param =>
            {
                if (param != null)
                {
                    SelectedAccentColor = param.ToString()!;
                    StatusMessage = $"Warna aksen berhasil diperbarui: {SelectedAccentColor}";
                    StatusType = "Success";
                    AddLog($"Warna aksen diubah ke {SelectedAccentColor}.");
                }
            });

            ResetThemeSettingsCommand = new RelayCommand(() =>
            {
                SelectedThemeMode = "Dark";
                SelectedAccentColor = "#FF4655";
                SelectedFontFamily = "Segoe UI Variable";
                BaseFontSize = 12.5;
                StatusMessage = "Pengaturan tema dan font direset ke default.";
                StatusType = "Info";
                AddLog("Pengaturan tema dan font direset ke default.");
            });

            SelectRatioFilterCommand = new RelayCommand(param =>
            {
                if (param != null)
                {
                    SelectedRatioFilter = param.ToString()!;
                }
            });

            FlushDnsCommand = new RelayCommand(() =>
            {
                var (ok, msg) = _gameOpt.FlushDnsCache();
                StatusMessage = msg;
                StatusType = ok ? "Success" : "Error";
                AddLog(msg);
            });

            OpenProjectUrlCommand = new RelayCommand(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/muhbr/RRes",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Tidak dapat membuka link: {ex.Message}";
                    StatusType = "Warning";
                }
            });

            SetDpiPresetCommand = new RelayCommand(param =>
            {
                if (param != null && int.TryParse(param.ToString(), out int dpi))
                {
                    MouseDpi = dpi;
                    AddLog($"DPI diubah ke {dpi}");
                }
            });

            SelectPresetCommand = new RelayCommand(param =>
            {
                if (param is ResolutionPreset preset)
                {
                    IsCustomResolution = false;
                    SelectedPreset = preset;
                    AddLog($"Preset dipilih: {preset.DisplayName}");
                }
                else if (param != null)
                {
                    string target = param.ToString()!;
                    var found = Presets.FirstOrDefault(p => p.DisplayName.Contains(target) || $"{p.Width}x{p.Height}" == target);
                    if (found != null)
                    {
                        IsCustomResolution = false;
                        SelectedPreset = found;
                        AddLog($"Preset dipilih: {found.DisplayName}");
                    }
                }
            });

            Apply43CompensationCommand = new RelayCommand(() =>
            {
                double newVal = Math.Round(InGameSens * 0.75, 3);
                InGameSens = newVal;
                YawCompensationFeedbackText = $"✓ Applied 4:3 ({newVal:F3})";
                IsYawCompensationApplied = true;
                if (AutoSyncRawAccelYaw)
                {
                    _rawAccelService.SyncStretchedYaw(1280, 960);
                    UpdateRawAccelYawDisplay();
                }
                StatusMessage = $"Sensitivitas dikompensasi untuk 4:3 Stretched: {newVal:F3}";
                StatusType = "Success";
                AddLog($"Kompensasi yaw 4:3 diterapkan -> Sens: {newVal:F3}");
            });

            Apply54CompensationCommand = new RelayCommand(() =>
            {
                double newVal = Math.Round(InGameSens * 0.703, 3);
                InGameSens = newVal;
                YawCompensationFeedbackText = $"✓ Applied 5:4 ({newVal:F3})";
                IsYawCompensationApplied = true;
                if (AutoSyncRawAccelYaw)
                {
                    _rawAccelService.SyncStretchedYaw(1280, 1024);
                    UpdateRawAccelYawDisplay();
                }
                StatusMessage = $"Sensitivitas dikompensasi untuk 5:4 Stretched: {newVal:F3}";
                StatusType = "Success";
                AddLog($"Kompensasi yaw 5:4 diterapkan -> Sens: {newVal:F3}");
            });

            ApplyRawAccelCommand = new RelayCommand(ExecuteApplyRawAccel, () => !IsBusy && IsRawAccelInstalled);
            SelectRawAccelPresetCommand = new RelayCommand(param => ExecuteSelectRawAccelPreset(param?.ToString()));
            OpenRawAccelFolderCommand = new RelayCommand(() => _rawAccelService.OpenConfigFolder());
            RefreshRawAccelCommand = new RelayCommand(RefreshRawAccelStatus);
            OpenInstallRawAccelCommand = new RelayCommand(ExecuteOpenInstallRawAccel);

            ApplyScopedSensPresetCommand = new RelayCommand(param =>
            {
                string mode = param?.ToString() ?? "43";
                double target = mode switch
                {
                    "54" => RecommendedScopedSens54,
                    "focal" => RecommendedScopedSensFocal,
                    _ => RecommendedScopedSens43
                };

                ScopedSensMultiplier = target;
                ScopedSensFeedbackText = $"✓ Applied ({target:F3})";
                IsScopedSensApplied = true;
                StatusMessage = $"Scoped Multiplier diatur ke {target:F3} ({mode}).";
                StatusType = "Success";
                AddLog($"Scoped sens multiplier disesuaikan -> {target:F3}");
            });

            RestartVanguardCommand = new RelayCommand(async () =>
            {
                if (IsRestartingVanguard) return;
                IsRestartingVanguard = true;
                AddLog("Memulai restart Windows Service Vanguard (vgc)...");
                StatusMessage = "Sedang me-restart Windows Service Vanguard (vgc)...";
                StatusType = "Info";

                try
                {
                    var (ok, msg) = await _riotProcessManager.RestartVanguardServiceAsync();
                    StatusMessage = msg;
                    StatusType = ok ? "Success" : "Error";
                    AddLog(msg);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error me-restart Vanguard: {ex.Message}";
                    StatusType = "Error";
                    AddLog($"Error Vanguard: {ex.Message}");
                }
                finally
                {
                    IsRestartingVanguard = false;
                }
            }, () => !IsRestartingVanguard);

            KillRiotProcessesCommand = new RelayCommand(async () =>
            {
                if (IsKillingProcesses) return;
                IsKillingProcesses = true;
                AddLog("Menghentikan seluruh proses Valorant & Riot Client...");
                StatusMessage = "Sedang menghentikan seluruh proses Riot...";
                StatusType = "Info";

                try
                {
                    var (ok, count, msg) = await _riotProcessManager.KillAllRiotProcessesAsync();
                    StatusMessage = msg;
                    StatusType = ok ? "Success" : "Warning";
                    AddLog(msg);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error taskkill: {ex.Message}";
                    StatusType = "Error";
                    AddLog($"Error taskkill: {ex.Message}");
                }
                finally
                {
                    IsKillingProcesses = false;
                }
            }, () => !IsKillingProcesses);

            EmergencyResetAllCommand = new RelayCommand(async () =>
            {
                if (IsRestartingVanguard || IsKillingProcesses) return;
                AddLog("Menjalankan Emergency Riot & Vanguard Reset...");
                StatusMessage = "Melakukan emergency reset Riot & Vanguard...";
                StatusType = "Info";

                await _riotProcessManager.KillAllRiotProcessesAsync();
                await Task.Delay(1500);
                var (ok, msg) = await _riotProcessManager.RestartVanguardServiceAsync();
                StatusMessage = msg;
                StatusType = ok ? "Success" : "Warning";
                AddLog("Emergency reset selesai: " + msg);
            });

            ClearLogsCommand = new RelayCommand(() =>
            {
                ActivityLogs.Clear();
                AddLog("Log aktivitas dibersihkan.");
            });

            CheckForUpdatesCommand = new RelayCommand(async () =>
            {
                if (IsCheckingUpdate) return;
                IsCheckingUpdate = true;
                UpdateStatusMessage = "Menghubungi server GitHub...";
                UpdateStatusColor = "#38BDF8";

                try
                {
                    var result = await _updateService.CheckForUpdatesAsync();
                    if (result.IsSuccess)
                    {
                        if (result.HasUpdate)
                        {
                            HasUpdateAvailable = true;
                            LatestVersionString = result.LatestVersion;
                            LatestReleaseUrl = result.ReleaseUrl;
                            UpdateStatusMessage = $"Pembaruan baru v{result.LatestVersion} tersedia!";
                            UpdateStatusColor = "#10B981";
                            AddLog($"Update ditemukan di GitHub: v{result.LatestVersion}");
                        }
                        else
                        {
                            HasUpdateAvailable = false;
                            UpdateStatusMessage = $"✓ Anda menggunakan versi terbaru (v{result.CurrentVersion}).";
                            UpdateStatusColor = "#10B981";
                            AddLog("Versi RRes adalah yang terbaru.");
                        }
                    }
                    else
                    {
                        UpdateStatusMessage = result.Message;
                        UpdateStatusColor = "#EF4444";
                        AddLog($"Gagal cek update: {result.Message}");
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatusMessage = $"Error: {ex.Message}";
                    UpdateStatusColor = "#EF4444";
                }
                finally
                {
                    IsCheckingUpdate = false;
                }
            }, () => !IsCheckingUpdate);

            OpenLatestReleaseUrlCommand = new RelayCommand(() =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = string.IsNullOrWhiteSpace(LatestReleaseUrl) ? "https://github.com/muhbr/RRes/releases" : LatestReleaseUrl,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Tidak dapat membuka link: {ex.Message}";
                    StatusType = "Warning";
                }
            });

            MinimizeToTrayCommand = new RelayCommand(() =>
            {
                RequestMinimizeToTrayAction?.Invoke();
            });

            // Feature 1: Cleaner Commands
            CleanValorantStorageCommand = new RelayCommand(async () => await ExecuteCleanStorageAsync(), () => !IsCleaning);
            ScanValorantStorageCommand = new RelayCommand(async () => await ScanJunkAsync(), () => !IsCleaning);

            // Feature 2: Crosshair Vault Commands
            CopyCrosshairCodeCommand = new RelayCommand(ExecuteCopyCrosshairCode);
            SaveNewCrosshairCommand = new RelayCommand(async () => await ExecuteSaveCrosshairAsync());
            DeleteCrosshairCommand = new RelayCommand(async param => await ExecuteDeleteCrosshairAsync(param));
            RefreshCrosshairsCommand = new RelayCommand(async () => await LoadCrosshairsAsync());

            // Feature 3: Audio Helper Commands
            OpenClassicSoundControlPanelCommand = new RelayCommand(ExecuteOpenClassicSoundControlPanel);
            OpenModernSoundSettingsCommand = new RelayCommand(ExecuteOpenModernSoundSettings);

            // Feature 5: Network Diagnostics Commands
            RunDeepNetworkTestCommand = new RelayCommand(async () => await ExecuteRunDeepNetworkTestAsync(), () => !IsNetworkDiagnosticsRunning);

            InitializeData();

            _watcher.Start();

            // Pengecekan latency awal & background loading
            _ = CheckServerPingAsync();
            _ = ScanJunkAsync();
            _ = LoadCrosshairsAsync();
        }

        public async Task CheckServerPingAsync()
        {
            if (IsPinging) return;
            IsPinging = true;
            ServerPingText = "SG: Mengukur...";
            TokyoPingText = "Tokyo: Mengukur...";
            HkPingText = "HK: Mengukur...";

            try
            {
                var sgTask = _pingService.CheckSingaporeLatencyAsync();
                var tokyoTask = _pingService.CheckTokyoLatencyAsync();
                var hkTask = _pingService.CheckHongKongLatencyAsync();

                await Task.WhenAll(sgTask, tokyoTask, hkTask);

                var sg = await sgTask;
                ServerPingText = sg.DisplayText;
                ServerPingColor = sg.StatusColor;

                var tokyo = await tokyoTask;
                TokyoPingText = tokyo.DisplayText;
                TokyoPingColor = tokyo.StatusColor;

                var hk = await hkTask;
                HkPingText = hk.DisplayText;
                HkPingColor = hk.StatusColor;
            }
            catch (Exception ex)
            {
                ServerPingText = "SG: Error";
                ServerPingColor = "#EF4444";
                Console.Error.WriteLine($"[RRes Ping Exception]: {ex.Message}");
            }
            finally
            {
                IsPinging = false;
            }
        }

        private void ExecuteShowGpuGuide()
        {
            MessageBox.Show(
                "PANDUAN AGAR LAYAR STRETCHED TANPA BLACK BAR:\n\n" +
                "1. NVIDIA Control Panel:\n" +
                "   - Buka NVIDIA Control Panel -> 'Display' -> 'Adjust desktop size and position'.\n" +
                "   - Scaling mode: Pilih 'Full-screen' (Layar Penuh).\n" +
                "   - Perform scaling on: Atur ke 'GPU'.\n" +
                "   - Centang 'Override the scaling mode set by games and programs'.\n\n" +
                "2. AMD Radeon Software:\n" +
                "   - Masuk ke 'Gaming' -> 'Display'.\n" +
                "   - Ubah 'Scaling Mode' menjadi 'Full Panel' (Bukan Preserve Aspect Ratio).\n" +
                "   - Aktifkan 'GPU Scaling'.\n\n" +
                "3. Intel Graphics Command Center:\n" +
                "   - Buka 'Display' -> 'General' -> Scale: Pilih 'Stretch' (Layar Penuh).",
                "RRes - Panduan Stretched Scaling GPU",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteOpenGpuControlPanel()
        {
            var (ok, msg) = _gameOpt.OpenGpuControlPanel();
            StatusMessage = msg;
            StatusType = ok ? "Info" : "Warning";
        }

        private void ExecuteOpenWindowsDisplaySettings()
        {
            var (ok, msg) = _gameOpt.OpenWindowsDisplaySettings();
            StatusMessage = msg;
            StatusType = ok ? "Info" : "Warning";
        }

        private void NotifyCalculatedSensProperties()
        {
            OnPropertyChanged(nameof(CurrentEdpi));
            OnPropertyChanged(nameof(EdpiCategory));
            OnPropertyChanged(nameof(CompensatedSens43));
            OnPropertyChanged(nameof(CompensatedSens54));
            OnPropertyChanged(nameof(RecommendedScopedSens43));
            OnPropertyChanged(nameof(RecommendedScopedSens54));
            OnPropertyChanged(nameof(RecommendedScopedSensFocal));
            OnPropertyChanged(nameof(EffectiveScopedEdpi));
            OnPropertyChanged(nameof(OperatorZoom1Edpi));
            OnPropertyChanged(nameof(OperatorZoom2Edpi));
        }

        private void InitializeData()
        {
            // Inisialisasi Monitor
            Monitors.Clear();
            var monitorList = _displayManager.GetConnectedMonitors();
            foreach (var m in monitorList)
            {
                Monitors.Add(m);
            }
            SelectedMonitor = Monitors.FirstOrDefault(m => m.IsPrimary) ?? Monitors.FirstOrDefault();

            var native = _displayManager.InitialResolution;
            NativeDesktopResolution = $"{native.Width}x{native.Height} ({native.AspectRatio})";
            UpdateCurrentResolutionDisplay();

            Presets.Clear();
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1280 × 960",
                Width = 1280,
                Height = 960,
                AspectRatio = "4:3",
                RatioBadgeColor = "#2C3240",
                Category = "Competitive Standard"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1440 × 1080",
                Width = 1440,
                Height = 1080,
                AspectRatio = "4:3",
                RatioBadgeColor = "#2C3240",
                Category = "HD Crisp Stretched"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1024 × 768",
                Width = 1024,
                Height = 768,
                AspectRatio = "4:3",
                RatioBadgeColor = "#2C3240",
                Category = "Ultra FPS / Low Spec"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = $"Native ({native.Width} × {native.Height})",
                Width = native.Width,
                Height = native.Height,
                AspectRatio = string.IsNullOrWhiteSpace(native.AspectRatio) ? "16:9" : native.AspectRatio,
                RatioBadgeColor = "#2C3240",
                IsNative = true,
                Category = "Default Display"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1600 × 900",
                Width = 1600,
                Height = 900,
                AspectRatio = "16:9",
                RatioBadgeColor = "#2C3240",
                Category = "Fast Performance 16:9"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1280 × 720",
                Width = 1280,
                Height = 720,
                AspectRatio = "16:9",
                RatioBadgeColor = "#2C3240",
                Category = "Max FPS 16:9"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1680 × 1050",
                Width = 1680,
                Height = 1050,
                AspectRatio = "16:10",
                RatioBadgeColor = "#2C3240",
                Category = "Balanced Stretched"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1440 × 900",
                Width = 1440,
                Height = 900,
                AspectRatio = "16:10",
                RatioBadgeColor = "#2C3240",
                Category = "Compact Stretched"
            });
            Presets.Add(new ResolutionPreset
            {
                DisplayName = "1280 × 1024",
                Width = 1280,
                Height = 1024,
                AspectRatio = "5:4",
                RatioBadgeColor = "#2C3240",
                Category = "Wide Stretched"
            });

            SelectedPreset = Presets.FirstOrDefault();
            UpdateFilteredPresets();
            UpdateRefreshRatesForPreset(SelectedPreset?.Width ?? 0, SelectedPreset?.Height ?? 0);
            LoadProfiles();

            AddLog($"RRes siap. Monitor utama: {SelectedMonitor?.DisplayName ?? "Default"} ({native.Width}x{native.Height}).");
        }

        private void UpdateRefreshRatesForPreset(int width, int height)
        {
            string? dev = SelectedMonitor?.DeviceName;
            var previousSelection = SelectedRefreshRate?.Value ?? 0;
            var rates = _displayManager.GetSupportedRefreshRates(width, height, dev);
            int maxHz = _displayManager.GetMaxSupportedRefreshRate(width, height, dev);

            RefreshRateOptions.Clear();

            RefreshRateOptions.Add(new RefreshRateOption
            {
                Value = maxHz,
                DisplayName = $"Kunci Maksimal ({maxHz} Hz)",
                IsAuto = true
            });

            // Pastikan refresh rate populer tersedia jika didukung
            var commonRates = new[] { 60, 75, 120, 144, 165, 240, 280, 360 };
            foreach (var r in commonRates)
            {
                if (!rates.Contains(r) && r <= maxHz && r >= 60)
                {
                    rates.Add(r);
                }
            }
            rates = rates.OrderByDescending(r => r).ToList();

            foreach (var rate in rates)
            {
                RefreshRateOptions.Add(new RefreshRateOption
                {
                    Value = rate,
                    DisplayName = $"{rate} Hz",
                    IsAuto = false
                });
            }

            var matching = RefreshRateOptions.FirstOrDefault(r => !r.IsAuto && r.Value == previousSelection);
            SelectedRefreshRate = matching ?? RefreshRateOptions.First();
        }

        public void LoadProfiles()
        {
            Profiles.Clear();
            var list = _configManager.FindProfiles();
            for (int i = 0; i < list.Count; i++)
            {
                list[i].AccountAlias = $"Profil Akun {i + 1}";
                Profiles.Add(list[i]);
            }

            if (Profiles.Count > 0)
            {
                SelectedProfile = Profiles.First();
                if (SelectedProfile.CurrentSensitivity > 0)
                {
                    InGameSens = SelectedProfile.CurrentSensitivity;
                }

                StatusMessage = $"Terdeteksi {Profiles.Count} profil Valorant aktif.";
                StatusType = "Info";
            }
            else
            {
                StatusMessage = "Folder config Valorant belum ditemukan atau belum ada akun tersimpan.";
                StatusType = "Warning";
            }
        }

        private void UpdateCurrentResolutionDisplay()
        {
            string? dev = SelectedMonitor?.DeviceName;
            var cur = _displayManager.GetCurrentResolution(dev);
            CurrentDesktopResolution = $"{cur.Width}x{cur.Height} @ {cur.RefreshRate}Hz";
        }


        private void ExecuteApplyResolution()
        {
            int targetW = _isCustomResolution ? _customWidth : (SelectedPreset?.Width ?? 0);
            int targetH = _isCustomResolution ? _customHeight : (SelectedPreset?.Height ?? 0);

            if (targetW <= 0 || targetH <= 0)
            {
                StatusMessage = "Pilih atau masukkan resolusi yang valid terlebih dahulu.";
                StatusType = "Warning";
                return;
            }

            IsBusy = true;
            try
            {
                string? devName = SelectedMonitor?.DeviceName;
                int targetHz = SelectedRefreshRate?.Value ?? _displayManager.GetMaxSupportedRefreshRate(targetW, targetH, devName);
                bool desktopSuccess = true;
                string desktopMsg = "";

                // 1. Terapkan akselerasi mouse jika opsi dicentang
                if (DisableMouseAcceleration)
                {
                    _mouseManager.SetMouseAcceleration(true);
                }

                // 2. Terapkan penonaktifan Game Bar jika opsi aktif
                if (DisableGameDvr)
                {
                    _gameOpt.SetGameDvrDisabled(true);
                }

                // 3. Ubah resolusi desktop jika opsi aktif
                if (SyncDesktopResolution)
                {
                    if (!IsCustomResolution && SelectedPreset != null && SelectedPreset.IsNative)
                    {
                        var (resOk, resMsg) = _displayManager.RevertToNative(devName);
                        desktopSuccess = resOk;
                        desktopMsg = resMsg;
                    }
                    else
                    {
                        var (resOk, resMsg) = _displayManager.SetResolution(targetW, targetH, targetHz, devName);
                        desktopSuccess = resOk;
                        desktopMsg = resMsg;
                    }
                }

                // 4. Modifikasi config file Valorant (Resolusi dan Sensitivitas)
                int modifiedCount = 0;
                string configMsg = "";

                if (ApplyToAllProfiles || SelectedProfile == null)
                {
                    var (successCount, failCount, _) = _configManager.ApplyToAllProfiles(
                        targetW,
                        targetH,
                        IsBackupChecked,
                        IsReadOnlyChecked);

                    modifiedCount = successCount;
                    configMsg = $"Config {successCount} akun diperbarui" + (failCount > 0 ? $", {failCount} gagal." : ".");

                    if (SyncSensitivityToConfig)
                    {
                        _configManager.ApplySensitivityToAllProfiles(InGameSens, IsBackupChecked, IsReadOnlyChecked);
                    }
                }
                else
                {
                    var (ok, msg) = _configManager.ModifyResolution(
                        SelectedProfile.ConfigFilePath,
                        targetW,
                        targetH,
                        IsBackupChecked,
                        IsReadOnlyChecked);

                    if (ok)
                    {
                        modifiedCount = 1;
                        configMsg = $"Config akun {SelectedProfile.AccountId} berhasil diperbarui.";
                        if (SyncSensitivityToConfig)
                        {
                            _configManager.ModifySensitivity(SelectedProfile, InGameSens, IsBackupChecked, IsReadOnlyChecked);
                        }
                    }
                    else
                    {
                        configMsg = $"Gagal modifikasi config: {msg}";
                    }
                }

                UpdateCurrentResolutionDisplay();
                LoadProfiles();

                string sensNote = SyncSensitivityToConfig ? $" | Sens: {InGameSens:F3}" : "";
                string rawAccelNote = "";

                if (AutoSyncRawAccelYaw)
                {
                    var (rawOk, appliedX, _) = _rawAccelService.SyncStretchedYaw(targetW, targetH);
                    if (rawOk)
                    {
                        rawAccelNote = $" | Raw Accel Yaw: {appliedX:F3}";
                        AddLog($"Raw Accel Domain Weight X disinkronkan ke {appliedX:F3} ({targetW}x{targetH}).");
                        UpdateRawAccelYawDisplay();
                    }
                }

                if (desktopSuccess && modifiedCount > 0)
                {
                    StatusMessage = $"Sukses! {targetW}x{targetH} @ {targetHz}Hz{sensNote}{rawAccelNote} diterapkan ke {(SyncDesktopResolution ? "Desktop & " : "")}{configMsg}";
                    StatusType = "Success";
                }
                else if (!desktopSuccess)
                {
                    StatusMessage = $"Config tersimpan, namun display desktop: {desktopMsg}";
                    StatusType = "Warning";
                }
                else
                {
                    StatusMessage = configMsg;
                    StatusType = "Error";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Terjadi error: {ex.Message}";
                StatusType = "Error";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteApplyAndLaunch()
        {
            ExecuteApplyResolution();

            // Berikan jeda singkat agar Windows selesai menerapkan perubahan display registry (CDS_UPDATEREGISTRY) secara bersih
            System.Threading.Thread.Sleep(250);

            // Eksekusi peluncuran game VALORANT
            var (ok, msg) = ValorantLauncher.LaunchGame();
            if (ok)
            {
                StatusMessage = $"{StatusMessage} -> {msg}";
                StatusType = "Success";
                AddLog($"Game VALORANT diluncurkan: {msg}");
            }
            else
            {
                StatusMessage = $"{StatusMessage} -> {msg}";
                StatusType = "Warning";
                AddLog($"Peringatan peluncuran game: {msg}");
            }
        }

        private void ExecuteQuickLaunch()
        {
            var (ok, msg) = ValorantLauncher.LaunchGame();
            StatusMessage = $"Quick Launch: {msg}";
            StatusType = ok ? "Success" : "Warning";
        }

        private void ExecuteRevertToNative()
        {
            IsBusy = true;
            try
            {
                string? devName = SelectedMonitor?.DeviceName;
                var (deskOk, deskMsg) = _displayManager.RevertToNative(devName);
                var native = _displayManager.InitialResolution;

                _configManager.ApplyToAllProfiles(native.Width, native.Height, IsBackupChecked, false);

                UpdateCurrentResolutionDisplay();
                LoadProfiles();

                StatusMessage = $"Reset ke Native ({native.Width}x{native.Height} @ {native.RefreshRate}Hz): {deskMsg}";
                StatusType = deskOk ? "Success" : "Warning";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal mengembalikan ke native: {ex.Message}";
                StatusType = "Error";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteOpenConfigFolder()
        {
            try
            {
                string path = _configManager.GetConfigDirectoryPath();
                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                else
                {
                    StatusMessage = "Folder config Valorant tidak ditemukan.";
                    StatusType = "Warning";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal membuka folder: {ex.Message}";
                StatusType = "Error";
            }
        }

        private void ExecuteOpenSelectedProfileFolder()
        {
            try
            {
                string path = SelectedProfile != null && Directory.Exists(SelectedProfile.ProfileDirectory)
                    ? SelectedProfile.ProfileDirectory
                    : _configManager.GetConfigDirectoryPath();

                if (Directory.Exists(path))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                    StatusMessage = $"Membuka direktori: {Path.GetFileName(path)}";
                    StatusType = "Info";
                }
                else
                {
                    StatusMessage = "Folder profil tidak ditemukan.";
                    StatusType = "Warning";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Gagal membuka folder: {ex.Message}";
                StatusType = "Error";
            }
        }

        public void RefreshStatus() => RefreshRawAccelStatus();

        public void RefreshRawAccelStatus()
        {
            UpdateRawAccelStatus();
            if (IsRawAccelInstalled)
            {
                StatusMessage = "Driver / Konfigurasi Raw Accel terdeteksi!";
                StatusType = "Success";
                RawAccelFeedbackText = "✓ Raw Accel terdeteksi dan siap digunakan.";
                IsRawAccelFeedbackVisible = true;
                AddLog("Pemeriksaan Raw Accel: Terinstal (Driver/Config terdeteksi).");
            }
            else
            {
                StatusMessage = "Driver Raw Accel belum terdeteksi di sistem.";
                StatusType = "Warning";
                RawAccelFeedbackText = "⚠ Driver Raw Accel belum terdeteksi di sistem Anda.";
                IsRawAccelFeedbackVisible = false;
                AddLog("Pemeriksaan Raw Accel: Driver belum terinstal.");
            }
        }

        private void ExecuteOpenInstallRawAccel()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/a1xd/rawaccel/releases",
                    UseShellExecute = true
                });
                StatusMessage = "Membuka link rilis resmi Raw Accel di browser...";
                StatusType = "Info";
                AddLog("Membuka browser ke https://github.com/a1xd/rawaccel/releases");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Tidak dapat membuka browser: {ex.Message}";
                StatusType = "Warning";
                AddLog($"Gagal membuka browser rilis Raw Accel: {ex.Message}");
            }
        }

        private void UpdateRawAccelStatus()
        {
            var (driverInstalled, configFound, configPath, statusText) = _rawAccelService.DetectInstallation();
            IsRawAccelDriverActive = driverInstalled;
            IsRawAccelInstalled = _rawAccelService.IsInstalled;
            RawAccelStatusText = IsRawAccelInstalled ? "Installed" : "Not Installed";
            RawAccelStatusColor = IsRawAccelInstalled ? "#10B981" : "#EF4444";

            if (configFound && !string.IsNullOrEmpty(configPath))
            {
                RawAccelConfigDisplayPath = $"{Path.GetFileName(configPath)} ({Path.GetDirectoryName(configPath)})";
            }
            else
            {
                RawAccelConfigDisplayPath = "settings.json belum ditemukan";
            }

            UpdateRawAccelYawDisplay();
        }

        private void UpdateRawAccelYawDisplay()
        {
            int targetW = _isCustomResolution ? _customWidth : (SelectedPreset?.Width ?? 1920);
            int targetH = _isCustomResolution ? _customHeight : (SelectedPreset?.Height ?? 1080);
            if (AutoSyncRawAccelYaw)
            {
                RawAccelCurrentYawX = RawAccelService.CalculateStretchedYawFactor(targetW, targetH);
                RawAccelCurrentYawY = 1.0;
            }
            else
            {
                RawAccelCurrentYawX = 1.0;
                RawAccelCurrentYawY = 1.0;
            }
        }

        private void ExecuteSelectRawAccelPreset(string? typeStr)
        {
            if (string.IsNullOrWhiteSpace(typeStr)) return;

            var match = _rawAccelService.CurvePresets.FirstOrDefault(p =>
                p.ShortName.Equals(typeStr, StringComparison.OrdinalIgnoreCase) ||
                p.Name.StartsWith(typeStr, StringComparison.OrdinalIgnoreCase) ||
                p.Type.ToString().Equals(typeStr, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                SelectedRawAccelPreset = match;
                RawAccelFeedbackText = $"Preset dipilih: {match.Name}";
                IsRawAccelFeedbackVisible = true;
            }
        }

        private void ExecuteApplyRawAccel()
        {
            if (!IsRawAccelInstalled || SelectedRawAccelPreset == null) return;

            UpdateRawAccelYawDisplay();
            double? yawX = AutoSyncRawAccelYaw ? RawAccelCurrentYawX : null;
            double? yawY = AutoSyncRawAccelYaw ? RawAccelCurrentYawY : null;

            var (ok, msg) = _rawAccelService.ApplyPreset(SelectedRawAccelPreset, yawX, yawY);
            if (ok)
            {
                string syncNote = AutoSyncRawAccelYaw ? $" (Domain Weight X: {RawAccelCurrentYawX:F3})" : "";
                RawAccelFeedbackText = $"✓ Kurva '{SelectedRawAccelPreset.ShortName}' diterapkan{syncNote}";
                IsRawAccelFeedbackVisible = true;
                StatusMessage = $"Konfigurasi Raw Accel berhasil diperbarui: {SelectedRawAccelPreset.Name}{syncNote}.";
                StatusType = "Success";
                AddLog($"Raw Accel profile updated -> {SelectedRawAccelPreset.Name}{syncNote}");
            }
            else
            {
                RawAccelFeedbackText = $"⚠ {msg}";
                IsRawAccelFeedbackVisible = true;
                StatusMessage = $"Gagal menerapkan Raw Accel: {msg}";
                StatusType = "Warning";
                AddLog($"Peringatan Raw Accel: {msg}");
            }
        }

        #region Feature 1: Valorant Storage & Cache Cleaner

        private async Task ScanJunkAsync()
        {
            try
            {
                var (bytes, count) = await _cleanerService.ScanJunkAsync();
                ScannedJunkSize = CleanerResult.FormatBytes(bytes);
                ScannedJunkFilesCount = count;
                CleanerStatusText = count > 0
                    ? $"Ditemukan {ScannedJunkSize} ({count} file) log & crash dump."
                    : "Direktori log & cache sudah bersih (0 file sampah).";
            }
            catch { }
        }

        private async Task ExecuteCleanStorageAsync()
        {
            if (IsCleaning) return;
            IsCleaning = true;
            CleanerStatusText = "Sedang membersihkan file sampah & crash logs...";
            StatusMessage = "Membersihkan log dan crash dump Valorant...";
            StatusType = "Info";

            try
            {
                var result = await _cleanerService.CleanStorageAsync();
                CleanerStatusText = result.Message;
                StatusMessage = result.Message;
                StatusType = result.Success ? "Success" : "Warning";
                AddLog($"[Cleaner] {result.Message}");

                await ScanJunkAsync();
                RequestShowNotificationAction?.Invoke("Storage Cleaner", result.Message);
            }
            catch (Exception ex)
            {
                CleanerStatusText = $"Error: {ex.Message}";
                StatusMessage = $"Gagal membersihkan storage: {ex.Message}";
                StatusType = "Error";
                AddLog($"[Cleaner Error] {ex.Message}");
            }
            finally
            {
                IsCleaning = false;
            }
        }

        #endregion

        #region Feature 2: Crosshair Profile Vault

        public async Task LoadCrosshairsAsync()
        {
            try
            {
                var list = await _crosshairService.LoadAllProfilesAsync();
                CrosshairProfiles.Clear();
                foreach (var p in list)
                {
                    CrosshairProfiles.Add(p);
                }
                if (SelectedCrosshair == null && CrosshairProfiles.Count > 0)
                {
                    SelectedCrosshair = CrosshairProfiles[0];
                }
            }
            catch (Exception ex)
            {
                AddLog($"[Crosshair Vault] Gagal memuat profil: {ex.Message}");
            }
        }

        private void ExecuteCopyCrosshairCode(object? param)
        {
            string? code = null;
            string name = "Crosshair";

            if (param is CrosshairProfile profile)
            {
                code = profile.Code;
                name = profile.Name;
            }
            else if (param is string s && !string.IsNullOrWhiteSpace(s))
            {
                code = s;
                name = "Kode";
            }
            else if (SelectedCrosshair != null)
            {
                code = SelectedCrosshair.Code;
                name = SelectedCrosshair.Name;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                try
                {
                    Clipboard.SetText(code);
                    CrosshairFeedbackText = $"✓ Kode '{name}' tersalin ke Clipboard!";
                    IsCrosshairFeedbackVisible = true;
                    StatusMessage = $"Kode crosshair '{name}' berhasil disalin. Paste di menu pengaturan Crosshair in-game!";
                    StatusType = "Success";
                    AddLog($"[Crosshair Vault] Kode crosshair disalin: {name}");
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Gagal menyalin ke clipboard: {ex.Message}";
                    StatusType = "Warning";
                }
            }
        }

        private async Task ExecuteSaveCrosshairAsync()
        {
            if (string.IsNullOrWhiteSpace(NewCrosshairCode))
            {
                StatusMessage = "Masukkan kode crosshair Valorant terlebih dahulu!";
                StatusType = "Warning";
                return;
            }

            string name = string.IsNullOrWhiteSpace(NewCrosshairName) ? "Custom Crosshair" : NewCrosshairName.Trim();
            string author = string.IsNullOrWhiteSpace(NewCrosshairAuthor) ? "User Custom" : NewCrosshairAuthor.Trim();

            var newProf = new CrosshairProfile
            {
                Name = name,
                Author = author,
                Code = NewCrosshairCode.Trim(),
                Description = "Crosshair kustom disimpan oleh pengguna di RRes Vault.",
                Tag = "Custom",
                ColorHex = "#FF4655",
                IsCustom = true
            };

            bool saved = await _crosshairService.SaveCustomProfileAsync(newProf);
            if (saved)
            {
                await LoadCrosshairsAsync();
                SelectedCrosshair = CrosshairProfiles.FirstOrDefault(p => p.Id == newProf.Id);
                NewCrosshairName = string.Empty;
                NewCrosshairCode = string.Empty;
                NewCrosshairAuthor = string.Empty;
                CrosshairFeedbackText = $"✓ Crosshair '{name}' berhasil disimpan ke Vault!";
                IsCrosshairFeedbackVisible = true;
                StatusMessage = $"Crosshair '{name}' berhasil disimpan!";
                StatusType = "Success";
                AddLog($"[Crosshair Vault] Profil custom disimpan: {name}");
            }
            else
            {
                StatusMessage = "Gagal menyimpan crosshair ke file penyimpanan.";
                StatusType = "Error";
            }
        }

        private async Task ExecuteDeleteCrosshairAsync(object? param)
        {
            if (param is CrosshairProfile profile && profile.IsCustom)
            {
                await _crosshairService.DeleteCustomProfileAsync(profile.Id);
                await LoadCrosshairsAsync();
                StatusMessage = $"Crosshair custom '{profile.Name}' dihapus dari Vault.";
                StatusType = "Info";
                AddLog($"[Crosshair Vault] Profil dihapus: {profile.Name}");
            }
        }

        #endregion

        #region Feature 3: Audio Helper

        private void ExecuteOpenClassicSoundControlPanel()
        {
            var (ok, msg) = WindowsAudioHelper.OpenClassicSoundControlPanel();
            StatusMessage = msg;
            StatusType = ok ? "Info" : "Warning";
            AddLog($"[Audio Helper] {msg}");
        }

        private void ExecuteOpenModernSoundSettings()
        {
            var (ok, msg) = WindowsAudioHelper.OpenModernSoundSettings();
            StatusMessage = msg;
            StatusType = ok ? "Info" : "Warning";
            AddLog($"[Audio Helper] {msg}");
        }

        #endregion

        #region Feature 4: Discord Rich Presence

        private void OnIsDiscordRpcEnabledChanged(bool isEnabled)
        {
            _discordRpcService.IsEnabled = isEnabled;

            if (isEnabled)
            {
                _discordRpcService.Initialize();
                if (IsGameRunning)
                {
                    UpdateDiscordPresence();
                    AddLog("[Discord RPC] Rich Presence diaktifkan dan segera dipancarkan ke Discord.");
                }
                else
                {
                    AddLog("[Discord RPC] Rich Presence diaktifkan (siaga menunggu game VALORANT berjalan).");
                }
                StatusMessage = "Discord Rich Presence diaktifkan.";
                StatusType = "Success";
            }
            else
            {
                _discordRpcService.ClearPresence();
                AddLog("[Discord RPC] Rich Presence dinonaktifkan (status profil Discord langsung dibersihkan).");
                StatusMessage = "Discord Rich Presence dinonaktifkan.";
                StatusType = "Info";
            }

            SaveAppSettings();
        }

        public void UpdateDiscordPresence()
        {
            if (!IsDiscordRpcEnabled || !IsGameRunning) return;

            try
            {
                string res = CurrentDesktopResolution;
                string mode = SelectedPreset?.DisplayName ?? (IsCustomResolution ? "Custom" : "Native");
                _discordRpcService.UpdateGamePresence($"Playing VALORANT • {res}", $"Preset: {mode} • RRes Tweaks Active");
            }
            catch { }
        }

        private void SaveAppSettings()
        {
            try
            {
                var current = _settingsService.LoadSettings();
                current.IsDiscordRpcEnabled = _isDiscordRpcEnabled;
                current.DisableGameDvr = _disableGameDvr;
                current.IsHighPriorityEnabled = _isHighPriorityEnabled;
                current.IsMinimizeToTrayEnabled = _isMinimizeToTrayEnabled;
                current.AutoSyncRawAccelYaw = _autoSyncRawAccelYaw;
                current.ThemeMode = _selectedThemeMode;
                current.AccentColor = _selectedAccentColor;
                current.FontFamily = _selectedFontFamily;
                current.BaseFontSize = _baseFontSize;
                _ = _settingsService.SaveSettingsAsync(current);
            }
            catch { }
        }

        #endregion

        #region Feature 5: Riot Direct Packet Loss & Jitter Tester

        private async Task ExecuteRunDeepNetworkTestAsync()
        {
            if (IsNetworkDiagnosticsRunning) return;
            IsNetworkDiagnosticsRunning = true;
            NetworkDiagnosticsStatusText = "Mengirim 10 paket probe ke AWS Singapore, HK, dan Tokyo...";
            StatusMessage = "Menguji latensi rata-rata, jitter, dan packet loss ke server Riot...";
            StatusType = "Info";
            AddLog("Memulai uji diagnostik jaringan batch...");

            try
            {
                var sgTask = _pingService.RunBatchQualityTestAsync("Singapore AP", RiotPingService.SgHost, 10);
                var hkTask = _pingService.RunBatchQualityTestAsync("Hong Kong", RiotPingService.HkHost, 10);
                var tokyoTask = _pingService.RunBatchQualityTestAsync("Tokyo JP", RiotPingService.TokyoHost, 10);

                await Task.WhenAll(sgTask, hkTask, tokyoTask);

                SgQuality = await sgTask;
                HkQuality = await hkTask;
                TokyoQuality = await tokyoTask;

                ServerPingText = $"SG: {SgQuality.AverageLatencyMs:F0}ms (Loss {SgQuality.PacketLossPercent}%)";
                ServerPingColor = SgQuality.QualityColor;
                TokyoPingText = $"Tokyo: {TokyoQuality.AverageLatencyMs:F0}ms";
                TokyoPingColor = TokyoQuality.QualityColor;
                HkPingText = $"HK: {HkQuality.AverageLatencyMs:F0}ms";
                HkPingColor = HkQuality.QualityColor;

                NetworkDiagnosticsStatusText = $"SG Avg: {SgQuality.AverageLatencyMs:F0}ms | Jitter: {SgQuality.JitterMs:F1}ms | Loss: {SgQuality.PacketLossPercent}%. Status: {SgQuality.QualityStatus}";
                StatusMessage = $"Uji jaringan selesai: SG {SgQuality.AverageLatencyMs:F0}ms, Jitter {SgQuality.JitterMs:F1}ms. Status: {SgQuality.QualityStatus}.";
                StatusType = SgQuality.QualityColor == "#10B981" ? "Success" : "Warning";
                AddLog($"[Network Diagnostics] SG: Avg {SgQuality.AverageLatencyMs:F0}ms, Jitter {SgQuality.JitterMs:F1}ms, Loss {SgQuality.PacketLossPercent}% -> {SgQuality.QualityStatus}");
            }
            catch (Exception ex)
            {
                NetworkDiagnosticsStatusText = $"Error diagnostik: {ex.Message}";
                StatusMessage = $"Gagal menguji jaringan: {ex.Message}";
                StatusType = "Error";
                AddLog($"[Network Error] {ex.Message}");
            }
            finally
            {
                IsNetworkDiagnosticsRunning = false;
            }
        }

        #endregion

        #region Watcher Event Handlers

        private void OnWatcherGameStateChanged(bool isRunning)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                IsGameRunning = isRunning;
                GameStatusBadge = isRunning ? "In-Game Aktif" : "Game Offline";
                AddLog(isRunning ? "Proses VALORANT aktif (Game berjalan)." : "Proses VALORANT telah keluar (Offline).");

                if (isRunning)
                {
                    if (IsDiscordRpcEnabled)
                    {
                        UpdateDiscordPresence();
                    }

                    if (IsAutoRevertOnGameExit && IsMinimizeToTrayEnabled)
                    {
                        RequestMinimizeToTrayAction?.Invoke();
                        AddLog("RRes otomatis di-minimize ke System Tray saat game berjalan.");
                    }
                }
                else
                {
                    _discordRpcService.ClearPresence();
                }
            });
        }

        private void OnWatcherAutoRevert()
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                string? dev = SelectedMonitor?.DeviceName;
                _displayManager.RevertToNative(dev);
                UpdateCurrentResolutionDisplay();
                StatusMessage = "Game VALORANT ditutup: Resolusi otomatis di-revert ke Native!";
                StatusType = "Success";
                AddLog("Auto-Revert: Resolusi desktop dan konfigurasi game dikembalikan ke Native.");
                RequestShowNotificationAction?.Invoke("Resolusi Dikembalikan", "VALORANT telah ditutup. Resolusi monitor dikembalikan ke Native.");
            });
        }

        private void OnWatcherStatusNotification(string message, string type)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                StatusMessage = message;
                StatusType = type;
                AddLog(message);
            });
        }

        #endregion
    }
}
