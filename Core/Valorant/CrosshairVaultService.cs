using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RRes.Models;

namespace RRes.Core.Valorant
{
    public class CrosshairVaultService
    {
        private readonly string _storageFilePath;
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public CrosshairVaultService()
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
            _storageFilePath = Path.Combine(rresDir, "crosshairs.json");
        }

        public string StoragePath => _storageFilePath;

        private void EnsureDirectoryExists(string? dir = null)
        {
            try
            {
                string targetDir = dir ?? Path.GetDirectoryName(_storageFilePath) ?? string.Empty;
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrosshairVaultService] Gagal memastikan direktori: {ex.Message}");
            }
        }

        public static IReadOnlyList<CrosshairProfile> GetDefaultProPresets()
        {
            return new List<CrosshairProfile>
            {
                new CrosshairProfile
                {
                    Id = "tenz-official",
                    Name = "TenZ Precision Cyan",
                    Author = "TenZ (Sentinels)",
                    Code = "0;s;1;P;c;5;h;0;m;1;0l;4;0o;2;0a;1;0f;0;1b;0",
                    Description = "Cyan outline 1-4-2-2. Sangat stabil untuk micro-adjustment dan first bullet accuracy.",
                    Tag = "Cyan",
                    ColorHex = "#00F5D4",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "aspas-dot",
                    Name = "aspas Minimalist Dot",
                    Author = "aspas (Leviatán)",
                    Code = "0;P;c;5;o;1;d;1;z;3;f;0;0b;0;1b;0",
                    Description = "Cyan dot kompak 1 pixel dengan outline. Visibilitas tinggi untuk headshot tapping jarak jauh.",
                    Tag = "Dot",
                    ColorHex = "#38BDF8",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "forsaken-stealth",
                    Name = "f0rsakeN Stealth Black Outline",
                    Author = "f0rsakeN (Paper Rex)",
                    Code = "0;s;1;P;o;1;0t;1;0l;1;0o;1;0a;1;0f;0;1t;1;1l;1;1o;1;1a;1;1m;0;1f;0",
                    Description = "Crosshair mini outline tebal dengan center dot samar. Andalan entry duelist agresif.",
                    Tag = "Outline",
                    ColorHex = "#F3F4F6",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "jinggg-green",
                    Name = "Jinggg Crisp Green",
                    Author = "Jinggg (Paper Rex)",
                    Code = "0;s;1;P;c;1;o;1;0t;1;0l;2;0o;2;0a;1;0f;0;1b;0",
                    Description = "Green 1-2-2-0. Kontras tinggi di map cerah (Ascent, Breeze, Sunset).",
                    Tag = "Green",
                    ColorHex = "#10B981",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "boaster-brackets",
                    Name = "Boaster Bracket Dot",
                    Author = "Boaster (Fnatic)",
                    Code = "0;s;1;P;c;5;o;1;d;1;z;1;f;0;s;0;0b;0;1t;3;1l;2;1o;2;1a;1;1m;0;1f;0",
                    Description = "Cyan center dot dengan bracket mini luar. Memudahkan centering crosshair penempatan sudut.",
                    Tag = "Cyan",
                    ColorHex = "#06B6D4",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "demon1-dot",
                    Name = "Demon1 Pure White Dot",
                    Author = "Demon1 (NRG / Champions MVP)",
                    Code = "0;s;1;P;o;1;d;1;m;1;0b;0;1b;0",
                    Description = "Titik putih bersih tanpa garis dalam/luar. Fokus mutlak pada titik tengah target.",
                    Tag = "Dot",
                    ColorHex = "#FFFFFF",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "yay-classic",
                    Name = "Yay (El Diablo) Static Cross",
                    Author = "Yay (BLEED / OpTic)",
                    Code = "0;P;h;0;f;0;0l;4;0o;0;0a;1;0f;0;1b;0",
                    Description = "Solid white cross tanpa celah (gap 0). Gaya klasik Chamber operator dan headhunter.",
                    Tag = "Static",
                    ColorHex = "#E5E7EB",
                    IsCustom = false
                },
                new CrosshairProfile
                {
                    Id = "chronicle-yellow",
                    Name = "Chronicle Tactical Yellow",
                    Author = "Chronicle (Fnatic)",
                    Code = "0;P;c;7;o;1;f;0;0t;1;0l;3;0v;3;0g;1;0o;2;0a;1;0f;0;1b;0",
                    Description = "Yellow outline 1-3-2-0. Warna paling cepat diproses oleh retina mata manusia.",
                    Tag = "Yellow",
                    ColorHex = "#FBBF24",
                    IsCustom = false
                }
            };
        }

        public async Task<List<CrosshairProfile>> LoadAllProfilesAsync()
        {
            var defaultPresets = GetDefaultProPresets().ToList();
            var customProfiles = await LoadCustomProfilesAsync();

            // Gabungkan pro presets dan custom profil user
            var combined = new List<CrosshairProfile>();
            combined.AddRange(customProfiles); // Custom di atas agar user mudah melihat
            combined.AddRange(defaultPresets);
            return combined;
        }

        public async Task<List<CrosshairProfile>> LoadCustomProfilesAsync()
        {
            if (!File.Exists(_storageFilePath)) return new List<CrosshairProfile>();

            try
            {
                using var stream = File.OpenRead(_storageFilePath);
                var list = await JsonSerializer.DeserializeAsync<List<CrosshairProfile>>(stream, JsonOptions);
                return list ?? new List<CrosshairProfile>();
            }
            catch
            {
                return new List<CrosshairProfile>();
            }
        }

        public async Task<bool> SaveCustomProfileAsync(CrosshairProfile profile)
        {
            try
            {
                profile.IsCustom = true;
                var current = await LoadCustomProfilesAsync();
                
                // Jika sudah ada dengan ID sama, ganti; jika belum, tambahkan di paling depan
                int existingIdx = current.FindIndex(p => p.Id == profile.Id);
                if (existingIdx >= 0)
                {
                    current[existingIdx] = profile;
                }
                else
                {
                    current.Insert(0, profile);
                }

                EnsureDirectoryExists();
                using var stream = File.Create(_storageFilePath);
                await JsonSerializer.SerializeAsync(stream, current, JsonOptions);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteCustomProfileAsync(string id)
        {
            try
            {
                var current = await LoadCustomProfilesAsync();
                int countBefore = current.Count;
                current.RemoveAll(p => p.Id == id);
                if (current.Count != countBefore)
                {
                    EnsureDirectoryExists();
                    using var stream = File.Create(_storageFilePath);
                    await JsonSerializer.SerializeAsync(stream, current, JsonOptions);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
