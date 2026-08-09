namespace KifuwarabeGo2026.Gui.Presentation.Shared.PlayerSelector;

using Microsoft.Xna.Framework;

/// <summary>
/// 人間・コンピューターの選択欄で共有する、ラベル・値・SELECT ボタンのレイアウトです。
/// </summary>
public readonly record struct PlayerSelector(
    Rectangle Bounds,
    string Label,
    string Value,
    string ButtonLabel = "REF",
    int LabelWidth = 126,
    int ButtonWidth = 112,
    int ButtonHeight = PlayerSelectorLayout.SelectButtonHeight,
    bool Enabled = true)
{
    public Rectangle LabelBounds => new(Bounds.X + 14, Bounds.Y + 10, LabelWidth, Bounds.Height - 20);

    public Rectangle ValueBounds => new(Bounds.X + LabelWidth + 38, Bounds.Y + 6, Bounds.Width - LabelWidth - ButtonWidth - 66, Bounds.Height - 12);

    public Rectangle BrowseButtonBounds => new(
        Bounds.Right - ButtonWidth - 14,
        Bounds.Center.Y - ButtonHeight / 2,
        ButtonWidth,
        ButtonHeight);

    public bool ContainsBrowseButton(Point point) => Enabled && BrowseButtonBounds.Contains(point);
}
