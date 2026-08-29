namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

public readonly record struct GoBoardLineVisual(GoBoardScreenPoint Start, GoBoardScreenPoint End, bool IsOuter);

public readonly record struct GoBoardStarVisual(GoPoint Intersection, GoBoardScreenPoint Center, float Radius);

public readonly record struct GoBoardCoordinateVisual(string Text, GoBoardScreenPoint Center, float Scale, bool IsColumn);

/// <summary>囲碁盤の罫線、星、座標ラベルをGUIフレームワーク非依存の描画要素へ変換します。</summary>
public sealed record GoBoardStaticPresentation(
    IReadOnlyList<GoBoardLineVisual> Lines,
    IReadOnlyList<GoBoardStarVisual> Stars,
    IReadOnlyList<GoBoardCoordinateVisual> Coordinates);

public static class GoBoardStaticPresenter
{
    private const string ColumnLabels = "ABCDEFGHJKLMNOPQRSTUVWXYZ";

    public static GoBoardStaticPresentation Create(GoBoardGeometry geometry, GoBoardViewport outerViewport)
    {
        if (outerViewport.Width <= 0 || outerViewport.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(outerViewport), outerViewport, "Outer viewport must have positive dimensions.");

        var end = geometry.GetScreenPoint(new GoPoint(geometry.BoardSize - 1, geometry.BoardSize - 1));
        var lines = new List<GoBoardLineVisual>(geometry.BoardSize * 2);
        for (var index = 0; index < geometry.BoardSize; index++)
        {
            var point = geometry.GetScreenPoint(new GoPoint(index, index));
            var isOuter = index == 0 || index == geometry.BoardSize - 1;
            lines.Add(new GoBoardLineVisual(new(point.X, geometry.Start.Y), new(point.X, end.Y), isOuter));
            lines.Add(new GoBoardLineVisual(new(geometry.Start.X, point.Y), new(end.X, point.Y), isOuter));
        }

        var stars = CreateStarPoints(geometry.BoardSize)
            .Select(point => new GoBoardStarVisual(point, geometry.GetScreenPoint(point), Math.Max(5f, geometry.Cell * 0.1f)))
            .ToArray();

        var scale = geometry.BoardSize >= 19 ? 0.34f : geometry.BoardSize >= 13 ? 0.38f : 0.42f;
        var bottomY = outerViewport.Y + outerViewport.Height - 40f;
        var leftX = outerViewport.X + 50f;
        var coordinates = new List<GoBoardCoordinateVisual>(geometry.BoardSize * 2);
        for (var index = 0; index < geometry.BoardSize; index++)
        {
            var point = geometry.GetScreenPoint(new GoPoint(index, index));
            coordinates.Add(new GoBoardCoordinateVisual(GetColumnLabel(index), new(point.X, bottomY), scale, true));
            coordinates.Add(new GoBoardCoordinateVisual((geometry.BoardSize - index).ToString(), new(leftX, point.Y), scale, false));
        }

        return new GoBoardStaticPresentation(lines, stars, coordinates);
    }

    private static string GetColumnLabel(int zeroBasedColumn) =>
        zeroBasedColumn >= 0 && zeroBasedColumn < ColumnLabels.Length
            ? ColumnLabels[zeroBasedColumn].ToString()
            : "?";

    private static IReadOnlyList<GoPoint> CreateStarPoints(int boardSize) => boardSize switch
    {
        9 => [new(2, 2), new(6, 2), new(4, 4), new(2, 6), new(6, 6)],
        13 => [new(3, 3), new(9, 3), new(6, 6), new(3, 9), new(9, 9)],
        >= 19 => [new(3, 3), new(9, 3), new(15, 3), new(3, 9), new(9, 9), new(15, 9), new(3, 15), new(9, 15), new(15, 15)],
        _ => [],
    };
}
