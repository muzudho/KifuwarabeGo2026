namespace KifuwarabeGo2026.Presentation;

using Microsoft.Xna.Framework;

public readonly record struct LabeledBrowseSelector(Rectangle Bounds, string Label, string Value)
{
    public Rectangle LabelBounds => new(Bounds.X + 14, Bounds.Y + 10, 126, Bounds.Height - 20);

    public Rectangle ValueBounds => new(Bounds.X + 164, Bounds.Y + 6, Bounds.Width - 300, Bounds.Height - 12);

    public Rectangle BrowseButtonBounds => new(Bounds.Right - 126, Bounds.Y + 8, 112, Bounds.Height - 16);

    public bool ContainsBrowseButton(Point point) => BrowseButtonBounds.Contains(point);
}
