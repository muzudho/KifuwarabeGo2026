namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using System.Collections.Generic;
using System;

/// <summary>画面rendererと文房具UIを分離する共通描画境界です。</summary>
public sealed class StationeryDrawingContext
{
    internal GoScreenRenderer ScreenRenderer { get; }
    public int ScreenWidth { get; }
    public int ScreenHeight { get; }
    private readonly Action<Rectangle, Color> _fillRectangle;
    private readonly Action<Rectangle, int, Color> _fillRoundedRectangle;
    private readonly Action<Rectangle, int, Color> _drawRectangle;
    private readonly Action<Vector2, Vector2, float, Color> _drawLine;
    private readonly Action<Vector2, float, Color> _drawCircle;
    private readonly Action<Vector2, float, bool> _drawStone;
    private readonly Action<Rectangle, Color> _drawCircleSurface;
    private readonly Action<string, Vector2, Color, float> _drawText;
    private readonly Action<string, Rectangle, Color, float> _drawFittedText;
    private readonly Action<string, Rectangle, Color, float> _drawCenteredFittedText;
    private readonly Action<string, Vector2, Color, float> _drawRotatedCenteredText;
    private readonly Func<string, Vector2> _measureText;
    private readonly Func<Point, Point> _toVirtualPoint;
    private readonly Action _begin;
    private readonly Action _end;
    private readonly Action _drawBackground;
    private readonly Action<Rectangle, string, Color> _drawVerticalResultSection;
    private readonly Action<Rectangle, string, bool, Point, bool, float> _drawCommandButton;
    private readonly Action<string, Rectangle, Color, float> _drawDynamicText;
    private readonly Action<Vector2, float> _drawSelectionFinger;
    private readonly Action<StickyNoteKind, Vector2, Color, Color, string, IReadOnlyList<string>, int, Rectangle?> _drawStickyNote;
    private readonly Func<int, string, Rectangle, float, int> _getTextCaretIndex;

    public StationeryDrawingContext(
        GoScreenRenderer screenRenderer,
        int screenWidth,
        int screenHeight,
        Action<Rectangle, Color> fillRectangle,
        Action<Rectangle, int, Color> fillRoundedRectangle,
        Action<Rectangle, int, Color> drawRectangle,
        Action<Vector2, Vector2, float, Color> drawLine,
        Action<Vector2, float, Color> drawCircle,
        Action<Vector2, float, bool> drawStone,
        Action<Rectangle, Color> drawCircleSurface,
        Action<string, Vector2, Color, float> drawText,
        Action<string, Rectangle, Color, float> drawFittedText,
        Action<string, Rectangle, Color, float> drawCenteredFittedText,
        Action<string, Vector2, Color, float> drawRotatedCenteredText,
        Func<string, Vector2> measureText,
        Func<Point, Point> toVirtualPoint,
        Action begin,
        Action end,
        Action drawBackground,
        Action<Rectangle, string, Color> drawVerticalResultSection,
        Action<Rectangle, string, bool, Point, bool, float> drawCommandButton,
        Action<string, Rectangle, Color, float> drawDynamicText,
        Action<Vector2, float> drawSelectionFinger,
        Action<StickyNoteKind, Vector2, Color, Color, string, IReadOnlyList<string>, int, Rectangle?> drawStickyNote,
        Func<int, string, Rectangle, float, int> getTextCaretIndex)
    {
        ScreenRenderer = screenRenderer ?? throw new ArgumentNullException(nameof(screenRenderer));
        ScreenWidth = screenWidth;
        ScreenHeight = screenHeight;
        _fillRectangle = fillRectangle ?? throw new ArgumentNullException(nameof(fillRectangle));
        _fillRoundedRectangle = fillRoundedRectangle ?? throw new ArgumentNullException(nameof(fillRoundedRectangle));
        _drawRectangle = drawRectangle ?? throw new ArgumentNullException(nameof(drawRectangle));
        _drawLine = drawLine ?? throw new ArgumentNullException(nameof(drawLine));
        _drawCircle = drawCircle ?? throw new ArgumentNullException(nameof(drawCircle));
        _drawStone = drawStone ?? throw new ArgumentNullException(nameof(drawStone));
        _drawCircleSurface = drawCircleSurface ?? throw new ArgumentNullException(nameof(drawCircleSurface));
        _drawText = drawText ?? throw new ArgumentNullException(nameof(drawText));
        _drawFittedText = drawFittedText ?? throw new ArgumentNullException(nameof(drawFittedText));
        _drawCenteredFittedText = drawCenteredFittedText ?? throw new ArgumentNullException(nameof(drawCenteredFittedText));
        _drawRotatedCenteredText = drawRotatedCenteredText ?? throw new ArgumentNullException(nameof(drawRotatedCenteredText));
        _measureText = measureText ?? throw new ArgumentNullException(nameof(measureText));
        _toVirtualPoint = toVirtualPoint ?? throw new ArgumentNullException(nameof(toVirtualPoint));
        _begin = begin ?? throw new ArgumentNullException(nameof(begin));
        _end = end ?? throw new ArgumentNullException(nameof(end));
        _drawBackground = drawBackground ?? throw new ArgumentNullException(nameof(drawBackground));
        _drawVerticalResultSection = drawVerticalResultSection ?? throw new ArgumentNullException(nameof(drawVerticalResultSection));
        _drawCommandButton = drawCommandButton ?? throw new ArgumentNullException(nameof(drawCommandButton));
        _drawDynamicText = drawDynamicText ?? throw new ArgumentNullException(nameof(drawDynamicText));
        _drawSelectionFinger = drawSelectionFinger ?? throw new ArgumentNullException(nameof(drawSelectionFinger));
        _drawStickyNote = drawStickyNote ?? throw new ArgumentNullException(nameof(drawStickyNote));
        _getTextCaretIndex = getTextCaretIndex ?? throw new ArgumentNullException(nameof(getTextCaretIndex));
    }

