namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Intermission;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Application.GoApps.Casual.Ponnuki;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using System;

/// <summary>ローカルアプリ対局の休憩ページを描画します。</summary>
public sealed class LocalMatchIntermissionPage
{
    public static LocalMatchIntermissionPage Default { get; } = new();

    private LocalMatchIntermissionPage()
    {
        ChangeAppProviderButton = new Button(new Rectangle(1658, 556, 154, 52), "CHANGE", 0.28f);
        AppProviderGameSettingsButton = new Button(new Rectangle(1328, 556, 320, 52), "GAME SETTINGS", 0.32f);
        ProviderSeedLink = CreateSeedLink(new Rectangle(1248, 870, 116, 32));
        Player1SeedLink = CreateSeedLink(new Rectangle(1408, 870, 170, 32));
        Player2SeedLink = CreateSeedLink(new Rectangle(1622, 870, 170, 32));
    }

    public Button ChangeAppProviderButton { get; }
    public Button AppProviderGameSettingsButton { get; }
    public LinkUnderline ProviderSeedLink { get; }
    public LinkUnderline Player1SeedLink { get; }
    public LinkUnderline Player2SeedLink { get; }
    public LocalMatchIntermissionRightSidePanel RightSidePanel { get; } = new();

    public PonnukiRandomSeedRole? GetRandomSeedHit(Point point, GoAppSession session) =>
        session.SupportsPonnukiRandomSeed(PonnukiRandomSeedRole.Provider) && ProviderSeedLink.IsHit(point) ? PonnukiRandomSeedRole.Provider :
        session.SupportsPonnukiRandomSeed(PonnukiRandomSeedRole.Player1) && Player1SeedLink.IsHit(point) ? PonnukiRandomSeedRole.Player1 :
        session.SupportsPonnukiRandomSeed(PonnukiRandomSeedRole.Player2) && Player2SeedLink.IsHit(point) ? PonnukiRandomSeedRole.Player2 : null;

    private static LinkUnderline CreateSeedLink(Rectangle bounds)
    {
        var link = new LinkUnderline(new RoundUnderline()) { Bounds = bounds, Placeholder = "AUTO" };
        link.SetActionBadge(ActionBadgeComponent.Create("CHANGE", bounds, 0.24f));
        return link;
    }

    internal void DrawRightSidePanelContent(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        var renderer = drawingContext;
        var screen = LocalMatchScreen.Default;
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
        RightSidePanel.PlayerSelector.DrawPlayerRow(drawingContext, session, GoStone.Black, mousePoint, LocalMatchIntermissionRightSidePanel.BlackPlayerKindButtonY);
        RightSidePanel.PlayerSelector.DrawPlayerRow(drawingContext, session, GoStone.White, mousePoint, LocalMatchIntermissionRightSidePanel.WhitePlayerKindButtonY);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 856, 668, 52), "RANDOM SEED", new Color(112, 76, 48), labelWidth: 56);
        DrawSeedLink(drawingContext, session, mousePoint, PonnukiRandomSeedRole.Provider, ProviderSeedLink);
        DrawSeedLink(drawingContext, session, mousePoint, PonnukiRandomSeedRole.Player1, Player1SeedLink);
        DrawSeedLink(drawingContext, session, mousePoint, PonnukiRandomSeedRole.Player2, Player2SeedLink);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        screen.StartPlayingButton.Label = session.CanStartPlaying ? "START" : "ENGINE REQUIRED";
        screen.StartPlayingButton.LabelScale = session.CanStartPlaying ? 0.48f : 0.28f;
        screen.StartPlayingButton.IsEnabled = session.CanStartPlaying;
        screen.StartPlayingButton.Draw(mousePoint, drawingContext);
    }

    private static void DrawSeedLink(KfwStationeryDrawingTools drawingContext, GoAppSession session,
        Point mousePoint, PonnukiRandomSeedRole role, LinkUnderline link)
    {
        if (!session.SupportsPonnukiRandomSeed(role)) return;
        link.UpdatePointer(mousePoint);
        if (role == PonnukiRandomSeedRole.Provider)
            drawingContext.DrawFittedText("PROVIDER", new Rectangle(1164, 874, 76, 22), new Color(180, 195, 195), 0.22f);
        else
            drawingContext.DrawIconStone(new Vector2(link.Bounds.X - 24, link.Bounds.Center.Y), 12,
                role == PonnukiRandomSeedRole.Player1);
        drawingContext.DrawFittedText(link.GetDisplayText(session.GetPonnukiRandomSeedText(role)),
            new Rectangle(link.Bounds.X + 6, link.Bounds.Y + 2, Math.Max(1, link.Bounds.Width - 108), 26), Color.White, 0.28f);
        link.Draw(drawingContext);
    }
}
