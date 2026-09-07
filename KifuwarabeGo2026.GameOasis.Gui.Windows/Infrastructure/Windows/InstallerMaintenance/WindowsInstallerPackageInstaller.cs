namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

internal sealed class WindowsInstallerPackageInstaller
{
    private const string Repository = "muzudho/KifuwarabeGo2026";
    private const string ExecutableName = "KifuwarabeGo2026.Installer.exe";
    private readonly string root;
    private readonly HttpClient client;

    public WindowsInstallerPackageInstaller(string? localApplicationData = null, HttpClient? client = null)
    {
        root = Path.Combine(
            localApplicationData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KifuwarabeGo2026");
        this.client = client ?? CreateClient();
    }

    // A stable location is part of existing shortcuts and old update clients' contract.
    public string CurrentDirectory => Path.Combine(root, "Launcher", "Current");

    public string CurrentExecutable => Path.Combine(CurrentDirectory,
        File.Exists(Path.Combine(CurrentDirectory, ExecutableName)) ? ExecutableName : "KifuwarabeGo2026.Launcher.exe");

    public string? InstalledVersion => File.Exists(CurrentExecutable)
        ? NormalizeFileVersion(FileVersionInfo.GetVersionInfo(CurrentExecutable).FileVersion)
        : null;

    public async Task<InstallerInstallResult> InstallLatestAsync(
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnsureInstallerIsStopped();
        progress?.Report("GitHub Releasesで最新版を確認しています…");
        using var releaseResponse = await client.GetAsync(
            $"https://api.github.com/repos/{Repository}/releases/latest",
            cancellationToken).ConfigureAwait(false);
        releaseResponse.EnsureSuccessStatusCode();
        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var release = await JsonDocument.ParseAsync(releaseStream, cancellationToken: cancellationToken).ConfigureAwait(false);
        var tag = release.RootElement.GetProperty("tag_name").GetString() ?? throw new InvalidDataException("The latest release has no tag.");
        var version = tag.Trim().TrimStart('v', 'V');
        if (!Version.TryParse(version, out _)) throw new InvalidDataException($"Invalid release version: {tag}");
        var assetName = $"KifuwarabeGo2026.Installer-v{version}-win-x64.zip";
        var assets = release.RootElement.GetProperty("assets").EnumerateArray().ToArray();
        if (!assets.Any(asset => asset.GetProperty("name").GetString() == assetName))
            assetName = $"KifuwarabeGo2026.Launcher-v{version}-win-x64.zip";
        var hashName = assetName + ".sha256";
        var zipUrl = FindAssetUrl(assets, assetName);
        var hashUrl = FindAssetUrl(assets, hashName);

        var downloads = Path.Combine(root, "Downloads");
        Directory.CreateDirectory(downloads);
        var work = Path.Combine(downloads, $"Installer-{version}-{Guid.NewGuid():N}");
        var zipPath = Path.Combine(work, assetName);
        var extracted = Path.Combine(work, "extracted");
        Directory.CreateDirectory(work);
        try
        {
            progress?.Report($"{assetName}をダウンロードしています…");
            await DownloadAsync(zipUrl, zipPath, cancellationToken).ConfigureAwait(false);
            var checksum = await client.GetStringAsync(hashUrl, cancellationToken).ConfigureAwait(false);
            progress?.Report("SHA-256を検証しています…");
            VerifySha256(zipPath, checksum.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
            progress?.Report("安全な一時フォルダーへ展開しています…");
            ExtractSafely(zipPath, extracted);
            ValidatePackage(extracted, version);

            var launcherRoot = Path.Combine(root, "Launcher");
            var versions = Path.Combine(launcherRoot, "Versions");
            var versionDirectory = Path.Combine(versions, "v" + version);
            Directory.CreateDirectory(versions);
            if (!Directory.Exists(versionDirectory)) Directory.Move(extracted, versionDirectory);
            else ValidatePackage(versionDirectory, version);

            EnsureInstallerIsStopped();
            progress?.Report("Currentを新版へ切り替えています…");
            SwitchCurrent(versionDirectory);
            progress?.Report($"インストーラーv{version}を配置しました。");
            return new InstallerInstallResult(version, CurrentExecutable);
        }
        finally
        {
            if (Directory.Exists(work)) Directory.Delete(work, recursive: true);
        }
    }

    public void StartCurrent()
    {
        if (!File.Exists(CurrentExecutable)) throw new FileNotFoundException("The managed launcher is not installed.", CurrentExecutable);
        _ = Process.Start(new ProcessStartInfo(CurrentExecutable)
        {
            WorkingDirectory = CurrentDirectory,
            UseShellExecute = true,
        }) ?? throw new InvalidOperationException("The managed launcher could not be started.");
    }

    private void SwitchCurrent(string versionDirectory)
    {
        var launcherRoot = Path.Combine(root, "Launcher");
        var current = CurrentDirectory;
        var next = Path.Combine(launcherRoot, "Current.new");
        var old = Path.Combine(launcherRoot, "Current.old");
        var previous = Path.Combine(launcherRoot, "Previous");
        Directory.CreateDirectory(launcherRoot);
        DeleteDirectoryIfExists(next);
        DeleteDirectoryIfExists(old);
        CopyDirectory(versionDirectory, next);
        var entry = File.Exists(Path.Combine(next, ExecutableName)) ? ExecutableName : "KifuwarabeGo2026.Launcher.exe";
        ValidatePackage(next, NormalizeFileVersion(FileVersionInfo.GetVersionInfo(Path.Combine(next, entry)).FileVersion)!);

        var movedCurrent = false;
        var installedNext = false;
        try
        {
            if (Directory.Exists(current))
            {
                Directory.Move(current, old);
                movedCurrent = true;
            }
            Directory.Move(next, current);
            installedNext = true;
            DeleteDirectoryIfExists(previous);
            if (movedCurrent) Directory.Move(old, previous);
        }
        catch
        {
            if (installedNext && Directory.Exists(current)) DeleteDirectoryIfExists(current);
            if (Directory.Exists(old)) Directory.Move(old, current);
            DeleteDirectoryIfExists(next);
            throw;
        }
    }

    private static void EnsureInstallerIsStopped()
    {
        using var processes = new ProcessCollection(Process.GetProcessesByName("KifuwarabeGo2026.Installer")
            .Concat(Process.GetProcessesByName("KifuwarabeGo2026.Launcher")).ToArray());
        if (processes.Any(process => !process.HasExited))
            throw new InvalidOperationException("インストーラーが起動中です。インストーラーを閉じてから再試行してください。");
    }

    private static string FindAssetUrl(JsonElement[] assets, string expectedName)
    {
        foreach (var asset in assets)
        {
            if (string.Equals(asset.GetProperty("name").GetString(), expectedName, StringComparison.Ordinal))
                return asset.GetProperty("browser_download_url").GetString() ?? throw new InvalidDataException($"Asset URL is missing: {expectedName}");
        }
        throw new InvalidDataException($"Required release asset was not found: {expectedName}");
    }

    private async Task DownloadAsync(string url, string destination, CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var output = File.Create(destination);
        await response.Content.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
    }

    internal static void VerifySha256(string filePath, string expected)
    {
        using var stream = File.OpenRead(filePath);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The launcher package SHA-256 does not match its release checksum.");
    }

    internal static void ExtractSafely(string zipPath, string destination)
    {
        var rootPath = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(destination);
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"The ZIP entry escapes the staging directory: {entry.FullName}");
            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    internal static void ValidatePackage(string directory, string expectedVersion)
    {
        var stem = File.Exists(Path.Combine(directory, ExecutableName)) ? "KifuwarabeGo2026.Installer" : "KifuwarabeGo2026.Launcher";
        string[] required =
        [
            stem + ".exe",
            stem + ".dll",
            stem + ".deps.json",
            stem + ".runtimeconfig.json",
        ];
        foreach (var file in required)
            if (!File.Exists(Path.Combine(directory, file))) throw new InvalidDataException($"The launcher package is missing {file}.");
        var actual = NormalizeFileVersion(FileVersionInfo.GetVersionInfo(Path.Combine(directory, stem + ".exe")).FileVersion);
        if (!string.Equals(actual, expectedVersion, StringComparison.Ordinal))
            throw new InvalidDataException($"Installer file version {actual ?? "(missing)"} does not match release {expectedVersion}.");
    }

    private static string? NormalizeFileVersion(string? value) =>
        Version.TryParse(value, out var version) ? $"{version.Major}.{version.Minor}.{version.Build}" : null;

    private static HttpClient CreateClient()
    {
        var result = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        result.DefaultRequestHeaders.UserAgent.ParseAdd("KifuwarabeGo2026-Concierge/1.0");
        result.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return result;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(Path.Combine(destination, Path.GetRelativePath(source, directory)));
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
            File.Copy(file, Path.Combine(destination, Path.GetRelativePath(source, file)), overwrite: true);
    }

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }

    private sealed class ProcessCollection(Process[] processes) : IDisposable, IEnumerable<Process>
    {
        public IEnumerator<Process> GetEnumerator() => ((IEnumerable<Process>)processes).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        public void Dispose()
        {
            foreach (var process in processes) process.Dispose();
        }
    }
}

internal sealed record InstallerInstallResult(string Version, string ExecutablePath);