    public void FillRectangle(Rectangle bounds, Color color) => _fillRectangle(bounds, color);
    public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => _fillRoundedRectangle(bounds, radius, color);
    public void DrawRectangle(Rectangle bounds, int thickness, Color color) => _drawRectangle(bounds, thickness, color);
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawLine(start, end, thickness, color);
    public void DrawCircle(Vector2 center, float radius, Color color) => _drawCircle(center, radius, color);
    public void DrawStone(Vector2 center, float radius, bool black) => _drawStone(center, radius, black);
    public void DrawCircleSurface(Rectangle bounds, Color color) => _drawCircleSurface(bounds, color);

    public void DrawIconStone(Vector2 center, float radius, bool black)
    {
        DrawCircle(center, radius + 5, black ? new Color(178, 219, 226) : new Color(72, 80, 84));
        _drawStone(center, radius, black);
        if (black)
            DrawCircle(new Vector2(center.X - radius * 0.28f, center.Y - radius * 0.32f), radius * 0.22f, new Color(255, 255, 255, 42));
    }

    public void DrawPlayerRoleFaceIcon(Vector2 center, bool isComputer)
    {
        var color = isComputer ? new Color(125, 225, 255) : new Color(255, 211, 138);
        if (isComputer)
        {
            var head = new Rectangle((int)center.X - 10, (int)center.Y - 10, 20, 20);
            FillRectangle(head, new Color(28, 49, 61));
            DrawRectangle(head, 2, color);
            DrawCircle(center + new Vector2(-4, -2), 2, color);
            DrawCircle(center + new Vector2(4, -2), 2, color);
            DrawLine(center + new Vector2(-5, 5), center + new Vector2(5, 5), 2, color);
            DrawLine(center + new Vector2(0, -10), center + new Vector2(0, -14), 2, color);
            DrawCircle(center + new Vector2(0, -15), 2, color);
            return;
        }

        DrawCircleOutline(center + new Vector2(0, -2), 12, 2, color);
        DrawLine(center + new Vector2(-6, -4), center + new Vector2(-4, -7), 2, color);
        DrawLine(center + new Vector2(-4, -7), center + new Vector2(-2, -4), 2, color);
        DrawLine(center + new Vector2(2, -4), center + new Vector2(4, -7), 2, color);
        DrawLine(center + new Vector2(4, -7), center + new Vector2(6, -4), 2, color);
        DrawLine(center + new Vector2(-6, 3), center + new Vector2(-2, 1), 2, color);
        DrawLine(center + new Vector2(-2, 1), center + new Vector2(2, 4), 2, color);
        DrawLine(center + new Vector2(2, 4), center + new Vector2(6, 1), 2, color);
    }

