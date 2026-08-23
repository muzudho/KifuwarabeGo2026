namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 現在のプロセスに属するゲームウィンドウを、タイトルバーと外枠を含めて PNG 保存します。
/// </summary>
public sealed class WindowsWindowScreenshotService : IWindowScreenshotService
{
    public WindowScreenshotResult SaveActiveWindow(string filePath)
    {
        var candidates = new StringBuilder();
        var window = FindCurrentProcessMainWindow(candidates, out var selection);
        if (window == IntPtr.Zero || !GetWindowRect(window, out var bounds))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not obtain the game window bounds.");

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("The active window has no drawable area.");

        GetClientRect(window, out var clientBounds);
        var clientOrigin = new WindowPoint();
        ClientToScreen(window, ref clientOrigin);
        var hasDwmBounds = DwmGetWindowAttribute(
            window,
            DwmExtendedFrameBounds,
            out var dwmBounds,
            Marshal.SizeOf<WindowRect>()) == 0;
        var captureBounds = hasDwmBounds ? dwmBounds : bounds;
        var captureWidth = captureBounds.Right - captureBounds.Left;
        var captureHeight = captureBounds.Bottom - captureBounds.Top;
        var windowDpi = GetDpiForWindow(window);
        var systemDpi = GetDpiForSystem();
        var dpiAwareness = GetAwarenessFromDpiAwarenessContext(GetThreadDpiAwarenessContext()).ToString();
        var virtualScreen = System.Windows.Forms.SystemInformation.VirtualScreen;

        using var bitmap = new Bitmap(captureWidth, captureHeight, PixelFormat.Format32bppArgb);
        float graphicsDpiX;
        float graphicsDpiY;
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphicsDpiX = graphics.DpiX;
            graphicsDpiY = graphics.DpiY;
            graphics.CopyFromScreen(captureBounds.Left, captureBounds.Top, 0, 0, new Size(captureWidth, captureHeight), CopyPixelOperation.SourceCopy);
        }

        bitmap.Save(filePath, ImageFormat.Png);
        var diagnostics =
            $"selection={selection} hwnd=0x{window.ToInt64():X} title=\"{GetWindowTitle(window)}\" " +
            $"window=({bounds.Left},{bounds.Top})-({bounds.Right},{bounds.Bottom}) size={width}x{height} " +
            $"clientOrigin=({clientOrigin.X},{clientOrigin.Y}) clientSize={clientBounds.Right - clientBounds.Left}x{clientBounds.Bottom - clientBounds.Top} " +
            $"dwm={(hasDwmBounds ? $"({dwmBounds.Left},{dwmBounds.Top})-({dwmBounds.Right},{dwmBounds.Bottom}) size={dwmBounds.Right - dwmBounds.Left}x{dwmBounds.Bottom - dwmBounds.Top}" : "unavailable")} " +
            $"capture=({captureBounds.Left},{captureBounds.Top}) size={captureWidth}x{captureHeight} " +
            $"logicalToPhysicalDelta=({captureBounds.Left - bounds.Left},{captureBounds.Top - bounds.Top}) " +
            $"logicalToPhysicalScale=({captureWidth / (double)width:0.###},{captureHeight / (double)height:0.###}) " +
            $"screenshot={bitmap.Width}x{bitmap.Height} windowDpi={windowDpi} systemDpi={systemDpi} " +
            $"graphicsDpi={graphicsDpiX:0.##}x{graphicsDpiY:0.##} awareness={dpiAwareness} " +
            $"virtualScreen=({virtualScreen.X},{virtualScreen.Y}) {virtualScreen.Width}x{virtualScreen.Height} " +
            $"candidates=[{candidates}]";
        return new WindowScreenshotResult(
            captureBounds.Left,
            captureBounds.Top,
            captureWidth,
            captureHeight,
            bitmap.Width,
            bitmap.Height,
            windowDpi,
            systemDpi,
            dpiAwareness,
            diagnostics);
    }

    private static IntPtr FindCurrentProcessMainWindow(StringBuilder candidates, out string selection)
    {
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        AppendWindowDiagnostics(candidates, "Process.MainWindowHandle", process.MainWindowHandle);
        if (process.MainWindowHandle != IntPtr.Zero &&
            IsWindowVisible(process.MainWindowHandle) &&
            GetWindow(process.MainWindowHandle, GetWindowOwner) == IntPtr.Zero &&
            GetWindowTextLength(process.MainWindowHandle) > 0 &&
            GetWindowRect(process.MainWindowHandle, out _))
        {
            selection = "Process.MainWindowHandle";
            return process.MainWindowHandle;
        }

        var processId = (uint)Environment.ProcessId;
        var bestWindow = IntPtr.Zero;
        long bestArea = 0;
        EnumWindows((window, _) =>
        {
            GetWindowThreadProcessId(window, out var windowProcessId);
            if (windowProcessId == processId)
                AppendWindowDiagnostics(candidates, "EnumWindows", window);
            if (windowProcessId != processId ||
                !IsWindowVisible(window) ||
                GetWindow(window, GetWindowOwner) != IntPtr.Zero ||
                GetWindowTextLength(window) == 0 ||
                !GetWindowRect(window, out var bounds))
                return true;

            var width = Math.Max(0, bounds.Right - bounds.Left);
            var height = Math.Max(0, bounds.Bottom - bounds.Top);
            var area = (long)width * height;
            if (area > bestArea)
            {
                bestArea = area;
                bestWindow = window;
            }
            return true;
        }, IntPtr.Zero);

        selection = "EnumWindows titled ownerless largest";
        return bestWindow;
    }

    private static void AppendWindowDiagnostics(StringBuilder output, string source, IntPtr window)
    {
        if (window == IntPtr.Zero)
        {
            output.Append(source).Append(":null; ");
            return;
        }

        GetWindowRect(window, out var bounds);
        output.Append(source)
            .Append(":hwnd=0x").Append(window.ToInt64().ToString("X"))
            .Append(" title=\"").Append(GetWindowTitle(window)).Append('"')
            .Append(" visible=").Append(IsWindowVisible(window))
            .Append(" owner=0x").Append(GetWindow(window, GetWindowOwner).ToInt64().ToString("X"))
            .Append(" rect=(").Append(bounds.Left).Append(',').Append(bounds.Top).Append(")-(")
            .Append(bounds.Right).Append(',').Append(bounds.Bottom).Append("); ");
    }

    private static string GetWindowTitle(IntPtr window)
    {
        var length = GetWindowTextLength(window);
        if (length <= 0) return "";
        var title = new StringBuilder(length + 1);
        GetWindowText(window, title, title.Capacity);
        return title.ToString();
    }

    private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr window);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximumCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(IntPtr window, out WindowRect bounds);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr window, ref WindowPoint point);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    private static extern IntPtr GetThreadDpiAwarenessContext();

    [DllImport("user32.dll")]
    private static extern DpiAwareness GetAwarenessFromDpiAwarenessContext(IntPtr value);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr window, int attribute, out WindowRect value, int valueSize);

    private const uint GetWindowOwner = 4;
    private const int DwmExtendedFrameBounds = 9;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr window, out WindowRect bounds);

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowPoint
    {
        public int X;
        public int Y;
    }

    private enum DpiAwareness
    {
        Invalid = -1,
        Unaware,
        SystemAware,
        PerMonitorAware,
    }
}
