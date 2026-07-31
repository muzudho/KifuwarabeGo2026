namespace KifuwarabeGo2026.Match;

using KifuwarabeGo2026.Shared.Domain;
using System.Collections.ObjectModel;

/// <summary>
/// Provides an immutable view of a match at one revision.
/// </summary>
public sealed class MatchSnapshot
{
    private readonly ReadOnlyCollection<GoStone> _stones;
    private readonly ReadOnlyCollection<MatchSetupStone> _setupStones;
    private readonly ReadOnlyCollection<MatchActionRecord> _actions;

    internal MatchSnapshot(
        int boardSize,
        GoStone[] stones,
        MatchSetupStone[] setupStones,
        MatchActionRecord[] actions,
        GoStone currentTurn,
        GoPoint? koPoint,
        int consecutivePasses,
        int moveCount,
        long revision,
        MatchClockSnapshot? clock,
        MatchPhase phase,
        MatchEndReason endReason,
        GoStone? winner)
    {
        BoardSize = boardSize;
        _stones = Array.AsReadOnly(stones);
        _setupStones = Array.AsReadOnly(setupStones);
        _actions = Array.AsReadOnly(actions);
        CurrentTurn = currentTurn;
        KoPoint = koPoint;
        ConsecutivePasses = consecutivePasses;
        MoveCount = moveCount;
        Revision = revision;
        Clock = clock;
        Phase = phase;
        EndReason = endReason;
        Winner = winner;
    }

    public int BoardSize { get; }

    public IReadOnlyList<GoStone> Stones => _stones;

    public IReadOnlyList<MatchSetupStone> SetupStones => _setupStones;

    public IReadOnlyList<MatchActionRecord> Actions => _actions;

    public GoStone CurrentTurn { get; }

    public GoPoint? KoPoint { get; }

    public int ConsecutivePasses { get; }

    public int MoveCount { get; }

    public long Revision { get; }

    public MatchClockSnapshot? Clock { get; }

    public MatchPhase Phase { get; }

    public MatchEndReason EndReason { get; }

    public bool IsAwaitingResult => Phase == MatchPhase.AwaitingResult;

    public bool IsCompleted => Phase == MatchPhase.Completed;

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
