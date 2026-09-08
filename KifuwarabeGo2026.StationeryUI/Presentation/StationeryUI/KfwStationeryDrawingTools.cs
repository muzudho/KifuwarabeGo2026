namespace StationeryUI.MonoGame;

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StationeryUI.Platform;
using StationeryUI.MonoGame.Controls.SectionLabel;
using StationeryUI.MonoGame.Controls.StickyNote;

/// <summary>きふわらべ固有の画面・碁石・結果・付箋配置を共通描画へ追加します。</summary>
public sealed class KfwStationeryDrawingTools : StationeryDrawingTools
{
    private readonly ScreenCanvas _canvas;
    private readonly Action<Vector2,float,bool> _drawStone;
    private readonly Func<StickyNoteScreenId> _getStickyNoteScreen;
    public KfwStationeryDrawingTools(ScreenCanvas canvas,ITextRasterizer textRasterizer,
        Action<Vector2,float,bool> drawStone,Func<StickyNoteScreenId>? getStickyNoteScreen=null)
        : base(canvas,textRasterizer)
    {
        _canvas=canvas;
        _drawStone=drawStone;
        _getStickyNoteScreen=getStickyNoteScreen ?? (()=>StickyNoteScreenId.Unknown);
    }
    public void DrawStone(Vector2 center, float radius, bool black) => _drawStone(center, radius, black);

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
        DrawCircleOutline(center + new Vector2(0, -8), 7, 2, color);

        // Bust silhouette: broad shoulders taper toward an open lower edge so
        // the Entry symbol cannot be mistaken for two stacked circles.
        DrawLine(center + new Vector2(-4, -1), center + new Vector2(-12, 5), 2, color);
        DrawLine(center + new Vector2(-12, 5), center + new Vector2(-10, 18), 2, color);
        DrawLine(center + new Vector2(4, -1), center + new Vector2(12, 5), 2, color);
        DrawLine(center + new Vector2(12, 5), center + new Vector2(10, 18), 2, color);

        // Shirt neckline.
        DrawLine(center + new Vector2(-4, -1), center + new Vector2(0, 8), 2, color);
        DrawLine(center + new Vector2(0, 8), center + new Vector2(4, -1), 2, color);

        // Cover line-cap seams at this small icon size.
        DrawCircle(center + new Vector2(-4, -1), 1.2f, color);
        DrawCircle(center + new Vector2(-12, 5), 1.2f, color);
        DrawCircle(center + new Vector2(4, -1), 1.2f, color);
        DrawCircle(center + new Vector2(12, 5), 1.2f, color);
        DrawCircle(center + new Vector2(0, 8), 1.2f, color);
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

    public void DrawSelectionFingerIcon(Vector2 origin, float scale = 1f)
    {
        var color = new Color(125, 225, 255);
        var thickness = 2f * scale;
        var points = new[]
        {
            origin + new Vector2(0, 2) * scale,
            origin + new Vector2(5, 2) * scale,
            origin + new Vector2(7, -3) * scale,
            origin + new Vector2(9, -3) * scale,
            origin + new Vector2(10, 0) * scale,
            origin + new Vector2(21, 0) * scale,
            origin + new Vector2(24, 3) * scale,
            origin + new Vector2(21, 6) * scale,
            origin + new Vector2(12, 6) * scale,
            origin + new Vector2(10, 12) * scale,
            origin + new Vector2(7, 12) * scale,
            origin + new Vector2(6, 7) * scale,
            origin + new Vector2(0, 7) * scale,
        };
        for (var i = 0; i < points.Length - 1; i++)
            DrawLine(points[i], points[i + 1], thickness, color);
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

    public void DrawStoneValue(int x, int centerY, string value, bool black, Color valueColor)
    {
        DrawIconStone(new Vector2(x + 18, centerY), 16, black);
        DrawText(value, new Vector2(x + 44, centerY - 14), valueColor, 0.5f);
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

    public void DrawBackground() => BackgroundRenderer.Draw(_canvas);

    public void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null)
    {
        var note = new StickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
        if (!note.TryPlace(_getStickyNoteScreen())) return;
        note.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRectangle, DrawRectangle, DrawDynamicText));
    }
}
