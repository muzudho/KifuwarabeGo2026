namespace KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;
using System;

/// <summary>プレイヤー行のエンジンエラーログ導線を所有します。</summary>
public sealed class PlayerEngineErrorButton
{
    public Rectangle GetBounds(Rectangle playerBounds) => new(playerBounds.Right - 190, playerBounds.Y + 48, 172, 30);

    public void Draw(Rectangle playerBounds, Point? mousePoint, PlayerEngineErrorButtonDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var bounds = GetBounds(playerBounds);
        var hovered = mousePoint is { } point && bounds.Contains(point);
        draw.FillRectangle(bounds, hovered ? new Color(104, 34, 38, 220) : new Color(57, 29, 34, 210));
        draw.DrawRectangle(bounds, 1, new Color(255, 96, 96));
        draw.DrawFittedText("ERROR LOG", new Rectangle(bounds.X + 10, bounds.Y + 4, bounds.Width - 20, bounds.Height - 8), new Color(255, 126, 126), 0.34f);
    }
}

public sealed record PlayerEngineErrorButtonDrawingCallbacks(Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle, Action<string, Rectangle, Color, float> DrawFittedText);
