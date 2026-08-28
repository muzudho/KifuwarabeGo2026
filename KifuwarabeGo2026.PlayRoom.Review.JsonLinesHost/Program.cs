using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.JsonLines;
using System.Text.Json;

string? sessionId = null;
ContractDocument? gameRecord = null;
var moveIndex = 0;
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
        (response, sessionId, gameRecord, moveIndex, completed) = Handle(request, sessionId, gameRecord, moveIndex);
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

static (PlayRoomProcessResponse Response, string? SessionId, ContractDocument? GameRecord, int MoveIndex, bool Completed) Handle(
    PlayRoomProcessRequest request, string? sessionId, ContractDocument? gameRecord, int moveIndex)
{
    if (request.ProtocolVersion != PlayRoomJsonLinesProtocol.Version)
        throw new InvalidDataException($"未対応のPlay Roomプロトコル版です: {request.ProtocolVersion}");
    if (string.IsNullOrWhiteSpace(request.RequestId)) throw new InvalidDataException("requestId がありません。");

    object result;
    var completed = false;
    switch (request.Method)
    {
        case PlayRoomJsonLinesProtocol.OpenMethod:
        {
            if (sessionId is not null) throw new InvalidOperationException("Reviewは既に開いています。");
            var launch = Read<PlayRoomLaunchRequest>(request);
            if (launch.Version != 1 || launch.RoomTypeId != PlayRoomIds.Review || launch.GameId != GameOasisOfficialNames.Go)
                throw new InvalidDataException("この実装は囲碁Review起動要求だけを受理します。");
            gameRecord = launch.InitialPosition ?? throw new InvalidDataException("Reviewの棋譜文書がありません。");
            sessionId = Guid.NewGuid().ToString("N");
            result = new PlayRoomReady(launch.RequestId, sessionId, PlayRoomIds.Review);
            break;
        }
        case PlayRoomJsonLinesProtocol.NavigateMethod:
        {
            var navigation = Read<ReviewNavigation>(request);
            RequireSession(sessionId, navigation.SessionId);
            if (navigation.MoveIndex < 0) throw new InvalidDataException("moveIndexは0以上でなければなりません。");
            moveIndex = navigation.MoveIndex;
            result = new ReviewViewState(sessionId!, moveIndex, gameRecord!);
            break;
        }
        case PlayRoomJsonLinesProtocol.UsePositionMethod:
        {
            var selection = Read<ReviewPositionSelection>(request);
            RequireSession(sessionId, selection.SessionId);
            if (selection.MoveIndex != moveIndex) throw new InvalidDataException("表示中ではない手数の局面は採用できません。");
            result = new ReviewCompletion(sessionId!, ReviewCompletionStatus.PositionSelected, moveIndex, selection.Position);
            completed = true;
            break;
        }
        case PlayRoomJsonLinesProtocol.GoodbyeMethod:
        {
            var command = Read<PlayRoomSessionCommand>(request);
            RequireSession(sessionId, command.SessionId);
            result = new ReviewCompletion(sessionId!, ReviewCompletionStatus.Closed);
            completed = true;
            break;
        }
        default:
            throw new InvalidDataException($"未対応のメソッドです: {request.Method}");
    }

    return (new PlayRoomProcessResponse(PlayRoomJsonLinesProtocol.Version, request.RequestId, true,
        JsonSerializer.SerializeToElement(result, PlayRoomJsonLinesProtocol.JsonOptions), null), sessionId, gameRecord, moveIndex, completed);
}

static T Read<T>(PlayRoomProcessRequest request)
{
    if (request.Parameters is not { } parameters) throw new InvalidDataException("parametersがありません。");
    return parameters.Deserialize<T>(PlayRoomJsonLinesProtocol.JsonOptions)
        ?? throw new InvalidDataException("parametersを読み取れませんでした。");
}

static void RequireSession(string? expected, string actual)
{
    if (expected is null || !string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidDataException("sessionIdが一致しません。");
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
