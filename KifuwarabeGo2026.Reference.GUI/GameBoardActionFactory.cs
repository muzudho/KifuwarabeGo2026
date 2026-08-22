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
        var schema = board.PlaySpaceTypeId.Value switch
        {
            GameOasisOfficialNames.Go => GameOasisOfficialNames.Go + ".action.v1",
            GameOasisOfficialNames.Ponnuki => GameOasisOfficialNames.Ponnuki + ".action.v1",
            _ => null,
        };
        if (schema is null)
            return Failure("unsupported-gui-action", $"Play-space '{board.PlaySpaceTypeId}' has no official GUI action factory.");
        var content = JsonSerializer.Serialize(new
        {
            version = 1,
            type = "play",
            player = board.NextToPlay,
            x,
            y,
        }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return ProtocolResponse<ContractDocument>.Success(new("application/json", schema, content));
    }

    private static ProtocolResponse<ContractDocument> Failure(string code, string message) =>
        ProtocolResponse<ContractDocument>.Failure(new(code, message));
}
