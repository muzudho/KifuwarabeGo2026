namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

public static class GoPlayRoomHostExitCodes
{
    public const int Success = 0;
    public const int InvalidArguments = 2;
    public const int RequestReadFailed = 3;
    public const int RequestRejected = 4;
    public const int ContractSmokeFailed = 5;
}

public sealed record GoPlayRoomHostStartupResult(
    bool IsReady,
    int ExitCode,
    string Code,
    string Message,
    GoPlayRoomLaunchPlan? Plan);

/// <summary>保存済み起動要求を囲碁Play Room専用Hostの開始Planへ変換します。</summary>
public static class GoPlayRoomHostStartup
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static GoPlayRoomHostStartupResult Load(string[] args)
    {
        if ((args.Length != 2 &&
             (args.Length != 3 ||
              (args[2] != "--contract-smoke" && args[2] != "--contract-smoke-fail-after-ready"))) ||
            !string.Equals(args[0], "--launch-request", StringComparison.Ordinal))
            return Fail(
                GoPlayRoomHostExitCodes.InvalidArguments,
                "invalid-arguments",
                "Usage: KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows --launch-request <json-file> [--contract-smoke|--contract-smoke-fail-after-ready]");

        PlayRoomLaunchRequest request;
        try
        {
            var path = Path.GetFullPath(args[1]);
            var json = File.ReadAllText(path);
            request = JsonSerializer.Deserialize<PlayRoomLaunchRequest>(json, JsonOptions)
                ?? throw new JsonException("The launch request is null.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return Fail(GoPlayRoomHostExitCodes.RequestReadFailed, "launch-request-read-failed", exception.Message);
        }

        if (!string.Equals(request.RoomTypeId, PlayRoomIds.Match, StringComparison.Ordinal))
            return Fail(
                GoPlayRoomHostExitCodes.RequestRejected,
                "unsupported-host-room-type",
                "The first Go Play Room Windows Host slice accepts Local Match requests only.");

        if (!GoPlayRoomLaunchInterpreter.TryCreate(request, out var plan, out var errorCode, out var message) || plan is null)
            return Fail(GoPlayRoomHostExitCodes.RequestRejected, errorCode, message);

        return new GoPlayRoomHostStartupResult(
            true,
            GoPlayRoomHostExitCodes.Success,
            "ready",
            "The saved Local Match request is valid and ready for the Go Play Room window loop.",
            plan);
    }

    private static GoPlayRoomHostStartupResult Fail(int exitCode, string code, string message) =>
        new(false, exitCode, code, message, null);
}
