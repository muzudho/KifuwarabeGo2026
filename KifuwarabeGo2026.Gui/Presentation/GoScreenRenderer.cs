namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;
using KifuwarabeGo2026.Gui.Presentation.Shared.SpinBox;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.VerticalSectionLabel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.MessageDialog;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenshotEffect;
using KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;
using KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared.RenBoundaries;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.MultilineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ［画面描画］の共通処理
/// </summary>
public sealed partial class GoScreenRenderer : IUnderlineDrawingSurface, IGoScreenRenderer
{
    private const int GameOverValueX = 1328;
    private const int GameOverSecondValueX = 1560;
    private const int PlayingPlayersY = 140;
    private const float MinimumTextScale = 0.32f;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ITextRasterizer _textRasterizer;
    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;
    private readonly Texture2D _pixel;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;
    private readonly LinkUnderline _wideLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly LinkUnderline _gtpEngineOptionLinkUnderline = new(
        new RoundUnderline { TopOffset = -4, Thickness = 4, Radius = 2 });
    private readonly MultilineTextUnderline _multilineTextUnderline = new(
        new SquareUnderline { Thickness = 1 });
    private readonly LinkUnderline _compactLinkUnderline = new(
        new RoundUnderline { TopOffset = 1, Thickness = 3, Radius = 1 });
    private readonly LinkUnderline _selectorLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 4, Radius = 2 });
    private readonly LinkUnderline _playerSelectorLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly LinkUnderline _settingsLogLinkUnderline = new(
        new RoundUnderline { TopOffset = -7, Thickness = 5, Radius = 2 });
    private readonly LinkUnderline _settingsValueLinkUnderline = new(
        new RoundUnderline { TopOffset = -7, Thickness = 6, Radius = 3 });
    private readonly Breadcrumb _breadcrumb = new();
    private readonly SpinBox _spinBox = new();
    private readonly VerticalSectionLabel _verticalSectionLabel = new();
    private readonly TextInputDialog _textInputDialog = new();
    public ScreenTransition ScreenTransition { get; } = new();
    public ScreenshotEffect ScreenshotEffect { get; } = new();
    public ReviewUnsavedChangesConfirmation ReviewUnsavedChangesConfirmation { get; } = new();
    public InitialPositionConcierge InitialPositionConcierge { get; } = new();
    public PopupNumberUnderline PopupNumberUnderline { get; } = new();
    private StickyNoteScreenId _stickyNoteScreen = StickyNoteScreenId.Unknown;
    private readonly CgosMatchNotification _cgosMatchNotification = new();
    private static readonly BoardLensButtonStrip LocalPlayingBoardLensButtons = new(1516, 800);
    public EditEntryProfile EditEntryProfile { get; } = new();

    public GoScreenRenderer(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        ITextRasterizer textRasterizer)
    {
        _graphicsDevice = graphicsDevice;
        _textRasterizer = textRasterizer;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("Fonts/Ui");
        _boardCoordinateFont = content.Load<SpriteFont>("Fonts/BoardCoordinate");
        _pixel = CreateTexture(1, 1, (_, _) => Color.White);
        _softCircle = CreateCircleTexture(128, new Color(255, 255, 255, 255), softEdge: true);
        _stoneLight = CreateStoneTexture(128, lightStone: true);
        _stoneDark = CreateStoneTexture(128, lightStone: false);
    }

    public void Draw(
        GoAppSession session,
        Point mousePosition,
        LiveBoardPreview? liveBoardPreview = null,
        InitialPositionConciergeView? initialPositionConcierge = null)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        var modalOpen = session.IsTournamentRulesSelectionDialogOpen || session.IsTournamentRulesAddPanelOpen ||
                        session.IsPlayerSelectionDialogOpen || session.IsPlayerEditPanelOpen || session.IsClientIdentityProfileSelectionPanelOpen || session.IsClientIdentityProfileEditPanelOpen ||
                        session.IsGtpEngineSelectionDialogOpen || session.IsGtpEngineEditPanelOpen ||
                        session.IsAppProviderGameSettingsDialogOpen;
        var backgroundMousePoint = modalOpen ? new Point(-1, -1) : mousePoint;
        DrawBoard(session, backgroundMousePoint);
        if (session.CurrentMode.Kind == GoAppModeKind.Playing &&
            session.CanOpenLocalChartPopup)
        {
            DrawBroadcastStatusBadge(
                session.IsLocalReplayMode ? "REPLAY" : "CURRENT",
                session.IsReviewChartPopupOpen);
        }
        if (!session.IsReviewChartPopupOpen)
        {
            DrawSidePanel(session, backgroundMousePoint, liveBoardPreview, initialPositionConcierge);
            if (session.IsLocalReplayMode)
            {
                DrawReplayNavigationControls(
                    session.LocalDisplayMoveIndex,
                    session.CurrentGameRecord.Moves.Count,
                    backgroundMousePoint,
                    showBackToLive: session.CurrentMode.Kind == GoAppModeKind.Playing,
                    backToLiveLabel: "BACK TO CURRENT");
            }
            else if (session.CanOpenLocalChartPopup ||
                     session.CurrentMode.Kind == GoAppModeKind.Reviewing)
            {
                DrawReplayEditIconButton(backgroundMousePoint);
            }
            DrawTournamentRulesSelectionDialog(session, mousePoint);
            DrawTournamentRulesAddPanel(session, mousePoint);
            DrawPlayerSelectionDialog(session, mousePoint);
            DrawPlayerEditPanel(session, mousePoint);
            DrawClientIdentityProfileEditPanel(session, mousePoint);
            DrawQuickClientIdentitySelectionPanel(session, mousePoint);
            DrawGtpEngineSelectionDialog(session, mousePoint);
            DrawGtpEngineEditPanel(session, mousePoint);
            if (session.IsAppProviderGameSettingsDialogOpen)
                DrawGtpEngineGuiOptionsDialog(session, mousePoint);
        }
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing && session.IsReviewChartPopupOpen)
        {
            DrawReviewChartPopup(session, mousePoint);
        }
        else if (session.CanOpenLocalChartPopup && session.IsReviewChartPopupOpen)
        {
            DrawLocalChartPopup(session, mousePoint);
        }

        _spriteBatch.End();
    }

    public void DrawUseSelection(GoAppSession session, Point mousePosition, TitleMenuPage page, int appProviderTabIndex, bool isAppProviderLoading)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        DrawUseSelectionPanel(session, mousePoint, page, appProviderTabIndex, isAppProviderLoading);
        DrawPlayerSelectionDialog(session, mousePoint);
        DrawGtpEngineSelectionDialog(session, mousePoint);
        DrawGtpEngineEditPanel(session, mousePoint);

        _spriteBatch.End();
    }

    /// <summary>バックグラウンドで設定ファイルを保存している間の入力遮断表示です。</summary>
    public void DrawSavingOverlay(string message)
    {
        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 145));
        var panel = new Rectangle(690, 470, 540, 150);
        FillRect(panel, new Color(24, 29, 36, 250));
        DrawRect(panel, 2, new Color(147, 244, 200));
        DrawFittedText(string.IsNullOrWhiteSpace(message) ? "SAVING..." : message, new Rectangle(panel.X + 40, panel.Y + 34, panel.Width - 80, 34), Color.White, 0.46f);

        var center = new Vector2(panel.Center.X, panel.Y + 108);
        var phase = (float)(Environment.TickCount64 % 900L) / 900f * MathF.PI * 2f;
        for (var index = 0; index < 12; index++)
        {
            var angle = phase + index * MathF.PI * 2f / 12f;
            var opacity = (index + 1) / 12f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            DrawLine(center + direction * 15f, center + direction * 27f, 4, new Color(147, 244, 200) * opacity);
        }

        _spriteBatch.End();
    }
    public static int? GetBoardSizeButtonHit(Point point, GoAppModeKind modeKind)
    {
        if (modeKind == GoAppModeKind.GameOver)
        {
            return null;
        }

        var y = AddPanelBoardSizeButtonY;
        if (BoardSizeButtonBounds(0, y).Contains(point))
        {
            return 9;
        }

        if (BoardSizeButtonBounds(1, y).Contains(point))
        {
            return 13;
        }

        return BoardSizeButtonBounds(2, y).Contains(point) ? 19 : null;
    }
    public static GoRuleKind? GetRuleKindButtonHit(Point point)
    {
        if (RuleKindButtonBounds(0).Contains(point))
        {
            return GoRuleKind.Japanese;
        }

        if (RuleKindButtonBounds(1).Contains(point))
        {
            return GoRuleKind.PureGo;
        }

        return RuleKindButtonBounds(2).Contains(point) ? GoRuleKind.Chinese : null;
    }

    public static decimal? GetKomiStepButtonHit(Point point)
    {
        if (KomiSpinButtonBounds(true).Contains(point)) return 0.5m;
        return KomiSpinButtonBounds(false).Contains(point) ? -0.5m : null;
    }

    public static TimeSpan? GetMainTimeStepButtonHit(Point point)
    {
        var steps = new[] { 3600, 60, 1 };
        for (var index = 0; index < steps.Length; index++)
        {
            if (MainTimeSpinButtonBounds(index, true).Contains(point)) return TimeSpan.FromSeconds(steps[index]);
            if (MainTimeSpinButtonBounds(index, false).Contains(point)) return TimeSpan.FromSeconds(-steps[index]);
        }

        return null;
    }

    public static int? GetMoveLimitStepButtonHit(Point point)
    {
        var steps = new[] { 100, 10, 1 };
        for (var index = 0; index < steps.Length; index++)
        {
            if (MoveLimitSpinButtonBounds(index, true).Contains(point)) return steps[index];
            if (MoveLimitSpinButtonBounds(index, false).Contains(point)) return -steps[index];
        }

        return null;
    }

    public static TournamentRulesNumericField? GetTournamentRulesMainTimeTextBoxHit(Point point)
    {
        for (var index = 0; index < 3; index++)
        {
            if (TournamentRulesMainTimePartTextBounds(index).Contains(point))
                return (TournamentRulesNumericField)((int)TournamentRulesNumericField.MainTimeHours + index);
        }
        return null;
    }

    public static bool GetTournamentRulesMoveLimitTextBoxHit(Point point) =>
        TournamentRulesMoveLimitTextBounds.Contains(point);
    public static bool GetLocalUseButtonHit(Point point) => LocalUseButtonBounds.Contains(point);
    public static bool GetImportSgfButtonHit(Point point) => ImportSgfButtonBounds.Contains(point);
    public static bool GetStartPlayingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartPlayingButtonBounds.Contains(point);
    public static bool GetChangeAppProviderButtonHit(Point point) => ChangeAppProviderButtonBounds.Contains(point);
    public static bool GetAppProviderGameSettingsButtonHit(Point point) => AppProviderGameSettingsButtonBounds.Contains(point);
    public static PonnukiRandomSeedRole? GetPonnukiRandomSeedAutoChangeHit(Point point) =>
        PonnukiProviderSeedAutoChangeBounds.Contains(point) ? PonnukiRandomSeedRole.Provider :
        PonnukiPlayer1SeedAutoChangeBounds.Contains(point) ? PonnukiRandomSeedRole.Player1 :
        PonnukiPlayer2SeedAutoChangeBounds.Contains(point) ? PonnukiRandomSeedRole.Player2 : null;

    public static bool GetReturnToSetupButtonHit(Point point) => ReturnToSetupButtonBounds.Contains(point);

    public static bool GetExportSgfButtonHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static bool GetSgfAutoSaveCheckHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static bool GetLocalGameOverReviewButtonHit(Point point) =>
        LocalGameOverReviewButtonBounds.Contains(point);

    public static bool GetSetupBackToTitleButtonHit(Point point) => SetupBackToTitleButtonBounds.Contains(point);

    public static GoPlayerKind? GetBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, BlackPlayerKindButtonY);

    public static GoPlayerKind? GetWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, WhitePlayerKindButtonY);

    public static GoPlayerKind? GetPonnukiBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, PonnukiBlackPlayerKindButtonY);

    public static GoPlayerKind? GetPonnukiWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, PonnukiWhitePlayerKindButtonY);

    public static GoStone? GetHumanPlayerNameTextBoxHit(Point point, GoAppSession session)
    {
        if (session.BlackPlayerKind == GoPlayerKind.Human && HumanPlayerNameRowBounds(BlackEngineButtonY).Contains(point)) return GoStone.Black;
        return session.WhitePlayerKind == GoPlayerKind.Human && HumanPlayerNameRowBounds(WhiteEngineButtonY).Contains(point) ? GoStone.White : null;
    }

    public int GetHumanPlayerNameCaretIndex(Point point, GoStone stone, string text) =>
        GetTextBoxCaretIndex(point.X, text, HumanPlayerNameTextBounds(stone == GoStone.Black ? BlackEngineButtonY : WhiteEngineButtonY), 0.42f);

    public int GetTournamentRulesNumericCaretIndex(Point point, TournamentRulesNumericField field, string text)
    {
        var bounds = field switch
        {
            TournamentRulesNumericField.MainTimeHours => TournamentRulesMainTimePartTextBounds(0),
            TournamentRulesNumericField.MainTimeMinutes => TournamentRulesMainTimePartTextBounds(1),
            TournamentRulesNumericField.MainTimeSeconds => TournamentRulesMainTimePartTextBounds(2),
            _ => TournamentRulesMoveLimitTextBounds,
        };
        return GetTextBoxCaretIndex(point.X, text, new Rectangle(bounds.X + 8, bounds.Y + 4, bounds.Width - 16, bounds.Height - 8), 0.42f);
    }
    public static bool GetPassButtonHit(Point point) => PassButtonBounds.Contains(point);

    public static bool GetResignButtonHit(Point point) => ResignButtonBounds.Contains(point);

    public static bool GetCancelPlayingButtonHit(Point point) => CancelPlayingButtonBounds.Contains(point);
    private void DrawBackground()
    {
        var topLeft = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, Point.Zero);
        var bottomRight = VirtualScreen.ToVirtualPoint(
            _graphicsDevice.Viewport,
            new Point(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height));
        var visibleLeft = Math.Min(topLeft.X, bottomRight.X) - 2;
        var visibleRight = Math.Max(topLeft.X, bottomRight.X) + 2;
        var visibleTop = Math.Min(topLeft.Y, bottomRight.Y) - 2;
        var visibleBottom = Math.Max(topLeft.Y, bottomRight.Y) + 2;
        var visibleWidth = visibleRight - visibleLeft;

        FillRect(
            new Rectangle(visibleLeft, visibleTop, visibleWidth, visibleBottom - visibleTop),
            new Color(11, 13, 18));
        FillRect(new Rectangle(visibleLeft, 0, visibleWidth, 150), new Color(24, 30, 40));
        FillRect(new Rectangle(visibleLeft, 930, visibleWidth, 150), new Color(9, 28, 31));

        for (var i = 0; i < 18; i++)
        {
            var alpha = (byte)(50 - i * 2);
            var start = new Vector2(-120, 180 + i * 42);
            var end = new Vector2(2050, -40 + i * 64);
            var slope = (end.Y - start.Y) / (end.X - start.X);
            DrawLine(
                new Vector2(visibleLeft, start.Y + (visibleLeft - start.X) * slope),
                new Vector2(visibleRight, start.Y + (visibleRight - start.X) * slope),
                2,
                new Color((byte)56, (byte)86, (byte)96, alpha));
        }

        DrawGlow(new Vector2(1030, 90), 520, new Color(39, 122, 104, 80));
        DrawGlow(new Vector2(1700, 850), 360, new Color(144, 59, 48, 72));
    }
    private void DrawSidePanel(
        GoAppSession session,
        Point mousePoint,
        LiveBoardPreview? liveBoardPreview,
        InitialPositionConciergeView? initialPositionConcierge)
    {
        var panel = new Rectangle(1102, 78, 760, 924);
        FillRect(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        FillRect(panel, new Color(21, 25, 32, 236));
        DrawRect(panel, 2, new Color(82, 111, 114));

        if (initialPositionConcierge is { IsVisible: true })
        {
            DrawInitialPositionConcierge(initialPositionConcierge, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.Playing)
        {
            DrawPlayingSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            DrawGameOverSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing)
        {
            DrawBoardEditingSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.VariationEditing)
        {
            DrawVariationEditingSidePanel(session, mousePoint, liveBoardPreview);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            DrawReviewingSidePanel(session, mousePoint);
            return;
        }

        if (session.UseKind == GoAppUseKind.LocalApps)
        {
            DrawLocalAppsIntermissionSidePanel(session, mousePoint);
        }
        else
        {
            DrawSetupSidePanel(session, mousePoint);
        }
    }
    private void DrawLocalClosedBox(Rectangle bounds)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 70));
        FillRect(bounds, new Color(17, 24, 29));
        DrawRect(bounds, 4, new Color(126, 150, 164));
        DrawMiniBoardGrid(new Rectangle(bounds.X + 22, bounds.Y + 20, bounds.Width - 44, bounds.Height - 40), new Color(88, 102, 112, 85));

        var left = new Vector2(bounds.X + 94, bounds.Y + 76);
        var right = new Vector2(bounds.X + 206, bounds.Y + 76);
        DrawLine(left, right, 5, new Color(99, 223, 185));
        DrawIconStone(left, 24, black: true);
        DrawIconStone(right, 24, black: false);
    }
    private void DrawIconStone(Vector2 center, float radius, bool black)
    {
        DrawCircle(center, radius + 5, black ? new Color(178, 219, 226) : new Color(72, 80, 84));
        DrawStone(center, radius, black);
        if (black)
        {
            DrawCircle(new Vector2(center.X - radius * 0.28f, center.Y - radius * 0.32f), radius * 0.22f, new Color(255, 255, 255, 42));
        }
    }

    private void DrawMiniBoardGrid(Rectangle bounds, Color color)
    {
        for (var i = 0; i < 7; i++)
        {
            var x = bounds.X + i * bounds.Width / 6f;
            DrawLine(new Vector2(x, bounds.Y), new Vector2(x, bounds.Bottom), 1, color);
            var y = bounds.Y + i * bounds.Height / 6f;
            DrawLine(new Vector2(bounds.X, y), new Vector2(bounds.Right, y), 1, color);
        }
    }
    private void DrawSetupSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawCommandButton(SetupBackToTitleButtonBounds, "BACK TO TITLE", false, mousePoint, scale: 0.32f);

        DrawVerticalResultSection(new Rectangle(1144, 184, 668, 176), "TOURNAMENT", new Color(62, 112, 105));
        DrawCommandButton(TournamentRulesSelectButtonBounds, "TOURNAMENT SELECT", false, mousePoint, scale: 0.32f);
        DrawCommandButton(ImportSgfButtonBounds, session.HasReviewGameRecord ? "KIFU CLEAR (SGF)" : "KIFU INPUT (SGF)", false, mousePoint, scale: 0.34f);
        DrawResultRow(new Rectangle(1164, 292, 628, 56), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);

        DrawVerticalResultSection(new Rectangle(1144, 376, 668, 304), "RULES", new Color(66, 104, 116));
        DrawInfoStrip(1144, 384, "RULE", session.RuleKind.ToString());
        DrawInfoStrip(1144, 456, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 528, "KOMI", FormatKomi(session.Komi));
        DrawInfoStrip(1144, 600, "MOVES", FormatMoveLimit(session.MoveLimit));

        DrawVerticalResultSection(new Rectangle(1144, 696, 668, 216), "PLAYERS", new Color(76, 91, 126));
        DrawSetupPlayerRow(session, GoStone.Black, mousePoint, BlackPlayerKindButtonY);
        DrawSetupPlayerRow(session, GoStone.White, mousePoint, WhitePlayerKindButtonY);

        DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        DrawCommandButton(StartReviewingButtonBounds, "KIFU REVIEW", false, mousePoint, enabled: session.HasReviewGameRecord, scale: 0.32f);
        DrawCommandButton(StartBoardEditingButtonBounds, "EDIT BOARD", false, mousePoint, scale: 0.36f);
        DrawCommandButton(
            StartPlayingButtonBounds,
            session.CanStartPlaying ? "START" : "ENGINE REQUIRED",
            false,
            mousePoint,
            enabled: session.CanStartPlaying,
            scale: session.CanStartPlaying ? 0.48f : 0.28f);
    }
    private void DrawPlayingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawVerticalResultSection(new Rectangle(1144, 132, 668, 200), "PLAYERS", new Color(76, 91, 126));
        DrawBothPlayersComponent(
            1144,
            PlayingPlayersY,
            668,
            session.GetLocalPlayerName(GoStone.Black),
            session.GetLocalPlayerName(GoStone.White),
            session.BlackElapsedTime,
            session.WhiteElapsedTime,
            session.MainTime,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            session.EngineErrorStone,
            mousePoint,
            minimal: true);

        DrawVerticalResultSection(new Rectangle(1144, 344, 668, 110), "FACTS", new Color(66, 104, 116));
        DrawInfoStrip(1144, 363, "NEXT", GetMoveThinkingText(session));

        DrawLocalTrendChart(session, mousePoint);

        DrawVerticalResultSection(new Rectangle(1144, 780, 668, 120), "REVIEW", new Color(76, 91, 126));
        DrawLocalPlayingBoardLensButtonStrip(session.IsRenParseDisplayEnabled, mousePoint);

        DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));

        if (session.CanAcceptHumanMove)
        {
            DrawCommandButton(PassButtonBounds, "PASS", false, mousePoint);
            DrawCommandButton(ResignButtonBounds, "RESIGN", false, mousePoint);
        }
        else
        {
            DrawCommandButton(CancelPlayingButtonBounds, "CANCEL", false, mousePoint);
        }
    }

    private void DrawGameOverSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("GAME OVER", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawText(FormatGameEndMoveCount(session.PlayedMoveCount), new Vector2(1144, 196), new Color(99, 223, 185), 0.58f);
        DrawCommandButton(ReturnToSetupButtonBounds, "BACK TO SETUP", false, mousePoint, scale: 0.34f);

        var resultSection = new Rectangle(1144, 236, 668, 128);
        DrawVerticalResultSection(resultSection, "RESULT", new Color(80, 48, 38));
        DrawResultRow(new Rectangle(1164, 242, 628, 52), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);
        DrawCalculationResultRow(new Rectangle(1164, 300, 628, 52), session);

        DrawLocalGameOverTrendChart(session, mousePoint);

        if (session.UseKind == GoAppUseKind.LocalApps)
        {
            DrawVerticalResultSection(new Rectangle(1144, 668, 668, 174), "AGEHAMA", new Color(112, 76, 48));
            DrawAgehamaSummaryComponent(new Rectangle(1164, 692, 628, 132), session.BlackAgehama, session.WhiteAgehama);
        }

        var actionSection = new Rectangle(1144, 854, 668, 126);
        DrawVerticalResultSection(actionSection, "ACTION", new Color(91, 82, 105));
        DrawCommandButton(LocalGameOverReviewButtonBounds, "KIFU REVIEW", false, mousePoint, scale: 0.36f);
        if (session.IsSgfAutoSaveAvailable)
            DrawSgfAutoSaveCheckBox(ExportSgfButtonBounds, session, mousePoint);
        else
            DrawCommandButton(ExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.52f);
    }

    private void DrawSgfAutoSaveCheckBox(Rectangle bounds, GoAppSession session, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, hovered ? new Color(47, 65, 91, 230) : new Color(31, 45, 70, 220));
        DrawRect(bounds, 2, new Color(137, 160, 205));

        var checkBounds = new Rectangle(bounds.X + 12, bounds.Y + (bounds.Height - 28) / 2, 28, 28);
        FillRect(checkBounds, new Color(17, 24, 48, 245));
        DrawRect(checkBounds, 2, new Color(176, 194, 242));
        if (session.IsSgfAutoSaveEnabled)
        {
            DrawLine(new Vector2(checkBounds.X + 6, checkBounds.Y + 15), new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7), 4, new Color(91, 218, 211));
            DrawLine(new Vector2(checkBounds.X + 12, checkBounds.Bottom - 7), new Vector2(checkBounds.Right - 5, checkBounds.Y + 6), 4, new Color(91, 218, 211));
        }

        var statusWidth = string.IsNullOrEmpty(session.SgfAutoSaveStatus) ? 0 : 116;
        DrawFittedText(
            "AUTO SAVE",
            new Rectangle(checkBounds.Right + 10, bounds.Y + 6, bounds.Width - 60 - statusWidth, bounds.Height - 12),
            Color.White,
            0.34f);
        if (statusWidth > 0)
        {
            var statusColor = session.SgfAutoSaveStatus == "AUTO SAVED"
                ? new Color(99, 223, 185)
                : new Color(255, 145, 151);
            DrawFittedText(
                session.SgfAutoSaveStatus,
                new Rectangle(bounds.Right - statusWidth - 8, bounds.Y + 6, statusWidth, bounds.Height - 12),
                statusColor,
                0.28f);
        }
    }
    private void DrawDisplayNameTextBox(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesAddPanelDisplayNameRowBounds;
        var active = session.IsTournamentRulesDisplayNameEditing;
        var displayName = active ? session.TournamentRulesDisplayNameDraft : session.TournamentDisplayName;
        var textBounds = TournamentRulesAddPanelDisplayNameTextBounds;
        var hovered = textBounds.Contains(mousePoint);
        DrawText("DISPLAY", new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawRoundedFill(
            new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5),
            2,
            active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (active)
            DrawTextBoxSelection(displayName, session.TournamentRulesDisplayNameSelectionStart, session.TournamentRulesDisplayNameSelectionLength, textBounds, 0.46f);
        DrawFittedText(string.IsNullOrEmpty(displayName) ? "-" : displayName, textBounds, Color.White, 0.46f);
        if (active)
        {
            DrawTextBoxCaret(displayName, session.TournamentRulesDisplayNameCaretIndex, textBounds, 0.46f);
        }
        DrawEditableTextEditHint(active, hovered, textBounds);

        if (!string.IsNullOrWhiteSpace(session.TournamentRulesDisplayNameWarning))
        {
            DrawFittedText(
                session.TournamentRulesDisplayNameWarning,
                new Rectangle(AddPanelControlX + 132, 740, 536, 28),
                new Color(255, 183, 146),
                0.34f);
        }
    }

    private void DrawFilePathSelector(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesAddPanelFileRowBounds;
        var filePath = string.IsNullOrWhiteSpace(session.CurrentTournamentRules.FilePath) ? "-" : session.CurrentTournamentRules.FilePath;
        DrawDataRowFrame(bounds);
        DrawTournamentRulesFieldLabel("SETTINGS", bounds);
        DrawFittedText(filePath, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 170, 42), Color.White, 0.38f);
    }

    private void DrawTextBoxCaret(string text, int caretIndex, Rectangle textBounds, float textScale)
    {
        var clampedCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        var prefix = text[..clampedCaretIndex];
        var measuredText = _font.MeasureString(text);
        var fittedScale = MathF.Min(textScale, MathF.Min(textBounds.Width / Math.Max(1f, measuredText.X), textBounds.Height / Math.Max(1f, measuredText.Y)));
        var x = textBounds.X + MathF.Min(textBounds.Width - 2, _font.MeasureString(prefix).X * fittedScale);
        DrawLine(new Vector2(x, textBounds.Y + 5), new Vector2(x, textBounds.Bottom - 5), 2, new Color(147, 244, 200));
    }

    private void DrawTextBoxSelection(string text, int selectionStart, int selectionLength, Rectangle textBounds, float textScale)
    {
        if (selectionLength <= 0 || selectionStart < 0 || selectionStart >= text.Length) return;
        var end = Math.Min(text.Length, selectionStart + selectionLength);
        var measuredText = _font.MeasureString(text);
        var fittedScale = MathF.Min(textScale, MathF.Min(textBounds.Width / Math.Max(1f, measuredText.X), textBounds.Height / Math.Max(1f, measuredText.Y)));
        var startX = textBounds.X + _font.MeasureString(text[..selectionStart]).X * fittedScale;
        var endX = textBounds.X + _font.MeasureString(text[..end]).X * fittedScale;
        FillRect(
            new Rectangle((int)startX, textBounds.Y + 3, Math.Max(2, (int)MathF.Ceiling(endX - startX)), textBounds.Height - 6),
            new Color(50, 108, 139, 210));
    }

    private int GetTextBoxCaretIndex(int pointX, string text, Rectangle textBounds, float textScale)
    {
        if (string.IsNullOrEmpty(text) || pointX <= textBounds.X)
        {
            return 0;
        }

        var measuredText = _font.MeasureString(text);
        var fittedScale = MathF.Min(textScale, MathF.Min(textBounds.Width / Math.Max(1f, measuredText.X), textBounds.Height / Math.Max(1f, measuredText.Y)));
        var previousX = (float)textBounds.X;
        for (var i = 0; i < text.Length; i++)
        {
            var nextX = textBounds.X + MathF.Min(textBounds.Width - 2, _font.MeasureString(text[..(i + 1)]).X * fittedScale);
            if (pointX < (previousX + nextX) * 0.5f)
            {
                return i;
            }

            previousX = nextX;
        }

        return text.Length;
    }
    private void DrawPropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(TournamentRulesSelectionDialogPropertyBounds.X + 18, y, TournamentRulesSelectionDialogPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPathPropertyRow(Rectangle bounds, string label, string value)
    {
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPathTooltipIfHovered(Rectangle rowBounds, string fullPath, Point mousePoint)
    {
        if (IsPathTooltipHovered(rowBounds, fullPath, mousePoint))
            DrawPathTooltip(StickyNoteKind.TournamentRulesPathHint, rowBounds, fullPath, mousePoint, "FILE とは？", ["対局ルールで利用するファイルの場所です。"]);
    }

    private static bool IsPathTooltipHovered(Rectangle rowBounds, string fullPath, Point mousePoint) =>
        !string.IsNullOrWhiteSpace(fullPath) && fullPath != "-" &&
        (rowBounds.Contains(mousePoint) || PathTooltipBounds(rowBounds).Contains(mousePoint));

    private void DrawBoardSizeButtons(int boardSize, Point mousePoint, int y)
    {
        var labels = new[] { "9 x 9", "13 x 13", "19 x 19" };
        var sizes = new[] { 9, 13, 19 };
        for (var i = 0; i < labels.Length; i++)
        {
            var bounds = BoardSizeButtonBounds(i, y);
            var selected = boardSize == sizes[i];
            DrawTournamentRulesChoiceButton(bounds, labels[i], selected, mousePoint, 0.56f);
        }
    }

    private void DrawRuleKindButtons(GoRuleKind selectedKind, Point mousePoint)
    {
        DrawTournamentRulesChoiceButton(RuleKindButtonBounds(0), "JAPANESE", selectedKind == GoRuleKind.Japanese, mousePoint, 0.44f);
        DrawTournamentRulesChoiceButton(RuleKindButtonBounds(1), "PURE GO", selectedKind == GoRuleKind.PureGo, mousePoint, 0.44f);
        DrawTournamentRulesChoiceButton(RuleKindButtonBounds(2), "CHINESE", selectedKind == GoRuleKind.Chinese, mousePoint, 0.44f);
    }

    private void DrawTournamentRulesKomiStrip(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(AddPanelControlX, 460, 668, 56);
        DrawDataRowFrame(bounds);
        DrawTournamentRulesFieldLabel("KOMI", bounds);
        DrawText(FormatKomi(session.Komi), new Vector2(bounds.X + 176, bounds.Y + 13), Color.White, 0.52f);
        DrawSpinBox(KomiSpinButtonBounds(true), KomiSpinButtonBounds(false), "0.5", mousePoint);
    }

    private void DrawTournamentRulesTimeStrip(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(AddPanelControlX, 532, 668, 56);
        DrawTournamentRulesFieldLabel("TIME", bounds);
        var values = new[] { ((int)session.MainTime.TotalHours).ToString("00"), session.MainTime.Minutes.ToString("00"), session.MainTime.Seconds.ToString("00") };
        var units = new[] { "h", "m", "s" };
        for (var index = 0; index < 3; index++)
        {
            var field = (TournamentRulesNumericField)((int)TournamentRulesNumericField.MainTimeHours + index);
            DrawTournamentRulesNumericTextBox(session, field, values[index], TournamentRulesMainTimePartTextBounds(index), mousePoint, index + 1);
            DrawSpinBox(MainTimeSpinButtonBounds(index, true), MainTimeSpinButtonBounds(index, false), units[index], mousePoint);
            if (index < 2)
            {
                var colonBounds = TournamentRulesMainTimeColonBounds(index);
                DrawFittedText(":", colonBounds, new Color(210, 218, 214), 0.46f);
            }
        }
    }

    private void DrawTournamentRulesMoveLimitStrip(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(AddPanelControlX, 604, 668, 56);
        DrawTournamentRulesFieldLabel("MOVES", bounds);
        DrawTournamentRulesNumericTextBox(session, TournamentRulesNumericField.MoveLimit, session.MoveLimit.ToString(), TournamentRulesMoveLimitTextBounds, mousePoint, 4);
        var amounts = new[] { "100", "10", "1" };
        for (var index = 0; index < amounts.Length; index++)
        {
            DrawSpinBox(MoveLimitSpinButtonBounds(index, true), MoveLimitSpinButtonBounds(index, false), amounts[index], mousePoint);
        }
    }

    private void DrawTournamentRulesNumericTextBox(GoAppSession session, TournamentRulesNumericField field, string value, Rectangle textBounds, Point mousePoint, int tabIndex)
    {
        var active = session.ActiveTournamentRulesNumericField == field;
        var text = active ? session.TournamentRulesNumericDraft : value;
        DrawTournamentRulesTabNavigationHint(textBounds, session, tabIndex);
        DrawTournamentRulesTextInputSurface(textBounds, active, textBounds.Contains(mousePoint));
        var contentBounds = new Rectangle(textBounds.X + 8, textBounds.Y + 4, textBounds.Width - 16, textBounds.Height - 8);
        if (active) DrawTextBoxSelection(text, session.TournamentRulesNumericSelectionStart, session.TournamentRulesNumericSelectionLength, contentBounds, 0.42f);
        DrawFittedText(text, contentBounds, Color.White, 0.42f);
        if (active) DrawTextBoxCaret(text, session.TournamentRulesNumericCaretIndex, contentBounds, 0.42f);
    }

    private void DrawTournamentRulesChoiceButton(
        Rectangle bounds,
        string label,
        bool selected,
        Point mousePoint,
        float scale)
    {
        var hovered = bounds.Contains(mousePoint);
        var background = selected
            ? new Color(38, 91, 78)
            : hovered
                ? new Color(42, 53, 61)
                : new Color(27, 35, 42);
        DrawTournamentRulesRoundedButton(
            bounds,
            background,
            selected ? new Color(190, 255, 229) : hovered ? new Color(128, 160, 164) : new Color(73, 91, 98));
        DrawFittedText(label, new Rectangle(bounds.X + 10, bounds.Y + 7, bounds.Width - 20, bounds.Height - 14), Color.White, scale);
    }

    private void DrawTournamentRulesFieldLabel(string label, Rectangle rowBounds)
    {
        const float preferredScale = 0.38f;
        const int labelRightGap = 20;
        var labelBounds = new Rectangle(
            AddPanelControlX,
            rowBounds.Y,
            132 - labelRightGap,
            rowBounds.Height);
        var measured = _font.MeasureString(label);
        var scale = MathF.Min(
            preferredScale,
            MathF.Min(
                labelBounds.Width / Math.Max(1f, measured.X),
                (labelBounds.Height - 8) / Math.Max(1f, measured.Y)));
        var size = measured * scale;
        DrawText(
            label,
            new Vector2(labelBounds.X, labelBounds.Center.Y - size.Y / 2),
            new Color(180, 195, 195),
            scale);
    }

    private void DrawTournamentRulesAdjustmentButton(Rectangle bounds, string label, Point mousePoint, float scale)
    {
        var hovered = bounds.Contains(mousePoint);
        DrawTournamentRulesRoundedButton(
            bounds,
            hovered ? new Color(53, 66, 75) : new Color(31, 40, 47),
            hovered ? new Color(184, 220, 216) : new Color(105, 127, 134));
        DrawFittedText(label, new Rectangle(bounds.X + 6, bounds.Y + 5, bounds.Width - 12, bounds.Height - 10), Color.White, scale);
    }

    private void DrawTournamentRulesRoundedButton(Rectangle bounds, Color background, Color border)
    {
        DrawRoundedFill(bounds, 7, border);
        DrawRoundedFill(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), 5, background);
    }

    private void DrawRoundedFill(Rectangle bounds, int radius, Color color)
    {
        radius = Math.Min(radius, Math.Min(bounds.Width, bounds.Height) / 2);
        FillRect(new Rectangle(bounds.X + radius, bounds.Y, bounds.Width - radius * 2, bounds.Height), color);
        FillRect(new Rectangle(bounds.X, bounds.Y + radius, bounds.Width, bounds.Height - radius * 2), color);
        DrawCircle(new Vector2(bounds.X + radius, bounds.Y + radius), radius, color);
        DrawCircle(new Vector2(bounds.Right - radius, bounds.Y + radius), radius, color);
        DrawCircle(new Vector2(bounds.X + radius, bounds.Bottom - radius), radius, color);
        DrawCircle(new Vector2(bounds.Right - radius, bounds.Bottom - radius), radius, color);
    }

    private void DrawTournamentRulesTabNavigationHint(Rectangle bounds, GoAppSession session, int tabIndex)
    {
        var activeIndex = session.IsTournamentRulesDisplayNameEditing
            ? 0
            : session.ActiveTournamentRulesNumericField switch
            {
                TournamentRulesNumericField.MainTimeHours => 1,
                TournamentRulesNumericField.MainTimeMinutes => 2,
                TournamentRulesNumericField.MainTimeSeconds => 3,
                TournamentRulesNumericField.MoveLimit => 4,
                _ => -1,
            };
        DrawTabNavigationHint(bounds, tabIndex, activeIndex, 5);
    }

    private void DrawTabNavigationHint(Rectangle bounds, int tabIndex, int activeIndex, int stopCount)
    {
        if (activeIndex < 0 || tabIndex == activeIndex || stopCount < 2)
        {
            return;
        }

        var isPrevious = tabIndex == (activeIndex + stopCount - 1) % stopCount;
        var isNext = tabIndex == (activeIndex + 1) % stopCount;
        if (!isPrevious && !isNext)
        {
            return;
        }
        var hintText = isPrevious ? "SHIFT + TAB" : "TAB";
        var hintWidth = isPrevious ? 132 : 56;
        var hintHeight = 28;
        var hintBounds = new Rectangle(bounds.X - hintWidth - 6, bounds.Y - hintHeight - 6, hintWidth, hintHeight);
        DrawRoundedFill(hintBounds, 6, new Color(4, 6, 8, 235));
        DrawFittedText(
            hintText,
            new Rectangle(hintBounds.X + 4, hintBounds.Y + 2, hintBounds.Width - 8, hintBounds.Height - 4),
            Color.White,
            MinimumTextScale);
    }

    private void DrawTournamentRulesTextInputSurface(Rectangle bounds, bool active, bool hovered)
    {
        var background = active
            ? new Color(63, 128, 106)
            : hovered
                ? new Color(35, 47, 53)
                : new Color(22, 29, 34);
        var underline = active
            ? new Color(190, 255, 229)
            : hovered
                ? new Color(128, 174, 168)
                : new Color(70, 91, 96);

        FillRect(bounds, background);
        FillRect(new Rectangle(bounds.X, bounds.Bottom - (active ? 3 : 2), bounds.Width, active ? 3 : 2), underline);
    }

    private void DrawSetupPlayerKindRow(GoStone stone, GoPlayerKind selectedKind, Point mousePoint, int y, string computerLabel = "COMPUTER")
    {
        var rowBounds = new Rectangle(1144, y - 14, 668, 72);
        DrawIconStone(new Vector2(rowBounds.X + 36, rowBounds.Center.Y), 18, stone == GoStone.Black);

        var humanBounds = PlayerKindButtonBounds(0, y);
        var computerBounds = PlayerKindButtonBounds(1, y);
        var bounds = PlayerKindSegmentBounds(y);

        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, 90));
        FillRect(bounds, new Color(33, 43, 52));
        DrawSegmentedPlayerKindButton(humanBounds, "HUMAN", selectedKind == GoPlayerKind.Human, humanBounds.Contains(mousePoint));
        DrawSegmentedPlayerKindButton(computerBounds, computerLabel, selectedKind == GoPlayerKind.Computer, computerBounds.Contains(mousePoint));
        DrawRect(bounds, 2, new Color(126, 150, 164));
    }

    private void DrawSegmentedPlayerKindButton(Rectangle bounds, string label, bool selected, bool hovered)
    {
        var fill = selected ? new Color(31, 151, 112) : hovered ? new Color(44, 59, 70) : new Color(33, 43, 52);
        var textColor = selected ? Color.White : new Color(202, 213, 211);
        FillRect(bounds, fill);

        var measured = _font.MeasureString(label);
        var fittedScale = MathF.Min(0.52f, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
    }

    private void DrawSetupPlayerSelector(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var playerKind = stone == GoStone.Black ? session.BlackPlayerKind : session.WhitePlayerKind;
        if (playerKind == GoPlayerKind.Human)
        {
            DrawHumanPlayerNameTextBox(session, stone, mousePoint, y);
            return;
        }

        var selectedIndex = stone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        var engineName = selectedIndex >= 0 && selectedIndex < session.GtpEngineProfiles.Count
            ? session.GtpEngineProfiles[selectedIndex].DisplayName
            : "No engine";
        DrawPlayerSelector(PlayerSelectorLayout.CreateComputerEngineSelector(y) with { Value = engineName }, mousePoint);
    }

    private void DrawHumanPlayerNameTextBox(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var bounds = HumanPlayerNameRowBounds(y);
        var active = session.ActiveHumanPlayerNameStone == stone;
        var text = active ? session.HumanPlayerNameDraft : session.GetHumanPlayerName(stone);
        DrawResultLabel(new Rectangle(bounds.X + 20, bounds.Y - 6, bounds.Width - 40, bounds.Height + 12), "NAME", new Color(76, 91, 126));
        var textBounds = HumanPlayerNameTextBounds(y);
        DrawTournamentRulesTextInputSurface(textBounds, active, bounds.Contains(mousePoint));
        var humanStops = new[] { GoStone.Black, GoStone.White }
            .Where(candidate => session.GetPlayerKind(candidate) == GoPlayerKind.Human)
            .ToArray();
        DrawTabNavigationHint(
            bounds,
            Array.IndexOf(humanStops, stone),
            session.ActiveHumanPlayerNameStone is { } activeStone ? Array.IndexOf(humanStops, activeStone) : -1,
            humanStops.Length);
        if (active)
            DrawTextBoxSelection(text, session.HumanPlayerNameSelectionStart, session.HumanPlayerNameSelectionLength, textBounds, 0.42f);
        DrawFittedText(text, textBounds, Color.White, 0.42f);
        if (active) DrawTextBoxCaret(text, session.HumanPlayerNameCaretIndex, textBounds, 0.42f);
    }

    private const int AddPanelControlX = 626;

    private const int AddPanelBoardSizeButtonY = 391;

    private const int BlackPlayerKindButtonY = 710;

    private const int WhitePlayerKindButtonY = 814;

    private const int BlackEngineButtonY = 768;

    private const int WhiteEngineButtonY = 872;
    private const int PonnukiBlackPlayerKindButtonY = 646;
    private const int PonnukiBlackEngineButtonY = 704;
    private const int PonnukiWhitePlayerKindButtonY = 750;
    private const int PonnukiWhiteEngineButtonY = 808;

    private static Rectangle BoardSizeButtonBounds(int index, int y) => new(AddPanelControlX + 132 + index * 180, y, 164, 50);
    private static Rectangle PathTooltipBounds(Rectangle rowBounds)
    {
        const int height = 370;
        // 行と同じ横幅に制限する。エンジン一覧には決して重ねない。
        // 画面下端へ出すことで、EXE と WORKDIR の各行も覆わない。
        return new Rectangle(
            rowBounds.X,
            VirtualScreen.Height - height - 10,
            rowBounds.Width,
            height);
    }

    private static Rectangle PathTooltipCopyButtonBounds(Rectangle rowBounds)
    {
        return PathTooltipCopyButtonBoundsFromPopup(PathTooltipBounds(rowBounds));
    }

    private static Rectangle PathTooltipCopyButtonBoundsFromPopup(Rectangle popupBounds) =>
        new(popupBounds.Right - 132, popupBounds.Bottom - 48, 108, 34);

    private static Rectangle RuleKindButtonBounds(int index) => new(AddPanelControlX + 132 + index * 180, 319, 164, 50);

    private static Rectangle KomiSpinButtonBounds(bool up) => new(AddPanelControlX + 300, up ? 462 : 498, 88, 14);

    private static Rectangle TournamentRulesMainTimePartTextBounds(int index) => new(AddPanelControlX + 132 + index * 112, 540, 52, 40);

    private static Rectangle TournamentRulesMainTimeColonBounds(int index) => new(AddPanelControlX + 230 + index * 112, 544, 14, 28);

    private static Rectangle TournamentRulesMoveLimitTextBounds => new(AddPanelControlX + 132, 612, 176, 40);

    private static Rectangle MainTimeSpinButtonBounds(int index, bool up) => new(AddPanelControlX + 188 + index * 112, up ? 534 : 570, 40, 14);

    private static Rectangle MoveLimitSpinButtonBounds(int index, bool up) => new(AddPanelControlX + 324 + index * 92, up ? 606 : 642, 76, 14);

    private static Rectangle PlayerKindButtonBounds(int index, int y) => new(GameOverValueX + index * 236, y, 236, 52);

    private static Rectangle PlayerKindSegmentBounds(int y) => new(GameOverValueX, y, 472, 52);

    private static Rectangle HumanPlayerNameRowBounds(int y) => new(1144, y - 4, 668, 44);

    private static Rectangle HumanPlayerNameTextBounds(int y) => new(GameOverValueX, y + 2, 468, 32);
    private static Rectangle StartPlayingButtonBounds => new(1658, 920, 154, 56);
    private static Rectangle ChangeAppProviderButtonBounds => new(1658, 556, 154, 52);
    private static Rectangle AppProviderGameSettingsButtonBounds => new(1328, 556, 320, 52);
    private static Rectangle PonnukiProviderSeedAutoChangeBounds => new(1164, 870, 200, 32);
    private static Rectangle PonnukiPlayer1SeedAutoChangeBounds => new(1378, 870, 200, 32);
    private static Rectangle PonnukiPlayer2SeedAutoChangeBounds => new(1592, 870, 200, 32);

    private static Rectangle ImportSgfButtonBounds => new(1492, 184, 320, 56);

    private static Rectangle SetupBackToTitleButtonBounds => new(1642, 104, 170, 52);
    private static Rectangle LocalUseButtonBounds => new(508, 404, 438, 300);
    private static Rectangle TitleMenuBackButtonBounds => new(1260, 316, 152, 54);
    private static Rectangle TitleAppProviderEngineDisplayBounds => new(570, 466, 780, 56);
    private static Rectangle TitleAppProviderEngineTextBounds => new(
        TitleAppProviderEngineDisplayBounds.X + 142,
        TitleAppProviderEngineDisplayBounds.Y + 7,
        TitleAppProviderEngineDisplayBounds.Width - 142,
        42);
    private static Rectangle TitleAppProviderStartButtonBounds => new(1198, 826, 152, 54);
    private static Rectangle TitleAppProviderRecheckButtonBounds => new(828, 826, 340, 54);
    private static Rectangle TitleHomeLocalButtonBounds => new(500, 390, 400, 126);
    private static Rectangle TitleHomeCgosButtonBounds => new(500, 536, 400, 126);
    private static Rectangle TitleAppBounds(int index) => new(950, 390 + index * 100, 440, 84);

    public static bool GetTitleMenuBackButtonHit(Point point) => TitleMenuBackButtonBounds.Contains(point);
    public static bool GetTitleAppProviderStartButtonHit(Point point) => TitleAppProviderStartButtonBounds.Contains(point);
    public static bool GetTitleAppProviderRecheckButtonHit(Point point) => TitleAppProviderRecheckButtonBounds.Contains(point);

    public static bool GetTitleAppProviderEngineSelectButtonHit(Point point) =>
        TitleAppProviderEngineTextBounds.Contains(point);

    private void DrawLocalAppsIntermissionSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawCommandButton(SetupBackToTitleButtonBounds, "BACK TO TITLE", false, mousePoint, scale: 0.32f);

        DrawVerticalResultSection(new Rectangle(1144, 184, 668, 176), "LOCAL APPS", new Color(99, 76, 48));
        DrawResultRow(new Rectangle(1164, 236, 628, 56), "APP", "PONNUKI", new Color(73, 57, 39), Color.White);
        DrawResultRow(
            new Rectangle(1164, 296, 628, 48),
            "STATUS",
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage) ? "INTERMISSION" : "PROVIDER ERROR",
            new Color(58, 48, 38),
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage) ? new Color(255, 210, 128) : new Color(255, 145, 151));

        DrawVerticalResultSection(new Rectangle(1144, 392, 668, 224), "APP PROVIDER ENGINE", new Color(66, 104, 116));
        DrawDynamicOptionText("アプリ提供エンジン", new Rectangle(1164, 410, 300, 34), new Color(180, 195, 195), 0.30f);
        DrawResultRow(
            new Rectangle(1164, 466, 628, 64),
            "PROVIDER",
            session.SelectedAppProviderEngineDisplayName,
            new Color(39, 68, 65),
            Color.White);
        DrawDynamicOptionText(
            string.IsNullOrWhiteSpace(session.LocalAppsErrorMessage)
                ? "初期局面とポン抜きの進行を提供します。"
                : session.LocalAppsErrorMessage,
            new Rectangle(1164, 536, 628, 22),
            new Color(180, 195, 195),
            0.30f);
        DrawCommandButton(AppProviderGameSettingsButtonBounds, "GAME SETTINGS", false, mousePoint, scale: 0.32f);
        DrawCommandButton(ChangeAppProviderButtonBounds, "CHANGE", false, mousePoint, scale: 0.28f);
        DrawVerticalResultSection(new Rectangle(1144, 632, 668, 216), "PLAYERS", new Color(76, 91, 126));
        DrawSetupPlayerRow(session, GoStone.Black, mousePoint, PonnukiBlackPlayerKindButtonY);
        DrawSetupPlayerRow(session, GoStone.White, mousePoint, PonnukiWhitePlayerKindButtonY);

        DrawVerticalResultSection(new Rectangle(1144, 856, 668, 52), "SEED AUTO", new Color(112, 76, 48), labelWidth: 56);
        DrawCommandButton(PonnukiProviderSeedAutoChangeBounds, session.PonnukiProviderSeedAutoChange ? "[x] PROVIDER" : "[ ] PROVIDER", session.PonnukiProviderSeedAutoChange, mousePoint, scale: 0.22f);
        DrawCommandButton(PonnukiPlayer1SeedAutoChangeBounds, session.PonnukiBlackPlayerSeedAutoChange ? "[x] BLACK" : "[ ] BLACK", session.PonnukiBlackPlayerSeedAutoChange, mousePoint, enabled: session.CanAutoChangePonnukiPlayer1Seed, scale: 0.22f);
        DrawCommandButton(PonnukiPlayer2SeedAutoChangeBounds, session.PonnukiWhitePlayerSeedAutoChange ? "[x] WHITE" : "[ ] WHITE", session.PonnukiWhitePlayerSeedAutoChange, mousePoint, enabled: session.CanAutoChangePonnukiPlayer2Seed, scale: 0.22f);

        DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));
        DrawCommandButton(
            StartPlayingButtonBounds,
            session.CanStartPlaying ? "START" : "ENGINE REQUIRED",
            false,
            mousePoint,
            enabled: session.CanStartPlaying,
            scale: session.CanStartPlaying ? 0.48f : 0.28f);
    }
    public static bool GetTitleHomeLocalButtonHit(Point point) => TitleHomeLocalButtonBounds.Contains(point);
    public static bool GetTitleHomeCgosButtonHit(Point point) => TitleHomeCgosButtonBounds.Contains(point);

    public static int? GetTitleAppHit(Point point)
    {
        for (var index = 0; index < 1; index++)
        {
            if (TitleAppBounds(index).Contains(point))
            {
                return index;
            }
        }

        return null;
    }
    private static Rectangle ReturnToSetupButtonBounds => new(1492, 132, 320, 56);

    private static Rectangle ExportSgfButtonBounds => new(1164, 910, 306, 56);

    private static Rectangle LocalGameOverReviewButtonBounds => new(1486, 910, 306, 56);

    private static Rectangle PassButtonBounds => new(1144, 920, 320, 72);

    private static Rectangle ResignButtonBounds => new(1492, 920, 320, 72);

    private static Rectangle CancelPlayingButtonBounds => new(1144, 920, 668, 72);
    private static GoPlayerKind? GetPlayerKindButtonHit(Point point, int y)
    {
        if (PlayerKindButtonBounds(0, y).Contains(point))
        {
            return GoPlayerKind.Human;
        }

        return PlayerKindButtonBounds(1, y).Contains(point) ? GoPlayerKind.Computer : null;
    }

    private static string PlayerKindLabel(GoPlayerKind playerKind) => playerKind == GoPlayerKind.Human ? "Human" : "Computer";

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        var totalHours = (int)elapsed.TotalHours;
        return totalHours > 0
            ? $"{totalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private static string FormatMainTime(TimeSpan mainTime) =>
        mainTime == TimeSpan.Zero ? "NO LIMIT" : FormatElapsedTime(mainTime);

    private static string FormatMoveLimit(int moveLimit) =>
        moveLimit <= 0 ? "NO LIMIT" : moveLimit.ToString();

    private static string GetMoveThinkingText(GoAppSession session)
    {
        var text = $"{session.NextMoveNumber}手目を思考中";
        return session.MoveLimit <= 0 ? text : $"{text} / {session.MoveLimit}";
    }

    private static string FormatGameEndMoveCount(int playedMoveCount) => $"{playedMoveCount}手で終局";

    private static string FormatRuleKind(GoRuleKind ruleKind) => ruleKind switch
    {
        GoRuleKind.PureGo => "PURE GO",
        GoRuleKind.Japanese => "JAPANESE",
        GoRuleKind.Chinese => "CHINESE",
        _ => ruleKind.ToString().ToUpperInvariant(),
    };

    private static string FormatCalculationResult(GoAppSession session)
    {
        const string pureGoPrefix = "PURE GO ";
        var result = string.IsNullOrWhiteSpace(session.GameOverReason) ? "GAME OVER" : session.GameOverReason;
        return result.StartsWith(pureGoPrefix, StringComparison.Ordinal)
            ? result[pureGoPrefix.Length..]
            : result;
    }

    private static string FormatKomi(decimal komi) => komi.ToString("0.0");
    private const float MinimumCommandButtonLabelScale = 0.36f;
    private const float CommandButtonLabelScaleMultiplier = 1.25f;

    private void DrawCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled = true, float scale = 0.62f)
    {
        var hovered = enabled && bounds.Contains(mousePoint);
        var fill = !enabled ? new Color(24, 27, 31) : selected ? new Color(31, 151, 112) : hovered ? new Color(58, 82, 94) : new Color(36, 48, 58);
        var border = !enabled ? new Color(43, 50, 56) : selected ? new Color(151, 255, 215) : hovered ? new Color(178, 219, 226) : new Color(126, 150, 164);
        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, enabled ? 95 : 28));
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);
        if (enabled)
        {
            DrawRect(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), 1, selected ? new Color(215, 255, 238, 95) : new Color(255, 255, 255, hovered ? 70 : 36));
        }

        var textColor = enabled ? Color.White : new Color(91, 100, 106);
        var requestedScale = MathF.Max(MinimumCommandButtonLabelScale, scale * CommandButtonLabelScaleMultiplier);
        if (label.All(character => _font.Characters.Contains(character)))
        {
            var measured = _font.MeasureString(label);
            var fittedScale = MathF.Min(requestedScale, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
            var size = measured * fittedScale;
            DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
        }
        else
        {
            DrawDynamicOptionText(label, new Rectangle(bounds.X + 10, bounds.Y + 5, bounds.Width - 20, bounds.Height - 10), textColor, requestedScale);
        }
    }

    public void DrawBoardLensBanner(string lensName, string lensAlias, string guide, float opacity, float compactProgress)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        compactProgress = Math.Clamp(compactProgress, 0f, 1f);
        compactProgress = compactProgress * compactProgress * (3f - (2f * compactProgress));
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        var hasAlias = !string.IsNullOrWhiteSpace(lensAlias);
        var largeBounds = new Rectangle(560, 48, 800, 122);
        var compactBounds = hasAlias
            ? new Rectangle(209, 4, 670, 88)
            : new Rectangle(209, 10, 670, 72);
        var bounds = new Rectangle(
            (int)MathF.Round(MathHelper.Lerp(largeBounds.X, compactBounds.X, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Y, compactBounds.Y, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Width, compactBounds.Width, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Height, compactBounds.Height, compactProgress)));
        var shadowAlpha = (int)(150f * opacity);
        var panelAlpha = (int)(235f * opacity);
        var textAlpha = (int)(255f * opacity);

        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, shadowAlpha));
        FillRect(bounds, new Color(13, 24, 31, panelAlpha));
        DrawRect(bounds, 2, new Color(125, 225, 255, textAlpha));
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 4), new Color(125, 225, 255, textAlpha));

        var measured = _font.MeasureString(lensName);
        var largeNameScale = MathF.Min(0.58f, (largeBounds.Width - 48f) / Math.Max(1f, measured.X));
        var compactNameScale = MathF.Min(0.34f, (compactBounds.Width - 28f) / Math.Max(1f, measured.X));
        var scale = MathHelper.Lerp(largeNameScale, compactNameScale, compactProgress);
        var size = measured * scale;
        var largeNameY = hasAlias ? bounds.Y + 15f : bounds.Y + 43f;
        var nameY = MathHelper.Lerp(largeNameY, bounds.Y + 5f, compactProgress);
        DrawText(
            lensName,
            new Vector2(bounds.Center.X - size.X / 2f, nameY),
            new Color(235, 251, 255, textAlpha),
            scale);

        if (hasAlias)
        {
            var aliasScale = MathHelper.Lerp(0.34f, 0.25f, compactProgress);
            var aliasSize = _font.MeasureString(lensAlias) * aliasScale;
            var aliasY = MathHelper.Lerp(bounds.Y + 51f, bounds.Y + 34f, compactProgress);
            DrawText(
                lensAlias,
                new Vector2(bounds.Center.X - aliasSize.X / 2f, aliasY),
                new Color(159, 215, 225, textAlpha),
                aliasScale);
        }

        var guideScale = hasAlias
            ? MathHelper.Lerp(0.28f, 0.23f, compactProgress)
            : MathHelper.Lerp(0.30f, 0.27f, compactProgress);
        var guideSize = _font.MeasureString(guide) * guideScale;
        var compactGuideY = hasAlias ? bounds.Y + 61f : bounds.Y + 41f;
        var guideY = MathHelper.Lerp(bounds.Y + 82f, compactGuideY, compactProgress);
        DrawText(
            guide,
            new Vector2(bounds.Center.X - guideSize.X / 2f, guideY),
            new Color(255, 220, 128, textAlpha),
            guideScale);

        _spriteBatch.End();
    }
    private void DrawPathTooltip(
        StickyNoteKind kind,
        Rectangle rowBounds,
        string fullPath,
        Point mousePoint,
        string heading,
        IReadOnlyList<string> descriptionLines)
    {
        // 長いパスは表示幅を超えたために縮小せず、区切り文字で改行する。
        var lines = descriptionLines.Concat(WrapPathForTooltip(fullPath, 72).Take(2)).ToArray();
        DrawStickyNote(
            kind,
            new Vector2(rowBounds.Center.X, rowBounds.Bottom),
            new Color(147, 244, 200),
            new Color(87, 157, 128),
            heading,
            lines,
            bodyLineSpacing: 32,
            anchorBounds: rowBounds);
        if (StickyNotePlacementStrategies.TryGetPlacement(
                _stickyNoteScreen,
                kind,
                new StickyNotePlacementContext(Vector2.Zero, rowBounds),
                out var placement))
            DrawCommandButton(PathTooltipCopyButtonBoundsFromPopup(placement.Bounds), "COPY", false, mousePoint, scale: 0.34f);
    }

    private static IEnumerable<string> WrapPathForTooltip(string path, int maximumLength)
    {
        while (path.Length > maximumLength)
        {
            var split = path.LastIndexOfAny(['\\', '/'], Math.Min(maximumLength, path.Length - 1));
            if (split <= 0) split = maximumLength;
            yield return path[..(split + (path[split] is '\\' or '/' ? 1 : 0))];
            path = path[(split + (path[split] is '\\' or '/' ? 1 : 0))..];
        }
        yield return path;
    }

    private void DrawPlayerSelector(PlayerSelector selector, Point mousePoint)
    {
        if (selector.Bounds.X == 1144 && selector.Bounds.Width == 668)
        {
            // Player 行は値欄の左端を全行で GameOverValueX に揃える。
            // 石アイコンが黒白を示し、セクション名も PLAYERS なので行内ラベルは重複表示しない。
            var isBlack = selector.Label.StartsWith("BLACK", StringComparison.Ordinal);
            DrawIconStone(new Vector2(selector.Bounds.X + 34, selector.Bounds.Center.Y), 13, isBlack);
            if (selector.IsComputer is { } isComputer)
                DrawPlayerRoleFaceIcon(new Vector2(selector.Bounds.X + 76, selector.Bounds.Center.Y), isComputer);
            var fieldBounds = new Rectangle(GameOverValueX, selector.Bounds.Y + 6, selector.Bounds.Right - GameOverValueX - 34, selector.Bounds.Height - 12);
            var hovered = selector.Enabled && selector.Bounds.Contains(mousePoint);
            var valueBounds = hovered
                ? new Rectangle(fieldBounds.X, fieldBounds.Y, fieldBounds.Width - 122, fieldBounds.Height)
                : fieldBounds;
            DrawFittedText(selector.Value, valueBounds, Color.White, 0.42f);
            _playerSelectorLinkUnderline.Draw(fieldBounds, hovered, this);
            if (hovered)
            {
                // 操作ヒントはアンダーライン終端の近くに、読みやすい反転プレートで表示する。
                var hintBounds = new Rectangle(fieldBounds.Right - 108, fieldBounds.Bottom - 28, 100, 26);
                DrawRoundedFill(hintBounds, 6, new Color(185, 196, 255));
                DrawSharpCenteredFittedText("CHANGE", hintBounds, new Color(15, 20, 31), 0.34f);
            }
            return;
        }

        DrawDataRowFrame(selector.Bounds);

        DrawFittedText(selector.Label, selector.LabelBounds, new Color(158, 178, 178), 0.36f);
        DrawFittedText(selector.Value, selector.ValueBounds, Color.White, 0.52f);
        DrawCommandButton(selector.BrowseButtonBounds, selector.ButtonLabel, false, mousePoint, enabled: selector.Enabled, scale: PlayerSelectorLayout.SelectButtonLabelScale);
    }

    /// <summary>石の右側に、人間またはコンピューターの操作主体を示す顔アイコンを描きます。</summary>
    private void DrawPlayerRoleFaceIcon(Vector2 center, bool isComputer)
    {
        var color = isComputer ? new Color(125, 225, 255) : new Color(255, 211, 138);
        if (isComputer)
        {
            var head = new Rectangle((int)center.X - 10, (int)center.Y - 10, 20, 20);
            FillRect(head, new Color(28, 49, 61));
            DrawRect(head, 2, color);
            DrawCircle(center + new Vector2(-4, -2), 2, color);
            DrawCircle(center + new Vector2(4, -2), 2, color);
            DrawLine(center + new Vector2(-5, 5), center + new Vector2(5, 5), 2, color);
            DrawLine(center + new Vector2(0, -10), center + new Vector2(0, -14), 2, color);
            DrawCircle(center + new Vector2(0, -15), 2, color);
            return;
        }

        DrawCrispCircleOutline(center + new Vector2(0, -2), 12, 2, color);
        // （＾～＾）を20px級に収めた、山形の目と波形の口。
        DrawLine(center + new Vector2(-6, -4), center + new Vector2(-4, -7), 2, color);
        DrawLine(center + new Vector2(-4, -7), center + new Vector2(-2, -4), 2, color);
        DrawLine(center + new Vector2(2, -4), center + new Vector2(4, -7), 2, color);
        DrawLine(center + new Vector2(4, -7), center + new Vector2(6, -4), 2, color);
        DrawLine(center + new Vector2(-6, 3), center + new Vector2(-2, 1), 2, color);
        DrawLine(center + new Vector2(-2, 1), center + new Vector2(2, 4), 2, color);
        DrawLine(center + new Vector2(2, 4), center + new Vector2(6, 1), 2, color);
    }

    private void DrawCrispCircleOutline(Vector2 center, float radius, int thickness, Color color)
    {
        const int segmentCount = 24;
        var previous = center + new Vector2(radius, 0);
        for (var index = 1; index <= segmentCount; index++)
        {
            var angle = MathHelper.TwoPi * index / segmentCount;
            var current = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(previous, current, thickness, color);
            previous = current;
        }
    }

    private void DrawDataRowFrame(Rectangle bounds, bool active = false, bool hovered = false)
    {
        var fill = active ? new Color(28, 41, 45) : hovered ? new Color(28, 36, 43) : new Color(21, 28, 34);
        var line = active ? new Color(104, 191, 165) : hovered ? new Color(58, 77, 85) : new Color(43, 56, 63);
        FillRect(bounds, fill);
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), line);
        FillRect(new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), line);
        if (active)
        {
            FillRect(new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), new Color(99, 223, 185));
        }
    }

    private void DrawInfoStrip(int x, int y, string label, string value)
    {
        var bounds = new Rectangle(x, y, 668, 72);
        DrawResultLabel(new Rectangle(x + 20, y, bounds.Width - 40, bounds.Height), label, new Color(62, 112, 105));
        DrawFittedText(value, new Rectangle(GameOverValueX, y + 12, bounds.Right - GameOverValueX - 20, bounds.Height - 24), Color.White, 0.58f);
    }

    private void DrawSectionTitle(string title, int x, int y, Color accentColor)
    {
        FillRect(new Rectangle(x, y + 2, 3, 30), accentColor);
        DrawText(title, new Vector2(x + 16, y), new Color(180, 195, 195), 0.5f);
        DrawLine(new Vector2(x, y + 40), new Vector2(x + 668, y + 40), 1, new Color(58, 78, 86));
    }

    private void DrawResultRow(Rectangle bounds, string label, string value, Color chipColor, Color valueColor)
    {
        DrawResultLabel(bounds, label, chipColor);
        DrawFittedText(value, new Rectangle(GameOverValueX, bounds.Y + 6, bounds.Right - GameOverValueX - 18, bounds.Height - 12), valueColor, 0.58f);
    }

    private void DrawCalculationMethodRow(Rectangle bounds, GoAppSession session)
    {
        if (session.RuleKind == GoRuleKind.PureGo)
        {
            DrawResultRow(bounds, "METHOD", "PURE GO", new Color(39, 68, 65), Color.White);
            return;
        }

        DrawResultLabel(bounds, "METHOD", new Color(39, 68, 65));
        var valueWidth = bounds.Right - GameOverValueX - 18;
        DrawFittedText("PURE GO", new Rectangle(GameOverValueX, bounds.Y + 1, valueWidth, 32), Color.White, 0.48f);
        DrawFittedText($"RULES: {FormatRuleKind(session.RuleKind)}", new Rectangle(GameOverValueX, bounds.Y + 34, valueWidth, 18), new Color(118, 139, 143), 0.24f);
    }

    private void DrawResultLabel(Rectangle bounds, string label, Color accentColor)
    {
        const int accentHeight = 28;
        // Intermission のラベル列は、section 内の行種別に関係なく同じグリッドへそろえる。
        FillRect(new Rectangle(bounds.X - 22, bounds.Center.Y - accentHeight / 2, 3, accentHeight), accentColor);
        DrawText(label, new Vector2(bounds.X - 8, bounds.Y + 14), new Color(180, 195, 195), 0.38f);
    }

    private void DrawCalculationResultRow(Rectangle bounds, GoAppSession session)
    {
        DrawResultLabel(bounds, "RESULT", new Color(80, 48, 38));

        var result = FormatCalculationResult(session);
        var black = result.StartsWith("BLACK ", StringComparison.Ordinal);
        var white = result.StartsWith("WHITE ", StringComparison.Ordinal);
        if (black || white)
        {
            DrawStoneValue(GameOverValueX, bounds.Center.Y, result[6..], black, new Color(99, 223, 185));
            return;
        }

        DrawFittedText(result, new Rectangle(GameOverValueX, bounds.Y + 6, bounds.Right - GameOverValueX - 18, bounds.Height - 12), new Color(99, 223, 185), 0.58f);
    }

    private void DrawCurrentStoneResultRow(Rectangle bounds, GoAppSession session)
    {
        DrawResultLabel(bounds, "RESULT", new Color(80, 48, 38));

        var difference = session.BlackStoneCount - session.WhiteStoneCount;
        if (difference == 0)
        {
            DrawText("EVEN", new Vector2(GameOverValueX, bounds.Center.Y - 14), new Color(99, 223, 185), 0.5f);
            return;
        }

        DrawStoneValue(GameOverValueX, bounds.Center.Y, $"+{Math.Abs(difference)}", difference > 0, new Color(99, 223, 185));
    }

    private void DrawStoneValue(int x, int centerY, string value, bool black, Color valueColor)
    {
        DrawIconStone(new Vector2(x + 18, centerY), 16, black);
        DrawText(value, new Vector2(x + 44, centerY - 14), valueColor, 0.5f);
    }

    private void DrawAgehamaStrip(GoAppSession session, int y = 540, bool minimal = false)
    {
        var bounds = new Rectangle(1144, y, 668, 56);
        if (!minimal)
        {
            FillRect(bounds, new Color(24, 31, 37));
            DrawRect(bounds, 1, new Color(70, 85, 94));
        }
        if (minimal)
        {
            DrawResultLabel(new Rectangle(bounds.X + 20, bounds.Y, bounds.Width - 40, bounds.Height), "AGEHAMA", new Color(66, 104, 116));
        }
        else
        {
            DrawText("AGEHAMA", new Vector2(bounds.X + 20, bounds.Y + 16), new Color(180, 195, 195), 0.46f);
        }
        var firstValueX = minimal ? GameOverValueX : bounds.X + 220;
        var secondValueX = minimal ? GameOverSecondValueX : bounds.X + 430;
        if (minimal)
        {
            DrawStoneValue(firstValueX, bounds.Center.Y, session.BlackAgehama.ToString(), black: true, valueColor: Color.White);
            DrawStoneValue(secondValueX, bounds.Center.Y, session.WhiteAgehama.ToString(), black: false, valueColor: Color.White);
        }
        else
        {
            DrawText($"BLACK {session.BlackAgehama}", new Vector2(firstValueX, bounds.Y + 14), Color.White, 0.5f);
            DrawText($"WHITE {session.WhiteAgehama}", new Vector2(secondValueX, bounds.Y + 14), Color.White, 0.5f);
        }
    }

    private void DrawStoneCountStrip(GoAppSession session, int y, bool showLeader = true, bool minimal = false)
    {
        var bounds = new Rectangle(1144, y, 668, 82);
        var blackStones = session.BlackStoneCount;
        var whiteStones = session.WhiteStoneCount;
        var total = blackStones + whiteStones;
        var leader = blackStones == whiteStones ? "EVEN" : blackStones > whiteStones ? $"BLACK +{blackStones - whiteStones}" : $"WHITE +{whiteStones - blackStones}";

        if (!minimal)
        {
            FillRect(bounds, new Color(24, 31, 37));
            DrawRect(bounds, 1, new Color(70, 85, 94));
        }
        if (minimal)
        {
            DrawResultLabel(new Rectangle(bounds.X + 20, bounds.Y, bounds.Width - 40, 56), "STONES", new Color(76, 91, 126));
        }
        else
        {
            DrawText("STONES", new Vector2(bounds.X + 20, bounds.Y + 15), new Color(180, 195, 195), 0.46f);
        }
        var firstValueX = minimal ? GameOverValueX : bounds.X + 150;
        var secondValueX = minimal ? GameOverSecondValueX : bounds.X + 334;
        if (minimal)
        {
            DrawStoneValue(firstValueX, bounds.Y + 28, blackStones.ToString(), black: true, valueColor: Color.White);
            DrawStoneValue(secondValueX, bounds.Y + 28, whiteStones.ToString(), black: false, valueColor: Color.White);
        }
        else
        {
            DrawText($"BLACK {blackStones}", new Vector2(firstValueX, bounds.Y + 13), Color.White, 0.5f);
            DrawText($"WHITE {whiteStones}", new Vector2(secondValueX, bounds.Y + 13), Color.White, 0.5f);
        }
        if (showLeader)
        {
            DrawText(leader, new Vector2(bounds.X + 518, bounds.Y + 13), new Color(99, 223, 185), 0.5f);
        }

        var bar = minimal
            ? new Rectangle(GameOverValueX, bounds.Y + 52, bounds.Right - GameOverValueX - 20, 14)
            : new Rectangle(bounds.X + 20, bounds.Y + 52, bounds.Width - 40, 14);
        FillRect(bar, new Color(14, 18, 23));
        if (total > 0)
        {
            var blackWidth = (int)MathF.Round(bar.Width * (blackStones / (float)total));
            if (blackWidth > 0)
            {
                FillRect(new Rectangle(bar.X, bar.Y, blackWidth, bar.Height), new Color(9, 10, 13));
            }

            var whiteWidth = bar.Width - blackWidth;
            if (whiteWidth > 0)
            {
                FillRect(new Rectangle(bar.X + blackWidth, bar.Y, whiteWidth, bar.Height), new Color(230, 224, 207));
            }
        }

        DrawRect(bar, 1, new Color(95, 108, 116));
    }

    private void DrawMiniBoard(Rectangle rect)
    {
        FillRect(rect, new Color(202, 145, 68));
        var margin = 14f;
        var cell = (rect.Width - margin * 2) / 8f;
        for (var i = 0; i < 9; i++)
        {
            var x = rect.X + margin + cell * i;
            DrawLine(new Vector2(x, rect.Y + margin), new Vector2(x, rect.Bottom - margin), 1, new Color(48, 34, 24));
            var y = rect.Y + margin + cell * i;
            DrawLine(new Vector2(rect.X + margin, y), new Vector2(rect.Right - margin, y), 1, new Color(48, 34, 24));
        }

        DrawStone(new Vector2(rect.X + margin + cell * 2, rect.Y + margin + cell * 2), 9, black: true);
        DrawStone(new Vector2(rect.X + margin + cell * 5, rect.Y + margin + cell * 4), 9, black: false);
    }
    private void DrawGlow(Vector2 center, float radius, Color color)
    {
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        _spriteBatch.Draw(_softCircle, destination, color);
    }
    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        var size = (int)(radius * 2);
        _spriteBatch.Draw(_softCircle, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size), color);
    }

    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        var angle = MathF.Atan2(direction.Y, direction.X);
        _spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private void FillRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

    private void DrawRect(Rectangle rect, int thickness, Color color)
    {
        FillRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        FillRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private static Rectangle CreateVerticalLineRect(float x, float top, float bottom, int thickness) =>
        new((int)MathF.Round(x - thickness / 2f), (int)MathF.Round(top), thickness, (int)MathF.Round(bottom - top));

    private static Rectangle CreateHorizontalLineRect(float left, float right, float y, int thickness) =>
        new((int)MathF.Round(left), (int)MathF.Round(y - thickness / 2f), (int)MathF.Round(right - left), thickness);

    private void DrawText(string text, Vector2 position, Color color, float scale)
    {
        var shadowAlpha = (int)MathF.Round(125f * color.A / 255f);
        _spriteBatch.DrawString(_font, text, position + new Vector2(2, 2), new Color(0, 0, 0, shadowAlpha), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCenteredText(string text, Vector2 center, Color color, float scale)
    {
        var size = _font.MeasureString(text) * scale;
        DrawText(text, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), color, scale);
    }

    private void DrawUiLabel(UiLabel label) => DrawFittedText(label.Text, label.Bounds, UiLabel.TextColor, label.Scale);

    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale)
    {
        var measured = _font.MeasureString(text);
        var fittedScale = MathF.Min(scale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(text, new Vector2(bounds.X, bounds.Center.Y - size.Y / 2), color, fittedScale);
    }

    private void DrawSharpCenteredFittedText(string text, Rectangle bounds, Color color, float scale)
    {
        var measured = _font.MeasureString(text);
        var fittedScale = MathF.Min(scale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        var position = new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2);

        // 操作ラベルは影も重ね描きもしない。プレートを切り抜いたような、くっきりした表示にする。
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, fittedScale, SpriteEffects.None, 0f);
    }

    private Texture2D CreateTexture(int width, int height, Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                data[y * width + x] = colorFactory(x, y);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateCircleTexture(int size, Color color, bool softEdge)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var dx = x - center;
            var dy = y - center;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            var radius = size * 0.48f;
            if (distance > radius)
            {
                return Color.Transparent;
            }

            var alpha = softEdge ? MathHelper.Clamp((radius - distance) / (radius * 0.45f), 0f, 1f) : 1f;
            return color * alpha;
        });
    }

    private void DrawEllipseWire(Vector2 center, float width, float height, Color color, int thickness, float rotation) =>
        DrawInscribedEllipseArc(center, width, height, color, thickness, rotation, 0f, MathHelper.TwoPi);

    private void DrawCircumscribedCircleArc(Vector2 center, float width, float height, Color color, int thickness,
        float rotation, float startAngle, float endAngle)
    {
        var diameter = MathF.Sqrt(width * width + height * height);
        DrawInscribedEllipseArc(center, diameter, diameter, color, thickness, rotation, startAngle, endAngle);
    }

    private void DrawInscribedEllipseArc(Vector2 center, float width, float height, Color color, int thickness,
        float rotation, float startAngle, float endAngle)
    {
        const int segments = 128;
        var cosRotation = MathF.Cos(rotation);
        var sinRotation = MathF.Sin(rotation);
        Vector2 Transform(float angle)
        {
            var x = MathF.Cos(angle) * width * 0.5f;
            var y = MathF.Sin(angle) * height * 0.5f;
            return center + new Vector2(x * cosRotation - y * sinRotation, x * sinRotation + y * cosRotation);
        }
        var drawWholeEllipse = MathF.Abs(endAngle - startAngle) >= MathHelper.TwoPi - 0.0001f;
        var normalizedStart = NormalizeEllipseAngle(startAngle);
        var normalizedEnd = NormalizeEllipseAngle(endAngle);
        for (var i = 0; i < segments; i++)
        {
            var segmentStart = MathHelper.TwoPi * i / segments;
            var segmentEnd = MathHelper.TwoPi * (i + 1) / segments;
            var segmentMiddle = (segmentStart + segmentEnd) * 0.5f;
            if (!drawWholeEllipse && !IsEllipseAngleVisible(segmentMiddle, normalizedStart, normalizedEnd)) continue;
            DrawLine(Transform(segmentStart), Transform(segmentEnd), thickness, color);
        }
    }

    private static float NormalizeEllipseAngle(float angle)
    {
        angle %= MathHelper.TwoPi;
        return angle < 0f ? angle + MathHelper.TwoPi : angle;
    }

    private static bool IsEllipseAngleVisible(float angle, float startAngle, float endAngle)
    {
        angle = NormalizeEllipseAngle(angle);
        return startAngle <= endAngle ? angle >= startAngle && angle <= endAngle : angle >= startAngle || angle <= endAngle;
    }

    public void DrawBreadcrumb(string path, bool visible = true)
    {
        if (!visible) return;
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _breadcrumb.Draw(path, VirtualScreen.Width, _font.MeasureString, new BreadcrumbDrawingCallbacks(FillRect, DrawFittedText));
        _spriteBatch.End();
    }

    private void DrawSpinBox(Rectangle upBounds, Rectangle downBounds, string amountLabel, Point mousePoint) =>
        _spinBox.Draw(upBounds, downBounds, amountLabel, mousePoint, new SpinBoxDrawingCallbacks(FillRect, DrawCenteredFittedText));

    private void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float preferredScale)
    {
        var measured = _font.MeasureString(text);
        var scale = MathF.Min(preferredScale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * scale;
        DrawText(text, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, scale);
    }

    private void DrawVerticalResultSection(Rectangle bounds, string title, Color accentColor,
        Color? textColor = null, int labelWidth = 38, int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));
        _verticalSectionLabel.Draw(bounds, title, accentColor, textColor ?? new Color(205, 218, 218), labelWidth, labelGap,
            new VerticalSectionLabelDrawingCallbacks(_font.MeasureString, DrawRotatedCenteredText, FillRect, DrawRect, DrawFittedText));
    }

    private void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _spriteBatch.DrawString(_font, text, center, color, -MathHelper.PiOver2, _font.MeasureString(text) / 2f, scale, SpriteEffects.None, 0f);

    public static bool GetTextInputDialogCancelButtonHit(Point point) => TextInputDialog.IsCancelButtonHit(point);
    public static bool GetTextInputDialogOkButtonHit(Point point) => TextInputDialog.IsOkButtonHit(point);
    public static bool GetTextInputDialogDefaultButtonHit(Point point) => TextInputDialog.IsDefaultButtonHit(point);
    public static bool IsTextInputDialogTextBoxHit(Point point) => TextInputDialog.IsTextBoxHit(point);
    public int GetTextInputDialogCaretIndex(Point point, string text) => GetTextBoxCaretIndex(point.X, text, TextInputDialog.TextContentBounds, 0.55f);

    public void DrawTextInputDialog(Point mousePosition, string title, string text, int caretIndex, int selectionStart,
        int selectionLength, string message, bool showDefaultButton = false, TextCompositionState composition = default,
        TextCompositionDiagnostics compositionDiagnostics = default, bool showCompositionDiagnostics = false)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _textInputDialog.Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message, showDefaultButton,
            composition, compositionDiagnostics, showCompositionDiagnostics,
            new TextInputDialogDrawingCallbacks(FillRect, DrawRect, DrawText, DrawFittedText, DrawTextBoxSelection,
                DrawDynamicCompositionText, _font.MeasureString, DrawLine, DrawCompositionLamp, DrawCommandButton));
        _spriteBatch.End();
    }

    private void DrawCompositionLamp(string label, int x, bool enabled, Color activeColor) =>
        DrawCompositionLamp(TextInputDialog.Bounds, label, x, enabled, activeColor);

    private void DrawCompositionLamp(Rectangle dialogBounds, string label, int x, bool enabled, Color activeColor)
    {
        var center = new Vector2(x, dialogBounds.Y + 47);
        DrawCircle(center, 8, enabled ? activeColor : new Color(79, 89, 98));
        DrawText(label, new Vector2(center.X - _font.MeasureString(label).X * 0.11f, dialogBounds.Y + 66), new Color(180, 195, 195), 0.22f);
    }

    private float DrawDynamicCompositionText(string text, Vector2 position, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            DrawText(text, position, color, scale);
            return _font.MeasureString(text).X * scale;
        }
        if (!_dynamicOptionTextTextures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new System.IO.MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[text] = texture;
        }
        var targetHeight = _font.LineSpacing * scale;
        var textureScale = targetHeight / texture.Height;
        var width = texture.Width * textureScale;
        _spriteBatch.Draw(texture, new Rectangle((int)position.X, (int)position.Y, (int)width, (int)targetHeight), color);
        return width;
    }

    public void DrawMessageDialog(MessageDialog dialog, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        dialog.Draw(mousePoint, new MessageDialogDrawingCallbacks(FillRect, DrawRect, DrawDynamicOptionText, DrawLine,
            (bounds, text, focused, point, scale) => DrawCommandButton(bounds, text, focused, point, scale: scale)));
        _spriteBatch.End();
    }

    public void DrawLightningScreenTransition(float progress)
    {
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ScreenTransition.Draw(progress, new ScreenTransitionDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, DrawLine));
        _spriteBatch.End();
    }

    public void DrawScreenshotCaptureEffect(float progress)
    {
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ScreenshotEffect.Draw(progress, new ScreenshotEffectDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect));
        _spriteBatch.End();
    }

    public void DrawReviewUnsavedChangesConfirmation(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        ReviewUnsavedChangesConfirmation.Draw(mousePoint,
            new ReviewUnsavedChangesConfirmationDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect,
                DrawRect, DrawText, DrawFittedText, DrawCommandButton));
        _spriteBatch.End();
    }

    private void DrawInitialPositionConcierge(InitialPositionConciergeView view, Point mousePoint) =>
        InitialPositionConcierge.Draw(view, mousePoint,
            new InitialPositionConciergeDrawingCallbacks(DrawDynamicOptionText, DrawFittedText, DrawText, FillRect, DrawRect, DrawCommandButton));

    public void DrawPopupNumberUnderline(Point mousePosition, string title, string text, int caretIndex,
        int selectionStart, int selectionLength, string message)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        PopupNumberUnderline.Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message,
            new PopupNumberUnderlineDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRect,
                DrawText, DrawFittedText, DrawTextBoxSelection, value => _font.MeasureString(value).X, DrawCommandButton));
        _spriteBatch.End();
    }

    public int GetPopupNumberUnderlineCaretIndex(Point point, string text) =>
        PopupNumberUnderline.GetCaretIndex(point, text, GetTextBoxCaretIndex);

    public void SetStickyNoteScreen(StickyNoteScreenId screen) => _stickyNoteScreen = screen;

    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(_stickyNoteScreen)) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRect, DrawRect, DrawDynamicOptionText));
    }

    public static bool GetCgosMatchWatchNowHit(Point point, bool enabled) => CgosMatchNotification.IsWatchNowHit(point, enabled);
    public static bool GetCgosMatchWatchLaterHit(Point point, bool enabled) => CgosMatchNotification.IsWatchLaterHit(point, enabled);
    public static bool GetCgosMatchDeferredHit(Point point) => CgosMatchNotification.IsDeferredHit(point);
    public static bool GetCgosMatchDeferredBannerHit(Point point) => CgosMatchNotification.IsDeferredBannerHit(point);

    public void DrawCgosMatchNotification(Point mousePosition, bool deferred, bool finished, int secondsRemaining,
        float opacity, float buttonOpacity, bool buttonsEnabled, bool showDeferredAction)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        var message = finished ? "対局が終了しました。結果画面へ移動します。" : $"対局が始まりました。{secondsRemaining} 秒後に観戦画面へ移動します。";
        _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        _cgosMatchNotification.Draw(mousePoint, deferred, finished, message, opacity, buttonOpacity, buttonsEnabled, showDeferredAction,
            new CgosMatchNotificationDrawingCallbacks(FillRect, DrawRect, DrawCircle, DrawDynamicOptionText, DrawFittedText));
        _spriteBatch.End();
    }

    private static Rectangle TextAreaDialogBounds => new(320, 150, 1280, 780);
    private static Rectangle TextAreaTextBounds => new(390, 330, 1140, 400);
    private static Rectangle TextAreaDiscardButtonBounds => new(1230, 172, 150, 54);
    private static Rectangle TextAreaApplyButtonBounds => new(1410, 172, 150, 54);
    public static bool GetTextAreaDialogCancelButtonHit(Point point) => TextAreaDiscardButtonBounds.Contains(point);
    public static bool GetTextAreaDialogApplyButtonHit(Point point) => TextAreaApplyButtonBounds.Contains(point);

    public void DrawTextAreaDialog(Point mousePosition, string title, string text, int caretIndex, string message,
        TextCompositionState composition = default, TextCompositionDiagnostics compositionDiagnostics = default,
        bool showCompositionDiagnostics = false)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 145));
        FillRect(new Rectangle(TextAreaDialogBounds.X + 14, TextAreaDialogBounds.Y + 16, TextAreaDialogBounds.Width, TextAreaDialogBounds.Height), new Color(0, 0, 0, 155));
        FillRect(TextAreaDialogBounds, new Color(24, 29, 36, 252));
        DrawRect(TextAreaDialogBounds, 2, new Color(116, 145, 146));
        DrawText("COMMENT EDITOR", new Vector2(TextAreaDialogBounds.X + 34, TextAreaDialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText(title, new Rectangle(TextAreaDialogBounds.X + 36, TextAreaDialogBounds.Y + 96, TextAreaDialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);
        if (showCompositionDiagnostics)
        {
            DrawCompositionLamp(TextAreaDialogBounds, "SDL", 1100, compositionDiagnostics.IsSdlWindowResolved, new Color(99, 223, 185));
            DrawCompositionLamp(TextAreaDialogBounds, "HOOK", 1146, compositionDiagnostics.IsWindowProcedureAttached, new Color(99, 223, 185));
            DrawCompositionLamp(TextAreaDialogBounds, "IME", 1192, composition.IsActive, new Color(255, 225, 128));
        }
        _multilineTextUnderline.Draw(TextAreaTextBounds, this);
        DrawTextAreaContent(text, TextAreaTextBounds);
        var caret = GetTextAreaCaretPosition(text, caretIndex);
        if (composition.IsActive && !string.IsNullOrEmpty(composition.Text))
        {
            var compositionWidth = DrawDynamicCompositionText(composition.Text, caret, new Color(255, 225, 128), 0.52f);
            DrawLine(caret + new Vector2(0, 29), caret + new Vector2(compositionWidth, 29), 2, new Color(255, 225, 128));
        }
        FillRect(new Rectangle((int)caret.X, (int)caret.Y, 2, 29), composition.IsActive ? new Color(255, 225, 128) : new Color(147, 244, 200));
        DrawDynamicOptionText(message, new Rectangle(TextAreaDialogBounds.X + 70, 752, 820, 34), new Color(180, 195, 195), 0.34f);
        DrawFittedText("ENTER: NEW LINE   CTRL+ENTER: SAVE SGF", new Rectangle(TextAreaDialogBounds.X + 70, 786, 800, 28), new Color(147, 201, 190), 0.29f);
        DrawCommandButton(TextAreaDiscardButtonBounds, "DISCARD", false, mousePoint, scale: 0.30f);
        DrawCommandButton(TextAreaApplyButtonBounds, "SAVE & CLOSE", false, mousePoint, scale: 0.25f);
        _spriteBatch.End();
    }

    private void DrawTextAreaContent(string text, Rectangle bounds)
    {
        if (string.IsNullOrEmpty(text)) { DrawFittedText("(EMPTY COMMENT)", new Rectangle(bounds.X + 18, bounds.Y + 18, bounds.Width - 36, 34), new Color(112, 132, 136), 0.34f); return; }
        var key = $"popup-text-area:{text.GetHashCode(StringComparison.Ordinal)}:{text.Length}:{bounds.Width}:{bounds.Height}";
        if (!_dynamicOptionTextTextures.TryGetValue(key, out var texture))
        {
            var png = _textRasterizer.RasterizeWrappedPagePng(text, bounds.Width - 36, bounds.Height - 36, 26, 5, 0);
            using var stream = new System.IO.MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[key] = texture;
        }
        _spriteBatch.Draw(texture, new Rectangle(bounds.X + 18, bounds.Y + 18, bounds.Width - 36, bounds.Height - 36), new Color(226, 232, 225));
    }

    private Vector2 GetTextAreaCaretPosition(string text, int caretIndex)
    {
        var safeIndex = Math.Clamp(caretIndex, 0, text.Length);
        var beforeCaret = text[..safeIndex];
        var lastLineStart = beforeCaret.LastIndexOf('\n') + 1;
        var lineText = beforeCaret[lastLineStart..];
        var lineNumber = 0;
        foreach (var character in beforeCaret) if (character == '\n') lineNumber++;
        var x = TextAreaTextBounds.X + 18 + (int)MathF.Round(_textRasterizer.MeasureTextWidth(lineText, pixelHeight: 26, bold: false));
        var y = TextAreaTextBounds.Y + 18 + lineNumber * 31;
        return new Vector2(Math.Clamp(x, TextAreaTextBounds.X + 18, TextAreaTextBounds.Right - 22), Math.Clamp(y, TextAreaTextBounds.Y + 18, TextAreaTextBounds.Bottom - 48));
    }

    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
            DrawRenNumber(renParse.GetRenNumber(x, y), BoardPoint(start, cell, x, y), scale);
    }

    internal static BoardLensButton? GetLocalPlayingBoardLensButtonHit(Point point, bool isLensEnabled) =>
        LocalPlayingBoardLensButtons.GetHit(point, isLensEnabled);

    private void DrawLocalPlayingBoardLensButtonStrip(bool isLensEnabled, Point mousePoint)
    {
        DrawFittedText("BOARD LENS  [L] / [J] / [K] / [1]", new Rectangle(1164, 812, 316, 36), new Color(147, 201, 190), 0.26f);
        DrawBoardLensButtonStrip(LocalPlayingBoardLensButtons, isLensEnabled, mousePoint, 0.32f);
    }

    private void DrawBoardLensButtonStrip(BoardLensButtonStrip buttons, bool isLensEnabled, Point mousePoint, float scale = 0.32f)
    {
        DrawCommandButton(buttons.ToggleBounds, "L", isLensEnabled, mousePoint, scale: scale);
        DrawCommandButton(buttons.PreviousBounds, "<J", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.82f);
        DrawCommandButton(buttons.NextBounds, "K>", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.82f);
        DrawCommandButton(buttons.ExitBounds, "OFF/1", false, mousePoint, enabled: isLensEnabled, scale: scale * 0.66f);
    }

    private void DrawBoardRenAnalysis(RenParseDisplayMode displayMode, int boardSize, Func<int, int, GoStone> getStone,
        Func<GoRenParseResult> parseRens, Action drawPlacedStones, Vector2 start, float cell)
    {
        if (displayMode == RenParseDisplayMode.Off) { drawPlacedStones(); return; }
        var renParse = parseRens();
        if (displayMode == RenParseDisplayMode.Overlay)
        {
            drawPlacedStones(); DrawRenBoundaries(renParse, start, cell); DrawRenNumbers(renParse, start, cell); return;
        }
        if (displayMode == RenParseDisplayMode.Graph)
        {
            DrawRenGraphCells(boardSize, getStone, start, cell); DrawRenBoundaries(renParse, start, cell);
            DrawRenRepresentativeNumbers(renParse, start, cell); return;
        }
        if (displayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            var nodes = CreateRenGraphNodes(renParse, start, cell, displayMode == RenParseDisplayMode.Eye);
            FillRect(BoardBounds, new Color(56, 145, 129)); DrawRenGraphEdges(nodes, renParse.Edges, cell); DrawRenGraphNodes(nodes, cell); return;
        }
        DrawRenGraphCells(boardSize, getStone, start, cell); DrawRenBoundaries(renParse, start, cell);
        if (displayMode == RenParseDisplayMode.RenArea) { DrawRenAreaNumbers(renParse, start, cell); return; }
        RenBoundaryLens.DrawRenBoundaryLens(this, renParse, displayMode, start, cell);
    }

    private void DrawRenAreaNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            DrawRenMetricNumber(ren, ren.Points.Count, RenMetricUnit.PointCount, RenGraphCellColor(ren.Stone), start,
                cell, RenGraphCellColor(OpponentOf(ren.Stone)));
        }
    }

    private void DrawDeferredStrongMetrics(GoRenParseResult renParse,
        List<(int RenNumber, int Value, Color Color, Color Outline)> metrics, Vector2 start, float cell)
    {
        foreach (var metric in metrics)
            DrawRenMetricNumber(renParse.GetRen(metric.RenNumber), metric.Value, RenMetricUnit.PointCount,
                metric.Color, start, cell, metric.Outline);
    }

    private void DrawRenGraphEyeMarkers(RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0) return;
        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;
        for (var i = 0; i < node.EyeNumbers.Count; i++)
        {
            var bounds = new Rectangle((int)MathF.Round(startX + i * spacing - markerSize * 0.5f),
                (int)MathF.Round(startY - markerSize * 0.5f), (int)MathF.Round(markerSize), (int)MathF.Round(markerSize));
            FillRect(bounds, new Color(255, 238, 0, 245));
            DrawRect(bounds, 2, new Color(255, 250, 220));
            DrawRenNumber(node.EyeNumbers[i], new Vector2(bounds.Center.X, bounds.Center.Y), markerScale);
        }
    }

    private void DrawRenGraphStep1Overlay(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        DrawRenGraphCells(session, start, cell); DrawRenBoundaries(renParse, start, cell);
        DrawRenRepresentativeNumbers(renParse, start, cell);
    }
    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell); var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++) for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            if (drawn[number]) continue;
            drawn[number] = true; DrawRenNumber(number, BoardPoint(start, cell, x, y), scale);
        }
    }
    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var size = renParse.Size; var halfCell = cell * 0.5f; var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f)); var color = new Color(255, 238, 0, 238);
        for (var y = 0; y < size; y++) for (var x = 0; x < size; x++)
        {
            var number = renParse.GetRenNumber(x, y); var center = BoardPoint(start, cell, x, y);
            var left = center.X - halfCell; var top = center.Y - halfCell; var right = center.X + halfCell; var bottom = center.Y + halfCell;
            if (x == 0 || renParse.GetRenNumber(x - 1, y) != number) FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
            if (y == 0 || renParse.GetRenNumber(x, y - 1) != number) FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
            if (x == size - 1) FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
            if (y == size - 1) FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
        }
    }

    private void DrawNobiLens(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        var legColor = RenGraphCellColor(session.CurrentTurn);
        var candidateColor = new Color(126, 255, 188);
        var legThickness = MathHelper.Clamp(cell * 0.045f, 2f, 5f);
        var markerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
        for (var number = 1; number <= renParse.Count; number++)
        {
            var ren = renParse.GetRen(number);
            if (ren.Stone != session.CurrentTurn) continue;
            var contacts = new List<(GoPoint From, GoPoint To)>();
            foreach (var point in ren.Points)
            {
                AddCandidate(point, point.X - 1, point.Y); AddCandidate(point, point.X + 1, point.Y);
                AddCandidate(point, point.X, point.Y - 1); AddCandidate(point, point.X, point.Y + 1);
            }
            var markers = new HashSet<GoPoint>();
            foreach (var contact in contacts)
            {
                DrawLine(BoardPoint(start, cell, contact.From.X, contact.From.Y), BoardPoint(start, cell, contact.To.X, contact.To.Y), legThickness, legColor);
                markers.Add(contact.To);
            }
            foreach (var marker in markers)
            {
                var center = BoardPoint(start, cell, marker.X, marker.Y);
                DrawCircle(center, markerRadius + 3f, RenGraphCellColor(session.CurrentTurn)); DrawCircle(center, markerRadius, candidateColor);
            }
            void AddCandidate(GoPoint from, int x, int y)
            {
                if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size ||
                    renParse.GetRen(renParse.GetRenNumber(x, y)).Stone != GoStone.Empty || !session.IsNobiCandidate(x, y)) return;
                contacts.Add((from, new GoPoint(x, y)));
            }
        }
    }

    public Vector2 GetBoardPoint(Vector2 start, float cell, int x, int y) => BoardPoint(start, cell, x, y);
    public Color GetRenGraphCellColor(GoStone stone) => RenGraphCellColor(stone);
    public void DrawBoardLensLine(Vector2 start, Vector2 end, float thickness, Color color) => DrawLine(start, end, thickness, color);
    public void DrawBoardLensCircle(Vector2 center, float radius, Color color) => DrawCircle(center, radius, color);
    public void FillBoardLensRectangle(Rectangle bounds, Color color) => FillRect(bounds, color);
    public void DrawRenBoundaryPointMetric(GoRen ren, int value, Color valueColor, Vector2 start, float cell, Color? outlineColor) =>
        DrawRenMetricNumber(ren, value, RenMetricUnit.PointCount, valueColor, start, cell, outlineColor);
    public void DrawDeferredStrongBoundaryMetrics(GoRenParseResult renParse,
        IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)> metrics, Vector2 start, float cell) =>
        DrawDeferredStrongMetrics(renParse, new List<(int RenNumber, int Value, Color Color, Color Outline)>(metrics), start, cell);

    private void DrawRenMetricNumber(GoRen ren, int value, RenMetricUnit unit, Color valueColor, Vector2 start,
        float cell, Color? valueOutlineColor = null)
    {
        var representative = ren.Points[0];
        var center = BoardPoint(start, cell, representative.X, representative.Y);
        var valueScale = MathHelper.Clamp(cell / 68f, 0.34f, 0.80f);
        DrawRenNumber(ren.Number, center - new Vector2(0f, cell * 0.20f), RenNumberScale(cell));
        var valueText = value.ToString();
        if (valueText.Length > 2) valueScale *= MathF.Min(1f, _font.MeasureString("88").X / Math.Max(1f, _font.MeasureString(valueText).X));
        var valueCenter = center + new Vector2(0f, cell * 0.10f);
        if (valueOutlineColor is { } outline) DrawCenteredOutlinedText(valueText, valueCenter, valueColor, outline, valueScale);
        else DrawCenteredText(valueText, valueCenter, valueColor, valueScale);
        if (unit == RenMetricUnit.RenCount) DrawRenMetricUnit(center + new Vector2(0f, cell * 0.37f), unit, valueColor, cell, valueOutlineColor);
    }

    private static float RenNumberScale(float cell) => MathHelper.Clamp(cell / 120f, 0.18f, 0.46f);
    private void DrawRenNumber(int number, Vector2 center, float scale) => DrawCenteredOutlinedText($"#{number}", center, new Color(0, 177, 238), new Color(0, 92, 132, 245), scale);
    private void DrawCenteredOutlinedText(string text, Vector2 center, Color color, Color outlineColor, float scale)
    {
        var position = center - _font.MeasureString(text) * scale / 2f;
        var outline = MathHelper.Clamp(scale * 7f, 1.5f, 3f);
        for (var i = 0; i < 16; i++)
        {
            var angle = MathHelper.TwoPi * i / 16;
            _spriteBatch.DrawString(_font, text, position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * outline, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
    private void DrawRenMetricUnit(Vector2 center, RenMetricUnit unit, Color color, float cell, Color? outlineColor = null)
    {
        var radius = MathHelper.Clamp(cell * 0.075f, 3f, 6f);
        var thickness = Math.Max(2, (int)MathF.Round(radius * 0.42f));
        var backing = new Color(16, 26, 32, 220);
        if (unit == RenMetricUnit.PointCount)
        {
            DrawCircle(center, radius + thickness, outlineColor ?? color);
            DrawCircle(center, radius, outlineColor is null ? backing : color);
            if (outlineColor is not null) DrawCircle(center, Math.Max(1f, radius - thickness), backing);
            return;
        }
        var extent = (int)MathF.Round(radius + thickness);
        var bounds = new Rectangle((int)MathF.Round(center.X) - extent, (int)MathF.Round(center.Y) - extent, extent * 2, extent * 2);
        FillRect(bounds, backing);
        DrawRect(bounds, thickness, color);
    }
    private static GoStone OpponentOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
    private enum RenMetricUnit { PointCount, RenCount }
    private static Color RenGraphNodeColor(GoStone stone) => stone switch { GoStone.Black => Color.Black, GoStone.White => new Color(248, 248, 244), _ => new Color(255, 197, 18) };
    private static Color RenGraphCellColor(GoStone stone) => stone switch { GoStone.Black => Color.Black, GoStone.White => new Color(248, 248, 244), _ => new Color(255, 197, 18) };

    public int GetPlayerEditPanelCaretIndex(Point point, EntryProfileEditField field, string text) =>
        EditEntryProfile.GetCaretIndex(point, field, text, GetTextBoxCaretIndex);

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint) =>
        EditEntryProfile.Draw(session, mousePoint, _stickyNoteScreen,
            new EditEntryProfileDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRoundedFill,
                DrawRect, DrawText, DrawFittedText, DrawCommandButton, DrawIconStone, DrawPlayerRoleFaceIcon,
                DrawTextBoxSelection, DrawTextBoxCaret, DrawEditableTextEditHint, bounds => DrawPlayerEditHint("CHANGE", bounds),
                DrawLine, DrawDynamicOptionText));

    void IUnderlineDrawingSurface.FillRectangle(Rectangle bounds, Color color) => FillRect(bounds, color);
    void IUnderlineDrawingSurface.FillRoundedRectangle(Rectangle bounds, int radius, Color color) => DrawRoundedFill(bounds, radius, color);
    void IUnderlineDrawingSurface.DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => DrawLine(start, end, thickness, color);
}
