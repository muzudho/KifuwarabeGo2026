namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
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
    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;
    private readonly Texture2D _pixel;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;
    private readonly StationeryDrawingContext _stationeryDrawingContext;
    private readonly BoardRenderer _boardRenderer;
    public GoPresentationRenderer Presentation { get; }
    internal StationeryDrawingContext StationeryDrawingContext => _stationeryDrawingContext;

    // 移設途中: StationeryDrawingContext の拡張と右側パネル共通部品への分離後に削除する一時的な描画ブリッジです。
    private readonly DynamicTextRenderer _dynamicTextRenderer;

    public GoScreenRenderer(
        GraphicsDevice graphicsDevice,
        ContentManager content,
        ITextRasterizer textRasterizer)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("Fonts/Ui");
        _boardCoordinateFont = content.Load<SpriteFont>("Fonts/BoardCoordinate");
        _pixel = CreateTexture(1, 1, (_, _) => Color.White);
        _softCircle = CreateCircleTexture(128, new Color(255, 255, 255, 255), softEdge: true);
        _stoneLight = BoardRenderer.CreateStoneTexture(graphicsDevice, 128, lightStone: true);
        _stoneDark = BoardRenderer.CreateStoneTexture(graphicsDevice, 128, lightStone: false);
        _dynamicTextRenderer = new DynamicTextRenderer(graphicsDevice, _spriteBatch, _font, textRasterizer, DrawFittedText);
        _stationeryDrawingContext = new StationeryDrawingContext(
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
            DrawCommandButton,
            _dynamicTextRenderer.Draw,
            DrawSelectionFingerMark,
            (kind, connectorStart, accent, border, heading, lines, spacing, anchor) =>
                DrawStickyNote(kind, connectorStart, accent, border, heading, lines, spacing, anchor),
            GetTextBoxCaretIndex);
        var boardLensModel = new BoardLensModel(
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
        _boardRenderer = new BoardRenderer(boardLensModel, _spriteBatch, _font, _boardCoordinateFont,
            _softCircle, _stoneLight, _stoneDark);
        var moveCommentPanelRenderer = new MoveCommentPanelRenderer(
            _graphicsDevice,
            _spriteBatch,
            textRasterizer,
            _stationeryDrawingContext);
        var moveTrendChartRenderer = new MoveTrendChartRenderer(moveCommentPanelRenderer);
        var popupTrendChartRenderer = new PopupTrendChartRenderer(moveTrendChartRenderer);
        var cgosWatchingRenderer = new CgosWatchingRenderer(_boardRenderer, moveTrendChartRenderer,
            popupTrendChartRenderer, _boardRenderer.DrawBoardRenAnalysis);
        var gtpEngineRenderer = new GtpEngineRenderer(_graphicsDevice, _spriteBatch, _font, textRasterizer);
        var cgosLoginRenderer = new CgosLoginRenderer(gtpEngineRenderer,
            (session, mousePoint) => EditEntryProfile.Default.Draw(
                _stationeryDrawingContext, session, mousePoint, HeadUpDisplayComponent.Default.StickyNoteScreen));
        var titleScreenRenderer = new TitleScreenRenderer(DrawEllipseWire, DrawCircumscribedCircleArc);
        Presentation = new GoPresentationRenderer(_stationeryDrawingContext, _boardRenderer,
            moveTrendChartRenderer, popupTrendChartRenderer, cgosWatchingRenderer,
            gtpEngineRenderer, cgosLoginRenderer, titleScreenRenderer);
    }

    /// <summary>バックグラウンドで設定ファイルを保存している間の入力遮断表示です。</summary>
    /// <summary>
    /// ［大会ルール設定　＞　コミ］のテキストボックスがクリックされたかどうかを判定します。
    /// XXX: なんでここにあるんだろう？　このクラスは画面描画の共通処理のはずなのに。
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>

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
            _dynamicTextRenderer.Draw(label, new Rectangle(bounds.X + 10, bounds.Y + 5, bounds.Width - 20, bounds.Height - 10), textColor, requestedScale);
        }
    }

    /// <summary>石の右側に、人間またはコンピューターの操作主体を示す顔アイコンを描きます。</summary>






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


    private void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _spriteBatch.DrawString(_font, text, center, color, -MathHelper.PiOver2, _font.MeasureString(text) / 2f, scale, SpriteEffects.None, 0f);

    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(HeadUpDisplayComponent.Default.StickyNoteScreen)) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRect, DrawRect, _dynamicTextRenderer.Draw));
    }


    private void DrawSelectionFingerMark(Vector2 origin, float scale) =>
        _stationeryDrawingContext.DrawSelectionFinger(origin, scale);

}
