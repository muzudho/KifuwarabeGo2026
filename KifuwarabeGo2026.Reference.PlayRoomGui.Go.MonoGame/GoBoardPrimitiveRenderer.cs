namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>囲碁盤Presenterの描画要素をMonoGameの描画命令へ変換します。</summary>
public sealed class GoBoardPrimitiveRenderer : IDisposable
{
    public static readonly Rectangle BoardBounds = new(88, 84, 912, 912);

    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;

    public GoBoardPrimitiveRenderer(
        SpriteFont font,
        SpriteFont boardCoordinateFont,
        Texture2D softCircle,
        GraphicsDevice graphicsDevice)
    {
        _font = font;
        _boardCoordinateFont = boardCoordinateFont;
        _softCircle = softCircle;
        _stoneLight = CreateStoneTexture(graphicsDevice, 128, lightStone: true);
        _stoneDark = CreateStoneTexture(graphicsDevice, 128, lightStone: false);
    }

    public void Dispose()
    {
        _stoneLight.Dispose();
        _stoneDark.Dispose();
    }

    public (Vector2 Start, float Cell, Rectangle Outer) DrawBoardSurface(
        KfwStationeryDrawingTools drawingContext,
        SpriteBatch spriteBatch,
        int boardSize,
        bool whiteboard = false)
    {
        var boardOuter = new Rectangle(54, 50, 980, 980);
        drawingContext.FillRectangle(new Rectangle(boardOuter.X + 18, boardOuter.Y + 22, boardOuter.Width, boardOuter.Height), new Color(0, 0, 0, 125));
        drawingContext.FillRectangle(boardOuter, whiteboard ? new Color(105, 112, 114) : new Color(66, 42, 28));
        drawingContext.FillRectangle(
            new Rectangle(boardOuter.X + 8, boardOuter.Y + 8, boardOuter.Width - 16, boardOuter.Height - 16),
            whiteboard ? new Color(210, 214, 211) : new Color(180, 126, 62));
        drawingContext.FillRectangle(BoardBounds, whiteboard ? new Color(239, 241, 235) : new Color(221, 166, 82));

        for (var index = 0; index < 24; index++)
        {
            var x = BoardBounds.X + index * 38;
            drawingContext.DrawLine(
                new Vector2(x, BoardBounds.Y),
                new Vector2(x + 220, BoardBounds.Bottom),
                1,
                whiteboard ? new Color(165, 177, 173, 28) : new Color(246, 196, 113, 42));
        }

        var geometry = GoBoardGeometry.Create(
            boardSize,
            new GoBoardViewport(BoardBounds.X, BoardBounds.Y, BoardBounds.Width, BoardBounds.Height));
        var staticPresentation = GoBoardStaticPresenter.Create(
            geometry,
            new GoBoardViewport(boardOuter.X, boardOuter.Y, boardOuter.Width, boardOuter.Height));
        DrawStaticBoard(drawingContext, spriteBatch, staticPresentation, whiteboard);
        return (new Vector2(geometry.Start.X, geometry.Start.Y), geometry.Cell, boardOuter);
    }

    public static void DrawBoardFrameHighlights(KfwStationeryDrawingTools drawingContext, Rectangle boardOuter)
    {
        drawingContext.FillRectangle(new Rectangle(boardOuter.X, boardOuter.Y, boardOuter.Width, 5), new Color(255, 220, 128, 90));
        drawingContext.FillRectangle(new Rectangle(boardOuter.X, boardOuter.Y, 5, boardOuter.Height), new Color(255, 220, 128, 70));
        drawingContext.FillRectangle(new Rectangle(boardOuter.Right - 7, boardOuter.Y, 7, boardOuter.Height), new Color(31, 20, 15, 120));
        drawingContext.FillRectangle(new Rectangle(boardOuter.X, boardOuter.Bottom - 7, boardOuter.Width, 7), new Color(31, 20, 15, 120));
    }

    public void DrawStaticBoard(
        KfwStationeryDrawingTools drawingContext,
        SpriteBatch spriteBatch,
        GoBoardStaticPresentation presentation,
        bool whiteboard)
    {
        var lineColor = whiteboard ? new Color(67, 78, 80) : new Color(42, 31, 24);
        foreach (var line in presentation.Lines)
            drawingContext.DrawLine(ToVector2(line.Start), ToVector2(line.End), line.IsOuter ? 4 : 2, lineColor);

        var starColor = whiteboard ? new Color(67, 78, 80) : new Color(55, 38, 25);
        foreach (var star in presentation.Stars)
            drawingContext.DrawCircle(ToVector2(star.Center), star.Radius, starColor);

        foreach (var coordinate in presentation.Coordinates)
            DrawCoordinate(spriteBatch, coordinate);
    }

