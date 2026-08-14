namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SectionLabel;

using Microsoft.Xna.Framework;
using System;

/// <summary>区画の見出し位置、サイズ、表示方向を所有する文房具UIです。</summary>
public sealed class SectionLabelComponent
{
    public Rectangle SectionBounds { get; set; }
    public string Text { get; set; } = string.Empty;
    public Color AccentColor { get; set; }
    public Color TextColor { get; set; } = new(205, 218, 218);
    public int LabelWidth { get; set; } = 38;
    public int LabelGap { get; set; } = 8;
    public SectionLabelDirection Direction { get; set; } = SectionLabelDirection.Vertical;

    public void Draw(SectionLabelDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        if (Direction == SectionLabelDirection.Horizontal)
        {
            DrawHorizontal(draw);
            return;
        }

        DrawVertical(draw);
    }

    private void DrawVertical(SectionLabelDrawingCallbacks draw)
    {
        const float verticalScale = 0.38f;
        if (draw.MeasureText(Text).X * verticalScale <= SectionBounds.Height - 12)
        {
            var labelBounds = new Rectangle(SectionBounds.X - LabelWidth - LabelGap, SectionBounds.Y, LabelWidth, SectionBounds.Height);
            DrawSurface(labelBounds, draw);
            var center = new Vector2(labelBounds.Center.X, labelBounds.Center.Y);
            draw.DrawRotatedCenteredText(Text, center + new Vector2(2, 2), new Color(0, 0, 0, 125), verticalScale);
            draw.DrawRotatedCenteredText(Text, center, TextColor, verticalScale);
            return;
        }

        var fallbackWidth = Math.Max(88, LabelWidth * 2);
        var fallbackBounds = new Rectangle(SectionBounds.X - fallbackWidth - LabelGap, SectionBounds.Y, fallbackWidth, SectionBounds.Height);
        DrawSurface(fallbackBounds, draw);
        var (firstLine, secondLine) = Split(Text);
        var lineHeight = Math.Max(1, (fallbackBounds.Height - 12) / 2);
        draw.DrawFittedText(firstLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 5, fallbackBounds.Width - 12, lineHeight), TextColor, 0.30f);
        draw.DrawFittedText(secondLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 7 + lineHeight, fallbackBounds.Width - 12, lineHeight), TextColor, 0.30f);
    }

    private void DrawHorizontal(SectionLabelDrawingCallbacks draw)
    {
        var labelBounds = new Rectangle(SectionBounds.X, SectionBounds.Y - LabelWidth - LabelGap, SectionBounds.Width, LabelWidth);
        DrawSurface(labelBounds, draw);
        draw.DrawFittedText(Text, new Rectangle(labelBounds.X + 8, labelBounds.Y + 4, labelBounds.Width - 16, labelBounds.Height - 8), TextColor, 0.36f);
    }

    private void DrawSurface(Rectangle bounds, SectionLabelDrawingCallbacks draw)
    {
        draw.FillRectangle(bounds, new Color(AccentColor, 150));
        draw.DrawRectangle(bounds, 1, new Color(AccentColor, 230));
    }

    private static (string FirstLine, string SecondLine) Split(string title)
    {
        var splitAt = title.LastIndexOf(' ');
        if (splitAt > 0 && splitAt < title.Length - 1) return (title[..splitAt], title[(splitAt + 1)..]);
        var middle = Math.Max(1, title.Length / 2);
        return (title[..middle], title[middle..]);
    }
}

public enum SectionLabelDirection
{
    Horizontal,
    Vertical,
}

public sealed record SectionLabelDrawingCallbacks(
    Func<string, Vector2> MeasureText,
    Action<string, Vector2, Color, float> DrawRotatedCenteredText,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Rectangle, Color, float> DrawFittedText);
