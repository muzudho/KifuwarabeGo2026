namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Provides the conventional fixed-handicap point sets for supported board sizes.
/// </summary>
public static class FixedHandicapPoints
{
    public static bool IsStandardPlacement(int boardSize, IReadOnlyCollection<GoPoint> points)
    {
        if (points.Count is < 2 or > 9)
        {
            return false;
        }

        var expected = Get(boardSize, points.Count);
        return expected.Count == points.Count && expected.ToHashSet().SetEquals(points);
    }

    public static IReadOnlyList<GoPoint> Get(int boardSize, int stoneCount)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }

        if (stoneCount is < 2 or > 9)
        {
            throw new ArgumentOutOfRangeException(nameof(stoneCount), stoneCount, "Fixed handicap must contain 2 through 9 stones.");
        }

        var low = boardSize == 9 ? 2 : 3;
        var high = boardSize - low - 1;
        var middle = boardSize / 2;
        var lowerLeft = new GoPoint(low, high);
        var upperRight = new GoPoint(high, low);
        var upperLeft = new GoPoint(low, low);
        var lowerRight = new GoPoint(high, high);
        var middleLeft = new GoPoint(low, middle);
        var middleRight = new GoPoint(high, middle);
        var upperMiddle = new GoPoint(middle, low);
        var lowerMiddle = new GoPoint(middle, high);
        var center = new GoPoint(middle, middle);

        return stoneCount switch
        {
            2 => [lowerLeft, upperRight],
            3 => [lowerLeft, upperRight, upperLeft],
            4 => [lowerLeft, upperRight, upperLeft, lowerRight],
            5 => [lowerLeft, upperRight, upperLeft, lowerRight, center],
            6 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight],
            7 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, center],
            8 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, upperMiddle, lowerMiddle],
            9 => [lowerLeft, upperRight, upperLeft, lowerRight, middleLeft, middleRight, upperMiddle, lowerMiddle, center],
            _ => throw new InvalidOperationException("Unexpected fixed handicap stone count."),
        };
    }
}
