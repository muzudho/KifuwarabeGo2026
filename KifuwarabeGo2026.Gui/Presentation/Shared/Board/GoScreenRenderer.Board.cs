namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

/// <summary>
/// ［盤］描画処理
/// </summary>
public sealed partial class GoScreenRenderer
{
    public static bool TryGetBoardIntersection(Point point, int boardSize, out Point intersection)
    {
        var layout = GetBoardLayout(boardSize);
        var nearestX = (int)MathF.Round((point.X - layout.Start.X) / layout.Cell);
        var nearestY = (int)MathF.Round((point.Y - layout.Start.Y) / layout.Cell);
        if (nearestX < 0 || nearestX >= boardSize || nearestY < 0 || nearestY >= boardSize)
        {
            intersection = Point.Zero;
            return false;
        }

        var center = BoardPoint(layout.Start, layout.Cell, nearestX, nearestY);
        var closeEnough = Vector2.Distance(new Vector2(point.X, point.Y), center) <= Math.Max(16f, layout.Cell * 0.42f);
        intersection = new Point(nearestX, nearestY);
        return closeEnough;
    }

    /// <summary>
    /// ［盤］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="mousePoint"></param>
    private void DrawBoard(GoAppSession session, Point mousePoint)
    {
        var surface = DrawBoardSurface(session.BoardSize);
        var start = surface.Start;
        var cell = surface.Cell;

        // ［連解析］描画
        DrawBoardRenAnalysis(
            session.RenParseDisplayMode,
            session.BoardSize,
            session.GetStone,
            session.ParseRens,
            () => DrawPlacedStones(session, start, cell),
            start,
            cell);

        DrawSuperKoMarks(session, start, cell);
        DrawKoMark(session, start, cell);
        DrawHoverStone(session, mousePoint, cell);
        DrawBoardFrameHighlights(surface.Outer);
    }

