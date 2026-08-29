using System.Text.Json;
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

Console.WriteLine("PASS: CGOS protocol messages and commands parsed and formatted login, setup, play, genmove, gameover, errors, admin, analysis, and sensitive data.");

static T RequireType<T>(object value) where T : class => value as T ?? throw new InvalidOperationException($"Expected {typeof(T).Name}.");
static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static void RequireThrows<T>(Action action, string message) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new InvalidOperationException(message);
}
