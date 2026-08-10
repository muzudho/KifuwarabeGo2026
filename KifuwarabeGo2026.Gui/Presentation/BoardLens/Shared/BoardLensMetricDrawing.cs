namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    private void DrawRenMetricNumber(GoRen ren, int value, RenMetricUnit unit, Color valueColor, Vector2 start, float cell, Color? valueOutlineColor = null)
    {
        var representative = ren.Points[0];
        var center = BoardPoint(start, cell, representative.X, representative.Y);
        var valueScale = MathHelper.Clamp(cell / 68f, 0.34f, 0.80f);
        DrawRenNumber(ren.Number, center - new Vector2(0f, cell * 0.20f), RenNumberScale(cell));
        var valueText = value.ToString();
        if (valueText.Length > 2)
            valueScale *= MathF.Min(1f, _font.MeasureString("88").X * valueScale / Math.Max(1f, _font.MeasureString(valueText).X * valueScale));
        var valueCenter = center + new Vector2(0f, cell * 0.10f);
        if (valueOutlineColor is { } outlineColor) DrawCenteredOutlinedText(valueText, valueCenter, valueColor, outlineColor, valueScale);
        else DrawCenteredText(valueText, valueCenter, valueColor, valueScale);
        if (unit == RenMetricUnit.RenCount) DrawRenMetricUnit(center + new Vector2(0f, cell * 0.37f), unit, valueColor, cell, valueOutlineColor);
    }

    private static float RenNumberScale(float cell) => MathHelper.Clamp(cell / 120f, 0.18f, 0.46f);
    private void DrawRenNumber(int renNumber, Vector2 center, float scale) => DrawCenteredOutlinedText($"#{renNumber}", center, new Color(0, 177, 238), new Color(0, 92, 132, 245), scale);

    private void DrawCenteredOutlinedText(string text, Vector2 center, Color color, Color outlineColor, float scale)
    {
        var size = _font.MeasureString(text) * scale;
        var position = new Vector2(center.X - size.X / 2f, center.Y - size.Y / 2f);
        var outline = MathHelper.Clamp(scale * 7f, 1.5f, 3f);
        for (var i = 0; i < 16; i++)
        {
            var angle = MathHelper.TwoPi * i / 16;
            var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * outline;
            _spriteBatch.DrawString(_font, text, position + offset, outlineColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawRenMetricUnit(Vector2 center, RenMetricUnit unit, Color color, float cell, Color? outlineColor = null)
    {
        var radius = MathHelper.Clamp(cell * 0.075f, 3f, 6f);
        var thickness = Math.Max(2, (int)MathF.Round(radius * 0.42f));
        var backing = new Color(16, 26, 32, 220);
        if (unit == RenMetricUnit.PointCount)
        {
            DrawCircle(center, radius + thickness, outlineColor ?? color);
            DrawCircle(center, radius, outlineColor is null ? backing : color);
            if (outlineColor is not null) DrawCircle(center, Math.Max(1f, radius - thickness), backing);
            return;
        }
        var extent = (int)MathF.Round(radius + thickness);
        var bounds = new Rectangle((int)MathF.Round(center.X) - extent, (int)MathF.Round(center.Y) - extent, extent * 2, extent * 2);
        FillRect(bounds, backing);
        DrawRect(bounds, thickness, color);
    }

    private static GoStone OpponentOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;
    private enum RenMetricUnit { PointCount, RenCount }
    private static Color RenGraphNodeColor(GoStone stone) => stone switch { GoStone.Black => Color.Black, GoStone.White => new Color(248, 248, 244), _ => new Color(255, 197, 18) };
    private static Color RenGraphCellColor(GoStone stone) => stone switch { GoStone.Black => Color.Black, GoStone.White => new Color(248, 248, 244), _ => new Color(255, 197, 18) };
}
