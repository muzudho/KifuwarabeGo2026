using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Concierge;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Ponnuki;

var concierge = new GameOasisConcierge();
IGuiProtocol gui = new GameOasisGuiProtocol(concierge);
var playSpace = RequireSuccess(await concierge.RegisterPlaySpaceAsync(new PonnukiPlaySpaceProtocol()));
var configuration = new ContractDocument(
    "application/json",
    PonnukiSchemas.Configuration,
    """{"version":1,"boardSize":9,"initialMoveCount":0,"randomSeed":42,"captureTarget":1,"startingPlayer":"black","setupStones":[]}""");
var opened = RequireSuccess(await concierge.OpenSessionAsync(playSpace.Descriptor.TypeId, configuration));

var playerCoordinator = new GameOasisPlayerCoordinator(concierge);
var observingPlayer = new ObservingPlayer();
var registeredPlayer = RequireSuccess(await playerCoordinator.RegisterPlayerAsync(observingPlayer));
var boundPlayer = RequireSuccess(await playerCoordinator.BindPlayerAsync(
    registeredPlayer.Descriptor.EngineId,
    opened.SessionId,
    "black"));
var failingPlayer = new ObservingPlayer(GameOasisOfficialNames.Root + ".tests.failing-end-player", failEnd: true);
var registeredFailingPlayer = RequireSuccess(await playerCoordinator.RegisterPlayerAsync(failingPlayer));
var boundFailingPlayer = RequireSuccess(await playerCoordinator.BindPlayerAsync(
    registeredFailingPlayer.Descriptor.EngineId,
    opened.SessionId,
    "white"));
var externalGameMaster = new ScriptedGameMaster();
var coordinator = new GameOasisGameMasterCoordinator(concierge, playerCoordinator);
var registered = RequireSuccess(await coordinator.RegisterGameMasterAsync(externalGameMaster));
Require(registered.Descriptor.EngineId == ScriptedGameMaster.EngineId, "The external game master must register through Protocol M.");
var bound = RequireSuccess(await coordinator.BindGameMasterAsync(registered.Descriptor.EngineId, opened.SessionId));
Require(externalGameMaster.StartCount == 1, "The game master must receive the session start.");

var paused = RequireSuccess(await coordinator.RequestAndExecuteCommandAsync(bound.BindingId));
Require(paused.Result.WasAccepted && paused.Result.CommandName == GameOasisGameMasterCoordinator.PauseCommand, "The pause command must be accepted.");
var pausedSnapshot = RequireSuccess(await concierge.GetSnapshotAsync(opened.SessionId));
Require(pausedSnapshot.OperationalState == GameOasisOperationalState.Paused, "The Concierge must own the paused state.");
Require(pausedSnapshot.OperationRevision == 1, "Pausing must advance the operation revision.");
var pausedForGui = RequireSuccess(await gui.GetSnapshotAsync(new(opened.SessionId)));
Require(pausedForGui.OperationalState == GameOasisOperationalState.Paused && pausedForGui.OperationRevision == 1, "Protocol G must expose the operational state without interpreting play-space data.");
Require(observingPlayer.StateNotificationCount == 1 && observingPlayer.LastObservation?.OperationalState == GameOasisOperationalState.Paused, "Protocol P must notify the player about pausing.");

var blockedAction = RequireSuccess(await concierge.ApplyActionAsync(new(
    opened.SessionId,
    new ContractDocument("application/json", PonnukiSchemas.Action, """{"version":1,"type":"play","player":"black","x":1,"y":1}"""),
    pausedSnapshot.Revision)));
Require(!blockedAction.IsAccepted && blockedAction.Rejection?.Code == "game-session-paused", "Player actions must be rejected while paused.");
Require(blockedAction.Snapshot.Revision == pausedSnapshot.Revision, "A blocked action must not change the play-space revision.");

var resumed = RequireSuccess(await coordinator.RequestAndExecuteCommandAsync(bound.BindingId));
Require(resumed.Result.WasAccepted && resumed.Result.CommandName == GameOasisGameMasterCoordinator.ResumeCommand, "The resume command must be accepted.");
var resumedSnapshot = RequireSuccess(await concierge.GetSnapshotAsync(opened.SessionId));
Require(resumedSnapshot.OperationalState == GameOasisOperationalState.Running, "The Concierge must return to running state.");
Require(resumedSnapshot.OperationRevision == 2, "Resuming must advance the operation revision.");
Require(observingPlayer.StateNotificationCount == 2 && observingPlayer.LastObservation?.OperationalState == GameOasisOperationalState.Running, "Protocol P must notify the player about resuming.");

var staleOperation = RequireSuccess(await concierge.ApplyOperationAsync(new(
    opened.SessionId,
    GameOasisConcierge.PauseOperation,
    0)));
Require(!staleOperation.IsAccepted && staleOperation.Rejection?.Code == "operation-revision-conflict", "A stale game-master operation must be rejected.");
var invalidAdjudication = RequireSuccess(await concierge.ApplyOperationAsync(new(
    opened.SessionId,
    GameOasisConcierge.AdjudicateOperation,
    resumedSnapshot.OperationRevision,
    new ContractDocument(
        GameOasisAdjudicationDocuments.MediaType,
        GameOasisAdjudicationDocuments.ResultSchemaId,
        """{"version":1,"kind":"winner","reasonCode":"missing-winner"}"""))));
