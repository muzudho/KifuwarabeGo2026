namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Small ASCII font for the standalone match window. Each row is a five-bit mask.</summary>
internal sealed class GoMatchText : IDisposable
{
    private const int Width = 5;
    private const int Height = 7;
    private const string Characters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789[]+-.%#|?";
    private static readonly byte[][] Glyphs =
    [
        [14,17,17,31,17,17,17], [30,17,17,30,17,17,30], [14,17,16,16,16,17,14],
        [30,17,17,17,17,17,30], [31,16,16,30,16,16,31], [31,16,16,30,16,16,16],
        [14,17,16,23,17,17,15], [17,17,17,31,17,17,17], [14,4,4,4,4,4,14],
        [7,2,2,2,18,18,12], [17,18,20,24,20,18,17], [16,16,16,16,16,16,31],
        [17,27,21,21,17,17,17], [17,25,25,21,19,19,17], [14,17,17,17,17,17,14],
        [30,17,17,30,16,16,16], [14,17,17,17,21,18,13], [30,17,17,30,20,18,17],
        [15,16,16,14,1,1,30], [31,4,4,4,4,4,4], [17,17,17,17,17,17,14],
        [17,17,17,17,17,10,4], [17,17,17,21,21,27,17], [17,17,10,4,10,17,17],
        [17,17,10,4,4,4,4], [31,1,2,4,8,16,31],
        [14,17,19,21,25,17,14], [4,12,4,4,4,4,14], [14,17,1,2,4,8,31],
        [30,1,1,14,1,1,30], [2,6,10,18,31,2,2], [31,16,16,30,1,1,30],
        [14,16,16,30,17,17,14], [31,1,2,4,8,8,8], [14,17,17,14,17,17,14],
        [14,17,17,15,1,1,14],
        [14,8,8,8,8,8,14], [14,2,2,2,2,2,14], [0,4,4,31,4,4,0],
        [0,0,0,31,0,0,0], [0,0,0,0,0,12,12], [25,26,2,4,8,11,19],
        [10,10,31,10,31,10,10], [4,4,4,4,4,4,4], [14,17,1,6,4,0,4],
    ];

    private readonly Texture2D _texture;

    public GoMatchText(GraphicsDevice graphics)
    {
        _texture = new Texture2D(graphics, Characters.Length * Width, Height);
        var pixels = new Color[_texture.Width * _texture.Height];
        for (var index = 0; index < Characters.Length; index++)
        for (var y = 0; y < Height; y++)
        for (var x = 0; x < Width; x++)
            if ((Glyphs[index][y] & (1 << (Width - 1 - x))) != 0)
                pixels[y * _texture.Width + index * Width + x] = Color.White;
        _texture.SetData(pixels);
    }

    public static int Measure(string text, int scale) => text.Length * (Width + 1) * scale;

    public void Draw(SpriteBatch batch, string text, int x, int y, int available, int scale, Color color)
    {
        if (available <= 0) return;
        scale = Math.Max(1, Math.Min(scale, available / Math.Max(1, Measure(text, 1))));
        var limit = x + available;
        foreach (var character in text)
        {
            if (x + Width * scale > limit) break;
            if (character != ' ')
            {
                var glyph = Characters.IndexOf(char.ToUpperInvariant(character));
                if (glyph < 0) glyph = Characters.Length - 1;
                batch.Draw(_texture, new Rectangle(x, y, Width * scale, Height * scale),
                    new Rectangle(glyph * Width, 0, Width, Height), color);
            }
            x += (Width + 1) * scale;
        }
    }

    public void Dispose() => _texture.Dispose();
}
