namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    private void DrawUseSelectionPanel(GoAppSession session, Point mousePoint, TitleMenuPage page)
    {
        // タイトル画面の囲碁用具ワイヤー装飾。
        DrawTitleGoEquipment();

        var panel = new Rectangle(420, 172, 1080, 736);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText("KIFUWARABE GO 2026", new Vector2(panel.X + 58, panel.Y + 58), new Color(244, 238, 218), 1.05f);
        DrawText(GetDisplayVersion(), new Vector2(panel.X + 790, panel.Y + 91), new Color(99, 223, 185), 0.38f);
        DrawLine(new Vector2(panel.X + 790, panel.Y + 126), new Vector2(panel.X + 958, panel.Y + 126), 2, new Color(99, 223, 185, 120));
        DrawTitleMenuContent(session, page, panel, mousePoint);
        DrawSettingsButton(mousePoint);
    }

    private void DrawTitleMenuContent(GoAppSession session, TitleMenuPage page, Rectangle panel, Point mousePoint)
    {
        switch (page)
        {
            case TitleMenuPage.Home:
                DrawText("GO PLAY", new Vector2(500, 338), new Color(99, 223, 185), 0.58f);
                DrawText("GO APPS", new Vector2(950, 338), new Color(255, 190, 92), 0.58f);
                DrawHomeServiceChoice(TitleHomeLocalButtonBounds, "Local", "PLAY / REVIEW", new Color(99, 223, 185), mousePoint);
                DrawHomeServiceChoice(TitleHomeCgosButtonBounds, "Connect To CGOS", "WATCH / CONNECT", new Color(99, 223, 185), mousePoint);
                DrawAppChoice(TitleAppBounds(0), "ポン抜きゲーム", "CAPTURE GAME", mousePoint);
                DrawDynamicOptionText("対局、観戦、問題演習をここから直接選べます。", new Rectangle(500, 700, 890, 38), new Color(180, 195, 195), 0.34f);
                if (TitleAppBounds(0).Contains(mousePoint))
                {
                    DrawCaptureGamePreview();
                }
                break;
            default:
                DrawAppPage(session, page, panel, mousePoint);
                break;
        }
    }

    private void DrawHomeServiceChoice(Rectangle bounds, string title, string caption, Color accent, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(new Rectangle(bounds.X + 6, bounds.Y + 8, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(88, 102, 112));
        FillRect(new Rectangle(bounds.X, bounds.Y, 6, bounds.Height), hovered ? accent : new Color(accent.R, accent.G, accent.B, (byte)100));
        DrawText(title, new Vector2(bounds.X + 28, bounds.Y + 24), Color.White, 0.52f);
        DrawFittedText(caption, new Rectangle(bounds.X + 28, bounds.Y + 74, bounds.Width - 100, 30), new Color(204, 241, 226), 0.34f);
        DrawFittedText("OPEN  >", new Rectangle(bounds.Right - 92, bounds.Y + 46, 68, 28), hovered ? accent : new Color(180, 195, 195), 0.28f);
    }

    private void DrawTitleBreadcrumb(string text, Rectangle panel)
    {
        DrawText(text, new Vector2(panel.X + 62, panel.Y + 142), new Color(180, 195, 195), 0.46f);
        DrawLine(new Vector2(panel.X + 62, panel.Y + 184), new Vector2(panel.Right - 62, panel.Y + 184), 1, new Color(82, 111, 114));
    }

    private void DrawAppChoice(Rectangle bounds, string title, string caption, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(new Rectangle(bounds.X + 7, bounds.Y + 9, bounds.Width, bounds.Height), new Color(0, 0, 0, 90));
        FillRect(bounds, hovered ? new Color(42, 55, 63) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(255, 214, 132) : new Color(88, 102, 112));
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 7), hovered ? new Color(255, 190, 92) : new Color(99, 76, 48));
        DrawDynamicOptionText(title, new Rectangle(bounds.X + 18, bounds.Y + 12, 250, 38), Color.White, 0.43f);
        DrawFittedText(caption, new Rectangle(bounds.X + 18, bounds.Y + 52, 260, 22), new Color(255, 221, 164), 0.27f);
        DrawFittedText("OPEN  >", new Rectangle(bounds.Right - 92, bounds.Y + 28, 68, 28), new Color(180, 195, 195), 0.28f);
    }

    /// <summary>
    /// ［ポン抜きゲーム］へカーソルを合わせたときの紹介ポップアップ
    /// </summary>
    private void DrawCaptureGamePreview()
    {
        var bounds = new Rectangle(1412, 390, 420, 174);
        var accent = new Color(255, 190, 92);

        FillRect(new Rectangle(bounds.X + 9, bounds.Y + 11, bounds.Width, bounds.Height), new Color(0, 0, 0, 115));
        FillRect(bounds, new Color(19, 25, 30, 248));
        DrawRect(bounds, 2, new Color(142, 105, 57));
        FillRect(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), accent);
        DrawLine(new Vector2(1390, 432), new Vector2(bounds.X, 432), 2, accent);

        DrawDynamicOptionText(
            "ポン抜きゲームとは？",
            new Rectangle(bounds.X + 26, bounds.Y + 20, bounds.Width - 52, 38),
            accent,
            0.40f);
        DrawDynamicOptionText(
            "とにかく相手よりアゲハマを",
            new Rectangle(bounds.X + 26, bounds.Y + 73, bounds.Width - 52, 32),
            new Color(235, 238, 229),
            0.34f);
        DrawDynamicOptionText(
            "多く取った方が勝ち！",
            new Rectangle(bounds.X + 26, bounds.Y + 113, bounds.Width - 52, 32),
            Color.White,
            0.38f);
    }

    private void DrawAppPage(GoAppSession session, TitleMenuPage page, Rectangle panel, Point mousePoint)
    {
        if (page == TitleMenuPage.CaptureGame)
        {
            DrawPonnukiProviderSelection(session, panel, mousePoint);
            return;
        }

        var (title, caption) = page switch
        {
            TitleMenuPage.CaptureGame => ("ポン抜きゲーム", "CAPTURE GAME"),
            TitleMenuPage.Tsumego => ("詰碁", "LIFE & DEATH"),
            _ => ("次の一手問題", "NEXT MOVE"),
        };
        DrawTitleBreadcrumb($"HOME  >  GO APPS  >  {caption}", panel);
        DrawDynamicOptionText(title, new Rectangle(panel.X + 150, panel.Y + 280, panel.Width - 300, 92), Color.White, 0.84f);
        DrawFittedText("COMING SOON", new Rectangle(panel.X + 250, panel.Y + 430, panel.Width - 500, 70), new Color(99, 223, 185), 0.72f);
        DrawDynamicOptionText("問題集と問題一覧は、ここからディレクトリーのように開いていく予定です。", new Rectangle(panel.X + 150, panel.Y + 530, panel.Width - 300, 54), new Color(180, 195, 195), 0.38f);
        DrawTitleBackButton(mousePoint);
    }

    private void DrawPonnukiProviderSelection(GoAppSession session, Rectangle panel, Point mousePoint)
    {
        DrawTitleBreadcrumb("HOME  >  GO APPS  >  PONNUKI", panel);
        DrawDynamicOptionText("ポン抜きゲーム", new Rectangle(500, 350, 500, 54), Color.White, 0.62f);
        DrawText("APP PROVIDER ENGINE", new Vector2(530, 416), new Color(255, 190, 92), 0.42f);
        DrawDynamicOptionText("問題提供エンジン", new Rectangle(950, 414, 330, 34), new Color(210, 214, 207), 0.32f);

        for (var index = 0; index < Math.Min(session.GtpEngineProfiles.Count, 5); index++)
        {
            var bounds = TitleAppProviderEngineBounds(index);
            var selected = index == session.SelectedAppProviderEngineIndex;
            var hovered = bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(58, 66, 51) : hovered ? new Color(42, 55, 63) : new Color(24, 31, 37));
            DrawRect(bounds, selected ? 3 : 2, selected ? new Color(255, 190, 92) : new Color(88, 102, 112));
            DrawFittedText(session.GtpEngineProfiles[index].DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 13, 560, 30), Color.White, 0.34f);
            DrawFittedText(selected ? "SELECTED" : "SELECT", new Rectangle(bounds.Right - 130, bounds.Y + 14, 104, 28), selected ? new Color(255, 210, 128) : new Color(180, 195, 195), 0.27f);
        }

        var capabilityColor = session.IsAppProviderCapabilityConfirmed
            ? new Color(99, 223, 185)
            : session.AppProviderCapabilityStatus == "NOT CHECKED"
                ? new Color(180, 195, 195)
                : new Color(255, 145, 151);
        DrawFittedText(
            session.AppProviderCapabilityStatus,
            new Rectangle(570, 794, 780, 26),
            capabilityColor,
            0.30f);
        DrawCommandButton(
            TitleAppProviderRecheckButtonBounds,
            "RECHECK PROVIDER",
            false,
            mousePoint,
            enabled: session.CanUseSelectedAppProvider,
            scale: 0.30f);
        DrawCommandButton(
            TitleAppProviderStartButtonBounds,
            session.CanUseSelectedAppProvider ? "START" : "ENGINE REQUIRED",
            false,
            mousePoint,
            enabled: session.CanUseSelectedAppProvider,
            scale: session.CanUseSelectedAppProvider ? 0.40f : 0.23f);
        DrawTitleBackButton(mousePoint);
    }

    private void DrawTitleBackButton(Point mousePoint) =>
        DrawCommandButton(TitleMenuBackButtonBounds, "BACK", false, mousePoint, scale: 0.36f);

    private static string GetDisplayVersion()
    {
        var version = typeof(GoScreenRenderer).Assembly.GetName().Version;
        return version is null ? "VERSION" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }
}
