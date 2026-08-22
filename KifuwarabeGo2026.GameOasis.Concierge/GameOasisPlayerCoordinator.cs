namespace KifuwarabeGo2026.GameOasis.Concierge;

using System.Collections.Concurrent;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

/// <summary>Protocol Pのプレイヤー登録、参加、着手要求、状態通知を調整します。</summary>
public sealed class GameOasisPlayerCoordinator(GameOasisConcierge concierge)
{
    private readonly GameOasisConcierge _concierge = concierge ?? throw new ArgumentNullException(nameof(concierge));
    private readonly ConcurrentDictionary<PlayerEngineId, RegisteredPlayer> _players = new();
    private readonly ConcurrentDictionary<PlayerBindingId, PlayerBinding> _bindings = new();

    /// <summary>プレイヤー実装を登録します。</summary>
    public async ValueTask<ProtocolResponse<PlayerRegistered>> RegisterPlayerAsync(
        IPlayerProtocol protocol,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        cancellationToken.ThrowIfCancellationRequested();
        var described = await protocol.DescribeAsync(cancellationToken);
        if (!described.IsSuccess || described.Value is null)
            return ForwardFailure<PlayerRegistered>(described.Error, "player-description-failed");
        var descriptor = described.Value;
        if (!ContractVersion.V1_0.IsCompatibleWith(descriptor.ProtocolVersion))
            return Failure<PlayerRegistered>(
                "incompatible-protocol-p-version",
                $"Protocol P {descriptor.ProtocolVersion} is not compatible with {ContractVersion.V1_0}.");
        if (string.IsNullOrWhiteSpace(descriptor.EngineId.Value))
            return Failure<PlayerRegistered>("empty-player-engine-id", "The player engine ID must not be empty.");
        if (!_players.TryAdd(descriptor.EngineId, new RegisteredPlayer(descriptor, protocol)))
            return Failure<PlayerRegistered>("player-already-registered", $"Player engine '{descriptor.EngineId}' is already registered.");
        return ProtocolResponse<PlayerRegistered>.Success(new PlayerRegistered(descriptor));
    }

    /// <summary>登録されたプレイヤーをゲーム内の役割へ割り当てます。</summary>
    public async ValueTask<ProtocolResponse<PlayerBound>> BindPlayerAsync(
        PlayerEngineId playerEngineId,
        GameOasisSessionId sessionId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(roleId))
            return Failure<PlayerBound>("empty-player-role", "The player role ID must not be empty.");
        if (!_players.TryGetValue(playerEngineId, out var player))
            return Failure<PlayerBound>("player-not-found", $"Player engine '{playerEngineId}' is not registered.");

        var snapshot = await _concierge.GetSnapshotAsync(sessionId, cancellationToken);
        if (!snapshot.IsSuccess || snapshot.Value is null)
            return ForwardFailure<PlayerBound>(snapshot.Error, "player-initial-snapshot-failed");
        if (player.Descriptor.SupportedPlaySpaces.Count > 0 &&
            !player.Descriptor.SupportedPlaySpaces.Contains(snapshot.Value.PlaySpaceTypeId))
            return Failure<PlayerBound>(
                "player-does-not-support-play-space",
                $"Player engine '{playerEngineId}' does not support '{snapshot.Value.PlaySpaceTypeId}'.");

        var bindingId = new PlayerBindingId(Guid.NewGuid().ToString("N"));
        var binding = new PlayerBinding(bindingId, player, sessionId, roleId);
        var started = await player.Protocol.StartSessionAsync(new PlayerSessionStartRequest(
            bindingId, roleId, ToObservation(snapshot.Value)), cancellationToken);
        if (!started.IsSuccess || started.Value is null)
            return ForwardFailure<PlayerBound>(started.Error, "player-session-start-failed");
        if (started.Value.BindingId != bindingId)
            return Failure<PlayerBound>("player-binding-id-mismatch", "The player returned a different binding ID.");
        if (!_bindings.TryAdd(bindingId, binding))
            return Failure<PlayerBound>("player-binding-id-conflict", "Could not allocate a player binding ID.");

