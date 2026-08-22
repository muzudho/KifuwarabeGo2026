using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.Reference.PlaySpace.Go;

IPlaySpaceProtocol go = new GoPlaySpaceProtocol();
var descriptor = RequireSuccess(await go.DescribeAsync());
Require(descriptor.TypeId.Value == GameOasisOfficialNames.Go, "The normal Go type ID must be stable.");
Require(descriptor.Capabilities.Contains("chinese-area-scoring"), "The ruleset capability must be advertised.");
Require(descriptor.Capabilities.Contains("move-history-observation"), "The additive move-history observation capability must be advertised.");

var schema = RequireSuccess(await go.GetConfigurationSchemaAsync());
using (var schemaJson = JsonDocument.Parse(schema.Content))
    Require(schemaJson.RootElement.GetProperty("$id").GetString() == GoSchemas.Configuration, "The configuration schema ID must be stable.");

var koConfiguration = Configuration(
    """
    {
      "version":1,
      "boardSize":9,
      "komi":6.5,
      "ruleset":"chinese-area",
      "startingPlayer":"black",
      "setupStones":[
        {"x":0,"y":0,"color":"black"},
        {"x":2,"y":0,"color":"black"},
        {"x":1,"y":0,"color":"white"},
        {"x":0,"y":1,"color":"white"},
        {"x":2,"y":1,"color":"white"},
        {"x":1,"y":2,"color":"white"}
      ]
    }
    """);
var validation = RequireSuccess(await go.ValidateConfigurationAsync(new(koConfiguration)));
Require(validation.IsValid, "The explicit ko setup must validate.");
var koSession = RequireSuccess(await go.CreateSessionAsync(new(koConfiguration)));
var capture = RequireSuccess(await go.ApplyActionAsync(new(
    koSession.SessionId,
    Action("""{"version":1,"type":"play","player":"black","x":1,"y":1}"""),
    0)));
Require(capture.IsAccepted && capture.Snapshot.Revision == 1, "Black must capture one stone.");
using (var state = JsonDocument.Parse(capture.Snapshot.State.Content))
{
    Require(state.RootElement.GetProperty("blackCaptures").GetInt32() == 1, "The capture count must advance.");
    var ko = state.RootElement.GetProperty("koPoint");
    Require(ko.GetProperty("x").GetInt32() == 1 && ko.GetProperty("y").GetInt32() == 0, "The simple ko point must be exposed.");
    Require(state.RootElement.GetProperty("setupBlack").GetArrayLength() == 2, "The original black setup must remain distinguishable from the current board.");
    Require(state.RootElement.GetProperty("setupWhite").GetArrayLength() == 4, "The original white setup must remain distinguishable from the current board.");
    var history = state.RootElement.GetProperty("moveHistory");
    Require(history.GetArrayLength() == 1, "Only accepted moves must enter the move history.");
    Require(history[0].GetProperty("player").GetString() == "black" && history[0].GetProperty("x").GetInt32() == 1, "The move history must preserve player and point.");
}
var recapture = RequireSuccess(await go.ApplyActionAsync(new(
    koSession.SessionId,
    Action("""{"version":1,"type":"play","player":"white","x":1,"y":0}"""),
    1)));
Require(!recapture.IsAccepted && recapture.Rejection?.Code == "illegal-action", "Immediate ko recapture must be rejected.");
using (var rejectedState = JsonDocument.Parse(recapture.Snapshot.State.Content))
    Require(rejectedState.RootElement.GetProperty("moveHistory").GetArrayLength() == 1, "A rejected move must not enter the move history.");
var stale = await go.ApplyActionAsync(new(
    koSession.SessionId,
    Action("""{"version":1,"type":"pass","player":"white"}"""),
    0));
Require(!stale.IsSuccess && stale.Error?.Code == "revision-conflict", "A stale play-space revision must be rejected.");
RequireSuccess(await go.CloseSessionAsync(new(koSession.SessionId)));

var scoringConfiguration = Configuration(
    """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}""");
var scoringSession = RequireSuccess(await go.CreateSessionAsync(new(scoringConfiguration)));
var blackPass = RequireSuccess(await go.ApplyActionAsync(new(
    scoringSession.SessionId,
    Action("""{"version":1,"type":"pass","player":"black"}"""),
    0)));
Require(blackPass.IsAccepted && !blackPass.Snapshot.IsTerminal, "One pass must not end the game.");
var whitePass = RequireSuccess(await go.ApplyActionAsync(new(
    scoringSession.SessionId,
    Action("""{"version":1,"type":"pass","player":"white"}"""),
    1)));
Require(whitePass.IsAccepted && whitePass.Snapshot.IsTerminal, "Two consecutive passes must end and score the game.");
using (var state = JsonDocument.Parse(whitePass.Snapshot.State.Content))
{
    var history = state.RootElement.GetProperty("moveHistory");
    Require(history.GetArrayLength() == 2 && history[0].GetProperty("type").GetString() == "pass", "Accepted passes must be preserved for SGF reconstruction.");
}
using (var outcome = JsonDocument.Parse(whitePass.Snapshot.Outcome!.Content))
{
    Require(outcome.RootElement.GetProperty("winner").GetString() == "white", "Komi must make white the winner on an empty board.");
    Require(outcome.RootElement.GetProperty("margin").GetDecimal() == 6.5m, "The scored margin must include komi.");
}
var afterTerminal = RequireSuccess(await go.ApplyActionAsync(new(
    scoringSession.SessionId,
    Action("""{"version":1,"type":"play","player":"black","x":4,"y":4}"""),
    2)));
Require(!afterTerminal.IsAccepted && afterTerminal.Rejection?.Code == "game-already-terminal", "Actions after scoring must be rejected.");
RequireSuccess(await go.CloseSessionAsync(new(scoringSession.SessionId)));

var resignationSession = RequireSuccess(await go.CreateSessionAsync(new(scoringConfiguration)));
var resignation = RequireSuccess(await go.ApplyActionAsync(new(
    resignationSession.SessionId,
    Action("""{"version":1,"type":"resign","player":"black"}"""),
    0)));
Require(resignation.Snapshot.IsTerminal, "Resignation must end the game.");
using (var outcome = JsonDocument.Parse(resignation.Snapshot.Outcome!.Content))
    Require(outcome.RootElement.GetProperty("winner").GetString() == "white", "The opponent must win by resignation.");
RequireSuccess(await go.CloseSessionAsync(new(resignationSession.SessionId)));

Console.WriteLine("PASS: Protocol S normal Go validated setup, capture, ko, revision, two-pass area scoring, terminal protection, and resignation.");
return;

static ContractDocument Configuration(string json) => new("application/json", GoSchemas.Configuration, json);
static ContractDocument Action(string json) => new("application/json", GoSchemas.Action, json);

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
