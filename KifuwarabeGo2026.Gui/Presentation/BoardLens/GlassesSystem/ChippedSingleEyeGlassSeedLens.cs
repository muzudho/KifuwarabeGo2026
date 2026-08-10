namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// CHIPPED SINGLE EYE GLASS SEED LENS を描画します。
    /// 3 種類の基本形を、回転・反転を含む 8 方向で照合します。
    /// </summary>
    private void DrawGlassesLens(GoAppSession session, Vector2 start, float cell)
    {
        // エメラルド色
        var emerald = new Color(126, 255, 188, 180);

        var size = session.BoardSize;
        // 各段（数字が大きくなるほど下の段）
        for (var y = 0; y < size; y++)
        {
            // 各筋
            for (var x = 0; x < size; x++)
            {
                
                if (
                    session.GetDisplayStone(x, y) != GoStone.Empty ||   // すでに石が置かれている場所は描画しない。
                    !MatchesAnyOrientation(x, y)    // どの形にも一致しない場合は描画しない。
                    ) continue;

                var center = BoardPoint(start, cell, x, y);

                // エメラルドの枠と、手番の石色の内側を持つ正方形を描画する。
                var bounds = new Rectangle(
                    (int)(center.X - cell * .28f),
                    (int)(center.Y - cell * .28f),
                    (int)(cell * .56f),
                    (int)(cell * .56f));
                var border = Math.Max(2, (int)MathF.Round(cell * .07f));
                var innerBounds = new Rectangle(
                    bounds.X + border,
                    bounds.Y + border,
                    Math.Max(1, bounds.Width - (border * 2)),
                    Math.Max(1, bounds.Height - (border * 2)));
                FillRect(bounds, emerald);
                FillRect(innerBounds, RenGraphCellColor(session.CurrentTurn));
            }
        }

        // パターンマッチング
        bool MatchesAnyOrientation(int centerX, int centerY)
        {
            for (var orientation = 0; orientation < 8; orientation++)
            {
                // いずれかのパターンにマッチした。
                if (MatchesPattern1(orientation) || MatchesPattern2(orientation) || MatchesPattern3(orientation)) return true;
            }
            // どのパターンにもマッチしなかった。
            return false;

            // (1)
            // zzz
            // z.z
            //  zz
            bool MatchesPattern1(int orientation) =>
                IsZ(-1, -1, orientation) && IsZ(0, -1, orientation) && IsZ(1, -1, orientation) &&
                IsZ(-1, 0, orientation) && IsZ(1, 0, orientation) &&
                IsZ(0, 1, orientation) && IsZ(1, 1, orientation);

            // (2)
            // zzz
            // z.z
            // ---
            bool MatchesPattern2(int orientation) =>
                IsZ(-1, -1, orientation) && IsZ(0, -1, orientation) && IsZ(1, -1, orientation) &&
                IsZ(-1, 0, orientation) && IsZ(1, 0, orientation) &&
                IsOutside(0, 1, orientation);

            // (3)
            // | zz
            // |.z
            // + --
            bool MatchesPattern3(int orientation) =>
                IsZ(0, -1, orientation) && IsZ(1, -1, orientation) && IsZ(1, 0, orientation) &&
                IsOutside(-1, 0, orientation) && IsOutside(0, 1, orientation) && IsOutside(-1, 1, orientation);

            // 手番の石、または空点であることを確認する。
            bool IsZ(int dx, int dy, int orientation)
            {
                var (tx, ty) = Transform(dx, dy, orientation);
                var px = centerX + tx;
                var py = centerY + ty;
                return px >= 0 && px < size && py >= 0 && py < size &&
                    (session.GetDisplayStone(px, py) == GoStone.Empty || session.GetDisplayStone(px, py) == session.CurrentTurn);
            }

            bool IsOutside(int dx, int dy, int orientation)
            {
                var (tx, ty) = Transform(dx, dy, orientation);
                var px = centerX + tx;
                var py = centerY + ty;
                return px < 0 || px >= size || py < 0 || py >= size;
            }
        }
    }

    /// <summary>基本形、回転、反転を含む二面体群 D4 の 8 変換です。</summary>
    private static (int X, int Y) Transform(int x, int y, int orientation) => orientation switch
    {
        0 => (x, y),
        1 => (-y, x),
        2 => (-x, y),
        3 => (-y, -x),
        4 => (y, -x),
        5 => (x, -y),
        6 => (y, x),
        7 => (-x, -y),
        _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
    };
}
