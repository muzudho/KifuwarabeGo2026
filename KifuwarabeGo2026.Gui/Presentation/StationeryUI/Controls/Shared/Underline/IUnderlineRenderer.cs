namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;

/// <summary>
/// 下線
/// </summary>
public interface IUnderlineRenderer
{
    Rectangle ContentBounds { get; set; }
    int TopOffset { get; set; }
    int Thickness { get; set; }
    Color Color { get; set; }
    void Draw(IUnderlineDrawingSurface surface);
}
