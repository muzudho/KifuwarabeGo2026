namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Owns the authoritative, platform-independent state of one match.
/// </summary>
public sealed class MatchSession
{
    private readonly GoBoard _board;
    private readonly HashSet<ulong> _positionHashes;
    private readonly int _moveLimit;
    private readonly MatchSetupStone[] _setupStones;
    private readonly List<MatchActionRecord> _actions = [];
    private readonly List<MatchEventRecord> _events = [];
    private GoStone _currentTurn = GoStone.Black;
    private GoPoint? _koPoint;
    private int _consecutivePasses;
    private int _moveCount;
    private long _revision;
    private MatchClockSnapshot? _clock;
    private MatchPhase _phase = MatchPhase.Playing;
    private MatchEndReason _endReason;
    private GoStone? _winner;
    private MatchResult? _blackResultDeclaration;
    private MatchResult? _whiteResultDeclaration;
    private MatchResult? _confirmedResult;

    public MatchSession(int boardSize = 19, int moveLimit = 0)
        : this(new MatchConfiguration(boardSize, moveLimit))
    {
    }

    public MatchSession(MatchConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _board = new GoBoard(configuration.BoardSize);
        foreach (var setupStone in configuration.SetupStones)
        {
            if (!_board.TrySetSetupStone(
                    setupStone.Point.X,
                    setupStone.Point.Y,
                    setupStone.Stone))
            {
                throw new ArgumentException($"Invalid setup stone at {setupStone.Point}.", nameof(configuration));
            }
        }

        _currentTurn = configuration.StartingTurn;
        _setupStones = configuration.SetupStones.ToArray();
        _positionHashes = [_board.CurrentHash];
        _moveLimit = configuration.MoveLimit;
    }

    public MatchSnapshot Snapshot => CreateSnapshot();

    public IReadOnlyList<MatchEventRecord> GetEventsAfter(long revision)
    {
        if (revision < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(revision), revision, "Revision cannot be negative.");
        }

