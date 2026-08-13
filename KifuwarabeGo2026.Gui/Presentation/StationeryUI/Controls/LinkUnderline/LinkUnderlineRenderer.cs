namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 非同期アクションへ接続する文房具 UI のリンクアンダーラインです。
/// 描画方法はホストがコールバックで渡すため、GoScreenRenderer には依存しません。
/// </summary>
public static class LinkUnderlineRenderer
{
    /// <summary>同期リンク向けに、ホバー状態だけで Link Underline を描画します。</summary>
    public static void Draw(
        Rectangle bounds,
        string label,
        bool hovered,
        Action<Rectangle, string, Color, Color> drawUnderline)
    {
        ArgumentNullException.ThrowIfNull(drawUnderline);
        drawUnderline(
            bounds,
            label,
            hovered ? new Color(185, 196, 255) : new Color(100, 110, 145),
            Color.White);
    }

    /// <summary>状態に応じた色を決め、下線と必要ならスピナーを描画します。</summary>
    /// <param name="drawUnderline">日本語ラベルと下線を描画する処理です。</param>
    /// <param name="drawSpinner">指定位置にスピナーを描画する処理です。</param>
    public static void Draw(
        Rectangle bounds,
        string label,
        Point mousePoint,
        LinkUnderlineController controller,
        double nowSeconds,
        Action<Rectangle, string, Color, Color> drawUnderline,
        Action<Vector2, Color> drawSpinner)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(drawUnderline);
        ArgumentNullException.ThrowIfNull(drawSpinner);

        var hovered = controller.CanActivate && LinkUnderlineHitTest.IsHit(bounds, mousePoint);
        var underlineColor = controller.State switch
        {
            LinkUnderlineState.Executing => new Color(255, 210, 128),
            LinkUnderlineState.Failed or LinkUnderlineState.Interrupted => new Color(255, 145, 151),
            _ when hovered => new Color(185, 196, 255),
            _ => new Color(100, 110, 145),
        };
        var textColor = controller.IsExecuting ? new Color(255, 225, 160) : Color.White;

        drawUnderline(bounds, label, underlineColor, textColor);
        if (controller.IsSpinnerVisible(nowSeconds))
            drawSpinner(new Vector2(bounds.Right - 14, bounds.Center.Y), underlineColor);
    }
}
