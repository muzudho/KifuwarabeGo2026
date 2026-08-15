namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.Gui.Application;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>SpriteFont にない文字を含む動的テキストを描画し、生成テクスチャを再利用します。</summary>
internal sealed class DynamicTextRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly ITextRasterizer _textRasterizer;
    private readonly Action<string, Rectangle, Color, float> _drawFittedText;
    private readonly Dictionary<string, Texture2D> _textures = [];

    public DynamicTextRenderer(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch, SpriteFont font,
        ITextRasterizer textRasterizer, Action<string, Rectangle, Color, float> drawFittedText)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _font = font;
        _textRasterizer = textRasterizer;
        _drawFittedText = drawFittedText;
    }

    public void Draw(string text, Rectangle bounds, Color color, float scale)
    {
        if (text.All(character => _font.Characters.Contains(character)))
        {
            _drawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_textures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_graphicsDevice, stream);
            _textures[text] = texture;
        }

        var targetHeight = MathF.Min(bounds.Height, _font.LineSpacing * scale);
        var fittedScale = MathF.Min(bounds.Width / (float)texture.Width, targetHeight / texture.Height);
        _spriteBatch.Draw(texture, new Rectangle(bounds.X,
            bounds.Y + (bounds.Height - (int)(texture.Height * fittedScale)) / 2,
            (int)(texture.Width * fittedScale), (int)(texture.Height * fittedScale)), color);
    }
}
