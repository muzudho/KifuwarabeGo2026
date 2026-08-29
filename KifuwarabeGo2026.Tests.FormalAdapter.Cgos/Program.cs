using System.Text.Json;
using System.Net;
using System.Net.Sockets;
using System.Text;
using KifuwarabeGo2026.FormalAdapter.Cgos.Client;
using KifuwarabeGo2026.FormalAdapter.Cgos.GameMasterEngine;
using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;
using KifuwarabeGo2026.FormalAdapter.Cgos.Go;
using KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility;
using KifuwarabeGo2026.FormalAdapter.Cgos.PlayerEngine;
using KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

var protocol = RequireType<CgosProtocolAdvertised>(CgosServerMessageParser.Parse("protocol genmove_analyze"));
Require(protocol.SupportsGenMoveAnalyze, "The advertised analysis extension must be detected.");
Require(CgosServerMessageParser.Parse("username") is CgosUsernameRequested, "Username prompts must be typed.");
Require(CgosServerMessageParser.Parse("password") is CgosPasswordRequested, "Password prompts must be typed.");
Require(CgosServerMessageParser.Parse("ok") is CgosLoginAccepted, "Login acceptance must be typed.");

var setup = RequireType<CgosMatchSetup>(CgosServerMessageParser.Parse(
    "setup 42 9 6.5 600000 WhiteBot(1d) BlackBot(2d) A9 590000 pass 580000"));
Require(setup.GameId == 42 && setup.BoardSize == 9 && setup.Komi == 6.5m && setup.MainTimeMilliseconds == 600000,
    "Setup numeric fields must be parsed invariantly.");
Require(setup.WhitePlayer == "WhiteBot" && setup.BlackPlayer == "BlackBot", "Ranks must be separated from player names.");
Require(setup.MoveHistory.SequenceEqual([
    new CgosHistoricalMove("b", "A9", 590000),
    new CgosHistoricalMove("w", "pass", 580000)]),
    "Setup history must restore alternating colors.");

var play = RequireType<CgosMovePlayed>(CgosServerMessageParser.Parse("play w pass 580000"));
Require(play.Color == "w" && play.Vertex == "pass" && play.TimeLeftMilliseconds == 580000, "Play must be typed.");
var genmove = RequireType<CgosGenMoveRequested>(CgosServerMessageParser.Parse("genmove b 570000"));
Require(genmove.Color == "b" && genmove.TimeLeftMilliseconds == 570000, "Genmove must be typed.");
Require(RequireType<CgosGameOver>(CgosServerMessageParser.Parse("gameover W+R")).Result == "W+R", "Gameover must be typed.");
Require(RequireType<CgosServerError>(CgosServerMessageParser.Parse("Error: denied")).Message == "denied", "Errors must be typed.");
Require(RequireType<CgosUnknownServerMessage>(CgosServerMessageParser.Parse("future one two")).Arguments.SequenceEqual(["one", "two"]),
    "Unknown commands must retain their arguments.");

Require(CgosClientCommandFormatter.Format(new CgosClientIdentity("e1", true)) == "e1 genmove_analyze", "Identity must advertise analysis.");
Require(CgosClientCommandFormatter.Format(new CgosMove("a9", "{\"moves\":[]}")) == "a9 {\"moves\":[]}", "Analyzed moves must format.");
Require(CgosClientCommandFormatter.Format(new CgosResign()) == "resign" &&
        CgosClientCommandFormatter.Format(new CgosReady()) == "ready" &&
        CgosClientCommandFormatter.Format(new CgosQuit()) == "quit", "Lifecycle commands must format.");
Require(CgosClientCommandFormatter.Format(new CgosMatch("white black")) == "match white black", "Admin match must format.");
var password = new CgosPassword("secret");
Require(CgosClientCommandFormatter.Format(password) == "secret" && CgosClientCommandFormatter.FormatForLog(password) == "(password)",
    "Passwords must format for transport but remain masked in logs.");
