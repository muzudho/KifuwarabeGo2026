namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 下線
/// </summary>
public abstract class AbstractUnderlineRenderer : IUnderlineRenderer
{
    public Rectangle ContentBounds { get; set; }
    public int TopOffset { get; set; }
    public int Thickness { get; set; } = 1;
    public Color Color { get; set; } = Color.White;

    protected Rectangle UnderlineBounds => new(
        ContentBounds.X,
        ContentBounds.Bottom + TopOffset,
        ContentBounds.Width,
        Thickness);

    public void Draw(IUnderlineDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        DrawCore(surface);
    }

    protected abstract void DrawCore(IUnderlineDrawingSurface surface);
}
