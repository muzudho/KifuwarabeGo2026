namespace KifuwarabeGo2026.LauncherEngine.JsonLines;

using System.Text.Json;

public static class LauncherEngineJsonLinesProtocol
{
    public const int Version = 1;
    public const string GetStateMethod = "getState";
    public const string GetInstalledVersionsMethod = "getInstalledVersions";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record LauncherEngineRequest(int ProtocolVersion, string RequestId, string Method);

public sealed record LauncherEngineResponse(
    int ProtocolVersion,
    string RequestId,
    bool Success,
    JsonElement? Result,
    string? Error);
