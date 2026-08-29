namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>
/// Classifies a requested setup without using engine-specific behavior.
/// </summary>
public static class InitialPositionClassifier
{
    public static InitialPositionClassification Classify(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var blackStones = request.SetupStones
            .Where(setupStone => setupStone.Stone == GoStone.Black)
            .Select(setupStone => setupStone.Point)
            .ToArray();
        var whiteStoneCount = request.SetupStones.Count - blackStones.Length;

        if (request.SetupStones.Count == 0)
        {
            return new InitialPositionClassification(
                InitialPositionKind.Empty,
                0,
                0,
                request.StartingTurn);
        }

        if (whiteStoneCount > 0)
        {
            return new InitialPositionClassification(
                InitialPositionKind.MixedSetup,
                blackStones.Length,
                whiteStoneCount,
                request.StartingTurn);
        }

        if (FixedHandicapPoints.IsStandardPlacement(request.BoardSize, blackStones))
        {
            return new InitialPositionClassification(
                InitialPositionKind.StandardFixedHandicap,
                blackStones.Length,
                0,
                request.StartingTurn,
                blackStones.Length);
        }

        return new InitialPositionClassification(
            InitialPositionKind.SpecifiedBlackHandicap,
            blackStones.Length,
            0,
            request.StartingTurn);
    }
}
