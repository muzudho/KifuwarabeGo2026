namespace KifuwarabeGo2026.Application;

using System;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// ［システム・クリップボード］の操作をするぜ（＾～＾）
/// </summary>
public static class SystemClipboard
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public static bool SetText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
        if (!OpenClipboard(IntPtr.Zero)) return false;

        var clipboardData = IntPtr.Zero;
        try
        {
            EmptyClipboard();

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            clipboardData = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
            if (clipboardData == IntPtr.Zero) return false;

            var lockedMemory = GlobalLock(clipboardData);
            if (lockedMemory == IntPtr.Zero) return false;

            try
            {
                Marshal.Copy(bytes, 0, lockedMemory, bytes.Length);
            }
            finally
            {
                GlobalUnlock(clipboardData);
            }

            if (SetClipboardData(CfUnicodeText, clipboardData) == IntPtr.Zero) return false;

            clipboardData = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (clipboardData != IntPtr.Zero)
            {
                GlobalFree(clipboardData);
            }

            CloseClipboard();
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
