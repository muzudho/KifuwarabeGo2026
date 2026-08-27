namespace KifuwarabeGo2026.Launcher;

public sealed class InProcessLauncherEngine : ILauncherEngine
{
    private readonly IPlatformServices _platform;
    private readonly HttpClient _httpClient;
    private readonly LauncherSettingsStore _settings;
    private readonly LauncherLog _log;
    private LauncherPaths _paths;
    private InstalledVersionCatalog _catalog = null!;
    private ProductLauncher _launcher = null!;
    private LauncherUpdateService _updates = null!;

    public InProcessLauncherEngine(IPlatformServices platform, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(httpClient);

        _platform = platform;
        _httpClient = httpClient;
        var settingsPaths = new LauncherPaths(platform.LocalApplicationData);
        _settings = new LauncherSettingsStore(settingsPaths);
        _log = new LauncherLog(settingsPaths);
        _paths = new LauncherPaths(platform.LocalApplicationData, _settings.Load().InstallationDirectory);
        Directory.CreateDirectory(_paths.InstallationRoot);
        RebuildInstallationServices();
    }

    public LauncherState GetState()
    {
        var settings = _settings.Load();
        return new LauncherState(
            _paths.InstallationRoot,
            settings.GuiCurrentVersion,
            settings.EngineCurrentVersion,
            ApplicationFamilySettings.ScreenshotSaveDirectory,
            ApplicationFamilySettings.FilePath,
            ApplicationFamilySettings.CloseLauncherAfterStartingGui);
    }

    public Task<string> UpdateAsync(
        LauncherProduct product,
        IProgress<LauncherProgress>? progress = null,
        CancellationToken cancellationToken = default) =>
        _updates.UpdateAsync(product, message => progress?.Report(new LauncherProgress(message)), cancellationToken);

    public IReadOnlyList<InstalledVersion> GetInstalledVersions() => _catalog.ReadAll();

    public void Uninstall(InstalledVersion installedVersion) => _catalog.Uninstall(installedVersion);

    public LaunchResult StartGui() => _launcher.StartGui();

    public string? GetCurrentDirectory(LauncherProduct product) => _launcher.CurrentDirectory(product);

    public LauncherState ChangeInstallationDirectory(string? directory)
    {
        var settings = _settings.Load();
        settings.InstallationDirectory = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFullPath(directory);
        var nextPaths = new LauncherPaths(_platform.LocalApplicationData, settings.InstallationDirectory);
        Directory.CreateDirectory(nextPaths.InstallationRoot);
        _settings.Save(settings);
        _paths = nextPaths;
        RebuildInstallationServices();
        return GetState();
    }

    public LauncherState ChangeScreenshotDirectory(string directory)
    {
        ApplicationFamilySettings.SaveScreenshotDirectory(directory);
        return GetState();
    }

    public LauncherState ChangeCloseAfterStartingGui(bool value)
    {
        ApplicationFamilySettings.SaveCloseLauncherAfterStartingGui(value);
        return GetState();
    }

    private void RebuildInstallationServices()
    {
        _catalog = new InstalledVersionCatalog(_paths, _settings, _platform);
        _launcher = new ProductLauncher(_paths, _settings, _log, _platform);
        _updates = new LauncherUpdateService(
            new GitHubReleaseClient(_httpClient),
            new PackageInstaller(_paths, _httpClient, _log),
            _settings,
            _log);
    }
}

