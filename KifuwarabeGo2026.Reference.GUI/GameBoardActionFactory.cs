namespace KifuwarabeGo2026.Reference.GUI;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>共通盤面上のGUI入力を、公式プレイスペースの意味的な行動文書へ変換します。</summary>
public static class GameBoardActionFactory
{
    public static ProtocolResponse<ContractDocument> CreatePlay(GuiBoardView board, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsTerminal)
            return Failure("gui-game-terminal", "A move cannot be created after the game has ended.");
        if (x < 0 || x >= board.BoardSize || y < 0 || y >= board.BoardSize)
            return Failure("gui-point-outside-board", $"Point ({x},{y}) is outside the board.");
        var point = new GuiBoardPoint(x, y);
        if (board.Black.Contains(point) || board.White.Contains(point))
            return Failure("gui-point-occupied", $"Point ({x},{y}) is already occupied.");
        return Create(board, "play", x, y);
    }

    public static ProtocolResponse<ContractDocument> CreatePass(GuiBoardView board) =>
        CreateGoOnly(board, "pass");

    public static ProtocolResponse<ContractDocument> CreateResign(GuiBoardView board) =>
        CreateGoOnly(board, "resign");

    private static ProtocolResponse<ContractDocument> CreateGoOnly(GuiBoardView board, string type)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.PlaySpaceTypeId.Value != GameOasisOfficialNames.Go)
            return Failure("unsupported-gui-action", $"Play-space '{board.PlaySpaceTypeId}' does not support GUI action '{type}'.");
        if (board.IsTerminal)
            return Failure("gui-game-terminal", $"Action '{type}' cannot be created after the game has ended.");
        return Create(board, type, null, null);
    }

    private static ProtocolResponse<ContractDocument> Create(GuiBoardView board, string type, int? x, int? y)
    {
        var schema = board.PlaySpaceTypeId.Value switch
        {
            GameOasisOfficialNames.Go => GameOasisOfficialNames.Go + ".action.v1",
            GameOasisOfficialNames.Ponnuki => GameOasisOfficialNames.Ponnuki + ".action.v1",
            _ => null,
        };
        if (schema is null)
            return Failure("unsupported-gui-action", $"Play-space '{board.PlaySpaceTypeId}' has no official GUI action factory.");
        var content = JsonSerializer.Serialize(new { version = 1, type, player = board.NextToPlay, x, y },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
            });
        return ProtocolResponse<ContractDocument>.Success(new("application/json", schema, content));
    }

    private static ProtocolResponse<ContractDocument> Failure(string code, string message) =>
        ProtocolResponse<ContractDocument>.Failure(new(code, message));
}
