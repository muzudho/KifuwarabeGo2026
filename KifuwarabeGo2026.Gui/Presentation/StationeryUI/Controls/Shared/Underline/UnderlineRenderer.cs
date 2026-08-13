namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 文房具 UI のアンダーラインを描画します。
/// 描画先はコールバックで受け取るため、特定のレンダラーや座標系には依存しません。
/// </summary>
public static class UnderlineRenderer
{
    public static void DrawRounded(
        Rectangle contentBounds,
        int topOffset,
        int thickness,
        int radius,
        Color color,
        Action<Rectangle, int, Color> drawRoundedFill)
    {
        ArgumentNullException.ThrowIfNull(drawRoundedFill);
        drawRoundedFill(
            new Rectangle(contentBounds.X, contentBounds.Bottom + topOffset, contentBounds.Width, thickness),
            radius,
            color);
    }

    public static void DrawLine(
        Vector2 start,
        Vector2 end,
        float thickness,
        Color color,
        Action<Vector2, Vector2, float, Color> drawLine)
    {
        ArgumentNullException.ThrowIfNull(drawLine);
        drawLine(start, end, thickness, color);
    }
}
