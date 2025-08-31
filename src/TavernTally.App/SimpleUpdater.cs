using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;

namespace TavernTally.App
{
    public static class SimpleUpdater
    {
        private class ReleaseInfo { public string? latest { get; set; } public string? msi { get; set; } public string? sha256 { get; set; } }

        public static async Task<string> CheckAndDownloadAsync(string updateJsonUrl)
        {
            using var http = new HttpClient();
            var json = await http.GetStringAsync(updateJsonUrl).ConfigureAwait(false);
            var info = JsonSerializer.Deserialize<ReleaseInfo>(json);
            if (info?.msi == null) return "No update info.";
            string tempMsi = Path.Combine(Path.GetTempPath(), "bgs-overlay-update.msi");

            var data = await http.GetByteArrayAsync(info.msi).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(info.sha256))
            {
                using var sha = SHA256.Create();
                var hash = BitConverter.ToString(sha.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
                if (!hash.Equals(info.sha256.ToLowerInvariant()))
                    return "Update failed integrity check.";
            }
            File.WriteAllBytes(tempMsi, data);
            // Launch MSI (passive upgrade). The running app should close first; caller handles that UX.
            Process.Start(new ProcessStartInfo { FileName = "msiexec", Arguments = $"/i \"{tempMsi}\" /passive", UseShellExecute = true });
            return "Launching installer…";
        }
    }
}
