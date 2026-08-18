namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SectionLabel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using System.Collections.Generic;
using System;

/// <summary>画面rendererと文房具UIを分離する共通描画境界です。</summary>
public sealed class KfwStationeryDrawingTools : IDisposable
{
    private readonly KfwScreenCanvas _canvas;
    private readonly Action<Vector2, float, bool> _drawStone;
    private readonly Func<StickyNoteScreenId> _getStickyNoteScreen;
    private readonly DynamicTextRenderer _dynamicTextRenderer;

    public KfwStationeryDrawingTools(
        KfwScreenCanvas canvas,
        ITextRasterizer textRasterizer,
        Action<Vector2, float, bool> drawStone,
        Func<StickyNoteScreenId>? getStickyNoteScreen = null)
    {
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _drawStone = drawStone ?? throw new ArgumentNullException(nameof(drawStone));
        _getStickyNoteScreen = getStickyNoteScreen ?? (() => StickyNoteScreenId.Unknown);
        _dynamicTextRenderer = new DynamicTextRenderer(canvas, textRasterizer);
    }

    public int ScreenWidth => _canvas.ScreenWidth;
    public int ScreenHeight => _canvas.ScreenHeight;
    public void FillRectangle(Rectangle bounds, Color color) => _canvas.FillRectangle(bounds, color);
    public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => _canvas.FillRoundedRectangle(bounds, radius, color);
    public void DrawRectangle(Rectangle bounds, int thickness, Color color) => _canvas.DrawRectangle(bounds, thickness, color);
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _canvas.DrawLine(start, end, thickness, color);
    public void DrawCircle(Vector2 center, float radius, Color color) => _canvas.DrawCircle(center, radius, color);
    public void DrawStone(Vector2 center, float radius, bool black) => _drawStone(center, radius, black);
    public void DrawCircleSurface(Rectangle bounds, Color color) => _canvas.DrawCircleSurface(bounds, color);

    public void DrawIconStone(Vector2 center, float radius, bool black)
    {
        DrawCircle(center, radius + 5, black ? new Color(178, 219, 226) : new Color(72, 80, 84));
        _drawStone(center, radius, black);
        if (black)
            DrawCircle(new Vector2(center.X - radius * 0.28f, center.Y - radius * 0.32f), radius * 0.22f, new Color(255, 255, 255, 42));
    }

    public void DrawPlayerRoleFaceIcon(Vector2 center, bool isComputer)
        => (isComputer ? (Action<Vector2>)DrawEngineIcon : DrawHumanIcon)(center);

    public void DrawMatchedPlayersIcon(Vector2 center)
    {
        DrawIconStone(center + new Vector2(-9, 0), 7, true);
        DrawIconStone(center + new Vector2(9, 0), 7, false);
    }

    public void DrawEntryIcon(Vector2 center)
    {
        var color = new Color(147, 244, 200);
        DrawCircleOutline(center + new Vector2(0, -7), 7, 2, color);
        DrawCircleOutline(center + new Vector2(0, 11), 13, 2, color);
    }

    public void DrawEngineIcon(Vector2 center)
    {
        var color = new Color(125, 225, 255);
            var head = new Rectangle((int)center.X - 10, (int)center.Y - 10, 20, 20);
            FillRectangle(head, new Color(28, 49, 61));
            DrawRectangle(head, 2, color);
            DrawCircle(center + new Vector2(-4, -2), 2, color);
            DrawCircle(center + new Vector2(4, -2), 2, color);
            DrawLine(center + new Vector2(-5, 5), center + new Vector2(5, 5), 2, color);
            DrawLine(center + new Vector2(0, -10), center + new Vector2(0, -14), 2, color);
            DrawCircle(center + new Vector2(0, -15), 2, color);
    }

    public void DrawHumanIcon(Vector2 center)
    {
        var color = new Color(255, 211, 138);
        DrawCircleOutline(center + new Vector2(0, -2), 12, 2, color);
        DrawLine(center + new Vector2(-6, -4), center + new Vector2(-4, -7), 2, color);
        DrawLine(center + new Vector2(-4, -7), center + new Vector2(-2, -4), 2, color);
        DrawLine(center + new Vector2(2, -4), center + new Vector2(4, -7), 2, color);
        DrawLine(center + new Vector2(4, -7), center + new Vector2(6, -4), 2, color);
        DrawLine(center + new Vector2(-6, 3), center + new Vector2(-2, 1), 2, color);
        DrawLine(center + new Vector2(-2, 1), center + new Vector2(2, 4), 2, color);
        DrawLine(center + new Vector2(2, 4), center + new Vector2(6, 1), 2, color);
    }

