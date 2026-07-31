namespace KifuwarabeGo2026.Match;

using KifuwarabeGo2026.Shared.Domain;
using System.Collections.ObjectModel;

/// <summary>
/// Provides an immutable view of a match at one revision.
/// </summary>
public sealed class MatchSnapshot
{
    private readonly ReadOnlyCollection<GoStone> _stones;

    internal MatchSnapshot(
        int boardSize,
        GoStone[] stones,
        GoStone currentTurn,
        GoPoint? koPoint,
        int consecutivePasses,
        int moveCount,
        long revision,
        MatchEndReason endReason,
        GoStone? winner)
    {
        BoardSize = boardSize;
        _stones = Array.AsReadOnly(stones);
        CurrentTurn = currentTurn;
        KoPoint = koPoint;
        ConsecutivePasses = consecutivePasses;
        MoveCount = moveCount;
        Revision = revision;
        EndReason = endReason;
        Winner = winner;
    }

    public int BoardSize { get; }

    public IReadOnlyList<GoStone> Stones => _stones;

    public GoStone CurrentTurn { get; }

    public GoPoint? KoPoint { get; }

    public int ConsecutivePasses { get; }

    public int MoveCount { get; }

    public long Revision { get; }

    public MatchEndReason EndReason { get; }

    public bool IsCompleted => EndReason != MatchEndReason.None;

    public GoStone? Winner { get; }

    public GoStone GetStone(GoPoint point)
    {
        if (point.X < 0 || point.X >= BoardSize || point.Y < 0 || point.Y >= BoardSize)
        {
            throw new ArgumentOutOfRangeException(nameof(point), point, "Point is outside the board.");
        }

        return _stones[(point.Y * BoardSize) + point.X];
    }
}
