namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

/// <summary>
/// ［画面描画］の共通処理
/// </summary>
public sealed partial class GoScreenRenderer
{
    private const int GameOverValueX = 1328;
    private const int GameOverSecondValueX = 1560;
    private const int PlayingPlayersY = 140;

    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly ITextRasterizer _textRasterizer;
    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;
    private readonly Texture2D _pixel;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;

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
        DrawBoard(session, mousePoint);
        if (session.CurrentMode.Kind == GoAppModeKind.Playing &&
            session.CanOpenLocalChartPopup)
        {
            DrawBroadcastStatusBadge(
                session.IsLocalReplayMode ? "REPLAY" : "CURRENT",
                session.IsReviewChartPopupOpen);
        }
        if (!session.IsReviewChartPopupOpen)
        {
            DrawSidePanel(session, mousePoint, liveBoardPreview, initialPositionConcierge);
            if (session.IsLocalReplayMode)
            {
                DrawReplayNavigationControls(
                    session.LocalDisplayMoveIndex,
                    session.CurrentGameRecord.Moves.Count,
                    mousePoint,
                    showBackToLive: session.CurrentMode.Kind == GoAppModeKind.Playing,
                    backToLiveLabel: "BACK TO CURRENT");
            }
            else if (session.CanOpenLocalChartPopup ||
                     session.CurrentMode.Kind == GoAppModeKind.Reviewing)
            {
                DrawReplayEditIconButton(mousePoint);
            }
            DrawTournamentRulesSelectionDialog(session, mousePoint);
            DrawTournamentRulesAddPanel(session, mousePoint);
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
        DrawGtpEngineSelectionDialog(session, mousePoint);
        DrawGtpEngineEditPanel(session, mousePoint);

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

    public static bool GetReturnToSetupButtonHit(Point point) => ReturnToSetupButtonBounds.Contains(point);

    public static bool GetExportSgfButtonHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static bool GetSgfAutoSaveCheckHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static bool GetLocalGameOverReviewButtonHit(Point point) =>
        LocalGameOverReviewButtonBounds.Contains(point);

    public static bool GetSetupBackToTitleButtonHit(Point point) => SetupBackToTitleButtonBounds.Contains(point);

    public static GoPlayerKind? GetBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, BlackPlayerKindButtonY);

