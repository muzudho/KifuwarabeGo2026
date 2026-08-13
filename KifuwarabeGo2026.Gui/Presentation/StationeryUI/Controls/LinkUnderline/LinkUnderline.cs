namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 非同期アクションへ接続する文房具 UI のリンクアンダーラインです。
/// 描画方法はホストがコールバックで渡すため、GoScreenRenderer には依存しません。
/// </summary>
public sealed class LinkUnderline
{
    private readonly Action<Rectangle, string, Color, Color> _drawUnderline;
    private readonly Action<Vector2, Color> _drawSpinner;

    /// <param name="drawUnderline">
    /// ラベル、下線色、文字色を描画します。日本語描画と座標変換はホスト側で担当します。
    /// </param>
    /// <param name="drawSpinner">指定位置にスピナーを描画します。</param>
    public LinkUnderline(
        Action<Rectangle, string, Color, Color> drawUnderline,
        Action<Vector2, Color> drawSpinner)
    {
        _drawUnderline = drawUnderline ?? throw new ArgumentNullException(nameof(drawUnderline));
        _drawSpinner = drawSpinner ?? throw new ArgumentNullException(nameof(drawSpinner));
    }

    public bool IsHit(Rectangle bounds, Point point) => bounds.Contains(point);

    /// <summary>状態に応じた色を決め、下線と必要ならスピナーを描画します。</summary>
    public void Draw(
        Rectangle bounds,
        string label,
        Point mousePoint,
        LinkUnderlineController controller,
        double nowSeconds)
    {
        ArgumentNullException.ThrowIfNull(controller);

        var hovered = controller.CanActivate && IsHit(bounds, mousePoint);
        var underlineColor = controller.State switch
        {
            LinkUnderlineState.Executing => new Color(255, 210, 128),
            LinkUnderlineState.Failed or LinkUnderlineState.Interrupted => new Color(255, 145, 151),
            _ when hovered => new Color(185, 196, 255),
            _ => new Color(100, 110, 145),
        };
        var textColor = controller.IsExecuting ? new Color(255, 225, 160) : Color.White;

        _drawUnderline(bounds, label, underlineColor, textColor);
        if (controller.IsSpinnerVisible(nowSeconds))
            _drawSpinner(new Vector2(bounds.Right - 14, bounds.Center.Y), underlineColor);
    }
}
