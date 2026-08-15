namespace KifuwarabeGo2026.Gui.Presentation.BoardLens.GlassesSystem;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

public sealed class ChippedSingleEyeGlassSeedLens
{
    public static ChippedSingleEyeGlassSeedLens Default { get; } = new();

    private ChippedSingleEyeGlassSeedLens()
    {
    }

    /// <summary>
    /// CHIPPED SINGLE EYE GLASS SEED LENS を描画します。
    /// 3 種類の基本形を、回転・反転を含む 8 方向で照合します。
    /// </summary>
    public void Draw(BoardLensModel model, GoAppSession session, Vector2 start, float cell)
    {
        // エメラルド色
        // 盤の空点色と同系統で、白い瞳とも区別しやすい濃いオレンジ。
        var eyeOutline = new Color(180, 94, 0);

        var size = session.BoardSize;
        // 各段（数字が大きくなるほど下の段）
        for (var y = 0; y < size; y++)
        {
            // 各筋
            for (var x = 0; x < size; x++)
            {
                
                // 黒・白を別々に照合する。両方が該当する空点も表示対象にする。
                var isBlackEye = MatchesOrientationForStone(x, y, GoStone.Black);
                var isWhiteEye = MatchesOrientationForStone(x, y, GoStone.White);
                if (
                    session.GetDisplayStone(x, y) != GoStone.Empty ||   // すでに石が置かれている場所は描画しない。
                    !MatchesAnyOrientation(x, y)    // どの形にも一致しない場合は描画しない。
                    ) continue;

                var center = model.GetBoardPoint(start, cell, x, y);

                // エメラルドの枠と、手番の石色の内側を持つ正方形を描画する。
                // 眼の外周はエメラルドの楕円、瞳はエメラルドで縁取った手番色の丸で描く。
                var wireThickness = Math.Max(2, (int)MathF.Round(cell * .045f));
                model.DrawEllipseWire(center, cell * .58f, cell * .36f, eyeOutline, wireThickness, 0f);
                if (!isBlackEye || !isWhiteEye)
                {
                    // 黒眼・白眼は、エメラルドで縁取った瞳を持つ。
                    var pupilColor = isBlackEye ? model.GetRenGraphCellColor(GoStone.Black) : model.GetRenGraphCellColor(GoStone.White);
                    model.DrawCircle(center, cell * .19f, eyeOutline);
                    model.DrawCircle(center, cell * .14f, pupilColor);
                }
                // 両者の候補地は瞳を描かず、エメラルドの眼の外周だけで示す。
            }
        }

        // パターンマッチング
        // 既存の呼び出しでは、黒白いずれかの候補地かを確認する。
        bool MatchesAnyOrientation(int centerX, int centerY) =>
            MatchesOrientationForStone(centerX, centerY, GoStone.Black) ||
            MatchesOrientationForStone(centerX, centerY, GoStone.White);

        // 指定した色の石と空点だけを z として、8 方向の形を照合する。
        bool MatchesOrientationForStone(int centerX, int centerY, GoStone eyeStone)
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
                    (session.GetDisplayStone(px, py) == GoStone.Empty || session.GetDisplayStone(px, py) == eyeStone);
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
