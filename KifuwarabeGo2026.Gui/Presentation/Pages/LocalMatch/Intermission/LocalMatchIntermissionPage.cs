namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Intermission;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Application.GoApps.Casual.Ponnuki;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

/// <summary>ローカルアプリ対局の休憩ページを描画します。</summary>
public sealed class LocalMatchIntermissionPage
{
    public static LocalMatchIntermissionPage Default { get; } = new();

    private LocalMatchIntermissionPage()
    {
        ChangeAppProviderButton = new Button(new Rectangle(1658, 556, 154, 52), "CHANGE", 0.28f);
        AppProviderGameSettingsButton = new Button(new Rectangle(1328, 556, 320, 52), "GAME SETTINGS", 0.32f);
        ProviderSeedAutoChangeButton = new Button(new Rectangle(1164, 870, 200, 32), "PROVIDER", 0.22f);
        Player1SeedAutoChangeButton = new Button(new Rectangle(1378, 870, 200, 32), "BLACK", 0.22f);
        Player2SeedAutoChangeButton = new Button(new Rectangle(1592, 870, 200, 32), "WHITE", 0.22f);
    }

    public Button ChangeAppProviderButton { get; }
    public Button AppProviderGameSettingsButton { get; }
    public Button ProviderSeedAutoChangeButton { get; }
    public Button Player1SeedAutoChangeButton { get; }
    public Button Player2SeedAutoChangeButton { get; }
    public LocalMatchIntermissionRightSidePanel RightSidePanel { get; } = new();

    public PonnukiRandomSeedRole? GetRandomSeedAutoChangeHit(Point point) =>
        ProviderSeedAutoChangeButton.IsHit(point) ? PonnukiRandomSeedRole.Provider :
        Player1SeedAutoChangeButton.IsHit(point) ? PonnukiRandomSeedRole.Player1 :
        Player2SeedAutoChangeButton.IsHit(point) ? PonnukiRandomSeedRole.Player2 : null;

    internal void DrawRightSidePanelContent(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        var screen = LocalMatchScreen.Default;
        var drawingContext = renderer.StationeryDrawingContext;
        screen.BackToTitleButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 184, 668, 176), "LOCAL APPS", new Color(99, 76, 48));
        renderer.DrawResultRow(new Rectangle(1164, 236, 628, 56), "APP", "PONNUKI", new Color(73, 57, 39), Color.White);
        renderer.DrawResultRow(
            new Rectangle(1164, 296, 628, 48),
            "STATUS",
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage) ? "INTERMISSION" : "PROVIDER ERROR",
            new Color(58, 48, 38),
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage) ? new Color(255, 210, 128) : new Color(255, 145, 151));

        renderer.DrawVerticalResultSection(new Rectangle(1144, 392, 668, 224), "APP PROVIDER ENGINE", new Color(66, 104, 116));
        renderer.DrawDynamicOptionText("アプリ提供エンジン", new Rectangle(1164, 410, 300, 34), new Color(180, 195, 195), 0.30f);
        renderer.DrawResultRow(new Rectangle(1164, 466, 628, 64), "PROVIDER", session.SelectedAppProviderEngineDisplayName,
            new Color(39, 68, 65), Color.White);
        renderer.DrawDynamicOptionText(
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage)
                ? "初期局面とポン抜きの進行を提供します。"
                : session.LocalAppsErrorMessage,
            new Rectangle(1164, 536, 628, 22), new Color(180, 195, 195), 0.30f);
        AppProviderGameSettingsButton.Draw(mousePoint, drawingContext);
        ChangeAppProviderButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 632, 668, 216), "PLAYERS", new Color(76, 91, 126));
        renderer.DrawSetupPlayerRow(session, GoStone.Black, mousePoint, LocalMatchIntermissionRightSidePanel.BlackPlayerKindButtonY);
        renderer.DrawSetupPlayerRow(session, GoStone.White, mousePoint, LocalMatchIntermissionRightSidePanel.WhitePlayerKindButtonY);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 856, 668, 52), "SEED AUTO", new Color(112, 76, 48), labelWidth: 56);
        ProviderSeedAutoChangeButton.Label = session.PonnukiProviderSeedAutoChange ? "[x] PROVIDER" : "[ ] PROVIDER";
        ProviderSeedAutoChangeButton.IsSelected = session.PonnukiProviderSeedAutoChange;
        ProviderSeedAutoChangeButton.Draw(mousePoint, drawingContext);
        Player1SeedAutoChangeButton.Label = session.PonnukiBlackPlayerSeedAutoChange ? "[x] BLACK" : "[ ] BLACK";
        Player1SeedAutoChangeButton.IsSelected = session.PonnukiBlackPlayerSeedAutoChange;
        Player1SeedAutoChangeButton.IsEnabled = session.CanAutoChangePonnukiPlayer1Seed;
        Player1SeedAutoChangeButton.Draw(mousePoint, drawingContext);
        Player2SeedAutoChangeButton.Label = session.PonnukiWhitePlayerSeedAutoChange ? "[x] WHITE" : "[ ] WHITE";
        Player2SeedAutoChangeButton.IsSelected = session.PonnukiWhitePlayerSeedAutoChange;
        Player2SeedAutoChangeButton.IsEnabled = session.CanAutoChangePonnukiPlayer2Seed;
        Player2SeedAutoChangeButton.Draw(mousePoint, drawingContext);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        screen.StartPlayingButton.Label = session.CanStartPlaying ? "START" : "ENGINE REQUIRED";
        screen.StartPlayingButton.LabelScale = session.CanStartPlaying ? 0.48f : 0.28f;
        screen.StartPlayingButton.IsEnabled = session.CanStartPlaying;
        screen.StartPlayingButton.Draw(mousePoint, drawingContext);
    }
}
