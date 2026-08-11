namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System.Collections.Generic;

/// <summary>
/// 対象へ線で結ぶ、タイトル画面用の案内付箋を描画します。
/// </summary>
public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// 見出しと本文を共通の大きさ・余白で描く案内付箋です。
    /// 本文は呼び出し側で行ごとに渡し、狭い欄でも文字を縮小しません。
    /// </summary>
    private void DrawStickyNote(
        Rectangle bounds,
        Vector2 connectorStart,
        Vector2 connectorEnd,
        Color accent,
        Color borderColor,
        string heading,
        IReadOnlyList<string> bodyLines,
        int bodyLineSpacing = 40)
    {
        DrawLine(connectorStart, connectorEnd, 2, accent);
        FillRect(new Rectangle(bounds.X + 9, bounds.Y + 11, bounds.Width, bounds.Height), new Color(0, 0, 0, 115));
        FillRect(bounds, new Color(19, 25, 30, 248));
        DrawRect(bounds, 2, borderColor);
        FillRect(new Rectangle(bounds.X, bounds.Y, 7, bounds.Height), accent);
        DrawDynamicOptionText(heading, new Rectangle(bounds.X + 26, bounds.Y + 20, bounds.Width - 52, 38), accent, 0.40f);

        for (var index = 0; index < bodyLines.Count; index++)
        {
            DrawDynamicOptionText(
                bodyLines[index],
                new Rectangle(bounds.X + 26, bounds.Y + 68 + index * bodyLineSpacing, bounds.Width - 52, 28),
                Color.White,
                0.38f);
        }
    }
}
