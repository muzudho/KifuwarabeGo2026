namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;

using Microsoft.Xna.Framework;

/// <summary>Link Underline の入力領域を判定します。</summary>
public static class LinkUnderlineHitTest
{
    public static bool IsHit(Rectangle bounds, Point point) => bounds.Contains(point);
}
