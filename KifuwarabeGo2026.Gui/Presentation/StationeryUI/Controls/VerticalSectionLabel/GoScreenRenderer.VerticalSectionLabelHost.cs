namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.VerticalSectionLabel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public sealed partial class GoScreenRenderer
{
    private readonly VerticalSectionLabel _verticalSectionLabel = new();

    private void DrawVerticalResultSection(Rectangle bounds, string title, Color accentColor,
        Color? textColor = null, int labelWidth = 38, int labelGap = 8)
    {
        DrawLine(new Vector2(bounds.X, bounds.Y), new Vector2(bounds.Right, bounds.Y), 1, new Color(58, 78, 86));
        _verticalSectionLabel.Draw(bounds, title, accentColor, textColor ?? new Color(205, 218, 218), labelWidth, labelGap,
            new VerticalSectionLabelDrawingCallbacks(
                _font.MeasureString,
                DrawRotatedCenteredText,
                FillRect,
                DrawRect,
                DrawFittedText));
    }

    private void DrawRotatedCenteredText(string text, Vector2 center, Color color, float scale)
    {
        _spriteBatch.DrawString(_font, text, center, color, -MathHelper.PiOver2, _font.MeasureString(text) / 2f, scale, SpriteEffects.None, 0f);
    }
}
