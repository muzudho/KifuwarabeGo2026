namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Board;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.GlassesSystem;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.RenSystem;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens.Shared.RenBoundaries;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.BoardAndReview;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Common;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ［盤］描画処理
/// </summary>
public sealed class BoardRenderer : IDisposable
{
    private readonly BoardLensModel _boardLensModel;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;
    private readonly GoBoardPrimitiveRenderer _primitiveRenderer;
    private KfwStationeryDrawingTools _drawingContext = null!;

    public BoardRenderer(BoardLensModel boardLensModel, SpriteBatch spriteBatch, SpriteFont font,
        SpriteFont boardCoordinateFont, Texture2D softCircle, Texture2D stoneLight, Texture2D stoneDark)
    {
        _boardLensModel = boardLensModel;
        _spriteBatch = spriteBatch;
        _font = font;
        _stoneLight = stoneLight;
        _stoneDark = stoneDark;
        _primitiveRenderer = new GoBoardPrimitiveRenderer(font, boardCoordinateFont, softCircle, stoneLight, stoneDark);
    }

    public void Dispose()
    {
        _stoneLight.Dispose();
        _stoneDark.Dispose();
    }
    public static bool TryGetBoardIntersection(Point point, int boardSize, out Point intersection)
    {
        var geometry = CreateBoardGeometry(boardSize);
        if (!geometry.TryGetIntersection(new GoBoardScreenPoint(point.X, point.Y), out var boardPoint))
        {
            intersection = Point.Zero;
            return false;
        }

        intersection = new Point(boardPoint.X, boardPoint.Y);
        return true;
    }

    /// <summary>
    /// ［盤］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="mousePoint"></param>
    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        _drawingContext = drawingContext;
        var whiteboard = session.CurrentMode.Kind == GoAppModeKind.VariationEditing;
        var surface = DrawBoardSurface(drawingContext, session.BoardSize, whiteboard);
        var start = surface.Start;
        var cell = surface.Cell;
        var viewState = session.CreatePlayRoomViewState(displayedPosition: true);
        var geometry = CreateBoardGeometry(session.BoardSize);
        var presentation = GoBoardPresenter.Create(
            viewState,
            geometry,
            session.IsLocalReplayMode ? null : session.EnumerateSuperKoPoints());

        // ［連解析］描画
        DrawBoardRenAnalysis(
            session.RenParseDisplayMode,
            session.BoardSize,
            session.GetDisplayStone,
            session.ParseDisplayRens,
            () => DrawStones(presentation),
            start,
            cell);
        if (session.RenParseDisplayMode == RenParseDisplayMode.Nobi)
            DrawNobiLens(session, start, cell);

        if (session.IsGlassesBoardLens)
            ChippedSingleEyeGlassSeedLens.Default.Draw(_boardLensModel, session, start, cell);

        if (!session.IsRenParseDisplayEnabled)
            DrawLastMoveMarker(presentation.LastMoveMarker);

