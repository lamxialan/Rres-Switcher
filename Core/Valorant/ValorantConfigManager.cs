using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace RRes.Core.Valorant
{
    public class ValorantConfigManager
    {
        private static readonly string[] ExcludedFolders = { "CrashReportClient" };

        /// <summary>
        /// Mendapatkan path direktori konfigurasi Valorant di %LOCALAPPDATA%\VALORANT\Saved\Config
        /// </summary>
        public string GetConfigDirectoryPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "VALORANT", "Saved", "Config");
        }

        /// <summary>
        /// Memeriksa apakah direktori config Valorant tersedia di sistem.
        /// </summary>
        public bool IsValorantInstalled()
        {
            string dir = GetConfigDirectoryPath();
            return Directory.Exists(dir);
        }

        /// <summary>
        /// Mencari semua file GameUserSettings.ini pada subfolder akun pengguna dan WindowsClient.
        /// </summary>
        public List<ValorantProfile> FindProfiles()
        {
            var profiles = new List<ValorantProfile>();
            string baseDir = GetConfigDirectoryPath();

            try
            {
                if (!Directory.Exists(baseDir))
                {
                    return profiles;
                }

                var subDirs = Directory.GetDirectories(baseDir);

                foreach (var dir in subDirs)
                {
                    string dirName = Path.GetFileName(dir);
                    if (ExcludedFolders.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // Cek kemungkinan path subfolder: WindowsClient atau Windows
                    string[] candidatePaths = new[]
                    {
                        Path.Combine(dir, "WindowsClient", "GameUserSettings.ini"),
                        Path.Combine(dir, "Windows", "GameUserSettings.ini"),
                        Path.Combine(dir, "GameUserSettings.ini")
                    };

                    string[] candidateRiotPaths = new[]
                    {
                        Path.Combine(dir, "Windows", "RiotUserSettings.ini"),
                        Path.Combine(dir, "WindowsClient", "RiotUserSettings.ini"),
                        Path.Combine(dir, "RiotUserSettings.ini")
                    };

                    string? foundRiotPath = candidateRiotPaths.FirstOrDefault(File.Exists);

                    foreach (var iniPath in candidatePaths)
                    {
                        if (File.Exists(iniPath) && !profiles.Any(p => p.ConfigFilePath.Equals(iniPath, StringComparison.OrdinalIgnoreCase)))
                        {
                            var (resX, resY) = ReadResolution(iniPath);
                            double sens = foundRiotPath != null ? ReadSensitivity(foundRiotPath) : 0.35;
                            var fileInfo = new FileInfo(iniPath);

                            profiles.Add(new ValorantProfile
                            {
                                AccountId = dirName,
                                ConfigFilePath = iniPath,
                                RiotUserSettingsPath = foundRiotPath,
                                LastModified = fileInfo.LastWriteTime,
                                CurrentResX = resX,
                                CurrentResY = resY,
                                CurrentSensitivity = sens
                            });
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes ValorantConfigManager FindProfiles Warning]: {ex.Message}");
            }

            // Urutkan berdasarkan waktu modifikasi terbaru
            return profiles.OrderByDescending(p => p.LastModified).ToList();
        }

        /// <summary>
        /// Membaca resolusi yang saat ini tersimpan di file GameUserSettings.ini
        /// </summary>
        public (int Width, int Height) ReadResolution(string configPath)
        {
            if (!File.Exists(configPath))
            {
                return (0, 0);
            }

            int width = 0;
            int height = 0;

            try
            {
                using var fs = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, Encoding.UTF8);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("ResolutionSizeX=", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(trimmed.Substring("ResolutionSizeX=".Length), out width);
                    }
                    else if (trimmed.StartsWith("ResolutionSizeY=", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(trimmed.Substring("ResolutionSizeY=".Length), out height);
                    }
                }
            }
            catch
            {
                // Diamkan jika terkunci atau bermasalah saat read
            }

            return (width, height);
        }


        /// <summary>
        /// Membaca in-game mouse sensitivity dari RiotUserSettings.ini atau GameUserSettings.ini
        /// </summary>
        public double ReadSensitivity(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return 0.35;
            }

            try
            {
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("EAresFloatSettingName::MouseSensitivity=", StringComparison.OrdinalIgnoreCase))
                    {
                        string valStr = trimmed.Substring("EAresFloatSettingName::MouseSensitivity=".Length).Trim();
                        if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                        {
                            return Math.Round(parsed, 4);
                        }
                    }
                    else if (trimmed.StartsWith("MouseSensitivity=", StringComparison.OrdinalIgnoreCase))
                    {
                        string valStr = trimmed.Substring("MouseSensitivity=".Length).Trim();
                        if (double.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
                        {
                            return Math.Round(parsed, 4);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[RRes ReadSensitivity Warning]: {ex.Message}");
            }

            return 0.35;
        }

        /// <summary>
        /// Mengupdate section tertentu dalam daftar baris INI dengan dictionary key-value
        /// </summary>
        private static void UpdateSectionKeys(List<string> lines, string sectionName, Dictionary<string, string> keyValues)
        {
            string header = $"[{sectionName}]";
            int sectionStart = lines.FindIndex(l => l.Trim().Equals(header, StringComparison.OrdinalIgnoreCase));

            if (sectionStart == -1)
            {
                lines.Add(string.Empty);
                lines.Add(header);
                foreach (var kvp in keyValues)
                {
                    lines.Add($"{kvp.Key}={kvp.Value}");
                }
                return;
            }

            // Cari batas akhir section (section berikutnya atau EOF)
            int sectionEnd = lines.Count;
            for (int i = sectionStart + 1; i < lines.Count; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
                {
                    sectionEnd = i;
                    break;
                }
            }

            var updatedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = sectionStart + 1; i < sectionEnd; i++)
            {
                var line = lines[i];
                int eqIndex = line.IndexOf('=');
                if (eqIndex > 0)
                {
                    string key = line.Substring(0, eqIndex).Trim();
                    if (keyValues.TryGetValue(key, out var newVal))
                    {
                        lines[i] = $"{key}={newVal}";
                        updatedKeys.Add(key);
                    }
                }
            }

            // Tambahkan key baru yang belum ada di dalam section
            int insertPos = sectionEnd;
            foreach (var kvp in keyValues)
            {
                if (!updatedKeys.Contains(kvp.Key))
                {
                    lines.Insert(insertPos++, $"{kvp.Key}={kvp.Value}");
                }
            }
        }

        /// <summary>
        /// Menulis baris konfigurasi INI ke disk secara aman dengan pelepasan atribut Read-Only sementara,
        /// penanganan kunci file oleh proses game lain, flush buffer disk fisik, dan penguncian kembali Read-Only.
        /// </summary>
        private static void SafeWriteIniFile(string filePath, IEnumerable<string> lines, bool setReadOnly)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("Path file konfigurasi tidak boleh kosong.", nameof(filePath));
            }

            // 1. Lepas flag Read-Only sementara ke FileAttributes.Normal jika file sudah ada agar stream dapat menulis
            if (File.Exists(filePath))
            {
                try
                {
                    var currentAttrs = File.GetAttributes(filePath);
                    if ((currentAttrs & FileAttributes.ReadOnly) != 0)
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                    }
                }
                catch (Exception ex)
                {
                    throw new IOException($"Tidak dapat melepas proteksi Read-Only pada '{Path.GetFileName(filePath)}': {ex.Message}", ex);
                }
            }

            // 2. Tulis data ke file menggunakan FileStream & StreamWriter
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(fs, new UTF8Encoding(false)))
                {
                    foreach (var line in lines)
                    {
                        writer.WriteLine(line);
                    }
                    writer.Flush();
                    fs.Flush(true); // Memaksa OS mem-flush data buffer langsung ke media penyimpanan fisik
                }
                // Handle FileStream & StreamWriter telah sepenuhnya ditutup dan di-dispose di akhir blok using
            }
            catch (IOException ioEx)
            {
                throw new IOException($"File '{Path.GetFileName(filePath)}' sedang dikunci oleh proses lain (seperti VALORANT atau Riot Client). Tutup game terlebih dahulu sebelum menerapkan pengaturan. Detail: {ioEx.Message}", ioEx);
            }
            catch (UnauthorizedAccessException authEx)
            {
                throw new UnauthorizedAccessException($"Akses ditolak saat menulis ke '{Path.GetFileName(filePath)}'. Pastikan file tidak sedang dikunci sistem atau jalankan aplikasi sebagai Administrator. Detail: {authEx.Message}", authEx);
            }

            // 3. Terapkan atribut Read-Only setelah FileStream & StreamWriter benar-benar di-dispose
            try
            {
                if (setReadOnly)
                {
                    File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
                }
                else
                {
                    File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Gagal mengatur atribut Read-Only pada '{Path.GetFileName(filePath)}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Memodifikasi file GameUserSettings.ini: ResolutionSizeX, ResolutionSizeY, DesiredScreenWidth/Height dsb.
        /// Selalu memaksa mode Exclusive Fullscreen (FullscreenMode=0) untuk mengeliminasi bug Windowed Fullscreen.
        /// </summary>
        public (bool Success, string Message) ModifyResolution(string configPath, int width, int height, bool createBackup, bool setReadOnly)
        {
            if (!File.Exists(configPath))
            {
                return (false, $"File config tidak ditemukan: {configPath}");
            }

            try
            {
                // 1. Cek & lepas atribut ReadOnly jika sedang aktif agar file dapat dibackup dan diedit
                var originalAttributes = File.GetAttributes(configPath);
                if ((originalAttributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(configPath, FileAttributes.Normal);
                }

                // 2. Mekanisme Backup otomatis dengan reset atribut Read-Only pada file backup lama
                if (createBackup)
                {
                    try
                    {
                        string backupStandard = configPath + ".bak";
                        if (File.Exists(backupStandard))
                        {
                            File.SetAttributes(backupStandard, FileAttributes.Normal);
                        }
                        File.Copy(configPath, backupStandard, true);

                        // Buat juga backup timestamp
                        string backupTimestamp = $"{configPath}.{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                        File.Copy(configPath, backupTimestamp, true);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[RRes Backup Warning]: {ex.Message}");
                    }
                }

                // 3. Baca seluruh baris konfigurasi (dengan FileShare.ReadWrite untuk mencegah konflik jika dibaca proses lain)
                List<string> lines;
                using (var fsRead = new FileStream(configPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fsRead, Encoding.UTF8))
                {
                    lines = new List<string>();
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lines.Add(line);
                    }
                }

                // 4. Update resolusi: Paksa True Exclusive Fullscreen (0) dan sesuaikan seluruh dimensi
                if (width > 0 && height > 0)
                {
                    var resKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "FullscreenMode", "0" },
                        { "LastConfirmedFullscreenMode", "0" },
                        { "PreferredFullscreenMode", "0" },
                        { "ResolutionSizeX", width.ToString() },
                        { "ResolutionSizeY", height.ToString() },
                        { "LastUserConfirmedResolutionSizeX", width.ToString() },
                        { "LastUserConfirmedResolutionSizeY", height.ToString() },
                        { "DesiredScreenWidth", width.ToString() },
                        { "DesiredScreenHeight", height.ToString() },
                        { "LastUserConfirmedDesiredScreenWidth", width.ToString() },
                        { "LastUserConfirmedDesiredScreenHeight", height.ToString() },
                        { "bUseDesiredScreenHeight", "False" }
                    };

                    UpdateSectionKeys(lines, "/Script/ShooterGame.ShooterGameUserSettings", resKeys);

                    // Update juga di section Engine.GameUserSettings jika ada di file
                    if (lines.Any(l => l.Trim().Equals("[/Script/Engine.GameUserSettings]", StringComparison.OrdinalIgnoreCase)))
                    {
                        UpdateSectionKeys(lines, "/Script/Engine.GameUserSettings", resKeys);
                    }
                }

                // 5. Tulis kembali perubahan ke file secara aman dengan SafeWriteIniFile
                SafeWriteIniFile(configPath, lines, setReadOnly);

                return (true, $"Config berhasil diperbarui ke {width}x{height} (Exclusive Fullscreen).");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal memodifikasi config: {ex.Message}");
            }
        }


        /// <summary>
        /// Menulis nilai sensitivitas baru ke file RiotUserSettings.ini dan GameUserSettings.ini
        /// </summary>
        public (bool Success, string Message) ModifySensitivity(ValorantProfile profile, double newSens, bool createBackup, bool setReadOnly)
        {
            try
            {
                string sensStr = newSens.ToString("0.000000", CultureInfo.InvariantCulture);
                bool updated = false;

                // 1. Tulis ke RiotUserSettings.ini jika ada
                if (!string.IsNullOrEmpty(profile.RiotUserSettingsPath) && File.Exists(profile.RiotUserSettingsPath))
                {
                    string riotPath = profile.RiotUserSettingsPath;

                    if (createBackup)
                    {
                        try
                        {
                            string backupPath = riotPath + ".bak";
                            if (File.Exists(backupPath))
                            {
                                File.SetAttributes(backupPath, FileAttributes.Normal);
                            }
                            File.Copy(riotPath, backupPath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[RRes Backup Warning RiotUserSettings]: {ex.Message}");
                        }
                    }

                    List<string> lines;
                    using (var fsRead = new FileStream(riotPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fsRead, Encoding.UTF8))
                    {
                        lines = new List<string>();
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }

                    bool found = false;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("EAresFloatSettingName::MouseSensitivity=", StringComparison.OrdinalIgnoreCase))
                        {
                            lines[i] = $"EAresFloatSettingName::MouseSensitivity={sensStr}";
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        lines.Add($"EAresFloatSettingName::MouseSensitivity={sensStr}");
                    }

                    SafeWriteIniFile(riotPath, lines, setReadOnly);
                    updated = true;
                }

                // 2. Tulis juga ke GameUserSettings.ini untuk konsistensi Unreal Engine
                if (File.Exists(profile.ConfigFilePath))
                {
                    string iniPath = profile.ConfigFilePath;

                    if (createBackup)
                    {
                        try
                        {
                            string backupPath = iniPath + ".bak";
                            if (File.Exists(backupPath))
                            {
                                File.SetAttributes(backupPath, FileAttributes.Normal);
                            }
                            File.Copy(iniPath, backupPath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[RRes Backup Warning GameUserSettings]: {ex.Message}");
                        }
                    }

                    List<string> lines;
                    using (var fsRead = new FileStream(iniPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var reader = new StreamReader(fsRead, Encoding.UTF8))
                    {
                        lines = new List<string>();
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }
                    }

                    var sensKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "MouseSensitivity", sensStr }
                    };

                    UpdateSectionKeys(lines, "/Script/ShooterGame.ShooterGameUserSettings", sensKeys);

                    SafeWriteIniFile(iniPath, lines, setReadOnly);
                    updated = true;
                }

                profile.CurrentSensitivity = newSens;

                return updated
                    ? (true, $"Sensitivitas berhasil disimpan ({newSens:F4}).")
                    : (false, "File profil tidak ditemukan.");
            }
            catch (Exception ex)
            {
                return (false, $"Gagal menulis sensitivitas: {ex.Message}");
            }
        }

        /// <summary>
        /// Menerapkan resolusi ke semua profil Valorant yang terdeteksi.
        /// </summary>
        public (int SuccessCount, int FailCount, List<string> Messages) ApplyToAllProfiles(
            int width,
            int height,
            bool createBackup,
            bool setReadOnly)
        {
            var profiles = FindProfiles();
            int success = 0;
            int fail = 0;
            var messages = new List<string>();

            if (profiles.Count == 0)
            {
                return (0, 0, new List<string> { "Tidak ada profil Valorant yang ditemukan di folder konfigurasi." });
            }

            foreach (var profile in profiles)
            {
                var (ok, msg) = ModifyResolution(profile.ConfigFilePath, width, height, createBackup, setReadOnly);
                if (ok)
                {
                    success++;
                    messages.Add($"[{profile.AccountId}]: Sukses");
                }
                else
                {
                    fail++;
                    messages.Add($"[{profile.AccountId}]: Gagal ({msg})");
                }
            }

            return (success, fail, messages);
        }

        /// <summary>
        /// Menerapkan sensitivitas baru ke semua profil akun Valorant.
        /// </summary>
        public (int SuccessCount, int FailCount) ApplySensitivityToAllProfiles(double newSens, bool createBackup, bool setReadOnly)
        {
            var profiles = FindProfiles();
            int success = 0;
            int fail = 0;

            foreach (var profile in profiles)
            {
                var (ok, _) = ModifySensitivity(profile, newSens, createBackup, setReadOnly);
                if (ok) success++; else fail++;
            }

            return (success, fail);
        }
    }
}