        return ProtocolResponse<PlayerBound>.Success(new PlayerBound(bindingId, playerEngineId, sessionId, roleId));
    }

    /// <summary>プレイヤーへ一手を要求し、プレイスペースへ適用して全参加プレイヤーへ通知します。</summary>
    public async ValueTask<ProtocolResponse<PlayerTurnCompleted>> RequestAndApplyActionAsync(
        PlayerBindingId bindingId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(bindingId, out var binding))
            return Failure<PlayerTurnCompleted>("player-binding-not-found", $"Player binding '{bindingId}' was not found.");

        await binding.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentBinding(bindingId, binding))
                return Failure<PlayerTurnCompleted>("player-binding-not-found", $"Player binding '{bindingId}' is no longer active.");
            var snapshot = await _concierge.GetSnapshotAsync(binding.SessionId, cancellationToken);
            if (!snapshot.IsSuccess || snapshot.Value is null)
                return ForwardFailure<PlayerTurnCompleted>(snapshot.Error, "player-snapshot-failed");
            if (snapshot.Value.IsTerminal)
                return Failure<PlayerTurnCompleted>("game-already-terminal", "A terminal game cannot request another player action.");
            if (snapshot.Value.OperationalState == GameOasisOperationalState.Paused)
                return Failure<PlayerTurnCompleted>("game-session-paused", "A paused game cannot request another player action.");

            var selected = await binding.Player.Protocol.SelectActionAsync(new PlayerActionRequest(
                binding.BindingId, binding.RoleId, ToObservation(snapshot.Value)), cancellationToken);
            if (!selected.IsSuccess || selected.Value is null)
                return ForwardFailure<PlayerTurnCompleted>(selected.Error, "player-action-selection-failed");
            if (selected.Value.BindingId != binding.BindingId)
                return Failure<PlayerTurnCompleted>("player-binding-id-mismatch", "The player selected an action for a different binding.");
            if (selected.Value.BasedOnRevision != snapshot.Value.Revision)
                return Failure<PlayerTurnCompleted>(
                    "player-revision-mismatch",
                    $"The player based its action on revision {selected.Value.BasedOnRevision}, expected {snapshot.Value.Revision}.");

            var applied = await _concierge.ApplyActionAsync(new ApplyGameOasisActionRequest(
                binding.SessionId, selected.Value.Action, selected.Value.BasedOnRevision), cancellationToken);
            if (!applied.IsSuccess || applied.Value is null)
                return ForwardFailure<PlayerTurnCompleted>(applied.Error, "player-action-apply-failed");

            var notificationFailures = new List<PlayerNotificationFailure>();
            foreach (var recipient in _bindings.Values.Where(value => value.SessionId == binding.SessionId).ToArray())
            {
                var notified = await recipient.Player.Protocol.NotifyActionAsync(new PlayerActionNotification(
                    recipient.BindingId,
                    selected.Value.Action,
                    applied.Value.IsAccepted,
                    ToObservation(applied.Value.Snapshot),
                    applied.Value.Events,
                    applied.Value.Rejection), cancellationToken);
                if (!notified.IsSuccess)
                {
                    notificationFailures.Add(new PlayerNotificationFailure(
                        recipient.BindingId,
                        notified.Error ?? new ProtocolError("player-notification-failed", "The player returned an invalid notification failure.")));
                }
            }

            return ProtocolResponse<PlayerTurnCompleted>.Success(new PlayerTurnCompleted(
                bindingId, applied.Value, notificationFailures));
        }
        finally
        {
            binding.Gate.Release();
        }
    }

    /// <summary>一つのプレイヤー割り当てを終了します。</summary>
    public async ValueTask<ProtocolResponse<PlayerUnbound>> UnbindPlayerAsync(
        PlayerBindingId bindingId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_bindings.TryGetValue(bindingId, out var binding))
            return Failure<PlayerUnbound>("player-binding-not-found", $"Player binding '{bindingId}' was not found.");

        await binding.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentBinding(bindingId, binding))
                return Failure<PlayerUnbound>("player-binding-not-found", $"Player binding '{bindingId}' is no longer active.");
            var snapshot = await _concierge.GetSnapshotAsync(binding.SessionId, cancellationToken);
            if (!snapshot.IsSuccess || snapshot.Value is null)
                return ForwardFailure<PlayerUnbound>(snapshot.Error, "player-final-snapshot-failed");
            var ended = await binding.Player.Protocol.EndSessionAsync(new PlayerSessionEndRequest(
                bindingId, ToObservation(snapshot.Value), reason), cancellationToken);
            if (!ended.IsSuccess)
                return ForwardFailure<PlayerUnbound>(ended.Error, "player-session-end-failed");
            if (!_bindings.TryRemove(new KeyValuePair<PlayerBindingId, PlayerBinding>(bindingId, binding)))
                return Failure<PlayerUnbound>("player-unbind-conflict", "The player binding changed while ending.");
            return ProtocolResponse<PlayerUnbound>.Success(new PlayerUnbound(bindingId));
        }
        finally
        {
            binding.Gate.Release();
        }
    }

    /// <summary>停止、再開、裁定などによる最新状態を同じゲームの全プレイヤーへ通知します。</summary>
    public async ValueTask<ProtocolResponse<PlayerStateBroadcast>> BroadcastStateAsync(
        GameOasisSessionId sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _concierge.GetSnapshotAsync(sessionId, cancellationToken);
        if (!snapshot.IsSuccess || snapshot.Value is null)
            return ForwardFailure<PlayerStateBroadcast>(snapshot.Error, "player-state-broadcast-snapshot-failed");

        var failures = new List<PlayerBindingId>();
        foreach (var recipient in _bindings.Values.Where(value => value.SessionId == sessionId).ToArray())
        {
            var notified = await recipient.Player.Protocol.NotifyStateAsync(new(
                recipient.BindingId,
                ToObservation(snapshot.Value),
                reason), cancellationToken);
            if (!notified.IsSuccess)
                failures.Add(recipient.BindingId);
        }
        return ProtocolResponse<PlayerStateBroadcast>.Success(new(sessionId, failures));
    }

    /// <summary>一つのゲームに割り当てられた全プレイヤーへ参加終了を通知して割り当てを破棄します。</summary>
    public async ValueTask<ProtocolResponse<PlayerSessionEndBroadcast>> EndSessionPlayersAsync(
        GameOasisSessionId sessionId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = await _concierge.GetSnapshotAsync(sessionId, cancellationToken);
        if (!snapshot.IsSuccess || snapshot.Value is null)
            return ForwardFailure<PlayerSessionEndBroadcast>(snapshot.Error, "player-session-end-snapshot-failed");

        var endedBindings = new List<PlayerBindingId>();
        var failures = new List<PlayerSessionEndFailure>();
        foreach (var binding in _bindings.Values.Where(value => value.SessionId == sessionId).ToArray())
        {
            await binding.Gate.WaitAsync(cancellationToken);
            try
            {
                if (!IsCurrentBinding(binding.BindingId, binding))
                    continue;
                var ended = await binding.Player.Protocol.EndSessionAsync(new(
                    binding.BindingId,
                    ToObservation(snapshot.Value),
                    reason), cancellationToken);
                if (ended.IsSuccess)
                    endedBindings.Add(binding.BindingId);
                else
                    failures.Add(new(
                        binding.BindingId,
                        ended.Error ?? new ProtocolError(
                            "player-session-end-failed",
                            "The player returned an invalid session-end failure response.")));
                _bindings.TryRemove(new KeyValuePair<PlayerBindingId, PlayerBinding>(binding.BindingId, binding));
            }
            finally
            {
                binding.Gate.Release();
            }
        }
        return ProtocolResponse<PlayerSessionEndBroadcast>.Success(new(sessionId, endedBindings, failures));
    }

    private bool IsCurrentBinding(PlayerBindingId bindingId, PlayerBinding binding) =>
        _bindings.TryGetValue(bindingId, out var current) && ReferenceEquals(current, binding);

    private static PlayerGameObservation ToObservation(GameOasisSnapshot snapshot) =>
        new(
            snapshot.SessionId,
            snapshot.PlaySpaceTypeId,
            snapshot.Revision,
            snapshot.OperationRevision,
            snapshot.OperationalState,
            snapshot.State,
            snapshot.IsTerminal,
            snapshot.Outcome);

    private static ProtocolResponse<T> ForwardFailure<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new ProtocolError(fallbackCode, "The connected protocol returned an invalid failure response."));

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new ProtocolError(code, message));

    private sealed record RegisteredPlayer(PlayerEngineDescriptor Descriptor, IPlayerProtocol Protocol);

    private sealed class PlayerBinding(
        PlayerBindingId bindingId,
        RegisteredPlayer player,
        GameOasisSessionId sessionId,
        string roleId)
    {
        public PlayerBindingId BindingId { get; } = bindingId;
        public RegisteredPlayer Player { get; } = player;
        public GameOasisSessionId SessionId { get; } = sessionId;
        public string RoleId { get; } = roleId;
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
