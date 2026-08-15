namespace KifuwarabeGo2026.Gui.Presentation.Shared.Breadcrumb;

using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>画面下部に表示する現在位置パンくずのレイアウトを所有します。</summary>
public sealed class Breadcrumb
{
    public void Draw(KfwStationeryDrawingTools drawingContext, string path, bool visible = true)
    {
        if (!visible) return;
        drawingContext.Begin();
        Draw(path, drawingContext.ScreenWidth, drawingContext.MeasureText,
            new BreadcrumbDrawingCallbacks(drawingContext.FillRectangle, drawingContext.DrawFittedText));
        drawingContext.End();
    }

    private const int Left = 24;
    private const int Top = 1036;
    private const int Height = 36;

    public void Draw(string path, int screenWidth, Func<string, Vector2> measureText,
        BreadcrumbDrawingCallbacks draw, float textScale = 0.40f)
    {
        ArgumentNullException.ThrowIfNull(measureText);
        ArgumentNullException.ThrowIfNull(draw);
        var textWidth = (int)MathF.Ceiling(measureText(path).X * textScale);
        var bounds = new Rectangle(Left, Top, Math.Min(screenWidth - Left * 2, textWidth + 28), Height);
        draw.FillRectangle(bounds, new Color(0, 0, 0, 160));
        draw.DrawFittedText(path, new Rectangle(bounds.X + 14, bounds.Y + 5, bounds.Width - 28, bounds.Height - 10), new Color(225, 240, 232), textScale);
    }
}

public sealed record BreadcrumbDrawingCallbacks(
    Action<Rectangle, Color> FillRectangle,
    Action<string, Rectangle, Color, float> DrawFittedText);