        if (!session.IsLocalReplayMode)
        {
            _primitiveRenderer.DrawSuperKoMarkers(_drawingContext, presentation.SuperKoMarkers);
            DrawKoMarker(presentation);
            DrawHoverStone(session, viewState, geometry, mousePoint, start, cell);
        }
        DrawBoardFrameHighlights(surface.Outer);
    }

    public void Draw(KfwStationeryDrawingTools drawingContext, GuiBoardView board, Point mousePoint, bool canAcceptInput)
    {
        _drawingContext = drawingContext;
        var surface = DrawBoardSurface(drawingContext, board.BoardSize);
        var viewState = GuiBoardViewAdapter.Create(board, GoPlayRoomActivity.Playing);
        var geometry = CreateBoardGeometry(board.BoardSize);
        var presentation = GoBoardPresenter.Create(viewState, geometry);
        DrawStones(presentation);
        DrawKoMarker(presentation);
        DrawLastMoveMarker(presentation.LastMoveMarker);
        if (canAcceptInput)
            DrawGameOasisHoverStone(viewState, geometry, mousePoint);
        DrawBoardFrameHighlights(surface.Outer);
    }

    /// <summary>
    /// 対局方式に依存しない碁盤面を描画します。
    /// </summary>
    public (Vector2 Start, float Cell, Rectangle Outer) DrawBoardSurface(
        KfwStationeryDrawingTools drawingContext,
        int boardSize,
        bool whiteboard = false)
    {
        _drawingContext = drawingContext;
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

        var geometry = CreateBoardGeometry(boardSize);
        var start = new Vector2(geometry.Start.X, geometry.Start.Y);
        var cell = geometry.Cell;
        var staticPresentation = GoBoardStaticPresenter.Create(
            geometry,
            new GoBoardViewport(boardOuter.X, boardOuter.Y, boardOuter.Width, boardOuter.Height));
        _primitiveRenderer.DrawStaticBoard(drawingContext, _spriteBatch, staticPresentation, whiteboard);

        return (start, cell, boardOuter);
    }

    /// <summary>
    /// 左下を A1 とし、I を飛ばした国際式の盤座標を下辺と左辺へ描画します。
    /// </summary>
    /// <summary>
    /// ［置いている石］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    public void DrawStones(GoBoardPresentation presentation)
        => _primitiveRenderer.DrawStones(_drawingContext, _spriteBatch, presentation);

    /// <summary>
    /// 現在表示中の局面における最終着手を、石の上の発光リングで示します。
    /// </summary>
    public void DrawLastMoveMarker(GoBoardMarkerVisual? marker)
        => _primitiveRenderer.DrawLastMoveMarker(_drawingContext, marker);

    /// <summary>
    /// ［浮いている石］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="mousePoint"></param>
    /// <param name="cell"></param>
    private void DrawHoverStone(
        GoAppSession session,
        GoPlayRoomViewState viewState,
        GoBoardGeometry geometry,
        Point mousePoint,
        Vector2 start,
        float cell)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing ||
            (session.CurrentMode.Kind == GoAppModeKind.VariationEditing &&
             session.VariationEditingStone is not null))
        {
            if (TryGetBoardIntersection(mousePoint, session.BoardSize, out var editingIntersection))
            {
                BoardAndReviewScreen.Default.DrawEditingHoverStone(
                    _boardLensModel,
                    session,
                    editingIntersection,
                    start,
                    cell);
            }
            return;
        }

        if (!GoBoardPresenter.TryCreateMoveHover(
                viewState,
                geometry,
                new GoBoardScreenPoint(mousePoint.X, mousePoint.Y),
                session.CanAcceptHumanMove,
                point => session.IsSuperKoPoint(point.X, point.Y),
                out var hover))
        {
            return;
        }

        GoBoardPrimitiveRenderer.DrawHoverStone(_drawingContext, hover);
    }

    /// <summary>
    /// ［スーパーコウ印］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <summary>
    /// ［コウ印］描画
    /// </summary>
    /// <param name="session"></param>
    /// <param name="start"></param>
    /// <param name="cell"></param>

    public void DrawKoMarker(GoBoardPresentation presentation)
        => _primitiveRenderer.DrawKoMarker(_drawingContext, presentation.KoMarker);

    private void DrawGameOasisHoverStone(
        GoPlayRoomViewState viewState,
        GoBoardGeometry geometry,
        Point mousePoint)
    {
        if (!GoBoardPresenter.TryCreateMoveHover(
                viewState,
                geometry,
                new GoBoardScreenPoint(mousePoint.X, mousePoint.Y),
                canAcceptHumanMove: true,
                isForbidden: null,
                out var hover))
            return;

        GoBoardPrimitiveRenderer.DrawHoverStone(_drawingContext, hover);
    }

    /// <summary>
    /// ［盤上の点］
    /// </summary>
    /// <param name="start"></param>
    /// <param name="cell"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>

    // 内部・画面描画座標は左上原点です。人向け／外部の囲碁座標は、入出力境界で左下原点へ変換します。
    public static Vector2 BoardPoint(Vector2 start, float cell, int x, int y) => new(start.X + cell * x, start.Y + cell * y);


    public static readonly Rectangle BoardBounds = new(88, 84, 912, 912);

    /// <summary>
    /// ［盤面のレイアウト］取得
    /// </summary>
    /// <param name="boardSize"></param>
    /// <returns></returns>

    public static GoBoardGeometry CreateBoardGeometry(int boardSize) =>
        GoBoardGeometry.Create(
            boardSize,
            new GoBoardViewport(BoardBounds.X, BoardBounds.Y, BoardBounds.Width, BoardBounds.Height));

    /// <summary>
    /// ［盤上の星］取得
    /// </summary>
    /// <param name="boardSize"></param>
    /// <returns></returns>
    /// <summary>
    /// ［盤の枠のハイライト］描画
    /// </summary>
    /// <param name="boardOuter"></param>

    public void DrawBoardFrameHighlights(Rectangle boardOuter)
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
    public void DrawStone(Vector2 center, float radius, bool black)
        => _primitiveRenderer.DrawStone(_spriteBatch, center, radius, black);

    public void DrawBoardRenAnalysis(RenParseDisplayMode displayMode, int boardSize,
        Func<int, int, GoStone> getStone, Func<GoRenParseResult> parseRens, Action drawPlacedStones,
        Vector2 start, float cell)
    {
        if (displayMode == RenParseDisplayMode.Off) { drawPlacedStones(); return; }
        var renParse = parseRens();
        if (displayMode == RenParseDisplayMode.Overlay)
        {
            drawPlacedStones(); DrawRenBoundaries(renParse, start, cell); DrawRenNumbers(renParse, start, cell); return;
        }
        if (displayMode == RenParseDisplayMode.Graph)
        {
            RenNetworkBasicLens.Default.DrawCells(_boardLensModel, boardSize, getStone, start, cell);
            DrawRenBoundaries(renParse, start, cell); DrawRenRepresentativeNumbers(renParse, start, cell); return;
        }
        if (displayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            RenNetworkBasicLens.Default.DrawOverlay(_boardLensModel, renParse, BoardBounds, start, cell,
                displayMode == RenParseDisplayMode.Eye);
            return;
        }
        RenNetworkBasicLens.Default.DrawCells(_boardLensModel, boardSize, getStone, start, cell);
        DrawRenBoundaries(renParse, start, cell);
        if (displayMode == RenParseDisplayMode.RenArea) { DrawRenAreaNumbers(renParse, start, cell); return; }
        RenBoundaryLens.DrawRenBoundaryLens(_boardLensModel, renParse, displayMode, start, cell);
    }

    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
            DrawRenNumber(renParse.GetRenNumber(x, y), BoardPoint(start, cell, x, y), scale);
    }

    private void DrawRenAreaNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            DrawRenMetricNumber(ren, ren.Points.Count, RenGraphCellColor(ren.Stone), start, cell,
                RenGraphCellColor(OpponentOf(ren.Stone)));
        }
    }

    public void DrawDeferredStrongMetrics(GoRenParseResult renParse,
        IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)> metrics, Vector2 start, float cell)
    {
        foreach (var metric in metrics)
            DrawRenMetricNumber(renParse.GetRen(metric.RenNumber), metric.Value, metric.Color, start, cell, metric.Outline);
    }

    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            if (drawn[number]) continue;
            drawn[number] = true;
            DrawRenNumber(number, BoardPoint(start, cell, x, y), scale);
        }
    }

    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f));
        var color = new Color(255, 238, 0, 238);
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            var center = BoardPoint(start, cell, x, y);
            var left = center.X - halfCell; var top = center.Y - halfCell;
            var right = center.X + halfCell; var bottom = center.Y + halfCell;
            if (x == 0 || renParse.GetRenNumber(x - 1, y) != number) FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
            if (y == 0 || renParse.GetRenNumber(x, y - 1) != number) FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
            if (x == renParse.Size - 1) FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
            if (y == renParse.Size - 1) FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
        }
    }

    private void DrawNobiLens(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        var legColor = RenGraphCellColor(session.CurrentTurn);
        var candidateColor = new Color(126, 255, 188);
        var legThickness = MathHelper.Clamp(cell * 0.045f, 2f, 5f);
        var markerRadius = MathHelper.Clamp(cell * 0.13f, 5f, 11f);
        for (var number = 1; number <= renParse.Count; number++)
        {
            var ren = renParse.GetRen(number);
            if (ren.Stone != session.CurrentTurn) continue;
            var contacts = new List<(GoPoint From, GoPoint To)>();
            foreach (var point in ren.Points)
            {
                AddCandidate(point, point.X - 1, point.Y); AddCandidate(point, point.X + 1, point.Y);
                AddCandidate(point, point.X, point.Y - 1); AddCandidate(point, point.X, point.Y + 1);
            }
            var markers = new HashSet<GoPoint>();
            foreach (var contact in contacts)
            {
                DrawLine(BoardPoint(start, cell, contact.From.X, contact.From.Y),
                    BoardPoint(start, cell, contact.To.X, contact.To.Y), legThickness, legColor);
                markers.Add(contact.To);
            }
            foreach (var marker in markers)
            {
                var center = BoardPoint(start, cell, marker.X, marker.Y);
                DrawCircle(center, markerRadius + 3f, legColor);
                DrawCircle(center, markerRadius, candidateColor);
            }
            void AddCandidate(GoPoint from, int x, int y)
            {
                if (x < 0 || x >= renParse.Size || y < 0 || y >= renParse.Size ||
                    renParse.GetRen(renParse.GetRenNumber(x, y)).Stone != GoStone.Empty || !session.IsNobiCandidate(x, y)) return;
                contacts.Add((from, new GoPoint(x, y)));
            }
        }
    }

    public void DrawRenMetricNumber(GoRen ren, int value, Color valueColor, Vector2 start, float cell,
        Color? valueOutlineColor = null)
    {
        var center = BoardPoint(start, cell, ren.Points[0].X, ren.Points[0].Y);
        var valueScale = MathHelper.Clamp(cell / 68f, 0.34f, 0.80f);
        DrawRenNumber(ren.Number, center - new Vector2(0f, cell * 0.20f), RenNumberScale(cell));
        var valueText = value.ToString();
        if (valueText.Length > 2)
            valueScale *= MathF.Min(1f, _font.MeasureString("88").X / Math.Max(1f, _font.MeasureString(valueText).X));
        var valueCenter = center + new Vector2(0f, cell * 0.10f);
        if (valueOutlineColor is { } outline) DrawCenteredOutlinedText(valueText, valueCenter, valueColor, outline, valueScale);
        else DrawCenteredText(valueText, valueCenter, valueColor, valueScale);
        DrawRenMetricUnit(center + new Vector2(0f, cell * 0.37f), valueColor, cell, valueOutlineColor);
    }

    public void DrawRenNumber(int number, Vector2 center, float scale) =>
        DrawCenteredOutlinedText($"#{number}", center, new Color(0, 177, 238), new Color(0, 92, 132, 245), scale);

    private void DrawCenteredOutlinedText(string text, Vector2 center, Color color, Color outlineColor, float scale)
    {
        var position = center - _font.MeasureString(text) * scale / 2f;
        var outline = MathHelper.Clamp(scale * 7f, 1.5f, 3f);
        for (var i = 0; i < 16; i++)
        {
            var angle = MathHelper.TwoPi * i / 16;
            _spriteBatch.DrawString(_font, text, position + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * outline,
                outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCenteredText(string text, Vector2 center, Color color, float scale) =>
        DrawText(text, center - _font.MeasureString(text) * scale / 2f, color, scale);

    private void DrawRenMetricUnit(Vector2 center, Color color, float cell, Color? outlineColor)
    {
        var radius = MathHelper.Clamp(cell * 0.075f, 3f, 6f);
        var thickness = Math.Max(2, (int)MathF.Round(radius * 0.42f));
        var backing = new Color(16, 26, 32, 220);
        DrawCircle(center, radius + thickness, outlineColor ?? color);
        DrawCircle(center, radius, outlineColor is null ? backing : color);
        if (outlineColor is not null) DrawCircle(center, Math.Max(1f, radius - thickness), backing);
    }

    private static float RenNumberScale(float cell) => MathHelper.Clamp(cell / 120f, 0.18f, 0.46f);
    private static GoStone OpponentOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
    public static Color RenGraphCellColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };
    private static Rectangle CreateVerticalLineRect(float x, float top, float bottom, int thickness) =>
        new((int)MathF.Round(x) - thickness / 2, (int)MathF.Round(top) - thickness / 2, thickness,
            (int)MathF.Round(bottom - top) + thickness);
    private static Rectangle CreateHorizontalLineRect(float left, float right, float y, int thickness) =>
        new((int)MathF.Round(left) - thickness / 2, (int)MathF.Round(y) - thickness / 2,
            (int)MathF.Round(right - left) + thickness, thickness);

    /// <summary>
    /// ［石テクスチャー］作成
    /// </summary>
    /// <param name="size"></param>
    /// <param name="lightStone"></param>
    /// <returns></returns>

    public static Texture2D CreateStoneTexture(GraphicsDevice graphicsDevice, int size, bool lightStone)
    {
        return CreateTexture(graphicsDevice, size, size, (x, y) =>
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

    private static Texture2D CreateTexture(GraphicsDevice graphicsDevice, int width, int height, Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var colors = new Color[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            colors[y * width + x] = colorFactory(x, y);
        texture.SetData(colors);
        return texture;
    }

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawingContext.DrawLine(start, end, thickness, color);
    private void DrawCircle(Vector2 center, float radius, Color color) => _drawingContext.DrawCircle(center, radius, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
}
