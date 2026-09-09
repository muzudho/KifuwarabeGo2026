namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;

using KifuwarabeGo2026.LobbyGui.Application;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.LobbyGui.MonoGame;
using StationeryUI.MonoGame;

public sealed class TitleScreenRenderer
{
    private readonly TitleScreenShellRenderer _shellRenderer;
    private readonly LobbyPageRenderer _pageRenderer = new(new TitleScreenLayoutAdapter());

    public TitleScreenRenderer(
        Action<Vector2, float, float, Color, int, float> drawEllipseWire,
        Action<Vector2, float, float, Color, int, float, float, float> drawCircumscribedCircleArc)
    {
        _shellRenderer = new TitleScreenShellRenderer(drawEllipseWire, drawCircumscribedCircleArc);
    }

    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        LobbyScreenPresentation lobby, Action drawProviderSelection)
    {
        var panel = _shellRenderer.DrawFrame(drawingContext);
        _pageRenderer.Draw(lobby, drawingContext, panel, mousePoint,
            _shellRenderer.SettingsHintConnectorTarget, drawProviderSelection);
        _shellRenderer.DrawControls(drawingContext, mousePoint,
            lobby.CurrentPage == LobbyPage.Home,
            connectorTarget => _pageRenderer.DrawHomeHint(
                lobby.Home.GetHint(LobbyHomeTarget.Settings), connectorTarget));
    }

}
