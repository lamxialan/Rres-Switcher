using System;
using System.Windows;
using System.Windows.Media;

namespace RRes.Core.Utils
{
    public static class ThemeManager
    {
        public const string ThemeDark = "Dark";
        public const string ThemeOled = "OLED";
        public const string ThemeMidnight = "Midnight";
        public const string ThemeLight = "Light";

        public static void ApplyTheme(string? themeMode, string? accentHex, string? fontFamilyName, double baseFontSize)
        {
            try
            {
                // 1. Standarisasi Palet Warna Latar & Teks
                Color bgDark, bgCard, bgCardHover, borderDark, textPrimary, textSecondary, textMuted;

                string mode = themeMode?.Trim() ?? ThemeDark;
                switch (mode)
                {
                    case ThemeOled:
                        // OLED Black (#0B0C0E - Pitch Black)
                        bgDark = Color.FromRgb(0x0B, 0x0C, 0x0E);
                        bgCard = Color.FromRgb(0x12, 0x14, 0x18);
                        bgCardHover = Color.FromRgb(0x1A, 0x1D, 0x24);
                        borderDark = Color.FromRgb(0x22, 0x26, 0x30);
                        textPrimary = Color.FromRgb(0xFF, 0xFF, 0xFF);
                        textSecondary = Color.FromRgb(0xA0, 0xA0, 0xA0);
                        textMuted = Color.FromRgb(0x6E, 0x76, 0x81);
                        break;

                    case ThemeMidnight:
                        // Midnight Navy (#0F141C - Deep Oceanic Blue)
                        bgDark = Color.FromRgb(0x0F, 0x14, 0x1C);
                        bgCard = Color.FromRgb(0x15, 0x1D, 0x2A);
                        bgCardHover = Color.FromRgb(0x1C, 0x26, 0x38);
                        borderDark = Color.FromRgb(0x28, 0x35, 0x4D);
                        textPrimary = Color.FromRgb(0xF1, 0xF5, 0xF9);
                        textSecondary = Color.FromRgb(0x94, 0xA3, 0xB8);
                        textMuted = Color.FromRgb(0x64, 0x74, 0x8B);
                        break;

                    case ThemeLight:
                        // Clean Light (#F3F4F6 - Soft Slate)
                        bgDark = Color.FromRgb(0xF3, 0xF4, 0xF6);
                        bgCard = Color.FromRgb(0xFF, 0xFF, 0xFF);
                        bgCardHover = Color.FromRgb(0xEB, 0xEE, 0xF3);
                        borderDark = Color.FromRgb(0xD1, 0xD5, 0xDB);
                        textPrimary = Color.FromRgb(0x11, 0x18, 0x27);
                        textSecondary = Color.FromRgb(0x4B, 0x55, 0x63);
                        textMuted = Color.FromRgb(0x6B, 0x72, 0x80);
                        break;

                    case ThemeDark:
                    default:
                        // Dark Mode (#16191E - Dark Charcoal)
                        bgDark = Color.FromRgb(0x16, 0x19, 0x1E);
                        bgCard = Color.FromRgb(0x1E, 0x22, 0x2B);
                        bgCardHover = Color.FromRgb(0x26, 0x2C, 0x37);
                        borderDark = Color.FromRgb(0x2C, 0x32, 0x40);
                        textPrimary = Color.FromRgb(0xF0, 0xF6, 0xFC);
                        textSecondary = Color.FromRgb(0x9E, 0x9E, 0x9E);
                        textMuted = Color.FromRgb(0x76, 0x7F, 0x8D);
                        break;
                }

                // 2. Standarisasi Aksen Warna
                Color accentColor;
                try
                {
                    accentColor = !string.IsNullOrWhiteSpace(accentHex)
                        ? (Color)ColorConverter.ConvertFromString(accentHex)
                        : Color.FromRgb(0xFF, 0x46, 0x55);
                }
                catch
                {
                    accentColor = Color.FromRgb(0xFF, 0x46, 0x55); // Crimson Red
                }

                Color accentHover = Color.FromArgb(
                    accentColor.A,
                    (byte)Math.Min(255, accentColor.R + 25),
                    (byte)Math.Min(255, accentColor.G + 25),
                    (byte)Math.Min(255, accentColor.B + 25)
                );

                Color accentPressed = Color.FromArgb(
                    accentColor.A,
                    (byte)Math.Max(0, accentColor.R - 25),
                    (byte)Math.Max(0, accentColor.G - 25),
                    (byte)Math.Max(0, accentColor.B - 25)
                );

                byte subtleAlpha = mode == ThemeLight ? (byte)32 : (byte)42;
                Color accentSubtle = Color.FromArgb(subtleAlpha, accentColor.R, accentColor.G, accentColor.B);

                // 3. Terapkan secara Realtime ke Application.Current.Resources
                var resources = Application.Current?.Resources;
                if (resources != null)
                {
                    resources["BgDarkBrush"] = new SolidColorBrush(bgDark);
                    resources["BgCardBrush"] = new SolidColorBrush(bgCard);
                    resources["BgCardHoverBrush"] = new SolidColorBrush(bgCardHover);
                    resources["BorderDarkBrush"] = new SolidColorBrush(borderDark);
                    resources["BorderFocusBrush"] = new SolidColorBrush(accentColor);
                    resources["TextPrimaryBrush"] = new SolidColorBrush(textPrimary);
                    resources["TextSecondaryBrush"] = new SolidColorBrush(textSecondary);
                    resources["TextMutedBrush"] = new SolidColorBrush(textMuted);

                    resources["ValoRedBrush"] = new SolidColorBrush(accentColor);
                    resources["ValoRedHoverBrush"] = new SolidColorBrush(accentHover);
                    resources["ValoRedPressedBrush"] = new SolidColorBrush(accentPressed);
                    resources["AccentSubtleBrush"] = new SolidColorBrush(accentSubtle);

                    // Tipografi & Font
                    if (!string.IsNullOrWhiteSpace(fontFamilyName))
                    {
                        resources["AppFontFamily"] = new FontFamily(fontFamilyName);
                    }

                    if (baseFontSize >= 10.0 && baseFontSize <= 20.0)
                    {
                        resources["AppBaseFontSize"] = baseFontSize;
                    }
                }

                // 4. Sinkronkan langsung ke MainWindow aktif
                if (Application.Current?.MainWindow is Window window)
                {
                    window.Background = new SolidColorBrush(bgDark);
                    window.Foreground = new SolidColorBrush(textPrimary);
                    if (!string.IsNullOrWhiteSpace(fontFamilyName))
                    {
                        window.FontFamily = new FontFamily(fontFamilyName);
                    }
                    if (baseFontSize >= 10.0 && baseFontSize <= 20.0)
                    {
                        window.FontSize = baseFontSize;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ThemeManager] Gagal menerapkan tema: {ex.Message}");
            }
        }
    }
}
