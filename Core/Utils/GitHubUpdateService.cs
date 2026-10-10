using System;
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
        public string Message { get; set; } = string.Empty;
    }

    public class GitHubUpdateService
    {
        private const string RepoReleasesUrl = "https://api.github.com/repos/muhbr/RRes/releases/latest";
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        public static string GetCurrentVersionString()
        {
            var ver = Assembly.GetExecutingAssembly().GetName().Version;
            return ver != null ? $"{ver.Major}.{ver.Minor}.{ver.Build}" : "1.0.0";
        }

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
                        Message = $"RRes v{currentVer} adalah build rilis terbaru. (Belum ada rilis publik baru di GitHub)."
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        IsSuccess = false,
                        CurrentVersion = currentVer,
                        Message = $"Server GitHub merespons dengan status: {response.StatusCode}."
                    };
                }

                string json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tagName = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
                string title = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";
                string htmlUrl = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? "https://github.com/muhbr/RRes" : "https://github.com/muhbr/RRes";
                string body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

                string cleanLatest = tagName.TrimStart('v', 'V');
                bool hasNewer = IsVersionNewer(cleanLatest, currentVer);

                return new UpdateCheckResult
                {
                    IsSuccess = true,
                    HasUpdate = hasNewer,
                    CurrentVersion = currentVer,
                    LatestVersion = cleanLatest,
                    ReleaseTitle = string.IsNullOrWhiteSpace(title) ? tagName : title,
                    ReleaseNotes = body,
                    ReleaseUrl = htmlUrl,
                    Message = hasNewer
                        ? $"Pembaruan baru v{cleanLatest} tersedia!"
                        : $"RRes Anda berada pada versi terbaru (v{currentVer})."
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    IsSuccess = false,
                    CurrentVersion = currentVer,
                    Message = $"Gagal memeriksa pembaruan: {ex.Message}"
                };
            }
        }

        private static bool IsVersionNewer(string latestStr, string currentStr)
        {
            if (Version.TryParse(latestStr, out var latest) && Version.TryParse(currentStr, out var current))
            {
                return latest > current;
            }
            return false;
        }
    }
}
