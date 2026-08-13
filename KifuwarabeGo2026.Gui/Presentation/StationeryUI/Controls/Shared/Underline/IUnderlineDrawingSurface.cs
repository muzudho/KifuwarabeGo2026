namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;

/// <summary>
///     下線の構成要素であるサーフェスを描画する機能
/// </summary>
public interface IUnderlineDrawingSurface
{
    void FillRectangle(Rectangle bounds, Color color);
    void FillRoundedRectangle(Rectangle bounds, int radius, Color color);
    void DrawLine(Vector2 start, Vector2 end, float thickness, Color color);
}
