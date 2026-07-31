namespace KifuwarabeGo2026.Match;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Owns the authoritative, platform-independent state of one match.
/// </summary>
public sealed class MatchSession
{
    private readonly GoBoard _board;
    private GoStone _currentTurn = GoStone.Black;
    private GoPoint? _koPoint;
    private int _consecutivePasses;
    private int _moveCount;
    private long _revision;
    private MatchEndReason _endReason;
    private GoStone? _winner;

    public MatchSession(int boardSize = 19)
    {
        _board = new GoBoard(boardSize);
    }

    public MatchSnapshot Snapshot => CreateSnapshot();

    public MatchActionResult Play(GoPoint point)
    {
        var playedBy = _currentTurn;
        if (IsCompleted)
        {
            return Failed(MatchActionKind.Play, MatchActionFailure.MatchCompleted, playedBy, point);
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
        CompleteTurn();
        return Succeeded(MatchActionKind.Play, playedBy, point, capturedStones);
    }

    public MatchActionResult Pass()
    {
        var playedBy = _currentTurn;
        if (IsCompleted)
        {
            return Failed(MatchActionKind.Pass, MatchActionFailure.MatchCompleted, playedBy);
        }

        _koPoint = null;
        _consecutivePasses++;
        _moveCount++;
        _revision++;
        PassTurn();
        if (_consecutivePasses >= 2)
        {
            _endReason = MatchEndReason.ConsecutivePasses;
        }

        return Succeeded(MatchActionKind.Pass, playedBy);
    }

    public MatchActionResult Resign()
    {
        var playedBy = _currentTurn;
        if (IsCompleted)
        {
            return Failed(MatchActionKind.Resign, MatchActionFailure.MatchCompleted, playedBy);
        }

        _koPoint = null;
        _consecutivePasses = 0;
        _revision++;
        _winner = OppositeOf(playedBy);
        _endReason = MatchEndReason.Resignation;
        return Succeeded(MatchActionKind.Resign, playedBy);
    }

    private bool IsCompleted => _endReason != MatchEndReason.None;

    private bool IsOnBoard(GoPoint point) =>
        point.X >= 0 && point.X < _board.Size && point.Y >= 0 && point.Y < _board.Size;

    private void CompleteTurn()
    {
        _moveCount++;
        _revision++;
        PassTurn();
    }

    private void PassTurn() => _currentTurn = OppositeOf(_currentTurn);

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
            _currentTurn,
            _koPoint,
            _consecutivePasses,
            _moveCount,
            _revision,
            _endReason,
            _winner);
    }
}
