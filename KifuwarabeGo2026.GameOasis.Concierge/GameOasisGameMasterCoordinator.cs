namespace KifuwarabeGo2026.GameOasis.Concierge;

using System.Collections.Concurrent;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

/// <summary>Protocol Mのゲームマスター登録、参加、運営命令を調整します。</summary>
public sealed class GameOasisGameMasterCoordinator(
    GameOasisConcierge concierge,
    GameOasisPlayerCoordinator playerCoordinator)
{
    /// <summary>セッションを終了するGame Oasis共通命令名です。</summary>
    public const string EndSessionCommand = "end-session";

    /// <summary>ゲームを一時停止するGame Oasis共通命令名です。</summary>
    public const string PauseCommand = GameOasisConcierge.PauseOperation;

    /// <summary>一時停止中のゲームを再開するGame Oasis共通命令名です。</summary>
    public const string ResumeCommand = GameOasisConcierge.ResumeOperation;

    /// <summary>ゲームマスターの裁定結果を確定するGame Oasis共通命令名です。</summary>
    public const string AdjudicateCommand = GameOasisConcierge.AdjudicateOperation;

    private readonly GameOasisConcierge _concierge = concierge ?? throw new ArgumentNullException(nameof(concierge));
    private readonly GameOasisPlayerCoordinator _playerCoordinator = playerCoordinator ?? throw new ArgumentNullException(nameof(playerCoordinator));
    private readonly ConcurrentDictionary<GameMasterEngineId, RegisteredGameMaster> _gameMasters = new();
    private readonly ConcurrentDictionary<GameMasterBindingId, GameMasterBinding> _bindings = new();

    /// <summary>ゲームマスター実装を登録します。</summary>
    public async ValueTask<ProtocolResponse<GameMasterRegistered>> RegisterGameMasterAsync(
        IGameMasterProtocol protocol,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        cancellationToken.ThrowIfCancellationRequested();
        var described = await protocol.DescribeAsync(cancellationToken);
        if (!described.IsSuccess || described.Value is null)
            return ForwardFailure<GameMasterRegistered>(described.Error, "game-master-description-failed");
        var descriptor = described.Value;
        if (!ContractVersion.V1_0.IsCompatibleWith(descriptor.ProtocolVersion))
            return Failure<GameMasterRegistered>(
                "incompatible-protocol-m-version",
                $"Protocol M {descriptor.ProtocolVersion} is not compatible with {ContractVersion.V1_0}.");
        if (string.IsNullOrWhiteSpace(descriptor.EngineId.Value))
            return Failure<GameMasterRegistered>("empty-game-master-engine-id", "The game master engine ID must not be empty.");
        if (!_gameMasters.TryAdd(descriptor.EngineId, new RegisteredGameMaster(descriptor, protocol)))
            return Failure<GameMasterRegistered>(
                "game-master-already-registered",
                $"Game master engine '{descriptor.EngineId}' is already registered.");
        return ProtocolResponse<GameMasterRegistered>.Success(new(descriptor));
    }

    /// <summary>登録されたゲームマスターを一つのゲームへ割り当てます。</summary>
    public async ValueTask<ProtocolResponse<GameMasterBound>> BindGameMasterAsync(
        GameMasterEngineId gameMasterEngineId,
        GameOasisSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_gameMasters.TryGetValue(gameMasterEngineId, out var gameMaster))
            return Failure<GameMasterBound>("game-master-not-found", $"Game master engine '{gameMasterEngineId}' is not registered.");
        var snapshot = await _concierge.GetSnapshotAsync(sessionId, cancellationToken);
        if (!snapshot.IsSuccess || snapshot.Value is null)
            return ForwardFailure<GameMasterBound>(snapshot.Error, "game-master-initial-snapshot-failed");

        var bindingId = new GameMasterBindingId(Guid.NewGuid().ToString("N"));
        var binding = new GameMasterBinding(bindingId, gameMaster, sessionId);
        var started = await gameMaster.Protocol.StartSessionAsync(new(
            bindingId, ToObservation(snapshot.Value)), cancellationToken);
        if (!started.IsSuccess || started.Value is null)
            return ForwardFailure<GameMasterBound>(started.Error, "game-master-session-start-failed");
        if (started.Value.BindingId != bindingId)
            return Failure<GameMasterBound>("game-master-binding-id-mismatch", "The game master returned a different binding ID.");
        if (!_bindings.TryAdd(bindingId, binding))
            return Failure<GameMasterBound>("game-master-binding-id-conflict", "Could not allocate a game master binding ID.");
        return ProtocolResponse<GameMasterBound>.Success(new(bindingId, gameMasterEngineId, sessionId));
    }

    /// <summary>ゲームマスターへ運営命令を要求し、権限を持つコンシェルジュで実行します。</summary>
    public async ValueTask<ProtocolResponse<GameMasterTurnCompleted>> RequestAndExecuteCommandAsync(
        GameMasterBindingId bindingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(bindingId, out var binding))
            return BindingNotFound<GameMasterTurnCompleted>(bindingId);

        await binding.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentBinding(bindingId, binding))
                return BindingNotFound<GameMasterTurnCompleted>(bindingId);
            var snapshot = await _concierge.GetSnapshotAsync(binding.SessionId, cancellationToken);
            if (!snapshot.IsSuccess || snapshot.Value is null)
                return ForwardFailure<GameMasterTurnCompleted>(snapshot.Error, "game-master-snapshot-failed");
            var selected = await binding.GameMaster.Protocol.SelectCommandAsync(new(
                bindingId, ToObservation(snapshot.Value)), cancellationToken);
            if (!selected.IsSuccess || selected.Value is null)
                return ForwardFailure<GameMasterTurnCompleted>(selected.Error, "game-master-command-selection-failed");
            if (selected.Value.BindingId != bindingId)
                return Failure<GameMasterTurnCompleted>("game-master-binding-id-mismatch", "The game master selected a command for a different binding.");
            if (selected.Value.BasedOnRevision != snapshot.Value.Revision)
                return Failure<GameMasterTurnCompleted>(
                    "game-master-revision-mismatch",
                    $"The game master based its command on revision {selected.Value.BasedOnRevision}, expected {snapshot.Value.Revision}.");
            if (selected.Value.BasedOnOperationRevision != snapshot.Value.OperationRevision)
                return Failure<GameMasterTurnCompleted>(
                    "game-master-operation-revision-mismatch",
                    $"The game master based its command on operation revision {selected.Value.BasedOnOperationRevision}, expected {snapshot.Value.OperationRevision}.");

            IReadOnlyList<PlayerSessionEndFailure> playerEndFailures = [];
            ProtocolError? playerEndError = null;
            if (selected.Value.Command.Name == EndSessionCommand)
            {
                var playersEnded = await _playerCoordinator.EndSessionPlayersAsync(
                    binding.SessionId,
                    selected.Value.Command.Reason,
                    cancellationToken);
                if (playersEnded.IsSuccess && playersEnded.Value is not null)
                    playerEndFailures = playersEnded.Value.Failures;
                else
                    playerEndError = playersEnded.Error ?? new ProtocolError(
                        "player-session-end-broadcast-failed",
                        "The player session-end broadcast returned an invalid failure response.");
            }

            var result = await ExecuteAsync(
                binding,
                selected.Value.Command,
                selected.Value.BasedOnOperationRevision,
                cancellationToken);
            var resultObservation = snapshot.Value;
            if (result.WasAccepted && result.CommandName != EndSessionCommand)
            {
                var current = await _concierge.GetSnapshotAsync(binding.SessionId, cancellationToken);
                if (current.IsSuccess && current.Value is not null)
                    resultObservation = current.Value;
            }
            var notificationFailures = new List<GameMasterBindingId>();
            var recipients = _bindings.Values.Where(value => value.SessionId == binding.SessionId).ToArray();
            foreach (var recipient in recipients)
            {
                var notified = await recipient.GameMaster.Protocol.NotifyCommandAsync(new(
                    recipient.BindingId,
                    result,
                    ToObservation(resultObservation)), cancellationToken);
                if (!notified.IsSuccess)
                    notificationFailures.Add(recipient.BindingId);
            }

            var endFailures = new List<GameMasterBindingId>();
            IReadOnlyList<PlayerBindingId> playerNotificationFailures = [];
            ProtocolError? playerBroadcastError = null;
            if (result.WasAccepted && result.CommandName is PauseCommand or ResumeCommand or AdjudicateCommand)
            {
                var broadcast = await _playerCoordinator.BroadcastStateAsync(
                    binding.SessionId,
                    result.CommandName,
                    cancellationToken);
                if (broadcast.IsSuccess && broadcast.Value is not null)
                    playerNotificationFailures = broadcast.Value.NotificationFailures;
                else
                    playerBroadcastError = broadcast.Error ?? new ProtocolError(
                        "player-state-broadcast-failed",
                        "The player state broadcast returned an invalid failure response.");
            }
            if (result.WasAccepted && result.CommandName == EndSessionCommand)
            {
                foreach (var recipient in recipients)
                {
                    var ended = await recipient.GameMaster.Protocol.EndSessionAsync(new(
                        recipient.BindingId,
                        ToObservation(snapshot.Value),
                        selected.Value.Command.Reason), cancellationToken);
                    if (!ended.IsSuccess)
                        endFailures.Add(recipient.BindingId);
                    _bindings.TryRemove(new KeyValuePair<GameMasterBindingId, GameMasterBinding>(recipient.BindingId, recipient));
                }
            }

            return ProtocolResponse<GameMasterTurnCompleted>.Success(new(
                result,
                notificationFailures,
                endFailures,
                playerNotificationFailures,
                playerBroadcastError,
                playerEndFailures,
                playerEndError));
        }
        finally
        {
            binding.Gate.Release();
        }
    }

    /// <summary>セッションを閉じずにゲームマスターの運営参加だけを終了します。</summary>
    public async ValueTask<ProtocolResponse<GameMasterUnbound>> UnbindGameMasterAsync(
        GameMasterBindingId bindingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(bindingId, out var binding))
            return BindingNotFound<GameMasterUnbound>(bindingId);
        await binding.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentBinding(bindingId, binding))
                return BindingNotFound<GameMasterUnbound>(bindingId);
            var snapshot = await _concierge.GetSnapshotAsync(binding.SessionId, cancellationToken);
            if (!snapshot.IsSuccess || snapshot.Value is null)
                return ForwardFailure<GameMasterUnbound>(snapshot.Error, "game-master-final-snapshot-failed");
            var ended = await binding.GameMaster.Protocol.EndSessionAsync(new(
                bindingId, ToObservation(snapshot.Value), reason), cancellationToken);
            if (!ended.IsSuccess)
                return ForwardFailure<GameMasterUnbound>(ended.Error, "game-master-session-end-failed");
            if (!_bindings.TryRemove(new KeyValuePair<GameMasterBindingId, GameMasterBinding>(bindingId, binding)))
                return Failure<GameMasterUnbound>("game-master-unbind-conflict", "The game master binding changed while ending.");
            return ProtocolResponse<GameMasterUnbound>.Success(new(bindingId));
        }
        finally
        {
            binding.Gate.Release();
        }
    }

    private async ValueTask<GameMasterCommandResult> ExecuteAsync(
        GameMasterBinding binding,
        GameMasterCommand command,
        long expectedOperationRevision,
        CancellationToken cancellationToken)
    {
        if (command.Name != EndSessionCommand)
        {
            if (command.Name is not (PauseCommand or ResumeCommand or AdjudicateCommand))
                return new(binding.BindingId, binding.SessionId, command.Name, false,
                    new ProtocolError("unsupported-game-master-command", $"Command '{command.Name}' is not supported."));
            var applied = await _concierge.ApplyOperationAsync(new(
                binding.SessionId,
                command.Name,
                expectedOperationRevision,
                command.Parameters), cancellationToken);
            return applied.IsSuccess && applied.Value is not null
                ? new(binding.BindingId, binding.SessionId, command.Name, applied.Value.IsAccepted, applied.Value.Rejection)
                : new(binding.BindingId, binding.SessionId, command.Name, false, applied.Error);
        }
        var closed = await _concierge.CloseSessionAsync(binding.SessionId, cancellationToken);
        return closed.IsSuccess
            ? new(binding.BindingId, binding.SessionId, command.Name, true, null)
            : new(binding.BindingId, binding.SessionId, command.Name, false, closed.Error);
    }

    private bool IsCurrentBinding(GameMasterBindingId id, GameMasterBinding binding) =>
        _bindings.TryGetValue(id, out var current) && ReferenceEquals(current, binding);

    private static GameMasterGameObservation ToObservation(GameOasisSnapshot snapshot) => new(
        snapshot.SessionId,
        snapshot.PlaySpaceTypeId,
        snapshot.Revision,
        snapshot.OperationRevision,
        snapshot.OperationalState,
        snapshot.State,
        snapshot.IsTerminal,
        snapshot.Outcome);

    private static ProtocolResponse<T> BindingNotFound<T>(GameMasterBindingId id) =>
        Failure<T>("game-master-binding-not-found", $"Game master binding '{id}' was not found.");

    private static ProtocolResponse<T> ForwardFailure<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new ProtocolError(fallbackCode, "The connected protocol returned an invalid failure response."));

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new ProtocolError(code, message));

    private sealed record RegisteredGameMaster(GameMasterEngineDescriptor Descriptor, IGameMasterProtocol Protocol);

    private sealed class GameMasterBinding(
        GameMasterBindingId bindingId,
        RegisteredGameMaster gameMaster,
        GameOasisSessionId sessionId)
    {
        public GameMasterBindingId BindingId { get; } = bindingId;
        public RegisteredGameMaster GameMaster { get; } = gameMaster;
        public GameOasisSessionId SessionId { get; } = sessionId;
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
