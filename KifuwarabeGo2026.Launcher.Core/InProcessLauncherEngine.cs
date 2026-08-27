namespace KifuwarabeGo2026.Launcher;

public sealed class InProcessLauncherEngine : ILauncherEngine
{
    private readonly ILauncherEnginePlatform _platform;
    private readonly HttpClient _httpClient;
    private readonly LauncherSettingsStore _settings;
    private readonly SharedGuiSettingsStore _sharedGuiSettings;
    private readonly LauncherLog _log;
    private LauncherPaths _paths;
    private InstalledVersionCatalog _catalog = null!;
    private ProductLauncher _launcher = null!;
    private LauncherUpdateService _updates = null!;

    public InProcessLauncherEngine(ILauncherEnginePlatform platform, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(httpClient);

        _platform = platform;
        _httpClient = httpClient;
        var settingsPaths = new LauncherPaths(platform.LocalApplicationData);
        _settings = new LauncherSettingsStore(settingsPaths);
        _sharedGuiSettings = new SharedGuiSettingsStore(platform.LocalApplicationData, platform.MyPictures);
        MigrateLegacyLauncherSettings();
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
            _sharedGuiSettings.ScreenshotSaveDirectory,
            _sharedGuiSettings.FilePath,
            settings.CloseLauncherAfterStartingGui ?? true);
    }

    public async Task<LauncherOperationResult<string>> UpdateAsync(
        LauncherProduct product,
        IProgress<LauncherProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var version = await _updates.UpdateAsync(
                product,
                message => progress?.Report(new LauncherProgress(message)),
                cancellationToken);
            return LauncherOperationResult<string>.Success(version, $"{product.DisplayName()} v{version} UPDATE COMPLETE");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return LauncherOperationResult<string>.Canceled();
        }
        catch (Exception exception)
        {
            return LauncherOperationResult<string>.Failure(exception.Message);
        }
    }

    public IReadOnlyList<InstalledVersion> GetInstalledVersions() => _catalog.ReadAll();

    public LauncherOperationResult Uninstall(InstalledVersion installedVersion)
    {
        try
        {
            _catalog.Uninstall(installedVersion);
            return LauncherOperationResult.Success();
        }
        catch (Exception exception)
        {
            return LauncherOperationResult.Failure(exception.Message);
        }
    }

    public LauncherOperationResult<LauncherLaunchDetails> StartGui()
    {
        var result = _launcher.StartGui();
        return result.Success
            ? LauncherOperationResult<LauncherLaunchDetails>.Success(new LauncherLaunchDetails(result.UsedPrevious), result.Message)
            : LauncherOperationResult<LauncherLaunchDetails>.Failure(result.Message);
    }

    public string? GetCurrentDirectory(LauncherProduct product) => _launcher.CurrentDirectory(product);

    public LauncherOperationResult<LauncherState> ChangeInstallationDirectory(string? directory)
    {
        try
        {
            var settings = _settings.Load();
            settings.InstallationDirectory = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFullPath(directory);
            var nextPaths = new LauncherPaths(_platform.LocalApplicationData, settings.InstallationDirectory);
            Directory.CreateDirectory(nextPaths.InstallationRoot);
            _settings.Save(settings);
            _paths = nextPaths;
            RebuildInstallationServices();
            return LauncherOperationResult<LauncherState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return LauncherOperationResult<LauncherState>.Failure(exception.Message);
        }
    }

    public LauncherOperationResult<LauncherState> ChangeScreenshotDirectory(string directory)
    {
        try
        {
            _sharedGuiSettings.SaveScreenshotDirectory(directory);
            return LauncherOperationResult<LauncherState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return LauncherOperationResult<LauncherState>.Failure(exception.Message);
        }
    }

    public LauncherOperationResult<LauncherState> ChangeCloseAfterStartingGui(bool value)
    {
        try
        {
            var settings = _settings.Load();
            settings.CloseLauncherAfterStartingGui = value;
            _settings.Save(settings);
            return LauncherOperationResult<LauncherState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return LauncherOperationResult<LauncherState>.Failure(exception.Message);
        }
    }

    private void MigrateLegacyLauncherSettings()
    {
        var settings = _settings.Load();
        if (settings.CloseLauncherAfterStartingGui is not null) return;
        var legacyValue = _sharedGuiSettings.ReadLegacyCloseLauncherAfterStartingGui();
        if (legacyValue is null) return;
        settings.CloseLauncherAfterStartingGui = legacyValue;
        _settings.Save(settings);
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
