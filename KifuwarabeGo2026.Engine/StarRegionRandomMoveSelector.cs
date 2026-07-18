namespace KifuwarabeGo2026.Engine;

using KifuwarabeGo2026.Domain;

/// <summary>
/// 星を基準に盤面を A・B・C の領域へ分け、領域と着手点を順番に抽選します。
/// </summary>
internal static class StarRegionRandomMoveSelector
{
    /// <summary>
    /// 合法手が存在する領域を等確率で選び、その領域から着手点を選びます。
    /// </summary>
    public static GoPoint Select(IReadOnlyList<GoPoint> legalMoves, int boardSize, Random random)
    {
        if (legalMoves.Count == 0) throw new ArgumentException("At least one legal move is required.", nameof(legalMoves));

        var movesByRegion = new List<GoPoint>[3]
        {
            [],
            [],
            [],
        };
        foreach (var move in legalMoves)
            movesByRegion[(int)GetRegion(move, boardSize)].Add(move);

        var availableRegions = new List<List<GoPoint>>(3);
        foreach (var moves in movesByRegion)
        {
            if (moves.Count > 0) availableRegions.Add(moves);
        }

        var selectedRegion = availableRegions[random.Next(availableRegions.Count)];
        return selectedRegion[random.Next(selectedRegion.Count)];
    }

    /// <summary>
    /// 星を A、星からチェビシェフ距離1を B、それ以外を C と判定します。
    /// </summary>
    private static StarRegion GetRegion(GoPoint point, int boardSize)
    {
        var stars = GetStarPoints(boardSize);
        foreach (var star in stars)
        {
            var distance = Math.Max(Math.Abs(point.X - star.X), Math.Abs(point.Y - star.Y));
            if (distance == 0) return StarRegion.A;
        }

        foreach (var star in stars)
        {
            var distance = Math.Max(Math.Abs(point.X - star.X), Math.Abs(point.Y - star.Y));
            if (distance == 1) return StarRegion.B;
        }

        return StarRegion.C;
    }

    /// <summary>
    /// スターランダムで使用する四隅側の星を取得します。
    /// </summary>
    private static GoPoint[] GetStarPoints(int boardSize)
    {
        var offset = boardSize == 9 ? 2 : 3;
        var opposite = boardSize - 1 - offset;
        return
        [
            new GoPoint(offset, offset),
            new GoPoint(opposite, offset),
            new GoPoint(offset, opposite),
            new GoPoint(opposite, opposite),
        ];
    }

    private enum StarRegion
    {
        A,
        B,
        C,
    }
}
