namespace KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;
using System;

/// <summary>捕獲石数を皿、石、数値で表す部品です。</summary>
public sealed class AgehamaPlate
{
    public void Draw(Rectangle bounds, int agehama, bool capturedBlack, AgehamaPlateDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        draw.DrawCircleSurface(bounds, new Color(91, 55, 31));
        draw.DrawCircleSurface(new Rectangle(bounds.X + 5, bounds.Y + 5, bounds.Width - 10, bounds.Height - 11), new Color(145, 92, 48));
        draw.DrawStone(new Vector2(bounds.X + 30, bounds.Center.Y - 1), 12, capturedBlack);
        draw.DrawFittedText(agehama.ToString(), new Rectangle(bounds.X + 53, bounds.Y + 5, bounds.Width - 62, bounds.Height - 10), Color.White, 0.50f);
    }
}
public sealed record AgehamaPlateDrawingCallbacks(Action<Rectangle, Color> DrawCircleSurface,
    Action<Vector2, float, bool> DrawStone, Action<string, Rectangle, Color, float> DrawFittedText);
