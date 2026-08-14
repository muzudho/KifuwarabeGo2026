namespace KifuwarabeGo2026.Gui.Presentation.Title;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using Microsoft.Xna.Framework;

public static class TitleRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition, TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading) =>
        renderer.DrawUseSelection(session, mousePosition, page, appProviderTabIndex, isAppProviderLoading);

    public static bool IsLocalGameButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeLocalButtonHit(point);

    public static bool IsCgosClientButtonHit(Point point) =>
        GoScreenRenderer.GetTitleHomeCgosButtonHit(point);

    public static int? GetAppHit(Point point) =>
        GoScreenRenderer.GetTitleAppHit(point);

    public static bool IsBackButtonHit(Point point) =>
        TitleScreen.Default.BackButton.IsHit(point);

    public static bool IsAppProviderEngineSelectButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.IsProviderLinkHit(point);

    public static bool IsAppProviderStartButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.StartButton.IsHit(point);

    public static bool IsAppProviderRecheckButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.RecheckButton.IsHit(point);

    public static bool IsSettingsButtonHit(Point point) =>
        GoScreenRenderer.GetSettingsButtonHit(point);

    public static bool IsUpdateButtonHit(Point point) =>
        GoScreenRenderer.GetUpdateButtonHit(point);
}
