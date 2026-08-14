namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using Microsoft.Xna.Framework;
using System;

/// <summary>画面rendererと文房具UIを分離する共通描画境界です。</summary>
public sealed class StationeryDrawingContext
{
    private readonly Action<Rectangle, Color> _fillRectangle;
    private readonly Action<Rectangle, int, Color> _fillRoundedRectangle;
    private readonly Action<Rectangle, int, Color> _drawRectangle;
    private readonly Action<Vector2, Vector2, float, Color> _drawLine;
    private readonly Action<string, Vector2, Color, float> _drawText;
    private readonly Action<string, Rectangle, Color, float> _drawFittedText;

    public StationeryDrawingContext(
        Action<Rectangle, Color> fillRectangle,
        Action<Rectangle, int, Color> fillRoundedRectangle,
        Action<Rectangle, int, Color> drawRectangle,
        Action<Vector2, Vector2, float, Color> drawLine,
        Action<string, Vector2, Color, float> drawText,
        Action<string, Rectangle, Color, float> drawFittedText)
    {
        _fillRectangle = fillRectangle ?? throw new ArgumentNullException(nameof(fillRectangle));
        _fillRoundedRectangle = fillRoundedRectangle ?? throw new ArgumentNullException(nameof(fillRoundedRectangle));
        _drawRectangle = drawRectangle ?? throw new ArgumentNullException(nameof(drawRectangle));
        _drawLine = drawLine ?? throw new ArgumentNullException(nameof(drawLine));
        _drawText = drawText ?? throw new ArgumentNullException(nameof(drawText));
        _drawFittedText = drawFittedText ?? throw new ArgumentNullException(nameof(drawFittedText));
    }

    public void FillRectangle(Rectangle bounds, Color color) => _fillRectangle(bounds, color);
    public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => _fillRoundedRectangle(bounds, radius, color);
    public void DrawRectangle(Rectangle bounds, int thickness, Color color) => _drawRectangle(bounds, thickness, color);
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawLine(start, end, thickness, color);
    public void DrawText(string text, Vector2 position, Color color, float scale) => _drawText(text, position, color, scale);
    public void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawFittedText(text, bounds, color, scale);
}
