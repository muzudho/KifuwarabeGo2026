namespace KifuwarabeGo2026.Reference.GUI;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>描画技術から独立した、Protocol G利用側の最小セッションモデルです。</summary>
public sealed class GameOasisGuiClient(IGuiProtocol protocol)
{
    private readonly IGuiProtocol _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private readonly object _stateLock = new();
    private GuiClientState _state = new([], null, null);

    public GuiClientState State
    {
        get { lock (_stateLock) return _state; }
    }

    public async ValueTask<ProtocolResponse<IReadOnlyList<GuiPlaySpaceEntry>>> InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var response = await _protocol.GetPlaySpacesAsync(cancellationToken);
            Update(response.IsSuccess && response.Value is not null
                ? new(response.Value, State.ActiveSnapshot, null)
                : State with { LastError = response.Error });
            return response;
        }
        finally { _operationLock.Release(); }
    }

    public async ValueTask<ProtocolResponse<GuiSessionOpened>> OpenSessionAsync(
        PlaySpaceTypeId playSpaceTypeId,
        ContractDocument configuration,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            if (State.ActiveSnapshot is not null)
                return LocalFailure<GuiSessionOpened>("gui-session-already-open", "Close the active GUI session before opening another one.");
            var response = await _protocol.OpenSessionAsync(new(playSpaceTypeId, configuration), cancellationToken);
            Update(response.IsSuccess && response.Value is not null
                ? State with { ActiveSnapshot = response.Value.InitialSnapshot, LastError = null }
                : State with { LastError = response.Error });
            return response;
        }
        finally { _operationLock.Release(); }
    }

    public async ValueTask<ProtocolResponse<GuiActionSubmitted>> SubmitActionAsync(
        ContractDocument action,
        CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var snapshot = State.ActiveSnapshot;
            if (snapshot is null)
                return LocalFailure<GuiActionSubmitted>("gui-session-not-open", "No GUI session is active.");
            var response = await _protocol.SubmitActionAsync(new(snapshot.SessionId, action, snapshot.Revision), cancellationToken);
            Update(response.IsSuccess && response.Value is not null
                ? State with { ActiveSnapshot = response.Value.Snapshot, LastError = response.Value.Rejection }
                : State with { LastError = response.Error });
            return response;
        }
        finally { _operationLock.Release(); }
    }

    public async ValueTask<ProtocolResponse<GuiGameSnapshot>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var snapshot = State.ActiveSnapshot;
            if (snapshot is null)
                return LocalFailure<GuiGameSnapshot>("gui-session-not-open", "No GUI session is active.");
            var response = await _protocol.GetSnapshotAsync(new(snapshot.SessionId), cancellationToken);
            Update(response.IsSuccess && response.Value is not null
                ? State with { ActiveSnapshot = response.Value, LastError = null }
                : State with { LastError = response.Error });
            return response;
        }
        finally { _operationLock.Release(); }
    }

    public async ValueTask<ProtocolResponse<GuiSessionClosed>> CloseSessionAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var snapshot = State.ActiveSnapshot;
            if (snapshot is null)
                return LocalFailure<GuiSessionClosed>("gui-session-not-open", "No GUI session is active.");
            var response = await _protocol.CloseSessionAsync(new(snapshot.SessionId), cancellationToken);
            Update(response.IsSuccess
                ? State with { ActiveSnapshot = null, LastError = null }
                : State with { LastError = response.Error });
            return response;
        }
        finally { _operationLock.Release(); }
    }

    private void Update(GuiClientState state) { lock (_stateLock) _state = state; }

    private ProtocolResponse<T> LocalFailure<T>(string code, string message)
    {
        var error = new ProtocolError(code, message);
        Update(State with { LastError = error });
        return ProtocolResponse<T>.Failure(error);
    }
}

public sealed record GuiClientState(
    IReadOnlyList<GuiPlaySpaceEntry> PlaySpaces,
    GuiGameSnapshot? ActiveSnapshot,
    ProtocolError? LastError);
