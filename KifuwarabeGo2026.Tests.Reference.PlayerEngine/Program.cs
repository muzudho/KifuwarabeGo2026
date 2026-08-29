using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.PlayerEngine;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki;

var concierge = new GameOasisConcierge();
var goRegistration = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new GoPlaySpaceProtocol()));
var ponnukiRegistration = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));

var goSession = RequireSuccess(await concierge.OpenSessionAsync(
    goRegistration.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        GoSchemas.Configuration,
        """{"version":1,"boardSize":9,"komi":6.5,"ruleset":"chinese-area","startingPlayer":"black","setupStones":[]}""")));
var ponnukiSession = RequireSuccess(await concierge.OpenSessionAsync(
    ponnukiRegistration.Descriptor.TypeId,
    new ContractDocument(
        "application/json",
        PonnukiSchemas.Configuration,
        """
        {
          "version":1,
          "boardSize":9,
          "initialMoveCount":0,
          "randomSeed":1,
          "captureTarget":20,
          "startingPlayer":"black",
          "setupStones":[
            {"x":1,"y":0,"color":"white"},
            {"x":0,"y":1,"color":"white"}
          ]
        }
        """)));

var player = new DeterministicPlayerProtocol();
var playerCoordinator = new GameOasisPlayerCoordinator(concierge);
var registered = RequireSuccess(await playerCoordinator.RegisterPlayerAsync(player));
Require(registered.Descriptor.SupportedPlaySpaces.Count == 2, "The reference player must advertise both play-spaces.");
var goBinding = RequireSuccess(await playerCoordinator.BindPlayerAsync(
    registered.Descriptor.EngineId,
    goSession.SessionId,
    "black"));
var ponnukiBinding = RequireSuccess(await playerCoordinator.BindPlayerAsync(
    registered.Descriptor.EngineId,
    ponnukiSession.SessionId,
    "black"));

var goTurn = RequireSuccess(await playerCoordinator.RequestAndApplyActionAsync(goBinding.BindingId));
Require(goTurn.Applied.IsAccepted, "The reference player must play normal Go through Protocol P.");
using (var goState = JsonDocument.Parse(goTurn.Applied.Snapshot.State.Content))
{
    var firstBlack = goState.RootElement.GetProperty("black")[0];
    Require(firstBlack.GetProperty("x").GetInt32() == 0 && firstBlack.GetProperty("y").GetInt32() == 0, "The deterministic first Go move must be (0,0).");
}

var rejectedPonnukiTurn = RequireSuccess(await playerCoordinator.RequestAndApplyActionAsync(ponnukiBinding.BindingId));
Require(!rejectedPonnukiTurn.Applied.IsAccepted && rejectedPonnukiTurn.Applied.Rejection?.Code == "illegal-action", "The deliberately suicidal first Ponnuki candidate must be rejected.");
var recoveredPonnukiTurn = RequireSuccess(await playerCoordinator.RequestAndApplyActionAsync(ponnukiBinding.BindingId));
Require(recoveredPonnukiTurn.Applied.IsAccepted, "The reference player must recover by selecting a different point at the same revision.");
using (var ponnukiState = JsonDocument.Parse(recoveredPonnukiTurn.Applied.Snapshot.State.Content))
{
    var black = ponnukiState.RootElement.GetProperty("black");
    Require(black.GetArrayLength() == 1, "The recovered Ponnuki move must place one black stone.");
    Require(black[0].GetProperty("x").GetInt32() == 2 && black[0].GetProperty("y").GetInt32() == 0, "The rejected point must not be selected again.");
}

RequireSuccess(await playerCoordinator.UnbindPlayerAsync(goBinding.BindingId, "smoke-test-complete"));
RequireSuccess(await playerCoordinator.UnbindPlayerAsync(ponnukiBinding.BindingId, "smoke-test-complete"));
RequireSuccess(await concierge.CloseSessionAsync(goSession.SessionId));
RequireSuccess(await concierge.CloseSessionAsync(ponnukiSession.SessionId));
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(goRegistration.Descriptor.TypeId));
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(ponnukiRegistration.Descriptor.TypeId));

Console.WriteLine("PASS: One Protocol P reference player joined normal Go and Ponnuki concurrently and recovered from a rejected candidate.");
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
