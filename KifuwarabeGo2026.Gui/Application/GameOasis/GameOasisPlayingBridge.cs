namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.Reference.GUI;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>同期フレームで動く現行GUIと非同期Protocol G盤面操作を接続します。</summary>
public sealed class GameOasisPlayingBridge(GameOasisBoardController controller) : IDisposable
{
    private readonly GameOasisBoardController _controller = controller ?? throw new ArgumentNullException(nameof(controller));
    private readonly CancellationTokenSource _cancellation = new();
    private Task<BridgeCompletion>? _pending;
    private bool _disposed;

    public GameOasisPlayingState State { get; private set; } = GameOasisPlayingState.Idle;
    public ProtocolError? LastError { get; private set; }
    public GuiBoardView? Board { get; private set; }
    public bool IsBusy => _pending is not null;

    public bool SupportsAction(PlaySpaceTypeId playSpaceTypeId, string capabilityId) =>
        _controller.SupportsAction(playSpaceTypeId, capabilityId);

    public bool BeginOpen(PlaySpaceTypeId playSpaceTypeId, ContractDocument configuration)
    {
        if (!CanBegin(GameOasisPlayingState.Idle)) return false;
        State = GameOasisPlayingState.Opening;
        _pending = OpenAsync(playSpaceTypeId, configuration, _cancellation.Token);
        return true;
    }

    public bool BeginPlay(int x, int y) => BeginSubmit(() => _controller.PlayAsync(x, y, _cancellation.Token));
    public bool BeginPass() => BeginSubmit(() => _controller.PassAsync(_cancellation.Token));
    public bool BeginResign() => BeginSubmit(() => _controller.ResignAsync(_cancellation.Token));

    public bool BeginRefresh()
    {
        if (!CanBegin(GameOasisPlayingState.Ready, GameOasisPlayingState.Terminal)) return false;
        State = GameOasisPlayingState.Refreshing;
        _pending = RefreshAsync(Board!, _cancellation.Token);
        return true;
    }

    public bool BeginClose()
    {
        if (!CanBegin(GameOasisPlayingState.Ready, GameOasisPlayingState.Terminal, GameOasisPlayingState.Faulted)) return false;
        State = GameOasisPlayingState.Closing;
        _pending = CloseAsync(_cancellation.Token);
        return true;
    }

    public bool Update()
    {
        var pending = _pending;
        if (pending is null || !pending.IsCompleted) return false;
        _pending = null;
        if (pending.IsCompletedSuccessfully)
        {
            var completion = pending.Result;
            LastError = completion.Error;
            Board = completion.Board;
            State = completion.State;
        }
        else
        {
            var exception = pending.Exception?.GetBaseException();
            LastError = new("gui-operation-exception", exception?.Message ?? "The GUI operation was cancelled.");
            State = GameOasisPlayingState.Faulted;
        }
        return true;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private bool BeginSubmit(Func<ValueTask<ProtocolResponse<GuiActionSubmitted>>> submit)
    {
        if (!CanBegin(GameOasisPlayingState.Ready)) return false;
        State = GameOasisPlayingState.Submitting;
        _pending = SubmitAsync(submit, Board!, _cancellation.Token);
        return true;
    }

    private bool CanBegin(params GameOasisPlayingState[] states) =>
        !_disposed && _pending is null && states.Contains(State);

    private async Task<BridgeCompletion> OpenAsync(PlaySpaceTypeId typeId, ContractDocument configuration, CancellationToken cancellationToken)
    {
        var response = await _controller.OpenAsync(typeId, configuration, cancellationToken);
        return response.IsSuccess && response.Value is not null
            ? Success(response.Value)
            : new(GameOasisPlayingState.Idle, null, NormalizeError(response.Error));
    }

    private async Task<BridgeCompletion> SubmitAsync(
        Func<ValueTask<ProtocolResponse<GuiActionSubmitted>>> submit,
        GuiBoardView currentBoard,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var response = await submit();
        if (!response.IsSuccess || response.Value is null) return Success(currentBoard, NormalizeError(response.Error));
        var board = _controller.GetBoard();
        return board.IsSuccess && board.Value is not null ? Success(board.Value, response.Value.Rejection) : Failure(board.Error);
    }

    private async Task<BridgeCompletion> RefreshAsync(GuiBoardView currentBoard, CancellationToken cancellationToken)
    {
        var response = await _controller.RefreshAsync(cancellationToken);
        return response.IsSuccess && response.Value is not null
            ? Success(response.Value)
            : Success(currentBoard, NormalizeError(response.Error));
    }

    private async Task<BridgeCompletion> CloseAsync(CancellationToken cancellationToken)
    {
        var response = await _controller.CloseAsync(cancellationToken);
        return response.IsSuccess
            ? new(GameOasisPlayingState.Idle, null, null)
            : Board is { } board
                ? Success(board, NormalizeError(response.Error))
                : Failure(response.Error);
    }

    private static BridgeCompletion Success(GuiBoardView board, ProtocolError? warning = null) =>
        new(board.IsTerminal ? GameOasisPlayingState.Terminal : GameOasisPlayingState.Ready, board, warning);

    private static BridgeCompletion Failure(ProtocolError? error) =>
        new(GameOasisPlayingState.Faulted, null, NormalizeError(error));

    private static ProtocolError NormalizeError(ProtocolError? error) =>
        error ?? new("gui-operation-failed", "The GUI operation returned an invalid failure response.");

    private sealed record BridgeCompletion(GameOasisPlayingState State, GuiBoardView? Board, ProtocolError? Error);
}

public enum GameOasisPlayingState
{
    Idle,
    Opening,
    Ready,
    Submitting,
    Refreshing,
    Terminal,
    Closing,
    Faulted,
}
