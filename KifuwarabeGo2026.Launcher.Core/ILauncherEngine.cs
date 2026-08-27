namespace KifuwarabeGo2026.Launcher;

public interface ILauncherEngine
{
    LauncherState GetState();

    Task<LauncherOperationResult<string>> UpdateAsync(
        LauncherProduct product,
        IProgress<LauncherProgress>? progress = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<InstalledVersion> GetInstalledVersions();
    LauncherOperationResult Uninstall(InstalledVersion installedVersion);
    LauncherOperationResult<LauncherLaunchDetails> StartGui();
    string? GetCurrentDirectory(LauncherProduct product);
    LauncherOperationResult<LauncherState> ChangeInstallationDirectory(string? directory);
    LauncherOperationResult<LauncherState> ChangeScreenshotDirectory(string directory);
    LauncherOperationResult<LauncherState> ChangeCloseAfterStartingGui(bool value);
}

public sealed record LauncherState(
    string InstallationRoot,
    string? GuiCurrentVersion,
    string? EngineCurrentVersion,
    string ScreenshotSaveDirectory,
    string SharedSettingsFile,
    bool CloseAfterStartingGui);

public sealed record LauncherProgress(string Message);

public enum LauncherOperationStatus
{
    Success,
    Failure,
    Canceled,
}

public record LauncherOperationResult(LauncherOperationStatus Status, string Message)
{
    public bool IsSuccess => Status == LauncherOperationStatus.Success;
    public bool IsCanceled => Status == LauncherOperationStatus.Canceled;

    public static LauncherOperationResult Success(string message = "") => new(LauncherOperationStatus.Success, message);
    public static LauncherOperationResult Failure(string message) => new(LauncherOperationStatus.Failure, message);
    public static LauncherOperationResult Canceled(string message = "処理をキャンセルしました。") => new(LauncherOperationStatus.Canceled, message);
}

public sealed record LauncherOperationResult<T>(LauncherOperationStatus Status, string Message, T? Value)
    : LauncherOperationResult(Status, Message)
{
    public static LauncherOperationResult<T> Success(T value, string message = "") => new(LauncherOperationStatus.Success, message, value);
    public new static LauncherOperationResult<T> Failure(string message) => new(LauncherOperationStatus.Failure, message, default);
    public new static LauncherOperationResult<T> Canceled(string message = "処理をキャンセルしました。") => new(LauncherOperationStatus.Canceled, message, default);
}

public sealed record LauncherLaunchDetails(bool UsedPrevious);
