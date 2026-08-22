using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

var concierge = new GameOasisConcierge();
var playSpace = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));

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
var opened = RequireSuccess(await concierge.OpenSessionAsync(playSpace.Descriptor.TypeId, configuration));

var externalPlayer = new ScriptedPlayer();
var coordinator = new GameOasisPlayerCoordinator(concierge);
var registered = RequireSuccess(await coordinator.RegisterPlayerAsync(externalPlayer));
Require(registered.Descriptor.EngineId == ScriptedPlayer.EngineId, "The external player must register through Protocol P.");

var bound = RequireSuccess(await coordinator.BindPlayerAsync(
    registered.Descriptor.EngineId,
    opened.SessionId,
    "black"));
Require(externalPlayer.StartCount == 1, "The player must receive the session start.");

var completed = RequireSuccess(await coordinator.RequestAndApplyActionAsync(bound.BindingId));
Require(completed.Applied.IsAccepted, "The scripted action must be accepted.");
Require(completed.Applied.Snapshot.IsTerminal, "The capture must finish this capture-target-one game.");
Require(completed.NotificationFailures.Count == 0, "Player notification must succeed.");
Require(externalPlayer.NotificationCount == 1, "The player must receive the applied action notification.");
using (var outcome = JsonDocument.Parse(completed.Applied.Snapshot.Outcome!.Content))
    Require(outcome.RootElement.GetProperty("winner").GetString() == "black", "Black must win after capturing white.");

RequireSuccess(await coordinator.UnbindPlayerAsync(bound.BindingId, "smoke-test-complete"));
Require(externalPlayer.EndCount == 1, "The player must receive the session end.");
RequireSuccess(await concierge.CloseSessionAsync(opened.SessionId));
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(playSpace.Descriptor.TypeId));

Console.WriteLine("PASS: An external-style Protocol P player selected an action that Concierge applied to Protocol S Ponnuki.");
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

internal sealed class ScriptedPlayer : IPlayerProtocol
{
    public static readonly PlayerEngineId EngineId = new("org.kifuwarabe.tests.scripted-player");

    public int StartCount { get; private set; }
    public int NotificationCount { get; private set; }
    public int EndCount { get; private set; }

    public ValueTask<ProtocolResponse<PlayerEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<PlayerEngineDescriptor>.Success(new PlayerEngineDescriptor(
            EngineId,
            "Scripted black player",
            ContractVersion.V1_0,
            nameof(ScriptedPlayer),
            "1.0.0",
            [new PlaySpaceTypeId("org.kifuwarabe.games.ponnuki")],
            ["select-action"])));
    }

    public ValueTask<ProtocolResponse<PlayerSessionStarted>> StartSessionAsync(
        PlayerSessionStartRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartCount++;
        return ValueTask.FromResult(ProtocolResponse<PlayerSessionStarted>.Success(new(request.BindingId)));
    }

    public ValueTask<ProtocolResponse<PlayerActionSelected>> SelectActionAsync(
        PlayerActionRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var action = new ContractDocument(
            "application/json",
            PonnukiSchemas.Action,
            """{"version":1,"type":"play","player":"black","x":1,"y":1}""");
        return ValueTask.FromResult(ProtocolResponse<PlayerActionSelected>.Success(new(
            request.BindingId,
            request.Observation.Revision,
            action)));
    }

    public ValueTask<ProtocolResponse<PlayerActionNotified>> NotifyActionAsync(
        PlayerActionNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NotificationCount++;
        return ValueTask.FromResult(ProtocolResponse<PlayerActionNotified>.Success(new(
            notification.BindingId,
            notification.Observation.Revision)));
    }

    public ValueTask<ProtocolResponse<PlayerSessionEnded>> EndSessionAsync(
        PlayerSessionEndRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EndCount++;
        return ValueTask.FromResult(ProtocolResponse<PlayerSessionEnded>.Success(new(request.BindingId)));
    }
}
