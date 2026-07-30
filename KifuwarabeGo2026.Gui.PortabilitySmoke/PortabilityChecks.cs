namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

internal static class PortabilityChecks
{
    private static readonly string[] ForbiddenAssemblyReferences =
    [
        "System.Drawing.Common",
        "System.Windows.Forms",
    ];

    public static void Run()
    {
        var coreAssembly = typeof(Game1).Assembly;

        VerifyTargetFramework(coreAssembly);
        VerifyAssemblyReferences(coreAssembly);
        VerifyNoPlatformInvokes(coreAssembly);
        VerifyPortableFallbacks();
        VerifyScoreAxisScaling();
        VerifyCommentNavigation();
        VerifyCatalogOrderEditor();
        VerifyCgosConnectionOrder();
        VerifyOptionalCgosInputs();
        VerifyTextBoxEditing();
        VerifyDefaultCgosConnection();
        VerifyTournamentRulesJsonCompatibility();
        VerifyComposition();
    }

    private static void VerifyTargetFramework(Assembly coreAssembly)
    {
        var framework = coreAssembly
            .GetCustomAttribute<TargetFrameworkAttribute>()
            ?.FrameworkName;

        Require(
            framework == ".NETCoreApp,Version=v8.0",
            $"Core must target net8.0, but was '{framework ?? "(unknown)"}'.");
    }

