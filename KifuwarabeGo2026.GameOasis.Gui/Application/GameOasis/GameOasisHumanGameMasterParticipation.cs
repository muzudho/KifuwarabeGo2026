namespace KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;
using KifuwarabeGo2026.Reference.Gui;
using System.Threading;
using System.Threading.Tasks;

/// <summary>GUIの人間ゲームマスターを選択中Protocol Gセッションへ参加させます。</summary>
public sealed class GameOasisHumanGameMasterParticipation(
    GameOasisGameMasterCoordinator coordinator,
    HumanGameMasterProtocol protocol,
    GameOasisGuiClient client)
{
    private readonly GameOasisGameMasterCoordinator _coordinator = coordinator;
    private readonly HumanGameMasterProtocol _protocol = protocol;
    private readonly GameOasisGuiClient _client = client;

    public GameMasterBindingId? BindingId { get; private set; }

    public async ValueTask<ProtocolResponse<GameMasterBound>> BindAsync(CancellationToken cancellationToken = default)
    {
        if (BindingId is not null) return Failure<GameMasterBound>("human-game-master-already-bound", "The GUI game master is already participating.");
        var snapshot = _client.State.ActiveSnapshot;
        if (snapshot is null) return Failure<GameMasterBound>("gui-session-not-open", "No GUI session is active.");
        var response = await _coordinator.BindGameMasterAsync(HumanGameMasterProtocol.EngineId, snapshot.SessionId, cancellationToken);
        if (response.IsSuccess && response.Value is { } bound) BindingId = bound.BindingId;
        return response;
    }

    public async ValueTask<ProtocolResponse<GameMasterTurnCompleted>> ExecuteAsync(string commandName, string reason, CancellationToken cancellationToken = default)
    {
        if (BindingId is not { } bindingId) return Failure<GameMasterTurnCompleted>("human-game-master-not-bound", "The GUI game master is not participating.");
        if (!_protocol.QueueCommand(new GameMasterCommand(commandName, reason)))
            return Failure<GameMasterTurnCompleted>("human-game-master-command-busy", "Another GUI game-master command is pending.");
        var response = await _coordinator.RequestAndExecuteCommandAsync(bindingId, cancellationToken);
        if (response.IsSuccess) await _client.RefreshAsync(cancellationToken);
        return response;
    }

    public async ValueTask<ProtocolResponse<GameMasterUnbound>> UnbindAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (BindingId is not { } bindingId) return Failure<GameMasterUnbound>("human-game-master-not-bound", "The GUI game master is not participating.");
        var response = await _coordinator.UnbindGameMasterAsync(bindingId, reason, cancellationToken);
        if (response.IsSuccess) BindingId = null;
        return response;
    }

    private static ProtocolResponse<T> Failure<T>(string code, string message) => ProtocolResponse<T>.Failure(new(code, message));
}
