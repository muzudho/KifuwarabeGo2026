namespace KifuwarabeGo2026.Gui.Application.Updates;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>GitHub Release の GUI ZIP を利用者設定とは別の更新フォルダーへ展開し、最新版を起動する。</summary>
public static class GuiReleaseUpdater
{
    private const string LatestReleaseApi = "https://api.github.com/repos/muzudho/KifuwarabeGo2026/releases/latest";
    private const string AssetPrefix = "KifuwarabeGo2026.Gui-v";
    private const string AssetSuffix = "-win-x64.zip";

    public static async Task<GuiReleaseUpdateResult> DownloadLatestAndStartAsync(
        Action<GuiReleaseUpdateProgress>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        Report(GuiReleaseUpdateStep.CheckingRelease, "GitHub Release を確認しています。", reportProgress);
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KifuwarabeGo2026.Gui-Updater");
        using var response = await client.GetAsync(LatestReleaseApi, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var release = JsonSerializer.Deserialize<Release>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false))
            ?? throw new InvalidDataException("GitHub Release information could not be read.");
        var asset = release.Assets?.FirstOrDefault(candidate =>
            candidate.Name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase) &&
            candidate.Name.EndsWith(AssetSuffix, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("The latest release does not contain a Windows x64 GUI ZIP.");
        if (string.IsNullOrWhiteSpace(release.TagName))
            throw new InvalidDataException("The latest release does not have a version tag.");

        if (IsCurrentOrNewer(release.TagName))
        {
            Report(GuiReleaseUpdateStep.Completed, $"{release.TagName} はインストール済みです。", reportProgress);
            return GuiReleaseUpdateResult.AlreadyLatest(release.TagName);
        }

        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KifuwarabeGo2026", "Updates");
        var versionDirectory = Path.Combine(root, SanitizeDirectoryName(release.TagName));
        var executablePath = Path.Combine(versionDirectory, "KifuwarabeGo2026.Gui.exe");
        if (!File.Exists(executablePath))
        {
            var stagingDirectory = versionDirectory + ".downloading";
            if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
            Directory.CreateDirectory(stagingDirectory);
            var zipPath = Path.Combine(stagingDirectory, asset.Name);
            try
            {
                Report(GuiReleaseUpdateStep.DownloadingPackage, $"{asset.Name} をダウンロードしています。", reportProgress);
                using var zipResponse = await client.GetAsync(asset.BrowserDownloadUrl, cancellationToken).ConfigureAwait(false);
                zipResponse.EnsureSuccessStatusCode();
                await using (var output = File.Create(zipPath))
                    await zipResponse.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                Report(GuiReleaseUpdateStep.ExtractingPackage, "ダウンロードしたファイルを展開しています。", reportProgress);
                ZipFile.ExtractToDirectory(zipPath, stagingDirectory, overwriteFiles: true);
                Report(GuiReleaseUpdateStep.VerifyingPackage, "更新版の実行ファイルを確認しています。", reportProgress);
                var stagedExecutablePath = Path.Combine(stagingDirectory, "KifuwarabeGo2026.Gui.exe");
                if (!File.Exists(stagedExecutablePath)) throw new InvalidDataException("The downloaded ZIP does not contain KifuwarabeGo2026.Gui.exe.");
                if (Directory.Exists(versionDirectory)) Directory.Delete(versionDirectory, recursive: true);
                Directory.Move(stagingDirectory, versionDirectory);
            }
            catch
            {
                if (Directory.Exists(stagingDirectory)) Directory.Delete(stagingDirectory, recursive: true);
                throw;
            }
        }

        Report(GuiReleaseUpdateStep.StartingUpdatedGui, $"{release.TagName} を起動しています。", reportProgress);
        var updatedProcess = Process.Start(new ProcessStartInfo { FileName = executablePath, WorkingDirectory = versionDirectory, UseShellExecute = true });
        if (updatedProcess is null) throw new InvalidOperationException("The updated GUI could not be started.");
        Report(GuiReleaseUpdateStep.Completed, $"{release.TagName} を起動しました。", reportProgress);
        return GuiReleaseUpdateResult.Started(release.TagName);
    }

    private static void Report(GuiReleaseUpdateStep step, string message, Action<GuiReleaseUpdateProgress>? reportProgress) =>
        reportProgress?.Invoke(new GuiReleaseUpdateProgress(step, message));

    private static bool IsCurrentOrNewer(string tagName)
    {
        var text = tagName.Trim().TrimStart('v', 'V');
        return Version.TryParse(text, out var latest) &&
            (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0)) >= latest;
    }

    private static string SanitizeDirectoryName(string value) =>
        string.Concat(value.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

    private sealed class Release { [JsonPropertyName("tag_name")] public string TagName { get; set; } = ""; [JsonPropertyName("assets")] public ReleaseAsset[]? Assets { get; set; } }
    private sealed class ReleaseAsset { [JsonPropertyName("name")] public string Name { get; set; } = ""; [JsonPropertyName("browser_download_url")] public string BrowserDownloadUrl { get; set; } = ""; }
}

public enum GuiReleaseUpdateStep
{
    CheckingRelease,
    DownloadingPackage,
    ExtractingPackage,
    VerifyingPackage,
    StartingUpdatedGui,
    Completed,
}

public sealed record GuiReleaseUpdateProgress(GuiReleaseUpdateStep Step, string Message);

public sealed record GuiReleaseUpdateResult(bool DidStartUpdatedGui, string Message)
{
    public static GuiReleaseUpdateResult Started(string tag) => new(true, $"LATEST RELEASE {tag} STARTED.");
    public static GuiReleaseUpdateResult AlreadyLatest(string tag) => new(false, $"ALREADY ON THE LATEST RELEASE ({tag}).");
}
