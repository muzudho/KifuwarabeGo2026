namespace KifuwarabeGo2026.Gui.Application.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;
using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Game Oasis方式のローカル対局セッションを開始・終了します。
/// 旧ローカル対局を移行するための境界であり、ゲーム状態は所有しません。
/// </summary>
public sealed class LocalMatchGameOasisLifecycle(GameOasisBoardController boardController) : IDisposable
{
    private readonly GameOasisBoardController _boardController = boardController ?? throw new ArgumentNullException(nameof(boardController));
    private readonly CancellationTokenSource _cancellation = new();
    private Task<Completion>? _pending;
    private bool _disposed;

    public LocalMatchGameOasisState State { get; private set; } = LocalMatchGameOasisState.Idle;
    public GuiBoardView? Board { get; private set; }
    public ProtocolError? LastError { get; private set; }
    public bool IsBusy => _pending is not null;

    public bool BeginStart(MatchSnapshot initialSnapshot, decimal komi)
    {
        ArgumentNullException.ThrowIfNull(initialSnapshot);
        if (!CanBegin(LocalMatchGameOasisState.Idle)) return false;

        ContractDocument configuration;
        try
        {
            configuration = LocalMatchGameOasisConfiguration.Create(initialSnapshot, komi);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            LastError = new("invalid-local-match-initial-state", exception.Message);
            return false;
        }

        State = LocalMatchGameOasisState.Opening;
        LastError = null;
        _pending = StartAsync(configuration, _cancellation.Token);
        return true;
    }

    public bool BeginClose()
    {
        if (!CanBegin(LocalMatchGameOasisState.Ready, LocalMatchGameOasisState.Faulted) || Board is null) return false;
        State = LocalMatchGameOasisState.Closing;
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
            State = completion.State;
            Board = completion.Board;
            LastError = completion.Error;
        }
        else
        {
            var exception = pending.Exception?.GetBaseException();
            State = Board is null ? LocalMatchGameOasisState.Idle : LocalMatchGameOasisState.Faulted;
            LastError = new("local-match-lifecycle-exception", exception?.Message ?? "The local match operation was cancelled.");
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

    private bool CanBegin(params LocalMatchGameOasisState[] states) =>
        !_disposed && _pending is null && Array.IndexOf(states, State) >= 0;

    private async Task<Completion> StartAsync(ContractDocument configuration, CancellationToken cancellationToken)
    {
        var response = await _boardController.OpenAsync(
            new PlaySpaceTypeId(GameOasisOfficialNames.Go),
            configuration,
            cancellationToken);
        return response.IsSuccess && response.Value is not null
            ? new(LocalMatchGameOasisState.Ready, response.Value, null)
            : new(LocalMatchGameOasisState.Idle, null, NormalizeError(response.Error));
    }

    private async Task<Completion> CloseAsync(CancellationToken cancellationToken)
    {
        var response = await _boardController.CloseAsync(cancellationToken);
        return response.IsSuccess
            ? new(LocalMatchGameOasisState.Idle, null, null)
            : new(LocalMatchGameOasisState.Faulted, Board, NormalizeError(response.Error));
    }

    private static ProtocolError NormalizeError(ProtocolError? error) =>
        error ?? new("local-match-lifecycle-failed", "The local match lifecycle returned an invalid failure response.");

    private sealed record Completion(LocalMatchGameOasisState State, GuiBoardView? Board, ProtocolError? Error);
}

public enum LocalMatchGameOasisState
{
    Idle,
    Opening,
    Ready,
    Closing,
    Faulted,
}
