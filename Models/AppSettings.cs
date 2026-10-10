using System;

namespace RRes.Models
{
    public class AppSettings
    {
        public bool IsDiscordRpcEnabled { get; set; } = true;
        public bool DisableGameDvr { get; set; } = true;
        public bool IsHighPriorityEnabled { get; set; } = true;
        public bool IsMinimizeToTrayEnabled { get; set; } = false;
        public bool AutoSyncRawAccelYaw { get; set; } = true;

        // UI/UX Customization: Theme & Typography
        public string ThemeMode { get; set; } = "Dark"; // "Dark", "OLED", "Midnight", "Light"
        public string AccentColor { get; set; } = "#FF4655"; // Valorant Crimson Red
        public string FontFamily { get; set; } = "Segoe UI Variable";
        public double BaseFontSize { get; set; } = 12.5;

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