        return _events.Where(matchEvent => matchEvent.Revision > revision).ToArray();
    }

    public bool TryApplyAuthoritativeClock(MatchClockUpdate update)
    {
        ValidateClockUpdate(update);
        if (_clock is { } currentClock && update.Sequence <= currentClock.Sequence)
        {
            return false;
        }

        _clock = new MatchClockSnapshot(
            update.Sequence,
            update.SynchronizedAt,
            update.BlackRemaining,
            update.WhiteRemaining,
            update.ActiveTurnDeadline);
        _revision++;
        _events.Add(new MatchEventRecord(
            _revision,
            MatchEventKind.ClockSynchronized,
            null,
            _clock,
            _phase,
            _endReason,
            _winner,
            null));
        return true;
    }

    public MatchResultUpdate DeclareResult(GoStone player, MatchResult result)
    {
        if (player is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(player), player, "A result can be declared only by black or white.");
        }

        ArgumentNullException.ThrowIfNull(result);
        if (_phase != MatchPhase.AwaitingResult)
        {
            return ResultUpdate(false, false);
        }

        var currentDeclaration = player == GoStone.Black
            ? _blackResultDeclaration
            : _whiteResultDeclaration;
        if (currentDeclaration == result)
        {
            return ResultUpdate(true, false);
        }

        if (player == GoStone.Black)
        {
            _blackResultDeclaration = result;
        }
        else
        {
            _whiteResultDeclaration = result;
        }

        var completed = _blackResultDeclaration is not null &&
                        _blackResultDeclaration == _whiteResultDeclaration;
        if (completed)
        {
            _confirmedResult = result;
            _winner = GetWinner(result);
            _phase = MatchPhase.Completed;
        }

        _revision++;
        AddResultEvent(
            completed ? MatchEventKind.ResultConfirmed : MatchEventKind.ResultDeclared,
            new MatchResultEventData(player, result));
        return ResultUpdate(true, true);
    }

    public MatchResultUpdate ResumePlay()
    {
        if (_phase != MatchPhase.AwaitingResult)
        {
            return ResultUpdate(false, false);
        }

        _blackResultDeclaration = null;
        _whiteResultDeclaration = null;
        _confirmedResult = null;
        _winner = null;
        _phase = MatchPhase.Playing;
        _endReason = MatchEndReason.None;
        _consecutivePasses = 0;
        _revision++;
        AddResultEvent(MatchEventKind.PlayResumed, null);
        return ResultUpdate(true, true);
    }

    public MatchResultUpdate ApplyAdjudicatedResult(MatchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (_phase == MatchPhase.Completed)
        {
            return ResultUpdate(false, false);
        }

        _confirmedResult = result;
        _winner = GetWinner(result);
        _phase = MatchPhase.Completed;
        _endReason = MatchEndReason.Adjudication;
        _koPoint = null;
        _revision++;
        AddResultEvent(
            MatchEventKind.ResultAdjudicated,
            new MatchResultEventData(null, result));
        return ResultUpdate(true, true);
    }

    public MatchActionResult Play(GoPoint point)
    {
        var playedBy = _currentTurn;
        if (_phase != MatchPhase.Playing)
        {
            return Failed(MatchActionKind.Play, GetInactiveFailure(), playedBy, point);
        }

        if (!IsOnBoard(point))
        {
            return Failed(MatchActionKind.Play, MatchActionFailure.PointOutsideBoard, playedBy, point);
        }

        if (_board.GetStone(point.X, point.Y) != GoStone.Empty)
        {
            return Failed(MatchActionKind.Play, MatchActionFailure.PointOccupied, playedBy, point);
        }

        if (_koPoint == point)
        {
            return Failed(MatchActionKind.Play, MatchActionFailure.Ko, playedBy, point);
        }

        if (!_board.TryPlaceStone(
                point.X,
                point.Y,
                playedBy,
                _koPoint,
                out var capturedStones,
                out var nextKoPoint))
        {
            return Failed(MatchActionKind.Play, MatchActionFailure.IllegalMove, playedBy, point);
        }

        _koPoint = nextKoPoint;
        _consecutivePasses = 0;
        _moveCount++;
        _revision++;
        if (!_positionHashes.Add(_board.CurrentHash))
        {
            _koPoint = null;
            _phase = MatchPhase.Completed;
            _endReason = MatchEndReason.SuperKoViolation;
            _winner = OppositeOf(playedBy);
        }
        else if (HasReachedMoveLimit())
        {
            _koPoint = null;
            _phase = MatchPhase.AwaitingResult;
            _endReason = MatchEndReason.MoveLimit;
        }
        else
        {
            PassTurn();
        }

        RecordAction(MatchActionKind.Play, playedBy, point, capturedStones);
        return Succeeded(MatchActionKind.Play, playedBy, point, capturedStones);
    }

    public MatchActionResult Pass()
    {
        var playedBy = _currentTurn;
        if (_phase != MatchPhase.Playing)
        {
            return Failed(MatchActionKind.Pass, GetInactiveFailure(), playedBy);
        }

        _koPoint = null;
        _consecutivePasses++;
        _moveCount++;
        _revision++;
        PassTurn();
        if (_consecutivePasses >= 2)
        {
            _phase = MatchPhase.AwaitingResult;
            _endReason = MatchEndReason.ConsecutivePasses;
        }
        else if (HasReachedMoveLimit())
        {
            _phase = MatchPhase.AwaitingResult;
            _endReason = MatchEndReason.MoveLimit;
        }

        RecordAction(MatchActionKind.Pass, playedBy);
        return Succeeded(MatchActionKind.Pass, playedBy);
    }

    public MatchActionResult Resign()
    {
        var playedBy = _currentTurn;
        if (_phase != MatchPhase.Playing)
        {
            return Failed(MatchActionKind.Resign, GetInactiveFailure(), playedBy);
        }

        _koPoint = null;
        _consecutivePasses = 0;
        _revision++;
        _winner = OppositeOf(playedBy);
        _confirmedResult = new MatchResult(
            _winner == GoStone.Black ? MatchOutcome.BlackWin : MatchOutcome.WhiteWin);
        _phase = MatchPhase.Completed;
        _endReason = MatchEndReason.Resignation;
        RecordAction(MatchActionKind.Resign, playedBy);
        return Succeeded(MatchActionKind.Resign, playedBy);
    }

    private bool IsOnBoard(GoPoint point) =>
        point.X >= 0 && point.X < _board.Size && point.Y >= 0 && point.Y < _board.Size;

    private void PassTurn() => _currentTurn = OppositeOf(_currentTurn);

    private bool HasReachedMoveLimit() => _moveLimit > 0 && _moveCount >= _moveLimit;

    private MatchActionFailure GetInactiveFailure() =>
        _phase == MatchPhase.AwaitingResult
            ? MatchActionFailure.AwaitingResult
            : MatchActionFailure.MatchCompleted;

    private void RecordAction(
        MatchActionKind action,
        GoStone playedBy,
        GoPoint? point = null,
        int capturedStones = 0)
    {
        var actionRecord = new MatchActionRecord(_revision, action, playedBy, point, capturedStones);
        _actions.Add(actionRecord);
        _events.Add(new MatchEventRecord(
            _revision,
            MatchEventKind.ActionAccepted,
            actionRecord,
            _clock,
            _phase,
            _endReason,
            _winner,
            null));
    }

    private void AddResultEvent(MatchEventKind kind, MatchResultEventData? resultData) =>
        _events.Add(new MatchEventRecord(
            _revision,
            kind,
            null,
            _clock,
            _phase,
            _endReason,
            _winner,
            resultData));

    private MatchResultUpdate ResultUpdate(bool accepted, bool changed) =>
        new(accepted, changed, _phase == MatchPhase.Completed, CreateSnapshot());

    private static GoStone? GetWinner(MatchResult result) => result.Outcome switch
    {
        MatchOutcome.BlackWin => GoStone.Black,
        MatchOutcome.WhiteWin => GoStone.White,
        _ => null,
    };

    private static void ValidateClockUpdate(MatchClockUpdate update)
    {
        if (update.Sequence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(update), update.Sequence, "Clock sequence cannot be negative.");
        }

        if (update.BlackRemaining < TimeSpan.Zero || update.WhiteRemaining < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(update), "Remaining time cannot be negative.");
        }
    }

    private static GoStone OppositeOf(GoStone stone) =>
        stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private MatchActionResult Succeeded(
        MatchActionKind action,
        GoStone playedBy,
        GoPoint? point = null,
        int capturedStones = 0) =>
        new(true, action, MatchActionFailure.None, playedBy, point, capturedStones, CreateSnapshot());

    private MatchActionResult Failed(
        MatchActionKind action,
        MatchActionFailure failure,
        GoStone playedBy,
        GoPoint? point = null) =>
        new(false, action, failure, playedBy, point, 0, CreateSnapshot());

    private MatchSnapshot CreateSnapshot()
    {
        var stones = new GoStone[_board.Size * _board.Size];
        for (var y = 0; y < _board.Size; y++)
        {
            for (var x = 0; x < _board.Size; x++)
            {
                stones[(y * _board.Size) + x] = _board.GetStone(x, y);
            }
        }

        return new MatchSnapshot(
            _board.Size,
            stones,
            _setupStones.ToArray(),
            _actions.ToArray(),
            _currentTurn,
            _koPoint,
            _consecutivePasses,
            _moveCount,
            _revision,
            _clock,
            _phase,
            _endReason,
            _winner,
            _blackResultDeclaration,
            _whiteResultDeclaration,
            _confirmedResult);
    }
}
