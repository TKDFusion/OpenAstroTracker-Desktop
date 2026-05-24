using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace OATControl
{
    public class UpdateCheckResult
    {
        public bool UpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string Changelog { get; set; }
        public string DownloadUrl { get; set; }
        public string ReleasePageUrl { get; set; }

        public static UpdateCheckResult NoUpdate => new UpdateCheckResult();
    }

    public static class UpdateChecker
    {
        private const string DesktopRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Desktop/releases/latest";
        private const string FirmwareRepoUrl = "https://api.github.com/repos/OpenAstroTech/OpenAstroTracker-Firmware/releases/latest";

        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler())
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        static UpdateChecker()
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "OATControl");
        }

        public static async Task<UpdateCheckResult> CheckForDesktopUpdateAsync()
        {
            try
            {
                var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var release = await FetchLatestReleaseAsync(DesktopRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagVersion = ParseVersionTag(release["tag_name"]?.ToString());
                if (tagVersion == null || currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                if (tagVersion > currentVersion)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        LatestVersion = release["tag_name"].ToString(),
                        CurrentVersion = $"V{currentVersion}",
                        Changelog = release["body"]?.ToString() ?? "",
                        DownloadUrl = release["assets"]?[0]?["browser_download_url"]?.ToString(),
                        ReleasePageUrl = release["html_url"]?.ToString()
                    };
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch
            {
                return UpdateCheckResult.NoUpdate;
            }
        }

        public static async Task<UpdateCheckResult> CheckForFirmwareUpdateAsync(string currentFirmwareVersion)
        {
            try
            {
                var currentVersion = ParseVersionTag(currentFirmwareVersion);
                if (currentVersion == null)
                    return UpdateCheckResult.NoUpdate;

                var release = await FetchLatestReleaseAsync(FirmwareRepoUrl);
                if (release == null)
                    return UpdateCheckResult.NoUpdate;

                var tagVersion = ParseVersionTag(release["tag_name"]?.ToString());
                if (tagVersion == null)
                    return UpdateCheckResult.NoUpdate;

                if (tagVersion > currentVersion)
                {
                    return new UpdateCheckResult
                    {
                        UpdateAvailable = true,
                        LatestVersion = release["tag_name"].ToString(),
                        CurrentVersion = currentFirmwareVersion,
                        Changelog = release["body"]?.ToString() ?? "",
                        ReleasePageUrl = release["html_url"]?.ToString()
                    };
                }

                return UpdateCheckResult.NoUpdate;
            }
            catch
            {
                return UpdateCheckResult.NoUpdate;
            }
        }

        private static async Task<JObject> FetchLatestReleaseAsync(string url)
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        private static Version ParseVersionTag(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return null;

            // Strip leading 'V' or 'v'
            var versionStr = tag.TrimStart('V', 'v');

            Version result;
            if (Version.TryParse(versionStr, out result))
                return result;

            return null;
        }
    }
}