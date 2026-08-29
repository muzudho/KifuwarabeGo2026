namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.GameOasis.Gui.Sgf;

internal static class FormalAdapterBaselineChecks
{
    public static void Run()
    {
        VerifyGtpBaseline();
        VerifyCgosBaseline();
        VerifySgfBaseline();
    }

    private static void VerifyGtpBaseline()
    {
        var baseline = ReadJson<GtpBaseline>("gtp-baseline.json");
        foreach (var testCase in baseline.FilePathCases)
        {
            Require(Enum.TryParse<GtpFilePathArgumentStyle>(testCase.Style, out var style), $"Unknown GTP path style: {testCase.Style}");
            Require(GtpCommandArgument.FormatFilePath(testCase.Path, style) == testCase.Expected,
                $"GTP path formatting changed for {testCase.Path}.");
        }

        foreach (var path in baseline.RejectedFilePaths)
        {
            RequireThrows<ArgumentException>(() => GtpCommandArgument.FormatFilePath(path, GtpFilePathArgumentStyle.Auto),
                $"Unsafe GTP path was accepted: {path}");
        }

        foreach (var testCase in baseline.VertexCases)
        {
            if (testCase.IsPass)
            {
                Require(GtpCoordinate.IsPass(testCase.Vertex), $"GTP pass spelling was not recognized: {testCase.Vertex}");
                continue;
            }

            Require(GtpCoordinate.TryParseVertex(testCase.Vertex, testCase.BoardSize, out var point),
                $"GTP vertex was rejected: {testCase.Vertex}");
            Require(point.X == testCase.X && point.Y == testCase.Y,
                $"GTP vertex coordinates changed: {testCase.Vertex}");
            Require(GtpCoordinate.FormatVertex(point, testCase.BoardSize) == testCase.Vertex,
                $"GTP vertex round-trip changed: {testCase.Vertex}");
        }

        foreach (var testCase in baseline.RejectedVertices)
        {
            Require(!GtpCoordinate.TryParseVertex(testCase.Vertex, testCase.BoardSize, out _),
                $"Invalid GTP vertex was accepted: {testCase.Vertex}");
        }
    }

    private static void VerifyCgosBaseline()
    {
        var baseline = ReadJson<CgosBaseline>("cgos-baseline.json");
        Require(baseline.LoginTranscript.All(line => !line.Contains("password-secret", StringComparison.Ordinal)),
            "CGOS baseline must not contain a real password.");
        Require(baseline.StdinCommands.SequenceEqual(["move 42 A9", "resign 42", "quit"]),
            "CGOS human input, resignation, or exit baseline changed.");

        var help = RunCgosHostHelp();
        foreach (var fragment in baseline.RequiredHelpFragments)
        {
            Require(help.Contains(fragment, StringComparison.Ordinal), $"CGOS --help no longer contains {fragment}.");
        }

        var observation = new CgosGameObservation();
        foreach (var line in baseline.GameLogLines) observation.ProcessLogLine(line);

        var expected = baseline.Expected;
        Require(observation.IsStarted && observation.IsFinished, "CGOS setup/gameover lifecycle changed.");
        Require(observation.GameId == expected.GameId && observation.BoardSize == expected.BoardSize,
            "CGOS game identity or board size changed.");
        Require(observation.Komi == expected.Komi && observation.MainTime == TimeSpan.FromMilliseconds(expected.MainTimeMilliseconds),
            "CGOS komi or main time changed.");
        Require(observation.BlackPlayerName == expected.BlackPlayerName && observation.WhitePlayerName == expected.WhitePlayerName,
            "CGOS player-name normalization changed.");
        Require(observation.MoveCount == expected.MoveCount && observation.Result == expected.Result,
            "CGOS move count or game result changed.");
        Require(observation.Moves.Count == 2 && !observation.Moves[0].IsPass && observation.Moves[1].IsPass == expected.SecondMoveIsPass,
            "CGOS generated/play move interpretation changed.");
        Require(GtpCoordinate.FormatVertex(observation.Moves[0].Point!.Value, observation.BoardSize) == expected.FirstMoveVertex,
            "CGOS first move vertex changed.");
        Require(observation.Moves[0].Comment == expected.FirstMoveComment && observation.Moves[0].Analysis?.Visits == expected.FirstMoveVisits,
            "CGOS analysis JSON interpretation changed.");
    }

