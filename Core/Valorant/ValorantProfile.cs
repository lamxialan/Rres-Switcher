using System;
using System.IO;

namespace RRes.Core.Valorant
{
    public class ValorantProfile
    {
        public string AccountId { get; set; } = string.Empty;
        public string ConfigFilePath { get; set; } = string.Empty;
        public string? RiotUserSettingsPath { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsReadOnly => File.Exists(ConfigFilePath) && (File.GetAttributes(ConfigFilePath) & FileAttributes.ReadOnly) != 0;
        public int CurrentResX { get; set; }
        public int CurrentResY { get; set; }
        public double CurrentSensitivity { get; set; } = 0.35;

        public string AccountAlias { get; set; } = string.Empty;

        public string TooltipText => $"ID Akun: {AccountId}\nFolder: {ProfileDirectory}\nResolusi Aktif: {(CurrentResX > 0 && CurrentResY > 0 ? $"{CurrentResX} × {CurrentResY}" : "Native")}\nSens: {CurrentSensitivity:F2}";

        public string ProfileDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(ConfigFilePath)) return string.Empty;
                var dir = Path.GetDirectoryName(ConfigFilePath);
                if (dir != null && (dir.EndsWith("WindowsClient", StringComparison.OrdinalIgnoreCase) || dir.EndsWith("Windows", StringComparison.OrdinalIgnoreCase)))
                {
                    return Path.GetDirectoryName(dir) ?? dir;
                }
                return dir ?? string.Empty;
            }
        }

        public string DisplayName
        {
            get
            {
                string label = !string.IsNullOrWhiteSpace(AccountAlias) ? AccountAlias : AccountId;
                string resPart = CurrentResX > 0 && CurrentResY > 0 ? $"{CurrentResX} × {CurrentResY}" : "Native";
                return $"{label} ({resPart})";
            }
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
