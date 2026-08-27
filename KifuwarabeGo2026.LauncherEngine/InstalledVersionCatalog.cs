namespace KifuwarabeGo2026.LauncherEngine;


internal sealed class InstalledVersionCatalog
{
    private readonly LauncherPaths _paths;
    private readonly LauncherSettingsStore _settingsStore;
    private readonly IRunningProcessCatalog _runningProcesses;

    public InstalledVersionCatalog(LauncherPaths paths, LauncherSettingsStore settingsStore, IRunningProcessCatalog runningProcesses)
    {
        _paths = paths;
        _settingsStore = settingsStore;
        _runningProcesses = runningProcesses;
    }

    public IReadOnlyList<InstalledVersion> ReadAll()
    {
        var settings = _settingsStore.Load();
        var versions = new List<InstalledVersion>();
        AddProductVersions(versions, InstalledProduct.Gui, _paths.ProductRoot(LauncherProduct.Gui), settings.GuiCurrentVersion, settings.GuiPreviousVersion);
        AddProductVersions(versions, InstalledProduct.Engine, _paths.ProductRoot(LauncherProduct.Engine), settings.EngineCurrentVersion, settings.EnginePreviousVersion);
        AddProductVersions(versions, InstalledProduct.LegacyGuiUpdate, Path.Combine(_paths.InstallationRoot, "Updates"), null, null);
        return versions
            .OrderBy(version => version.Product)
            .ThenByDescending(version => ParseVersion(version.Version))
            .ThenByDescending(version => version.Version, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public void Uninstall(InstalledVersion installedVersion)
    {
        if (!installedVersion.CanUninstall)
            throw new InvalidOperationException("現在使用中またはロールバック用のバージョンはアンインストールできません。");

        var expectedRoot = installedVersion.Product switch
        {
            InstalledProduct.Gui => _paths.ProductRoot(LauncherProduct.Gui),
            InstalledProduct.Engine => _paths.ProductRoot(LauncherProduct.Engine),
            _ => Path.Combine(_paths.InstallationRoot, "Updates"),
        };
        var expectedPath = Path.GetFullPath(Path.Combine(expectedRoot, Path.GetFileName(installedVersion.DirectoryPath)));
        var actualPath = Path.GetFullPath(installedVersion.DirectoryPath);
        if (!string.Equals(expectedPath, actualPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ランチャーの管理フォルダー外は削除できません。");
        if (Directory.Exists(actualPath)) Directory.Delete(actualPath, recursive: true);
    }

    private void AddProductVersions(List<InstalledVersion> result, InstalledProduct product, string root, string? current, string? previous)
    {
        if (!Directory.Exists(root)) return;
        foreach (var directory in Directory.EnumerateDirectories(root))
        {
            var name = Path.GetFileName(directory);
            if (name.EndsWith(".downloading", StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(new InstalledVersion(
                product,
                name,
                Path.GetFullPath(directory),
                CalculateDirectorySize(directory),
                VersionEquals(name, current),
                VersionEquals(name, previous),
                _runningProcesses.IsProcessRunningFrom(directory)));
        }
    }

    private static long CalculateDirectorySize(string directory)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
            };
            return Directory.EnumerateFiles(directory, "*", options)
                .Sum(path =>
                {
                    try { return new FileInfo(path).Length; }
                    catch (IOException) { return 0L; }
                    catch (UnauthorizedAccessException) { return 0L; }
                });
        }
        catch (IOException) { return 0; }
        catch (UnauthorizedAccessException) { return 0; }
    }

    private static bool VersionEquals(string directoryName, string? configuredVersion) =>
        !string.IsNullOrWhiteSpace(configuredVersion) &&
        string.Equals(directoryName.TrimStart('v', 'V'), configuredVersion.Trim().TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase);

    private static Version ParseVersion(string value) =>
        Version.TryParse(value.TrimStart('v', 'V'), out var version) ? version : new Version(0, 0);

}
