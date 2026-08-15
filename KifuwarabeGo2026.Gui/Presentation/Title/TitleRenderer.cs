namespace KifuwarabeGo2026.Gui.Presentation.Title;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Pages.ApplicationSettings;
using Microsoft.Xna.Framework;

public static class TitleRenderer
{
    public static void Draw(StationeryDrawingContext drawingContext, GoAppSession session, Point mousePosition, TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading) =>
        drawingContext.ScreenRenderer.DrawUseSelection(session, mousePosition, page, appProviderTabIndex, isAppProviderLoading);

    public static bool IsLocalGameButtonHit(Point point) =>
        TitleScreen.Default.LocalMatchButton.IsHit(point);

    public static bool IsCgosClientButtonHit(Point point) =>
        TitleScreen.Default.CgosClientButton.IsHit(point);

    public static int? GetAppHit(Point point) =>
        TitleScreen.Default.GetAppHit(point);

    public static bool IsBackButtonHit(Point point) =>
        TitleScreen.Default.BackButton.IsHit(point);

    public static bool IsAppProviderEngineSelectButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.IsProviderLinkHit(point);

    public static bool IsAppProviderStartButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.StartButton.IsHit(point);

    public static bool IsAppProviderRecheckButtonHit(Point point) =>
        PonnukiProviderSelectionScreen.Default.RecheckButton.IsHit(point);

    public static bool IsSettingsButtonHit(Point point) =>
        ApplicationSettingsScreen.Default.SettingsButton.IsHit(point);

    public static bool IsUpdateButtonHit(Point point) =>
        ApplicationSettingsScreen.Default.UpdateButton.IsHit(point);
}
