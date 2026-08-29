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
    float Radius,
    float Cell);

public readonly record struct GoHoverStoneVisual(
    GoPoint Intersection,
    GoBoardScreenPoint Center,
    GoStone Stone,
    float OuterRadius,
    float InnerRadius);

public readonly record struct GoSuperKoMarkerVisual(
    GoPoint Intersection,
    GoBoardScreenPoint Center,
    float Radius,
    string Label,
    float LabelScale);

/// <summary>囲碁盤Rendererへ渡す、描画フレームワーク非依存の描画要素です。</summary>
public sealed record GoBoardPresentation(
    IReadOnlyList<GoStoneVisual> Stones,
    GoBoardMarkerVisual? KoMarker,
    GoBoardMarkerVisual? LastMoveMarker,
    IReadOnlyList<GoSuperKoMarkerVisual> SuperKoMarkers);

/// <summary>囲碁Play Room表示状態を盤面の描画要素へ変換します。</summary>
public static class GoBoardPresenter
{
    public static GoBoardPresentation Create(
        GoPlayRoomViewState state,
        GoBoardGeometry geometry,
        IEnumerable<GoPoint>? superKoPoints = null)
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
                Math.Max(12f, geometry.Cell * 0.26f),
                geometry.Cell);

        GoBoardMarkerVisual? lastMoveMarker = null;
        if (state.LastMovePoint is { } lastMove)
            lastMoveMarker = new GoBoardMarkerVisual(
                lastMove,
                geometry.GetScreenPoint(lastMove),
                Math.Max(9f, geometry.Cell * 0.19f),
                geometry.Cell);

        var superKoMarkers = (superKoPoints ?? [])
            .Select(point => new GoSuperKoMarkerVisual(
                point,
                geometry.GetScreenPoint(point),
                Math.Max(15f, geometry.Cell * 0.32f),
                "S-KO",
                geometry.Cell < 55f ? 0.24f : 0.3f))
            .ToArray();

        return new GoBoardPresentation(stones, koMarker, lastMoveMarker, superKoMarkers);
    }

    public static bool TryCreateMoveHover(
        GoPlayRoomViewState state,
        GoBoardGeometry geometry,
        GoBoardScreenPoint pointer,
        bool canAcceptHumanMove,
        Func<GoPoint, bool>? isForbidden,
        out GoHoverStoneVisual visual)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.BoardSize != geometry.BoardSize)
            throw new ArgumentException("View state and geometry must use the same board size.", nameof(geometry));

        if (!canAcceptHumanMove ||
            state.Activity is not (GoPlayRoomActivity.Playing or GoPlayRoomActivity.VariationEditing) ||
            !geometry.TryGetIntersection(pointer, out var intersection) ||
            state.GetStone(intersection.X, intersection.Y) != GoStone.Empty ||
            state.KoPoint == intersection ||
            isForbidden?.Invoke(intersection) == true)
        {
            visual = default;
            return false;
        }

        visual = new GoHoverStoneVisual(
            intersection,
            geometry.GetScreenPoint(intersection),
            state.CurrentTurn,
            geometry.Cell * 0.55f,
            geometry.Cell * 0.36f);
        return true;
    }
}