RequireThrows<ArgumentException>(() => CgosClientCommandFormatter.Format(new CgosMove("a9\nquit")), "Line injection must be rejected.");
RequireThrows<CgosProtocolException>(() => CgosServerMessageParser.Parse("play x A1 100"), "Invalid colors must be rejected.");
RequireThrows<CgosProtocolException>(() => CgosServerMessageParser.Parse("setup 1 9 bad 100 W B"), "Invalid komi must be rejected.");

var admin = new CgosAdminStateMachine();
Require(!admin.TryCreateCommand("who", out _), "Admin commands must not be accepted before login.");
Require(admin.Handle(new CgosLoginAccepted("ok")) && admin.IsReady, "Admin login acceptance must make command input ready once.");
Require(admin.TryCreateCommand("who", out var who) && who is CgosWho, "Admin who input must become a typed command.");
Require(admin.TryCreateCommand("match white black", out var match) && match is CgosMatch { Arguments: "white black" },
    "Admin match input must retain its arguments.");
Require(admin.TryCreateCommand("quit", out var quit) && quit is CgosQuit && !admin.TryCreateCommand("future", out _),
    "Admin quit must be typed and unsupported input rejected.");

var setupNotificationLine = CgosNotificationJsonLines.Format(new CgosSetupNotification(
    "black", 42, 9, 6.5m, 600000, "WhiteBot", "BlackBot",
    [new CgosHistoricalMove("b", "A9", 590000)]));
Require(CgosNotificationJsonLines.TryParse(setupNotificationLine, out var setupNotice) &&
        setupNotice is CgosSetupNotification { GameId: 42, BoardSize: 9, MoveHistory.Count: 1 },
    "Versioned setup notifications must round-trip with history.");
var playNotificationLine = CgosNotificationJsonLines.Format(
    new CgosPlayNotification("black", "w", "pass", 580000, "{\"moves\":[]}"));
Require(CgosNotificationJsonLines.TryParse("prefix " + playNotificationLine, out var playNotice) &&
        playNotice is CgosPlayNotification { Vertex: "pass", TimeLeftMilliseconds: 580000, AnalysisJson: "{\"moves\":[]}" },
    "Versioned play notifications must parse through a display prefix.");
var runtimeNotificationLine = CgosNotificationJsonLines.Format(
    new CgosRuntimeNotification("black", CgosRuntimeState.GtpWait, "genmove b"));
Require(CgosNotificationJsonLines.TryParse(runtimeNotificationLine, out var runtimeNotice) &&
        runtimeNotice is CgosRuntimeNotification { State: CgosRuntimeState.GtpWait, Detail: "genmove b" },
    "Versioned runtime notifications must retain state and diagnostic detail.");
Require(!CgosNotificationJsonLines.TryParse("@kifuwarabe-cgos-v1 {bad", out _) &&
        !CgosNotificationJsonLines.TryParse("ordinary human log", out _),
    "Malformed and human-readable lines must not become machine notifications.");

var goProjector = new CgosGoEventProjector();
Require(goProjector.TryProject((CgosSetupNotification)setupNotice!, out var projectedSetup) &&
        projectedSetup is CgosGoSetup { BoardSize: 9, MoveHistory.Count: 1 } goSetup &&
        goSetup.MoveHistory[0].Vertex is { X: 0, Y: 0 },
    "CGOS setup notifications must project history into neutral Go coordinates.");
Require(goProjector.TryProject(new CgosPlayNotification("black", "w", "J1", 580000), out var projectedMove) &&
        projectedMove is CgosGoMove { Color: CgosGoColor.White, Vertex.X: 8, Vertex.Y: 8 },
    "CGOS play notifications must project colors and GTP vertices without GUI types.");
