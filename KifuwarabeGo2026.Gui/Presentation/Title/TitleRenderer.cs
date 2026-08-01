namespace KifuwarabeGo2026.Gui.Presentation.Title;

using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class TitleRenderer
{
    public static void Draw(GoScreenRenderer renderer, Point mousePosition, TitleMenuPage page) =>
        renderer.DrawUseSelection(mousePosition, page);

    public static bool IsLocalGameButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeLocalButtonHit(point);

    public static bool IsCgosClientButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeCgosButtonHit(point);

    public static int? GetAppHit(Point point) =>
        GoScreenRenderer.GetTitleAppHit(point);

    public static bool IsBackButtonHit(Point point) =>
        GoScreenRenderer.GetTitleMenuBackButtonHit(point);

    public static bool IsSettingsButtonHit(Point point) =>
        GoScreenRenderer.GetSettingsButtonHit(point);
}