    public void DrawGuiIcon(Vector2 center)
    {
        var color = new Color(180, 195, 195);
        var board = new Rectangle((int)center.X - 13, (int)center.Y - 13, 26, 26);
        DrawRectangle(board, 2, color);
        for (var i = 1; i < 3; i++)
        {
            DrawLine(new Vector2(board.X + i * 9, board.Y), new Vector2(board.X + i * 9, board.Bottom), 1, color);
            DrawLine(new Vector2(board.X, board.Y + i * 9), new Vector2(board.Right, board.Y + i * 9), 1, color);
        }
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

    public void DrawVerticalResultSection(Rectangle bounds, string title, Color accentColor,
        Color? textColor = null, int labelWidth = 38, int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));
        SectionLabelComponent.CreateVertical(bounds, title, accentColor,
            textColor ?? new Color(205, 218, 218), this, labelWidth, labelGap).Draw(this);
    }

    public void DrawInfoStrip(int x, int y, string label, string value)
    {
        var bounds = new Rectangle(x, y, 668, 72);
        DrawResultLabel(new Rectangle(x + 20, y, bounds.Width - 40, bounds.Height), label, new Color(62, 112, 105));
        DrawFittedText(value, new Rectangle(x + 218, y + 14, 566, 44), Color.White, 0.46f);
    }

    public void DrawResultRow(Rectangle bounds, string label, string value, Color chipColor, Color valueColor)
    {
        FillRectangle(new Rectangle(bounds.X, bounds.Y + 8, 6, bounds.Height - 16), chipColor);
        DrawFittedText(label, new Rectangle(bounds.X + 20, bounds.Y + 8, 170, bounds.Height - 16), new Color(180, 195, 195), 0.34f);
        DrawFittedText(value, new Rectangle(bounds.X + 196, bounds.Y + 6, bounds.Width - 210, bounds.Height - 12), valueColor, 0.43f);
    }

    public void DrawStoneCountStrip(int black, int white, int y, bool showLeader = true, bool minimal = false)
    {
        var blackBounds = new Rectangle(1164, y, minimal ? 260 : 300, 54);
        var whiteBounds = new Rectangle(blackBounds.Right + 16, y, minimal ? 260 : 300, 54);
        FillRectangle(blackBounds, new Color(24, 30, 36));
        FillRectangle(whiteBounds, new Color(238, 238, 232));
        DrawRectangle(blackBounds, 2, new Color(72, 82, 88));
        DrawRectangle(whiteBounds, 2, new Color(142, 148, 148));
        DrawIconStone(new Vector2(blackBounds.X + 30, blackBounds.Center.Y), 16, true);
        DrawIconStone(new Vector2(whiteBounds.X + 30, whiteBounds.Center.Y), 16, false);
        DrawFittedText(black.ToString(), new Rectangle(blackBounds.X + 58, blackBounds.Y + 8, blackBounds.Width - 72, 38), Color.White, 0.46f);
        DrawFittedText(white.ToString(), new Rectangle(whiteBounds.X + 58, whiteBounds.Y + 8, whiteBounds.Width - 72, 38), new Color(30, 35, 38), 0.46f);
        if (!showLeader || black == white) return;
        var leader = black > white ? blackBounds : whiteBounds;
        DrawFittedText("LEAD", new Rectangle(leader.Right - 78, leader.Y + 15, 62, 24),
            black > white ? new Color(147, 244, 200) : new Color(43, 92, 80), 0.24f);
    }

    public void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale) =>
        DrawDynamicText(text, bounds, color, scale);

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
    public void DrawText(string text, Vector2 position, Color color, float scale) => _canvas.DrawText(text, position, color, scale);
    public void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _canvas.DrawFittedText(text, bounds, color, scale);
    public void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float scale) =>
        _canvas.DrawCenteredFittedText(text, bounds, color, scale);
    public void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale) =>
        _canvas.DrawRotatedCenteredText(text, center, color, scale);
    public Vector2 MeasureText(string text) => _canvas.MeasureText(text);
    public Point ToVirtualPoint(Point point) => _canvas.ToVirtualPoint(point);
    public void Begin() => _canvas.Begin();
    public void End() => _canvas.End();
    public void DrawBackground() => BackgroundRenderer.Draw(_canvas);

    /// <summary>
    /// 動的に内容が決まるボタンを文房具 UI の <see cref="Controls.Button.Button"/> として描画します。
    /// 固定ボタンは、画面モデルが Button インスタンスを保持して直接 Draw する方式を優先してください。
    /// </summary>
    public void DrawButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled, float scale)
    {
        var button = new Controls.Button.Button(bounds, label, scale)
        {
            IsSelected = selected,
            IsEnabled = enabled,
        };
        button.Draw(mousePoint, this);
    }

    public void DrawDynamicText(string text, Rectangle bounds, Color color, float scale) =>
        _dynamicTextRenderer.Draw(text, bounds, color, scale);

    public void DrawSelectionFinger(Vector2 origin, float scale)
    {
        var color = new Color(125, 225, 255);
        var thickness = 2f * scale;
        var points = new[]
        {
            origin + new Vector2(0, 2) * scale, origin + new Vector2(5, 2) * scale,
            origin + new Vector2(7, -3) * scale, origin + new Vector2(9, -3) * scale,
            origin + new Vector2(10, 0) * scale, origin + new Vector2(21, 0) * scale,
            origin + new Vector2(24, 3) * scale, origin + new Vector2(21, 6) * scale,
            origin + new Vector2(12, 6) * scale, origin + new Vector2(15, 9) * scale,
            origin + new Vector2(13, 12) * scale, origin + new Vector2(10, 10) * scale,
            origin + new Vector2(11, 14) * scale, origin + new Vector2(8, 16) * scale,
            origin + new Vector2(5, 12) * scale, origin + new Vector2(0, 10) * scale,
            origin + new Vector2(0, 2) * scale,
        };
        for (var index = 1; index < points.Length; index++)
            DrawLine(points[index - 1], points[index], thickness, color);
    }

    public void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(_getStickyNoteScreen())) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRectangle, DrawRectangle, DrawDynamicText));
    }

    public int GetTextCaretIndex(int pointX, string text, Rectangle bounds, float scale)
    {
        if (string.IsNullOrEmpty(text) || pointX <= bounds.X) return 0;
        var fittedScale = GetFittedScale(text, bounds, scale);
        var previousX = (float)bounds.X;
        for (var index = 0; index < text.Length; index++)
        {
            var nextX = bounds.X + MathF.Min(bounds.Width - 2, MeasureText(text[..(index + 1)]).X * fittedScale);
            if (pointX < (previousX + nextX) * 0.5f) return index;
            previousX = nextX;
        }
        return text.Length;
    }

    public void Dispose() => _dynamicTextRenderer.Dispose();
}
