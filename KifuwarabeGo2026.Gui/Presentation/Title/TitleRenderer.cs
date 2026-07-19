namespace KifuwarabeGo2026.Gui.Presentation.Title;

using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class TitleRenderer
{
    public static void Draw(GoScreenRenderer renderer, Point mousePosition) =>
        renderer.DrawUseSelection(mousePosition);

    public static bool IsLocalGameButtonHit(Point point) =>
        GoScreenRenderer.GetLocalUseButtonHit(point);

    public static bool IsCgosClientButtonHit(Point point) =>
        GoScreenRenderer.GetCgosUseButtonHit(point);
}
