namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// 画面内で最も強く伝えたいメッセージを表示する、大見出し用の文房具 UI です。
/// </summary>
public sealed class Headline
{
    public Headline(string text, Vector2 position, Color color, float textScale)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Position = position;
        Color = color;
        TextScale = textScale;
    }

    public string Text { get; set; }
    public Vector2 Position { get; set; }
    public Color Color { get; set; }
    public float TextScale { get; set; }

    public void Draw(IHeadlineDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);
        surface.DrawText(Text, Position, Color, TextScale);
    }
}

public interface IHeadlineDrawingSurface
{
    void DrawText(string text, Vector2 position, Color color, float scale);
}
