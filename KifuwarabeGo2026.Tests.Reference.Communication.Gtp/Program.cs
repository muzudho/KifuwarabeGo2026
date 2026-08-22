using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;
using KifuwarabeGo2026.Reference.Communication.Gtp;
using KifuwarabeGo2026.Reference.PlaySpace.Go;

if (args is ["--fake-gtp"])
{
    await RunFakeGtpAsync();
    return;
}

await VerifyProcessTransportAsync();

var concierge = new GameOasisConcierge();
var registeredGo = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol()));
var opened = RequireSuccess(await concierge.OpenSessionAsync(
    registeredGo.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        GoSchemas.Configuration,
        """
        {
          "version":1,
          "boardSize":9,
          "komi":6.5,
          "ruleset":"chinese-area",
          "startingPlayer":"black",
          "setupStones":[
            {"x":0,"y":0,"color":"black"},
            {"x":1,"y":0,"color":"white"}
          ]
        }
        """)));

var transport = new RecordingGtpTransport(["A9", "D4"]);
var gtpPlayer = new KifuwarabeGtpPlayerProtocol(
    transport,
    new PlayerEngineId("org.kifuwarabe.tests.gtp-player"));
var players = new GameOasisPlayerCoordinator(concierge);
var registeredPlayer = RequireSuccess(await players.RegisterPlayerAsync(gtpPlayer));
var bound = RequireSuccess(await players.BindPlayerAsync(
    registeredPlayer.Descriptor.EngineId,
    opened.SessionId,
    "black"));

RequireSequence(
    transport.Commands.Take(8),
    [
        "known_command kfw-begin-position",
        "boardsize 9",
        "komi 6.5",
        "kfw-begin-position",
        "kfw-add-black A9",
        "kfw-add-white B9",
        "kfw-set-to-play black",
        "kfw-commit-position",
    ],
    "The initial observation must be synchronized atomically.");

var genericTransport = new GenericRecordingGtpTransport();
var genericPlayer = new KifuwarabeGtpPlayerProtocol(
    genericTransport,
    new PlayerEngineId("org.kifuwarabe.tests.generic-gtp-player"));
var genericRegistered = RequireSuccess(await players.RegisterPlayerAsync(genericPlayer));
var genericBound = RequireSuccess(await players.BindPlayerAsync(
    genericRegistered.Descriptor.EngineId,
    opened.SessionId,
    "white"));
RequireSequence(
    genericTransport.Commands,
    [
        "known_command kfw-begin-position",
        "known_command fixed_handicap",
        "known_command set_free_handicap",
        "boardsize 9",
        "clear_board",
        "komi 6.5",
        "play black A9",
        "play white B9",
    ],
    "A generic GTP engine must receive the standard sequential setup fallback.");
RequireSuccess(await players.UnbindPlayerAsync(genericBound.BindingId, "generic-fallback-verified"));

genericTransport.Commands.Clear();
var handicapSession = RequireSuccess(await concierge.OpenSessionAsync(
    registeredGo.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        GoSchemas.Configuration,
        """
        {
          "version":1,
          "boardSize":9,
          "komi":0.5,
          "ruleset":"chinese-area",
          "startingPlayer":"white",
          "setupStones":[
            {"x":2,"y":2,"color":"black"},
            {"x":6,"y":6,"color":"black"}
          ]
        }
        """)));
var handicapBound = RequireSuccess(await players.BindPlayerAsync(
    genericRegistered.Descriptor.EngineId,
    handicapSession.SessionId,
    "white"));
RequireSequence(
    genericTransport.Commands,
    ["boardsize 9", "clear_board", "komi 0.5", "set_free_handicap C7 G3"],
    "A supported set_free_handicap command must be preferred for a black-only handicap position.");
RequireSuccess(await players.UnbindPlayerAsync(handicapBound.BindingId, "set-free-handicap-verified"));
RequireSuccess(await concierge.CloseSessionAsync(handicapSession.SessionId));

