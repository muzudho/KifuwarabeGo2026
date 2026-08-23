namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>SpriteFont にない文字を含む動的テキストを描画し、生成テクスチャを再利用します。</summary>
internal sealed class DynamicTextRenderer : IDisposable
{
    private readonly KfwScreenCanvas _canvas;
    private readonly ITextRasterizer _textRasterizer;
    private readonly Dictionary<string, Texture2D> _textures = [];

    public DynamicTextRenderer(KfwScreenCanvas canvas, ITextRasterizer textRasterizer)
    {
        _canvas = canvas;
        _textRasterizer = textRasterizer;
    }

    public void Draw(string text, Rectangle bounds, Color color, float scale)
    {
        if (_canvas.CanDrawText(text))
        {
            _canvas.DrawFittedText(text, bounds, color, scale);
            return;
        }

        if (!_textures.TryGetValue(text, out var texture))
        {
            var png = _textRasterizer.RasterizePng(text, pixelHeight: 28, bold: true);
            using var stream = new MemoryStream(png, writable: false);
            texture = Texture2D.FromStream(_canvas.GraphicsDevice, stream);
            _textures[text] = texture;
        }

        var targetHeight = MathF.Min(bounds.Height, _canvas.FontLineSpacing * scale);
        var fittedScale = MathF.Min(bounds.Width / (float)texture.Width, targetHeight / texture.Height);
        _canvas.DrawTexture(texture, new Rectangle(bounds.X,
            bounds.Y + (bounds.Height - (int)(texture.Height * fittedScale)) / 2,
            (int)(texture.Width * fittedScale), (int)(texture.Height * fittedScale)), color);
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values) texture.Dispose();
        _textures.Clear();
    }
}
