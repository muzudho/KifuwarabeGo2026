namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

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
        var boardSize = session.BoardSize;
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

        if (session.RenParseDisplayMode == RenParseDisplayMode.Graph)
        {
            DrawRenGraphStep1Overlay(session, start, cell);
        }
        else if (session.RenParseDisplayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            DrawRenGraphOverlay(session, start, cell, session.RenParseDisplayMode == RenParseDisplayMode.Eye);
        }
        else
        {
            DrawPlacedStones(session, start, cell);
            DrawRenParseOverlay(session, start, cell);
        }

        DrawSuperKoMarks(session, start, cell);
        DrawKoMark(session, start, cell);
        DrawHoverStone(session, mousePoint, cell);
        DrawBoardFrameHighlights(boardOuter);
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
    /// ［連］のグラフノード作成
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="applyEyeJudgement"></param>
    /// <returns></returns>
    private RenGraphNode[] CreateRenGraphNodes(GoRenParseResult renParse, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var sumX = new float[renParse.Count + 1];
        var sumY = new float[renParse.Count + 1];

        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                sumX[renNumber] += center.X;
                sumY[renNumber] += center.Y;
            }
        }

        var nodes = new RenGraphNode[renParse.Count + 1];
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            nodes[renNumber] = new RenGraphNode(
                renNumber,
                ren.Stone,
                new Vector2(sumX[renNumber] / ren.Points.Count, sumY[renNumber] / ren.Points.Count),
                !applyEyeJudgement || !ren.IsEye,
                applyEyeJudgement ? new List<int>(ren.EyeRenNumbers) : []);
        }

        return nodes;
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

    /// <summary>
    /// ［連パース・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenParseOverlay(GoAppSession session, Vector2 start, float cell)
    {
        if (session.RenParseDisplayMode != RenParseDisplayMode.Overlay)
        {
            return;
        }

        var renParse = session.ParseRens();
        DrawRenBoundaries(renParse, start, cell);
        DrawRenNumbers(renParse, start, cell);
    }

    /// <summary>
    /// ［連グラフ・ステップ１・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphStep1Overlay(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        DrawRenGraphCells(session, start, cell);
        DrawRenBoundaries(renParse, start, cell);
        DrawRenRepresentativeNumbers(renParse, start, cell);
    }

    /// <summary>
    /// ［連グラフ・オーバレイ］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="applyEyeJudgement"></param>

    private void DrawRenGraphOverlay(GoAppSession session, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var renParse = session.ParseRens();
        var nodes = CreateRenGraphNodes(renParse, start, cell, applyEyeJudgement);

        FillRect(BoardBounds, new Color(56, 145, 129));
        DrawRenGraphEdges(nodes, renParse.Edges, cell);
        DrawRenGraphNodes(nodes, cell);
    }

    /// <summary>
    /// ［連グラフ・セル］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphCells(GoAppSession session, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        for (var y = 0; y < session.BoardSize; y++)
        {
            for (var x = 0; x < session.BoardSize; x++)
            {
                var center = BoardPoint(start, cell, x, y);
                var rect = new Rectangle(
                    (int)MathF.Round(center.X - halfCell),
                    (int)MathF.Round(center.Y - halfCell),
                    (int)MathF.Ceiling(cell),
                    (int)MathF.Ceiling(cell));
                FillRect(rect, RenGraphCellColor(session.GetStone(x, y)));
            }
        }
    }

    /// <summary>
    /// ［連グラフ・エッジ］描画
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="edges"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphEdges(RenGraphNode[] nodes, IReadOnlyList<GoRenGraphEdge> edges, float cell)
    {
        var thickness = MathHelper.Clamp(cell * 0.08f, 4f, 8f);
        var color = new Color(70, 70, 220, 230);
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
            {
                continue;
            }

            DrawLine(nodes[edge.From].Center, nodes[edge.To].Center, thickness, color);
        }
    }

    /// <summary>
    /// ［連グラフ・ノード］描画
    /// </summary>
    /// <param name="nodes"></param>
    /// <param name="cell"></param>
    private void DrawRenGraphNodes(RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = MathHelper.Clamp(cell / 72f, 0.34f, 0.84f);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
            {
                continue;
            }

            DrawCircle(node.Center, radius, RenGraphNodeColor(node.Stone));
            DrawCenteredText(node.Number.ToString(), node.Center, new Color(0, 177, 238), scale);
            DrawRenGraphEyeMarkers(node, radius, scale);
        }
    }

    /// <summary>
    /// ［連グラフ・目マーカー］描画
    /// </summary>
    /// <param name="node"></param>
    /// <param name="radius"></param>
    /// <param name="scale"></param>
    private void DrawRenGraphEyeMarkers(RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0)
        {
            return;
        }

        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;

        for (var i = 0; i < node.EyeNumbers.Count; i++)
        {
            var markerBounds = new Rectangle(
                (int)MathF.Round(startX + (i * spacing) - (markerSize * 0.5f)),
                (int)MathF.Round(startY - (markerSize * 0.5f)),
                (int)MathF.Round(markerSize),
                (int)MathF.Round(markerSize));
            FillRect(markerBounds, new Color(255, 238, 0, 245));
            DrawRect(markerBounds, 2, new Color(255, 250, 220));
            DrawCenteredText(node.EyeNumbers[i].ToString(), new Vector2(markerBounds.Center.X, markerBounds.Center.Y), new Color(56, 94, 120), markerScale);
        }
    }

    /// <summary>
    /// ［連境界］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var size = renParse.Size;
        var halfCell = cell * 0.5f;
        var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f));
        var color = new Color(255, 238, 0, 238);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                var left = center.X - halfCell;
                var top = center.Y - halfCell;
                var right = center.X + halfCell;
                var bottom = center.Y + halfCell;

                if (x == 0 || renParse.GetRenNumber(x - 1, y) != renNumber)
                {
                    FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
                }

                if (y == 0 || renParse.GetRenNumber(x, y - 1) != renNumber)
                {
                    FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
                }

                if (x == size - 1)
                {
                    FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
                }

                if (y == size - 1)
                {
                    FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
                }
            }
        }
    }

    /// <summary>
    /// ［連番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var label = renParse.GetRenNumber(x, y).ToString();
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(label, center, new Color(0, 177, 238), scale);
            }
        }
    }

    /// <summary>
    /// ［連代表番号］描画
    /// </summary>
    /// <param name="renParse"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                if (drawn[renNumber])
                {
                    continue;
                }

                drawn[renNumber] = true;
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(renNumber.ToString(), center, new Color(0, 177, 238), scale);
            }
        }
    }

    /// <summary>
    /// ［連グラフ・ノード色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>
    private static Color RenGraphNodeColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };

    /// <summary>
    /// ［連グラフ・セル色］
    /// </summary>
    /// <param name="stone"></param>
    /// <returns></returns>
    private static Color RenGraphCellColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };

    /// <summary>
    /// ［連グラフノード］
    /// </summary>
    private sealed class RenGraphNode
    {
        public RenGraphNode(int number, GoStone stone, Vector2 center, bool isVisible, List<int> eyeNumbers)
        {
            Number = number;
            Stone = stone;
            Center = center;
            IsVisible = isVisible;
            EyeNumbers = eyeNumbers;
        }

        public int Number { get; }

        public GoStone Stone { get; }

        public Vector2 Center { get; }

        public bool IsVisible { get; set; }

        public List<int> EyeNumbers { get; }
    }
}

