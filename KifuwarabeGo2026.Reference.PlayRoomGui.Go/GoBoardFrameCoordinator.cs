namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>1フレーム分の通常盤面描画要素をまとめます。</summary>
public sealed record GoBoardFramePresentation(
    GoBoardGeometry Geometry,
    GoBoardPresentation Board,
    GoHoverStoneVisual? Hover);

/// <summary>表示状態、盤面幾何、禁止点を通常盤面の1フレームへ組み立てます。</summary>
public static class GoBoardFrameCoordinator
{
    public static GoBoardFramePresentation Create(
        GoPlayRoomViewState state,
        GoBoardGeometry geometry,
        IEnumerable<GoPoint>? superKoPoints = null,
        GoBoardScreenPoint? pointer = null,
        bool canAcceptHumanMove = false,
        Func<GoPoint, bool>? isForbidden = null)
    {
        var board = GoBoardPresenter.Create(state, geometry, superKoPoints);
        GoHoverStoneVisual? hover = null;
        if (pointer is { } screenPoint &&
            GoBoardPresenter.TryCreateMoveHover(
                state,
                geometry,
                screenPoint,
                canAcceptHumanMove,
                isForbidden,
                out var hoverVisual))
        {
            hover = hoverVisual;
        }

        return new GoBoardFramePresentation(geometry, board, hover);
    }
}
