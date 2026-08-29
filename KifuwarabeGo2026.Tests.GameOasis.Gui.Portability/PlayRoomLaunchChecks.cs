namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;
using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.GameOasis.Gui.Sgf;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using System;
using System.Text.Json;

internal static class PlayRoomLaunchChecks
{
    public static void Run()
    {
        Require(typeof(IPlayRoomLauncher).Namespace == "KifuwarabeGo2026.PlayRoom.Launching",
            "The play-room launch boundary must be identifiable by its namespace.");
        var launcher = new InProcessPlayRoomLauncher();
        PlayRoomLaunchRequest? received = null;
        launcher.Register(PlayRoomIds.Match, GameOasisOfficialNames.Go, request =>
        {
            received = request;
            return PlayRoomLaunchResult.Started(request.RequestId, "session-1");
        });

        var request = CreateRequest("request-1", PlayRoomIds.Match, GameOasisOfficialNames.Go);
        var result = launcher.Launch(request);
        Require(result.Status == PlayRoomLaunchStatus.Started, "Registered play-room request was not started.");
        Require(ReferenceEquals(request, received), "In-process adapter did not preserve the launch contract.");

        var missing = launcher.Launch(CreateRequest("request-2", PlayRoomIds.BoardEditor, GameOasisOfficialNames.Go));
        Require(missing.Status == PlayRoomLaunchStatus.Rejected && missing.ErrorCode == "play-room-not-registered",
            "Missing play-room handler did not produce a structured rejection.");

        var unsupported = launcher.Launch(request with { Version = 2, RequestId = "request-3" });
        Require(unsupported.Status == PlayRoomLaunchStatus.Rejected && unsupported.ErrorCode == "unsupported-launch-version",
            "Unsupported launch contract version was accepted.");

        VerifySavedGoLaunchRequest();
        VerifySavedGoReviewLaunchRequest();
    }

    private static void VerifySavedGoReviewLaunchRequest()
    {
        var record = new GoGameRecord
        {
            BoardSize = 9,
            Komi = 6.5m,
            TimeLimit = TimeSpan.FromMinutes(5),
            BlackPlayerName = "BLACK",
            WhitePlayerName = "WHITE",
            RootComment = "saved review",
        };
        record.Moves.Add(new GoGameMove(GoStone.Black, new GoPoint(2, 2)));
        record.Moves.Add(new GoGameMove(GoStone.White, new GoPoint(3, 3)));

        var request = PlayRoomLaunchRequestFactory.CreateReview(record);
        var restored = JsonSerializer.Deserialize<PlayRoomLaunchRequest>(JsonSerializer.Serialize(request))
            ?? throw new InvalidOperationException("The saved Go review request was not restored.");
        Require(GoPlayRoomLaunchInterpreter.TryCreate(restored, out var plan, out var errorCode, out var message) &&
                plan?.Activity == GoPlayRoomActivity.Reviewing && plan.InitialPosition is not null,
            $"The restored Go review request was not interpreted: {errorCode}: {message}");

        var restoredRecord = SgfGameRecordConverter.FromSgf(plan!.InitialPosition!.Content);
        var session = new GoAppSession();
        Require(session.StartReviewingGameRecord(restoredRecord, out var warning) &&
                session.CurrentMode.Kind == GoAppModeKind.Reviewing &&
                session.ReviewMoveCount == 2 &&
                session.ReviewRootComment == "saved review",
            $"The restored Go review request did not start a fresh review session: {warning}");

        var missingRecord = restored with { InitialPosition = null };
        Require(!GoPlayRoomLaunchInterpreter.TryCreate(missingRecord, out _, out errorCode, out _) &&
                errorCode == "missing-go-review-record",
            "The Go review launch interpreter accepted a request without an SGF record.");
    }

    private static void VerifySavedGoLaunchRequest()
    {
        var configuration = new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Go + ".configuration.v1",
            """{"version":1,"boardSize":9,"komi":7.5,"ruleset":"chinese-area","startingPlayer":"white","setupStones":[{"x":2,"y":3,"color":"black"},{"x":4,"y":5,"color":"white"}],"mainTimeMilliseconds":90000}""");
        var original = new PlayRoomLaunchRequest(
            1,
            "saved-go-request",
            PlayRoomIds.Match,
            GameOasisOfficialNames.Go,
            new PlaySpaceTypeId(GameOasisOfficialNames.Go),
            configuration,
            null,
            [new PlayRoomParticipant("black", "entry-black", "BLACK", "human", "", null)]);

        var savedJson = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PlayRoomLaunchRequest>(savedJson)
            ?? throw new InvalidOperationException("The saved Go launch request was not restored.");
        Require(GoPlayRoomLaunchInterpreter.TryCreate(restored, out var plan, out var errorCode, out var message) && plan is not null,
            $"The restored Go launch request was not interpreted: {errorCode}: {message}");
        var launchPlan = plan ?? throw new InvalidOperationException("The restored Go launch plan was null.");
        Require(launchPlan.BoardSize == 9 && launchPlan.Komi == 7.5m && launchPlan.StartingPlayer == GoStone.White &&
                launchPlan.SetupStones.Count == 2 && launchPlan.MainTime == TimeSpan.FromSeconds(90) &&
                launchPlan.Participants.Count == 1,
            "The restored Go launch plan did not preserve configuration and participant data.");

        var session = new GoAppSession();
        Require(session.TryApplyPlayRoomLaunchPlan(launchPlan, out var warning),
            $"The restored Go launch plan was not applied to a fresh play-room session: {warning}");
        var view = session.CreatePlayRoomViewState();
        Require(view.BoardSize == 9 && view.CurrentTurn == GoStone.White &&
                view.GetStone(2, 3) == GoStone.Black && view.GetStone(4, 5) == GoStone.White &&
                session.Komi == 7.5m && session.MainTime == TimeSpan.FromSeconds(90),
            "A fresh Go play-room session did not start from the restored launch request configuration.");

        var wrongGame = restored with { GameId = GameOasisOfficialNames.Ponnuki };
        Require(!GoPlayRoomLaunchInterpreter.TryCreate(wrongGame, out _, out errorCode, out _) &&
                errorCode == "unsupported-go-launch-game",
            "The Go launch interpreter accepted a request for another game.");
    }

    private static PlayRoomLaunchRequest CreateRequest(string requestId, string roomTypeId, string gameId) =>
        new(1, requestId, roomTypeId, gameId, new PlaySpaceTypeId(gameId),
            new ContractDocument("application/json", gameId + ".configuration.v1", "{}"), null, []);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
