namespace KifuwarabeGo2026.Gui.PortabilitySmoke;

using KifuwarabeGo2026.Gui;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;

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
        VerifyDefaultCgosConnection();
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

    private static void VerifyDefaultCgosConnection()
    {
        Require(
            File.Exists(ReleaseDefaultSettings.FilePath),
            $"Release default settings file was not found: {ReleaseDefaultSettings.FilePath}");
        var settings = new ApplicationSettings();
        Require(
            settings.TournamentRules.Count == 2,
            "Default tournament rules must be loaded from default-settings.json.");
        Require(settings.CgosConnections.Count > 0, "Default settings must contain a CGOS connection.");
        var profile = settings.CgosConnections[0];
        Require(profile.DisplayName == "Yamashita CGOS Server", "Yamashita CGOS Server must be the first default connection.");
        Require(profile.Host == "yss-aya.com" && profile.Port == 6809, "Yamashita CGOS Server endpoint is incorrect.");
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
}
