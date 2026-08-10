namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
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
        var whiteboard = session.CurrentMode.Kind == GoAppModeKind.VariationEditing;
        var surface = DrawBoardSurface(session.BoardSize, whiteboard);
        var start = surface.Start;
        var cell = surface.Cell;

        // ［連解析］描画
        DrawBoardRenAnalysis(
            session.RenParseDisplayMode,
            session.BoardSize,
            session.GetDisplayStone,
            session.ParseDisplayRens,
            () => DrawPlacedStones(session, start, cell),
            start,
            cell);
        if (session.RenParseDisplayMode == RenParseDisplayMode.Nobi)
            DrawNobiLens(session, start, cell);
        if (!session.IsRenParseDisplayEnabled)
            DrawLastMoveMarker(GetLocalDisplayLastMove(session), start, cell);

        if (!session.IsLocalReplayMode)
        {
            DrawSuperKoMarks(session, start, cell);
            DrawKoMark(session, start, cell);
            DrawHoverStone(session, mousePoint, cell);
        }
        DrawBoardFrameHighlights(surface.Outer);
    }

    /// <summary>
    /// 対局方式に依存しない碁盤面を描画します。
    /// </summary>
    private (Vector2 Start, float Cell, Rectangle Outer) DrawBoardSurface(int boardSize, bool whiteboard = false)
    {
        var boardOuter = new Rectangle(54, 50, 980, 980);

        FillRect(new Rectangle(boardOuter.X + 18, boardOuter.Y + 22, boardOuter.Width, boardOuter.Height), new Color(0, 0, 0, 125));
        FillRect(boardOuter, whiteboard ? new Color(105, 112, 114) : new Color(66, 42, 28));
        FillRect(
            new Rectangle(boardOuter.X + 8, boardOuter.Y + 8, boardOuter.Width - 16, boardOuter.Height - 16),
            whiteboard ? new Color(210, 214, 211) : new Color(180, 126, 62));
        FillRect(BoardBounds, whiteboard ? new Color(239, 241, 235) : new Color(221, 166, 82));

        for (var i = 0; i < 24; i++)
        {
            var x = BoardBounds.X + i * 38;
            DrawLine(
                new Vector2(x, BoardBounds.Y),
                new Vector2(x + 220, BoardBounds.Bottom),
                1,
                whiteboard ? new Color(165, 177, 173, 28) : new Color(246, 196, 113, 42));
        }

        var layout = GetBoardLayout(boardSize);
        var start = layout.Start;
        var cell = layout.Cell;
        var boardMargin = GetBoardMargin(boardSize);
        var end = new Vector2(BoardBounds.Right - boardMargin, BoardBounds.Bottom - boardMargin);

        for (var i = 0; i < boardSize; i++)
        {
            var p = start.X + cell * i;
            DrawLine(new Vector2(p, start.Y), new Vector2(p, end.Y), i == 0 || i == boardSize - 1 ? 4 : 2, whiteboard ? new Color(67, 78, 80) : new Color(42, 31, 24));
            p = start.Y + cell * i;
            DrawLine(new Vector2(start.X, p), new Vector2(end.X, p), i == 0 || i == boardSize - 1 ? 4 : 2, whiteboard ? new Color(67, 78, 80) : new Color(42, 31, 24));
        }

        foreach (var star in GetStarPoints(boardSize))
        {
            var center = BoardPoint(start, cell, star.X, star.Y);
            DrawCircle(center, Math.Max(5, cell * 0.1f), whiteboard ? new Color(67, 78, 80) : new Color(55, 38, 25));
        }

        DrawBoardCoordinates(boardSize, start, cell, boardOuter);

        return (start, cell, boardOuter);
    }

    /// <summary>
    /// 左下を A1 とし、I を飛ばした国際式の盤座標を下辺と左辺へ描画します。
    /// </summary>
    private void DrawBoardCoordinates(int boardSize, Vector2 start, float cell, Rectangle boardOuter)
    {
        var scale = boardSize >= 19 ? 0.34f : boardSize >= 13 ? 0.38f : 0.42f;
        var bottomY = boardOuter.Bottom - 40f;
        var leftX = boardOuter.X + 50f;

        for (var index = 0; index < boardSize; index++)
        {
            var column = GetBoardColumnLabel(index);
            var x = start.X + cell * index;
            DrawBoardCoordinateText(column, new Vector2(x, bottomY), scale, red: true);

            var row = (boardSize - index).ToString();
            var y = start.Y + cell * index;
            DrawBoardCoordinateText(row, new Vector2(leftX, y), scale, red: false);
        }
    }

    private void DrawBoardCoordinateText(string text, Vector2 center, float scale, bool red)
    {
        var size = _boardCoordinateFont.MeasureString(text) * scale;
        var position = center - size / 2f;
        var farShadow = Color.FromNonPremultiplied(0, 0, 0, 18);
        var nearShadow = Color.FromNonPremultiplied(0, 0, 0, 34);
        var innerEdge = red
            ? Color.FromNonPremultiplied(62, 33, 49, 42)
            : Color.FromNonPremultiplied(24, 65, 61, 42);
        var body = red
            ? Color.FromNonPremultiplied(112, 67, 91, 84)
            : Color.FromNonPremultiplied(62, 112, 105, 82);
        var highlight = red
            ? Color.FromNonPremultiplied(211, 151, 181, 34)
            : Color.FromNonPremultiplied(147, 201, 190, 32);

        _spriteBatch.DrawString(_boardCoordinateFont, text, position + new Vector2(5, 6), farShadow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_boardCoordinateFont, text, position + new Vector2(3, 4), nearShadow, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_boardCoordinateFont, text, position + new Vector2(1, 1), innerEdge, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_boardCoordinateFont, text, position, body, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_boardCoordinateFont, text, position - new Vector2(1, 1), highlight, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private static string GetBoardColumnLabel(int zeroBasedColumn)
    {
        const string columns = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
        return zeroBasedColumn >= 0 && zeroBasedColumn < columns.Length
            ? columns[zeroBasedColumn].ToString()
            : "?";
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
                var stone = session.GetDisplayStone(x, y);
                if (stone != GoStone.Empty)
                {
                    var center = BoardPoint(start, cell, x, y);
                    if (session.CurrentMode.Kind == GoAppModeKind.VariationEditing)
                        DrawWhiteboardStone(center, cell * 0.4f, stone == GoStone.Black);
                    else
                        DrawStone(center, cell * 0.44f, stone == GoStone.Black);
                }
            }
        }
    }

    private void DrawWhiteboardStone(Vector2 center, float radius, bool black)
    {
        // 扁平な碁石が盤へ落とす影。外側の柔らかい影と、石の直下の接地影を重ねる。
        // 黒石は光を吸うため濃く冷たい影、白石は反射光を含むため薄く暖かい影にする。
        var castShadowColor = black
            ? new Color(13, 20, 24, 105)
            : new Color(48, 54, 55, 120);
        var contactShadowColor = black
            ? new Color(5, 9, 12, 115)
            : new Color(30, 35, 36, 145);
        _spriteBatch.Draw(
            _softCircle,
            new Rectangle(
                (int)(center.X - radius + 9),
                (int)(center.Y - radius + 11),
                (int)(radius * 2),
                (int)(radius * 2)),
            castShadowColor);
        _spriteBatch.Draw(
            _softCircle,
            new Rectangle(
                (int)(center.X - radius * 0.72f + 5),
                (int)(center.Y - radius * 0.24f + 9),
                (int)(radius * 1.44f),
                (int)(radius * 0.48f)),
            contactShadowColor);

        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        _spriteBatch.Draw(black ? _stoneDark : _stoneLight, destination, Color.White);

        var outlineColor = black ? new Color(14, 20, 23) : new Color(73, 83, 84);
        var outlineRadius = radius * 0.96f;
        const int segments = 24;
        for (var index = 0; index < segments; index++)
        {
            var a = MathHelper.TwoPi * index / segments;
            var b = MathHelper.TwoPi * (index + 1) / segments;
            DrawLine(
                center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * outlineRadius,
                center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * outlineRadius,
                2,
                outlineColor);
        }
    }

    private static GoGameMove? GetLocalDisplayLastMove(GoAppSession session)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing)
            return session.ReviewCurrentMove;

        var moveIndex = session.LocalDisplayMoveIndex;
        return moveIndex > 0 && moveIndex <= session.CurrentGameRecord.Moves.Count
            ? session.CurrentGameRecord.Moves[moveIndex - 1]
            : null;
    }

    /// <summary>
    /// 現在表示中の局面における最終着手を、石の上の発光リングで示します。
    /// </summary>
    private void DrawLastMoveMarker(GoGameMove? move, Vector2 start, float cell)
    {
        if (move?.Point is not { } point)
            return;

        var center = BoardPoint(start, cell, point.X, point.Y);
        var radius = Math.Max(9f, cell * 0.19f);
        var shadowColor = new Color(8, 24, 30, 185);
        var accentColor = new Color(91, 218, 211, 245);
        const int segmentCount = 20;

        for (var index = 0; index < segmentCount; index++)
        {
            var startAngle = MathHelper.TwoPi * index / segmentCount;
            var endAngle = MathHelper.TwoPi * (index + 1) / segmentCount;
            var segmentStart = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * radius;
            var segmentEnd = center + new Vector2(MathF.Cos(endAngle), MathF.Sin(endAngle)) * radius;
            DrawLine(segmentStart, segmentEnd, Math.Max(5f, cell * 0.09f), shadowColor);
            DrawLine(segmentStart, segmentEnd, Math.Max(2f, cell * 0.045f), accentColor);
        }

        DrawCircle(center, Math.Max(3f, cell * 0.055f), shadowColor);
        DrawCircle(center, Math.Max(2f, cell * 0.032f), accentColor);
    }

    /// <summary>
    /// ［浮いている石］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="mousePoint"></param>
    /// <param name="cell"></param>
    private void DrawHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing ||
            (session.CurrentMode.Kind == GoAppModeKind.VariationEditing &&
             session.VariationEditingStone is not null))
        {
            DrawBoardEditingHoverStone(session, mousePoint, cell);
            return;
        }

        if (session.CurrentMode.Kind is not (GoAppModeKind.Playing or GoAppModeKind.VariationEditing) ||
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


    private static readonly Rectangle BoardBounds = new(88, 84, 912, 912);

    /// <summary>
    /// ［盤面のレイアウト］取得
    /// </summary>
    /// <param name="boardSize"></param>
    /// <returns></returns>

    private static (Vector2 Start, float Cell) GetBoardLayout(int boardSize)
    {
        var boardMargin = GetBoardMargin(boardSize);
        var playable = BoardBounds.Width - boardMargin * 2;
        var cell = playable / (boardSize - 1);
        var start = new Vector2(BoardBounds.X + boardMargin, BoardBounds.Y + boardMargin);
        return (start, cell);
    }

    private static float GetBoardMargin(int boardSize) => boardSize switch
    {
        <= 9 => 82f,
        <= 13 => 68f,
        _ => 50f,
    };

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
