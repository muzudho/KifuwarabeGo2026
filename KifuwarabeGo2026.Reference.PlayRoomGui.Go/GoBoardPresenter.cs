namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

public readonly record struct GoStoneVisual(
    GoPoint Intersection,
    GoBoardScreenPoint Center,
    GoStone Stone,
    float Radius,
    bool UseWhiteboardStyle);

public readonly record struct GoBoardMarkerVisual(
    GoPoint Intersection,
    GoBoardScreenPoint Center,
    float Radius);

/// <summary>囲碁盤Rendererへ渡す、描画フレームワーク非依存の描画要素です。</summary>
public sealed record GoBoardPresentation(
    IReadOnlyList<GoStoneVisual> Stones,
    GoBoardMarkerVisual? KoMarker);

/// <summary>囲碁Play Room表示状態を盤面の描画要素へ変換します。</summary>
public static class GoBoardPresenter
{
    public static GoBoardPresentation Create(GoPlayRoomViewState state, GoBoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.BoardSize != geometry.BoardSize)
            throw new ArgumentException("View state and geometry must use the same board size.", nameof(geometry));

        var useWhiteboardStyle = state.Activity == GoPlayRoomActivity.VariationEditing;
        var stoneRadius = geometry.Cell * (useWhiteboardStyle ? 0.4f : 0.44f);
        var stones = new List<GoStoneVisual>();
        for (var y = 0; y < state.BoardSize; y++)
        for (var x = 0; x < state.BoardSize; x++)
        {
            var stone = state.GetStone(x, y);
            if (stone == GoStone.Empty)
                continue;

            var intersection = new GoPoint(x, y);
            stones.Add(new GoStoneVisual(
                intersection,
                geometry.GetScreenPoint(intersection),
                stone,
                stoneRadius,
                useWhiteboardStyle));
        }

        GoBoardMarkerVisual? koMarker = null;
        if (state.KoPoint is { } ko)
            koMarker = new GoBoardMarkerVisual(
                ko,
                geometry.GetScreenPoint(ko),
                Math.Max(12f, geometry.Cell * 0.26f));

        return new GoBoardPresentation(stones, koMarker);
    }
}
