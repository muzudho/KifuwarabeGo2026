using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;
using KifuwarabeGo2026.PlayRoomGui.JsonLines;
using KifuwarabeGo2026.Reference.MatchRunner.Go;

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private const string Game = GameOasisOfficialNames.Go;
    private static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() == "--window") return Window(args[1]);
        if (args.FirstOrDefault() == "--gtp") return Gtp();
        try
        {
            if (args.FirstOrDefault() is "--real-host" or "--real-window")
            {
                var launcher = new GoStdioMatchLauncher(_ =>
                {
                    var start = new ProcessStartInfo("dotnet");
                    start.ArgumentList.Add(Path.GetFullPath(args[1]));
                    start.ArgumentList.Add(args[0] == "--real-window" ? "--stdio" : "--stdio-contract-smoke");
                    return start;
                });
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var completed = await launcher.LaunchAsync(Request(ai: true), cancellationToken: deadline.Token);
                Check(completed.IsNormalExit && completed.Match is { Status: MatchCompletionStatus.Finished, WinnerRoleId: "white" },
                    completed.Message + " " + completed.Diagnostic);
                Console.WriteLine("PASS official runner -> official host -> two GTP players -> scored completion");
                return 0;
            }
            foreach (var scenario in new[] { "passes", "capture", "resign", "close", "stale", "wrong-role", "bad-id", "crash", "no-exit", "ai", "cancel" })
            {
                var launcher = new GoStdioMatchLauncher(_ => Child("--window", scenario));
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(scenario == "cancel" ? 3 : 30));
                var result = await launcher.LaunchAsync(Request(scenario == "ai"), cancellationToken: timeout.Token);
                if (scenario == "cancel")
                    Check(result.Status == PlayRoomProcessCompletionStatus.Cancelled, "cancellation must close the session");
                else if (scenario is "bad-id" or "crash" or "no-exit")
                    Check(!result.IsNormalExit && result.Status != PlayRoomProcessCompletionStatus.Cancelled, scenario + " must fail");
                else if (scenario == "close")
                    Check(result.IsNormalExit && result.Match?.Status == MatchCompletionStatus.Closed, "close result");
                else
                {
                    Check(result.IsNormalExit && result.Match?.Status == MatchCompletionStatus.Finished,
                        scenario + ": " + result.Message + " " + result.Diagnostic);
                    Check(result.Match!.WinnerRoleId == (scenario is "passes" or "capture" ? "black" : "white"), scenario + " winner");
                    Check(result.Match.Reason == (scenario == "resign" ? "resignation" : "two-passes-area-score"), scenario + " reason");
                }
                Console.WriteLine("PASS " + scenario);
            }
            // Run again after failures: no stale session or child process should prevent reopening.
            var repeat = await new GoStdioMatchLauncher(_ => Child("--window", "passes")).LaunchAsync(Request());
            Check(repeat.IsNormalExit, "restart after failure");
            Console.WriteLine("PASS restart after failure");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    private static ProcessStartInfo Child(params string[] args)
    {
        var start = new ProcessStartInfo("dotnet");
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        return start;
    }

    private static PlayRoomLaunchRequest Request(bool ai = false)
    {
        var computer = new ContractDocument("application/json", PlayerConnectionSchemas.GtpProcessV1,
            JsonSerializer.Serialize(new { executablePath = "dotnet", workingDirectory = AppContext.BaseDirectory,
                arguments = "\"" + Assembly.GetExecutingAssembly().Location + "\" --gtp", enableGtpLog = false }, Json));
        return new(1, Guid.NewGuid().ToString("N"), "match", Game, new(Game),
            new("application/json", Game + ".configuration.v1",
                "{\"version\":1,\"boardSize\":9,\"komi\":6.5,\"ruleset\":\"chinese-area\",\"startingPlayer\":\"black\",\"setupStones\":[]}"),
            new("application/x-go-sgf", Game + ".sgf.v1", "(;SZ[9])"),
            [new("black", "b", "黒", ai ? "computer" : "human", "", null, ai ? computer : null),
             new("white", "w", "白", ai ? "computer" : "human", "", null, ai ? computer : null)]);
    }

    // Independent process speaking only the public protocol. Scripted inputs are confined to this test host.
    private static int Window(string scenario)
    {
        const string session = "external-window";
        long revision = -1, eventId = 0;
        int actionIndex = 0;
        ContractDocument? displayed = null;
        string turn = "black";
        try
        {
            while (Console.ReadLine() is { } line)
            {
                var request = JsonSerializer.Deserialize<PlayRoomProcessRequest>(line, Json)!;
                T Read<T>() => request.Parameters!.Value.Deserialize<T>(Json)!;
                object result;
                bool exit = false;
                switch (request.Method)
                {
                    case "describe":
                        result = new { capabilities = new[] { "poll-input-events.v1", "go-display-state.v1" }, roomTypes = new[] { "match" } };
                        break;
                    case "open":
                        var launch = Read<PlayRoomLaunchRequest>();
                        Check(launch.InitialPosition is null && launch.Participants.All(p => p.PlayerConnection is null), "display must be self-contained");
                        result = new PlayRoomReady(launch.RequestId, session, "match");
                        break;
                    case "updateState":
                        var update = Read<MatchStateUpdate>();
                        Check(update.SessionId == session && update.Revision > revision, "display revision");
                        displayed = update.State;
                        revision = update.Revision;
                        using (var doc = JsonDocument.Parse(displayed.Content))
                        {
                            turn = doc.RootElement.GetProperty("currentTurn").GetString()!;
                            if (scenario == "passes" && actionIndex == 2)
                                Check(doc.RootElement.GetProperty("stones").GetArrayLength() == 1 && turn == "white", "occupied move must be rejected");
                            if (scenario == "capture" && actionIndex == 3)
                                Check(doc.RootElement.GetProperty("stones").GetArrayLength() == 2 &&
                                    doc.RootElement.GetProperty("stones").EnumerateArray().All(s => s.GetProperty("color").GetString() == "black"),
                                    "captured white stone must disappear from the projection");
                        }
                        result = new MatchViewState(session, revision, displayed);
                        break;
                    case "readEvents":
                        if (scenario == "crash") return 9;
                        if (scenario == "cancel") Thread.Sleep(20000);
                        if (scenario == "close")
                        {
                            result = new MatchInputEvents(session, [new(++eventId, "closed", revision,
                                Completion: new(session, MatchCompletionStatus.Closed, displayed))], true);
                            exit = true;
                        }
                        else if (scenario == "ai" || actionIndex >= (scenario == "resign" ? 1 : scenario == "passes" ? 4 : scenario == "capture" ? 5 : 3))
                            result = new MatchInputEvents(session, [], false);
                        else
                        {
                            var kind = scenario == "resign" ? MatchActionKind.Resign :
                                (scenario == "passes" && actionIndex < 2 || scenario == "capture" && actionIndex < 3) ? MatchActionKind.PlayPoint : MatchActionKind.Pass;
                            var x = scenario == "capture" ? (actionIndex == 0 ? 1 : 0) : 2;
                            var y = scenario == "capture" ? (actionIndex == 2 ? 1 : 0) : 3;
                            var role = scenario == "wrong-role" && actionIndex == 0 ? "white" : turn;
                            var inputRevision = scenario == "stale" && actionIndex == 0 ? revision - 1 : revision;
                            result = new MatchInputEvents(session, [new(++eventId, "action", inputRevision,
                                new(session, Guid.NewGuid().ToString("N"), role, kind, kind == MatchActionKind.PlayPoint ? x : null,
                                    kind == MatchActionKind.PlayPoint ? y : null))], false);
                            actionIndex++;
                        }
                        break;
                    case "complete":
                        var complete = Read<MatchCompletionCommand>();
                        result = new MatchCompletion(session, MatchCompletionStatus.Finished, complete.FinalState, complete.WinnerRoleId, complete.Reason);
                        exit = true;
                        break;
                    default: throw new Exception("Unexpected method " + request.Method);
                }
                Console.WriteLine(JsonSerializer.Serialize(new PlayRoomProcessResponse(1,
                    scenario == "bad-id" ? "wrong" : request.RequestId, true, JsonSerializer.SerializeToElement(result, Json), null), Json));
                Console.Out.Flush();
                if (exit)
                {
                    if (scenario == "no-exit") Thread.Sleep(20000);
                    return 0;
                }
            }
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 8; }
    }

    private static int Gtp()
    {
        while (Console.ReadLine() is { } line)
        {
            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var id = int.TryParse(tokens[0], out _) ? tokens[0] : "";
            var command = tokens[id.Length > 0 ? 1 : 0];
            var payload = command switch { "genmove" => "pass", "name" => "Test", "version" => "1", "protocol_version" => "2", _ => "" };
            Console.WriteLine($"={id} {payload}\n");
            Console.Out.Flush();
            if (command == "quit") return 0;
        }
        return 0;
    }

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
