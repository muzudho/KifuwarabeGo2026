namespace KifuwarabeGo2026.Presentation.Local.Resting.TournamentRule;

using Microsoft.Xna.Framework;

public readonly record struct LabeledBrowseSelector(
    Rectangle Bounds,
    string Label,
    string Value,
    string ButtonLabel = "REF",
    int LabelWidth = 126,
    int ButtonWidth = 112,
    bool Enabled = true)
{
    public Rectangle LabelBounds => new(Bounds.X + 14, Bounds.Y + 10, LabelWidth, Bounds.Height - 20);

    public Rectangle ValueBounds => new(Bounds.X + LabelWidth + 38, Bounds.Y + 6, Bounds.Width - LabelWidth - ButtonWidth - 66, Bounds.Height - 12);

    public Rectangle BrowseButtonBounds => new(Bounds.Right - ButtonWidth - 14, Bounds.Y + 8, ButtonWidth, Bounds.Height - 16);

    public bool ContainsBrowseButton(Point point) => Enabled && BrowseButtonBounds.Contains(point);
}