    public void DrawSuperKoMarkers(
        KfwStationeryDrawingTools drawingContext,
        IReadOnlyList<GoSuperKoMarkerVisual> markers)
    {
        foreach (var marker in markers)
        {
            var center = ToVector2(marker.Center);
            var bounds = new Rectangle(
                (int)(center.X - marker.Radius),
                (int)(center.Y - marker.Radius),
                (int)(marker.Radius * 2),
                (int)(marker.Radius * 2));
            drawingContext.FillRectangle(bounds, new Color(82, 39, 138, 198));
            drawingContext.DrawRectangle(bounds, 2, new Color(235, 206, 255));

            var size = _font.MeasureString(marker.Label) * marker.LabelScale;
            drawingContext.DrawText(
                marker.Label,
                new Vector2(center.X - size.X / 2, center.Y - size.Y / 2),
                Color.White,
                marker.LabelScale);
        }
    }

    public void DrawStones(
        KfwStationeryDrawingTools drawingContext,
        SpriteBatch spriteBatch,
        GoBoardPresentation presentation)
    {
        foreach (var visual in presentation.Stones)
        {
            var center = ToVector2(visual.Center);
            if (visual.UseWhiteboardStyle)
                DrawWhiteboardStone(drawingContext, spriteBatch, center, visual.Radius, visual.Stone == GoStone.Black);
            else
                DrawStone(spriteBatch, center, visual.Radius, visual.Stone == GoStone.Black);
        }
    }

    public void DrawStone(SpriteBatch spriteBatch, Vector2 center, float radius, bool black)
    {
        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        spriteBatch.Draw(_softCircle, new Rectangle(destination.X + 7, destination.Y + 10, destination.Width, destination.Height), new Color(0, 0, 0, 110));
        spriteBatch.Draw(black ? _stoneDark : _stoneLight, destination, Color.White);
    }

    public void DrawLastMoveMarker(KfwStationeryDrawingTools drawingContext, GoBoardMarkerVisual? marker)
    {
        if (marker is not { } value)
            return;

        var center = ToVector2(value.Center);
        var shadowColor = new Color(8, 24, 30, 185);
        var accentColor = new Color(91, 218, 211, 245);
        const int segmentCount = 20;
        for (var index = 0; index < segmentCount; index++)
        {
            var startAngle = MathHelper.TwoPi * index / segmentCount;
            var endAngle = MathHelper.TwoPi * (index + 1) / segmentCount;
            var segmentStart = center + new Vector2(MathF.Cos(startAngle), MathF.Sin(startAngle)) * value.Radius;
            var segmentEnd = center + new Vector2(MathF.Cos(endAngle), MathF.Sin(endAngle)) * value.Radius;
            drawingContext.DrawLine(segmentStart, segmentEnd, Math.Max(5f, value.Cell * 0.09f), shadowColor);
            drawingContext.DrawLine(segmentStart, segmentEnd, Math.Max(2f, value.Cell * 0.045f), accentColor);
        }

        drawingContext.DrawCircle(center, Math.Max(3f, value.Cell * 0.055f), shadowColor);
        drawingContext.DrawCircle(center, Math.Max(2f, value.Cell * 0.032f), accentColor);
    }