Require(goProjector.TryProject(new CgosPlayNotification("black", "b", "pass", 570000), out var projectedPass) &&
        projectedPass is CgosGoMove { Vertex.IsPass: true } &&
        !goProjector.TryProject(new CgosPlayNotification("black", "x", "A1", 1), out _),
    "CGOS Go projection must preserve pass and reject invalid colors.");
Require(CgosLegacyLogNotificationAdapter.TryParse(
        "2026-01-01 00:00:00.000 [black] > setup 9 9 6.5 600000 White(1d) Black(2d) A9 590000",
        out var legacySetup) && legacySetup is CgosSetupNotification { GameId: 9, MoveHistory.Count: 1 },
    "Legacy Host setup logs must be isolated behind the compatibility adapter.");
Require(CgosLegacyLogNotificationAdapter.TryParse(
        "2026-01-01 00:00:00.000 [black] # Generated black move: J1 {\"moves\":[]}",
        out var legacyGenerated) && legacyGenerated is CgosPlayNotification { Vertex: "J1", IsGenerated: true },
    "Legacy generated-move logs must retain analysis behind the compatibility adapter.");

using (var baseline = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Vectors", "cgos-baseline.json"))))
{
    var transcript = baseline.RootElement.GetProperty("loginTranscript").EnumerateArray().Select(value => value.GetString()!).ToArray();
    Require(CgosServerMessageParser.Parse(transcript[0][2..]) is CgosProtocolAdvertised, "Baseline protocol prompt must parse.");
    Require(CgosClientCommandFormatter.Format(new CgosClientIdentity("e1", true)) == transcript[1][2..], "Baseline identity must format.");
    Require(CgosServerMessageParser.Parse(transcript[2][2..]) is CgosUsernameRequested, "Baseline username prompt must parse.");
    Require(CgosServerMessageParser.Parse(transcript[4][2..]) is CgosPasswordRequested, "Baseline password prompt must parse.");
    Require(CgosServerMessageParser.Parse(transcript[6][2..]) is CgosLoginAccepted, "Baseline login acceptance must parse.");
    foreach (var command in baseline.RootElement.GetProperty("stdinCommands").EnumerateArray().Select(value => value.GetString()!))
        Require(!string.IsNullOrWhiteSpace(command), "Baseline client control commands must remain non-empty.");
}

var listener = new TcpListener(IPAddress.Loopback, 0);
listener.Start();
var port = ((IPEndPoint)listener.LocalEndpoint).Port;
var receivedCommands = new List<string>();
var serverTask = Task.Run(async () =>
{
    using var client = await listener.AcceptTcpClientAsync();
    await using var stream = client.GetStream();
    using var reader = new StreamReader(stream, Encoding.UTF8, false, leaveOpen: true);
    await using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true) { NewLine = "\n", AutoFlush = true };
    foreach (var prompt in new[] { "protocol genmove_analyze", "username", "password" })
    {
        await writer.WriteLineAsync(prompt);
        receivedCommands.Add((await reader.ReadLineAsync())!);
    }
    await writer.WriteLineAsync("ok");
});
var networkMessages = new List<CgosServerMessage>();
var networkLogs = new List<string>();
var networkEvents = new List<CgosNetworkEvent>();
var passwordNotifications = 0;
var networkSession = new CgosNetworkSession(
    new CgosConnectionOptions("127.0.0.1", port, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)),
    new CgosCredentials("baseline-player", "top-secret"),
    networkLogs.Add,
    networkEvents.Add);
await networkSession.RunAsync(
    (message, _) => { networkMessages.Add(message); return Task.CompletedTask; },
    _ => { passwordNotifications++; return Task.CompletedTask; },
    CancellationToken.None);
await serverTask;
listener.Stop();
Require(receivedCommands.SequenceEqual(["e1 genmove_analyze", "baseline-player", "top-secret"]),
    "The network session must perform the typed login exchange.");
Require(networkMessages.Single() is CgosLoginAccepted && networkSession.ServerSupportsAnalyze,
    "The network session must expose post-login messages and advertised capabilities.");
