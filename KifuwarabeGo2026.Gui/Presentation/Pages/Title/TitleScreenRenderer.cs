namespace KifuwarabeGo2026.Gui.Presentation.Pages.Title;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.Shared.TitleBackground;
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;
using KifuwarabeGo2026.Gui.Presentation.Pages.ApplicationSettings;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using System.Collections.Generic;

public sealed class TitleScreenRenderer
{
    public void DrawScreen(KfwStationeryDrawingTools drawingContext, GtpEngineRenderer gtpEngineRenderer,
        GoAppSession session, Point mousePosition,
        TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading,
        IReadOnlyList<GuiPlaySpaceEntry> gameOasisPlaySpaces)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        drawingContext.DrawBackground();
        Draw(drawingContext, session, mousePoint, page, appProviderTabIndex, isAppProviderLoading, gameOasisPlaySpaces);
        SelectEntryPresenter.Default.Draw(drawingContext, session, mousePoint);
        EditEntryProfile.Default.Draw(drawingContext, session, mousePoint, HeadUpDisplayComponent.Default.StickyNoteScreen);
        EntryProfilesPresenter.Default.DrawPanels(drawingContext, session, mousePoint);
        gtpEngineRenderer.Draw(drawingContext, session, mousePoint);
        drawingContext.End();
    }

    private readonly TitleGoEquipment _titleGoEquipment = new();
    private readonly TitleScreen _titleScreen = TitleScreen.Default;
    private readonly Action<Vector2, float, float, Color, int, float> _drawEllipseWire;
    private readonly Action<Vector2, float, float, Color, int, float, float, float> _drawCircumscribedCircleArc;
    private KfwStationeryDrawingTools _drawingContext = null!;

    public TitleScreenRenderer(
        Action<Vector2, float, float, Color, int, float> drawEllipseWire,
        Action<Vector2, float, float, Color, int, float, float, float> drawCircumscribedCircleArc)
    {
        _drawEllipseWire = drawEllipseWire;
        _drawCircumscribedCircleArc = drawCircumscribedCircleArc;
    }

    #region ［FORMAL APPS 区画］
    #endregion

    #region ［CASUAL APPS 区画］
    #endregion

    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint, TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading,
        IReadOnlyList<GuiPlaySpaceEntry> gameOasisPlaySpaces)
    {
        _drawingContext = drawingContext;
        // タイトル画面の囲碁用具ワイヤー装飾。
        _titleGoEquipment.Draw(new TitleGoEquipmentDrawingCallbacks(_drawEllipseWire, _drawCircumscribedCircleArc));

        var panel = _titleScreen.PanelBounds;
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        // 見出し（ヘッドライン）
        _titleScreen.Headline.Position = new Vector2(panel.X + 58, panel.Y + 58);
        _titleScreen.Headline.Draw(_drawingContext);
        DrawText(GetDisplayVersion(), new Vector2(panel.X + 790, panel.Y + 91), new Color(99, 223, 185), 0.38f);
        DrawLine(new Vector2(panel.X + 790, panel.Y + 126), new Vector2(panel.X + 958, panel.Y + 126), 2, new Color(99, 223, 185, 120));
        DrawTitleMenuContent(session, page, panel, mousePoint, appProviderTabIndex, isAppProviderLoading, gameOasisPlaySpaces);
        DrawUpdateButton(mousePoint);
        ApplicationSettingsScreen.Default.DrawSettingsButton(_drawingContext, mousePoint);
    }

    private void DrawTitleMenuContent(GoAppSession session, TitleMenuPage page, Rectangle panel, Point mousePoint, int appProviderTabIndex, bool isAppProviderLoading,
        IReadOnlyList<GuiPlaySpaceEntry> gameOasisPlaySpaces)
    {
        switch (page)
        {
            case TitleMenuPage.Home:
                var entrySettingsHovered = _titleScreen.EntrySettingsLabelBounds.Contains(mousePoint);
                var formalAppsHovered = _titleScreen.FormalAppsLabelBounds.Contains(mousePoint);
                var casualAppsHovered = _titleScreen.CasualAppsLabelBounds.Contains(mousePoint);
                var gamePlatformHovered = _titleScreen.GamePlatformLabelBounds.Contains(mousePoint) ||
                    _titleScreen.GameOasisButton.IsHit(mousePoint);
                var localMatchHovered = _titleScreen.LocalMatchButton.IsHit(mousePoint);
                var onlineMatchHovered = _titleScreen.CgosClientButton.IsHit(mousePoint);
                var engineProfilesHovered = _titleScreen.EngineProfilesButton.IsHit(mousePoint);
                var entryProfilesHovered = _titleScreen.EntryProfilesButton.IsHit(mousePoint);
                var settingsBounds = ApplicationSettingsScreen.Default.SettingsButton.Bounds;
                var updateBounds = ApplicationSettingsScreen.Default.UpdateButton.Bounds;
                var settingsHovered = settingsBounds.Contains(mousePoint);
                var updateHovered = updateBounds.Contains(mousePoint);
                _titleScreen.EntrySettingsLabel.Draw(_drawingContext);
                _titleScreen.FormalAppsLabel.Draw(_drawingContext);
                _titleScreen.CasualAppsLabel.Draw(_drawingContext);
                _titleScreen.GamePlatformLabel.Draw(_drawingContext);
                DrawProfileChoice(_titleScreen.EngineProfilesButton.Bounds, "エンジン登録", "REGISTER ENGINES", mousePoint, true);
                DrawProfileChoice(_titleScreen.EntryProfilesButton.Bounds, "エントリー登録", "REGISTER ENTRIES", mousePoint, false);
                DrawHomeServiceChoice(_titleScreen.LocalMatchButton.Bounds, _titleScreen.LocalMatchButton.Label, "PLAY / REVIEW", new Color(99, 223, 185), mousePoint);
                DrawHomeServiceChoice(_titleScreen.CgosClientButton.Bounds, _titleScreen.CgosClientButton.Label, "WATCH / CONNECT", new Color(99, 223, 185), mousePoint);
                DrawAppChoice(_titleScreen.CaptureGameButton.Bounds, _titleScreen.CaptureGameButton.Label, "CAPTURE GAME", mousePoint);
                DrawAppChoice(_titleScreen.GameOasisButton.Bounds, _titleScreen.GameOasisButton.Label, "REFERENCE PLAY-SPACES", mousePoint, new Color(178, 145, 255));
                DrawDynamicOptionText("左で対局候補を準備し、利用するアプリを選べます。", new Rectangle(460, 676, 980, 30), new Color(180, 195, 195), 0.34f);
                if (entrySettingsHovered)
                    DrawTitleHomeHint("ENTRY SETTINGS", "エンジンと対局候補を準備します！", new Color(125, 225, 255));
                else if (formalAppsHovered)
                    DrawTitleHomeHint("FORMAL APPS", "他のコンピュータ碁ソフトとできるだけ連携します！", new Color(99, 223, 185));
                else if (casualAppsHovered)
                    DrawTitleHomeHint("CASUAL APPS", "独自実装で機能追加を進めます！", new Color(255, 190, 92));
                else if (gamePlatformHovered)
                    DrawTitleHomeHint("GAME PLATFORM", "Replaceable play-spaces connect through Game Oasis.", new Color(178, 145, 255));
                else if (updateHovered)
                    DrawStickyNote(StickyNoteKind.TitleUpdateHint, new Vector2(updateBounds.Left, updateBounds.Center.Y), new Color(99, 223, 185), new Color(82, 111, 114), "ランチャーを開くとは？", ["共通ランチャーを前面に開き、", "このGUIを閉じます。", "GUIとEngineの更新は", "ランチャーから行います！"]);
                else if (settingsHovered)
                    DrawTitleHomeHint("SETTINGS", "アプリケーションを設定します！", new Color(147, 201, 190));
                else if (localMatchHovered)
                    DrawTitleHomeHint("LOCAL MATCH", "ローカルPCで、人間や碁エンジンが対局！ など。", new Color(99, 223, 185));
                else if (onlineMatchHovered)
                    DrawTitleHomeHint("ONLINE MATCH", "インターネット上の碁サーバーにお邪魔して碁エンジンが対局！", new Color(99, 223, 185));
                else if (engineProfilesHovered)
                    DrawTitleHomeHint("ENGINE PROFILES", "GTPエンジンの起動設定を管理します。", new Color(125, 225, 255));
                else if (entryProfilesHovered)
                    DrawTitleHomeHint("ENTRY PROFILES", "対局へ参加させる候補を準備します。", new Color(147, 244, 200));
                else if (_titleScreen.CaptureGameButton.IsHit(mousePoint))
                {
                    DrawCaptureGamePreview();
                }
                break;
            case TitleMenuPage.GameOasis:
                DrawTitleBreadcrumb("GAME OASIS  >  SELECT PLAY-SPACE", panel);
                for (var index = 0; index < Math.Min(gameOasisPlaySpaces.Count, 4); index++)
                {
                    var entry = gameOasisPlaySpaces[index];
                    DrawGameOasisPlaySpaceChoice(
                        TitleScreen.GetGameOasisPlaySpaceBounds(index),
                        entry,
                        mousePoint);
                }
                if (gameOasisPlaySpaces.Count == 0)
                    DrawFittedText("CONNECTING TO GAME OASIS...", new Rectangle(560, 500, 800, 52), new Color(180, 195, 195), 0.42f);
                else if (gameOasisPlaySpaces.Count > 4)
                    DrawFittedText($"+ {gameOasisPlaySpaces.Count - 4} MORE PLAY-SPACES", new Rectangle(560, 790, 800, 32), new Color(180, 195, 195), 0.3f);
                DrawTitleBackButton(mousePoint);
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
        DrawFittedText(title, new Rectangle(bounds.X + 28, bounds.Y + 20, bounds.Width - 56, 42), Color.White, 0.52f);
        DrawFittedText(caption, new Rectangle(bounds.X + 28, bounds.Y + 74, bounds.Width - 120, 30), new Color(204, 241, 226), 0.34f);
        DrawFittedText("OPEN  >", new Rectangle(bounds.Right - 92, bounds.Y + 76, 68, 28), hovered ? accent : new Color(180, 195, 195), 0.28f);
    }

    private void DrawGameOasisPlaySpaceChoice(Rectangle bounds, GuiPlaySpaceEntry entry, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        var accent = new Color(178, 145, 255);
        FillRect(new Rectangle(bounds.X + 6, bounds.Y + 8, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, hovered ? new Color(42, 45, 60) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(212, 194, 255) : new Color(103, 87, 142));
        FillRect(new Rectangle(bounds.X, bounds.Y, 6, bounds.Height), hovered ? accent : new Color(126, 96, 192));

        DrawDynamicOptionText(entry.DisplayName,
            new Rectangle(bounds.X + 28, bounds.Y + 14, bounds.Width - 126, 44), Color.White, 0.52f);
        DrawDynamicOptionText($"v{entry.ImplementationVersion}",
            new Rectangle(bounds.Right - 92, bounds.Y + 18, 68, 30), new Color(210, 198, 242), 0.3f);

        DrawDynamicOptionText("IMPLEMENTATION",
            new Rectangle(bounds.X + 28, bounds.Y + 64, bounds.Width - 56, 22), new Color(148, 130, 194), 0.24f);
        var (firstLine, secondLine) = SplitImplementationName(entry.ImplementationName);
        DrawDynamicOptionText(firstLine,
            new Rectangle(bounds.X + 28, bounds.Y + 87, bounds.Width - 56, 28), new Color(205, 213, 214), 0.31f);
        if (secondLine.Length > 0)
            DrawDynamicOptionText(secondLine,
                new Rectangle(bounds.X + 28, bounds.Y + 113, bounds.Width - 130, 28), new Color(205, 213, 214), 0.31f);

        DrawFittedText("OPEN  >", new Rectangle(bounds.Right - 92, bounds.Bottom - 40, 68, 28),
            hovered ? new Color(220, 205, 255) : new Color(180, 195, 195), 0.28f);
    }

    internal static (string FirstLine, string SecondLine) SplitImplementationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ("-", "");
        var center = value.Length / 2;
        var separators = value.Select((character, index) => (character, index))
            .Where(item => item.character == '.' && item.index > 0 && item.index < value.Length - 1)
            .Select(item => item.index)
            .ToArray();
        if (separators.Length == 0) return (value, "");
        var split = separators.MinBy(index => Math.Abs(index - center));
        return (value[..split], value[(split + 1)..]);
    }

    private void DrawTitleBreadcrumb(string text, Rectangle panel)
    {
        DrawText(text, new Vector2(panel.X + 62, panel.Y + 142), new Color(180, 195, 195), 0.46f);
        DrawLine(new Vector2(panel.X + 62, panel.Y + 184), new Vector2(panel.Right - 62, panel.Y + 184), 1, new Color(82, 111, 114));
    }

    private void DrawAppChoice(Rectangle bounds, string title, string caption, Point mousePoint, Color? accentOverride = null)
    {
        var accent = accentOverride ?? new Color(255, 190, 92);
        var hovered = bounds.Contains(mousePoint);
        FillRect(new Rectangle(bounds.X + 7, bounds.Y + 9, bounds.Width, bounds.Height), new Color(0, 0, 0, 90));
        FillRect(bounds, hovered ? new Color(42, 55, 63) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? accent : new Color(accent.R, accent.G, accent.B, (byte)135));
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 7), hovered ? accent : new Color(accent.R, accent.G, accent.B, (byte)105));
        DrawDynamicOptionText(title, new Rectangle(bounds.X + 18, bounds.Y + 12, 250, 38), Color.White, 0.43f);
        DrawFittedText(caption, new Rectangle(bounds.X + 18, bounds.Y + 52, 260, 22), accent, 0.27f);
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
            "ENTRY SETTINGS" =>
                (StickyNoteKind.TitleSettingsHint, GetTitleSectionLabelConnectorTarget("ENTRY SETTINGS", new Vector2(460, 338), connectsToRight: false)),
            "FORMAL APPS" =>
                (StickyNoteKind.TitleFormalAppsHint, GetTitleSectionLabelConnectorTarget("FORMAL APPS", new Vector2(800, 338), connectsToRight: false)),
            "CASUAL APPS" =>
                (StickyNoteKind.TitleCasualAppsHint, GetTitleSectionLabelConnectorTarget("CASUAL APPS", new Vector2(1140, 338), connectsToRight: true)),
            "GAME PLATFORM" =>
                (StickyNoteKind.TitleFormalAppsHint, new Vector2(_titleScreen.GameOasisButton.Bounds.Left, _titleScreen.GameOasisButton.Bounds.Center.Y)),
            "LOCAL MATCH" =>
                (StickyNoteKind.TitleLocalMatchHint, new Vector2(_titleScreen.LocalMatchButton.Bounds.Left, _titleScreen.LocalMatchButton.Bounds.Center.Y)),
            "ONLINE MATCH" =>
                (StickyNoteKind.TitleOnlineMatchHint, new Vector2(_titleScreen.CgosClientButton.Bounds.Left, _titleScreen.CgosClientButton.Bounds.Center.Y)),
            _ =>
                (StickyNoteKind.TitleSettingsHint, new Vector2(ApplicationSettingsScreen.Default.SettingsButton.Bounds.Left - 14, ApplicationSettingsScreen.Default.SettingsButton.Bounds.Center.Y)),
        };
        var bodyLines = heading switch
        {
            "FORMAL APPS" => new[]
            {
                "他の人が作った GTP対応の",
                "コンピュータ碁の思考エンジンを",
                "動かせるよう、",
                "有名なエンジンの拡張仕様は",
                "取り込んでいます！",
            },
            "CASUAL APPS" => new[] { "独自実装で", "機能追加を進めます！" },
            "ENTRY SETTINGS" => new[] { "エンジンを登録し、", "対局へ参加させる候補を準備します！" },
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
            ? labelPosition.X + _drawingContext.MeasureText(label).X * labelScale + gap
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
        PonnukiProviderSelectionScreen.Default.Draw(session, mousePoint, appProviderTabIndex, isAppProviderLoading,
            new PonnukiProviderSelectionDrawingCallbacks(
                _drawingContext, _drawingContext, _drawingContext, DrawText, DrawDynamicOptionText, DrawFittedText, DrawLine,
                (kind, connectorStart, accent, borderColor, heading, bodyLines) =>
                    DrawStickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines),
                DrawTabNavigationHint));
    }

    private void DrawProfileChoice(Rectangle bounds, string title, string englishTitle, Point mousePoint, bool engine)
    {
        var hovered = bounds.Contains(mousePoint);
        var accent = engine ? new Color(125, 225, 255) : new Color(147, 244, 200);
        FillRect(new Rectangle(bounds.X + 6, bounds.Y + 8, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(88, 102, 112));
        FillRect(new Rectangle(bounds.X, bounds.Y, 6, bounds.Height), hovered ? accent : new Color(accent.R, accent.G, accent.B, (byte)100));
        DrawDynamicOptionText(title, new Rectangle(bounds.X + 22, bounds.Y + 18, bounds.Width - 76, 44), Color.White, 0.43f);
        DrawFittedText(englishTitle, new Rectangle(bounds.X + 22, bounds.Y + 72, bounds.Width - 44, 30), accent, 0.30f);
        var center = new Vector2(bounds.Right - 36, bounds.Y + 42);
        if (engine) _drawingContext.DrawEngineIcon(center);
        else _drawingContext.DrawEntryIcon(center);
    }

    private void DrawTitleBackButton(Point mousePoint, bool focused = false)
    {
        TitleScreen.Default.BackButton.IsSelected = focused;
        TitleScreen.Default.BackButton.Draw(mousePoint, _drawingContext);
    }

    private static string GetDisplayVersion()
    {
        var version = typeof(TitleScreenRenderer).Assembly.GetName().Version;
        return version is null ? "VERSION" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    #region ［ランチャーを開く］ボタン
    /// <summary>
    /// ［ランチャーを開く］ボタンを描画します。
    /// </summary>
    /// <param name="mousePoint"></param>
    private void DrawUpdateButton(Point mousePoint)
    {
        var bounds = ApplicationSettingsScreen.Default.UpdateButton.Bounds;
        var hovered = bounds.Contains(mousePoint);
        var color = hovered ? new Color(99, 223, 185) : new Color(180, 195, 195);
        FillRect(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var board = new Rectangle(bounds.X + 12, bounds.Y + 11, 40, 40);
        DrawRect(board, 2, color);
        for (var index = 1; index < 5; index++)
        {
            var offset = index * 8;
            DrawLine(new Vector2(board.X + offset, board.Y), new Vector2(board.X + offset, board.Bottom), 1, color);
            DrawLine(new Vector2(board.X, board.Y + offset), new Vector2(board.Right, board.Y + offset), 1, color);
        }
        DrawStone(new Vector2(board.X + 16, board.Y + 24), 5, true);
        DrawStone(new Vector2(board.X + 31, board.Y + 16), 5, false);
        DrawDynamicOptionText("ランチャーを開く", new Rectangle(bounds.X + 60, bounds.Y + 15, bounds.Width - 70, 34), color, 0.26f);
    }
    #endregion

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawingContext.DrawLine(start, end, thickness, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);
    private void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawDynamicText(text, bounds, color, scale);
    private void DrawStone(Vector2 center, float radius, bool black) => _drawingContext.DrawStone(center, radius, black);
    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, System.Collections.Generic.IReadOnlyList<string> bodyLines) =>
        _drawingContext.DrawStickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines);

    private void DrawTabNavigationHint(Rectangle bounds, int tabIndex, int activeIndex, int stopCount)
    {
        if (activeIndex < 0 || tabIndex == activeIndex || stopCount < 2) return;
        var previous = tabIndex == (activeIndex + stopCount - 1) % stopCount;
        var next = tabIndex == (activeIndex + 1) % stopCount;
        if (!previous && !next) return;
        var text = previous ? "SHIFT + TAB" : "TAB";
        var width = previous ? 132 : 56;
        var hint = new Rectangle(bounds.X - width - 6, bounds.Y - 34, width, 28);
        _drawingContext.FillRoundedRectangle(hint, 6, new Color(4, 6, 8, 235));
        DrawFittedText(text, new Rectangle(hint.X + 4, hint.Y + 2, hint.Width - 8, hint.Height - 4), Color.White, 0.32f);
    }
}
