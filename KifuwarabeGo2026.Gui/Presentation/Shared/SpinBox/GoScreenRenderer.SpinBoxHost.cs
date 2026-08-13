namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.Shared.SpinBox;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    private readonly SpinBox _spinBox = new();

    private void DrawSpinBox(Rectangle upBounds, Rectangle downBounds, string amountLabel, Point mousePoint) =>
        _spinBox.Draw(upBounds, downBounds, amountLabel, mousePoint, new SpinBoxDrawingCallbacks(FillRect, DrawCenteredFittedText));

    private void DrawCenteredFittedText(string text, Rectangle bounds, Color color, float preferredScale)
    {
        var measured = _font.MeasureString(text);
        var scale = MathF.Min(preferredScale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * scale;
        DrawText(text, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), color, scale);
    }
}
