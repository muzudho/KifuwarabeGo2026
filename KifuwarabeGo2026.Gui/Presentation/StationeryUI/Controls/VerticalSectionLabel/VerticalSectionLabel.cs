namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.VerticalSectionLabel;

using Microsoft.Xna.Framework;
using System;

/// <summary>縦書きの区画ラベルを描画する、Renderer 非依存のコンポーネントです。</summary>
public sealed class VerticalSectionLabel
{
    public void Draw(Rectangle sectionBounds, string title, Color accentColor, Color textColor,
        int labelWidth, int labelGap, VerticalSectionLabelDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        const float verticalScale = 0.38f;
        if (draw.MeasureText(title).X * verticalScale <= sectionBounds.Height - 12)
        {
            var labelBounds = new Rectangle(sectionBounds.X - labelWidth - labelGap, sectionBounds.Y, labelWidth, sectionBounds.Height);
            DrawSurface(labelBounds, accentColor, draw);
            var center = new Vector2(labelBounds.Center.X, labelBounds.Center.Y);
            draw.DrawRotatedCenteredText(title, center + new Vector2(2, 2), new Color(0, 0, 0, 125), verticalScale);
            draw.DrawRotatedCenteredText(title, center, textColor, verticalScale);
            return;
        }

        var fallbackWidth = Math.Max(88, labelWidth * 2);
        var fallbackBounds = new Rectangle(sectionBounds.X - fallbackWidth - labelGap, sectionBounds.Y, fallbackWidth, sectionBounds.Height);
        DrawSurface(fallbackBounds, accentColor, draw);
        var (firstLine, secondLine) = Split(title);
        var lineHeight = Math.Max(1, (fallbackBounds.Height - 12) / 2);
        draw.DrawFittedText(firstLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 5, fallbackBounds.Width - 12, lineHeight), textColor, 0.30f);
        draw.DrawFittedText(secondLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 7 + lineHeight, fallbackBounds.Width - 12, lineHeight), textColor, 0.30f);
    }

    private static void DrawSurface(Rectangle bounds, Color accentColor, VerticalSectionLabelDrawingCallbacks draw)
    {
        draw.FillRectangle(bounds, new Color(accentColor, 150));
        draw.DrawRectangle(bounds, 1, new Color(accentColor, 230));
    }

    private static (string FirstLine, string SecondLine) Split(string title)
    {
        var splitAt = title.LastIndexOf(' ');
        if (splitAt > 0 && splitAt < title.Length - 1) return (title[..splitAt], title[(splitAt + 1)..]);
        var middle = Math.Max(1, title.Length / 2);
        return (title[..middle], title[middle..]);
    }
}

public sealed record VerticalSectionLabelDrawingCallbacks(
    Func<string, Vector2> MeasureText,
    Action<string, Vector2, Color, float> DrawRotatedCenteredText,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Rectangle, Color, float> DrawFittedText);