    public static GoPlayerKind? GetWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, WhitePlayerKindButtonY);

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
        DrawCommandButton(ImportSgfButtonBounds, session.HasReviewGameRecord ? "SGF CLEAR" : "SGF INPUT", false, mousePoint, scale: 0.42f);
        DrawResultRow(new Rectangle(1164, 292, 628, 56), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);

        DrawVerticalResultSection(new Rectangle(1144, 376, 668, 304), "RULES", new Color(66, 104, 116));
        DrawInfoStrip(1144, 384, "RULE", session.RuleKind.ToString());
        DrawInfoStrip(1144, 456, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 528, "KOMI", FormatKomi(session.Komi));
        DrawInfoStrip(1144, 600, "MOVES", FormatMoveLimit(session.MoveLimit));

        DrawVerticalResultSection(new Rectangle(1144, 696, 668, 216), "PLAYERS", new Color(76, 91, 126));
        DrawSetupPlayerKindRow(GoStone.Black, session.BlackPlayerKind, mousePoint, BlackPlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.Black, mousePoint, BlackEngineButtonY);
        DrawSetupPlayerKindRow(GoStone.White, session.WhitePlayerKind, mousePoint, WhitePlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.White, mousePoint, WhiteEngineButtonY);

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
        var hovered = bounds.Contains(mousePoint);
        var active = session.IsTournamentRulesDisplayNameEditing;
        var displayName = active ? session.TournamentRulesDisplayNameDraft : session.TournamentDisplayName;

        DrawTournamentRulesTabNavigationHint(bounds, session, 0);
        DrawTournamentRulesFieldLabel("DISPLAY", bounds);

        var textBounds = TournamentRulesAddPanelDisplayNameTextBounds;
        DrawTournamentRulesTextInputSurface(textBounds, active, hovered);
        if (active)
            DrawTextBoxSelection(displayName, session.TournamentRulesDisplayNameSelectionStart, session.TournamentRulesDisplayNameSelectionLength, textBounds, 0.46f);
        DrawFittedText(displayName, textBounds, Color.White, 0.46f);
        if (active)
        {
            DrawTextBoxCaret(displayName, session.TournamentRulesDisplayNameCaretIndex, textBounds, 0.46f);
        }

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
        if (string.IsNullOrWhiteSpace(fullPath) || fullPath == "-")
        {
            return;
        }

        var popupBounds = PathTooltipBounds(rowBounds);
        if (rowBounds.Contains(mousePoint) || popupBounds.Contains(mousePoint))
        {
            DrawPathTooltip(popupBounds, fullPath, mousePoint);
        }
    }

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
        var hintWidth = isPrevious ? 104 : 48;
        var hintBounds = new Rectangle(bounds.X - hintWidth - 6, bounds.Y + 2, hintWidth, 20);
        DrawRoundedFill(hintBounds, 6, new Color(4, 6, 8, 235));
        DrawFittedText(
            hintText,
            new Rectangle(hintBounds.X + 4, hintBounds.Y + 2, hintBounds.Width - 8, hintBounds.Height - 4),
            Color.White,
            0.24f);
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

    private void DrawSetupPlayerKindRow(GoStone stone, GoPlayerKind selectedKind, Point mousePoint, int y)
    {
        var rowBounds = new Rectangle(1144, y - 14, 668, 72);
        DrawIconStone(new Vector2(rowBounds.X + 36, rowBounds.Center.Y), 18, stone == GoStone.Black);

        var humanBounds = PlayerKindButtonBounds(0, y);
        var computerBounds = PlayerKindButtonBounds(1, y);
        var bounds = PlayerKindSegmentBounds(y);

        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, 90));
        FillRect(bounds, new Color(33, 43, 52));
        DrawSegmentedPlayerKindButton(humanBounds, "HUMAN", selectedKind == GoPlayerKind.Human, humanBounds.Contains(mousePoint));
        DrawSegmentedPlayerKindButton(computerBounds, "COMPUTER", selectedKind == GoPlayerKind.Computer, computerBounds.Contains(mousePoint));
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
        DrawLabeledBrowseSelector(GtpEngineSelectorBounds(y) with { Value = engineName }, mousePoint);
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

    private static Rectangle BoardSizeButtonBounds(int index, int y) => new(AddPanelControlX + 132 + index * 180, y, 164, 50);
    private static Rectangle PathTooltipBounds(Rectangle rowBounds)
    {
        var y = rowBounds.Y - 102;
        if (y < 140)
        {
            y = rowBounds.Bottom - 2;
        }

        return new Rectangle(rowBounds.X, y, rowBounds.Width, 104);
    }

    private static Rectangle PathTooltipCopyButtonBounds(Rectangle rowBounds)
    {
        return PathTooltipCopyButtonBoundsFromPopup(PathTooltipBounds(rowBounds));
    }

    private static Rectangle PathTooltipCopyButtonBoundsFromPopup(Rectangle popupBounds) =>
        new(popupBounds.Right - 124, popupBounds.Y + 56, 100, 34);

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
    private static Rectangle ChangeAppProviderButtonBounds => new(1492, 620, 320, 56);
    private static Rectangle AppProviderGameSettingsButtonBounds => new(1328, 556, 320, 52);

    private static Rectangle ImportSgfButtonBounds => new(1492, 184, 320, 56);

    private static Rectangle SetupBackToTitleButtonBounds => new(1642, 104, 170, 52);
    private static Rectangle LocalUseButtonBounds => new(508, 404, 438, 300);
    private static Rectangle TitleMenuBackButtonBounds => new(1260, 316, 152, 54);
    private static Rectangle TitleAppProviderEngineDisplayBounds => new(570, 466, 780, 56);
    private static Rectangle TitleAppProviderEngineSelectButtonBounds => new(850, 548, 500, 54);
    private static Rectangle TitleAppProviderStartButtonBounds => new(1198, 826, 152, 54);
    private static Rectangle TitleAppProviderRecheckButtonBounds => new(828, 826, 340, 54);
    private static Rectangle TitleHomeLocalButtonBounds => new(500, 390, 400, 126);
    private static Rectangle TitleHomeCgosButtonBounds => new(500, 536, 400, 126);
    private static Rectangle TitleAppBounds(int index) => new(950, 390 + index * 100, 440, 84);

    public static bool GetTitleMenuBackButtonHit(Point point) => TitleMenuBackButtonBounds.Contains(point);
    public static bool GetTitleAppProviderStartButtonHit(Point point) => TitleAppProviderStartButtonBounds.Contains(point);
    public static bool GetTitleAppProviderRecheckButtonHit(Point point) => TitleAppProviderRecheckButtonBounds.Contains(point);

    public static bool GetTitleAppProviderEngineSelectButtonHit(Point point) =>
        TitleAppProviderEngineSelectButtonBounds.Contains(point);

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
        DrawCommandButton(ChangeAppProviderButtonBounds, "CHANGE PROVIDER", false, mousePoint, scale: 0.30f);

        DrawVerticalResultSection(new Rectangle(1144, 696, 668, 216), "PLAYERS", new Color(76, 91, 126));
        DrawSetupPlayerKindRow(GoStone.Black, session.BlackPlayerKind, mousePoint, BlackPlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.Black, mousePoint, BlackEngineButtonY);
        DrawSetupPlayerKindRow(GoStone.White, session.WhitePlayerKind, mousePoint, WhitePlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.White, mousePoint, WhiteEngineButtonY);

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
        if (label.All(character => _font.Characters.Contains(character)))
        {
            var measured = _font.MeasureString(label);
            var fittedScale = MathF.Min(scale, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
            var size = measured * fittedScale;
            DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
        }
        else
        {
            DrawDynamicOptionText(label, new Rectangle(bounds.X + 10, bounds.Y + 5, bounds.Width - 20, bounds.Height - 10), textColor, scale);
        }
    }

    public void DrawBoardLensBanner(string lensName, float opacity, float compactProgress)
    {
        opacity = Math.Clamp(opacity, 0f, 1f);
        compactProgress = Math.Clamp(compactProgress, 0f, 1f);
        compactProgress = compactProgress * compactProgress * (3f - (2f * compactProgress));
        _spriteBatch.Begin(
            blendState: BlendState.AlphaBlend,
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        var largeBounds = new Rectangle(560, 48, 800, 122);
        var compactBounds = new Rectangle(222, 18, 644, 58);
        var bounds = new Rectangle(
            (int)MathF.Round(MathHelper.Lerp(largeBounds.X, compactBounds.X, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Y, compactBounds.Y, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Width, compactBounds.Width, compactProgress)),
            (int)MathF.Round(MathHelper.Lerp(largeBounds.Height, compactBounds.Height, compactProgress)));
        var shadowAlpha = (int)(150f * opacity);
        var panelAlpha = (int)(235f * opacity);
        var textAlpha = (int)(255f * opacity);
        var largeTextAlpha = (int)(textAlpha * (1f - compactProgress));
        var compactTextAlpha = (int)(textAlpha * compactProgress);

        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, shadowAlpha));
        FillRect(bounds, new Color(13, 24, 31, panelAlpha));
        DrawRect(bounds, 2, new Color(125, 225, 255, textAlpha));
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 4), new Color(125, 225, 255, textAlpha));

        const string heading = "BOARD LENS";
        var headingScale = 0.28f;
        var headingSize = _font.MeasureString(heading) * headingScale;
        DrawText(
            heading,
            new Vector2(bounds.Center.X - headingSize.X / 2f, bounds.Y + 12),
            new Color(159, 215, 225, largeTextAlpha),
            headingScale);

        var measured = _font.MeasureString(lensName);
        var scale = MathF.Min(0.58f, (bounds.Width - 48f) / Math.Max(1f, measured.X));
        var size = measured * scale;
        DrawText(
            lensName,
            new Vector2(bounds.Center.X - size.X / 2f, bounds.Y + 43),
            new Color(235, 251, 255, largeTextAlpha),
            scale);

        const string guide = "[L] NEXT    [1] EXIT";
        var guideScale = 0.30f;
        var guideSize = _font.MeasureString(guide) * guideScale;
        DrawText(
            guide,
            new Vector2(bounds.Center.X - guideSize.X / 2f, bounds.Y + 82),
            new Color(255, 220, 128, largeTextAlpha),
            guideScale);

        const string compactGuide = "[L] NEXT    [1] EXIT";
        var compactNameScale = MathF.Min(0.34f, (bounds.Width - 28f) / Math.Max(1f, measured.X));
        var compactNameSize = measured * compactNameScale;
        DrawText(
            lensName,
            new Vector2(bounds.Center.X - compactNameSize.X / 2f, bounds.Y + 5),
            new Color(235, 251, 255, compactTextAlpha),
            compactNameScale);
        var compactGuideScale = 0.19f;
        var compactGuideSize = _font.MeasureString(compactGuide) * compactGuideScale;
        DrawText(
            compactGuide,
            new Vector2(bounds.Center.X - compactGuideSize.X / 2f, bounds.Y + 34),
            new Color(255, 220, 128, compactTextAlpha),
            compactGuideScale);

        _spriteBatch.End();
    }
    private void DrawPathTooltip(Rectangle bounds, string fullPath, Point mousePoint)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(30, 36, 43, 252));
        DrawRect(bounds, 2, new Color(147, 244, 200));
        DrawText("FULL PATH", new Vector2(bounds.X + 18, bounds.Y + 12), new Color(180, 195, 195), 0.34f);
        DrawFittedText(fullPath, new Rectangle(bounds.X + 18, bounds.Y + 38, bounds.Width - 150, 44), Color.White, 0.42f);
        DrawCommandButton(PathTooltipCopyButtonBoundsFromPopup(bounds), "COPY", false, mousePoint, scale: 0.34f);
    }

    private void DrawLabeledBrowseSelector(LabeledBrowseSelector selector, Point mousePoint)
    {
        if (selector.Bounds.X == 1144 && selector.Bounds.Width == 668)
        {
            DrawResultLabel(new Rectangle(selector.Bounds.X + 20, selector.Bounds.Y - 6, selector.Bounds.Width - 40, selector.Bounds.Height + 12), selector.Label, new Color(76, 91, 126));
            DrawFittedText(selector.Value, new Rectangle(GameOverValueX, selector.Bounds.Y + 6, selector.BrowseButtonBounds.X - GameOverValueX - 12, selector.Bounds.Height - 12), Color.White, 0.42f);
            DrawCommandButton(selector.BrowseButtonBounds, selector.ButtonLabel, false, mousePoint, enabled: selector.Enabled, scale: 0.34f);
            return;
        }

        DrawDataRowFrame(selector.Bounds);

        DrawFittedText(selector.Label, selector.LabelBounds, new Color(158, 178, 178), 0.36f);
        DrawFittedText(selector.Value, selector.ValueBounds, Color.White, 0.52f);
        DrawCommandButton(selector.BrowseButtonBounds, selector.ButtonLabel, false, mousePoint, enabled: selector.Enabled, scale: 0.34f);
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

    private void DrawVerticalResultSection(
        Rectangle bounds,
        string title,
        Color accentColor,
        Color? textColor = null,
        int labelWidth = 38,
        int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));

        var labelBounds = new Rectangle(bounds.X - labelWidth - labelGap, bounds.Y, labelWidth, bounds.Height);
        FillRect(labelBounds, new Color(accentColor, 150));
        DrawRect(labelBounds, 1, new Color(accentColor, 230));

        const float scale = 0.38f;
        var textSize = _font.MeasureString(title);
        var center = new Vector2(labelBounds.Center.X, labelBounds.Center.Y);
        var origin = textSize / 2f;
        _spriteBatch.DrawString(_font, title, center + new Vector2(2, 2), new Color(0, 0, 0, 125), -MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, title, center, textColor ?? new Color(205, 218, 218), -MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0f);
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
        FillRect(new Rectangle(bounds.X + 14, bounds.Center.Y - accentHeight / 2, 3, accentHeight), accentColor);
        DrawText(label, new Vector2(bounds.X + 30, bounds.Y + 14), new Color(180, 195, 195), 0.38f);
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
}
