namespace KifuwarabeGo2026.GameOasis.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>GUIで選ばれた運営命令をProtocol Mへ渡す人間ゲームマスターアダプターです。</summary>
public sealed class HumanGameMasterProtocol : IGameMasterProtocol
{
    public static readonly GameMasterEngineId EngineId = new(GameOasisOfficialNames.Root + ".gui.human-game-master");
    private readonly object _sync = new();
    private GameMasterBindingId? _bindingId;
    private GameMasterCommand? _pendingCommand;

    public bool QueueCommand(GameMasterCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        lock (_sync)
        {
            if (_bindingId is null || _pendingCommand is not null) return false;
            _pendingCommand = command;
            return true;
        }
    }

    public ValueTask<ProtocolResponse<GameMasterEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<GameMasterEngineDescriptor>.Success(new(
            EngineId, "Human Game Master", ContractVersion.V1_0, GetType().FullName!, "4.0.0",
            [GameOasisGameMasterCoordinator.PauseCommand, GameOasisGameMasterCoordinator.ResumeCommand,
                GameOasisGameMasterCoordinator.AdjudicateCommand])));

    public ValueTask<ProtocolResponse<GameMasterSessionStarted>> StartSessionAsync(GameMasterSessionStartRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_bindingId is not null) return ValueTask.FromResult(Failure<GameMasterSessionStarted>("human-game-master-already-bound", "The human game master is already bound."));
            _bindingId = request.BindingId;
            return ValueTask.FromResult(ProtocolResponse<GameMasterSessionStarted>.Success(new(request.BindingId)));
        }
    }

    public ValueTask<ProtocolResponse<GameMasterCommandSelected>> SelectCommandAsync(GameMasterCommandRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_bindingId != request.BindingId || _pendingCommand is not { } command)
                return ValueTask.FromResult(Failure<GameMasterCommandSelected>("human-game-master-command-not-queued", "No GUI game-master command is queued."));
            _pendingCommand = null;
            return ValueTask.FromResult(ProtocolResponse<GameMasterCommandSelected>.Success(new(
                request.BindingId, request.Observation.Revision, request.Observation.OperationRevision, command)));
        }
    }

    public ValueTask<ProtocolResponse<GameMasterCommandNotified>> NotifyCommandAsync(GameMasterCommandNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<GameMasterCommandNotified>.Success(new(notification.BindingId)));
    }

    public ValueTask<ProtocolResponse<GameMasterSessionEnded>> EndSessionAsync(GameMasterSessionEndRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            _bindingId = null;
            _pendingCommand = null;
            return ValueTask.FromResult(ProtocolResponse<GameMasterSessionEnded>.Success(new(request.BindingId)));
        }
    }

    private static ProtocolResponse<T> Failure<T>(string code, string message) => ProtocolResponse<T>.Failure(new(code, message));
}
