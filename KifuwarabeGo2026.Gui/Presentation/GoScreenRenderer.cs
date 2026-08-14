namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared.RenBoundaries;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenshotEffect;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;
using KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;
using KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.Shared.SpinBox;
using KifuwarabeGo2026.Gui.Presentation.Shared.TextAreaDialog;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.MultilineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupTimeUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SectionLabel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.MessageDialog;
using KifuwarabeGo2026.Gui.Presentation.Title;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ［画面描画］の共通処理
/// </summary>
public sealed partial class GoScreenRenderer : IGoScreenRenderer
{
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
    private readonly StationeryDrawingContext _stationeryDrawingContext;
    internal StationeryDrawingContext StationeryDrawingContext => _stationeryDrawingContext;

    // 移設途中: RightSidePanelDrawingContext の導入後に削除する一時的な描画ブリッジです。
    internal void DrawRightSidePanelIconStone(Vector2 center, float radius, bool black) => DrawIconStone(center, radius, black);
    internal void DrawRightSidePanelPlayerRoleFaceIcon(Vector2 center, bool isComputer) => DrawPlayerRoleFaceIcon(center, isComputer);
    internal void DrawRightSidePanelFittedText(string text, Rectangle bounds, Color color, float scale) => DrawFittedText(text, bounds, color, scale);
    internal void DrawRightSidePanelRoundedFill(Rectangle bounds, int radius, Color color) => DrawRoundedFill(bounds, radius, color);
    internal void DrawRightSidePanelTextSelection(string text, int start, int length, Rectangle bounds, float scale) =>
        DrawTextBoxSelection(text, start, length, bounds, scale);
    internal void DrawRightSidePanelTextCaret(string text, int caret, Rectangle bounds, float scale) =>
        DrawTextBoxCaret(text, caret, bounds, scale);
    internal void DrawRightSidePanelCenteredFittedText(string text, Rectangle bounds, Color color, float scale) =>
        DrawSharpCenteredFittedText(text, bounds, color, scale);
    internal void DrawRightSidePanelDataRowFrame(Rectangle bounds) => DrawDataRowFrame(bounds);
    internal void DrawRightSidePanelCommandButton(Rectangle bounds, string label, Point mousePoint, bool enabled, float scale) =>
        DrawCommandButton(bounds, label, false, mousePoint, enabled, scale);
    internal void DrawRightSidePanelSelectableCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled, float scale) =>
        DrawCommandButton(bounds, label, selected, mousePoint, enabled, scale);
    internal void DrawRightSidePanelResultLabel(Rectangle bounds, string label, Color accentColor) =>
        DrawResultLabel(bounds, label, accentColor);
    internal void DrawRightSidePanelStoneValue(int x, int centerY, string value, bool black, Color valueColor) =>
        DrawStoneValue(x, centerY, value, black, valueColor);
    internal void DrawRightSidePanelGameOverTrendChart(GoAppSession session, Point mousePoint) =>
        DrawLocalGameOverTrendChart(session, mousePoint);
    internal void DrawRightSidePanelAgehamaSummary(Rectangle bounds, int blackAgehama, int whiteAgehama) =>
        DrawAgehamaSummaryComponent(bounds, blackAgehama, whiteAgehama);
    internal void DrawRightSidePanelStoneCountStrip(GoAppSession session, int y, bool showLeader, bool minimal) =>
        DrawStoneCountStrip(session, y, showLeader, minimal);
    internal void DrawRightSidePanelCircle(Vector2 center, float radius, Color color) => DrawCircle(center, radius, color);
    internal void DrawRightSidePanelReviewTrendChart(GoAppSession session, Point mousePoint) =>
        DrawReviewTrendChart(session, mousePoint);
    private readonly LinkUnderline _gtpEngineOptionLinkUnderline = new(
        new RoundUnderline { TopOffset = -4, Thickness = 4, Radius = 2 });
    private readonly MultilineTextUnderline _multilineTextUnderline = new(
        new SquareUnderline { Thickness = 1 }, "EDIT");
    private readonly LinkUnderline _compactLinkUnderline = new(
        new RoundUnderline { TopOffset = 1, Thickness = 3, Radius = 1 });
    private readonly LinkUnderline _selectorLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 4, Radius = 2 });
    private readonly LinkUnderline _tournamentRulesSettingsFileLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly SpinBox _spinBox = new();

    public HeadUpDisplayComponent HeadUpDisplay { get; } = HeadUpDisplayComponent.Default;
    public InitialPositionConcierge InitialPositionConcierge { get; } = new();
    private readonly CgosMatchNotification _cgosMatchNotification = CgosMatchNotification.Default;
    public EditEntryProfile EditEntryProfile { get; } = new();
    public ActionBadgeComponent EditActionBadge { get; } = ActionBadgeComponent.Create("EDIT", Rectangle.Empty);
    public ActionBadgeComponent ChangeActionBadge { get; } = ActionBadgeComponent.Create("CHANGE", Rectangle.Empty);

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
        _stationeryDrawingContext = new StationeryDrawingContext(
            FillRect, DrawRoundedFill, DrawRect, DrawLine, DrawText, DrawFittedText, DrawSharpCenteredFittedText,
            DrawRotatedCenteredText, _font.MeasureString);
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
            RightSidePanel.Default.Draw(this, session, backgroundMousePoint, liveBoardPreview, initialPositionConcierge);
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
    /// <summary>
    /// ［大会ルール設定　＞　コミ］のテキストボックスがクリックされたかどうかを判定します。
    /// XXX: なんでここにあるんだろう？　このクラスは画面描画の共通処理のはずなのに。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>

    public int GetHumanPlayerNameCaretIndex(Point point, GoStone stone, string text, bool isPonnuki) =>
        GetTextBoxCaretIndex(point.X, text, LocalMatchScreen.Default.GetPlayerKindRow(stone, isPonnuki).HumanNameTextBounds, 0.42f);

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
    private void DrawDisplayNameTextBox(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesScreen.Default.AddPanelDisplayNameRowBounds;
        var active = session.IsTournamentRulesDisplayNameEditing;
        var displayName = active ? session.TournamentRulesDisplayNameDraft : session.TournamentDisplayName;
        var textBounds = TournamentRulesScreen.Default.AddPanelDisplayNameTextBounds;
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
        var bounds = TournamentRulesScreen.Default.AddPanelFileRowBounds;
        var filePath = string.IsNullOrWhiteSpace(session.CurrentTournamentRules.FilePath) ? "-" : session.CurrentTournamentRules.FilePath;
        DrawTournamentRulesFieldLabel("SETTINGS", bounds);
        var textBounds = new Rectangle(bounds.X + 132, bounds.Y + 7, bounds.Width - 152, 42);
        _tournamentRulesSettingsFileLinkUnderline.Bounds = textBounds;
        _tournamentRulesSettingsFileLinkUnderline.SetActionBadge(ActionBadgeComponent.Create("OPEN", textBounds));
        _tournamentRulesSettingsFileLinkUnderline.UpdatePointer(mousePoint);
        DrawFittedText(filePath, textBounds, Color.White, 0.38f);
        _tournamentRulesSettingsFileLinkUnderline.Draw(_stationeryDrawingContext);
    }

    public bool IsTournamentRulesSettingsFileHit(Point point) => _tournamentRulesSettingsFileLinkUnderline.IsHit(point);

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

    /// <summary>画面固有レイアウトから利用できる、共通の単一行キャレット計測です。</summary>
    public int GetSinglelineTextCaretIndex(int pointX, string text, Rectangle textBounds, float textScale) =>
        GetTextBoxCaretIndex(pointX, text, textBounds, textScale);
    private void DrawPropertyRow(int y, string label, string value)
    {
        var propertyBounds = TournamentRulesScreen.Default.SelectionPropertyBounds;
        var bounds = new Rectangle(propertyBounds.X + 18, y, propertyBounds.Width - 36, 52);
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
        HeadUpDisplay.PopupFilePathTooltip.Draw(
            HeadUpDisplay.StickyNoteScreen,
            StickyNoteKind.TournamentRulesPathHint,
            rowBounds,
            fullPath,
            mousePoint,
            "FILE とは？",
            ["対局ルールで利用するファイルの場所です。"],
            _stationeryDrawingContext,
            DrawDynamicOptionText);
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

    private const int AddPanelControlX = 626;


    private static Rectangle TournamentRulesMoveLimitTextBounds => new(AddPanelControlX + 132, 612, 176, 40);


    private static Rectangle LocalUseButtonBounds => LocalMatchScreen.Default.LocalUseCardBounds;

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

    internal static string GetMoveThinkingText(GoAppSession session)
    {
        var text = $"{session.NextMoveNumber}手目を思考中";
        return session.MoveLimit <= 0 ? text : $"{text} / {session.MoveLimit}";
    }

    private static string FormatKomi(decimal komi) => komi.ToString("0.0");
    private const float MinimumCommandButtonLabelScale = 0.36f;
    private const float CommandButtonLabelScaleMultiplier = 1.25f;

    /// <summary>
    /// XXX: 何これ（＾～＾）？　ボタン（＾～＾）？
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="label"></param>
    /// <param name="selected"></param>
    /// <param name="mousePoint"></param>
    /// <param name="enabled"></param>
    /// <param name="scale"></param>
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

    internal void DrawInfoStrip(int x, int y, string label, string value)
    {
        var bounds = new Rectangle(x, y, 668, 72);
        DrawResultLabel(new Rectangle(x + 20, y, bounds.Width - 40, bounds.Height), label, new Color(62, 112, 105));
        DrawFittedText(value, new Rectangle(RightSidePanelLayout.PrimaryValueX, y + 12, bounds.Right - RightSidePanelLayout.PrimaryValueX - 20, bounds.Height - 24), Color.White, 0.58f);
    }

    internal void DrawResultRow(Rectangle bounds, string label, string value, Color chipColor, Color valueColor)
    {
        DrawResultLabel(bounds, label, chipColor);
        DrawFittedText(value, new Rectangle(RightSidePanelLayout.PrimaryValueX, bounds.Y + 6, bounds.Right - RightSidePanelLayout.PrimaryValueX - 18, bounds.Height - 12), valueColor, 0.58f);
    }

    private void DrawResultLabel(Rectangle bounds, string label, Color accentColor)
    {
        const int accentHeight = 28;
        // Intermission のラベル列は、section 内の行種別に関係なく同じグリッドへそろえる。
        FillRect(new Rectangle(bounds.X - 22, bounds.Center.Y - accentHeight / 2, 3, accentHeight), accentColor);
        DrawText(label, new Vector2(bounds.X - 8, bounds.Y + 14), new Color(180, 195, 195), 0.38f);
    }

    private void DrawStoneValue(int x, int centerY, string value, bool black, Color valueColor)
    {
        DrawIconStone(new Vector2(x + 18, centerY), 16, black);
        DrawText(value, new Vector2(x + 44, centerY - 14), valueColor, 0.5f);
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
        var firstValueX = minimal ? RightSidePanelLayout.PrimaryValueX : bounds.X + 150;
        var secondValueX = minimal ? RightSidePanelLayout.SecondaryValueX : bounds.X + 334;
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
            ? new Rectangle(RightSidePanelLayout.PrimaryValueX, bounds.Y + 52, bounds.Right - RightSidePanelLayout.PrimaryValueX - 20, 14)
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

    internal void FillRectangle(Rectangle rect, Color color) => FillRect(rect, color);

    private void DrawRect(Rectangle rect, int thickness, Color color)
    {
        FillRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        FillRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    internal void DrawRectangle(Rectangle rect, int thickness, Color color) => DrawRect(rect, thickness, color);

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
        HeadUpDisplay.Breadcrumb.Draw(path, VirtualScreen.Width, _font.MeasureString, new BreadcrumbDrawingCallbacks(FillRect, DrawFittedText));
        _spriteBatch.End();
    }

    private void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float preferredScale)
    {
        var measured = _font.MeasureString(text);
        var scale = MathF.Min(preferredScale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * scale;
        DrawText(text, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, scale);
    }

    internal void DrawVerticalResultSection(Rectangle bounds, string title, Color accentColor,
        Color? textColor = null, int labelWidth = 38, int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));
        var sectionLabel = SectionLabelComponent.CreateVertical(
            bounds,
            title,
            accentColor,
            textColor ?? new Color(205, 218, 218),
            _stationeryDrawingContext,
            labelWidth,
            labelGap);
        sectionLabel.Draw(_stationeryDrawingContext);
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
        HeadUpDisplay.TextInputDialog.Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message, showDefaultButton,
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
        HeadUpDisplay.ScreenTransition.Draw(progress, new ScreenTransitionDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, DrawLine));
        _spriteBatch.End();
    }

    public void DrawScreenshotCaptureEffect(float progress)
    {
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        HeadUpDisplay.ScreenshotEffect.Draw(progress, new ScreenshotEffectDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect));
        _spriteBatch.End();
    }

    public void DrawReviewUnsavedChangesConfirmation(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        HeadUpDisplay.ReviewUnsavedChangesConfirmation.Draw(mousePoint,
            new ReviewUnsavedChangesConfirmationDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect,
                DrawRect, DrawText, DrawFittedText, _stationeryDrawingContext));
        _spriteBatch.End();
    }

    internal void DrawInitialPositionConciergeContent(InitialPositionConciergeView view, Point mousePoint) =>
        InitialPositionConcierge.Draw(view, mousePoint,
            new InitialPositionConciergeDrawingCallbacks(DrawDynamicOptionText, DrawFittedText, DrawText, FillRect, DrawRect, DrawCommandButton));

    public void DrawPopupNumberUnderline(Point mousePosition, string title, string text, int caretIndex,
        int selectionStart, int selectionLength, string message, PopupNumberUnderlineOptions options = default)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        HeadUpDisplay.PopupNumberUnderline.Draw(mousePoint, title, text, caretIndex, selectionStart, selectionLength, message,
            new PopupNumberUnderlineDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRect,
                DrawText, DrawFittedText, DrawTextBoxSelection, value => _font.MeasureString(value).X, _stationeryDrawingContext,
                DrawLine, DrawSharpCenteredFittedText), options);
        _spriteBatch.End();
    }

    public int GetPopupNumberUnderlineCaretIndex(Point point, string text) =>
        HeadUpDisplay.PopupNumberUnderline.GetCaretIndex(point, text, GetTextBoxCaretIndex);

    public void DrawPopupTimeUnderline(Point mousePosition, string[] values, int[] carets, int activePart, string message)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        HeadUpDisplay.PopupTimeUnderline.Draw(mousePoint, values, carets, activePart, message,
            new PopupTimeUnderlineDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRect,
                DrawText, DrawFittedText, value => _font.MeasureString(value).X, _stationeryDrawingContext, DrawLine, DrawSharpCenteredFittedText));
        _spriteBatch.End();
    }

    public int GetPopupTimeUnderlineCaretIndex(int part, Point point, string text) =>
        HeadUpDisplay.PopupTimeUnderline.GetCaretIndex(part, point, text, GetTextBoxCaretIndex);

    public void SetStickyNoteScreen(StickyNoteScreenId screen) => HeadUpDisplay.StickyNoteScreen = screen;

    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(HeadUpDisplay.StickyNoteScreen)) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRect, DrawRect, DrawDynamicOptionText));
    }

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

    public void DrawTextAreaDialog(Point mousePosition, string title, string text, int caretIndex, string message, bool hasChanges,
        TextCompositionState composition = default, TextCompositionDiagnostics compositionDiagnostics = default,
        bool showCompositionDiagnostics = false)
    {
        var dialog = TextAreaDialog.Default;
        dialog.SetHasChanges(hasChanges);
        var dialogBounds = dialog.Bounds;
        var textBounds = dialog.TextBounds;
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 145));
        FillRect(new Rectangle(dialogBounds.X + 14, dialogBounds.Y + 16, dialogBounds.Width, dialogBounds.Height), new Color(0, 0, 0, 155));
        FillRect(dialogBounds, new Color(24, 29, 36, 252));
        DrawRect(dialogBounds, 2, new Color(116, 145, 146));
        DrawText("COMMENT EDITOR", new Vector2(dialogBounds.X + 34, dialogBounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText(title, new Rectangle(dialogBounds.X + 36, dialogBounds.Y + 96, dialogBounds.Width - 72, 40), new Color(180, 195, 195), 0.42f);
        if (showCompositionDiagnostics)
        {
            DrawCompositionLamp(dialogBounds, "SDL", 1100, compositionDiagnostics.IsSdlWindowResolved, new Color(99, 223, 185));
            DrawCompositionLamp(dialogBounds, "HOOK", 1146, compositionDiagnostics.IsWindowProcedureAttached, new Color(99, 223, 185));
            DrawCompositionLamp(dialogBounds, "IME", 1192, composition.IsActive, new Color(255, 225, 128));
        }
        const int textPixelHeight = 26;
        const int extraLineSpacing = 5;
        _multilineTextUnderline.Bounds = textBounds;
        _multilineTextUnderline.LineHeight = _textRasterizer.MeasureLineHeight(textPixelHeight, extraLineSpacing);
        _multilineTextUnderline.BaselineOffset = _textRasterizer.MeasureBaselineOffset(textPixelHeight);
        var virtualScreenScale = Math.Max(0.01f, VirtualScreen.GetScale(_graphicsDevice.Viewport));
        _multilineTextUnderline.Underline.Thickness = Math.Max(1, (int)MathF.Ceiling(1f / virtualScreenScale));
        _multilineTextUnderline.SetEditing(true);
        _multilineTextUnderline.UpdatePointer(mousePoint);
        _multilineTextUnderline.Draw(_stationeryDrawingContext);
        DrawTextAreaContent(text, textBounds);
        var caret = GetTextAreaCaretPosition(text, caretIndex);
        if (composition.IsActive && !string.IsNullOrEmpty(composition.Text))
        {
            var compositionWidth = DrawDynamicCompositionText(composition.Text, caret, new Color(255, 225, 128), 0.52f);
            DrawLine(caret + new Vector2(0, 29), caret + new Vector2(compositionWidth, 29), 2, new Color(255, 225, 128));
        }
        FillRect(new Rectangle((int)caret.X, (int)caret.Y, 2, 29), composition.IsActive ? new Color(255, 225, 128) : new Color(147, 244, 200));
        DrawDynamicOptionText(message, new Rectangle(dialogBounds.X + 70, 752, 820, 34), new Color(180, 195, 195), 0.34f);
        DrawFittedText("ENTER: NEW LINE   CTRL+ENTER: SAVE SGF", new Rectangle(dialogBounds.X + 70, 786, 800, 28), new Color(147, 201, 190), 0.29f);
        dialog.DiscardButton.Draw(mousePoint, _stationeryDrawingContext);
        dialog.ApplyButton.Draw(mousePoint, _stationeryDrawingContext);
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
        var textBounds = TextAreaDialog.Default.TextBounds;
        var x = textBounds.X + 18 + (int)MathF.Round(_textRasterizer.MeasureTextWidth(lineText, pixelHeight: 26, bold: false));
        var lineHeight = _textRasterizer.MeasureLineHeight(pixelHeight: 26, extraLineSpacing: 5);
        var y = textBounds.Y + 18 + lineNumber * lineHeight;
        return new Vector2(Math.Clamp(x, textBounds.X + 18, textBounds.Right - 22), Math.Clamp(y, textBounds.Y + 18, textBounds.Bottom - 48));
    }

    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
            DrawRenNumber(renParse.GetRenNumber(x, y), BoardPoint(start, cell, x, y), scale);
    }

    internal void DrawLocalPlayingBoardLensButtonStrip(bool isLensEnabled, Point mousePoint)
    {
        DrawFittedText("BOARD LENS  [L] / [J] / [K] / [1]", new Rectangle(1164, 812, 316, 36), new Color(147, 201, 190), 0.26f);
        DrawBoardLensButtonStrip(LocalMatchPlayPage.Default.RightSidePanel.BoardLensButtons, isLensEnabled, mousePoint, 0.32f);
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
        EditEntryProfile.Draw(session, mousePoint, HeadUpDisplay.StickyNoteScreen,
            new EditEntryProfileDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRoundedFill,
                DrawRect, DrawText, DrawFittedText, _stationeryDrawingContext, DrawIconStone, DrawPlayerRoleFaceIcon,
                DrawTextBoxSelection, DrawTextBoxCaret,
                DrawLine, DrawDynamicOptionText, DrawRotatedCenteredText));

}
