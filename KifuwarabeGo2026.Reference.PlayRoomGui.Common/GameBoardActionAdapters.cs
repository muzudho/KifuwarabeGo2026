namespace KifuwarabeGo2026.Reference.PlayRoomGui.Common;

using System.Text.Json;
using System.Text.Json.Serialization;
using KifuwarabeGo2026.GameOasis.Contracts.Common;

public interface IGameBoardActionAdapter
{
    PlaySpaceTypeId PlaySpaceTypeId { get; }
    IReadOnlySet<string> Capabilities { get; }
    ProtocolResponse<ContractDocument> Create(GuiBoardView board, string actionType, int? x, int? y);
}

public sealed class GameBoardActionAdapters
{
    private readonly IReadOnlyDictionary<PlaySpaceTypeId, IGameBoardActionAdapter> _adapters;

    public GameBoardActionAdapters(IEnumerable<IGameBoardActionAdapter> adapters)
    {
        ArgumentNullException.ThrowIfNull(adapters);
        _adapters = adapters.ToDictionary(adapter => adapter.PlaySpaceTypeId);
    }

    public static GameBoardActionAdapters Official { get; } = new(
    [
        new JsonGameBoardActionAdapter(
            new(GameOasisOfficialNames.Go),
            GameOasisOfficialNames.Go + ".action.v1",
            new HashSet<string>(StringComparer.Ordinal)
            {
                GameOasisCapabilityIds.ActionPlayPoint,
                GameOasisCapabilityIds.ActionPass,
                GameOasisCapabilityIds.ActionResign,
            }),
        new JsonGameBoardActionAdapter(
            new(GameOasisOfficialNames.Ponnuki),
            GameOasisOfficialNames.Ponnuki + ".action.v1",
            new HashSet<string>(StringComparer.Ordinal)
            {
                GameOasisCapabilityIds.ActionPlayPoint,
            }),
    ]);

    public bool Supports(PlaySpaceTypeId playSpaceTypeId, string capabilityId) =>
        _adapters.TryGetValue(playSpaceTypeId, out var adapter) && adapter.Capabilities.Contains(capabilityId);

    public ProtocolResponse<ContractDocument> CreatePlay(GuiBoardView board, int x, int y)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsTerminal)
            return Failure("gui-game-terminal", "A move cannot be created after the game has ended.");
        if (x < 0 || x >= board.BoardSize || y < 0 || y >= board.BoardSize)
            return Failure("gui-point-outside-board", $"Point ({x},{y}) is outside the board.");
        var point = new GuiBoardPoint(x, y);
        if (board.Black.Contains(point) || board.White.Contains(point))
            return Failure("gui-point-occupied", $"Point ({x},{y}) is already occupied.");
        return Create(board, GameOasisCapabilityIds.ActionPlayPoint, "play", x, y);
    }

    public ProtocolResponse<ContractDocument> CreatePass(GuiBoardView board) =>
        CreateTerminalChecked(board, GameOasisCapabilityIds.ActionPass, "pass");

    public ProtocolResponse<ContractDocument> CreateResign(GuiBoardView board) =>
        CreateTerminalChecked(board, GameOasisCapabilityIds.ActionResign, "resign");

    private ProtocolResponse<ContractDocument> CreateTerminalChecked(GuiBoardView board, string capabilityId, string actionType)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.IsTerminal)
            return Failure("gui-game-terminal", $"Action '{actionType}' cannot be created after the game has ended.");
        return Create(board, capabilityId, actionType, null, null);
    }

    private ProtocolResponse<ContractDocument> Create(
        GuiBoardView board, string capabilityId, string actionType, int? x, int? y)
    {
        if (!_adapters.TryGetValue(board.PlaySpaceTypeId, out var adapter) || !adapter.Capabilities.Contains(capabilityId))
            return Failure("unsupported-gui-action", $"Play-space '{board.PlaySpaceTypeId}' has no GUI adapter for '{actionType}'.");
        return adapter.Create(board, actionType, x, y);
    }

    private static ProtocolResponse<ContractDocument> Failure(string code, string message) =>
        ProtocolResponse<ContractDocument>.Failure(new(code, message));
}

public sealed class JsonGameBoardActionAdapter(
    PlaySpaceTypeId playSpaceTypeId,
    string actionSchemaId,
    IReadOnlySet<string> capabilities) : IGameBoardActionAdapter
{
    public PlaySpaceTypeId PlaySpaceTypeId { get; } = playSpaceTypeId;
    public IReadOnlySet<string> Capabilities { get; } = capabilities;

    public ProtocolResponse<ContractDocument> Create(GuiBoardView board, string actionType, int? x, int? y)
    {
        var content = JsonSerializer.Serialize(new { version = 1, type = actionType, player = board.NextToPlay, x, y },
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            });
        return ProtocolResponse<ContractDocument>.Success(new("application/json", actionSchemaId, content));
    }
}
