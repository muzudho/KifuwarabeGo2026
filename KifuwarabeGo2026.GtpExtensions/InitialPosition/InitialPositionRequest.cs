namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Match;
using KifuwarabeGo2026.Shared.Domain;
using System.Collections.ObjectModel;

/// <summary>
/// Contains the engine-independent facts needed to reproduce a Match initial position.
/// </summary>
public sealed class InitialPositionRequest
{
    private readonly ReadOnlyCollection<MatchSetupStone> _setupStones;

    public InitialPositionRequest(
        int boardSize,
        decimal komi,
        GoStone startingTurn,
        IEnumerable<MatchSetupStone>? setupStones = null)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }

        if (startingTurn is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(startingTurn), startingTurn, "Starting turn must be black or white.");
        }

        BoardSize = boardSize;
        Komi = komi;
        StartingTurn = startingTurn;
        var copiedSetupStones = setupStones?.ToArray() ?? [];
        ValidateSetupStones(copiedSetupStones, boardSize);
        _setupStones = Array.AsReadOnly(copiedSetupStones);
    }

    public int BoardSize { get; }

    public decimal Komi { get; }

    public GoStone StartingTurn { get; }

    public IReadOnlyList<MatchSetupStone> SetupStones => _setupStones;

    public static InitialPositionRequest FromSnapshot(MatchSnapshot snapshot, decimal komi)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new InitialPositionRequest(snapshot.BoardSize, komi, snapshot.CurrentTurn, snapshot.SetupStones);
    }

    private static void ValidateSetupStones(IEnumerable<MatchSetupStone> setupStones, int boardSize)
    {
        var occupiedPoints = new HashSet<GoPoint>();
        foreach (var setupStone in setupStones)
        {
            if (setupStone.Stone is not (GoStone.Black or GoStone.White))
            {
                throw new ArgumentException("A setup stone must be black or white.", nameof(setupStones));
            }

            if (setupStone.Point.X < 0 || setupStone.Point.X >= boardSize ||
                setupStone.Point.Y < 0 || setupStone.Point.Y >= boardSize)
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
