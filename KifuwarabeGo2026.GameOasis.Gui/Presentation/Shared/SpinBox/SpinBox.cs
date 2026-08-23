namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.SpinBox;

using Microsoft.Xna.Framework;
using System;

/// <summary>数値の増減用スピンボックスを描画する独立コンポーネントです。</summary>
public sealed class SpinBox
{
    private static readonly Color BorderColor = new(105, 127, 134);
    private static readonly Color ButtonColor = new(31, 40, 47);
    private static readonly Color HoveredButtonColor = new(53, 66, 75);
    private static readonly Color LabelColor = new(142, 157, 158);

    public void Draw(Rectangle upBounds, Rectangle downBounds, string amountLabel, Point mousePoint, SpinBoxDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        DrawTriangle(upBounds, true, BorderColor, draw.FillRectangle);
        DrawTriangle(downBounds, false, BorderColor, draw.FillRectangle);
        DrawTriangle(InnerBounds(upBounds, true), true, upBounds.Contains(mousePoint) ? HoveredButtonColor : ButtonColor, draw.FillRectangle);
        DrawTriangle(InnerBounds(downBounds, false), false, downBounds.Contains(mousePoint) ? HoveredButtonColor : ButtonColor, draw.FillRectangle);
        var wholeBounds = Rectangle.Union(upBounds, downBounds);
        var gapTop = upBounds.Bottom;
        var labelBounds = new Rectangle(wholeBounds.X + 4, gapTop, wholeBounds.Width - 8, Math.Max(1, downBounds.Top - gapTop));
        draw.DrawCenteredFittedText(amountLabel, labelBounds, LabelColor, 0.38f);
    }

    private static void DrawTriangle(Rectangle bounds, bool pointsUp, Color color, Action<Rectangle, Color> fillRectangle)
    {
        for (var row = 0; row < bounds.Height; row++)
        {
            var distanceFromTip = pointsUp ? row : bounds.Height - 1 - row;
            var halfWidth = Math.Max(1, (distanceFromTip + 1) * bounds.Width / (bounds.Height * 2));
            fillRectangle(new Rectangle(bounds.Center.X - halfWidth, bounds.Y + row, halfWidth * 2, 1), color);
        }
    }

    private static Rectangle InnerBounds(Rectangle bounds, bool pointsUp) => pointsUp
        ? new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 3)
        : new Rectangle(bounds.X + 3, bounds.Y, bounds.Width - 6, bounds.Height - 3);
}

public sealed record SpinBoxDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<string, Rectangle, Color, float> DrawCenteredFittedText);
