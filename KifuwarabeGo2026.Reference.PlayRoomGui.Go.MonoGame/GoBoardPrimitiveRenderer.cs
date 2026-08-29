namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>囲碁盤Presenterの描画要素をMonoGameの描画命令へ変換します。</summary>
public sealed class GoBoardPrimitiveRenderer
{
    private readonly SpriteFont _font;
    private readonly SpriteFont _boardCoordinateFont;

    public GoBoardPrimitiveRenderer(SpriteFont font, SpriteFont boardCoordinateFont)
    {
        _font = font;
        _boardCoordinateFont = boardCoordinateFont;
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
