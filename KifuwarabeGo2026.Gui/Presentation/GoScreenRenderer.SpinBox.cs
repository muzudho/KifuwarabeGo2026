namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 数値の増減に使う共通スピンボックス部品。
/// 上下ボタンの間に、増分の絶対値を表示する。
/// </summary>
public sealed partial class GoScreenRenderer
{
    private static readonly Color SpinBoxBorderColor = new(105, 127, 134);
    private static readonly Color SpinBoxButtonColor = new(31, 40, 47);
    private static readonly Color SpinBoxHoveredButtonColor = new(53, 66, 75);
    private static readonly Color SpinBoxLabelColor = new(142, 157, 158);

    private void DrawSpinBox(Rectangle upBounds, Rectangle downBounds, string amountLabel, Point mousePoint)
    {
        DrawFilledSpinTriangle(upBounds, pointsUp: true, SpinBoxBorderColor);
        DrawFilledSpinTriangle(downBounds, pointsUp: false, SpinBoxBorderColor);
        DrawFilledSpinTriangle(
            SpinTriangleInnerBounds(upBounds, pointsUp: true),
            pointsUp: true,
            upBounds.Contains(mousePoint) ? SpinBoxHoveredButtonColor : SpinBoxButtonColor);
        DrawFilledSpinTriangle(
            SpinTriangleInnerBounds(downBounds, pointsUp: false),
            pointsUp: false,
            downBounds.Contains(mousePoint) ? SpinBoxHoveredButtonColor : SpinBoxButtonColor);

        var wholeBounds = Rectangle.Union(upBounds, downBounds);
        var gapTop = upBounds.Bottom;
        var gapHeight = Math.Max(1, downBounds.Top - gapTop);
        var labelBounds = new Rectangle(wholeBounds.X + 4, gapTop, wholeBounds.Width - 8, gapHeight);
        DrawCenteredFittedText(amountLabel, labelBounds, SpinBoxLabelColor, 0.38f);
    }

    private void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float preferredScale)
    {
        var measured = _font.MeasureString(text);
        var scale = MathF.Min(
            preferredScale,
            MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * scale;
        DrawText(text, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, scale);
    }

    private void DrawFilledSpinTriangle(Rectangle bounds, bool pointsUp, Color color)
    {
        for (var row = 0; row < bounds.Height; row++)
        {
            var distanceFromTip = pointsUp ? row : bounds.Height - 1 - row;
            var halfWidth = Math.Max(1, (distanceFromTip + 1) * bounds.Width / (bounds.Height * 2));
            FillRect(new Rectangle(bounds.Center.X - halfWidth, bounds.Y + row, halfWidth * 2, 1), color);
        }
    }

    private static Rectangle SpinTriangleInnerBounds(Rectangle bounds, bool pointsUp) =>
        pointsUp
            ? new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 3)
            : new Rectangle(bounds.X + 3, bounds.Y, bounds.Width - 6, bounds.Height - 3);
}
