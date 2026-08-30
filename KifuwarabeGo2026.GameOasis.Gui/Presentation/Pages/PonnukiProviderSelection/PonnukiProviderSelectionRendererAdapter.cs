namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PonnukiProviderSelection;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

/// <summary>互換Host固有のセッション状態をポン抜きProvider選択画面へ接続します。</summary>
public sealed class PonnukiProviderSelectionRendererAdapter
{
    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint,
        int activeTabIndex, bool isProviderLoading)
    {
        PonnukiProviderSelectionScreen.Default.Draw(session, mousePoint, activeTabIndex, isProviderLoading,
            new PonnukiProviderSelectionDrawingCallbacks(
                drawingContext,
                drawingContext,
                drawingContext,
                drawingContext.DrawText,
                drawingContext.DrawDynamicText,
                drawingContext.DrawFittedText,
                drawingContext.DrawLine,
                (kind, connectorStart, accent, borderColor, heading, bodyLines) =>
                    drawingContext.DrawStickyNote(
                        kind, connectorStart, accent, borderColor, heading, bodyLines),
                (bounds, tabIndex, selectedIndex, stopCount) =>
                    DrawTabNavigationHint(drawingContext, bounds, tabIndex, selectedIndex, stopCount)));
    }

    private static void DrawTabNavigationHint(KfwStationeryDrawingTools drawingContext,
        Rectangle bounds, int tabIndex, int activeIndex, int stopCount)
    {
        if (activeIndex < 0 || tabIndex == activeIndex || stopCount < 2) return;
        var previous = tabIndex == (activeIndex + stopCount - 1) % stopCount;
        var next = tabIndex == (activeIndex + 1) % stopCount;
        if (!previous && !next) return;
        var text = previous ? "SHIFT + TAB" : "TAB";
        var width = previous ? 132 : 56;
        var hint = new Rectangle(bounds.X - width - 6, bounds.Y - 34, width, 28);
        drawingContext.FillRoundedRectangle(hint, 6, new Color(4, 6, 8, 235));
        drawingContext.DrawFittedText(text,
            new Rectangle(hint.X + 4, hint.Y + 2, hint.Width - 8, hint.Height - 4), Color.White, 0.32f);
    }
}
