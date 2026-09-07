namespace KifuwarabeGo2026.InstallerEngine;

internal sealed class InstallerUpdateService(GitHubReleaseClient releases, PackageInstaller installer, InstallerSettingsStore settings, InstallerLog log)
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    public async Task<string> UpdateAsync(InstallerProduct product, Action<string>? progress, CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken)) throw new InvalidOperationException("別の更新が進行中です。");
        try
        {
            progress?.Invoke("GitHub Releaseを確認しています…");
            var package = await releases.GetLatestAsync(product, cancellationToken);
            await installer.InstallAsync(package, progress, cancellationToken);
            var document = settings.Load();
            if (!string.Equals(document.Current(product), package.Version, StringComparison.OrdinalIgnoreCase))
            {
                document.Promote(product, package.Version);
                settings.Save(document);
                log.Write($"CURRENT CHANGED {product} v{package.Version}");
            }
            progress?.Invoke($"{product.DisplayName()} v{package.Version} の更新が完了しました。");
            return package.Version;
        }
        finally { _gate.Release(); }
    }
}
