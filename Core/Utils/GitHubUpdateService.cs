using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace RRes.Core.Utils
{
    public class UpdateCheckResult
    {
        public bool IsSuccess { get; set; }
        public bool HasUpdate { get; set; }
        public string CurrentVersion { get; set; } = string.Empty;
        public string LatestVersion { get; set; } = string.Empty;
        public string ReleaseTitle { get; set; } = string.Empty;
        public string ReleaseNotes { get; set; } = string.Empty;
        public string ReleaseUrl { get; set; } = string.Empty;
        public string? ExeDownloadUrl { get; set; }
        public string? ExeAssetName { get; set; }
        public long ExeAssetSizeBytes { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class GitHubUpdateService
    {
        public const string RepoOwner = "lamxialan";
        public const string RepoName = "Rres-Switcher";
        public const string RepoReleasesUrl = $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest";
        public const string RepoHtmlUrl = $"https://github.com/{RepoOwner}/{RepoName}";
        public const string RepoReleasesHtmlUrl = $"https://github.com/{RepoOwner}/{RepoName}/releases";

        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        public static string GetCurrentVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
        }

        /// <summary>
        /// Mengabaikan awalan 'v' atau 'V' pada nama tag release (misal: 'v1.0.1' -> '1.0.1').
        /// </summary>
        public static string NormalizeVersionString(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return "0.0.0";
            string trimmed = tag.Trim();
            while (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[1..].Trim();
            }
            return trimmed;
        }

        public static bool TryParseVersion(string versionStr, out Version version)
        {
            string normalized = NormalizeVersionString(versionStr);
            var segments = normalized.Split('.');
            if (segments.Length == 1 && int.TryParse(segments[0], out _))
            {
                normalized += ".0";
            }
            return Version.TryParse(normalized, out version!);
        }

        public static bool IsVersionNewer(string latestStr, string currentStr)
        {
            if (TryParseVersion(latestStr, out var latest) && TryParseVersion(currentStr, out var current))
            {
                return latest > current;
            }
            return false;
        }

        /// <summary>
        /// Memeriksa release terbaru di GitHub repository lamxialan/Rres-Switcher secara asynchronous.
        /// Mengabaikan huruf 'v' pada tag name dan memfilter file aset yang berakhiran '.exe'.
        /// </summary>
        public async Task<UpdateCheckResult> CheckForUpdatesAsync()
        {
            string currentVer = GetCurrentVersionString();

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, RepoReleasesUrl);
                request.Headers.Add("User-Agent", "RRes-App-Updater");
                request.Headers.Add("Accept", "application/vnd.github.v3+json");

                var response = await HttpClient.SendAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new UpdateCheckResult
                    {
                        IsSuccess = true,
                        HasUpdate = false,
                        CurrentVersion = currentVer,
                        LatestVersion = currentVer,
                        ReleaseUrl = RepoReleasesHtmlUrl,
                        Message = $"RRes v{currentVer} adalah rilis terbaru. (Belum ada rilis baru di repositori GitHub)."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        IsSuccess = false,
                        CurrentVersion = currentVer,
                        ReleaseUrl = RepoReleasesHtmlUrl,
                        Message = $"Server GitHub merespons dengan status: {response.StatusCode}."
                    };
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
                string title = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                string htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? RepoReleasesHtmlUrl : RepoReleasesHtmlUrl;
                string body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

                // 1. Mengabaikan huruf 'v' pada tag name (misal: 'v1.0.1' dibaca 1.0.1)
                string cleanLatest = NormalizeVersionString(tagName);
                bool hasNewer = IsVersionNewer(cleanLatest, currentVer);

                // 2. Hanya mencari dan mengambil file aset yang berakhiran ".exe"
                string? exeDownloadUrl = null;
                string? exeAssetName = null;
                long exeAssetSize = 0;

                if (root.TryGetProperty("assets", out var assetsEl) && assetsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var asset in assetsEl.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out var aName) ? aName.GetString() ?? "" : "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            exeAssetName = name;
                            exeDownloadUrl = asset.TryGetProperty("browser_download_url", out var aUrl) ? aUrl.GetString() : null;
                            exeAssetSize = asset.TryGetProperty("size", out var aSize) ? aSize.GetInt64() : 0;
                            break;
                        }
                    }
                }

                return new UpdateCheckResult
                {
                    IsSuccess = true,
                    HasUpdate = hasNewer,
                    CurrentVersion = currentVer,
                    LatestVersion = cleanLatest,
                    ReleaseTitle = string.IsNullOrWhiteSpace(title) ? tagName : title,
                    ReleaseNotes = body,
                    ReleaseUrl = htmlUrl,
                    ExeDownloadUrl = exeDownloadUrl,
                    ExeAssetName = exeAssetName,
                    ExeAssetSizeBytes = exeAssetSize,
                    Message = hasNewer
                        ? (exeDownloadUrl != null
                            ? $"Pembaruan baru v{cleanLatest} ({exeAssetName}) tersedia!"
                            : $"Pembaruan baru v{cleanLatest} tersedia di GitHub Releases!")
                        : $"RRes Anda berada pada versi terbaru (v{currentVer})."
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    IsSuccess = false,
                    CurrentVersion = currentVer,
                    ReleaseUrl = RepoReleasesHtmlUrl,
                    Message = $"Gagal memeriksa pembaruan: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Mengunduh file aset .exe langsung dari GitHub release. Hanya mengizinkan file berakhiran '.exe'.
        /// </summary>
        public async Task<(bool success, string filePath, string message)> DownloadAssetExeAsync(
            string downloadUrl,
            string destinationFolder,
            string? fileName = null,
            IProgress<double>? progress = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(downloadUrl))
                {
                    return (false, string.Empty, "URL download aset tidak valid.");
                }

                string resolvedName = fileName ?? Path.GetFileName(new Uri(downloadUrl).LocalPath);
                // Validasi ketat: Hanya izinkan file berekstensi .exe
                if (!resolvedName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    return (false, string.Empty, "Keamanan: Hanya file aset dengan ekstensi .exe yang diperbolehkan untuk diunduh.");
                }

                Directory.CreateDirectory(destinationFolder);
                string destinationPath = Path.Combine(destinationFolder, resolvedName);

                using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                request.Headers.Add("User-Agent", "RRes-App-Updater");

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, string.Empty, $"Gagal mengunduh installer (HTTP {response.StatusCode}).");
                }

                long totalBytes = response.Content.Headers.ContentLength ?? -1L;
                await using var sourceStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

                var buffer = new byte[81920];
                long totalRead = 0;
                int read;

                while ((read = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, read));
                    totalRead += read;
                    if (totalBytes > 0 && progress != null)
                    {
                        double percent = (double)totalRead / totalBytes * 100.0;
                        progress.Report(percent);
                    }
                }

                return (true, destinationPath, $"File installer {resolvedName} berhasil diunduh.");
            }
            catch (Exception ex)
            {
                return (false, string.Empty, $"Kesalahan saat mengunduh aset: {ex.Message}");
            }
        }
    }
}
