namespace KifuwarabeGo2026.InstallerEngine;

public interface IInstallerEngine
{
    InstallerState GetState();

    Task<InstallerOperationResult<string>> UpdateAsync(
        InstallerProduct product,
        IProgress<InstallerProgress>? progress = null,
        CancellationToken cancellationToken = default);

    IReadOnlyList<InstalledVersion> GetInstalledVersions();
    InstallerOperationResult Uninstall(InstalledVersion installedVersion);
    InstallerOperationResult<InstallerLaunchDetails> StartGui();
    string? GetCurrentDirectory(InstallerProduct product);
    InstallerOperationResult<InstallerState> ChangeInstallationDirectory(string? directory);
    InstallerOperationResult<InstallerState> ChangeScreenshotDirectory(string directory);
    InstallerOperationResult<InstallerState> ChangeCloseAfterStartingGui(bool value);
}

public interface IInstallerEngineCommunicationStatus
{
    string? CommunicationWarning { get; }
}

public sealed record InstallerState(
    string InstallationRoot,
    string? GuiCurrentVersion,
    string? EngineCurrentVersion,
    string ScreenshotSaveDirectory,
    string SharedSettingsFile,
    bool CloseAfterStartingGui);

public sealed record InstallerProgress(string Message);

public enum InstallerOperationStatus
{
    Success,
    Failure,
    Canceled,
}

public record InstallerOperationResult(InstallerOperationStatus Status, string Message)
{
    public bool IsSuccess => Status == InstallerOperationStatus.Success;
    public bool IsCanceled => Status == InstallerOperationStatus.Canceled;

    public static InstallerOperationResult Success(string message = "") => new(InstallerOperationStatus.Success, message);
    public static InstallerOperationResult Failure(string message) => new(InstallerOperationStatus.Failure, message);
    public static InstallerOperationResult Canceled(string message = "処理をキャンセルしました。") => new(InstallerOperationStatus.Canceled, message);
}

public sealed record InstallerOperationResult<T>(InstallerOperationStatus Status, string Message, T? Value)
    : InstallerOperationResult(Status, Message)
{
    public static InstallerOperationResult<T> Success(T value, string message = "") => new(InstallerOperationStatus.Success, message, value);
    public new static InstallerOperationResult<T> Failure(string message) => new(InstallerOperationStatus.Failure, message, default);
    public new static InstallerOperationResult<T> Canceled(string message = "処理をキャンセルしました。") => new(InstallerOperationStatus.Canceled, message, default);
}

public sealed record InstallerLaunchDetails(bool UsedPrevious);
