namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.Shared.TitleBackground;
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;

public sealed partial class GoScreenRenderer
{
    private readonly TitleGoEquipment _titleGoEquipment = new();
    private static Rectangle TitleFormalAppsLabelBounds => new(480, 322, 310, 62);
    private static Rectangle TitleCasualAppsLabelBounds => new(930, 322, 310, 62);

    private void DrawUseSelectionPanel(GoAppSession session, Point mousePoint, TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading)
    {
        // タイトル画面の囲碁用具ワイヤー装飾。
        _titleGoEquipment.Draw(new TitleGoEquipmentDrawingCallbacks(DrawEllipseWire, DrawCircumscribedCircleArc));

        var panel = new Rectangle(420, 172, 1080, 736);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText("KIFUWARABE GO 2026", new Vector2(panel.X + 58, panel.Y + 58), new Color(244, 238, 218), 1.05f);
        DrawText(GetDisplayVersion(), new Vector2(panel.X + 790, panel.Y + 91), new Color(99, 223, 185), 0.38f);
        DrawLine(new Vector2(panel.X + 790, panel.Y + 126), new Vector2(panel.X + 958, panel.Y + 126), 2, new Color(99, 223, 185, 120));
        DrawTitleMenuContent(session, page, panel, mousePoint, appProviderTabIndex, isAppProviderLoading);
        DrawUpdateButton(mousePoint);
        DrawSettingsButton(mousePoint);
    }