Require(!invalidAdjudication.IsAccepted && invalidAdjudication.Rejection?.Code == "adjudication-winner-required", "Concierge must reject an invalid standard adjudication result.");
Require(invalidAdjudication.Snapshot.OperationRevision == resumedSnapshot.OperationRevision, "A rejected adjudication must not advance the operation revision.");

var adjudicated = RequireSuccess(await coordinator.RequestAndExecuteCommandAsync(bound.BindingId));
Require(adjudicated.Result.WasAccepted && adjudicated.Result.CommandName == GameOasisGameMasterCoordinator.AdjudicateCommand, "The adjudication command must be accepted.");
var adjudicatedForGui = RequireSuccess(await gui.GetSnapshotAsync(new(opened.SessionId)));
Require(adjudicatedForGui.OperationalState == GameOasisOperationalState.Adjudicated, "Protocol G must expose the adjudicated state.");
Require(adjudicatedForGui.IsTerminal, "A game-master adjudication must make the Game Oasis game terminal.");
Require(adjudicatedForGui.OperationRevision == 3, "Adjudication must advance the operation revision.");
using (var outcome = JsonDocument.Parse(adjudicatedForGui.Outcome!.Content))
{
    Require(outcome.RootElement.GetProperty("kind").GetString() == "winner", "The adjudication kind must be preserved.");
    Require(outcome.RootElement.GetProperty("winnerRoleId").GetString() == "white", "The adjudicated winner must be preserved.");
    Require(outcome.RootElement.GetProperty("reasonCode").GetString() == "disqualification", "The adjudication reason must be preserved.");
}
Require(observingPlayer.StateNotificationCount == 3, "Protocol P must notify the player about adjudication.");
Require(observingPlayer.LastObservation?.IsTerminal == true && observingPlayer.LastObservation.Outcome?.Content == adjudicatedForGui.Outcome.Content, "The player and GUI must receive the same adjudicated outcome.");
Require(externalGameMaster.LastObservation?.Outcome?.Content == adjudicatedForGui.Outcome.Content, "The game master and GUI must receive the same adjudicated outcome.");
var actionAfterAdjudication = RequireSuccess(await concierge.ApplyActionAsync(new(
    opened.SessionId,
    new ContractDocument("application/json", PonnukiSchemas.Action, """{"version":1,"type":"play","player":"black","x":1,"y":1}"""),
    adjudicatedForGui.Revision)));
Require(!actionAfterAdjudication.IsAccepted && actionAfterAdjudication.Rejection?.Code == "game-session-adjudicated", "Player actions must be rejected after adjudication.");

var completed = RequireSuccess(await coordinator.RequestAndExecuteCommandAsync(bound.BindingId));
Require(completed.Result.WasAccepted, "The end-session command must be accepted.");
Require(completed.Result.CommandName == GameOasisGameMasterCoordinator.EndSessionCommand, "The executed command name must be preserved.");
Require(completed.NotificationFailures.Count == 0, "The command result notification must succeed.");
Require(completed.EndFailures.Count == 0, "Automatic game-master participation end must succeed.");
Require(completed.PlayerEndFailures.Count == 1 && completed.PlayerEndFailures[0].BindingId == boundFailingPlayer.BindingId, "A player session-end failure must be reported with its binding ID.");
Require(completed.PlayerEndError is null, "The player end broadcast itself must complete.");
Require(externalGameMaster.NotificationCount == 4, "The game master must receive pause, resume, adjudication, and end results.");
Require(externalGameMaster.EndCount == 1, "The game master must receive participation end after closing the game.");
Require(observingPlayer.EndCount == 1, "The player must receive participation end before the game closes.");
Require(failingPlayer.EndCount == 1, "The failing player must still receive one participation-end attempt.");
var playerAfterAutomaticUnbind = await playerCoordinator.UnbindPlayerAsync(boundPlayer.BindingId, "must-already-be-ended");
Require(!playerAfterAutomaticUnbind.IsSuccess && playerAfterAutomaticUnbind.Error?.Code == "player-binding-not-found", "Closing the game must remove its player binding.");
var failingPlayerAfterAutomaticUnbind = await playerCoordinator.UnbindPlayerAsync(boundFailingPlayer.BindingId, "must-not-remain-as-ghost");
Require(!failingPlayerAfterAutomaticUnbind.IsSuccess && failingPlayerAfterAutomaticUnbind.Error?.Code == "player-binding-not-found", "A failed end notification must not leave a ghost player binding.");

var afterClose = await concierge.GetSnapshotAsync(opened.SessionId);
Require(!afterClose.IsSuccess && afterClose.Error?.Code == "game-oasis-session-not-found", "The game master command must close the Game Oasis session.");
var afterAutomaticUnbind = await coordinator.UnbindGameMasterAsync(bound.BindingId, "must-already-be-ended");
Require(!afterAutomaticUnbind.IsSuccess && afterAutomaticUnbind.Error?.Code == "game-master-binding-not-found", "Closing the game must remove its game-master binding.");
RequireSuccess(await concierge.UnregisterPlaySpaceAsync(playSpace.Descriptor.TypeId));

