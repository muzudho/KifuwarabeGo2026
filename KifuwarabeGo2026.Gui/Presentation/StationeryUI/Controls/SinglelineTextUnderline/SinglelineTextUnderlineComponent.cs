namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;

/// <summary>単一行テキスト入力のアンダーライン表示を担当します。</summary>
public sealed class SinglelineTextUnderline
{
    public SinglelineTextUnderline(IUnderline underline)
    {
        Underline = underline ?? throw new ArgumentNullException(nameof(underline));
    }

    public IUnderline Underline { get; }

    public void Draw(Rectangle textBounds, bool isEditing, bool isHovered, IUnderlineDrawingSurface surface)
    {
        Underline.ContentBounds = textBounds;
        Underline.Color = isEditing
            ? new Color(147, 244, 200)
            : isHovered ? new Color(185, 196, 255) : new Color(100, 110, 145);
        Underline.Draw(surface);
    }
}