Require(passwordNotifications == 1 && networkLogs.All(line => !line.Contains("top-secret", StringComparison.Ordinal)),
    "The password callback must run and logs must not expose credentials.");
Require(networkEvents.Select(value => value.Kind).SequenceEqual([
        CgosNetworkEventKind.Connecting,
        CgosNetworkEventKind.Connected,
        CgosNetworkEventKind.Protocol,
        CgosNetworkEventKind.Login,
        CgosNetworkEventKind.Ready,
        CgosNetworkEventKind.Closed]),
    "The network session must report its lifecycle without parsing human-readable logs.");

var fakeEngine = new FakePlayerEngine();
await using (var player = new CgosPlayerStateMachine(
    "WhiteBot",
    (_, _) => Task.FromResult<ICgosPlayerEngine>(fakeEngine)))
{
    await player.HandleAsync(setup, serverSupportsAnalyze: true);
    Require(fakeEngine.BoardSize == 9 && fakeEngine.Komi == 6.5m && fakeEngine.Played.Count == 2,
        "The player state machine must configure and replay setup history.");
    await player.HandleAsync(new CgosMovePlayed("play b C3 570000", "b", "C3", 570000), true);
    var generated = await player.HandleAsync(new CgosGenMoveRequested("genmove w 560000", "w", 560000), true);
    Require(generated is CgosMove { Vertex: "d4", AnalysisJson: "{\"moves\":[]}" } && fakeEngine.LastAnalyze,
        "The player state machine must produce analyzed moves when both sides support them.");
    Require(await player.HandleAsync(new CgosGameOver("gameover W+R", "W+R"), true) is CgosReady && fakeEngine.Disposed,
        "Gameover must dispose the engine and return ready.");
}
var resignRequested = true;
await using (var human = new CgosPlayerStateMachine(
    "Human",
    engineFactory: null,
    humanMoveProvider: (_, _, _) => Task.FromResult("A9"),
    consumeResignRequest: _ => resignRequested))
{
    await human.HandleAsync(new CgosMatchSetup("setup", 7, 9, 6.5m, 600000, "Human", "Other", []), false);
    Require(await human.HandleAsync(new CgosGenMoveRequested("genmove b 1", "b", 1), false) is CgosResign,
        "A queued resignation must take precedence over a human move.");
}

Console.WriteLine("PASS: CGOS protocol messages and commands parsed and formatted login, setup, play, genmove, gameover, errors, admin, analysis, and sensitive data.");

static T RequireType<T>(object value) where T : class => value as T ?? throw new InvalidOperationException($"Expected {typeof(T).Name}.");
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static void RequireThrows<T>(Action action, string message) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException(message);
}

sealed class FakePlayerEngine : ICgosPlayerEngine
{
    public bool SupportsAnalyze => true;
    public int BoardSize { get; private set; }
    public decimal Komi { get; private set; }
    public List<(string Color, string Vertex, long Time)> Played { get; } = [];
    public bool LastAnalyze { get; private set; }
    public bool Disposed { get; private set; }
    public Task ConfigureAsync(int boardSize, decimal komi, CancellationToken cancellationToken = default)
    { BoardSize = boardSize; Komi = komi; return Task.CompletedTask; }
    public Task PlayAsync(string color, string vertex, long timeLeftMilliseconds, CancellationToken cancellationToken = default)
    { Played.Add((color, vertex, timeLeftMilliseconds)); return Task.CompletedTask; }
    public Task<CgosGeneratedMove> GenerateMoveAsync(string color, bool includeAnalysis, CancellationToken cancellationToken = default)
    { LastAnalyze = includeAnalysis; return Task.FromResult(new CgosGeneratedMove("D4", includeAnalysis ? "{\"moves\":[]}" : null)); }
    public ValueTask DisposeAsync() { Disposed = true; return ValueTask.CompletedTask; }
}
