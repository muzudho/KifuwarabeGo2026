namespace KifuwarabeGo2026.InstallerEngine;

public sealed class InProcessInstallerEngine : IInstallerEngine
{
    private readonly IInstallerEnginePlatform _platform;
    private readonly HttpClient _httpClient;
    private readonly InstallerSettingsStore _settings;
    private readonly SharedGuiSettingsStore _sharedGuiSettings;
    private readonly InstallerLog _log;
    private InstallerPaths _paths;
    private InstalledVersionCatalog _catalog = null!;
    private ProductLauncher _launcher = null!;
    private InstallerUpdateService _updates = null!;

    public InProcessInstallerEngine(IInstallerEnginePlatform platform, HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(platform);
        ArgumentNullException.ThrowIfNull(httpClient);

        _platform = platform;
        _httpClient = httpClient;
        var settingsPaths = new InstallerPaths(platform.LocalApplicationData);
        _settings = new InstallerSettingsStore(settingsPaths);
        _sharedGuiSettings = new SharedGuiSettingsStore(platform.LocalApplicationData, platform.MyPictures);
        MigrateLegacyInstallerSettings();
        _log = new InstallerLog(settingsPaths);
        _paths = new InstallerPaths(platform.LocalApplicationData, _settings.Load().InstallationDirectory);
        Directory.CreateDirectory(_paths.InstallationRoot);
        RebuildInstallationServices();
    }

    public InstallerState GetState()
    {
        var settings = _settings.Load();
        return new InstallerState(
            _paths.InstallationRoot,
            settings.GuiCurrentVersion,
            settings.EngineCurrentVersion,
            _sharedGuiSettings.ScreenshotSaveDirectory,
            _sharedGuiSettings.FilePath,
            settings.CloseLauncherAfterStartingGui ?? true);
    }

    public async Task<InstallerOperationResult<string>> UpdateAsync(
        InstallerProduct product,
        IProgress<InstallerProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var operation = InstallerOperationLease.Acquire(_paths);
            _paths = new InstallerPaths(_platform.LocalApplicationData, _settings.Load().InstallationDirectory);
            RebuildInstallationServices();
            var version = await _updates.UpdateAsync(
                product,
                message => progress?.Report(new InstallerProgress(message)),
                cancellationToken);
            return InstallerOperationResult<string>.Success(version, $"{product.DisplayName()} v{version} UPDATE COMPLETE");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return InstallerOperationResult<string>.Canceled();
        }
        catch (Exception exception)
        {
            return InstallerOperationResult<string>.Failure(exception.Message);
        }
    }

    public IReadOnlyList<InstalledVersion> GetInstalledVersions() => _catalog.ReadAll();

    public InstallerOperationResult Uninstall(InstalledVersion installedVersion)
    {
        try
        {
            using var operation = InstallerOperationLease.Acquire(_paths);
            _paths = new InstallerPaths(_platform.LocalApplicationData, _settings.Load().InstallationDirectory);
            RebuildInstallationServices();
            _catalog.Uninstall(installedVersion);
            return InstallerOperationResult.Success();
        }
        catch (Exception exception)
        {
            return InstallerOperationResult.Failure(exception.Message);
        }
    }

    public InstallerOperationResult<InstallerLaunchDetails> StartGui()
    {
        try
        {
            using var operation = InstallerOperationLease.Acquire(_paths);
            // Another management process may have changed the installation root since this window opened.
            _paths = new InstallerPaths(_platform.LocalApplicationData, _settings.Load().InstallationDirectory);
            RebuildInstallationServices();
            var result = _launcher.StartGui();
            return result.Success
                ? InstallerOperationResult<InstallerLaunchDetails>.Success(new InstallerLaunchDetails(result.UsedPrevious), result.Message)
                : InstallerOperationResult<InstallerLaunchDetails>.Failure(result.Message);
        }
        catch (Exception exception) { return InstallerOperationResult<InstallerLaunchDetails>.Failure(exception.Message); }
    }

    public string? GetCurrentDirectory(InstallerProduct product) => _launcher.CurrentDirectory(product);

    public InstallerOperationResult<InstallerState> ChangeInstallationDirectory(string? directory)
    {
        try
        {
            using var operation = InstallerOperationLease.Acquire(_paths);
            var settings = _settings.Load();
            settings.InstallationDirectory = string.IsNullOrWhiteSpace(directory) ? null : Path.GetFullPath(directory);
            var nextPaths = new InstallerPaths(_platform.LocalApplicationData, settings.InstallationDirectory);
            Directory.CreateDirectory(nextPaths.InstallationRoot);
            _settings.Save(settings);
            _paths = nextPaths;
            RebuildInstallationServices();
            return InstallerOperationResult<InstallerState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return InstallerOperationResult<InstallerState>.Failure(exception.Message);
        }
    }

    public InstallerOperationResult<InstallerState> ChangeScreenshotDirectory(string directory)
    {
        try
        {
            _sharedGuiSettings.SaveScreenshotDirectory(directory);
            return InstallerOperationResult<InstallerState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return InstallerOperationResult<InstallerState>.Failure(exception.Message);
        }
    }

    public InstallerOperationResult<InstallerState> ChangeCloseAfterStartingGui(bool value)
    {
        try
        {
            using var operation = InstallerOperationLease.Acquire(_paths);
            var settings = _settings.Load();
            settings.CloseLauncherAfterStartingGui = value;
            _settings.Save(settings);
            return InstallerOperationResult<InstallerState>.Success(GetState());
        }
        catch (Exception exception)
        {
            return InstallerOperationResult<InstallerState>.Failure(exception.Message);
        }
    }

    private void MigrateLegacyInstallerSettings()
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
        _updates = new InstallerUpdateService(
            new GitHubReleaseClient(_httpClient),
            new PackageInstaller(_paths, _httpClient, _log),
            _settings,
            _log);
    }
}
