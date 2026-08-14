namespace KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;

using Microsoft.Xna.Framework;
using System;

/// <summary>USED、NOW、LIMITを比率バーと固定3列の時刻で表示します。</summary>
public sealed class PlayerTimeUsageBar
{
    public void Draw(
        Rectangle bounds,
        TimeSpan? used,
        TimeSpan? now,
        TimeSpan? limit,
        PlayerRowDrawingCallbacks draw)
    {
        var iconBounds = new Rectangle(bounds.X, bounds.Y + 2, 30, 30);
        DrawClockIcon(iconBounds, draw);

        var content = new Rectangle(iconBounds.Right + 8, bounds.Y, Math.Max(1, bounds.Right - iconBounds.Right - 8), bounds.Height);
        var bar = new Rectangle(content.X, content.Y, content.Width, 10);
        var usedRatio = Ratio(used, limit);
        var nowRatio = Math.Max(usedRatio, Ratio(now ?? used, limit));
        var usedRight = bar.X + (int)Math.Round(bar.Width * usedRatio);
        var nowRight = bar.X + (int)Math.Round(bar.Width * nowRatio);
        draw.FillRectangle(bar, new Color(135, 224, 238));
        if (nowRight > usedRight)
            draw.FillRectangle(new Rectangle(usedRight, bar.Y, nowRight - usedRight, bar.Height), new Color(52, 137, 221));
        if (usedRight > bar.X)
            draw.FillRectangle(new Rectangle(bar.X, bar.Y, usedRight - bar.X, bar.Height), new Color(8, 13, 18));
        draw.DrawRectangle(bar, 1, new Color(205, 231, 235));

        var columnWidth = content.Width / 3;
        DrawColumn(0, "USED", used, new Color(22, 28, 34));
        DrawColumn(1, "NOW", now ?? used, new Color(78, 163, 238));
        DrawColumn(2, "LIMIT", limit, new Color(135, 224, 238));

        void DrawColumn(int index, string label, TimeSpan? value, Color color)
        {
            var column = new Rectangle(content.X + columnWidth * index, content.Y + 13,
                index == 2 ? content.Right - (content.X + columnWidth * index) : columnWidth, Math.Max(1, content.Height - 13));
            draw.DrawFittedText(label, new Rectangle(column.X, column.Y, column.Width, 10), color, 0.17f);
            var text = value is { } time
                ? FormatClockTime(time)
                : "--:--:--";
            draw.DrawFittedText(text, new Rectangle(column.X, column.Y + 10, column.Width, Math.Max(1, column.Height - 10)), Color.White, 0.25f);
        }
    }

    private static string FormatClockTime(TimeSpan value)
    {
        var totalHours = Math.Max(0, (int)value.TotalHours);
        return $"{totalHours:00}:{value.Minutes:00}:{value.Seconds:00}";
    }

    private static double Ratio(TimeSpan? value, TimeSpan? limit)
    {
        if (value is null || limit is null || limit.Value <= TimeSpan.Zero) return 0;
        return Math.Clamp(value.Value.TotalSeconds / limit.Value.TotalSeconds, 0, 1);
    }

    private static void DrawClockIcon(Rectangle bounds, PlayerRowDrawingCallbacks draw)
    {
        var color = new Color(151, 205, 216);
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);
        var points = new[]
        {
            center + new Vector2(0, -13), center + new Vector2(9, -9),
            center + new Vector2(13, 0), center + new Vector2(9, 9),
            center + new Vector2(0, 13), center + new Vector2(-9, 9),
            center + new Vector2(-13, 0), center + new Vector2(-9, -9),
        };
        for (var index = 0; index < points.Length; index++)
            draw.DrawLine(points[index], points[(index + 1) % points.Length], 2, color);
        draw.DrawLine(center, center + new Vector2(0, -8), 2, color);
        draw.DrawLine(center, center + new Vector2(7, 4), 2, color);
    }
}
