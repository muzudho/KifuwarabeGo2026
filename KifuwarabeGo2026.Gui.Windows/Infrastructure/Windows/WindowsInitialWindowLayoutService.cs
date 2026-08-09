namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

/// <summary>
/// タスクバーを除くモニター作業領域に、ウィンドウの枠も含めて収まるようにします。
/// </summary>
public sealed class WindowsInitialWindowLayoutService : IInitialWindowLayoutService
{
    public bool TryGetInitialClientSize(IntPtr windowHandle, WindowClientSize preferredSize, out WindowClientSize clientSize)
    {
        // MonoGame's GameWindow.Handle is an SDL handle, not a Win32 HWND.
        // Obtain the actual top-level HWND before calling user32 APIs.
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var nativeWindowHandle = process.MainWindowHandle;
        if (nativeWindowHandle == IntPtr.Zero ||
            !GetWindowRect(nativeWindowHandle, out var windowBounds) ||
            !GetClientRect(nativeWindowHandle, out var clientBounds))
        {
            clientSize = preferredSize;
            return false;
        }

        var windowWidth = windowBounds.Right - windowBounds.Left;
        var windowHeight = windowBounds.Bottom - windowBounds.Top;
        var clientWidth = clientBounds.Right - clientBounds.Left;
        var clientHeight = clientBounds.Bottom - clientBounds.Top;
        if (clientWidth <= 0 || clientHeight <= 0)
        {
            clientSize = preferredSize;
            return false;
        }

        var monitor = MonitorFromWindow(nativeWindowHandle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
        {
            clientSize = preferredSize;
            return false;
        }

        var workingArea = monitorInfo.WorkArea;
        var frameWidth = Math.Max(0, windowWidth - clientWidth);
        var frameHeight = Math.Max(0, windowHeight - clientHeight);

        // MonoGame's preferred back buffer size is in logical pixels, whereas
        // Win32 monitor and window rectangles are physical pixels. The live
        // client size may still be DPI-virtualized while SDL is starting, so
        // use the monitor's actual DPI rather than deriving it from that size.
        var dpi = GetDpiForWindow(nativeWindowHandle);
        var pixelsPerLogical = dpi > 0 ? dpi / 96d : 1d;
        var maximumClientSize = new WindowClientSize(
            Math.Max(1, (int)Math.Floor((workingArea.Right - workingArea.Left - frameWidth) / pixelsPerLogical)),
            Math.Max(1, (int)Math.Floor((workingArea.Bottom - workingArea.Top - frameHeight) / pixelsPerLogical)));

        GuiOperationLog.App(
            "Measured initial window layout",
            $"workArea=({workingArea.Left},{workingArea.Top})-({workingArea.Right},{workingArea.Bottom}); " +
            $"window={windowWidth}x{windowHeight}; client={clientWidth}x{clientHeight}; frame={frameWidth}x{frameHeight}; " +
            $"dpi={dpi}; maxClient={maximumClientSize.Width}x{maximumClientSize.Height}");

        clientSize = preferredSize.ConstrainTo(maximumClientSize);
        return true;
    }

    public void CenterWindowInWorkingArea(IntPtr windowHandle)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        var nativeWindowHandle = process.MainWindowHandle;
        if (nativeWindowHandle == IntPtr.Zero || !GetWindowRect(nativeWindowHandle, out var windowBounds))
            return;

        var monitor = MonitorFromWindow(nativeWindowHandle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref monitorInfo))
            return;

        var windowWidth = windowBounds.Right - windowBounds.Left;
        var windowHeight = windowBounds.Bottom - windowBounds.Top;
        var workingArea = monitorInfo.WorkArea;
        var positionX = workingArea.Left + Math.Max(0, (workingArea.Right - workingArea.Left - windowWidth) / 2);
        var positionY = workingArea.Top + Math.Max(0, (workingArea.Bottom - workingArea.Top - windowHeight) / 2);
        SetWindowPos(nativeWindowHandle, IntPtr.Zero, positionX, positionY, 0, 0, SetWindowPositionFlags);
        GuiOperationLog.App(
            "Centered initial window",
            $"position={positionX},{positionY}; window={windowWidth}x{windowHeight}; " +
            $"workArea=({workingArea.Left},{workingArea.Top})-({workingArea.Right},{workingArea.Bottom})");
    }

    private const uint MonitorDefaultToNearest = 2;
    private const uint SetWindowPositionFlags = 0x0001 | 0x0004 | 0x0010; // NOSIZE | NOZORDER | NOACTIVATE

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out WindowRect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr hWnd, out WindowRect lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public WindowRect MonitorArea;
        public WindowRect WorkArea;
        public int Flags;
    }
}
