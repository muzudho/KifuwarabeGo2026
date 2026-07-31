namespace KifuwarabeGo2026.Gui.Presentation.Local.Resting;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class LocalRestingRenderer
{
    public static void Draw(
        GoScreenRenderer renderer,
        GoAppSession session,
        Point mousePosition,
        LiveBoardPreview? liveBoardPreview = null,
        InitialPositionConciergeView? initialPositionConcierge = null) =>
        renderer.Draw(session, mousePosition, liveBoardPreview, initialPositionConcierge);
}
