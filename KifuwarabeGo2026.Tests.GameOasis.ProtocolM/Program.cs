using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;
using KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

var concierge = new GameOasisConcierge();
var playSpace = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));
var configuration = new ContractDocument(
    "application/json",
    PonnukiSchemas.Configuration,
    """{"version":1,"boardSize":9,"initialMoveCount":0,"randomSeed":42,"captureTarget":1,"startingPlayer":"black","setupStones":[]}""");
var opened = RequireSuccess(await concierge.OpenSessionAsync(playSpace.Descriptor.TypeId, configuration));

var externalGameMaster = new ScriptedGameMaster();
var coordinator = new GameOasisGameMasterCoordinator(concierge);
var registered = RequireSuccess(await coordinator.RegisterGameMasterAsync(externalGameMaster));
Require(registered.Descriptor.EngineId == ScriptedGameMaster.EngineId, "The external game master must register through Protocol M.");
var bound = RequireSuccess(await coordinator.BindGameMasterAsync(registered.Descriptor.EngineId, opened.SessionId));
Require(externalGameMaster.StartCount == 1, "The game master must receive the session start.");

var completed = RequireSuccess(await coordinator.RequestAndExecuteCommandAsync(bound.BindingId));
Require(completed.Result.WasAccepted, "The end-session command must be accepted.");
Require(completed.Result.CommandName == GameOasisGameMasterCoordinator.EndSessionCommand, "The executed command name must be preserved.");
Require(completed.NotificationFailures.Count == 0, "The command result notification must succeed.");
Require(completed.EndFailures.Count == 0, "Automatic game-master participation end must succeed.");
Require(externalGameMaster.NotificationCount == 1, "The game master must receive the command result.");
Require(externalGameMaster.EndCount == 1, "The game master must receive participation end after closing the game.");

var afterClose = await concierge.GetSnapshotAsync(opened.SessionId);
Require(!afterClose.IsSuccess && afterClose.Error?.Code == "game-oasis-session-not-found", "The game master command must close the Game Oasis session.");
var afterAutomaticUnbind = await coordinator.UnbindGameMasterAsync(bound.BindingId, "must-already-be-ended");
Require(!afterAutomaticUnbind.IsSuccess && afterAutomaticUnbind.Error?.Code == "game-master-binding-not-found", "Closing the game must remove its game-master binding.");
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(playSpace.Descriptor.TypeId));

Console.WriteLine("PASS: An external-style Protocol M game master ended a Protocol S Ponnuki session through Concierge.");
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

internal sealed class ScriptedGameMaster : IGameMasterProtocol
{
    public static readonly GameMasterEngineId EngineId = new("org.kifuwarabe.tests.scripted-game-master");

    public int StartCount { get; private set; }
    public int NotificationCount { get; private set; }
    public int EndCount { get; private set; }

    public ValueTask<ProtocolResponse<GameMasterEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<GameMasterEngineDescriptor>.Success(new(
            EngineId,
            "Scripted game master",
            ContractVersion.V1_0,
            nameof(ScriptedGameMaster),
            "1.0.0",
            [GameOasisGameMasterCoordinator.EndSessionCommand])));
    }

    public ValueTask<ProtocolResponse<GameMasterSessionStarted>> StartSessionAsync(
        GameMasterSessionStartRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        StartCount++;
        return ValueTask.FromResult(ProtocolResponse<GameMasterSessionStarted>.Success(new(request.BindingId)));
    }

    public ValueTask<ProtocolResponse<GameMasterCommandSelected>> SelectCommandAsync(
        GameMasterCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<GameMasterCommandSelected>.Success(new(
            request.BindingId,
            request.Observation.Revision,
            new GameMasterCommand(GameOasisGameMasterCoordinator.EndSessionCommand, "broadcast-finished"))));
    }

    public ValueTask<ProtocolResponse<GameMasterCommandNotified>> NotifyCommandAsync(
        GameMasterCommandNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NotificationCount++;
        return ValueTask.FromResult(ProtocolResponse<GameMasterCommandNotified>.Success(new(notification.BindingId)));
    }

    public ValueTask<ProtocolResponse<GameMasterSessionEnded>> EndSessionAsync(
        GameMasterSessionEndRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EndCount++;
        return ValueTask.FromResult(ProtocolResponse<GameMasterSessionEnded>.Success(new(request.BindingId)));
    }
}
