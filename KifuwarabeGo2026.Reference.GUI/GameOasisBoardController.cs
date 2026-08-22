namespace KifuwarabeGo2026.Reference.GUI;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>C#参照GUIの盤面画面からProtocol Gを利用する操作窓口です。</summary>
public sealed class GameOasisBoardController(GameOasisGuiClient client, GameBoardActionAdapters? actionAdapters = null)
{
    private readonly GameOasisGuiClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly GameBoardActionAdapters _actionAdapters = actionAdapters ?? GameBoardActionAdapters.Official;

    public bool SupportsAction(PlaySpaceTypeId playSpaceTypeId, string capabilityId) =>
        _actionAdapters.Supports(playSpaceTypeId, capabilityId);

    public ProtocolResponse<GuiBoardView> GetBoard()
    {
        var snapshot = _client.State.ActiveSnapshot;
        return snapshot is null
            ? Failure<GuiBoardView>("gui-session-not-open", "No Game Oasis GUI session is active.")
            : GameBoardProjection.Project(snapshot);
    }

    public async ValueTask<ProtocolResponse<GuiBoardView>> OpenAsync(
        PlaySpaceTypeId playSpaceTypeId,
        ContractDocument configuration,
        CancellationToken cancellationToken = default)
    {
        var opened = await _client.OpenSessionAsync(playSpaceTypeId, configuration, cancellationToken);
        return opened.IsSuccess ? GetBoard() : Forward<GuiBoardView>(opened.Error, "gui-open-failed");
    }

    public ValueTask<ProtocolResponse<GuiActionSubmitted>> PlayAsync(
        int x,
        int y,
        CancellationToken cancellationToken = default) =>
        SubmitAsync(board => _actionAdapters.CreatePlay(board, x, y), cancellationToken);

    public ValueTask<ProtocolResponse<GuiActionSubmitted>> PassAsync(CancellationToken cancellationToken = default) =>
        SubmitAsync(_actionAdapters.CreatePass, cancellationToken);

    public ValueTask<ProtocolResponse<GuiActionSubmitted>> ResignAsync(CancellationToken cancellationToken = default) =>
        SubmitAsync(_actionAdapters.CreateResign, cancellationToken);

    public async ValueTask<ProtocolResponse<GuiBoardView>> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var refreshed = await _client.RefreshAsync(cancellationToken);
        return refreshed.IsSuccess ? GetBoard() : Forward<GuiBoardView>(refreshed.Error, "gui-refresh-failed");
    }

    public ValueTask<ProtocolResponse<GuiSessionClosed>> CloseAsync(CancellationToken cancellationToken = default) =>
        _client.CloseSessionAsync(cancellationToken);

    private async ValueTask<ProtocolResponse<GuiActionSubmitted>> SubmitAsync(
        Func<GuiBoardView, ProtocolResponse<ContractDocument>> createAction,
        CancellationToken cancellationToken)
    {
        var board = GetBoard();
        if (!board.IsSuccess || board.Value is null)
            return Forward<GuiActionSubmitted>(board.Error, "gui-board-unavailable");
        var action = createAction(board.Value);
        if (!action.IsSuccess || action.Value is null)
            return Forward<GuiActionSubmitted>(action.Error, "gui-action-not-created");
        return await _client.SubmitActionAsync(action.Value, cancellationToken);
    }

    private static ProtocolResponse<T> Forward<T>(ProtocolError? error, string fallbackCode) =>
        ProtocolResponse<T>.Failure(error ?? new(fallbackCode, "The GUI operation returned an invalid failure response."));

    private static ProtocolResponse<T> Failure<T>(string code, string message) =>
        ProtocolResponse<T>.Failure(new(code, message));
}