    private static void VerifyAssemblyReferences(Assembly coreAssembly)
    {
        var references = coreAssembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var forbiddenReference in ForbiddenAssemblyReferences)
        {
            Require(
                !references.Contains(forbiddenReference),
                $"Core directly references Windows-only assembly '{forbiddenReference}'.");
        }
    }

    private static void VerifyNoPlatformInvokes(Assembly coreAssembly)
    {
        var platformInvoke = coreAssembly
            .GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public
                | BindingFlags.NonPublic
                | BindingFlags.Static
                | BindingFlags.Instance))
            .FirstOrDefault(method =>
                (method.Attributes & MethodAttributes.PinvokeImpl) != 0);

        Require(
            platformInvoke is null,
            platformInvoke is null
                ? string.Empty
                : $"Core contains P/Invoke method '{platformInvoke.DeclaringType?.FullName}.{platformInvoke.Name}'.");
    }

    private static void VerifyPortableFallbacks()
    {
        var platform = new PortablePlatformServices();

        Require(!platform.TrySetText("smoke"), "Clipboard fallback must report unsupported.");
        Require(platform.GetFileName("engine") == "engine", "Portable executable name must not add .exe.");
        Require(platform.SelectionFilters.Count > 0, "Executable selection needs at least one fallback filter.");
        Require(platform.RasterizePng("smoke", 16, false).Length > 8, "Text fallback must return PNG bytes.");
        Require(
            platform.GetWrappedPageCount("smoke", 320, 180, 16, 2) == 1,
            "Text fallback must expose one safe placeholder page.");
    }

    private static void VerifyComposition()
    {
        Func<Game1> composition = CreateGame;
        Require(composition is not null, "Game composition must compile.");
    }

    private static void VerifyScoreAxisScaling()
    {
        Require(
            MoveTrendScoreAxis.CalculateMaximum([]) == 20.0,
            "Empty score series must use the default ±20 axis.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 19.9, null)]) == 20.0,
            "Scores within ±20 must retain the default axis.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 21.0, null)]) == 30.0,
            "A score of 21 must expand the axis to ±30.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.White, -47.0, null)]) == 50.0,
            "A score of -47 must expand the axis to ±50.");
        Require(
            MoveTrendScoreAxis.CalculateMaximum(
                [new MoveTrendPoint(1, GoStone.Black, 50.0, null)]) == 60.0,
            "An exact outer tick must receive headroom.");
    }

    private static void VerifyCommentNavigation()
    {
        var moves = new[]
        {
            new GoGameMove(GoStone.Black, null, "first"),
            new GoGameMove(GoStone.White, null, ""),
            new GoGameMove(GoStone.Black, null, "second"),
            new GoGameMove(GoStone.White, null, "third"),
        };

        Require(MoveCommentNavigator.Count(moves) == 3, "Comment count must ignore empty comments.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 1) == 1, "First comment ordinal is incorrect.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 2) == 0, "Uncommented move must not have an ordinal.");
        Require(MoveCommentNavigator.GetOrdinal(moves, 4) == 3, "Last comment ordinal is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 0, 1) == 1,
            "Next comment from the beginning must select the first comment.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 1, 1) == 3,
            "Next comment navigation is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 4, -1) == 3,
            "Previous comment navigation is incorrect.");
        Require(
            MoveCommentNavigator.FindAdjacent(moves, 4, 1) is null,
            "Comment navigation must not wrap at the end.");
    }

    private static void VerifyCatalogOrderEditor()
    {
        var source = new[] { "A", "B", "C", "D", "E", "F", "G" };
        var editor = new CatalogOrderEditor<string>();
        editor.Open(source, 6, 3);
        Require(editor.PagePairIndex == 1, "Order editor must open the page pair containing the selection.");

        editor.MoveSelected(-3);
        Require(editor.SelectedIndex == 3, "Page move must update the selected index.");
        Require(editor.Items.SequenceEqual(["A", "B", "C", "G", "D", "E", "F"]), "Page move produced the wrong order.");

        editor.BeginDrag(3);
        editor.DragTo(1);
        editor.EndDrag();
        Require(editor.Items.SequenceEqual(["A", "G", "B", "C", "D", "E", "F"]), "Drag move produced the wrong order.");

        editor.MoveSelectedToTop();
        Require(editor.Items.SequenceEqual(["G", "A", "B", "C", "D", "E", "F"]), "Move-to-top produced the wrong order.");
        Require(editor.Commit().SequenceEqual(["G", "A", "B", "C", "D", "E", "F"]), "Commit must return the edited order.");

        editor.Open(source, 0, 3);
        editor.MoveSelected(1);
        editor.Cancel();
        Require(source.SequenceEqual(["A", "B", "C", "D", "E", "F", "G"]), "Cancel must not mutate the source list.");
    }

    private static void VerifyCgosConnectionOrder()
    {
        var session = new GoAppSession();
        session.SetCgosConnectionProfiles(
        [
            new CgosConnectionProfile("A", "a.example", 6809, "", ""),
            new CgosConnectionProfile("B", "b.example", 6809, "", ""),
            new CgosConnectionProfile("C", "c.example", 6809, "", ""),
        ]);
        session.SelectCgosConnectionProfile(1);
        session.OpenCgosConnectionOrderEditor();
        session.CgosConnectionOrderEditor.MoveSelected(-1);
        var ordered = session.CommitCgosConnectionOrderEditor();

        Require(ordered.Select(profile => profile.DisplayName).SequenceEqual(["B", "A", "C"]), "CGOS connection order was not committed.");
        Require(session.SelectedCgosConnectionProfile.DisplayName == "B", "CGOS selection must follow the same profile after reordering.");
    }

    private static void VerifyOptionalCgosInputs()
    {
        var session = new GoAppSession();
        Require(!session.IsCgosPlayer2InputEnabled, "CGOS Player 2 input must be disabled initially.");
        Require(!session.IsCgosAdminInputEnabled, "CGOS Admin input must be disabled initially.");

        session.ToggleCgosPlayer2Input();
        session.ToggleCgosAdminInput();
        Require(session.IsCgosPlayer2InputEnabled, "CGOS Player 2 input toggle did not enable the panel.");
        Require(session.IsCgosAdminInputEnabled, "CGOS Admin input toggle did not enable the panel.");
    }

    private static void VerifyTextBoxEditing()
    {
        var clipboard = new TestClipboardService();
        var controller = new TextBoxController(20);
        var frame = new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16));

        controller.Begin("abcd", 1);
        controller.BeginMouseSelection(1, false);
        controller.UpdateMouseSelection(3);
        controller.EndMouseSelection();
        Require(controller.SelectionStart == 1 && controller.SelectionLength == 2, "Mouse selection range is incorrect.");
        controller.TryInputCharacter('X');
        Require(controller.Text == "aXd", "Typing must replace the selected text.");

        controller.Begin("abcd", 2);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftShift, Keys.Right),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.SelectionStart == 2 && controller.SelectionLength == 1, "Shift+Right must extend the selection.");

        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.A),
            new KeyboardState(),
            frame,
            clipboard);
        controller.HandleKeyboard(new KeyboardState(Keys.Back), new KeyboardState(), frame, clipboard);
        Require(controller.Text == "", "Backspace must delete the complete selection.");

        clipboard.Text = "paste";
        controller.Begin("ab", 1);
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.V),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "apasteb", "Ctrl+V must insert clipboard text at the caret.");

        controller.BeginMouseSelection(1, false);
        controller.UpdateMouseSelection(6);
        controller.EndMouseSelection();
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.C),
            new KeyboardState(),
            frame,
            clipboard);
        Require(clipboard.Text == "paste", "Ctrl+C must copy the selected text.");

        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.X),
            new KeyboardState(),
            frame,
            clipboard,
            allowClipboardExport: false);
        Require(controller.Text == "apasteb", "Password-style Ctrl+X must not copy or delete text.");

        controller.Begin("abc", 3);
        controller.TryInputCharacter('d');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abc" && controller.CaretIndex == 3, "Ctrl+Z must undo the previous text edit.");
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abcd" && controller.CaretIndex == 4, "Ctrl+Y must redo the previous text edit.");

        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Z),
            new KeyboardState(),
            frame,
            clipboard);
        controller.TryInputCharacter('X');
        controller.HandleKeyboard(
            new KeyboardState(Keys.LeftControl, Keys.Y),
            new KeyboardState(),
            frame,
            clipboard);
        Require(controller.Text == "abcX", "A new edit must discard the redo history.");
    }

    private static void VerifyDefaultCgosConnection()
    {
        Require(
            File.Exists(ReleaseDefaultSettings.FilePath),
            $"Release default settings file was not found: {ReleaseDefaultSettings.FilePath}");
        var settings = new ApplicationSettings();
        Require(
            settings.TournamentRules.Count == 3,
            "Default tournament rules must be loaded from default-settings.json.");
        Require(
            ReleaseDefaultSettings.Current.SchemaVersion == ReleaseDefaultSettings.CurrentSchemaVersion,
            "Release default settings schema version is incorrect.");
        Require(
            ReleaseDefaultSettings.Current.EngineSettings.GtpEngines.Count == 1,
            "Release default settings must contain one editable GTP engine example.");
        Require(settings.CgosConnections.Count > 0, "Default settings must contain a CGOS connection.");
        var profile = settings.CgosConnections[0];
        Require(profile.DisplayName == "Yamashita CGOS Server", "Yamashita CGOS Server must be the first default connection.");
        Require(profile.Host == "yss-aya.com" && profile.Port == 6809, "Yamashita CGOS Server endpoint is incorrect.");
    }

    private static void VerifyTournamentRulesJsonCompatibility()
    {
        const string legacyJson =
            """
            {
              "DisplayName": "Legacy",
              "Rule": "chinese",
              "MainTimeMinutes": 61,
              "MainTimeSeconds": 2,
              "MoveLimit": 400
            }
            """;
        var legacy = JsonSerializer.Deserialize<TournamentRules>(legacyJson) ??
            throw new InvalidOperationException("Legacy tournament rules JSON must deserialize.");
        Require(legacy.Rule == GoRuleKind.Chinese, "Lower-case legacy rule name must remain compatible.");
        Require(legacy.MainTime == TimeSpan.FromSeconds(3662), "Legacy minute/second time must remain compatible.");

        const string currentJson =
            """
            {
              "DisplayName": "Current",
              "Rule": "Japanese",
              "TimeControl": {
                "Main": "999:59:59"
              },
              "MoveLimit": 9999
            }
            """;
        var current = JsonSerializer.Deserialize<TournamentRules>(currentJson) ??
            throw new InvalidOperationException("Current tournament rules JSON must deserialize.");
        Require((int)current.MainTime.TotalHours == 999, "Current main time must accept 999 hours.");
        Require(!string.IsNullOrWhiteSpace(current.Id), "A missing tournament rule ID must be generated.");

        var serialized = JsonSerializer.Serialize(current);
        Require(serialized.Contains("\"Rule\":\"Japanese\"", StringComparison.Ordinal), "Rule names must serialize in PascalCase.");
        Require(
            serialized.Contains("\"TimeControl\":{\"Main\":\"999:59:59\"}", StringComparison.Ordinal),
            "Main time must serialize as TimeControl.Main.");
        Require(!serialized.Contains("MainTimeMinutes", StringComparison.Ordinal), "Legacy minute fields must not be written.");
    }

    private static Game1 CreateGame()
    {
        var platform = new PortablePlatformServices();
        return new Game1(
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform,
            platform);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private sealed class TestClipboardService : IClipboardService
    {
        public string Text { get; set; } = "";

        public bool TrySetText(string text)
        {
            Text = text;
            return true;
        }

        public bool TryGetText(out string text)
        {
            text = Text;
            return true;
        }
    }
}
