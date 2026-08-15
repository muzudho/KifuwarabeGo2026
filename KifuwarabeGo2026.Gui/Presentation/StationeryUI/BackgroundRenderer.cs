namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using Microsoft.Xna.Framework;
using System;

/// <summary>アプリ共通の背景テーマを描画します。</summary>
internal static class BackgroundRenderer
{
    public static void Draw(KfwScreenCanvas canvas)
    {
        var topLeft = canvas.ToVirtualPoint(Point.Zero);
        var bottomRight = canvas.ToVirtualPoint(new Point(canvas.GraphicsDevice.Viewport.Width, canvas.GraphicsDevice.Viewport.Height));
        var left = Math.Min(topLeft.X, bottomRight.X) - 2;
        var right = Math.Max(topLeft.X, bottomRight.X) + 2;
        var top = Math.Min(topLeft.Y, bottomRight.Y) - 2;
        var bottom = Math.Max(topLeft.Y, bottomRight.Y) + 2;
        var width = right - left;
        canvas.FillRectangle(new Rectangle(left, top, width, bottom - top), new Color(11, 13, 18));
        canvas.FillRectangle(new Rectangle(left, 0, width, 150), new Color(24, 30, 40));
        canvas.FillRectangle(new Rectangle(left, 930, width, 150), new Color(9, 28, 31));
        for (var index = 0; index < 18; index++)
        {
            var alpha = (byte)(50 - index * 2);
            var start = new Vector2(-120, 180 + index * 42);
            var end = new Vector2(2050, -40 + index * 64);
            var slope = (end.Y - start.Y) / (end.X - start.X);
            canvas.DrawLine(new Vector2(left, start.Y + (left - start.X) * slope),
                new Vector2(right, start.Y + (right - start.X) * slope), 2,
                new Color((byte)56, (byte)86, (byte)96, alpha));
        }
        canvas.DrawGlow(new Vector2(1030, 90), 520, new Color(39, 122, 104, 80));
        canvas.DrawGlow(new Vector2(1700, 850), 360, new Color(144, 59, 48, 72));
    }
}
