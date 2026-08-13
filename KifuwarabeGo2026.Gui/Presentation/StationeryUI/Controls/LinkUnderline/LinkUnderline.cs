namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    public static bool IsLinkUnderlineHit(Rectangle bounds, Point point) => bounds.Contains(point);

    /// <summary>Link Underline と、その非同期実行状態を描画します。SpriteBatch 開始中に呼び出してください。</summary>
    public void DrawLinkUnderline(
        Rectangle bounds,
        string label,
        Point mousePoint,
        LinkUnderlineController controller,
        double nowSeconds)
    {
        var hovered = controller.CanActivate && bounds.Contains(mousePoint);
        var underlineColor = controller.State switch
        {
            LinkUnderlineState.Executing => new Color(255, 210, 128),
            LinkUnderlineState.Failed or LinkUnderlineState.Interrupted => new Color(255, 145, 151),
            _ when hovered => new Color(185, 196, 255),
            _ => new Color(100, 110, 145),
        };
        var textColor = controller.IsExecuting ? new Color(255, 225, 160) : Color.White;

        DrawDynamicOptionText(label, bounds, textColor, 0.34f);
        DrawRoundedFill(new Rectangle(bounds.X, bounds.Bottom + 2, bounds.Width, 4), 2, underlineColor);

        if (controller.IsSpinnerVisible(nowSeconds))
            DrawLinkUnderlineSpinner(new Vector2(bounds.Right - 14, bounds.Center.Y), underlineColor);

        if (!string.IsNullOrEmpty(controller.Message))
        {
            DrawDynamicOptionText(
                controller.Message,
                new Rectangle(bounds.X, bounds.Bottom + 12, bounds.Width, 28),
                underlineColor,
                0.25f);
        }
    }

    private void DrawLinkUnderlineSpinner(Vector2 center, Color color)
    {
        const int segmentCount = 10;
        var head = (int)(System.Environment.TickCount64 / 70 % segmentCount);
        for (var index = 0; index < segmentCount; index++)
        {
            var angle = MathF.Tau * index / segmentCount;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var distance = (head - index + segmentCount) % segmentCount;
            var opacity = (byte)System.Math.Clamp(235 - distance * 18, 60, 235);
            DrawLine(center + direction * 7, center + direction * 13, 3, new Color(color.R, color.G, color.B, opacity));
        }
    }
}
