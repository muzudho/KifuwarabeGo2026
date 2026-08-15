namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.Gui.Presentation.Pages.ReviewUnsavedChangesConfirmation;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenshotEffect;
using KifuwarabeGo2026.Gui.Presentation.Pages.ScreenTransition;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.Gui.Presentation.Shared.LiveBoardPreview;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.TextAreaDialog;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
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
public sealed class GoScreenRenderer
{
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
    private readonly BoardLensModel _boardLensModel;
    private readonly MoveCommentPanelRenderer _moveCommentPanelRenderer;
    private readonly MoveTrendChartRenderer _moveTrendChartRenderer;
    private readonly PopupTrendChartRenderer _popupTrendChartRenderer;
    private readonly TitleScreenRenderer _titleScreenRenderer;
    private readonly BoardRenderer _boardRenderer;
    public CgosWatchingRenderer CgosWatchingRenderer { get; }
    public GtpEngineRenderer GtpEngineRenderer { get; }
    public CgosLoginRenderer CgosLoginRenderer { get; }
    internal StationeryDrawingContext StationeryDrawingContext => _stationeryDrawingContext;

    // 移設途中: StationeryDrawingContext の拡張と右側パネル共通部品への分離後に削除する一時的な描画ブリッジです。
    private readonly Dictionary<string, Texture2D> _dynamicOptionTextTextures = [];
    private readonly MultilineTextUnderline _multilineTextUnderline = new(
        new SquareUnderline { Thickness = 1 }, "EDIT");
    public HeadUpDisplayComponent HeadUpDisplay { get; } = HeadUpDisplayComponent.Default;
    public InitialPositionConcierge InitialPositionConcierge { get; } = new();
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
        _stoneLight = BoardRenderer.CreateStoneTexture(graphicsDevice, 128, lightStone: true);
        _stoneDark = BoardRenderer.CreateStoneTexture(graphicsDevice, 128, lightStone: false);
        _stationeryDrawingContext = new StationeryDrawingContext(
            this,
            VirtualScreen.Width,
            VirtualScreen.Height,
            FillRect, DrawRoundedFill, DrawRect, DrawLine, DrawCircle, (center, radius, black) => _boardRenderer!.DrawStone(center, radius, black),
            (bounds, color) => _spriteBatch.Draw(_softCircle, bounds, color),
            DrawText, DrawFittedText, DrawSharpCenteredFittedText,
            DrawRotatedCenteredText,
            _font.MeasureString,
            point => VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, point),
            () => _spriteBatch.Begin(samplerState: SamplerState.LinearClamp, transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport)),
            _spriteBatch.End,
            DrawBackground,
            (bounds, label, color) => DrawVerticalResultSection(bounds, label, color),
            DrawCommandButton,
            DrawDynamicOptionText,
            DrawSelectionFingerMark,
            (kind, connectorStart, accent, border, heading, lines, spacing, anchor) =>
                DrawStickyNote(kind, connectorStart, accent, border, heading, lines, spacing, anchor),
            GetTextBoxCaretIndex);
        _boardLensModel = new BoardLensModel(
            BoardRenderer.BoardPoint,
            BoardRenderer.RenGraphCellColor,
            DrawLine,
            DrawCircle,
            FillRect,
            DrawRect,
            DrawEllipseWire,
            (number, center, scale) => _boardRenderer!.DrawRenNumber(number, center, scale),
            (ren, value, color, start, cell, outline) => _boardRenderer!.DrawRenMetricNumber(ren, value, color, start, cell, outline),
            (parse, metrics, start, cell) => _boardRenderer!.DrawDeferredStrongMetrics(parse, metrics, start, cell));
        _boardRenderer = new BoardRenderer(_boardLensModel, _spriteBatch, _font, _boardCoordinateFont,
            _softCircle, _stoneLight, _stoneDark);
        _moveCommentPanelRenderer = new MoveCommentPanelRenderer(
            _graphicsDevice,
            _spriteBatch,
            _textRasterizer,
            _stationeryDrawingContext);
        _moveTrendChartRenderer = new MoveTrendChartRenderer(_moveCommentPanelRenderer);
        _popupTrendChartRenderer = new PopupTrendChartRenderer(_moveTrendChartRenderer);
        CgosWatchingRenderer = new CgosWatchingRenderer(_boardRenderer, _moveTrendChartRenderer,
            _popupTrendChartRenderer, _boardRenderer.DrawBoardRenAnalysis);
        GtpEngineRenderer = new GtpEngineRenderer(_graphicsDevice, _spriteBatch, _font, _textRasterizer);
        CgosLoginRenderer = new CgosLoginRenderer(GtpEngineRenderer, DrawPlayerEditPanel);
        _titleScreenRenderer = new TitleScreenRenderer(DrawEllipseWire, DrawCircumscribedCircleArc);
    }

    public void Draw(
        GoAppSession session,
        Point mousePosition,
        LiveBoardPreviewModel? liveBoardPreview = null,
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
        _boardRenderer.Draw(_stationeryDrawingContext, session, backgroundMousePoint);
        if (session.CurrentMode.Kind == GoAppModeKind.Playing &&
            session.CanOpenLocalChartPopup)
        {
            CgosWatchingRenderer.DrawBroadcastStatusBadge(_stationeryDrawingContext,
                session.IsLocalReplayMode ? "REPLAY" : "CURRENT",
                session.IsReviewChartPopupOpen);
        }
        if (!session.IsReviewChartPopupOpen)
        {
            RightSidePanel.Default.Draw(_stationeryDrawingContext, _moveTrendChartRenderer, session, backgroundMousePoint, liveBoardPreview, initialPositionConcierge);
            if (session.IsLocalReplayMode)
            {
                _popupTrendChartRenderer.DrawReplayNavigationControls(
                    _stationeryDrawingContext,
                    session.LocalDisplayMoveIndex,
                    session.CurrentGameRecord.Moves.Count,
                    backgroundMousePoint,
                    showBackToLive: session.CurrentMode.Kind == GoAppModeKind.Playing,
                    backToLiveLabel: "BACK TO CURRENT");
            }
            else if (session.CanOpenLocalChartPopup ||
                     session.CurrentMode.Kind == GoAppModeKind.Reviewing)
            {
                _popupTrendChartRenderer.DrawReplayEditIconButton(_stationeryDrawingContext, backgroundMousePoint);
            }
            TournamentRulesPresenter.Default.Draw(_stationeryDrawingContext, session, mousePoint);
            SelectEntryPresenter.Default.Draw(_stationeryDrawingContext, session, mousePoint);
            DrawPlayerEditPanel(session, mousePoint);
            EntryProfilesPresenter.Default.DrawPanels(_stationeryDrawingContext, session, mousePoint);
            GtpEngineRenderer.Draw(_stationeryDrawingContext, session, mousePoint);
        }
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing && session.IsReviewChartPopupOpen)
        {
            _popupTrendChartRenderer.DrawReview(_stationeryDrawingContext, session, mousePoint);
        }
        else if (session.CanOpenLocalChartPopup && session.IsReviewChartPopupOpen)
        {
            _popupTrendChartRenderer.DrawLocal(_stationeryDrawingContext, session, mousePoint);
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
        _titleScreenRenderer.Draw(_stationeryDrawingContext, session, mousePoint, page, appProviderTabIndex, isAppProviderLoading);
        SelectEntryPresenter.Default.Draw(_stationeryDrawingContext, session, mousePoint);
        GtpEngineRenderer.Draw(_stationeryDrawingContext, session, mousePoint);

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

    internal static string GetMoveThinkingText(GoAppSession session)
    {
        var text = $"{session.NextMoveNumber}手目を思考中";
        return session.MoveLimit <= 0 ? text : $"{text} / {session.MoveLimit}";
    }

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
    internal void DrawCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled = true, float scale = 0.62f)
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

    internal void DrawStoneCountStrip(GoAppSession session, int y, bool showLeader = true, bool minimal = false)
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
            _stationeryDrawingContext.DrawStoneValue(firstValueX, bounds.Y + 28, blackStones.ToString(), black: true, valueColor: Color.White);
            _stationeryDrawingContext.DrawStoneValue(secondValueX, bounds.Y + 28, whiteStones.ToString(), black: false, valueColor: Color.White);
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

    private void DrawText(string text, Vector2 position, Color color, float scale)
    {
        var shadowAlpha = (int)MathF.Round(125f * color.A / 255f);
        _spriteBatch.DrawString(_font, text, position + new Vector2(2, 2), new Color(0, 0, 0, shadowAlpha), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

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


    public int GetPlayerEditPanelCaretIndex(Point point, EntryProfileEditField field, string text) =>
        EditEntryProfile.GetCaretIndex(point, field, text, GetTextBoxCaretIndex);

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint) =>
        EditEntryProfile.Draw(session, mousePoint, HeadUpDisplay.StickyNoteScreen,
            new EditEntryProfileDrawingCallbacks(VirtualScreen.Width, VirtualScreen.Height, FillRect, DrawRoundedFill,
                DrawRect, DrawText, DrawFittedText, _stationeryDrawingContext, _stationeryDrawingContext.DrawIconStone, _stationeryDrawingContext.DrawPlayerRoleFaceIcon,
                _stationeryDrawingContext.DrawTextSelection, _stationeryDrawingContext.DrawTextCaret,
                DrawLine, DrawDynamicOptionText, DrawRotatedCenteredText));

    public void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            DrawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_dynamicOptionTextTextures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new System.IO.MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[text] = texture;
        }

        var targetHeight = MathF.Min(bounds.Height, _font.LineSpacing * scale);
        var fittedScale = MathF.Min(bounds.Width / (float)texture.Width, targetHeight / texture.Height);
        _spriteBatch.Draw(texture, new Rectangle(bounds.X, bounds.Y + (bounds.Height - (int)(texture.Height * fittedScale)) / 2,
            (int)(texture.Width * fittedScale), (int)(texture.Height * fittedScale)), color);
    }

    private void DrawSelectionFingerMark(Vector2 origin, float scale) =>
        _stationeryDrawingContext.DrawSelectionFinger(origin, scale);

}
