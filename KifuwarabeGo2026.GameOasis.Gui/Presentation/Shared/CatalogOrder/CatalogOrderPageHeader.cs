namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.CatalogOrder;

using Microsoft.Xna.Framework;
using System;

/// <summary>見開きページ番号と中央区切り線を描画します。</summary>
public sealed class CatalogOrderPageHeader
{
    public void Draw(int firstPageIndex, int pageCount, CatalogOrderPageHeaderDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var board = CatalogOrderEditorLayout.BoardBounds;
        draw.DrawText($"PAGE {firstPageIndex + 1}", new Vector2(board.X + 16, board.Y + 12), new Color(99, 223, 185), 0.38f);
        if (firstPageIndex + 1 < pageCount)
            draw.DrawText($"PAGE {firstPageIndex + 2}", new Vector2(board.X + 528, board.Y + 12), new Color(99, 223, 185), 0.38f);
        draw.DrawLine(new Vector2(board.X + 520, board.Y + 12), new Vector2(board.X + 520, board.Bottom - 12), 2, new Color(50, 91, 89));
    }
}

public sealed record CatalogOrderPageHeaderDrawingCallbacks(Action<string, Vector2, Color, float> DrawText,
    Action<Vector2, Vector2, float, Color> DrawLine);
