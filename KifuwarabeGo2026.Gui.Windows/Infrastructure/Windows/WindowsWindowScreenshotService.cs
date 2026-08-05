namespace KifuwarabeGo2026.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.Gui.Application;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

/// <summary>
/// Windows のアクティブウィンドウを、タイトルバーと外枠を含めて PNG 保存します。
/// </summary>
public sealed class WindowsWindowScreenshotService : IWindowScreenshotService
{
    public void SaveActiveWindow(string filePath)
    {
        var window = GetForegroundWindow();
        if (window == IntPtr.Zero || !GetWindowRect(window, out var bounds))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not obtain the active window bounds.");

        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        if (width <= 0 || height <= 0)
            throw new InvalidOperationException("The active window has no drawable area.");

        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        }

        bitmap.Save(filePath, ImageFormat.Png);
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

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
}
