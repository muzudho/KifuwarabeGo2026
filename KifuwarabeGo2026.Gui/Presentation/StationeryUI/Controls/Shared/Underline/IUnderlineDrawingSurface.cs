namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;

/// <summary>
///     下線の構成要素であるサーフェスを描画する機能
/// </summary>
public interface IUnderlineDrawingSurface
{
    /// <summary>
    /// 四角形を塗りつぶす
    /// </summary>
    /// <param name="bounds">塗りつぶす範囲</param>
    /// <param name="color">色</param>
    void FillRectangle(Rectangle bounds, Color color);

    /// <summary>
    /// 角が丸い四角形を塗りつぶす
    /// </summary>
    /// <param name="bounds">塗りつぶす範囲</param>
    /// <param name="radius">角の半径</param>
    /// <param name="color">色</param>
    void FillRoundedRectangle(Rectangle bounds, int radius, Color color);

    void DrawLine(Vector2 start, Vector2 end, float thickness, Color color);
}