genericTransport.Commands.Clear();
var fixedHandicapSession = RequireSuccess(await concierge.OpenSessionAsync(
    registeredGo.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        GoSchemas.Configuration,
        """
        {
          "version":1,
          "boardSize":9,
          "komi":0.5,
          "ruleset":"chinese-area",
          "startingPlayer":"white",
          "setupStones":[
            {"x":2,"y":6,"color":"black"},
            {"x":6,"y":2,"color":"black"}
          ]
        }
        """)));
var fixedHandicapBound = RequireSuccess(await players.BindPlayerAsync(
    genericRegistered.Descriptor.EngineId,
    fixedHandicapSession.SessionId,
    "white"));
RequireSequence(
    genericTransport.Commands,
    ["boardsize 9", "clear_board", "komi 0.5", "fixed_handicap 2"],
    "A standard star-point setup must prefer fixed_handicap.");
RequireSuccess(await players.UnbindPlayerAsync(fixedHandicapBound.BindingId, "fixed-handicap-verified"));
RequireSuccess(await concierge.CloseSessionAsync(fixedHandicapSession.SessionId));

genericTransport.Commands.Clear();
genericTransport.ReturnMismatchedFixedHandicap = true;
var mismatchedSession = RequireSuccess(await concierge.OpenSessionAsync(
    registeredGo.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        GoSchemas.Configuration,
        """
        {"version":1,"boardSize":9,"komi":0.5,"ruleset":"chinese-area","startingPlayer":"white",
         "setupStones":[{"x":2,"y":6,"color":"black"},{"x":6,"y":2,"color":"black"}]}
        """)));
var mismatched = await players.BindPlayerAsync(
    genericRegistered.Descriptor.EngineId,
    mismatchedSession.SessionId,
    "white");
Require(!mismatched.IsSuccess && mismatched.Error?.Code == "gtp-fixed-handicap-mismatch", "A mismatched fixed_handicap response must reject the binding.");
Require(genericTransport.Commands.Last() == "clear_board", "A mismatched fixed_handicap response must clear the engine board.");
RequireSuccess(await concierge.CloseSessionAsync(mismatchedSession.SessionId));

var rejected = RequireSuccess(await players.RequestAndApplyActionAsync(bound.BindingId));
Require(!rejected.Applied.IsAccepted, "The occupied A9 generated move must be rejected by the play-space.");
Require(transport.Commands.Count(command => command == "kfw-begin-position") == 2, "A rejected generated move must trigger full position resynchronization.");

var accepted = RequireSuccess(await players.RequestAndApplyActionAsync(bound.BindingId));
Require(accepted.Applied.IsAccepted, "The second generated move D4 must be accepted.");
Require(transport.Commands.Count(command => command.StartsWith("play black", StringComparison.Ordinal)) == 0, "An accepted genmove must not be echoed back as play.");

var whiteAction = new ContractDocument(
    "application/json",
    GoSchemas.Action,
    """{"version":1,"type":"play","player":"white","x":4,"y":4}""");
var whiteApplied = RequireSuccess(await concierge.ApplyActionAsync(new(
    opened.SessionId,
    whiteAction,
    accepted.Applied.Snapshot.Revision)));
Require(whiteApplied.IsAccepted, "The simulated opponent move must be accepted.");
var notified = RequireSuccess(await gtpPlayer.NotifyActionAsync(new(
    bound.BindingId,
    whiteAction,
    true,
    ToObservation(whiteApplied.Snapshot),
    whiteApplied.Events,
    null)));
Require(notified.Revision == whiteApplied.Snapshot.Revision, "The GTP adapter must acknowledge the opponent revision.");
Require(transport.Commands.Last() == "play white E5", "The opponent move must be converted to a standard GTP play command.");

