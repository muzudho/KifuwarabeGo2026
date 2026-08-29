namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>囲碁盤Presenterの描画要素をMonoGameの描画命令へ変換します。</summary>
public sealed class GoBoardPrimitiveRenderer
{
    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;

    public GoBoardPrimitiveRenderer(
        SpriteFont font,
        SpriteFont boardCoordinateFont,
        Texture2D softCircle,
        Texture2D stoneLight,
        Texture2D stoneDark)
    {
        _font = font;
        _boardCoordinateFont = boardCoordinateFont;
        _softCircle = softCircle;
        _stoneLight = stoneLight;
        _stoneDark = stoneDark;
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
}
