using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

var concierge = new GameOasisConcierge();
RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));

// From this point, the simulated GUI knows only Protocol G.
IGuiProtocol gui = new GameOasisGuiProtocol(concierge);
var catalog = RequireSuccess(await gui.GetPlaySpacesAsync());
Require(catalog.Count == 1, "Protocol G must expose one registered play-space.");
var ponnuki = catalog[0];
Require(ponnuki.TypeId.Value == "org.kifuwarabe.games.ponnuki", "Protocol G must preserve the stable play-space type ID.");

var schema = RequireSuccess(await gui.GetConfigurationSchemaAsync(ponnuki.TypeId));
Require(schema.SchemaId == "org.kifuwarabe.games.ponnuki.configuration.v1", "Protocol G must expose the selected play-space configuration schema.");

const string configurationJson = """
    {
      "version":1,
      "boardSize":9,
      "initialMoveCount":0,
      "randomSeed":99,
      "captureTarget":1,
      "startingPlayer":"black",
      "setupStones":[
        {"x":0,"y":0,"color":"black"},
        {"x":2,"y":0,"color":"black"},
        {"x":1,"y":0,"color":"white"}
      ]
    }
    """;
var configuration = new ContractDocument("application/json", schema.SchemaId, configurationJson);
var opened = RequireSuccess(await gui.OpenSessionAsync(new GuiOpenSessionRequest(ponnuki.TypeId, configuration)));
Require(opened.InitialSnapshot.PlaySpaceTypeId == ponnuki.TypeId, "GUI snapshot must identify the selected play-space.");

var action = new ContractDocument(
    "application/json",
    "org.kifuwarabe.games.ponnuki.action.v1",
    """{"version":1,"type":"play","player":"black","x":1,"y":1}""");
var submitted = RequireSuccess(await gui.SubmitActionAsync(new GuiSubmitActionRequest(
    opened.InitialSnapshot.SessionId,
    action,
    opened.InitialSnapshot.Revision)));
Require(submitted.IsAccepted, "Protocol G must report the accepted action.");
Require(submitted.Snapshot.IsTerminal, "Protocol G must report the terminal state.");
using (var outcome = JsonDocument.Parse(submitted.Snapshot.Outcome!.Content))
    Require(outcome.RootElement.GetProperty("winner").GetString() == "black", "Protocol G must preserve the outcome document.");

var current = RequireSuccess(await gui.GetSnapshotAsync(new GuiGetSnapshotRequest(opened.InitialSnapshot.SessionId)));
Require(current.Revision == submitted.Snapshot.Revision, "Protocol G must return the current revision.");

RequireSuccess(await gui.CloseSessionAsync(new GuiCloseSessionRequest(opened.InitialSnapshot.SessionId)));
var afterClose = await gui.GetSnapshotAsync(new GuiGetSnapshotRequest(opened.InitialSnapshot.SessionId));
Require(!afterClose.IsSuccess && afterClose.Error?.Code == "game-oasis-session-not-found", "Protocol G must report a closed session without exposing Protocol S IDs.");

Console.WriteLine("PASS: Protocol G catalog, schema, session, action, snapshot, outcome, and close lifecycle crossed Concierge without concrete GUI coupling.");
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
