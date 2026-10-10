using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Win32;
using RRes.Models;

namespace RRes.Core.Mouse
{
    /// <summary>
    /// Service untuk deteksi instalasi Raw Accel, manajemen kurva akselerasi,
    /// serta sinkronisasi horizontal yaw/domain weight untuk resolusi stretched.
    /// </summary>
    public class RawAccelService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public bool IsDriverInstalled { get; private set; }
        public bool IsConfigFound { get; private set; }
        public bool IsInstalled => IsDriverInstalled || IsConfigFound;
        public string? ConfigFilePath { get; private set; }
        public string? InstallDirectory { get; private set; }
        public string StatusMessage { get; private set; } = "Belum diperiksa";

        public IReadOnlyList<RawAccelCurvePreset> CurvePresets { get; }

        public RawAccelService()
        {
            CurvePresets = InitializePresets();
            DetectInstallation();
        }

        private static List<RawAccelCurvePreset> InitializePresets()
        {
            return new List<RawAccelCurvePreset>
            {
                new RawAccelCurvePreset
                {
                    Type = RawAccelCurveType.Linear,
                    Name = "Linear (Tracking)",
                    ShortName = "Linear",
                    BadgeText = "TRACKING",
                    Description = "Akselerasi linear konstan untuk pelacakan target halus (Apex / Tracking konsisten).",
                    ModeString = "linear",
                    Acceleration = 0.05,
                    Cap = 1.5,
                    Offset = 0.0,
                    Exponent = 2.0
                },
                new RawAccelCurvePreset
                {
                    Type = RawAccelCurveType.Natural,
                    Name = "Natural (CS/Val)",
                    ShortName = "Natural",
                    BadgeText = "ESPORTS",
                    Description = "Sens rendah untuk micro-adjustment dengan akselerasi halus saat flick cepat.",
                    ModeString = "natural",
                    Acceleration = 0.025,
                    Cap = 1.35,
                    Offset = 10.0,
                    DecayRate = 0.1
                },
                new RawAccelCurvePreset
                {
                    Type = RawAccelCurveType.Jump,
                    Name = "Jump (Flick)",
                    ShortName = "Jump",
                    BadgeText = "FLICK",
                    Description = "Sens stabil di kecepatan bidik rendah dan langsung melonjak saat flick lebar.",
                    ModeString = "jump",
                    Acceleration = 0.0,
                    Cap = 1.4,
                    Offset = 12.0,
                    Smooth = 15.0
                },
                new RawAccelCurvePreset
                {
                    Type = RawAccelCurveType.Off,
                    Name = "Bypass / Off (1:1 Raw)",
                    ShortName = "Bypass/Off",
                    BadgeText = "RAW 1:1",
                    Description = "Nonaktifkan akselerasi: 1:1 Raw mouse input tanpa modifikasi kurva.",
                    ModeString = "linear",
                    Acceleration = 0.0,
                    Cap = 1.0,
                    Offset = 0.0
                }
            };
        }

