using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.PlaySpace.JsonLines;
using KifuwarabeGo2026.Reference.PlaySpace.Go;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;
using System.Text.Json;

var implementation = ReadOption(args, "--play-space") ?? "go";
IPlaySpaceProtocol playSpace = implementation switch
{
    "go" => new GoPlaySpaceProtocol(),
    "ponnuki" => new PonnukiPlaySpaceProtocol(),
    _ => throw new ArgumentException($"Unknown play-space implementation: {implementation}"),
};
var singleSession = args.Contains("--single-session", StringComparer.Ordinal);
var exitAfterDescribe = args.Contains("--exit-after-describe", StringComparer.Ordinal);
var activeSessions = new HashSet<PlaySpaceSessionId>();
string? line;
while ((line = Console.ReadLine()) is not null)
{
    PlaySpaceProcessResponse response;
    var goodbye = false;
    try
    {
        var request = JsonSerializer.Deserialize<PlaySpaceProcessRequest>(line, PlaySpaceJsonLinesProtocol.JsonOptions)
            ?? throw new JsonException("要求がnullです。");
        (response, goodbye) = await HandleAsync(request, playSpace, singleSession, activeSessions);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        response = new PlaySpaceProcessResponse(PlaySpaceJsonLinesProtocol.Version, TryReadRequestId(line), false, null,
            new PlaySpaceProcessError("invalid-request", exception.Message));
    }
    Console.WriteLine(JsonSerializer.Serialize(response, PlaySpaceJsonLinesProtocol.JsonOptions));
    if (goodbye || (exitAfterDescribe && response.Success)) break;
}

static async ValueTask<(PlaySpaceProcessResponse Response, bool Goodbye)> HandleAsync(
    PlaySpaceProcessRequest request, IPlaySpaceProtocol playSpace, bool singleSession, HashSet<PlaySpaceSessionId> activeSessions)
{
    if (request.ProtocolVersion != PlaySpaceJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応のPlaySpaceプロトコル版です: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestIdがありません。");

    object result;
    var goodbye = false;
    switch (request.Method)
    {
        case PlaySpaceJsonLinesProtocol.DescribeMethod:
            result = Wire(await playSpace.DescribeAsync());
            break;
        case PlaySpaceJsonLinesProtocol.GetConfigurationSchemaMethod:
            result = Wire(await playSpace.GetConfigurationSchemaAsync());
            break;
        case PlaySpaceJsonLinesProtocol.ValidateConfigurationMethod:
            result = Wire(await playSpace.ValidateConfigurationAsync(Read<ValidatePlaySpaceConfigurationRequest>(request)));
            break;
        case PlaySpaceJsonLinesProtocol.CreateSessionMethod:
        {
            if (singleSession && activeSessions.Count != 0)
            {
                result = new PlaySpaceProtocolResult<PlaySpaceSessionCreated>(null,
                    new ProtocolError("single-session-busy", "This host supports one active session."));
                break;
            }
            var response = await playSpace.CreateSessionAsync(Read<CreatePlaySpaceSessionRequest>(request));
            if (response.IsSuccess && response.Value is { } created) activeSessions.Add(created.SessionId);
            result = Wire(response);
            break;
        }
        case PlaySpaceJsonLinesProtocol.GetSnapshotMethod:
            result = Wire(await playSpace.GetSnapshotAsync(Read<GetPlaySpaceSnapshotRequest>(request)));
            break;
        case PlaySpaceJsonLinesProtocol.ApplyActionMethod:
            result = Wire(await playSpace.ApplyActionAsync(Read<ApplyPlaySpaceActionRequest>(request)));
            break;
        case PlaySpaceJsonLinesProtocol.CloseSessionMethod:
        {
            var command = Read<ClosePlaySpaceSessionRequest>(request);
            var response = await playSpace.CloseSessionAsync(command);
            if (response.IsSuccess) activeSessions.Remove(command.SessionId);
            result = Wire(response);
            break;
        }
        case PlaySpaceJsonLinesProtocol.GoodbyeMethod:
            result = new PlaySpaceProtocolResult<bool>(true, null);
            goodbye = true;
            break;
        default:
            throw new InvalidDataException($"未対応のメソッドです: {request.Method}");
    }
    return (new PlaySpaceProcessResponse(PlaySpaceJsonLinesProtocol.Version, request.RequestId, true,
        JsonSerializer.SerializeToElement(result, PlaySpaceJsonLinesProtocol.JsonOptions), null), goodbye);
}

static PlaySpaceProtocolResult<T> Wire<T>(ProtocolResponse<T> response) => new(response.Value, response.Error);

static T Read<T>(PlaySpaceProcessRequest request)
{
    if (request.Parameters is not { } parameters) throw new InvalidDataException("parametersがありません。");
    return parameters.Deserialize<T>(PlaySpaceJsonLinesProtocol.JsonOptions)
        ?? throw new InvalidDataException("parametersを読み取れませんでした。");
}

static string? ReadOption(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

static string TryReadRequestId(string json)
{
    try
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty("requestId", out var value) ? value.GetString() ?? "" : "";
    }
    catch (JsonException) { return ""; }
}
