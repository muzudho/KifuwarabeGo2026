namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.MultilineTextUnderline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;

/// <summary>複数行テキスト入力欄の罫線を描画する独立コンポーネントです。</summary>
public sealed class MultilineTextUnderline
{
    public MultilineTextUnderline(IUnderline underline)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
    }

    public IUnderline Underline { get; }

    public void Draw(Rectangle bounds, IUnderlineDrawingSurface surface, int lineHeight = 31, int horizontalInset = 18)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var underlineColor = new Color(99, 223, 185, 180);
        for (var y = bounds.Y + horizontalInset + lineHeight - 3; y < bounds.Bottom - horizontalInset; y += lineHeight)
        {
            Underline.ContentBounds = new Rectangle(bounds.X + horizontalInset, y, bounds.Width - horizontalInset * 2, 0);
            Underline.TopOffset = 0;
            Underline.Color = underlineColor;
            Underline.Draw(surface);
        }
    }
}
