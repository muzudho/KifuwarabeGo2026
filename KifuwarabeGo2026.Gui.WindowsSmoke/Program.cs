namespace KifuwarabeGo2026.Gui.WindowsSmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Infrastructure.Windows;
using KifuwarabeGo2026.Engine;
using KifuwarabeGo2026.Gui.Gtp;
using KifuwarabeGo2026.GtpExtensions.Capabilities;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Match;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.IO;

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
            VerifyAtomicInitialPositionProtocol();
            VerifyBundledEngineInitialPositionPipeline();
            Console.WriteLine("PASS: Windows platform services passed non-interactive checks.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"FAIL: {ex.Message}");
            return 1;
        }
    }

    private static void VerifyBundledEngineInitialPositionPipeline()
    {
        var executablePath = Path.Combine(AppContext.BaseDirectory, "KifuwarabeGo2026.Engine.exe");
        Require(File.Exists(executablePath), "The bundled engine executable was not copied to WindowsSmoke output.");
        var workingDirectoryProfile = new GtpEngineProfile
        {
            WorkingDirectoryStr = AppContext.BaseDirectory,
        };
        var settings = new GtpEngineSettings(
            "Bundled Kifuwarabe smoke",
            executablePath,
            workingDirectoryProfile.WorkingDirectoryModel,
            "",
            EnableGtpLog: false,
            LogPrefix: "[atomic-smoke]",
            GuiOptions: new Dictionary<string, string>());
        var client = new GtpEngineClient(settings, TimeSpan.FromSeconds(10));
        try
        {
            client.StartAsync().GetAwaiter().GetResult();
            var capabilities = new GtpCapabilityProbe()
                .ProbeInitialPositionAsync(new GtpEngineClientCommandSession(client))
                .GetAwaiter().GetResult();
            var profile = BuiltInGtpProfiles.Resolve(capabilities);
            Require(profile.Id == BuiltInGtpProfiles.KifuwarabeId,
                "The bundled engine identity did not select the Kifuwarabe profile.");
            var request = new InitialPositionRequest(
                19,
                6.5m,
                GoStone.White,
                [
                    new MatchSetupStone(GoStone.Black, new GoPoint(3, 15)),
                    new MatchSetupStone(GoStone.White, new GoPoint(15, 3)),
                ]);
            var result = new InitialPositionConcierge().ExecuteAsync(
                new GtpInitialPositionExecutionHost(client),
                request,
                capabilities,
                profile).GetAwaiter().GetResult();
            Require(result.IsVerified &&
                    result.LastAttempt?.Method == InitialPositionMethod.KifuwarabeAtomicSetup,
                "The bundled engine did not complete the GUI atomic initial-position pipeline as verified.");
        }
        finally
        {
            client.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    private static void VerifyAtomicInitialPositionProtocol()
    {
        var successful = RunEngine(
            "list_commands\n" +
            "clear_board\n" +
            "begin_position\n" +
            "add_black D4\n" +
            "add_white Q16\n" +
            "set_to_play white\n" +
            "commit_position\n" +
            "play black D4\n" +
            "play black Q16\n" +
            "quit\n");
        Require(successful.Contains("begin_position", StringComparison.Ordinal) &&
                successful.Contains("commit_position", StringComparison.Ordinal) &&
                CountOccurrences(successful, "? illegal move") == 2,
            "Atomic commit did not expose both setup stones on the live board.");

        var freelyEdited = RunEngine(
            "clear_board\n" +
            "begin_position\n" +
            "add_black D4\n" +
            "add_white C4\n" +
            "add_white E4\n" +
            "add_white D3\n" +
            "add_white D5\n" +
            "set_to_play black\n" +
            "commit_position\n" +
            "play black D4\n" +
            "quit\n");
        Require(CountOccurrences(freelyEdited, "?") == 1 &&
                freelyEdited.Contains("? illegal move", StringComparison.Ordinal),
            "Atomic setup did not preserve a freely edited position containing a stone with no liberties.");

        var failed = RunEngine(
            "clear_board\n" +
            "play black D4\n" +
            "begin_position\n" +
            "add_white Q16\n" +
            "add_black Q16\n" +
            "play white D4\n" +
            "begin_position\n" +
            "add_black A0\n" +
            "play white D4\n" +
            "begin_position\n" +
            "add_black Q16\n" +
            "commit_position\n" +
            "play white D4\n" +
            "quit\n");
        Require(failed.Contains("point is already occupied", StringComparison.Ordinal) &&
                failed.Contains("invalid vertex", StringComparison.Ordinal) &&
                failed.Contains("set_to_play is required", StringComparison.Ordinal) &&
                CountOccurrences(failed, "? illegal move") == 3,
            "A failed atomic setup changed the pre-existing live position.");

        var guarded = RunEngine(
            "clear_board\n" +
            "play black D4\n" +
            "begin_position\n" +
            "add_white Q16\n" +
            "play white C3\n" +
            "abort_position\n" +
            "abort_position\n" +
            "play white D4\n" +
            "quit\n");
        Require(guarded.Contains("position setup is active", StringComparison.Ordinal) &&
                guarded.Contains("? illegal move", StringComparison.Ordinal),
            "Standard board mutation was not guarded during atomic setup or abort changed the live board.");
    }

    private static string RunEngine(string commands)
    {
        using var input = new StringReader(commands);
        using var output = new StringWriter();
        new GtpEngine().Run(input, output);
        return output.ToString();
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        for (var index = 0; (index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0; index += value.Length)
            count++;
        return count;
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
