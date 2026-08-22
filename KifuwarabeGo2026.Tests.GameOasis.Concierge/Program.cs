using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

var concierge = new GameOasisConcierge();
IPlaySpaceProtocol externalPlaySpace = new PonnukiPlaySpaceProtocol();

var registered = RequireSuccess(await concierge.RegisterPlaySpaceAsync(externalPlaySpace));
Require(registered.Descriptor.TypeId.Value == "org.kifuwarabe.games.ponnuki", "Ponnuki must register through Protocol S.");
Require(concierge.GetPlaySpaces().Count == 1, "The catalog must contain the registered play-space.");

var duplicate = await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol());
Require(!duplicate.IsSuccess && duplicate.Error?.Code == "play-space-already-registered", "Duplicate type IDs must be rejected.");

const string configurationJson = """
    {
      "version":1,
      "boardSize":9,
      "initialMoveCount":0,
      "randomSeed":42,
      "captureTarget":1,
      "startingPlayer":"black",
      "setupStones":[
        {"x":0,"y":0,"color":"black"},
        {"x":2,"y":0,"color":"black"},
        {"x":1,"y":0,"color":"white"}
      ]
    }
    """;
var configuration = new ContractDocument("application/json", PonnukiSchemas.Configuration, configurationJson);
var opened = RequireSuccess(await concierge.OpenSessionAsync(registered.Descriptor.TypeId, configuration));

var inUse = await concierge.UnregisterPlaySpaceAsync(registered.Descriptor.TypeId);
Require(!inUse.IsSuccess && inUse.Error?.Code == "play-space-in-use", "An in-use play-space must not unregister.");

var action = new ContractDocument(
    "application/json",
    PonnukiSchemas.Action,
    """{"version":1,"type":"play","player":"black","x":1,"y":1}""");
var applied = RequireSuccess(await concierge.ApplyActionAsync(new ApplyGameOasisActionRequest(
    opened.SessionId, action, opened.InitialSnapshot.Revision)));
Require(applied.IsAccepted && applied.Snapshot.IsTerminal, "Concierge must forward the capture through Protocol S.");
using (var outcome = JsonDocument.Parse(applied.Snapshot.Outcome!.Content))
    Require(outcome.RootElement.GetProperty("winner").GetString() == "black", "The forwarded outcome must name black as winner.");

var snapshot = RequireSuccess(await concierge.GetSnapshotAsync(opened.SessionId));
Require(snapshot.Revision == applied.Snapshot.Revision, "Concierge must return the current play-space snapshot.");

RequireSuccess(await concierge.CloseSessionAsync(opened.SessionId));
var afterClose = await concierge.GetSnapshotAsync(opened.SessionId);
Require(!afterClose.IsSuccess && afterClose.Error?.Code == "game-oasis-session-not-found", "Closed sessions must leave the Concierge catalog.");

RequireSuccess(await concierge.UnregisterPlaySpaceAsync(registered.Descriptor.TypeId));
Require(concierge.GetPlaySpaces().Count == 0, "The play-space catalog must be empty after unregistering.");

Console.WriteLine("PASS: Concierge registered an external-style Protocol S implementation and managed its session without concrete coupling.");
return;

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
