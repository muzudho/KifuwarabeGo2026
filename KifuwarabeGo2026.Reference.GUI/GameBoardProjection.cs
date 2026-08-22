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
                snapshot.Outcome));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return Failure("invalid-gui-state-document", exception.Message);
        }
    }

    private static IReadOnlyList<GuiBoardPoint> ReadPoints(JsonElement array, int boardSize, string field) =>
        array.EnumerateArray().Select(value => ReadPoint(value, boardSize, field)).ToArray();

    private static GuiBoardPoint ReadPoint(JsonElement value, int boardSize, string field)
    {
        var point = new GuiBoardPoint(value.GetProperty("x").GetInt32(), value.GetProperty("y").GetInt32());
        if (point.X < 0 || point.X >= boardSize || point.Y < 0 || point.Y >= boardSize)
            throw new InvalidOperationException($"The {field} point ({point.X},{point.Y}) is outside the board.");
        return point;
    }

    private static int ReadOptionalInt(JsonElement root, string property) =>
        root.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : 0;

    private static ProtocolResponse<GuiBoardView> Failure(string code, string message) =>
        ProtocolResponse<GuiBoardView>.Failure(new(code, message));
}

public readonly record struct GuiBoardPoint(int X, int Y);

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
    ContractDocument? Outcome);
