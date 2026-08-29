using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoomGui.JsonLines;
using System.Text.Json;

string? sessionId = null;
ContractDocument? state = null;
long revision = -1;
var completed = false;
var exitAfterOpen = args.Contains("--exit-after-open", StringComparer.Ordinal);
string? line;
while (!completed && (line = Console.ReadLine()) is not null)
{
    PlayRoomProcessResponse response;
    try
    {
        var request = JsonSerializer.Deserialize<PlayRoomProcessRequest>(line, PlayRoomJsonLinesProtocol.JsonOptions)
            ?? throw new JsonException("要求が null です。");
        (response, sessionId, state, revision, completed) = Handle(request, sessionId, state, revision);
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        response = new PlayRoomProcessResponse(PlayRoomJsonLinesProtocol.Version, TryReadRequestId(line), false, null,
            new PlayRoomProcessError("invalid-request", exception.Message));
    }
    Console.WriteLine(JsonSerializer.Serialize(response, PlayRoomJsonLinesProtocol.JsonOptions));
    if (exitAfterOpen && sessionId is not null) break;
}

static (PlayRoomProcessResponse Response, string? SessionId, ContractDocument? State, long Revision, bool Completed) Handle(
    PlayRoomProcessRequest request, string? sessionId, ContractDocument? state, long revision)
{
    if (request.ProtocolVersion != PlayRoomJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応の Play Room プロトコル版です: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestId がありません。");

    object result;
    var completed = false;
    switch (request.Method)
    {
        case PlayRoomJsonLinesProtocol.OpenMethod:
        {
            if (sessionId is not null) throw new InvalidOperationException("Match は既に開いています。");
            var launch = Read<PlayRoomLaunchRequest>(request);
            if (launch.Version != 1 || launch.RoomTypeId != PlayRoomIds.Match)
                throw new InvalidDataException("この実装は version 1 の Match 起動要求だけを受理します。");
            sessionId = Guid.NewGuid().ToString("N");
            result = new PlayRoomReady(launch.RequestId, sessionId, PlayRoomIds.Match);
            break;
        }
        case PlayRoomJsonLinesProtocol.UpdateStateMethod:
        {
            var update = Read<MatchStateUpdate>(request);
            RequireSession(sessionId, update.SessionId);
            if (update.Revision <= revision) throw new InvalidDataException("revision は単調増加でなければなりません。");
            revision = update.Revision;
            state = update.State;
            result = new MatchViewState(sessionId!, revision, state);
            break;
        }
        case PlayRoomJsonLinesProtocol.SubmitActionMethod:
        {
            var action = Read<MatchActionRequest>(request);
            RequireSession(sessionId, action.SessionId);
            if (string.IsNullOrWhiteSpace(action.ActionId) || string.IsNullOrWhiteSpace(action.PlayerRoleId))
                throw new InvalidDataException("actionId と playerRoleId が必要です。");
            if (action.Kind == MatchActionKind.PlayPoint && (action.X is null || action.Y is null))
                throw new InvalidDataException("PlayPoint には X と Y が必要です。");
            result = new MatchActionAccepted(sessionId!, action.ActionId);
            break;
        }
        case PlayRoomJsonLinesProtocol.CompleteMethod:
        {
            var command = Read<MatchCompletionCommand>(request);
            RequireSession(sessionId, command.SessionId);
            result = new MatchCompletion(sessionId!, MatchCompletionStatus.Finished,
                command.FinalState, command.WinnerRoleId, command.Reason);
            completed = true;
            break;
        }
        case PlayRoomJsonLinesProtocol.GoodbyeMethod:
        {
            var command = Read<PlayRoomSessionCommand>(request);
            RequireSession(sessionId, command.SessionId);
            result = new MatchCompletion(sessionId!, MatchCompletionStatus.Closed, state);
            completed = true;
            break;
        }
        default:
            throw new InvalidDataException($"未対応のメソッドです: {request.Method}");
    }

    return (new PlayRoomProcessResponse(PlayRoomJsonLinesProtocol.Version, request.RequestId, true,
        JsonSerializer.SerializeToElement(result, PlayRoomJsonLinesProtocol.JsonOptions), null), sessionId, state, revision, completed);
}

static T Read<T>(PlayRoomProcessRequest request)
{
    if (request.Parameters is not { } parameters) throw new InvalidDataException("parameters がありません。");
    return parameters.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions)
        ?? throw new InvalidDataException("parameters を読み取れませんでした。");
}

static void RequireSession(string? expected, string actual)
{
    if (expected is null || !string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidDataException("sessionId が一致しません。");
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
