using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.Reference.PlaySpace.Go;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;
using KifuwarabeGo2026.Reference.Gui;

var concierge = new GameOasisConcierge();
RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));
RequireSuccess(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol()));

// From this point, the simulated GUI knows only Protocol G.
IGuiProtocol gui = new GameOasisGuiProtocol(concierge);
var catalog = RequireSuccess(await gui.GetPlaySpacesAsync());
Require(catalog.Count == 2, "Protocol G must expose both replaceable play-spaces.");
var ponnuki = catalog.Single(entry => entry.TypeId.Value == GameOasisOfficialNames.Ponnuki);
Require(ponnuki.TypeId.Value == GameOasisOfficialNames.Ponnuki, "Protocol G must preserve the stable play-space type ID.");
var normalGo = catalog.Single(entry => entry.TypeId.Value == GameOasisOfficialNames.Go);

var schema = RequireSuccess(await gui.GetConfigurationSchemaAsync(ponnuki.TypeId));
Require(schema.SchemaId == GameOasisOfficialNames.Ponnuki + ".configuration.v1", "Protocol G must expose the selected play-space configuration schema.");

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
var initialBoard = RequireSuccess(GameBoardProjection.Project(opened.InitialSnapshot));
Require(initialBoard.BoardSize == 9 && initialBoard.Black.Count == 2 && initialBoard.White.Count == 1 && initialBoard.NextToPlay == "black", "The Ponnuki state must project to the common GUI board.");
var unsupportedPonnukiPass = GameBoardActionFactory.CreatePass(initialBoard);
Require(!unsupportedPonnukiPass.IsSuccess && unsupportedPonnukiPass.Error?.Code == "unsupported-gui-action", "The GUI action factory must not invent a Ponnuki pass action.");
var invalidProjection = GameBoardProjection.Project(opened.InitialSnapshot with
{
    State = new ContractDocument("application/json", opened.InitialSnapshot.State.SchemaId,
        """{"boardSize":9,"black":[{"x":1,"y":1}],"white":[{"x":1,"y":1}],"nextToPlay":"black","koPoint":null}""")
});
Require(!invalidProjection.IsSuccess && invalidProjection.Error?.Code == "duplicate-gui-board-point", "The GUI projection must reject overlapping black and white stones.");

var action = new ContractDocument(
    "application/json",
    GameOasisOfficialNames.Ponnuki + ".action.v1",
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

var goSchema = RequireSuccess(await gui.GetConfigurationSchemaAsync(normalGo.TypeId));
var goConfiguration = new ContractDocument(
    "application/json",
    goSchema.SchemaId,
    """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}""");
var goOpened = RequireSuccess(await gui.OpenSessionAsync(new(normalGo.TypeId, goConfiguration)));
var goBoard = RequireSuccess(GameBoardProjection.Project(goOpened.InitialSnapshot));
Require(goBoard.BoardSize == 9 && goBoard.Black.Count == 0 && goBoard.White.Count == 0, "The normal Go state must project through the same GUI board model.");
var blackPassAction = RequireSuccess(GameBoardActionFactory.CreatePass(goBoard));
var blackPass = RequireSuccess(await gui.SubmitActionAsync(new(
    goOpened.InitialSnapshot.SessionId,
    blackPassAction,
    0)));
var afterBlackPassBoard = RequireSuccess(GameBoardProjection.Project(blackPass.Snapshot));
var whitePassAction = RequireSuccess(GameBoardActionFactory.CreatePass(afterBlackPassBoard));
var whitePass = RequireSuccess(await gui.SubmitActionAsync(new(
    goOpened.InitialSnapshot.SessionId,
    whitePassAction,
    blackPass.Snapshot.Revision)));
Require(whitePass.Snapshot.IsTerminal, "The same Protocol G lifecycle must operate normal Go.");
using (var goOutcome = JsonDocument.Parse(whitePass.Snapshot.Outcome!.Content))
    Require(goOutcome.RootElement.GetProperty("winner").GetString() == "white", "Protocol G must preserve the normal Go outcome.");
RequireSuccess(await gui.CloseSessionAsync(new(goOpened.InitialSnapshot.SessionId)));

var client = new GameOasisGuiClient(gui);
var boardController = new GameOasisBoardController(client);
var clientCatalog = RequireSuccess(await client.InitializeAsync());
Require(clientCatalog.Count == 2 && client.State.PlaySpaces.Count == 2, "The reference GUI client must retain the Protocol G catalog.");
var clientOpened = RequireSuccess(await boardController.OpenAsync(ponnuki.TypeId, configuration));
Require(client.State.ActiveSnapshot?.SessionId == clientOpened.SessionId, "The reference GUI client must retain its active snapshot.");
var duplicateOpen = await client.OpenSessionAsync(ponnuki.TypeId, configuration);
Require(!duplicateOpen.IsSuccess && client.State.LastError?.Code == "gui-session-already-open", "The reference GUI client must reject a second local active session.");
var clientBoard = RequireSuccess(boardController.GetBoard());
var occupiedAction = GameBoardActionFactory.CreatePlay(clientBoard, 0, 0);
Require(!occupiedAction.IsSuccess && occupiedAction.Error?.Code == "gui-point-occupied", "The GUI action factory must reject an occupied point before Protocol G submission.");
var generatedAction = RequireSuccess(GameBoardActionFactory.CreatePlay(clientBoard, 1, 1));
Require(generatedAction.SchemaId == GameOasisOfficialNames.Ponnuki + ".action.v1", "The GUI action factory must select the official Ponnuki action schema.");
var clientSubmitted = RequireSuccess(await boardController.PlayAsync(1, 1));
Require(clientSubmitted.IsAccepted && client.State.ActiveSnapshot?.IsTerminal == true, "The reference GUI client must advance its snapshot using the current revision.");
var terminalBoard = RequireSuccess(GameBoardProjection.Project(client.State.ActiveSnapshot!));
Require(terminalBoard.IsTerminal && terminalBoard.BlackCaptures == 1 && terminalBoard.Outcome is not null, "The common GUI board must preserve Ponnuki terminal captures and outcome.");
RequireSuccess(await boardController.RefreshAsync());
RequireSuccess(await boardController.CloseAsync());
Require(client.State.ActiveSnapshot is null && client.State.LastError is null, "Closing must clear the active GUI session and error.");
var submitWithoutSession = await client.SubmitActionAsync(action);
Require(!submitWithoutSession.IsSuccess && submitWithoutSession.Error?.Code == "gui-session-not-open", "A semantic action without a session must fail locally.");

Console.WriteLine("PASS: Protocol G and the Contracts-only reference GUI client selected and operated both replaceable play-spaces.");
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
