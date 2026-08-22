namespace KifuwarabeGo2026.GameOasis.Concierge;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>ゲームコンシェルジュをGUIへ公開するProtocol Gアダプターです。</summary>
public sealed class GameOasisGuiProtocol(GameOasisConcierge concierge) : IGuiProtocol
{
    private readonly GameOasisConcierge _concierge = concierge ?? throw new ArgumentNullException(nameof(concierge));

    public ValueTask<ProtocolResponse<IReadOnlyList<GuiPlaySpaceEntry>>> GetPlaySpacesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<GuiPlaySpaceEntry> entries = _concierge.GetPlaySpaces()
            .Select(value => new GuiPlaySpaceEntry(
                value.TypeId,
                value.DisplayName,
                value.ImplementationName,
                value.ImplementationVersion,
                value.Capabilities))
            .ToArray();
        return ValueTask.FromResult(ProtocolResponse<IReadOnlyList<GuiPlaySpaceEntry>>.Success(entries));
    }

    public ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(
        PlaySpaceTypeId playSpaceTypeId,
        CancellationToken cancellationToken = default) =>
        _concierge.GetConfigurationSchemaAsync(playSpaceTypeId, cancellationToken);

    public async ValueTask<ProtocolResponse<GuiSessionOpened>> OpenSessionAsync(
        GuiOpenSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var opened = await _concierge.OpenSessionAsync(
            request.PlaySpaceTypeId, request.Configuration, cancellationToken);
        return opened.IsSuccess && opened.Value is not null
            ? ProtocolResponse<GuiSessionOpened>.Success(
                new GuiSessionOpened(ToGuiSnapshot(opened.Value.InitialSnapshot)))
            : ForwardFailure<GuiSessionOpened>(opened.Error, "open-session-failed");
    }

    public async ValueTask<ProtocolResponse<GuiGameSnapshot>> GetSnapshotAsync(
        GuiGetSnapshotRequest request,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _concierge.GetSnapshotAsync(request.SessionId, cancellationToken);
        return snapshot.IsSuccess && snapshot.Value is not null
            ? ProtocolResponse<GuiGameSnapshot>.Success(ToGuiSnapshot(snapshot.Value))
            : ForwardFailure<GuiGameSnapshot>(snapshot.Error, "get-snapshot-failed");
    }

    public async ValueTask<ProtocolResponse<GuiActionSubmitted>> SubmitActionAsync(
        GuiSubmitActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var applied = await _concierge.ApplyActionAsync(new ApplyGameOasisActionRequest(
            request.SessionId, request.Action, request.ExpectedRevision), cancellationToken);
        return applied.IsSuccess && applied.Value is not null
            ? ProtocolResponse<GuiActionSubmitted>.Success(new GuiActionSubmitted(
                applied.Value.IsAccepted,
                ToGuiSnapshot(applied.Value.Snapshot),
                applied.Value.Events,
                applied.Value.Rejection))
            : ForwardFailure<GuiActionSubmitted>(applied.Error, "submit-action-failed");
    }

    public async ValueTask<ProtocolResponse<GuiSessionClosed>> CloseSessionAsync(
        GuiCloseSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var closed = await _concierge.CloseSessionAsync(request.SessionId, cancellationToken);
        return closed.IsSuccess && closed.Value is not null
            ? ProtocolResponse<GuiSessionClosed>.Success(new GuiSessionClosed(closed.Value.SessionId))
            : ForwardFailure<GuiSessionClosed>(closed.Error, "close-session-failed");
    }

    private static GuiGameSnapshot ToGuiSnapshot(GameOasisSnapshot snapshot) =>
        new(
            snapshot.SessionId,
            snapshot.PlaySpaceTypeId,
            snapshot.Revision,
            snapshot.State,
            snapshot.IsTerminal,
            snapshot.Outcome);

    private static ProtocolResponse<T> ForwardFailure<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new ProtocolError(fallbackCode, "The Concierge returned an invalid failure response."));
}
