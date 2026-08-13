namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Linq;

public sealed partial class GoScreenRenderer
{
    private void DrawCompositionLamp(string label, int x, bool enabled, Color activeColor) =>
        DrawCompositionLamp(TextInputDialog.Bounds, label, x, enabled, activeColor);

    private void DrawCompositionLamp(Rectangle dialogBounds, string label, int x, bool enabled, Color activeColor)
    {
        var center = new Vector2(x, dialogBounds.Y + 47);
        DrawCircle(center, 8, enabled ? activeColor : new Color(79, 89, 98));
        DrawText(label, new Vector2(center.X - _font.MeasureString(label).X * 0.11f, dialogBounds.Y + 66), new Color(180, 195, 195), 0.22f);
    }

    private float DrawDynamicCompositionText(string text, Vector2 position, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            DrawText(text, position, color, scale);
            return _font.MeasureString(text).X * scale;
        }

        if (!_dynamicOptionTextTextures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _dynamicOptionTextTextures[text] = texture;
        }

        var targetHeight = _font.LineSpacing * scale;
        var textureScale = targetHeight / texture.Height;
        var width = texture.Width * textureScale;
        _spriteBatch.Draw(texture, new Rectangle((int)position.X, (int)position.Y, (int)width, (int)targetHeight), color);
        return width;
    }
}
