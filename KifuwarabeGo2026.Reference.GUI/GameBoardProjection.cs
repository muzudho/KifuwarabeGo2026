namespace KifuwarabeGo2026.Reference.GUI;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>ゲーム固有state文書を、参照GUIが描画できる共通盤面へ投影します。</summary>
public static class GameBoardProjection
{
    private static readonly HashSet<string> SupportedSchemas =
    [
        GameOasisOfficialNames.Go + ".state.v1",
        GameOasisOfficialNames.Ponnuki + ".state.v1",
    ];

    public static ProtocolResponse<GuiBoardView> Project(GuiGameSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.State.MediaType != "application/json" || !SupportedSchemas.Contains(snapshot.State.SchemaId))
            return Failure("unsupported-gui-state", $"State schema '{snapshot.State.SchemaId}' cannot be projected as a Go board.");
        try
        {
            using var json = JsonDocument.Parse(snapshot.State.Content);
            var root = json.RootElement;
            var boardSize = root.GetProperty("boardSize").GetInt32();
            if (boardSize is not (9 or 13 or 19)) return Failure("invalid-gui-board-size", $"Board size '{boardSize}' is not supported.");
            var black = ReadPoints(root.GetProperty("black"), boardSize, "black");
            var white = ReadPoints(root.GetProperty("white"), boardSize, "white");
            var occupied = black.Concat(white).ToArray();
            if (occupied.Distinct().Count() != occupied.Length)
                return Failure("duplicate-gui-board-point", "The state places more than one stone on the same point.");
            var nextToPlay = root.GetProperty("nextToPlay").GetString();
            if (nextToPlay is not ("black" or "white")) return Failure("invalid-gui-next-player", $"Next player '{nextToPlay}' is invalid.");
            GuiBoardPoint? koPoint = root.TryGetProperty("koPoint", out var ko) && ko.ValueKind != JsonValueKind.Null
                ? ReadPoint(ko, boardSize, "ko")
                : null;
            var setupBlack = root.TryGetProperty("setupBlack", out var setupBlackElement)
                ? ReadPoints(setupBlackElement, boardSize, "setupBlack")
                : [];
            var setupWhite = root.TryGetProperty("setupWhite", out var setupWhiteElement)
                ? ReadPoints(setupWhiteElement, boardSize, "setupWhite")
                : [];
            var moveHistory = root.TryGetProperty("moveHistory", out var moveHistoryElement)
                ? ReadMoves(moveHistoryElement, boardSize)
                : [];
            return ProtocolResponse<GuiBoardView>.Success(new(
                snapshot.SessionId,
                snapshot.PlaySpaceTypeId,
                snapshot.Revision,
                boardSize,
                black,
                white,
                nextToPlay,
                ReadOptionalInt(root, "blackCaptures"),
                ReadOptionalInt(root, "whiteCaptures"),
                koPoint,
                snapshot.IsTerminal,
                snapshot.Outcome,
                setupBlack,
                setupWhite,
                moveHistory,
                ReadOptionalTime(root, "mainTimeMilliseconds"),
                ReadOptionalTime(root, "blackTimeLeftMilliseconds"),
                ReadOptionalTime(root, "whiteTimeLeftMilliseconds")));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return Failure("invalid-gui-state-document", exception.Message);
        }
    }

    private static IReadOnlyList<GuiBoardPoint> ReadPoints(JsonElement array, int boardSize, string field) =>
        array.EnumerateArray().Select(value => ReadPoint(value, boardSize, field)).ToArray();

    private static IReadOnlyList<GuiBoardMove> ReadMoves(JsonElement array, int boardSize) =>
        array.EnumerateArray().Select(value =>
        {
            var player = value.GetProperty("player").GetString();
            var type = value.GetProperty("type").GetString();
            if (player is not ("black" or "white"))
                throw new InvalidOperationException($"Move player '{player}' is invalid.");
            if (type is not ("play" or "pass"))
                throw new InvalidOperationException($"Move type '{type}' is invalid.");
            GuiBoardPoint? point = type == "play"
                ? ReadPoint(value, boardSize, "move")
                : null;
            return new GuiBoardMove(player, type, point, ReadOptionalTime(value, "timeLeftMilliseconds"));
        }).ToArray();

    private static GuiBoardPoint ReadPoint(JsonElement value, int boardSize, string field)
    {
        var point = new GuiBoardPoint(value.GetProperty("x").GetInt32(), value.GetProperty("y").GetInt32());
        if (point.X < 0 || point.X >= boardSize || point.Y < 0 || point.Y >= boardSize)
            throw new InvalidOperationException($"The {field} point ({point.X},{point.Y}) is outside the board.");
        return point;
    }

    private static int ReadOptionalInt(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0;

    private static TimeSpan? ReadOptionalTime(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind == JsonValueKind.Null) return null;
        var milliseconds = value.GetInt64();
        if (milliseconds < 0) throw new InvalidOperationException($"The {property} value cannot be negative.");
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static ProtocolResponse<GuiBoardView> Failure(string code, string message) =>
        ProtocolResponse<GuiBoardView>.Failure(new(code, message));
}

public readonly record struct GuiBoardPoint(int X, int Y);

public sealed record GuiBoardMove(string Player, string Type, GuiBoardPoint? Point, TimeSpan? TimeLeftAfterMove);

public sealed record GuiBoardView(
    GameOasisSessionId SessionId,
    PlaySpaceTypeId PlaySpaceTypeId,
    long Revision,
    int BoardSize,
    IReadOnlyList<GuiBoardPoint> Black,
    IReadOnlyList<GuiBoardPoint> White,
    string NextToPlay,
    int BlackCaptures,
    int WhiteCaptures,
    GuiBoardPoint? KoPoint,
    bool IsTerminal,
    ContractDocument? Outcome,
    IReadOnlyList<GuiBoardPoint> SetupBlack,
    IReadOnlyList<GuiBoardPoint> SetupWhite,
    IReadOnlyList<GuiBoardMove> MoveHistory,
    TimeSpan? MainTime,
    TimeSpan? BlackTimeLeft,
    TimeSpan? WhiteTimeLeft);
