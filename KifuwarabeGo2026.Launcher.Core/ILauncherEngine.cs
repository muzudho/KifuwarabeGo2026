namespace KifuwarabeGo2026.Launcher;

public interface ILauncherEngine
{
    LauncherState GetState();

    Task<string> UpdateAsync(
        LauncherProduct product,
        IProgress<LauncherProgress>? progress = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<InstalledVersion> GetInstalledVersions();
    void Uninstall(InstalledVersion installedVersion);
    LaunchResult StartGui();
    string? GetCurrentDirectory(LauncherProduct product);
    LauncherState ChangeInstallationDirectory(string? directory);
    LauncherState ChangeScreenshotDirectory(string directory);
    LauncherState ChangeCloseAfterStartingGui(bool value);
}

public sealed record LauncherState(
    string InstallationRoot,
    string? GuiCurrentVersion,
    string? EngineCurrentVersion,
    string ScreenshotSaveDirectory,
    string SharedSettingsFile,
    bool CloseAfterStartingGui);

public sealed record LauncherProgress(string Message);

