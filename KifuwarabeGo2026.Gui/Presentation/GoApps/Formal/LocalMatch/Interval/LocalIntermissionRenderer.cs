namespace KifuwarabeGo2026.Gui.Presentation.GoApps.Formal.LocalMatch.Interval;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

/// <summary>
/// ローカルプレイとローカルアップスが共有する幕間画面の描画入口です。
/// </summary>
public static class LocalIntermissionRenderer
{
    public static void Draw(
        StationeryDrawingContext drawingContext,
        GoAppSession session,
        Point mousePosition,
        LiveBoardPreview? liveBoardPreview = null,
        InitialPositionConciergeView? initialPositionConcierge = null) =>
        drawingContext.ScreenRenderer.Draw(session, mousePosition, liveBoardPreview, initialPositionConcierge);
}
