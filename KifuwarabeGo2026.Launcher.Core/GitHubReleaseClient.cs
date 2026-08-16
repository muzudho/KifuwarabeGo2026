namespace KifuwarabeGo2026.Launcher;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

internal sealed class GitHubReleaseClient(HttpClient client)
{
    private const string LatestApi = "https://api.github.com/repos/muzudho/KifuwarabeGo2026/releases/latest";

    public async Task<ReleasePackage> GetLatestAsync(LauncherProduct product, CancellationToken cancellationToken)
    {
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KifuwarabeGo2026.Launcher");
        var release = await client.GetFromJsonAsync<Release>(LatestApi, cancellationToken) ?? throw new InvalidDataException("GitHub Releaseを読み取れませんでした。");
        var version = release.TagName.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(version, out _)) throw new InvalidDataException("リリースタグがバージョンではありません。");
        var expected = product.AssetName(version);
        var asset = release.Assets.SingleOrDefault(item => string.Equals(item.Name, expected, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException($"最新リリースに {expected} がありません。");
        var checksum = release.Assets.SingleOrDefault(item => string.Equals(item.Name, expected + ".sha256", StringComparison.OrdinalIgnoreCase));
        return new ReleasePackage(product, version, asset.Name, new Uri(asset.DownloadUrl),
            string.IsNullOrWhiteSpace(checksum?.DownloadUrl) ? null : new Uri(checksum.DownloadUrl));
    }

    private sealed class Release
    {
        [JsonPropertyName("tag_name")] public string TagName { get; set; } = "";
        [JsonPropertyName("assets")] public ReleaseAsset[] Assets { get; set; } = [];
    }
    private sealed class ReleaseAsset
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("browser_download_url")] public string DownloadUrl { get; set; } = "";
    }
}

internal sealed record ReleasePackage(LauncherProduct Product, string Version, string AssetName, Uri DownloadUri, Uri? ChecksumUri);
