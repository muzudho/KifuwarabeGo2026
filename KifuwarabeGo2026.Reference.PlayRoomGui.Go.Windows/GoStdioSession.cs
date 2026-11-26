namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;

/// <summary>外部進行役が所有する局面の表示投影と、画面入力の受け渡し。</summary>
public sealed class GoStdioSession
{
    public const string StateSchema = GameOasisOfficialNames.Go + ".display-state.v1";
    private readonly object _gate = new();
    private readonly Queue<MatchInputEvent> _events = new();
    private long _eventId;
    private long _revision = -1;
    private GoPlayRoomViewState _view;
    private IReadOnlyList<GoMatchEvaluation> _evaluations = [];
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private ContractDocument? _state;
    private bool _pendingInput;
    private bool _closed;
    public string SessionId { get; } = Guid.NewGuid().ToString("N");
    public GoPlayRoomLaunchPlan Plan { get; }
    public bool ShouldExit { get { lock (_gate) return _closed; } }
    public GoPlayRoomViewState View { get { lock (_gate) return _view; } }

    public (GoPlayRoomViewState Board, IReadOnlyList<GoMatchEvaluation> Evaluations) CaptureDisplay()
    {
        lock (_gate) return (_view, _evaluations);
    }

    public GoStdioSession(GoPlayRoomLaunchPlan plan)
    {
        Plan = plan;
        _view = GoPlayRoomViewState.Capture(GoPlayRoomActivity.Playing, plan.BoardSize,
            (x, y) => plan.SetupStones.FirstOrDefault(s => s.Point.X == x && s.Point.Y == y)?.Stone ?? GoStone.Empty,
            plan.StartingPlayer, 0, 0, 0, null, null, "", 0, 0);
    }

    public MatchViewState Update(MatchStateUpdate update)
    {
        lock (_gate)
        {
            RequireSession(update.SessionId);
            if (_closed || update.Revision <= _revision) throw new InvalidDataException("Closed session or stale revision.");
            (_view, _evaluations) = Decode(update.State);
            _state = update.State;
            _revision = update.Revision;
            _pendingInput = false;
            return new(SessionId, _revision, _state);
        }
    }

    public bool SubmitInput(MatchActionKind kind, int? x = null, int? y = null)
    {
        lock (_gate)
        {
            if (_closed || _pendingInput || _revision < 0 || _view.Activity == GoPlayRoomActivity.GameOver) return false;
            if (kind == MatchActionKind.PlayPoint && (x is null || y is null || x < 0 || y < 0 || x >= Plan.BoardSize || y >= Plan.BoardSize)) return false;
            if (!Enum.IsDefined(kind)) return false;
            var action = new MatchActionRequest(SessionId, Guid.NewGuid().ToString("N"),
                _view.CurrentTurn == GoStone.Black ? "black" : "white", kind, x, y);
            _events.Enqueue(new(++_eventId, "action", _revision, action));
            _pendingInput = true;
            return true;
        }
    }

    public MatchInputEvents ReadEvents(string sessionId)
    {
        lock (_gate)
        {
            RequireSession(sessionId);
            var events = _events.ToArray();
            _events.Clear();
            return new(SessionId, events, _closed);
        }
    }

    public MatchCompletion Complete(MatchCompletionCommand command)
    {
        lock (_gate)
        {
            RequireSession(command.SessionId);
            if (_closed) throw new InvalidDataException("Session is closed.");
            (_view, _evaluations) = Decode(command.FinalState);
            _state = command.FinalState;
            _closed = true;
            return new(SessionId, MatchCompletionStatus.Finished, _state, command.WinnerRoleId, command.Reason);
        }
    }

    public MatchCompletion Close(string sessionId)
    {
        lock (_gate)
        {
            RequireSession(sessionId);
            if (!_closed)
            {
                _closed = true;
                _events.Enqueue(new(++_eventId, "closed", _revision,
                    Completion: new(SessionId, MatchCompletionStatus.Closed, _state)));
            }
            return new(SessionId, MatchCompletionStatus.Closed, _state);
        }
    }

    private void RequireSession(string id)
    {
        if (id != SessionId) throw new InvalidDataException("sessionId mismatch.");
    }

    private (GoPlayRoomViewState Board, IReadOnlyList<GoMatchEvaluation> Evaluations) Decode(ContractDocument state)
    {
        if (state.MediaType != "application/json" || state.SchemaId != StateSchema)
            throw new InvalidDataException("Unsupported display state schema.");
        using var document = JsonDocument.Parse(state.Content);
        var root = document.RootElement;
        if (root.GetProperty("version").GetInt32() != 1 || root.GetProperty("boardSize").GetInt32() != Plan.BoardSize)
            throw new InvalidDataException("Display version or board size mismatch.");
        var turn = root.GetProperty("currentTurn").GetString() switch
        {
            "black" => GoStone.Black, "white" => GoStone.White,
            _ => throw new InvalidDataException("Invalid currentTurn."),
        };
        var stones = new Dictionary<(int, int), GoStone>();
        foreach (var stone in root.GetProperty("stones").EnumerateArray())
        {
            var x = stone.GetProperty("x").GetInt32();
            var y = stone.GetProperty("y").GetInt32();
            var color = stone.GetProperty("color").GetString() switch
            {
                "black" => GoStone.Black, "white" => GoStone.White,
                _ => throw new InvalidDataException("Invalid stone color."),
            };
            if (x < 0 || y < 0 || x >= Plan.BoardSize || y >= Plan.BoardSize || !stones.TryAdd((x, y), color))
                throw new InvalidDataException("Invalid or duplicate intersection.");
        }
        var over = root.TryGetProperty("gameOver", out var value) && value.GetBoolean();
        var moveCount = root.TryGetProperty("moveCount", out var count) ? count.GetInt32() : 0;
        if (moveCount < 0) throw new InvalidDataException("Invalid move count.");
        var evaluations = root.TryGetProperty("evaluations", out var series)
            ? series.Deserialize<GoMatchEvaluation[]>(JsonOptions) ?? throw new InvalidDataException("Expected an evaluation array.")
            : [];
        var previousMove = 0;
        foreach (var evaluation in evaluations)
        {
            if (evaluation is null || evaluation.MoveNumber <= previousMove || evaluation.MoveNumber > moveCount ||
                evaluation.BlackWinRate is < 0 or > 1 ||
                evaluation.BlackScore is { } score && !double.IsFinite(score) ||
                evaluation.BlackWinRate is null && evaluation.BlackScore is null)
                throw new InvalidDataException("Invalid match evaluation.");
            previousMove = evaluation.MoveNumber;
        }
        var board = GoPlayRoomViewState.Capture(over ? GoPlayRoomActivity.GameOver : GoPlayRoomActivity.Playing,
            Plan.BoardSize, (x, y) => stones.GetValueOrDefault((x, y)), turn, moveCount, 0, 0, null, null, "", moveCount, moveCount);
        return (board, Array.AsReadOnly(evaluations));
    }
}