    public void DrawTextSelection(string text, int start, int length, Rectangle bounds, float scale)
    {
        if (length <= 0 || start < 0 || start >= text.Length) return;
        var end = Math.Min(text.Length, start + length);
        var fittedScale = GetFittedScale(text, bounds, scale);
        var startX = bounds.X + MeasureText(text[..start]).X * fittedScale;
        var endX = bounds.X + MeasureText(text[..end]).X * fittedScale;
        FillRectangle(new Rectangle((int)startX, bounds.Y + 3, Math.Max(2, (int)MathF.Ceiling(endX - startX)), bounds.Height - 6), new Color(50, 108, 139, 210));
    }

    public void DrawTextCaret(string text, int caret, Rectangle bounds, float scale)
    {
        var prefix = text[..Math.Clamp(caret, 0, text.Length)];
        var x = bounds.X + MathF.Min(bounds.Width - 2, MeasureText(prefix).X * GetFittedScale(text, bounds, scale));
        DrawLine(new Vector2(x, bounds.Y + 5), new Vector2(x, bounds.Bottom - 5), 2, new Color(147, 244, 200));
    }

    public void DrawDataRowFrame(Rectangle bounds, bool active = false, bool hovered = false)
    {
        var fill = active ? new Color(28, 41, 45) : hovered ? new Color(28, 36, 43) : new Color(21, 28, 34);
        var line = active ? new Color(104, 191, 165) : hovered ? new Color(58, 77, 85) : new Color(43, 56, 63);
        FillRectangle(bounds, fill);
        FillRectangle(new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), line);
        FillRectangle(new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), line);
        if (active) FillRectangle(new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), new Color(99, 223, 185));
    }

    public void DrawResultLabel(Rectangle bounds, string label, Color accentColor)
    {
        FillRectangle(new Rectangle(bounds.X - 22, bounds.Center.Y - 14, 3, 28), accentColor);
        DrawText(label, new Vector2(bounds.X - 8, bounds.Y + 14), new Color(180, 195, 195), 0.38f);
    }

    public void DrawStoneValue(int x, int centerY, string value, bool black, Color valueColor)
    {
        DrawIconStone(new Vector2(x + 18, centerY), 16, black);
        DrawText(value, new Vector2(x + 44, centerY - 14), valueColor, 0.5f);
    }

    private float GetFittedScale(string text, Rectangle bounds, float scale)
    {
        var measured = MeasureText(text);
        return MathF.Min(scale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
    }

    private void DrawCircleOutline(Vector2 center, float radius, int thickness, Color color)
    {
        const int segments = 24;
        var previous = center + new Vector2(radius, 0);
        for (var index = 1; index <= segments; index++)
        {
            var angle = MathHelper.TwoPi * index / segments;
            var current = center + new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius);
            DrawLine(previous, current, thickness, color);
            previous = current;
        }
    }
    public void DrawText(string text, Vector2 position, Color color, float scale) => _drawText(text, position, color, scale);
    public void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawFittedText(text, bounds, color, scale);
    public void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float scale) =>
        _drawCenteredFittedText(text, bounds, color, scale);
    public void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _drawRotatedCenteredText(text, center, color, scale);
    public Vector2 MeasureText(string text) => _measureText(text);
    public Point ToVirtualPoint(Point point) => _toVirtualPoint(point);
    public void Begin() => _begin();
    public void End() => _end();
    public void DrawBackground() => _drawBackground();
    public void DrawVerticalResultSection(Rectangle bounds, string label, Color color) =>
        _drawVerticalResultSection(bounds, label, color);
    public void DrawCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled, float scale) =>
        _drawCommandButton(bounds, label, selected, mousePoint, enabled, scale);
    public void DrawDynamicText(string text, Rectangle bounds, Color color, float scale) => _drawDynamicText(text, bounds, color, scale);
    public void DrawSelectionFinger(Vector2 origin, float scale) => _drawSelectionFinger(origin, scale);
    public void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null) =>
        _drawStickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
    public int GetTextCaretIndex(int pointX, string text, Rectangle bounds, float scale) =>
        _getTextCaretIndex(pointX, text, bounds, scale);
}
