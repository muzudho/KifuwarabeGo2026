namespace KifuwarabeGo2026.PlaySpace.JsonLines;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using System.Text.Json;

public sealed record PlaySpaceJsonLinesHostOptions(bool SupportsMultipleSessions = true, bool ExitAfterDescribe = false);

/// <summary>外部のProtocol S実装を標準入出力ホストとして公開するSDKです。</summary>
public static class PlaySpaceJsonLinesHost
{
    public static async Task RunAsync(IPlaySpaceProtocol playSpace, PlaySpaceJsonLinesHostOptions? options = null,
        TextReader? input = null, TextWriter? output = null, TextWriter? error = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(playSpace);
        options ??= new(); input ??= Console.In; output ??= Console.Out; error ??= Console.Error;
        var activeSessions = new HashSet<PlaySpaceSessionId>();
        string? line;
        while ((line = await input.ReadLineAsync(cancellationToken)) is not null)
        {
            PlaySpaceProcessResponse response;
            var goodbye = false;
            try
            {
                var request = JsonSerializer.Deserialize<PlaySpaceProcessRequest>(line, PlaySpaceJsonLinesProtocol.JsonOptions)
                    ?? throw new JsonException("要求がnullです。");
                (response, goodbye) = await HandleAsync(request, playSpace, options, activeSessions, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                await error.WriteLineAsync(exception.ToString());
                response = new(PlaySpaceJsonLinesProtocol.Version, TryReadRequestId(line), false, null,
                    new("invalid-request", exception.Message));
            }
            await output.WriteLineAsync(JsonSerializer.Serialize(response, PlaySpaceJsonLinesProtocol.JsonOptions));
            await output.FlushAsync(cancellationToken);
            if (goodbye || (options.ExitAfterDescribe && response.Success)) break;
        }
    }

    private static async ValueTask<(PlaySpaceProcessResponse, bool)> HandleAsync(PlaySpaceProcessRequest request,
        IPlaySpaceProtocol playSpace, PlaySpaceJsonLinesHostOptions options, HashSet<PlaySpaceSessionId> activeSessions,
        CancellationToken cancellationToken)
    {
        if (request.ProtocolVersion != PlaySpaceJsonLinesProtocol.Version)
            throw new InvalidDataException($"未対応のPlaySpaceプロトコル版です: {request.ProtocolVersion}");
        if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestIdがありません。");
        object result;
        var goodbye = false;
        switch (request.Method)
        {
            case PlaySpaceJsonLinesProtocol.DescribeMethod:
                result = Wire(await playSpace.DescribeAsync(cancellationToken)); break;
            case PlaySpaceJsonLinesProtocol.GetConfigurationSchemaMethod:
                result = Wire(await playSpace.GetConfigurationSchemaAsync(cancellationToken)); break;
            case PlaySpaceJsonLinesProtocol.ValidateConfigurationMethod:
                result = Wire(await playSpace.ValidateConfigurationAsync(Read<ValidatePlaySpaceConfigurationRequest>(request), cancellationToken)); break;
            case PlaySpaceJsonLinesProtocol.CreateSessionMethod:
            {
                if (!options.SupportsMultipleSessions && activeSessions.Count != 0)
                {
                    result = new PlaySpaceProtocolResult<PlaySpaceSessionCreated>(null,
                        new ProtocolError("single-session-busy", "This host supports one active session.")); break;
                }
                var response = await playSpace.CreateSessionAsync(Read<CreatePlaySpaceSessionRequest>(request), cancellationToken);
                if (response.IsSuccess && response.Value is { } created) activeSessions.Add(created.SessionId);
                result = Wire(response); break;
            }
            case PlaySpaceJsonLinesProtocol.GetSnapshotMethod:
                result = Wire(await playSpace.GetSnapshotAsync(Read<GetPlaySpaceSnapshotRequest>(request), cancellationToken)); break;
            case PlaySpaceJsonLinesProtocol.ApplyActionMethod:
                result = Wire(await playSpace.ApplyActionAsync(Read<ApplyPlaySpaceActionRequest>(request), cancellationToken)); break;
            case PlaySpaceJsonLinesProtocol.CloseSessionMethod:
            {
                var command = Read<ClosePlaySpaceSessionRequest>(request);
                var response = await playSpace.CloseSessionAsync(command, cancellationToken);
                if (response.IsSuccess) activeSessions.Remove(command.SessionId);
                result = Wire(response); break;
            }
            case PlaySpaceJsonLinesProtocol.GoodbyeMethod:
                result = new PlaySpaceProtocolResult<bool>(true, null); goodbye = true; break;
            default: throw new InvalidDataException($"未対応のメソッドです: {request.Method}");
        }
        return (new(PlaySpaceJsonLinesProtocol.Version, request.RequestId, true,
            JsonSerializer.SerializeToElement(result, PlaySpaceJsonLinesProtocol.JsonOptions), null), goodbye);
    }

    private static PlaySpaceProtocolResult<T> Wire<T>(ProtocolResponse<T> response) => new(response.Value, response.Error);
    private static T Read<T>(PlaySpaceProcessRequest request) =>
        request.Parameters is { } parameters
            ? parameters.Deserialize<T>(PlaySpaceJsonLinesProtocol.JsonOptions) ?? throw new InvalidDataException("parametersを読み取れませんでした。")
            : throw new InvalidDataException("parametersがありません。");
    private static string TryReadRequestId(string json)
    {
        try { using var document = JsonDocument.Parse(json); return document.RootElement.TryGetProperty("requestId", out var value) ? value.GetString() ?? "" : ""; }
        catch (JsonException) { return ""; }
    }
}
