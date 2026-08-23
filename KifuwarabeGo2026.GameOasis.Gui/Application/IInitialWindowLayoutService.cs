namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;

/// <summary>
/// 起動時に、現在のモニターで利用できるクライアント領域を決定します。
/// </summary>
public interface IInitialWindowLayoutService
{
    bool TryGetInitialClientSize(IntPtr windowHandle, WindowClientSize preferredSize, out WindowClientSize clientSize);

    void CenterWindowInWorkingArea(IntPtr windowHandle);
}

public readonly record struct WindowClientSize(int Width, int Height)
{
    public WindowClientSize ConstrainTo(WindowClientSize maximumSize) => new(
        Math.Max(1, Math.Min(Width, maximumSize.Width)),
        Math.Max(1, Math.Min(Height, maximumSize.Height)));
}