        /// <summary>
        /// Mendeteksi instalasi driver Raw Accel dan file settings.json
        /// </summary>
        public (bool DriverInstalled, bool ConfigFound, string? ConfigPath, string StatusText) DetectInstallation(string? customPath = null)
        {
            try
            {
                // 1. Cek Service / Driver Kernel di Windows Registry
                bool driverRegistered = false;
                try
                {
                    using var serviceKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\rawaccel", false);
                    if (serviceKey != null)
                    {
                        driverRegistered = true;
                    }
                }
                catch
                {
                    // Abaikan jika tidak memiliki izin pembacaan registry sistem tertentu
                }

                // Cek juga proses yang sedang berjalan
                if (!driverRegistered)
                {
                    try
                    {
                        var procs = Process.GetProcessesByName("rawaccel");
                        if (procs.Length > 0)
                        {
                            driverRegistered = true;
                        }
                    }
                    catch { }
                }

                IsDriverInstalled = driverRegistered;

                // 2. Cari file settings.json
                string? foundConfigPath = null;

                if (!string.IsNullOrWhiteSpace(customPath) && File.Exists(customPath))
                {
                    foundConfigPath = customPath;
                }
                else
                {
                    var candidates = GetCandidateConfigPaths();
                    foundConfigPath = candidates.FirstOrDefault(File.Exists);
                }

                if (foundConfigPath != null)
                {
                    ConfigFilePath = Path.GetFullPath(foundConfigPath);
                    InstallDirectory = Path.GetDirectoryName(ConfigFilePath);
                    IsConfigFound = true;
                }
                else
                {
                    // Cek apakah ada folder instalasi Raw Accel dengan file rawaccel.exe atau installer.exe
                    string[] candidateDirs = new[]
                    {
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RawAccel"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RawAccel"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Raw Accel"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RawAccel"),
                        @"C:\tools\rawaccel",
                        @"C:\rawaccel"
                    };

                    string? detectedDir = null;
                    foreach (var dir in candidateDirs)
                    {
                        if (Directory.Exists(dir) && (File.Exists(Path.Combine(dir, "rawaccel.exe")) || File.Exists(Path.Combine(dir, "installer.exe"))))
                        {
                            detectedDir = dir;
                            break;
                        }
                    }

                    if (detectedDir != null)
                    {
                        InstallDirectory = detectedDir;
                        ConfigFilePath = Path.Combine(detectedDir, "settings.json");
                        IsConfigFound = true;
                    }
                    else
                    {
                        // Jika tidak ditemukan file spesifik, gunakan direktori default di AppData untuk persiapan inisialisasi
                        string defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RawAccel");
                        InstallDirectory = defaultDir;
                        ConfigFilePath = Path.Combine(defaultDir, "settings.json");
                        IsConfigFound = false;
                    }
                }

                // 3. Bangun Pesan Status
                if (IsDriverInstalled && IsConfigFound)
                {
                    StatusMessage = "Driver Ready (Active)";
                }
                else if (IsDriverInstalled && !IsConfigFound)
                {
                    StatusMessage = "Driver Ready (No Config)";
                }
                else if (!IsDriverInstalled && IsConfigFound)
                {
                    StatusMessage = "Config Detected (Driver Inactive)";
                }
                else
                {
                    StatusMessage = "Not Installed";
                }

                return (IsDriverInstalled, IsConfigFound, ConfigFilePath, StatusMessage);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deteksi: {ex.Message}";
                return (false, false, null, StatusMessage);
            }
        }

        /// <summary>
        /// Memeriksa ulang status deteksi instalasi Raw Accel.
        /// </summary>
        public bool RefreshStatus()
        {
            DetectInstallation();
            return IsInstalled;
        }

        private static IEnumerable<string> GetCandidateConfigPaths()
        {
            var paths = new List<string>();

            // Folder aplikasi saat ini / subfolder
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            paths.Add(Path.Combine(baseDir, "settings.json"));
            paths.Add(Path.Combine(baseDir, "RawAccel", "settings.json"));
            paths.Add(Path.Combine(baseDir, "Tools", "RawAccel", "settings.json"));

            // Roaming AppData (Lokasi standar v1.5+)
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            paths.Add(Path.Combine(appData, "RawAccel", "settings.json"));
            paths.Add(Path.Combine(appData, "Raw Accel", "settings.json"));

            // Local AppData
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            paths.Add(Path.Combine(localAppData, "RawAccel", "settings.json"));
            paths.Add(Path.Combine(localAppData, "Raw Accel", "settings.json"));

            // Program Files
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            paths.Add(Path.Combine(progFiles, "Raw Accel", "settings.json"));
            paths.Add(Path.Combine(progFiles, "RawAccel", "settings.json"));

            // Program Files (x86)
            string progFiles86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            paths.Add(Path.Combine(progFiles86, "Raw Accel", "settings.json"));
            paths.Add(Path.Combine(progFiles86, "RawAccel", "settings.json"));

            // Root Tools drive C:
            paths.Add(@"C:\tools\rawaccel\settings.json");
            paths.Add(@"C:\rawaccel\settings.json");
            paths.Add(@"C:\Raw Accel\settings.json");

            // Folder Downloads atau Desktop pengguna jika berupa versi portable
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            paths.Add(Path.Combine(userProfile, "Downloads", "RawAccel", "settings.json"));
            paths.Add(Path.Combine(userProfile, "Desktop", "RawAccel", "settings.json"));

            return paths;
        }

        /// <summary>
        /// Membaca file settings.json Raw Accel secara aman
        /// </summary>
        public (bool Success, JsonNode? Root, string Message) LoadSettings(string? path = null)
        {
            string targetPath = path ?? ConfigFilePath ?? "";
            if (string.IsNullOrWhiteSpace(targetPath) || !File.Exists(targetPath))
            {
                return (false, null, $"File konfigurasi tidak ditemukan: {targetPath}");
            }

            try
            {
                using var fs = new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string content = reader.ReadToEnd();

                if (string.IsNullOrWhiteSpace(content))
                {
                    return (false, null, "File settings.json kosong.");
                }

                var root = JsonNode.Parse(content);
                if (root == null)
                {
                    return (false, null, "Gagal mem-parsing format JSON dari settings.json.");
                }

                return (true, root, "Berhasil memuat settings.json.");
            }
            catch (JsonException jEx)
            {
                return (false, null, $"Format JSON tidak valid: {jEx.Message}");
            }
            catch (IOException ioEx)
            {
                return (false, null, $"File sedang terkunci atau tidak dapat diakses: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, null, $"Kesalahan saat membaca settings.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Menyimpan file settings.json Raw Accel secara aman dengan backup .bak
        /// dan memanggil writer.exe jika tersedia untuk live driver reload.
        /// </summary>
        public (bool Success, string Message) SaveSettings(string? path, JsonNode root)
        {
            string targetPath = path ?? ConfigFilePath ?? "";
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return (false, "Path file konfigurasi Raw Accel tidak valid.");
            }

            try
            {
                string? dir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Buat backup sebelum menimpa file
                if (File.Exists(targetPath))
                {
                    try
                    {
                        string bakPath = targetPath + ".bak";
                        File.Copy(targetPath, bakPath, true);
                    }
                    catch (Exception bakEx)
                    {
                        Console.Error.WriteLine($"[RRes RawAccel Backup Warning]: {bakEx.Message}");
                    }
                }

                // Tulis JSON dengan WriteIndented = true
                string jsonString = root.ToJsonString(JsonOptions);
                using (var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    writer.Write(jsonString);
                    writer.Flush();
                    fs.Flush(true);
                }

                ConfigFilePath = targetPath;
                InstallDirectory = dir;
                IsConfigFound = true;

                // Coba jalankan writer.exe jika ada di direktori yang sama untuk memperbarui driver secara real-time
                string writerNotifyMsg = "";
                if (!string.IsNullOrEmpty(dir))
                {
                    string writerExe = Path.Combine(dir, "writer.exe");
                    if (File.Exists(writerExe))
                    {
                        var (wOk, wMsg) = ExecuteWriter(writerExe, targetPath);
                        writerNotifyMsg = wOk ? " (Driver direload via writer.exe)" : $" (Peringatan writer: {wMsg})";
                    }
                }

                return (true, $"Settings Raw Accel berhasil disimpan{writerNotifyMsg}.");
            }
            catch (UnauthorizedAccessException uEx)
            {
                return (false, $"Akses ditolak saat menyimpan settings.json: {uEx.Message}. Jalankan aplikasi sebagai Administrator.");
            }
            catch (IOException ioEx)
            {
                return (false, $"File settings.json sedang dikunci oleh driver atau aplikasi lain: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menyimpan settings.json: {ex.Message}");
            }
        }

        /// <summary>
        /// Menerapkan Preset Kurva dan opsional horizontal yaw multiplier ke settings.json
        /// </summary>
        public (bool Success, string Message) ApplyPreset(
            RawAccelCurvePreset preset,
            double? yawMultiplierX = null,
            double? yawMultiplierY = null,
            string? path = null)
        {
            string targetPath = path ?? ConfigFilePath ?? "";
            var (loadOk, root, _) = LoadSettings(targetPath);

            JsonObject rootObj;
            if (loadOk && root is JsonObject existingObj)
            {
                rootObj = existingObj;
            }
            else
            {
                // Buat struktur default template Raw Accel jika belum ada file sebelumnya
                rootObj = CreateDefaultRawAccelJson();
            }

            try
            {
                // Cari atau buat profil aktif
                JsonObject profile = GetOrCreateActiveProfile(rootObj);

                // Update kurva akselerasi
                var accelParams = new JsonObject
                {
                    ["mode"] = preset.ModeString,
                    ["acceleration"] = preset.Acceleration,
                    ["offset"] = preset.Offset
                };

                if (preset.Type == RawAccelCurveType.Jump)
                {
                    accelParams["smooth"] = preset.Smooth;
                    accelParams["cap"] = new JsonObject
                    {
                        ["type"] = "output",
                        ["value"] = preset.Cap
                    };
                }
                else if (preset.Type == RawAccelCurveType.Off)
                {
                    accelParams["acceleration"] = 0.0;
                    accelParams["cap"] = new JsonObject
                    {
                        ["type"] = "output",
                        ["value"] = 1.0
                    };
                }
                else
                {
                    accelParams["cap"] = new JsonObject
                    {
                        ["type"] = "output",
                        ["value"] = preset.Cap
                    };
                    if (preset.Type == RawAccelCurveType.Natural)
                    {
                        accelParams["decayRate"] = preset.DecayRate;
                    }
                }

                profile["Whole or horizontal accel parameters"] = accelParams;

                // Update domain weight horizontal (stretched yaw compensation) jika ditentukan
                if (yawMultiplierX.HasValue)
                {
                    double xVal = Math.Round(yawMultiplierX.Value, 4);
                    double yVal = Math.Round(yawMultiplierY ?? 1.0, 4);

                    profile["Stretches domain for horizontal vs vertical inputs"] = new JsonObject
                    {
                        ["x"] = xVal,
                        ["y"] = yVal
                    };

                    // Sinkronkan juga ratio DPI Y/X untuk kompatibilitas versi
                    double yxRatio = Math.Round(yVal / xVal, 4);
                    profile["Y/X output DPI ratio"] = yxRatio;
                }

                return SaveSettings(targetPath, rootObj);
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menerapkan preset Raw Accel: {ex.Message}");
            }
        }

        /// <summary>
        /// Menerapkan sinkronisasi horizontal yaw berdasarkan resolusi monitor yang aktif
        /// (misal 4:3 -> x: 0.75, 5:4 -> x: 0.703, 16:9 -> x: 1.0).
        /// </summary>
        public (bool Success, double AppliedX, string Message) SyncStretchedYaw(int width, int height, string? path = null)
        {
            if (width <= 0 || height <= 0)
            {
                return (false, 1.0, "Dimensi resolusi tidak valid.");
            }

            double factorX = CalculateStretchedYawFactor(width, height);

            string targetPath = path ?? ConfigFilePath ?? "";
            var (loadOk, root, _) = LoadSettings(targetPath);

            JsonObject rootObj = (loadOk && root is JsonObject existingObj) ? existingObj : CreateDefaultRawAccelJson();

            try
            {
                JsonObject profile = GetOrCreateActiveProfile(rootObj);

                profile["Stretches domain for horizontal vs vertical inputs"] = new JsonObject
                {
                    ["x"] = Math.Round(factorX, 4),
                    ["y"] = 1.0
                };

                double yxRatio = Math.Round(1.0 / factorX, 4);
                profile["Y/X output DPI ratio"] = yxRatio;

                var (saveOk, saveMsg) = SaveSettings(targetPath, rootObj);
                return (saveOk, factorX, saveMsg);
            }
            catch (Exception ex)
            {
                return (false, factorX, $"Gagal sinkronisasi stretched yaw: {ex.Message}");
            }
        }

        /// <summary>
        /// Menghitung faktor pengali horizontal domain/yaw berdasarkan rasio layar terhadap 16:9 native.
        /// </summary>
        public static double CalculateStretchedYawFactor(int width, int height)
        {
            if (width <= 0 || height <= 0) return 1.0;

            double ratio = (double)width / height;
            const double nativeRatio = 16.0 / 9.0; // 1.777777...

            // Cek rasio umum dengan nilai presisi standar kompetitif
            if (Math.Abs(ratio - (4.0 / 3.0)) < 0.02)
            {
                return 0.75; // 4:3 stretched (1.333x visual stretch -> 0.75x yaw compensation)
            }
            if (Math.Abs(ratio - (5.0 / 4.0)) < 0.02)
            {
                return 0.703125; // 5:4 stretched (45/64)
            }
            if (Math.Abs(ratio - (16.0 / 10.0)) < 0.02)
            {
                return 0.900; // 16:10 stretched (9/10)
            }
            if (Math.Abs(ratio - nativeRatio) < 0.02)
            {
                return 1.0; // 16:9 native
            }

            // Rasio custom: domain weight = currentRatio / nativeRatio
            double customFactor = ratio / nativeRatio;
            return Math.Clamp(Math.Round(customFactor, 4), 0.5, 1.5);
        }

        private static JsonObject CreateDefaultRawAccelJson()
        {
            return new JsonObject
            {
                ["Driver settings"] = new JsonObject
                {
                    ["profiles"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["name"] = "default",
                            ["Stretches domain for horizontal vs vertical inputs"] = new JsonObject
                            {
                                ["x"] = 1.0,
                                ["y"] = 1.0
                            },
                            ["Stretches accel range for horizontal vs vertical inputs"] = new JsonObject
                            {
                                ["x"] = 1.0,
                                ["y"] = 1.0
                            },
                            ["Whole or horizontal accel parameters"] = new JsonObject
                            {
                                ["mode"] = "linear",
                                ["acceleration"] = 0.05,
                                ["offset"] = 0.0,
                                ["cap"] = new JsonObject
                                {
                                    ["type"] = "output",
                                    ["value"] = 1.5
                                }
                            },
                            ["Input speed calculation parameters"] = new JsonObject
                            {
                                ["Whole/combined accel"] = true,
                                ["lpNorm"] = 2
                            },
                            ["Output DPI"] = 1000,
                            ["Y/X output DPI ratio"] = 1.0,
                            ["Degrees of rotation"] = 0
                        }
                    }
                }
            };
        }

        private static JsonObject GetOrCreateActiveProfile(JsonObject root)
        {
            JsonObject driverSettings;
            if (root.TryGetPropertyValue("Driver settings", out var dsNode) && dsNode is JsonObject dsObj)
            {
                driverSettings = dsObj;
            }
            else
            {
                driverSettings = new JsonObject();
                root["Driver settings"] = driverSettings;
            }

            JsonArray profiles;
            if (driverSettings.TryGetPropertyValue("profiles", out var profNode) && profNode is JsonArray profArr)
            {
                profiles = profArr;
            }
            else
            {
                profiles = new JsonArray();
                driverSettings["profiles"] = profiles;
            }

            if (profiles.Count == 0 || profiles[0] is not JsonObject)
            {
                var newProfile = new JsonObject
                {
                    ["name"] = "default"
                };
                profiles.Add(newProfile);
                return newProfile;
            }

            return (JsonObject)profiles[0]!;
        }

        private static (bool Success, string Message) ExecuteWriter(string writerExePath, string configPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = writerExePath,
                    Arguments = $"\"{configPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    bool finished = proc.WaitForExit(2000);
                    if (finished && proc.ExitCode == 0)
                    {
                        return (true, "Writer berhasil memperbarui driver.");
                    }
                    return (true, $"Writer selesai dengan kode: {proc.ExitCode}");
                }
                return (false, "Gagal memulai proses writer.exe");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Membuka folder konfigurasi Raw Accel di Windows Explorer
        /// </summary>
        public bool OpenConfigFolder()
        {
            try
            {
                string targetDir = InstallDirectory ?? "";
                if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
                {
                    targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RawAccel");
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                }

                Process.Start("explorer.exe", targetDir);
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes OpenConfigFolder Warning]: {ex.Message}");
                return false;
            }
        }
    }
}
