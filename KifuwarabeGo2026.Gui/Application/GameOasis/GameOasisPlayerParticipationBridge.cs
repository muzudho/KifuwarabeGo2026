namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;
using KifuwarabeGo2026.Reference.GUI;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// 同期フレームからProtocol Pの参加、着手、退出を扱います。
/// ゲーム状態は所有せず、着手後の表示状態はProtocol Gから取り直します。
/// </summary>
public sealed class GameOasisPlayerParticipationBridge(
    GameOasisPlayerParticipation participation,
    GameOasisBoardController boardController) : IDisposable
{
    private readonly GameOasisPlayerParticipation _participation = participation ?? throw new ArgumentNullException(nameof(participation));
    private readonly GameOasisBoardController _boardController = boardController ?? throw new ArgumentNullException(nameof(boardController));
    private readonly CancellationTokenSource _cancellation = new();
    private Task<Completion>? _pending;
    private bool _disposed;

    public GameOasisPlayerParticipationState State { get; private set; } = GameOasisPlayerParticipationState.Idle;
    public PlayerBindingId? BindingId { get; private set; }
    public ProtocolError? LastError { get; private set; }
    public GuiBoardView? Board { get; private set; }
    public bool IsBusy => _pending is not null;

    public bool BeginBind(IPlayerProtocol protocol, string roleId)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        if (!CanBegin(GameOasisPlayerParticipationState.Idle)) return false;
        State = GameOasisPlayerParticipationState.Binding;
        _pending = BindAsync(protocol, roleId, _cancellation.Token);
        return true;
    }

    public bool BeginTurn()
    {
        if (!CanBegin(GameOasisPlayerParticipationState.Ready) || BindingId is not { } bindingId) return false;
        State = GameOasisPlayerParticipationState.Thinking;
        _pending = TurnAsync(bindingId, _cancellation.Token);
        return true;
    }

    public bool BeginUnbind(string reason)
    {
        if (!CanBegin(GameOasisPlayerParticipationState.Ready, GameOasisPlayerParticipationState.Faulted) ||
            BindingId is not { } bindingId) return false;
        State = GameOasisPlayerParticipationState.Unbinding;
        _pending = UnbindAsync(bindingId, reason, _cancellation.Token);
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
            State = completion.State;
            BindingId = completion.BindingId;
            LastError = completion.Error;
            Board = completion.Board;
        }
        else
        {
            var exception = pending.Exception?.GetBaseException();
            LastError = new("player-operation-exception", exception?.Message ?? "The player operation was cancelled.");
            State = BindingId is null ? GameOasisPlayerParticipationState.Idle : GameOasisPlayerParticipationState.Faulted;
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

    private bool CanBegin(params GameOasisPlayerParticipationState[] states) =>
        !_disposed && _pending is null && Array.IndexOf(states, State) >= 0;

    private async Task<Completion> BindAsync(IPlayerProtocol protocol, string roleId, CancellationToken cancellationToken)
    {
        var response = await _participation.RegisterAndBindAsync(protocol, roleId, cancellationToken);
        return response.IsSuccess && response.Value is not null
            ? new(GameOasisPlayerParticipationState.Ready, response.Value.BindingId, null, null)
            : new(GameOasisPlayerParticipationState.Idle, null, NormalizeError(response.Error), null);
    }

    private async Task<Completion> TurnAsync(PlayerBindingId bindingId, CancellationToken cancellationToken)
    {
        var response = await _participation.RequestAndApplyActionAsync(bindingId, cancellationToken);
        if (!response.IsSuccess || response.Value is null)
            return new(GameOasisPlayerParticipationState.Faulted, bindingId, NormalizeError(response.Error), Board);

        var refreshed = await _boardController.RefreshAsync(cancellationToken);
        return refreshed.IsSuccess && refreshed.Value is not null
            ? new(GameOasisPlayerParticipationState.Ready, bindingId, response.Value.Applied.Rejection, refreshed.Value)
            : new(GameOasisPlayerParticipationState.Faulted, bindingId, NormalizeError(refreshed.Error), Board);
    }

    private async Task<Completion> UnbindAsync(PlayerBindingId bindingId, string reason, CancellationToken cancellationToken)
    {
        var response = await _participation.UnbindAsync(bindingId, reason, cancellationToken);
        return response.IsSuccess
            ? new(GameOasisPlayerParticipationState.Idle, null, null, null)
            : new(GameOasisPlayerParticipationState.Faulted, bindingId, NormalizeError(response.Error), Board);
    }

    private static ProtocolError NormalizeError(ProtocolError? error) =>
        error ?? new("player-operation-failed", "The player operation returned an invalid failure response.");

    private sealed record Completion(
        GameOasisPlayerParticipationState State,
        PlayerBindingId? BindingId,
        ProtocolError? Error,
        GuiBoardView? Board);
}

public enum GameOasisPlayerParticipationState
{
    Idle,
    Binding,
    Ready,
    Thinking,
    Unbinding,
    Faulted,
}