RequireSuccess(await players.UnbindPlayerAsync(bound.BindingId, "smoke-test-complete"));
RequireSuccess(await concierge.CloseSessionAsync(opened.SessionId));
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(registeredGo.Descriptor.TypeId));

Console.WriteLine("PASS: Protocol P synchronized a Kifuwarabe GTP engine, recovered from a rejected genmove, and relayed the opponent move.");
return;

static async Task VerifyProcessTransportAsync()
{
    var assemblyPath = typeof(RecordingGtpTransport).Assembly.Location;
    await using var transport = ProcessGtpCommandTransport.Start(new("dotnet", [assemblyPath, "--fake-gtp"]));
    var name = await transport.SendAsync("name");
    Require(name == new GtpCommandResponse(true, "Kifuwarabe Fake GTP"), "The process transport must parse a successful response.");
    var list = await transport.SendAsync("list_commands");
    Require(list.IsSuccess && list.Payload == "name\nlist_commands\nknown_command", "The process transport must preserve a multiline payload.");
    var unknown = await transport.SendAsync("unknown");
    Require(!unknown.IsSuccess && unknown.Payload == "unknown command", "The process transport must parse an error response.");
}

static async Task RunFakeGtpAsync()
{
    while (await Console.In.ReadLineAsync() is { } command)
    {
        var response = command switch
        {
            "name" => "= Kifuwarabe Fake GTP",
            "list_commands" => "= name\nlist_commands\nknown_command",
            "quit" => "=",
            _ => "? unknown command",
        };
        await Console.Out.WriteLineAsync(response);
        await Console.Out.WriteLineAsync();
        await Console.Out.FlushAsync();
        if (command == "quit") return;
    }
}

static PlayerGameObservation ToObservation(GameOasisSnapshot snapshot) => new(
    snapshot.SessionId,
    snapshot.PlaySpaceTypeId,
    snapshot.Revision,
    snapshot.OperationRevision,
    snapshot.OperationalState,
    snapshot.State,
    snapshot.IsTerminal,
    snapshot.Outcome);

static void RequireSequence(IEnumerable<string> actual, IEnumerable<string> expected, string message)
{
    if (!actual.SequenceEqual(expected))
        throw new InvalidOperationException($"{message} Actual: {string.Join(" | ", actual)}");
}

static T RequireSuccess<T>(ProtocolResponse<T> response)
{
    if (!response.IsSuccess || response.Value is null)
        throw new InvalidOperationException($"Expected success: {response.Error?.Code} {response.Error?.Message}");
    return response.Value;
}

static void Require(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

internal sealed class RecordingGtpTransport(IEnumerable<string> generatedMoves) : IGtpCommandTransport
{
    private readonly Queue<string> _generatedMoves = new(generatedMoves);
    public List<string> Commands { get; } = [];

    public ValueTask<GtpCommandResponse> SendAsync(string command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commands.Add(command);
        return ValueTask.FromResult(command == "known_command kfw-begin-position"
            ? new GtpCommandResponse(true, "true")
            : command.StartsWith("genmove ", StringComparison.Ordinal)
            ? new GtpCommandResponse(true, _generatedMoves.Dequeue())
            : new GtpCommandResponse(true, string.Empty));
    }
}

internal sealed class GenericRecordingGtpTransport : IGtpCommandTransport
{
    public List<string> Commands { get; } = [];
    public bool ReturnMismatchedFixedHandicap { get; set; }

    public ValueTask<GtpCommandResponse> SendAsync(string command, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commands.Add(command);
        return ValueTask.FromResult(command.StartsWith("known_command ", StringComparison.Ordinal)
            ? new GtpCommandResponse(true, command is "known_command fixed_handicap" or "known_command set_free_handicap" ? "true" : "false")
            : command == "fixed_handicap 2"
            ? new GtpCommandResponse(true, ReturnMismatchedFixedHandicap ? "C3 C7" : "C3 G7")
            : new GtpCommandResponse(true, string.Empty));
    }
}
