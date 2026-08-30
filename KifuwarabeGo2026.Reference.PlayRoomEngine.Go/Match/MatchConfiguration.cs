namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System.Collections.ObjectModel;

/// <summary>
/// Describes the immutable rules needed to construct a match session.
/// </summary>
public sealed class MatchConfiguration
{
    private readonly ReadOnlyCollection<MatchSetupStone> _setupStones;

    public MatchConfiguration(
        int boardSize = 19,
        int moveLimit = 0,
        GoStone startingTurn = GoStone.Black,
        IEnumerable<MatchSetupStone>? setupStones = null)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }

        if (moveLimit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(moveLimit), moveLimit, "Move limit cannot be negative.");
        }

        if (startingTurn is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(startingTurn), startingTurn, "Starting turn must be black or white.");
        }

        BoardSize = boardSize;
        MoveLimit = moveLimit;
        StartingTurn = startingTurn;
        var copiedSetupStones = setupStones?.ToArray() ?? [];
        ValidateSetupStones(copiedSetupStones);
        _setupStones = Array.AsReadOnly(copiedSetupStones);
    }

    public int BoardSize { get; }

    public int MoveLimit { get; }

    public GoStone StartingTurn { get; }

    public IReadOnlyList<MatchSetupStone> SetupStones => _setupStones;

    private void ValidateSetupStones(IEnumerable<MatchSetupStone> setupStones)
    {
        var occupiedPoints = new HashSet<GoPoint>();
        foreach (var setupStone in setupStones)
        {
            if (setupStone.Stone is not (GoStone.Black or GoStone.White))
            {
                throw new ArgumentException("A setup stone must be black or white.", nameof(setupStones));
            }

            if (setupStone.Point.X < 0 || setupStone.Point.X >= BoardSize ||
                setupStone.Point.Y < 0 || setupStone.Point.Y >= BoardSize)
            {
                throw new ArgumentException($"Setup point {setupStone.Point} is outside the board.", nameof(setupStones));
            }

            if (!occupiedPoints.Add(setupStone.Point))
            {
                throw new ArgumentException($"Setup point {setupStone.Point} is duplicated.", nameof(setupStones));
            }
        }
    }
}
