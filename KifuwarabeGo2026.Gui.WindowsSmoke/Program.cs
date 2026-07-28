namespace KifuwarabeGo2026.Gui.WindowsSmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Infrastructure.Windows;
using System;
using System.Buffers.Binary;
using System.Linq;

internal static class Program
{
    private static readonly byte[] PngSignature =
        [137, 80, 78, 71, 13, 10, 26, 10];

    private static int Main()
    {
        try
        {
            VerifyServiceComposition();
            VerifyExecutableNaming();
            VerifyTextRasterizer();
            VerifyWindowsAssembly();
            Console.WriteLine("PASS: Windows platform services passed non-interactive checks.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void VerifyServiceComposition()
    {
        IClipboardService clipboard = new WindowsClipboardService();
        IMessageDialogService messageDialog = new WindowsMessageDialogService();
        IFileDialogService fileDialog = new WindowsFileDialogService();
        ITextInputDialogService textInputDialog = new WindowsTextInputDialogService();
        IDesktopLauncher desktopLauncher = new WindowsDesktopLauncher();
        ITextRasterizer textRasterizer = new WindowsTextRasterizer();
        IWindowIconService windowIcon = new WindowsWindowIconService();
        IPlatformExecutableService executable = new WindowsPlatformExecutableService();

        Require(
            clipboard is not null
            && messageDialog is not null
            && fileDialog is not null
            && textInputDialog is not null
            && desktopLauncher is not null
            && textRasterizer is not null
            && windowIcon is not null
            && executable is not null,
            "One or more Windows platform services could not be created.");
    }

    private static void VerifyExecutableNaming()
    {
        var service = new WindowsPlatformExecutableService();

        Require(service.GetFileName("engine") == "engine.exe", "Windows executable suffix was not added.");
        Require(service.GetFileName("engine.EXE") == "engine.EXE", "Existing executable suffix was changed.");
        Require(
            service.SelectionFilters.Any(filter =>
                filter.Patterns.Any(pattern =>
                    pattern.Equals("*.exe", StringComparison.OrdinalIgnoreCase))),
            "Windows executable selection filter does not contain *.exe.");
    }

    private static void VerifyTextRasterizer()
    {
        var rasterizer = new WindowsTextRasterizer();
        var singleLine = rasterizer.RasterizePng("Portability smoke", 18, bold: true);
        VerifyPng(singleLine, minimumWidth: 2, minimumHeight: 2);

        var wrapped = rasterizer.RasterizeWrappedPagePng(
            "Windows text rasterizer wrapped page smoke test.",
            width: 320,
            height: 180,
            pixelHeight: 18,
            extraLineSpacing: 2,
            requestedPage: 0);
        VerifyPng(wrapped, minimumWidth: 320, minimumHeight: 180);
        Require(
            ReadPngWidth(wrapped) == 320 && ReadPngHeight(wrapped) == 180,
            "Wrapped text PNG dimensions differ from the requested drawing area.");
        Require(
            rasterizer.GetWrappedPageCount(
                "Windows text rasterizer wrapped page smoke test.",
                width: 320,
                height: 180,
                pixelHeight: 18,
                extraLineSpacing: 2) >= 1,
            "Wrapped text page count must be at least one.");
    }

    private static void VerifyWindowsAssembly()
    {
        var windowsAssembly = typeof(WindowsPlatformExecutableService).Assembly;
        var coreAssembly = typeof(Game1).Assembly;
        var resources = windowsAssembly.GetManifestResourceNames();

        Require(
            windowsAssembly.GetName().Name == "KifuwarabeGo2026.Gui",
            "Windows entry assembly name must remain KifuwarabeGo2026.Gui.");
        Require(
            windowsAssembly.GetName().Version == coreAssembly.GetName().Version,
            "Windows and Core assembly versions differ.");
        Require(
            resources.Contains("GuiIcon.ico", StringComparer.Ordinal),
            "Embedded GuiIcon.ico resource was not found.");
    }

    private static void VerifyPng(byte[] bytes, int minimumWidth, int minimumHeight)
    {
        Require(
            bytes.Length >= 24 && bytes.AsSpan(0, 8).SequenceEqual(PngSignature),
            "Text rasterizer did not return a PNG image.");
        Require(
            ReadPngWidth(bytes) >= minimumWidth && ReadPngHeight(bytes) >= minimumHeight,
            "Text rasterizer returned an unexpectedly small PNG image.");
    }

    private static int ReadPngWidth(byte[] bytes) =>
        BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));

    private static int ReadPngHeight(byte[] bytes) =>
        BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
