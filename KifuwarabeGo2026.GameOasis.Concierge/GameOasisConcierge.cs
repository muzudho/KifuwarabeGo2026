namespace KifuwarabeGo2026.GameOasis.Concierge;

using System.Collections.Concurrent;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>
/// 登録されたプレイスペースをProtocol Sだけで操作するゲームコンシェルジュです。
/// </summary>
public sealed class GameOasisConcierge
{
    /// <summary>ゲームを一時停止する共通運営操作名です。</summary>
    public const string PauseOperation = "pause";

    /// <summary>一時停止中のゲームを再開する共通運営操作名です。</summary>
    public const string ResumeOperation = "resume";

    /// <summary>ゲームマスターの裁定結果を確定する共通運営操作名です。</summary>
    public const string AdjudicateOperation = "adjudicate";

    private readonly ConcurrentDictionary<PlaySpaceTypeId, RegisteredPlaySpace> _playSpaces = new();
    private readonly ConcurrentDictionary<GameOasisSessionId, ManagedSession> _sessions = new();

    /// <summary>プレイスペース実装を登録します。</summary>
    public async ValueTask<ProtocolResponse<PlaySpaceRegistered>> RegisterPlaySpaceAsync(
        IPlaySpaceProtocol protocol,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        cancellationToken.ThrowIfCancellationRequested();
        var described = await protocol.DescribeAsync(cancellationToken);
        if (!described.IsSuccess || described.Value is null)
            return ForwardFailure<PlaySpaceRegistered>(described.Error, "play-space-description-failed");

        var descriptor = described.Value;
        if (!ContractVersion.V1_0.IsCompatibleWith(descriptor.ProtocolVersion))
            return Failure<PlaySpaceRegistered>(
                "incompatible-protocol-s-version",
                $"Protocol S {descriptor.ProtocolVersion} is not compatible with {ContractVersion.V1_0}.");
        if (string.IsNullOrWhiteSpace(descriptor.TypeId.Value))
            return Failure<PlaySpaceRegistered>("empty-play-space-type-id", "The play-space type ID must not be empty.");
        if (!_playSpaces.TryAdd(descriptor.TypeId, new RegisteredPlaySpace(descriptor, protocol)))
            return Failure<PlaySpaceRegistered>(
                "play-space-already-registered",
                $"Play-space type '{descriptor.TypeId}' is already registered.");

        return ProtocolResponse<PlaySpaceRegistered>.Success(new PlaySpaceRegistered(descriptor));
    }

    /// <summary>現在登録されているプレイスペースを取得します。</summary>
    public IReadOnlyList<PlaySpaceDescriptor> GetPlaySpaces() =>
        _playSpaces.Values
            .Select(value => value.Descriptor)
            .OrderBy(value => value.TypeId.Value, StringComparer.Ordinal)
            .ToArray();

    /// <summary>登録されたプレイスペースのゲーム設定スキーマを取得します。</summary>
    public async ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(
        PlaySpaceTypeId typeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_playSpaces.TryGetValue(typeId, out var playSpace))
            return Failure<ContractDocument>("play-space-not-found", $"Play-space type '{typeId}' is not registered.");

