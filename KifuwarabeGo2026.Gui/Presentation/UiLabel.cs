namespace KifuwarabeGo2026.Presentation;

using Microsoft.Xna.Framework;

public sealed class UiLabel
{
    public static readonly Color TextColor = new(158, 178, 178);

    public UiLabel(string text, Rectangle bounds, float scale)
    {
        Text = text;
        Bounds = bounds;
        Scale = scale;
    }

    public string Text { get; }

    public Rectangle Bounds { get; }

    public float Scale { get; }

    public static UiLabel InRow(string text, Rectangle rowBounds) =>
        new(text, new Rectangle(rowBounds.X + 18, rowBounds.Y + (rowBounds.Height - 38) / 2, 132, 38), 0.40f);

    public static UiLabel InCompactRow(string text, Rectangle rowBounds) =>
        new(text, new Rectangle(rowBounds.X + 18, rowBounds.Y + (rowBounds.Height - 32) / 2, 118, 32), 0.34f);
}
