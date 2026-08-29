namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.Shared.RenBoundaries;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

/// <summary>
/// 連の各石から上下左右へ一つ隣にある、空点または相手石の交点を集めます。
/// 同じ交点は一つにまとめ、描画用の接触方向と隣接連番号も保持します。
/// </summary>
public sealed class RenBoundaryPoints
{
    public RenBoundaryPointSet Collect(GoRenParseResult renParse, GoRen ren, bool includeEmpty, bool includeOpponent)
    {
        var contacts = new List<(GoPoint From, GoPoint To)>();
        var points = new HashSet<GoPoint>();
        var directionSums = new Dictionary<GoPoint, Vector2>();
        var fallbackDirections = new Dictionary<GoPoint, Vector2>();
        var adjacentRenNumbers = new HashSet<int>();
        foreach (var point in ren.Points)
        {
            Add(point, point.X - 1, point.Y); Add(point, point.X + 1, point.Y);
            Add(point, point.X, point.Y - 1); Add(point, point.X, point.Y + 1);
        }
        return new RenBoundaryPointSet(contacts, points, directionSums, fallbackDirections, adjacentRenNumbers);

        void Add(GoPoint from, int x, int y)
        {
            if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size) return;
            var targetRenNumber = renParse.GetRenNumber(x, y);
            if (targetRenNumber == ren.Number) return;
            var targetRen = renParse.GetRen(targetRenNumber);
            var isEmpty = targetRen.Stone == GoStone.Empty;
            var isOpponent = targetRen.Stone != GoStone.Empty && targetRen.Stone != ren.Stone;
            if ((!isEmpty || !includeEmpty) && (!isOpponent || !includeOpponent)) return;
            var target = new GoPoint(x, y);
            contacts.Add((from, target)); points.Add(target);
            var direction = new Vector2(from.X - x, from.Y - y);
            directionSums[target] = directionSums.TryGetValue(target, out var sum) ? sum + direction : direction;
            fallbackDirections.TryAdd(target, direction);
            adjacentRenNumbers.Add(targetRenNumber);
        }
    }
}

public sealed record RenBoundaryPointSet(
    List<(GoPoint From, GoPoint To)> Contacts,
    HashSet<GoPoint> Points,
    Dictionary<GoPoint, Vector2> DirectionSums,
    Dictionary<GoPoint, Vector2> FallbackDirections,
    HashSet<int> AdjacentRenNumbers);