    public void DrawKoMarker(KfwStationeryDrawingTools drawingContext, GoBoardMarkerVisual? marker)
    {
        if (marker is not { } value)
            return;

        var center = ToVector2(value.Center);
        var bounds = new Rectangle(
            (int)(center.X - value.Radius),
            (int)(center.Y - value.Radius),
            (int)(value.Radius * 2),
            (int)(value.Radius * 2));
        drawingContext.FillRectangle(bounds, new Color(143, 38, 38, 210));
        drawingContext.DrawRectangle(bounds, 2, new Color(255, 230, 160));

        const string label = "KO";
        const float scale = 0.34f;
        var size = _font.MeasureString(label) * scale;
        drawingContext.DrawText(label, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), Color.White, scale);
    }

    public static void DrawHoverStone(KfwStationeryDrawingTools drawingContext, GoHoverStoneVisual hover)
    {
        var center = ToVector2(hover.Center);
        var black = hover.Stone == GoStone.Black;
        drawingContext.DrawCircle(center, hover.OuterRadius, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 110));
        drawingContext.DrawCircle(center, hover.InnerRadius, black ? new Color(8, 10, 14, 90) : new Color(255, 250, 232, 95));
    }

    private void DrawWhiteboardStone(
        KfwStationeryDrawingTools drawingContext,
        SpriteBatch spriteBatch,
        Vector2 center,
        float radius,
        bool black)
    {
        var castShadowColor = black ? new Color(13, 20, 24, 105) : new Color(48, 54, 55, 120);
        var contactShadowColor = black ? new Color(5, 9, 12, 115) : new Color(30, 35, 36, 145);
        spriteBatch.Draw(
            _softCircle,
            new Rectangle((int)(center.X - radius + 9), (int)(center.Y - radius + 11), (int)(radius * 2), (int)(radius * 2)),
            castShadowColor);
        spriteBatch.Draw(
            _softCircle,
            new Rectangle((int)(center.X - radius * 0.72f + 5), (int)(center.Y - radius * 0.24f + 9), (int)(radius * 1.44f), (int)(radius * 0.48f)),
            contactShadowColor);

        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        spriteBatch.Draw(black ? _stoneDark : _stoneLight, destination, Color.White);

        var outlineColor = black ? new Color(14, 20, 23) : new Color(73, 83, 84);
        var outlineRadius = radius * 0.96f;
        const int segments = 24;
        for (var index = 0; index < segments; index++)
        {
            var a = MathHelper.TwoPi * index / segments;
            var b = MathHelper.TwoPi * (index + 1) / segments;
            drawingContext.DrawLine(
                center + new Vector2(MathF.Cos(a), MathF.Sin(a)) * outlineRadius,
                center + new Vector2(MathF.Cos(b), MathF.Sin(b)) * outlineRadius,
                2,
                outlineColor);
        }
    }

    private void DrawCoordinate(SpriteBatch spriteBatch, GoBoardCoordinateVisual coordinate)
    {
        var center = ToVector2(coordinate.Center);
        var size = _boardCoordinateFont.MeasureString(coordinate.Text) * coordinate.Scale;
        var position = center - size / 2f;
        var farShadow = Color.FromNonPremultiplied(0, 0, 0, 18);
        var nearShadow = Color.FromNonPremultiplied(0, 0, 0, 34);
        var innerEdge = coordinate.IsColumn
            ? Color.FromNonPremultiplied(62, 33, 49, 42)
            : Color.FromNonPremultiplied(24, 65, 61, 42);
        var body = coordinate.IsColumn
            ? Color.FromNonPremultiplied(112, 67, 91, 84)
            : Color.FromNonPremultiplied(62, 112, 105, 82);
        var highlight = coordinate.IsColumn
            ? Color.FromNonPremultiplied(211, 151, 181, 34)
            : Color.FromNonPremultiplied(147, 201, 190, 32);

        spriteBatch.DrawString(_boardCoordinateFont, coordinate.Text, position + new Vector2(5, 6), farShadow, 0f, Vector2.Zero, coordinate.Scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_boardCoordinateFont, coordinate.Text, position + new Vector2(3, 4), nearShadow, 0f, Vector2.Zero, coordinate.Scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_boardCoordinateFont, coordinate.Text, position + new Vector2(1, 1), innerEdge, 0f, Vector2.Zero, coordinate.Scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_boardCoordinateFont, coordinate.Text, position, body, 0f, Vector2.Zero, coordinate.Scale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(_boardCoordinateFont, coordinate.Text, position - new Vector2(1, 1), highlight, 0f, Vector2.Zero, coordinate.Scale, SpriteEffects.None, 0f);
    }

    private static Vector2 ToVector2(GoBoardScreenPoint point) => new(point.X, point.Y);

    private static Texture2D CreateStoneTexture(GraphicsDevice graphicsDevice, int size, bool lightStone) =>
        CreateTexture(graphicsDevice, size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var nx = (x - center) / center;
            var ny = (y - center) / center;
            var distance = MathF.Sqrt(nx * nx + ny * ny);
            if (distance > 0.96f)
                return Color.Transparent;

            var highlight = MathF.Max(0f, 1f - MathF.Sqrt((nx + 0.32f) * (nx + 0.32f) + (ny + 0.38f) * (ny + 0.38f)) * 2.2f);
            if (lightStone)
            {
                var value = (byte)MathHelper.Clamp(232 + highlight * 22 - distance * 22, 205, 255);
                var blue = (byte)MathHelper.Clamp(value - 10, 195, 245);
                return new Color(value, value, blue, (byte)255);
            }

            var baseValue = 18 + highlight * 72 - distance * 12;
            return new Color(
                (byte)MathHelper.Clamp(baseValue, 8, 92),
                (byte)MathHelper.Clamp(baseValue + 2, 9, 96),
                (byte)MathHelper.Clamp(baseValue + 7, 14, 105));
        });

    private static Texture2D CreateTexture(
        GraphicsDevice graphicsDevice,
        int width,
        int height,
        Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(graphicsDevice, width, height);
        var colors = new Color[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            colors[y * width + x] = colorFactory(x, y);
        texture.SetData(colors);
        return texture;
    }
}
