namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;

/// <summary>Underline が利用する最小限の描画面です。</summary>
public interface IUnderlineDrawingSurface
{
    void FillRectangle(Rectangle bounds, Color color);
    void FillRoundedRectangle(Rectangle bounds, int radius, Color color);
    void DrawLine(Vector2 start, Vector2 end, float thickness, Color color);
}