    private void DrawTitleMenuContent(GoAppSession session, TitleMenuPage page, Rectangle panel, Point mousePoint, int appProviderTabIndex, bool isAppProviderLoading)
    {
        switch (page)
        {
            case TitleMenuPage.Home:
                var formalAppsHovered = TitleFormalAppsLabelBounds.Contains(mousePoint);
                var casualAppsHovered = TitleCasualAppsLabelBounds.Contains(mousePoint);
                var localMatchHovered = TitleHomeLocalButtonBounds.Contains(mousePoint);
                var onlineMatchHovered = TitleHomeCgosButtonBounds.Contains(mousePoint);
                var settingsHovered = SettingsButtonBounds.Contains(mousePoint);
                var updateHovered = UpdateButtonBounds.Contains(mousePoint);
                DrawText("FORMAL APPS", new Vector2(500, 338), new Color(99, 223, 185), 0.48f);
                DrawText("CASUAL APPS", new Vector2(950, 338), new Color(255, 190, 92), 0.48f);
                DrawHomeServiceChoice(TitleHomeLocalButtonBounds, "Local Match", "PLAY / REVIEW", new Color(99, 223, 185), mousePoint);
                DrawHomeServiceChoice(TitleHomeCgosButtonBounds, "Online Match (CGOS)", "WATCH / CONNECT", new Color(99, 223, 185), mousePoint);
                DrawAppChoice(TitleAppBounds(0), "ポン抜きゲーム", "CAPTURE GAME", mousePoint);
                DrawDynamicOptionText("対局、観戦、問題演習をここから直接選べます。", new Rectangle(500, 700, 890, 38), new Color(180, 195, 195), 0.34f);
                if (formalAppsHovered)
                    DrawTitleHomeHint("FORMAL APPS", "他のコンピュータ碁ソフトとできるだけ連携します！", new Color(99, 223, 185));
                else if (casualAppsHovered)
                    DrawTitleHomeHint("CASUAL APPS", "独自実装で機能追加を進めます！", new Color(255, 190, 92));
                else if (updateHovered)
                    DrawStickyNote(StickyNoteKind.TitleUpdateHint, new Vector2(UpdateButtonBounds.Left, UpdateButtonBounds.Center.Y), new Color(99, 223, 185), new Color(82, 111, 114), "このボタンは？", ["このGUIを最新バージョンに更新します。", "アプリを再起動します。"]);
                else if (settingsHovered)
                    DrawTitleHomeHint("SETTINGS", "アプリケーションを設定します！", new Color(147, 201, 190));
                else if (localMatchHovered)
                    DrawTitleHomeHint("LOCAL MATCH", "ローカルPCで、人間や碁エンジンが対局！ など。", new Color(99, 223, 185));
                else if (onlineMatchHovered)
                    DrawTitleHomeHint("ONLINE MATCH", "インターネット上の碁サーバーにお邪魔して碁エンジンが対局！", new Color(99, 223, 185));
                else if (TitleAppBounds(0).Contains(mousePoint))
                {
                    DrawCaptureGamePreview();
                }
                break;
            default:
                DrawAppPage(session, page, panel, mousePoint, appProviderTabIndex, isAppProviderLoading);
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
        DrawFittedText(title, new Rectangle(bounds.X + 28, bounds.Y + 20, bounds.Width - 124, 42), Color.White, 0.52f);
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
        var accent = new Color(255, 190, 92);
        DrawStickyNote(
            StickyNoteKind.TitlePonnukiPreview,
            new Vector2(1390, 432),
            accent,
            new Color(142, 105, 57),
            "ポン抜きゲームとは？",
            ["とにかく相手よりアゲハマを", "多く取った方が勝ち！"]);
#if false
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
#endif
    }

    private void DrawTitleHomeHint(string heading, string message, Color accent)
    {
        var (kind, target) = heading switch
        {
            "FORMAL APPS" =>
                (StickyNoteKind.TitleFormalAppsHint, GetTitleSectionLabelConnectorTarget("FORMAL APPS", new Vector2(500, 338), connectsToRight: false)),
            "CASUAL APPS" =>
                (StickyNoteKind.TitleCasualAppsHint, GetTitleSectionLabelConnectorTarget("CASUAL APPS", new Vector2(950, 338), connectsToRight: true)),
            "LOCAL MATCH" =>
                (StickyNoteKind.TitleLocalMatchHint, new Vector2(TitleHomeLocalButtonBounds.Left, TitleHomeLocalButtonBounds.Center.Y)),
            "ONLINE MATCH" =>
                (StickyNoteKind.TitleOnlineMatchHint, new Vector2(TitleHomeCgosButtonBounds.Left, TitleHomeCgosButtonBounds.Center.Y)),
            _ =>
                (StickyNoteKind.TitleSettingsHint, new Vector2(SettingsButtonBounds.Left - 14, SettingsButtonBounds.Center.Y)),
        };
        var bodyLines = heading switch
        {
            "FORMAL APPS" => new[] { "他のコンピュータ碁ソフトと", "できるだけ連携します！" },
            "CASUAL APPS" => new[] { "独自実装で", "機能追加を進めます！" },
            "LOCAL MATCH" => new[] { "ローカルPCで、人間や碁エンジンが", "対局！ など。" },
            "ONLINE MATCH" => new[] { "インターネット上の碁サーバーにお邪魔して", "碁エンジンが対局！" },
            _ => new[] { message },
        };
        DrawStickyNote(
            kind,
            target,
            accent,
            new Color(accent.R, accent.G, accent.B, (byte)190),
            $"{heading} とは？",
            bodyLines);
    }

    private Vector2 GetTitleSectionLabelConnectorTarget(string label, Vector2 labelPosition, bool connectsToRight)
    {
        const float labelScale = 0.48f;
        const int gap = 14;
        var x = connectsToRight
            ? labelPosition.X + _font.MeasureString(label).X * labelScale + gap
            : labelPosition.X - gap;
        return new Vector2(x, labelPosition.Y + 15);
    }

    private void DrawAppPage(GoAppSession session, TitleMenuPage page, Rectangle panel, Point mousePoint, int appProviderTabIndex, bool isAppProviderLoading)
    {
        if (page == TitleMenuPage.CaptureGame)
        {
            DrawPonnukiProviderSelection(session, panel, mousePoint, appProviderTabIndex, isAppProviderLoading);
            return;
        }

        var (title, caption) = page switch
        {
            TitleMenuPage.CaptureGame => ("ポン抜きゲーム", "CAPTURE GAME"),
            TitleMenuPage.Tsumego => ("詰碁", "LIFE & DEATH"),
            _ => ("次の一手問題", "NEXT MOVE"),
        };
        DrawTitleBreadcrumb($"HOME  >  CASUAL APPS  >  {caption}", panel);
        DrawDynamicOptionText(title, new Rectangle(panel.X + 150, panel.Y + 280, panel.Width - 300, 92), Color.White, 0.84f);
        DrawFittedText("COMING SOON", new Rectangle(panel.X + 250, panel.Y + 430, panel.Width - 500, 70), new Color(99, 223, 185), 0.72f);
        DrawDynamicOptionText("問題集と問題一覧は、ここからディレクトリーのように開いていく予定です。", new Rectangle(panel.X + 150, panel.Y + 530, panel.Width - 300, 54), new Color(180, 195, 195), 0.38f);
        DrawTitleBackButton(mousePoint);
    }

    private void DrawPonnukiProviderSelection(GoAppSession session, Rectangle panel, Point mousePoint, int appProviderTabIndex, bool isAppProviderLoading)
    {
        DrawDynamicOptionText("ポン抜きゲーム", new Rectangle(500, 350, 500, 54), Color.White, 0.62f);
        DrawText("APP PROVIDER ENGINE", new Vector2(530, 416), new Color(255, 190, 92), 0.42f);
        DrawDynamicOptionText("アプリ提供エンジン", new Rectangle(950, 414, 330, 34), new Color(210, 214, 207), 0.32f);

        var bounds = TitleAppProviderEngineDisplayBounds;
        var textBounds = TitleAppProviderEngineTextBounds;
        var hovered = !isAppProviderLoading && textBounds.Contains(mousePoint);
        DrawText("PROVIDER", new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawDynamicOptionText(session.SelectedAppProviderEngineDisplayName, textBounds, Color.White, 0.34f);
        _wideLinkUnderline.Bounds = textBounds;
        _wideLinkUnderline.SetActionBadge(ActionBadge.Create("CHANGE", textBounds));
        _wideLinkUnderline.UpdatePointer(mousePoint);
        _wideLinkUnderline.Draw(this, new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText));
        if (hovered)
        {
            DrawStickyNote(
                StickyNoteKind.AppProviderEngineHint,
                new Vector2(textBounds.Right, textBounds.Center.Y),
                new Color(185, 196, 255),
                new Color(116, 145, 178),
                "APP PROVIDER ENGINE とは？",
                ["このＧＵＩの代わりにアプリ実行を", "担当してくれるエンジンです。"]);
        }
        if (isAppProviderLoading)
        {
            DrawFittedText("LOADING PROVIDERS", textBounds, new Color(255, 210, 128), 0.30f);
            DrawAppProviderLoadingSpinner(new Vector2(textBounds.Right - 22, textBounds.Center.Y));
        }

        var capabilityColor = session.IsAppProviderCapabilityConfirmed
            ? new Color(99, 223, 185)
            : session.IsAppProviderCapabilityCheckRunning
                ? new Color(255, 210, 128)
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
            appProviderTabIndex == 1,
            mousePoint,
            enabled: session.CanUseSelectedAppProvider && !session.IsAppProviderCapabilityCheckRunning,
            scale: 0.30f);
        DrawCommandButton(
            TitleAppProviderStartButtonBounds,
            session.CanStartSelectedAppProvider ? "NEXT" : session.CanUseSelectedAppProvider ? "CHECK REQUIRED" : "ENGINE REQUIRED",
            appProviderTabIndex == 2,
            mousePoint,
            enabled: session.CanStartSelectedAppProvider,
            scale: session.CanStartSelectedAppProvider ? 0.40f : 0.23f);
        DrawTitleBackButton(mousePoint, appProviderTabIndex == 3);

        DrawAppProviderTabHints(session, appProviderTabIndex, isAppProviderLoading);
    }

