using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using RRes.Models;

namespace RRes.Core.Utils
{
    public class AppSettingsService
    {
        private readonly string _settingsFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly object _lock = new();

        public AppSettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            if (string.IsNullOrWhiteSpace(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            string rresDir = Path.Combine(appData, "RRes");
            EnsureDirectoryExists(rresDir);
            _settingsFilePath = Path.Combine(rresDir, "AppSettings.json");
            _legacySettingsFilePath = Path.Combine(rresDir, "config.json");
        }

        private readonly string _legacySettingsFilePath;
        public string SettingsFilePath => _settingsFilePath;

        private void EnsureDirectoryExists(string? dir = null)
        {
            try
            {
                string targetDir = dir ?? Path.GetDirectoryName(_settingsFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Gagal memastikan direktori: {ex.Message}");
            }
        }

        public AppSettings LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    EnsureDirectoryExists();
                    string targetFile = File.Exists(_settingsFilePath)
                        ? _settingsFilePath
                        : (File.Exists(_legacySettingsFilePath) ? _legacySettingsFilePath : string.Empty);

                    if (!string.IsNullOrEmpty(targetFile) && File.Exists(targetFile))
                    {
                        string json = File.ReadAllText(targetFile);
                        var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                        if (settings != null)
                        {
                            return settings;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Gagal membaca konfigurasi: {ex.Message}");
                }

                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            if (settings == null) return;

            lock (_lock)
            {
                try
                {
                    EnsureDirectoryExists();
                    settings.LastUpdated = DateTime.Now;
                    string json = JsonSerializer.Serialize(settings, JsonOptions);
                    File.WriteAllText(_settingsFilePath, json);
                    try
                    {
                        File.WriteAllText(_legacySettingsFilePath, json);
                    }
                    catch { }
                    try
                    {
                        string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppSettings.json");
                        File.WriteAllText(localPath, json);
                    }
                    catch { }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Gagal menyimpan konfigurasi: {ex.Message}");
                }
            }
        }

        public async Task SaveSettingsAsync(AppSettings settings)
        {
            await Task.Run(() => SaveSettings(settings));
        }
    }
}
