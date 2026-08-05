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
            VerifyGoAppsDiscoveryProtocol();
            VerifyAtomicInitialPositionProtocol();
            VerifyJsonEngineOptionsProtocol();
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

    private static void VerifyGoAppsDiscoveryProtocol()
    {
        var output = RunEngine(
            "known_command kfw-list-apps\n" +
            "list_commands\n" +
            "kfw-list-apps\n" +
            "kfw-list-apps player\n" +
            "kfw-list-apps provider\n" +
            "kfw-list-apps spectator\n" +
            "kfw-list-apps player extra\n" +
            "quit\n");

        Require(output.Contains("= true", StringComparison.Ordinal) &&
                output.Contains("kfw-list-apps", StringComparison.Ordinal) &&
                output.Contains("= play\nponnuki", StringComparison.Ordinal) &&
                output.Contains("= ponnuki", StringComparison.Ordinal) &&
                output.Contains("? usage: kfw-list-apps [player|provider]", StringComparison.Ordinal),
            "The bundled engine did not publish or validate its supported Go Apps list.");

        var executablePath = Path.Combine(AppContext.BaseDirectory, "KifuwarabeGo2026.Engine.exe");
        var profile = new GtpEngineProfile
        {
            DisplayName = "Bundled Kifuwarabe discovery smoke",
            ExecutablePath = executablePath,
            WorkingDirectoryStr = AppContext.BaseDirectory,
        };
        var playPlayer = GtpEngineAppCompatibilityProbe.CheckAsync(profile, "play", "player").GetAwaiter().GetResult();
        var playProvider = GtpEngineAppCompatibilityProbe.CheckAsync(profile, "play", "provider").GetAwaiter().GetResult();
        var ponnukiPlayer = GtpEngineAppCompatibilityProbe.CheckAsync(profile, "ponnuki", "player").GetAwaiter().GetResult();
        var ponnukiProvider = GtpEngineAppCompatibilityProbe.CheckAsync(profile, "ponnuki", "provider").GetAwaiter().GetResult();
        Require(
            playPlayer.CanSelect &&
            playProvider.Kind == GtpEngineAppCompatibilityKind.Unsupported &&
            ponnukiPlayer.CanSelect &&
            ponnukiProvider.CanSelect,
            "The GUI did not honor the bundled engine's role-specific Go App list.");
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
            GuiOptions: new Dictionary<string, string>
            {
                ["RandomMove"] = "Normal",
                ["AvoidEyes"] = "false",
                ["RandomSeed"] = "42",
                ["EngineTag"] = "windows smoke",
                ["ClearCache"] = "true",
            });
        var client = new GtpEngineClient(settings, TimeSpan.FromSeconds(10));
        try
        {
            client.StartAsync().GetAwaiter().GetResult();
            var options = client.SendCommandAsync("kfw-get-options play player").GetAwaiter().GetResult();
            options.ThrowIfError("kfw-get-options play player");
            Require(options.Payload.Contains("\"RandomMove\":\"Normal\"", StringComparison.Ordinal) &&
                    options.Payload.Contains("\"AvoidEyes\":false", StringComparison.Ordinal) &&
                    options.Payload.Contains("\"RandomSeed\":42", StringComparison.Ordinal) &&
                    options.Payload.Contains("\"EngineTag\":\"windows smoke\"", StringComparison.Ordinal),
                "The GUI client did not apply saved options through the typed JSON protocol.");
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
            "kfw-begin-position\n" +
            "kfw-add-black D4\n" +
            "kfw-add-white Q16\n" +
            "kfw-set-to-play white\n" +
            "kfw-commit-position\n" +
            "play black D4\n" +
            "play black Q16\n" +
            "quit\n");
        Require(successful.Contains("kfw-begin-position", StringComparison.Ordinal) &&
                successful.Contains("kfw-commit-position", StringComparison.Ordinal) &&
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
                failed.Contains("kfw-set-to-play is required", StringComparison.Ordinal) &&
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

    private static void VerifyJsonEngineOptionsProtocol()
    {
        var output = RunEngine(
            "known_command kfw-describe-options\n" +
            "known_command kfw-patch-options\n" +
            "known_command kfw-invoke-option\n" +
            "kfw-describe-options play player\n" +
            "kfw-describe-options ponnuki provider\n" +
            "kfw-describe-options play provider\n" +
            "kfw-patch-options play player {\"version\":1,\"values\":{\"RandomMove\":\"Normal\",\"AvoidEyes\":false,\"RandomSeed\":42,\"EngineTag\":\"two words\"}}\n" +
            "kfw-get-options play player\n" +
            "kfw-patch-options play player {\"version\":1,\"values\":{\"RandomMove\":\"invalid\",\"AvoidEyes\":true}}\n" +
            "kfw-get-options play player\n" +
            "kfw-patch-options play player {\"version\":1,\"values\":{\"ClearCache\":true}}\n" +
            "kfw-invoke-option play player ClearCache\n" +
            "kfw-get-option RandomMove\n" +
            "quit\n");

        const string expectedValues = "\"values\":{\"RandomMove\":\"Normal\",\"AvoidEyes\":false,\"RandomSeed\":42,\"EngineTag\":\"two words\"";
        Require(CountOccurrences(output, "= true") >= 3,
            "The JSON option commands were not published through known_command.");
        Require(output.Contains("\"type\":\"boolean\"", StringComparison.Ordinal) &&
                output.Contains("\"apply\":\"restart\"", StringComparison.Ordinal),
            "The option schema did not expose JSON-native types and apply timing.");
        Require(output.Contains("\"app\":\"ponnuki\",\"role\":\"provider\"", StringComparison.Ordinal) &&
                output.Contains("\"id\":\"BoardSize\"", StringComparison.Ordinal) &&
                output.Contains("\"binding\":\"gtp.boardsize\"", StringComparison.Ordinal) &&
                output.Contains("\"values\":[\"9\",\"13\",\"19\"]", StringComparison.Ordinal) &&
                output.Contains("\"id\":\"InitialMoveCount\"", StringComparison.Ordinal) &&
                output.Contains("\"code\":\"unsupported-app-role\"", StringComparison.Ordinal),
            "Ponnuki Provider options were not distinguished from unsupported app roles.");
        Require(CountOccurrences(output, expectedValues) == 2 &&
                output.Contains("\"code\":\"option-validation-failed\"", StringComparison.Ordinal),
            "A failed option patch changed state or did not return a JSON validation error.");
        Require(output.Contains("action options must be invoked with kfw-invoke-option", StringComparison.Ordinal) &&
                output.Contains("\"invoked\":\"ClearCache\"", StringComparison.Ordinal),
            "Action options were not separated from atomic value patches.");
        Require(output.Contains("= Normal", StringComparison.Ordinal),
            "The legacy single-option commands did not remain compatible.");

        var playSchema = RunEngine("kfw-describe-options play player\nquit\n");
        Require(!playSchema.Contains("BoardSize", StringComparison.Ordinal) &&
                !playSchema.Contains("InitialMoveCount", StringComparison.Ordinal),
            "Provider-owned Ponnuki settings leaked into the Play Player option schema.");

        var evaluation = RunEngine(
            "kfw-evaluate-options ponnuki provider {\"version\":1,\"values\":{\"BoardSize\":13,\"InitialMoveCount\":90}}\n" +
            "kfw-get-options ponnuki provider\nquit\n");
        Require(evaluation.Contains("\"BoardSize\":13", StringComparison.Ordinal) &&
                evaluation.Contains("\"InitialMoveCount\":42", StringComparison.Ordinal) &&
                evaluation.Contains("\"maximum\":42", StringComparison.Ordinal) &&
                evaluation.Contains("\"adjustments\":[{", StringComparison.Ordinal) &&
                evaluation.Contains("\"BoardSize\":9,\"InitialMoveCount\":20", StringComparison.Ordinal),
            "Tentative Ponnuki options were not evaluated dynamically and without mutation.");

        var lifecycle = RunEngine(
            "known_command kfw-start-app\n" +
            "known_command kfw-end-app\n" +
            "kfw-patch-options ponnuki provider {\"version\":1,\"values\":{\"BoardSize\":\"9\",\"InitialMoveCount\":0,\"RandomSeed\":123}}\n" +
            "kfw-get-options ponnuki provider\n" +
            "kfw-start-app ponnuki provider\n" +
            "kfw-start-app ponnuki provider\n" +
            "kfw-listen-move pass\n" +
            "kfw-end-app ponnuki provider\n" +
            "kfw-listen-move pass\n" +
            "kfw-end-app ponnuki provider\n" +
            "kfw-start-app ponnuki provider\n" +
            "kfw-end-app ponnuki provider\n" +
            "quit\n");
        Require(CountOccurrences(lifecycle, "= true") >= 2 &&
                lifecycle.Contains("\"BoardSize\":9", StringComparison.Ordinal) &&
                lifecycle.Contains("\"InitialMoveCount\":0", StringComparison.Ordinal) &&
                lifecycle.Contains("\"RandomSeed\":123", StringComparison.Ordinal) &&
                lifecycle.Contains("\"boardSize\":9", StringComparison.Ordinal) &&
                lifecycle.Contains("ponnuki provider app is already started", StringComparison.Ordinal) &&
                lifecycle.Contains("kfw-start-app or kfw-make-position must be called first", StringComparison.Ordinal),
            "The Ponnuki Provider lifecycle did not apply scoped options or enforce start/end state.");
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
