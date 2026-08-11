namespace KifuwarabeGo2026.Gui.Presentation.BoardLens;

using Microsoft.Xna.Framework;

/// <summary>
/// Board Lens を操作する L / 前へ / 次へ / 終了ボタンの共通配置です。
/// </summary>
internal readonly record struct BoardLensButtonStrip(
    Rectangle ToggleBounds,
    Rectangle PreviousBounds,
    Rectangle NextBounds,
    Rectangle ExitBounds)
{
    public BoardLensButtonStrip(int x, int y, int buttonSize = 60, int gap = 12)
        : this(
            new Rectangle(x, y, buttonSize, buttonSize),
            new Rectangle(x + buttonSize + gap, y, buttonSize, buttonSize),
            new Rectangle(x + (buttonSize + gap) * 2, y, buttonSize, buttonSize),
            new Rectangle(x + (buttonSize + gap) * 3, y, buttonSize, buttonSize))
    {
    }

    public BoardLensButton? GetHit(Point point, bool isLensEnabled)
    {
        if (ToggleBounds.Contains(point)) return BoardLensButton.Toggle;
        if (!isLensEnabled) return null;
        if (PreviousBounds.Contains(point)) return BoardLensButton.Previous;
        if (NextBounds.Contains(point)) return BoardLensButton.Next;
        return ExitBounds.Contains(point) ? BoardLensButton.Exit : null;
    }
}

internal enum BoardLensButton
{
    Toggle,
    Previous,
    Next,
    Exit,
}
