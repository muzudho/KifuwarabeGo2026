using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki;

var playSpace = new PonnukiPlaySpaceProtocol();

var descriptor = RequireSuccess(await playSpace.DescribeAsync());
Require(descriptor.ProtocolVersion == ContractVersion.V1_0, "Protocol version must be 1.0.");
Require(descriptor.TypeId.Value == GameOasisOfficialNames.Ponnuki, "Unexpected play-space type ID.");

var invalid = RequireSuccess(await playSpace.ValidateConfigurationAsync(new(
    Configuration("""{"version":1,"boardSize":8,"initialMoveCount":0,"captureTarget":1}"""))));
Require(!invalid.IsValid && invalid.Issues.Any(issue => issue.Code == "invalid-board-size"), "Invalid board size must be reported.");

const string deterministicConfiguration = """
    {"version":1,"boardSize":9,"initialMoveCount":20,"randomSeed":12345,"captureTarget":20}
    """;
var first = RequireSuccess(await playSpace.CreateSessionAsync(new(Configuration(deterministicConfiguration))));
var second = RequireSuccess(await playSpace.CreateSessionAsync(new(Configuration(deterministicConfiguration))));
Require(first.InitialSnapshot.State.Content == second.InitialSnapshot.State.Content, "The same seed must create the same initial state.");

const string captureConfiguration = """
    {
      "version":1,
      "boardSize":9,
      "initialMoveCount":0,
      "randomSeed":7,
      "captureTarget":1,
      "startingPlayer":"black",
      "setupStones":[
        {"x":0,"y":0,"color":"black"},
        {"x":2,"y":0,"color":"black"},
        {"x":1,"y":0,"color":"white"}
      ]
    }
    """;
var captureSession = RequireSuccess(await playSpace.CreateSessionAsync(new(Configuration(captureConfiguration))));
var capture = RequireSuccess(await playSpace.ApplyActionAsync(new(
    captureSession.SessionId,
    Action("""{"version":1,"type":"play","player":"black","x":1,"y":1}"""),
    captureSession.InitialSnapshot.Revision)));
Require(capture.IsAccepted, "Capture action must be accepted.");
Require(capture.Snapshot.IsTerminal, "Capture target one must end after one captured stone.");
Require(capture.Snapshot.Outcome?.SchemaId == PonnukiSchemas.Outcome, "Terminal snapshot must include an outcome.");
using (var outcome = JsonDocument.Parse(capture.Snapshot.Outcome!.Content))
    Require(outcome.RootElement.GetProperty("winner").GetString() == "black", "Black must win the capture scenario.");

var afterTerminal = RequireSuccess(await playSpace.ApplyActionAsync(new(
    captureSession.SessionId,
    Action("""{"version":1,"type":"pass","player":"white"}"""),
    capture.Snapshot.Revision)));
Require(!afterTerminal.IsAccepted && afterTerminal.Rejection?.Code == "game-already-terminal", "Actions after terminal state must be rejected.");

var conflict = await playSpace.ApplyActionAsync(new(
    first.SessionId,
    Action("""{"version":1,"type":"pass","player":"black"}"""),
    999));
Require(!conflict.IsSuccess && conflict.Error?.Code == "revision-conflict", "Stale revisions must fail at the protocol boundary.");

RequireSuccess(await playSpace.CloseSessionAsync(new(first.SessionId)));
RequireSuccess(await playSpace.CloseSessionAsync(new(second.SessionId)));
RequireSuccess(await playSpace.CloseSessionAsync(new(captureSession.SessionId)));

Console.WriteLine("PASS: Protocol S Ponnuki reference play-space lifecycle, deterministic setup, capture, terminal state, and revision checks.");
return;

static ContractDocument Configuration(string content) => new("application/json", PonnukiSchemas.Configuration, content);
static ContractDocument Action(string content) => new("application/json", PonnukiSchemas.Action, content);

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