    /// <summary>
    /// 対局方式に依存しない碁盤面を描画します。
    /// </summary>
    private (Vector2 Start, float Cell, Rectangle Outer) DrawBoardSurface(int boardSize)
    {
        var boardOuter = new Rectangle(54, 50, 980, 980);

        FillRect(new Rectangle(boardOuter.X + 18, boardOuter.Y + 22, boardOuter.Width, boardOuter.Height), new Color(0, 0, 0, 125));
        FillRect(boardOuter, new Color(66, 42, 28));
        FillRect(new Rectangle(boardOuter.X + 8, boardOuter.Y + 8, boardOuter.Width - 16, boardOuter.Height - 16), new Color(180, 126, 62));
        FillRect(BoardBounds, new Color(221, 166, 82));

        for (var i = 0; i < 24; i++)
        {
            var x = BoardBounds.X + i * 38;
            DrawLine(new Vector2(x, BoardBounds.Y), new Vector2(x + 220, BoardBounds.Bottom), 1, new Color(246, 196, 113, 42));
        }

        var layout = GetBoardLayout(boardSize);
        var start = layout.Start;
        var cell = layout.Cell;
        var end = new Vector2(BoardBounds.Right - BoardMargin, BoardBounds.Bottom - BoardMargin);

        for (var i = 0; i < boardSize; i++)
        {
            var p = start.X + cell * i;
            DrawLine(new Vector2(p, start.Y), new Vector2(p, end.Y), i == 0 || i == boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
            p = start.Y + cell * i;
            DrawLine(new Vector2(start.X, p), new Vector2(end.X, p), i == 0 || i == boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
        }

        foreach (var star in GetStarPoints(boardSize))
        {
            var center = BoardPoint(start, cell, star.X, star.Y);
            DrawCircle(center, Math.Max(5, cell * 0.1f), new Color(55, 38, 25));
        }

        return (start, cell, boardOuter);
    }

    /// <summary>
    /// ［置いている石］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawPlacedStones(GoAppSession session, Vector2 start, float cell)
    {
        for (var y = 0; y < session.BoardSize; y++)
        {
            for (var x = 0; x < session.BoardSize; x++)
            {
                var stone = session.GetStone(x, y);
                if (stone != GoStone.Empty)
                {
                    DrawStone(BoardPoint(start, cell, x, y), cell * 0.44f, stone == GoStone.Black);
                }
            }
        }
    }

    /// <summary>
    /// ［浮いている石］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="mousePoint"></param>
    /// <param name="cell"></param>
    private void DrawHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing)
        {
            DrawBoardEditingHoverStone(session, mousePoint, cell);
            return;
        }

        if (session.CurrentMode.Kind != GoAppModeKind.Playing ||
            !session.CanAcceptHumanMove ||
            !TryGetBoardIntersection(mousePoint, session.BoardSize, out var intersection) ||
            session.GetStone(intersection.X, intersection.Y) != GoStone.Empty ||
            (session.KoPoint is { } ko && ko.X == intersection.X && ko.Y == intersection.Y) ||
            session.IsSuperKoPoint(intersection.X, intersection.Y))
        {
            return;
        }

        var layout = GetBoardLayout(session.BoardSize);
        var center = BoardPoint(layout.Start, layout.Cell, intersection.X, intersection.Y);
        var black = session.CurrentTurn == GoStone.Black;
        DrawCircle(center, cell * 0.55f, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 110));
        DrawCircle(center, cell * 0.36f, black ? new Color(8, 10, 14, 90) : new Color(255, 250, 232, 95));
    }

    /// <summary>
    /// ［スーパーコウ印］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawSuperKoMarks(GoAppSession session, Vector2 start, float cell)
    {
        foreach (var point in session.EnumerateSuperKoPoints())
        {
            var center = BoardPoint(start, cell, point.X, point.Y);
            var radius = Math.Max(15f, cell * 0.32f);
            var bounds = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
            FillRect(bounds, new Color(82, 39, 138, 198));
            DrawRect(bounds, 2, new Color(235, 206, 255));

            const string label = "S-KO";
            var scale = cell < 55 ? 0.24f : 0.3f;
            var size = _font.MeasureString(label) * scale;
            DrawText(label, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), Color.White, scale);
        }
    }

    /// <summary>
    /// ［コウ印］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>

    private void DrawKoMark(GoAppSession session, Vector2 start, float cell)
    {
        if (session.KoPoint is not { } ko)
        {
            return;
        }

        var center = BoardPoint(start, cell, ko.X, ko.Y);
        var radius = Math.Max(12f, cell * 0.26f);
        var bounds = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        FillRect(bounds, new Color(143, 38, 38, 210));
        DrawRect(bounds, 2, new Color(255, 230, 160));

        const string label = "KO";
        var size = _font.MeasureString(label) * 0.34f;
        DrawText(label, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), Color.White, 0.34f);
    }

    /// <summary>
    /// ［盤上の点］
    /// </summary>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>

    private static Vector2 BoardPoint(Vector2 start, float cell, int x, int y) => new(start.X + cell * x, start.Y + cell * y);


    private const float BoardMargin = 38f;


    private static readonly Rectangle BoardBounds = new(88, 84, 912, 912);

    /// <summary>
    /// ［盤面のレイアウト］取得
    /// </summary>
    /// <param name="boardSize"></param>
    /// <returns></returns>

    private static (Vector2 Start, float Cell) GetBoardLayout(int boardSize)
    {
        var playable = BoardBounds.Width - BoardMargin * 2;
        var cell = playable / (boardSize - 1);
        var start = new Vector2(BoardBounds.X + BoardMargin, BoardBounds.Y + BoardMargin);
        return (start, cell);
    }

    /// <summary>
    /// ［盤上の星］取得
    /// </summary>
    /// <param name="boardSize"></param>
    /// <returns></returns>
    private static Point[] GetStarPoints(int boardSize)
    {
        return boardSize switch
        {
            9 => new[] { new Point(2, 2), new Point(6, 2), new Point(4, 4), new Point(2, 6), new Point(6, 6) },
            13 => new[] { new Point(3, 3), new Point(9, 3), new Point(6, 6), new Point(3, 9), new Point(9, 9) },
            _ => new[] { new Point(3, 3), new Point(9, 3), new Point(15, 3), new Point(3, 9), new Point(9, 9), new Point(15, 9), new Point(3, 15), new Point(9, 15), new Point(15, 15) },
        };
    }

    /// <summary>
    /// ［盤の枠のハイライト］描画
    /// </summary>
    /// <param name="boardOuter"></param>

    private void DrawBoardFrameHighlights(Rectangle boardOuter)
    {
        FillRect(new Rectangle(boardOuter.X, boardOuter.Y, boardOuter.Width, 5), new Color(255, 220, 128, 90));
        FillRect(new Rectangle(boardOuter.X, boardOuter.Y, 5, boardOuter.Height), new Color(255, 220, 128, 70));
        FillRect(new Rectangle(boardOuter.Right - 7, boardOuter.Y, 7, boardOuter.Height), new Color(31, 20, 15, 120));
        FillRect(new Rectangle(boardOuter.X, boardOuter.Bottom - 7, boardOuter.Width, 7), new Color(31, 20, 15, 120));
    }

    /// <summary>
    /// ［石］描画
    /// </summary>
    /// <param name="center"></param>
    /// <param name="radius"></param>
    /// <param name="black"></param>
    private void DrawStone(Vector2 center, float radius, bool black)
    {
        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        _spriteBatch.Draw(_softCircle, new Rectangle(destination.X + 7, destination.Y + 10, destination.Width, destination.Height), new Color(0, 0, 0, 110));
        _spriteBatch.Draw(black ? _stoneDark : _stoneLight, destination, Color.White);
    }

    /// <summary>
    /// ［石テクスチャー］作成
    /// </summary>
    /// <param name="size"></param>
    /// <param name="lightStone"></param>
    /// <returns></returns>

    private Texture2D CreateStoneTexture(int size, bool lightStone)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var nx = (x - center) / center;
            var ny = (y - center) / center;
            var distance = MathF.Sqrt(nx * nx + ny * ny);
            if (distance > 0.96f)
            {
                return Color.Transparent;
            }

            var highlight = MathF.Max(0f, 1f - MathF.Sqrt((nx + 0.32f) * (nx + 0.32f) + (ny + 0.38f) * (ny + 0.38f)) * 2.2f);
            var shade = 1f - MathHelper.Clamp(distance * 0.55f, 0f, 0.55f);
            if (lightStone)
            {
                var value = (byte)MathHelper.Clamp(232 + highlight * 22 - distance * 22, 205, 255);
                var blue = (byte)MathHelper.Clamp(value - 10, 195, 245);
                return new Color(value, value, blue, (byte)255);
            }

            var baseValue = 18 + highlight * 72 - distance * 12;
            return new Color((byte)MathHelper.Clamp(baseValue, 8, 92), (byte)MathHelper.Clamp(baseValue + 2, 9, 96), (byte)MathHelper.Clamp(baseValue + 7, 14, 105));
        });
    }
}
