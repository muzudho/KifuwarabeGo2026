namespace KifuwarabeGo2026.Match;

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
    private GoStone _currentTurn = GoStone.Black;
    private GoPoint? _koPoint;
    private int _consecutivePasses;
    private int _moveCount;
    private long _revision;
    private MatchPhase _phase = MatchPhase.Playing;
    private MatchEndReason _endReason;
    private GoStone? _winner;

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
        int capturedStones = 0) =>
        _actions.Add(new MatchActionRecord(_revision, action, playedBy, point, capturedStones));

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
            _phase,
            _endReason,
            _winner);
    }
}
