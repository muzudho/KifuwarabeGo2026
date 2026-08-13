namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

public sealed partial class GoScreenRenderer
{
    private void DrawVerticalResultSection(
        Rectangle bounds,
        string title,
        Color accentColor,
        Color? textColor = null,
        int labelWidth = 38,
        int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));
        DrawVerticalSectionLabel(bounds, title, accentColor, textColor ?? new Color(205, 218, 218), labelWidth, labelGap);
    }

    /// <summary>区画左端のラベルを縦書きで描画し、狭い区画では左寄せの二段横書きへ切り替えます。</summary>
    private void DrawVerticalSectionLabel(
        Rectangle sectionBounds,
        string title,
        Color accentColor,
        Color textColor,
        int labelWidth,
        int labelGap)
    {
        const float verticalScale = 0.38f;
        var titleSize = _font.MeasureString(title) * verticalScale;
        var canDrawVertically = titleSize.X <= sectionBounds.Height - 12;
        if (canDrawVertically)
        {
            var labelBounds = new Rectangle(
                sectionBounds.X - labelWidth - labelGap,
                sectionBounds.Y,
                labelWidth,
                sectionBounds.Height);
            DrawVerticalSectionLabelSurface(labelBounds, accentColor);
            var center = new Vector2(labelBounds.Center.X, labelBounds.Center.Y);
            var origin = _font.MeasureString(title) / 2f;
            _spriteBatch.DrawString(_font, title, center + new Vector2(2, 2), new Color(0, 0, 0, 125), -MathHelper.PiOver2, origin, verticalScale, SpriteEffects.None, 0f);
            _spriteBatch.DrawString(_font, title, center, textColor, -MathHelper.PiOver2, origin, verticalScale, SpriteEffects.None, 0f);
            return;
        }

        var fallbackWidth = Math.Max(88, labelWidth * 2);
        var fallbackBounds = new Rectangle(
            sectionBounds.X - fallbackWidth - labelGap,
            sectionBounds.Y,
            fallbackWidth,
            sectionBounds.Height);
        DrawVerticalSectionLabelSurface(fallbackBounds, accentColor);
        var (firstLine, secondLine) = SplitVerticalSectionLabel(title);
        var lineHeight = Math.Max(1, (fallbackBounds.Height - 12) / 2);
        DrawFittedText(firstLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 5, fallbackBounds.Width - 12, lineHeight), textColor, 0.30f);
        DrawFittedText(secondLine, new Rectangle(fallbackBounds.X + 6, fallbackBounds.Y + 7 + lineHeight, fallbackBounds.Width - 12, lineHeight), textColor, 0.30f);
    }

    private void DrawVerticalSectionLabelSurface(Rectangle bounds, Color accentColor)
    {
        FillRect(bounds, new Color(accentColor, 150));
        DrawRect(bounds, 1, new Color(accentColor, 230));
    }

    private static (string FirstLine, string SecondLine) SplitVerticalSectionLabel(string title)
    {
        var splitAt = title.LastIndexOf(' ');
        if (splitAt > 0 && splitAt < title.Length - 1)
            return (title[..splitAt], title[(splitAt + 1)..]);

        var middle = Math.Max(1, title.Length / 2);
        return (title[..middle], title[middle..]);
    }
}
