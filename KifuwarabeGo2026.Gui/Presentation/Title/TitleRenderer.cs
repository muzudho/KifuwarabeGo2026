namespace KifuwarabeGo2026.Gui.Presentation.Title;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class TitleRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition, TitleMenuPage page) =>
        renderer.DrawUseSelection(session, mousePosition, page);

    public static bool IsLocalGameButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeLocalButtonHit(point);

    public static bool IsCgosClientButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeCgosButtonHit(point);

    public static int? GetAppHit(Point point) =>
        GoScreenRenderer.GetTitleAppHit(point);

    public static bool IsBackButtonHit(Point point) =>
        GoScreenRenderer.GetTitleMenuBackButtonHit(point);

    public static int? GetAppProviderEngineHit(Point point, int engineCount) =>
        GoScreenRenderer.GetTitleAppProviderEngineHit(point, engineCount);

    public static bool IsAppProviderStartButtonHit(Point point) =>
        GoScreenRenderer.GetTitleAppProviderStartButtonHit(point);

    public static bool IsAppProviderRecheckButtonHit(Point point) =>
        GoScreenRenderer.GetTitleAppProviderRecheckButtonHit(point);

    public static bool IsSettingsButtonHit(Point point) =>
        GoScreenRenderer.GetSettingsButtonHit(point);
}
