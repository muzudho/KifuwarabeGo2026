namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    /// <summary>CHIPPED SINGLE EYE GLASS SEED LENS の候補点を描画します。</summary>
    private void DrawGlassesLens(GoAppSession session, Vector2 start, float cell)
    {
        var emerald = new Color(126, 255, 188, 180);
        var size = session.BoardSize;
        // 各段
        for (var y = 0; y < size; y++)
        // 各筋
        for (var x = 0; x < size; x++)
        {
            // すでに石が置かれている場所は描画しない。
            if (session.GetDisplayStone(x, y) != GoStone.Empty) continue;

            bool Z(int px, int py) => px >= 0 && px < size && py >= 0 && py < size &&
                (session.GetDisplayStone(px, py) == GoStone.Empty || session.GetDisplayStone(px, py) == session.CurrentTurn);
            var pattern1 = Z(x - 1, y - 1) && Z(x, y - 1) && Z(x + 1, y - 1) && Z(x - 1, y) && Z(x, y + 1) && Z(x + 1, y + 1);
            var pattern2 = y == size - 1 && Z(x - 1, y - 1) && Z(x, y - 1) && Z(x + 1, y - 1) && Z(x - 1, y) && Z(x + 1, y);
            var pattern3 = x == 0 && y == size - 2 && Z(x, y - 1) && Z(x + 1, y - 1) && Z(x + 1, y);
            if (pattern1 || pattern2 || pattern3)
            {
                var center = BoardPoint(start, cell, x, y);
                FillRect(new Rectangle((int)(center.X - cell * .28f), (int)(center.Y - cell * .28f), (int)(cell * .56f), (int)(cell * .56f)), emerald);
            }
        }
    }
}