        await playSpace.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!_playSpaces.TryGetValue(typeId, out var current) || !ReferenceEquals(current, playSpace))
                return Failure<ContractDocument>("play-space-not-found", $"Play-space type '{typeId}' is no longer registered.");
            return await playSpace.Protocol.GetConfigurationSchemaAsync(cancellationToken);
        }
        finally
        {
            playSpace.Gate.Release();
        }
    }

    /// <summary>利用中でないプレイスペース実装を登録解除します。</summary>
    public async ValueTask<ProtocolResponse<PlaySpaceUnregistered>> UnregisterPlaySpaceAsync(
        PlaySpaceTypeId typeId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_playSpaces.TryGetValue(typeId, out var playSpace))
            return Failure<PlaySpaceUnregistered>("play-space-not-found", $"Play-space type '{typeId}' is not registered.");

        await playSpace.Gate.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.Values.Any(session => ReferenceEquals(session.PlaySpace, playSpace)))
                return Failure<PlaySpaceUnregistered>(
                    "play-space-in-use",
                    $"Play-space type '{typeId}' has active sessions.");
            return _playSpaces.TryRemove(new KeyValuePair<PlaySpaceTypeId, RegisteredPlaySpace>(typeId, playSpace))
                ? ProtocolResponse<PlaySpaceUnregistered>.Success(new PlaySpaceUnregistered(typeId))
                : Failure<PlaySpaceUnregistered>("play-space-registration-changed", $"Play-space type '{typeId}' changed while unregistering.");
        }
        finally
        {
            playSpace.Gate.Release();
        }
    }

    /// <summary>選択したプレイスペースと設定からGame Oasisセッションを開始します。</summary>
    public async ValueTask<ProtocolResponse<GameOasisSessionOpened>> OpenSessionAsync(
        PlaySpaceTypeId typeId,
        ContractDocument configuration,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_playSpaces.TryGetValue(typeId, out var playSpace))
            return Failure<GameOasisSessionOpened>("play-space-not-found", $"Play-space type '{typeId}' is not registered.");

        await playSpace.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!_playSpaces.TryGetValue(typeId, out var current) || !ReferenceEquals(current, playSpace))
                return Failure<GameOasisSessionOpened>("play-space-not-found", $"Play-space type '{typeId}' is no longer registered.");

            var validation = await playSpace.Protocol.ValidateConfigurationAsync(
                new ValidatePlaySpaceConfigurationRequest(configuration), cancellationToken);
            if (!validation.IsSuccess || validation.Value is null)
                return ForwardFailure<GameOasisSessionOpened>(validation.Error, "configuration-validation-failed");
            if (!validation.Value.IsValid)
            {
                var issue = validation.Value.Issues.FirstOrDefault()
                    ?? new ProtocolError("invalid-configuration", "The play-space configuration is invalid.");
                return ProtocolResponse<GameOasisSessionOpened>.Failure(issue);
            }

            var created = await playSpace.Protocol.CreateSessionAsync(
                new CreatePlaySpaceSessionRequest(configuration), cancellationToken);
            if (!created.IsSuccess || created.Value is null)
                return ForwardFailure<GameOasisSessionOpened>(created.Error, "play-space-session-create-failed");

            var sessionId = new GameOasisSessionId(Guid.NewGuid().ToString("N"));
            var managed = new ManagedSession(playSpace, created.Value.SessionId);
            if (!_sessions.TryAdd(sessionId, managed))
            {
                await playSpace.Protocol.CloseSessionAsync(
                    new ClosePlaySpaceSessionRequest(created.Value.SessionId), CancellationToken.None);
                return Failure<GameOasisSessionOpened>("session-id-conflict", "Could not allocate a Game Oasis session ID.");
            }

            return ProtocolResponse<GameOasisSessionOpened>.Success(new GameOasisSessionOpened(
                sessionId, playSpace.Descriptor, ToSnapshot(sessionId, managed, created.Value.InitialSnapshot)));
        }
        finally
        {
            playSpace.Gate.Release();
        }
    }

    /// <summary>現在のプレイスペース状態を取得します。</summary>
    public async ValueTask<ProtocolResponse<GameOasisSnapshot>> GetSnapshotAsync(
        GameOasisSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(sessionId, out var session))
            return SessionNotFound<GameOasisSnapshot>(sessionId);

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentSession(sessionId, session))
                return SessionNotFound<GameOasisSnapshot>(sessionId);
            var snapshot = await session.PlaySpace.Protocol.GetSnapshotAsync(
                new GetPlaySpaceSnapshotRequest(session.PlaySpaceSessionId), cancellationToken);
            return snapshot.IsSuccess && snapshot.Value is not null
                ? ProtocolResponse<GameOasisSnapshot>.Success(ToSnapshot(sessionId, session, snapshot.Value))
                : ForwardFailure<GameOasisSnapshot>(snapshot.Error, "play-space-snapshot-failed");
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>行動をプレイスペースへ適用します。</summary>
    public async ValueTask<ProtocolResponse<GameOasisActionApplied>> ApplyActionAsync(
        ApplyGameOasisActionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(request.SessionId, out var session))
            return SessionNotFound<GameOasisActionApplied>(request.SessionId);

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentSession(request.SessionId, session))
                return SessionNotFound<GameOasisActionApplied>(request.SessionId);
            if (session.OperationalState == GameOasisOperationalState.Paused)
            {
                var current = await session.PlaySpace.Protocol.GetSnapshotAsync(
                    new GetPlaySpaceSnapshotRequest(session.PlaySpaceSessionId), cancellationToken);
                if (!current.IsSuccess || current.Value is null)
                    return ForwardFailure<GameOasisActionApplied>(current.Error, "play-space-snapshot-failed");
                return ProtocolResponse<GameOasisActionApplied>.Success(new(
                    false,
                    ToSnapshot(request.SessionId, session, current.Value),
                    [],
                    new ProtocolError("game-session-paused", "Player actions are not accepted while the game is paused.")));
            }
            if (session.OperationalState == GameOasisOperationalState.Adjudicated)
            {
                var current = await session.PlaySpace.Protocol.GetSnapshotAsync(
                    new GetPlaySpaceSnapshotRequest(session.PlaySpaceSessionId), cancellationToken);
                if (!current.IsSuccess || current.Value is null)
                    return ForwardFailure<GameOasisActionApplied>(current.Error, "play-space-snapshot-failed");
                return ProtocolResponse<GameOasisActionApplied>.Success(new(
                    false,
                    ToSnapshot(request.SessionId, session, current.Value),
                    [],
                    new ProtocolError("game-session-adjudicated", "Player actions are not accepted after a game-master adjudication.")));
            }
            var applied = await session.PlaySpace.Protocol.ApplyActionAsync(new ApplyPlaySpaceActionRequest(
                session.PlaySpaceSessionId, request.Action, request.ExpectedRevision), cancellationToken);
            return applied.IsSuccess && applied.Value is not null
                ? ProtocolResponse<GameOasisActionApplied>.Success(new GameOasisActionApplied(
                    applied.Value.IsAccepted,
                    ToSnapshot(request.SessionId, session, applied.Value.Snapshot),
                    applied.Value.Events,
                    applied.Value.Rejection))
                : ForwardFailure<GameOasisActionApplied>(applied.Error, "play-space-action-failed");
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>コンシェルジュが所有する停止・再開状態を変更します。</summary>
    public async ValueTask<ProtocolResponse<GameOasisOperationApplied>> ApplyOperationAsync(
        ApplyGameOasisOperationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(request.SessionId, out var session))
            return SessionNotFound<GameOasisOperationApplied>(request.SessionId);

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentSession(request.SessionId, session))
                return SessionNotFound<GameOasisOperationApplied>(request.SessionId);
            var playSpaceSnapshot = await session.PlaySpace.Protocol.GetSnapshotAsync(
                new GetPlaySpaceSnapshotRequest(session.PlaySpaceSessionId), cancellationToken);
            if (!playSpaceSnapshot.IsSuccess || playSpaceSnapshot.Value is null)
                return ForwardFailure<GameOasisOperationApplied>(playSpaceSnapshot.Error, "play-space-snapshot-failed");
            if (request.ExpectedOperationRevision != session.OperationRevision)
                return RejectedOperation(
                    request.SessionId,
                    session,
                    playSpaceSnapshot.Value,
                    "operation-revision-conflict",
                    $"Expected operation revision {request.ExpectedOperationRevision}, current revision is {session.OperationRevision}.");
            if (playSpaceSnapshot.Value.IsTerminal)
                return RejectedOperation(
                    request.SessionId,
                    session,
                    playSpaceSnapshot.Value,
                    "game-result-already-final",
                    "The play-space has already finalized the game result.");

            if (request.OperationName == AdjudicateOperation)
            {
                if (request.Parameters is null)
                    return RejectedOperation(
                        request.SessionId,
                        session,
                        playSpaceSnapshot.Value,
                        "adjudication-result-required",
                        "The adjudicate operation requires a self-describing result document.");
                if (session.OperationalState == GameOasisOperationalState.Adjudicated)
                    return RejectedOperation(
                        request.SessionId,
                        session,
                        playSpaceSnapshot.Value,
                        "game-result-already-final",
                        "The game result is already final.");
                session.AdjudicatedOutcome = request.Parameters;
                session.OperationalState = GameOasisOperationalState.Adjudicated;
                session.OperationRevision++;
                return ProtocolResponse<GameOasisOperationApplied>.Success(new(
                    true,
                    ToSnapshot(request.SessionId, session, playSpaceSnapshot.Value),
                    null));
            }

            var target = request.OperationName switch
            {
                PauseOperation => GameOasisOperationalState.Paused,
                ResumeOperation => GameOasisOperationalState.Running,
                _ => (GameOasisOperationalState?)null,
            };
            if (target is null)
                return RejectedOperation(
                    request.SessionId,
                    session,
                    playSpaceSnapshot.Value,
                    "unsupported-game-oasis-operation",
                    $"Operation '{request.OperationName}' is not supported.");
            if (session.OperationalState == target.Value)
                return RejectedOperation(
                    request.SessionId,
                    session,
                    playSpaceSnapshot.Value,
                    "game-oasis-operation-not-applicable",
                    $"The game is already {target.Value.ToString().ToLowerInvariant()}.");
            if (session.OperationalState == GameOasisOperationalState.Adjudicated)
                return RejectedOperation(
                    request.SessionId,
                    session,
                    playSpaceSnapshot.Value,
                    "game-result-already-final",
                    "An adjudicated game cannot be paused or resumed.");

            session.OperationalState = target.Value;
            session.OperationRevision++;
            return ProtocolResponse<GameOasisOperationApplied>.Success(new(
                true,
                ToSnapshot(request.SessionId, session, playSpaceSnapshot.Value),
                null));
        }
        finally
        {
            session.Gate.Release();
        }
    }

    /// <summary>Game Oasisセッションと対応するプレイスペースセッションを終了します。</summary>
    public async ValueTask<ProtocolResponse<GameOasisSessionClosed>> CloseSessionAsync(
        GameOasisSessionId sessionId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_sessions.TryGetValue(sessionId, out var session))
            return SessionNotFound<GameOasisSessionClosed>(sessionId);

        await session.Gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsCurrentSession(sessionId, session))
                return SessionNotFound<GameOasisSessionClosed>(sessionId);
            var closed = await session.PlaySpace.Protocol.CloseSessionAsync(
                new ClosePlaySpaceSessionRequest(session.PlaySpaceSessionId), cancellationToken);
            if (!closed.IsSuccess)
                return ForwardFailure<GameOasisSessionClosed>(closed.Error, "play-space-session-close-failed");
            if (!_sessions.TryRemove(new KeyValuePair<GameOasisSessionId, ManagedSession>(sessionId, session)))
                return Failure<GameOasisSessionClosed>("session-close-conflict", "The Game Oasis session changed while it was closing.");
            return ProtocolResponse<GameOasisSessionClosed>.Success(new GameOasisSessionClosed(sessionId));
        }
        finally
        {
            session.Gate.Release();
        }
    }

    private static ProtocolResponse<T> SessionNotFound<T>(GameOasisSessionId sessionId) =>
        Failure<T>("game-oasis-session-not-found", $"Game Oasis session '{sessionId}' was not found.");

    private static ProtocolResponse<T> ForwardFailure<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new ProtocolError(fallbackCode, "The connected protocol returned an invalid failure response."));

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new ProtocolError(code, message));

    private static ProtocolResponse<GameOasisOperationApplied> RejectedOperation(
        GameOasisSessionId sessionId,
        ManagedSession session,
        PlaySpaceSnapshot snapshot,
        string code,
        string message)
    {
        var error = new ProtocolError(code, message);
        return ProtocolResponse<GameOasisOperationApplied>.Success(new(
            false,
            ToSnapshot(sessionId, session, snapshot),
            error));
    }

    private bool IsCurrentSession(GameOasisSessionId sessionId, ManagedSession session) =>
        _sessions.TryGetValue(sessionId, out var current) && ReferenceEquals(current, session);

    private static GameOasisSnapshot ToSnapshot(
        GameOasisSessionId sessionId,
        ManagedSession session,
        PlaySpaceSnapshot snapshot) =>
        new(
            sessionId,
            session.PlaySpace.Descriptor.TypeId,
            snapshot.Revision,
            session.OperationRevision,
            session.OperationalState,
            snapshot.State,
            snapshot.IsTerminal || session.AdjudicatedOutcome is not null,
            session.AdjudicatedOutcome ?? snapshot.Outcome);

    private sealed class RegisteredPlaySpace(PlaySpaceDescriptor descriptor, IPlaySpaceProtocol protocol)
    {
        public PlaySpaceDescriptor Descriptor { get; } = descriptor;
        public IPlaySpaceProtocol Protocol { get; } = protocol;
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }

    private sealed class ManagedSession(RegisteredPlaySpace playSpace, PlaySpaceSessionId playSpaceSessionId)
    {
        public RegisteredPlaySpace PlaySpace { get; } = playSpace;
        public PlaySpaceSessionId PlaySpaceSessionId { get; } = playSpaceSessionId;
        public long OperationRevision { get; set; }
        public GameOasisOperationalState OperationalState { get; set; } = GameOasisOperationalState.Running;
        public ContractDocument? AdjudicatedOutcome { get; set; }
        public SemaphoreSlim Gate { get; } = new(1, 1);
    }
}
