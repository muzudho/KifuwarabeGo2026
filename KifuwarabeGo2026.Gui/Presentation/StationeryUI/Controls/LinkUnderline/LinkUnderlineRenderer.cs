namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using System;

/// <summary>
/// 非同期アクションへ接続する文房具 UI のリンクアンダーラインです。
/// 描画方法はホストがコールバックで渡すため、GoScreenRenderer には依存しません。
/// </summary>
public sealed class LinkUnderlineRenderer
{
    public LinkUnderlineRenderer(IUnderlineRenderer underline)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
    }

    /// <summary>
    /// 下線
    /// </summary>
    public IUnderlineRenderer Underline { get; }

    /// <summary>同期リンク向けに、ホバー状態だけで所有する Underline を描画します。</summary>
    public void Draw(Rectangle bounds, bool hovered, IUnderlineDrawingSurface surface)
    {
        Underline.ContentBounds = bounds;
        Underline.Color = hovered ? new Color(185, 196, 255) : new Color(100, 110, 145);
        Underline.Draw(surface);
    }

    /// <summary>状態に応じた色を決め、下線と必要ならスピナーを描画します。</summary>
    /// <param name="drawUnderline">日本語ラベルと下線を描画する処理です。</param>
    /// <param name="drawSpinner">指定位置にスピナーを描画する処理です。</param>
    public void Draw(
        Rectangle bounds,
        string label,
        Point mousePoint,
        LinkUnderlineController controller,
        double nowSeconds,
        Action<Rectangle, string, Color> drawText,
        Action<Vector2, Color> drawSpinner,
        IUnderlineDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(drawText);
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

        drawText(bounds, label, textColor);
        Underline.ContentBounds = bounds;
        Underline.Color = underlineColor;
        Underline.Draw(surface);
        if (controller.IsSpinnerVisible(nowSeconds))
            drawSpinner(new Vector2(bounds.Right - 14, bounds.Center.Y), underlineColor);
    }
}
