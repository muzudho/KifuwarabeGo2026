namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;

using System;
using KifuwarabeGo2026.LobbyGui.Application;
using KifuwarabeGo2026.LobbyGui.MonoGame;
using Microsoft.Xna.Framework;
using StationeryUI.MonoGame;

/// <summary>描画と入力が同じTitleScreenの座標・コントロールを使用するためのアダプター。</summary>
public sealed class TitleScreenLayoutAdapter : ILobbyPageLayout
{
    private readonly TitleScreen _screen = TitleScreen.Default;

    public Rectangle GetItemBounds(LobbyHomeTarget target) => target switch
    {
        LobbyHomeTarget.EngineProfiles => _screen.EngineProfilesButton.Bounds,
        LobbyHomeTarget.EntryProfiles => _screen.EntryProfilesButton.Bounds,
        LobbyHomeTarget.LocalMatch => _screen.LocalMatchButton.Bounds,
        LobbyHomeTarget.OnlineMatch => _screen.CgosClientButton.Bounds,
        LobbyHomeTarget.CaptureGame => _screen.CaptureGameButton.Bounds,
        LobbyHomeTarget.GamePlatform => _screen.GameOasisButton.Bounds,
        LobbyHomeTarget.ReferenceGo => _screen.GameOasisButton.Bounds,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Not a home item."),
    };

    public Rectangle GetSectionBounds(LobbyHomeTarget target) => target switch
    {
        LobbyHomeTarget.EntrySettings => _screen.EntrySettingsLabelBounds,
        LobbyHomeTarget.FormalApps => _screen.FormalAppsLabelBounds,
        LobbyHomeTarget.CasualApps => _screen.CasualAppsLabelBounds,
        LobbyHomeTarget.GamePlatform => _screen.GamePlatformLabelBounds,
        _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Not a home section."),
    };

    public Rectangle GetGameOasisItemBounds(int index) => TitleScreen.GetGameOasisPlaySpaceBounds(index);

    public void DrawHomeLabels(KfwStationeryDrawingTools drawingContext)
    {
        _screen.EntrySettingsLabel.Draw(drawingContext);
        _screen.FormalAppsLabel.Draw(drawingContext);
        _screen.CasualAppsLabel.Draw(drawingContext);
        _screen.GamePlatformLabel.Draw(drawingContext);
    }

    public void DrawBackButton(Point mousePoint, KfwStationeryDrawingTools drawingContext, bool focused)
    {
        _screen.BackButton.IsSelected = focused;
        _screen.BackButton.Draw(mousePoint, drawingContext);
    }
}