    private void DrawAppProviderLoadingSpinner(Vector2 center)
    {
        const int segmentCount = 12;
        var head = (int)(Environment.TickCount64 / 70 % segmentCount);
        for (var index = 0; index < segmentCount; index++)
        {
            var angle = MathF.Tau * index / segmentCount;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var distance = (head - index + segmentCount) % segmentCount;
            var alpha = (byte)Math.Clamp(235 - distance * 15, 70, 235);
            DrawLine(center + direction * 11, center + direction * 22, 4, new Color(147, 244, 200, (int)alpha));
        }
    }

    private void DrawAppProviderTabHints(GoAppSession session, int activeTabIndex, bool isAppProviderLoading)
    {
        var stops = new[]
        {
            (Index: 0, Bounds: TitleAppProviderEngineDisplayBounds, Enabled: !isAppProviderLoading),
            (Index: 1, Bounds: TitleAppProviderRecheckButtonBounds, Enabled: session.CanUseSelectedAppProvider && !session.IsAppProviderCapabilityCheckRunning),
            (Index: 2, Bounds: TitleAppProviderStartButtonBounds, Enabled: session.CanStartSelectedAppProvider),
            (Index: 3, Bounds: TitleMenuBackButtonBounds, Enabled: true),
        }.Where(stop => stop.Enabled).ToArray();
        var activeStopIndex = Array.FindIndex(stops, stop => stop.Index == activeTabIndex);
        for (var index = 0; index < stops.Length; index++)
        {
            DrawTabNavigationHint(stops[index].Bounds, index, activeStopIndex, stops.Length);
        }
    }

    private void DrawTitleBackButton(Point mousePoint, bool focused = false) =>
        DrawCommandButton(TitleMenuBackButtonBounds, "BACK", focused, mousePoint, scale: 0.36f);

    private static string GetDisplayVersion()
    {
        var version = typeof(GoScreenRenderer).Assembly.GetName().Version;
        return version is null ? "VERSION" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    #region ［最新バージョンへ更新］ボタン
    /// <summary>
    /// ［最新バージョンへ更新］ボタンを描画します。
    /// </summary>
    /// <param name="mousePoint"></param>
    private void DrawUpdateButton(Point mousePoint)
    {
        var hovered = UpdateButtonBounds.Contains(mousePoint);
        var color = hovered ? new Color(99, 223, 185) : new Color(180, 195, 195);
        FillRect(UpdateButtonBounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(UpdateButtonBounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var board = new Rectangle(UpdateButtonBounds.X + 15, UpdateButtonBounds.Y + 11, 40, 40);
        DrawRect(board, 2, color);
        for (var index = 1; index < 5; index++)
        {
            var offset = index * 8;
            DrawLine(new Vector2(board.X + offset, board.Y), new Vector2(board.X + offset, board.Bottom), 1, color);
            DrawLine(new Vector2(board.X, board.Y + offset), new Vector2(board.Right, board.Y + offset), 1, color);
        }
        DrawStone(new Vector2(board.X + 16, board.Y + 24), 5, true);
        DrawStone(new Vector2(board.X + 31, board.Y + 16), 5, false);
    }
    #endregion
}
