namespace KifuwarabeGo2026.LauncherEngine;

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;

internal sealed class PackageInstaller(LauncherPaths paths, HttpClient client, LauncherLog log)
{
    public async Task<string> InstallAsync(ReleasePackage package, Action<string>? progress, CancellationToken cancellationToken)
    {
        var finalDirectory = paths.VersionDirectory(package.Product, package.Version);
        var executable = Path.Combine(finalDirectory, package.Product.ExecutableName());
        if (File.Exists(executable)) { Validate(package.Product, package.Version, finalDirectory); return finalDirectory; }

        Directory.CreateDirectory(paths.Downloads);
        Directory.CreateDirectory(paths.ProductRoot(package.Product));
        var work = Path.Combine(paths.Downloads, $"{package.Product}-{package.Version}-{Guid.NewGuid():N}");
        var zip = Path.Combine(work, package.AssetName);
        var staging = finalDirectory + ".downloading";
        Directory.CreateDirectory(work);
        try
        {
            progress?.Invoke($"{package.AssetName} をダウンロードしています…");
            log.Write($"DOWNLOAD START {package.DownloadUri}");
            using (var response = await client.GetAsync(package.DownloadUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
            {
                response.EnsureSuccessStatusCode();
                await using var output = File.Create(zip);
                await response.Content.CopyToAsync(output, cancellationToken);
            }
            if (package.ChecksumUri is not null)
            {
                progress?.Invoke("SHA-256を検証しています…");
                var checksumText = await client.GetStringAsync(package.ChecksumUri, cancellationToken);
                VerifySha256(zip, checksumText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)[0]);
            }
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            Directory.CreateDirectory(staging);
            progress?.Invoke("パッケージを安全に展開しています…");
            ExtractSafely(zip, staging);
            progress?.Invoke("必須ファイルとバージョンを検証しています…");
            Validate(package.Product, package.Version, staging);
            if (Directory.Exists(finalDirectory)) Directory.Delete(finalDirectory, true);
            Directory.Move(staging, finalDirectory);
            log.Write($"INSTALL COMPLETE {package.Product} {package.Version} {finalDirectory}");
            return finalDirectory;
        }
        catch (Exception exception)
        {
            log.Write($"INSTALL FAILED {package.Product} {package.Version}: {exception}");
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            throw;
        }
        finally { if (Directory.Exists(work)) Directory.Delete(work, true); }
    }

    internal static void ExtractSafely(string zipPath, string destination)
    {
        var root = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"管理フォルダー外を指すZIPエントリーです: {entry.FullName}");
            if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    internal static void VerifySha256(string filePath, string expected)
    {
        using var stream = File.OpenRead(filePath);
        var actual = Convert.ToHexString(SHA256.HashData(stream));
        if (!string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"SHA-256が一致しません。expected={expected}; actual={actual}");
    }

    internal static void Validate(LauncherProduct product, string expectedVersion, string directory)
    {
        var stem = Path.GetFileNameWithoutExtension(product.ExecutableName());
        string[] required = [product.ExecutableName(), stem + ".deps.json", stem + ".runtimeconfig.json"];
        foreach (var name in required) if (!File.Exists(Path.Combine(directory, name))) throw new InvalidDataException($"必須ファイルがありません: {name}");
        if (product == LauncherProduct.Gui)
        {
            if (!Directory.Exists(Path.Combine(directory, "Content"))) throw new InvalidDataException("GUI Contentフォルダーがありません。");
            if (!Directory.Exists(Path.Combine(directory, "Tools", "Cgos"))) throw new InvalidDataException("GUI Tools\\Cgosフォルダーがありません。");
        }
        var actualText = FileVersionInfo.GetVersionInfo(Path.Combine(directory, product.ExecutableName())).FileVersion;
        if (!Version.TryParse(actualText, out var actual) || !Version.TryParse(expectedVersion, out var expected) ||
            actual.Major != expected.Major || actual.Minor != expected.Minor || actual.Build != expected.Build)
            throw new InvalidDataException($"ファイルバージョン {actualText} がリリース {expectedVersion} と一致しません。");
    }
}
