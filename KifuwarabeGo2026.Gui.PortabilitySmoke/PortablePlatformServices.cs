namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui.Application;
using System;
using System.Collections.Generic;

/// <summary>
/// Windows API を使わずに Core のプラットフォーム契約を満たす最小実装です。
/// Linux/macOS 版を作る際は、このクラスを役割別の実装へ置き換えます。
/// </summary>
internal sealed class PortablePlatformServices :
    IClipboardService,
    ITextCompositionService,
    IFileDialogService,
    IDesktopLauncher,
    ITextRasterizer,
    IWindowIconService,
    IInitialWindowLayoutService,
    IPlatformExecutableService,
    IWindowScreenshotService
{
    public bool SupportsDiagnosticAdornment => false;

    public event Action<TextCompositionState>? CompositionChanged
    {
        add { }
        remove { }
    }

    public event Action<TextCompositionDiagnostics>? DiagnosticsChanged
    {
        add { }
        remove { }
    }

    public void Update()
    {
    }

    private static readonly byte[] TransparentPixelPng =
        Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M/wHwAF/gL+X2NDNwAAAABJRU5ErkJggg==");

    public IReadOnlyList<FileDialogFilter> SelectionFilters { get; } =
        [new FileDialogFilter("All files", ["*"])];

    public bool TrySetText(string text) => false;

    public bool TryGetText(out string text)
    {
        text = "";
        return false;
    }

    public string? OpenFile(OpenFileDialogOptions options) => null;

    public string? SaveFile(SaveFileDialogOptions options) => null;

    public string? SelectFolder(FolderDialogOptions options) => null;

    public void OpenTextFile(string path)
    {
    }

    public DesktopOpenResult OpenFileWithPreferredApplication(
        string path,
        string preferredApplication) =>
        DesktopOpenResult.DefaultApplication;

    public void OpenDirectory(string path)
    {
    }

    public void RevealFile(string path)
    {
    }

    public void TailTextFile(string path, string windowTitle)
    {
    }

    public byte[] RasterizePng(string text, int pixelHeight, bool bold) =>
        (byte[])TransparentPixelPng.Clone();

    public float MeasureTextWidth(string text, int pixelHeight, bool bold) => 0;

    public int GetWrappedPageCount(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing) =>
        1;

    public byte[] RasterizeWrappedPagePng(
        string text,
        int width,
        int height,
        int pixelHeight,
        int extraLineSpacing,
        int requestedPage) =>
        (byte[])TransparentPixelPng.Clone();

    public void TryApply(IntPtr windowHandle)
    {
    }

    public bool TryGetInitialClientSize(IntPtr windowHandle, WindowClientSize preferredSize, out WindowClientSize clientSize)
    {
        clientSize = preferredSize;
        return true;
    }

    public void CenterWindowInWorkingArea(IntPtr windowHandle)
    {
    }

    public string GetFileName(string baseName) => baseName;

    public WindowScreenshotResult SaveActiveWindow(string filePath) =>
        new(0, 0, 1, 1, 1, 1, 96, 96, "Portable", "Portable screenshot stub");
}