Console.WriteLine("PASS: Protocol M paused, resumed, adjudicated, notified participants, and ended players/game masters without ghost bindings.");
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
    public static readonly GameMasterEngineId EngineId = new(GameOasisOfficialNames.Root + ".tests.scripted-game-master");

    public int StartCount { get; private set; }
    public int NotificationCount { get; private set; }
    public int EndCount { get; private set; }
    public GameMasterGameObservation? LastObservation { get; private set; }
    private readonly Queue<string> _commands = new([
        GameOasisGameMasterCoordinator.PauseCommand,
        GameOasisGameMasterCoordinator.ResumeCommand,
        GameOasisGameMasterCoordinator.AdjudicateCommand,
        GameOasisGameMasterCoordinator.EndSessionCommand]);

    public ValueTask<ProtocolResponse<GameMasterEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(ProtocolResponse<GameMasterEngineDescriptor>.Success(new(
            EngineId,
            "Scripted game master",
            ContractVersion.V1_0,
            nameof(ScriptedGameMaster),
            "1.0.0",
            [
                GameOasisGameMasterCoordinator.PauseCommand,
                GameOasisGameMasterCoordinator.ResumeCommand,
                GameOasisGameMasterCoordinator.AdjudicateCommand,
                GameOasisGameMasterCoordinator.EndSessionCommand,
            ])));
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
        var command = _commands.Dequeue();
        var parameters = command == GameOasisGameMasterCoordinator.AdjudicateCommand
            ? GameOasisAdjudicationDocuments.CreateResult(
                GameOasisAdjudicationKind.Winner,
                "disqualification",
                "white",
                "Black was disqualified by the game master.")
            : null;
        return ValueTask.FromResult(ProtocolResponse<GameMasterCommandSelected>.Success(new(
            request.BindingId,
            request.Observation.Revision,
            request.Observation.OperationRevision,
            new GameMasterCommand(
                command,
                command == GameOasisGameMasterCoordinator.EndSessionCommand ? "broadcast-finished" : "operator-request",
                parameters))));
    }

    public ValueTask<ProtocolResponse<GameMasterCommandNotified>> NotifyCommandAsync(
        GameMasterCommandNotification notification,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NotificationCount++;
        LastObservation = notification.Observation;
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

internal sealed class ObservingPlayer : IPlayerProtocol
{
    private readonly PlayerEngineId _engineId;
    private readonly bool _failEnd;

    public ObservingPlayer(
        string engineId = GameOasisOfficialNames.Root + ".tests.observing-player",
        bool failEnd = false)
    {
        _engineId = new(engineId);
        _failEnd = failEnd;
    }

    public int StateNotificationCount { get; private set; }
    public int EndCount { get; private set; }
    public PlayerGameObservation? LastObservation { get; private set; }

    public ValueTask<ProtocolResponse<PlayerEngineDescriptor>> DescribeAsync(CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<PlayerEngineDescriptor>.Success(new(
            _engineId,
            "Observing player",
            ContractVersion.V1_0,
            nameof(ObservingPlayer),
            "1.0.0",
            [new PlaySpaceTypeId(GameOasisOfficialNames.Ponnuki)],
            ["state-notification"])));

    public ValueTask<ProtocolResponse<PlayerSessionStarted>> StartSessionAsync(
        PlayerSessionStartRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<PlayerSessionStarted>.Success(new(request.BindingId)));

    public ValueTask<ProtocolResponse<PlayerActionSelected>> SelectActionAsync(
        PlayerActionRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<PlayerActionSelected>.Failure(new(
            "not-scripted",
            "This observing player does not select actions.")));

    public ValueTask<ProtocolResponse<PlayerActionNotified>> NotifyActionAsync(
        PlayerActionNotification notification,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ProtocolResponse<PlayerActionNotified>.Success(new(
            notification.BindingId,
            notification.Observation.Revision)));

    public ValueTask<ProtocolResponse<PlayerStateNotified>> NotifyStateAsync(
        PlayerStateNotification notification,
        CancellationToken cancellationToken = default)
    {
        StateNotificationCount++;
        LastObservation = notification.Observation;
        return ValueTask.FromResult(ProtocolResponse<PlayerStateNotified>.Success(new(
            notification.BindingId,
            notification.Observation.Revision,
            notification.Observation.OperationRevision)));
    }

    public ValueTask<ProtocolResponse<PlayerSessionEnded>> EndSessionAsync(
        PlayerSessionEndRequest request,
        CancellationToken cancellationToken = default)
    {
        EndCount++;
        LastObservation = request.FinalObservation;
        if (_failEnd)
            return ValueTask.FromResult(ProtocolResponse<PlayerSessionEnded>.Failure(new(
                "simulated-player-end-failure",
                "The smoke player intentionally rejected participation end.")));
        return ValueTask.FromResult(ProtocolResponse<PlayerSessionEnded>.Success(new(request.BindingId)));
    }
}
