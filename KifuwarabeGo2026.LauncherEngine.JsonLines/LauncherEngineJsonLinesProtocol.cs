namespace KifuwarabeGo2026.LauncherEngine.JsonLines;

using System.Text.Json;
using KifuwarabeGo2026.LauncherEngine;

public static class LauncherEngineJsonLinesProtocol
{
    public const int Version = 1;
    public const string GetStateMethod = "getState";
    public const string GetInstalledVersionsMethod = "getInstalledVersions";
    public const string GetCurrentDirectoryMethod = "getCurrentDirectory";
    public const string UninstallMethod = "uninstall";
    public const string ChangeInstallationDirectoryMethod = "changeInstallationDirectory";
    public const string ChangeScreenshotDirectoryMethod = "changeScreenshotDirectory";
    public const string ChangeCloseAfterStartingGuiMethod = "changeCloseAfterStartingGui";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record LauncherEngineRequest(
    int ProtocolVersion,
    string RequestId,
    string Method,
    JsonElement? Parameters = null);

public sealed record LauncherEngineResponse(
    int ProtocolVersion,
    string RequestId,
    bool Success,
    JsonElement? Result,
    string? Error);

public sealed record LauncherProductParameters(LauncherProduct Product);
public sealed record UninstallParameters(InstalledVersion InstalledVersion);
public sealed record InstallationDirectoryParameters(string? Directory);
public sealed record ScreenshotDirectoryParameters(string Directory);
public sealed record CloseAfterStartingGuiParameters(bool Value);