    private static void VerifySgfBaseline()
    {
        var record = SgfGameRecordConverter.FromSgf(File.ReadAllText(VectorPath("sgf-baseline.sgf")));
        Require(record.RuleName == "Japanese" && record.BoardSize == 9 && record.Komi == 6.5m,
            "SGF rule, board size, or komi changed.");
        Require(record.TimeLimit == TimeSpan.FromSeconds(600) && record.GameName == "Baseline game",
            "SGF time or game name changed.");
        Require(record.BlackPlayerName == "BlackBot" && record.WhitePlayerName == "WhiteBot" &&
                record.BlackRank == "2d" && record.WhiteRank == "1d",
            "SGF player metadata changed.");
        Require(record.PlayedDate == "2026-08-29" && record.Place == "Tokyo" && record.Result == "W+R",
            "SGF date, place, or result changed.");
        Require(record.SetupStones.Count == 3 && record.RootComment == "root] comment",
            "SGF setup stones or escaped root comment changed.");
        Require(record.Moves.Count == 2 && !record.Moves[0].IsPass && record.Moves[1].IsPass,
            "SGF play/pass sequence changed.");
        Require(record.Moves[0].TimeLeftAfterMove == TimeSpan.FromSeconds(590) &&
                record.Moves[1].TimeLeftAfterMove == TimeSpan.FromSeconds(580),
            "SGF BL/WL interpretation changed.");
        Require(record.Moves[0].Comment == "first move" && record.Moves[0].Analysis?.Visits == 123 &&
                record.Moves[1].Comment == "pass move",
            "SGF comment or analysis interpretation changed.");

        var roundTrip = SgfGameRecordConverter.FromSgf(SgfGameRecordConverter.ToSgf(record));
        Require(roundTrip.SetupStones.SequenceEqual(record.SetupStones) && roundTrip.Moves.Count == record.Moves.Count,
            "SGF representative document no longer survives the current model round-trip.");
        Require(roundTrip.Moves[0].CommonAnalysisJson == record.Moves[0].CommonAnalysisJson,
            "SGF CC analysis JSON was not retained verbatim.");

        var upgraded = SgfGameRecordConverter.UpgradeToCurrentFormat(File.ReadAllText(VectorPath("sgf-legacy-kfa.sgf")));
        Require(upgraded.Contains("KFW[{\"future\":\"preserve-me\"}]", StringComparison.Ordinal) &&
                !upgraded.Contains("KFA[", StringComparison.Ordinal),
            "Legacy SGF KFA-to-KFW update changed or lost unknown JSON.");
    }

    private static string RunCgosHostHelp()
    {
        var repositoryRoot = FindRepositoryRoot();
        var hostDll = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Reference.Communication.Cgos.Host", "bin", "Release", "net8.0", "KifuwarabeGo2026.Reference.Communication.Cgos.Host.dll");
        Require(File.Exists(hostDll), "CGOS Host Release DLL was not built for the baseline test.");

        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            ArgumentList = { hostDll, "--help" },
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }) ?? throw new InvalidOperationException("Could not start CGOS Host --help.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit(10_000);
        Require(process.HasExited && process.ExitCode == 0, "CGOS Host --help failed: " + error);
        return output;
    }

    private static T ReadJson<T>(string fileName) where T : class =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(VectorPath(fileName)), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException($"Could not deserialize {fileName}.");

    private static string VectorPath(string fileName) => Path.Combine(AppContext.BaseDirectory, "Vectors", fileName);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void RequireThrows<TException>(Action action, string message) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException(message);
    }

    private sealed record GtpBaseline(IReadOnlyList<GtpFilePathCase> FilePathCases, IReadOnlyList<string> RejectedFilePaths, IReadOnlyList<GtpVertexCase> VertexCases, IReadOnlyList<GtpRejectedVertex> RejectedVertices);
    private sealed record GtpFilePathCase(string Path, string Style, string Expected);
    private sealed record GtpVertexCase(int BoardSize, string Vertex, int? X, int? Y, bool IsPass);
    private sealed record GtpRejectedVertex(int BoardSize, string Vertex);
    private sealed record CgosBaseline(IReadOnlyList<string> RequiredHelpFragments, IReadOnlyList<string> LoginTranscript, IReadOnlyList<string> GameLogLines, IReadOnlyList<string> StdinCommands, CgosExpected Expected);
    private sealed record CgosExpected(int GameId, int BoardSize, decimal Komi, long MainTimeMilliseconds, string BlackPlayerName, string WhitePlayerName, int MoveCount, string Result, string FirstMoveVertex, string FirstMoveComment, long FirstMoveVisits, bool SecondMoveIsPass);
}
