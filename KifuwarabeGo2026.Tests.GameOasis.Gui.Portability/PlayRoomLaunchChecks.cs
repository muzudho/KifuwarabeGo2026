namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;
using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.GameOasis.Gui.Sgf;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows;
using System;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

internal static class PlayRoomLaunchChecks
{
    public static void Run()
    {
        Require(typeof(IPlayRoomLauncher).Namespace == "KifuwarabeGo2026.PlayRoom.Launching",
            "The play-room launch boundary must be identifiable by its namespace.");
        Require(typeof(IPlayRoomLauncher).Assembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayRoomGui.Common" &&
                typeof(InProcessPlayRoomLauncher).Assembly == typeof(IPlayRoomLauncher).Assembly,
            "The game-neutral play-room launcher boundary must be owned by Reference.PlayRoomGui.Common.");
        Require(typeof(IPlayRoomProcessLauncher).Assembly == typeof(IPlayRoomLauncher).Assembly &&
                typeof(ProcessPlayRoomLauncher).Assembly == typeof(IPlayRoomLauncher).Assembly,
            "The Lobby-independent process launcher contract and adapter must be owned by Reference.PlayRoomGui.Common.");
        var goCompositionAssembly = typeof(GoPlayRoomComposition).Assembly;
        Require(goCompositionAssembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayRoomGui.Go" &&
                !goCompositionAssembly.GetReferencedAssemblies().Any(reference =>
                    reference.Name is "KifuwarabeGo2026.LobbyGui" or "KifuwarabeGo2026.GameOasis.Gui"),
            "The Go play-room composition must be owned by Reference.PlayRoomGui.Go without Lobby or compatibility-GUI references.");
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

        var composedLauncher = GoPlayRoomComposition.CreateInProcessLauncher(
            launchRequest => PlayRoomLaunchResult.Started(launchRequest.RequestId, "match"),
            launchRequest => PlayRoomLaunchResult.Started(launchRequest.RequestId, "editor"),
            launchRequest => PlayRoomLaunchResult.Started(launchRequest.RequestId, "review"));
        Require(composedLauncher.Launch(CreateRequest("go-match", PlayRoomIds.Match, GameOasisOfficialNames.Go)).SessionId == "match" &&
                composedLauncher.Launch(CreateRequest("go-editor", PlayRoomIds.BoardEditor, GameOasisOfficialNames.Go)).SessionId == "editor" &&
                composedLauncher.Launch(CreateRequest("go-review", PlayRoomIds.Review, GameOasisOfficialNames.Go)).SessionId == "review",
            "The Lobby-independent Go composition did not register all Go play-room handlers.");

        VerifySavedGoLaunchRequest();
        VerifySavedGoReviewLaunchRequest();
        VerifyGoWindowsHostStartup();
        VerifyProcessLauncher();
        VerifyLocalMatchProcessCoordinator();
    }

    private static void VerifyLocalMatchProcessCoordinator()
    {
        var launcher = new ControllableProcessLauncher();
        var coordinator = new LocalMatchProcessLaunchCoordinator(launcher);
        var request = CreateSavedMatchRequest("coordinated-process-request");

        var started = coordinator.Start(request);
        Require(started.Status == PlayRoomLaunchStatus.Started && coordinator.IsRunning,
            "The Local Match process coordinator did not accept an asynchronous launch.");
        Require(!coordinator.TryTakeCompletion(out _),
            "The Local Match process coordinator blocked or completed before its child Host result.");

        var duplicate = coordinator.Start(request with { RequestId = "duplicate-process-request" });
        Require(duplicate.Status == PlayRoomLaunchStatus.Rejected &&
                duplicate.ErrorCode == "play-room-host-already-running",
            "The Local Match process coordinator accepted a second child Host while one was running.");

        launcher.Complete(new(PlayRoomProcessCompletionStatus.ExitedNormally, request.RequestId, 0));
        Require(coordinator.TryTakeCompletion(out var completion) &&
                completion is { IsNormalExit: true, ExitCode: 0 } &&
                !coordinator.IsRunning,
            "The Local Match process coordinator did not return to an idle Lobby state after normal exit.");
    }

    private static void VerifyProcessLauncher()
    {
        var requestDirectory = Path.Combine(Path.GetTempPath(), $"kifuwarabe-process-launch-{Guid.NewGuid():N}");
        try
        {
            var launcher = new ProcessPlayRoomLauncher(
                _ => CreateCurrentTestProcessStartInfo("--play-room-child-normal-exit"),
                requestDirectory);
            var request = CreateSavedMatchRequest("process-launch-request");
            var result = launcher.LaunchAsync(request).GetAwaiter().GetResult();
            Require(result is
                {
                    Status: PlayRoomProcessCompletionStatus.ExitedNormally,
                    RequestId: "process-launch-request",
                    ExitCode: 0,
                    IsNormalExit: true,
                }, "The process Play Room launcher did not report the child Host's normal exit.");
            Require(!Directory.EnumerateFiles(requestDirectory).Any(),
                "The process Play Room launcher did not remove its saved launch request.");

            var unsupported = launcher.LaunchAsync(request with { Version = 2 }).GetAwaiter().GetResult();
            Require(unsupported.Status == PlayRoomProcessCompletionStatus.StartFailed &&
                    unsupported.ErrorCode == "unsupported-launch-version",
                "The process Play Room launcher accepted an unsupported launch contract version.");

            var missingHost = new ProcessPlayRoomLauncher(
                _ => new ProcessStartInfo(Path.Combine(requestDirectory, "missing-play-room-host.exe")),
                requestDirectory);
            var failed = missingHost.LaunchAsync(request).GetAwaiter().GetResult();
            Require(failed.Status == PlayRoomProcessCompletionStatus.StartFailed &&
                    failed.ErrorCode == "play-room-host-start-failed" &&
                    !Directory.EnumerateFiles(requestDirectory).Any(),
                "The process Play Room launcher did not structure a Host start failure or clean its request file.");
        }
        finally
        {
            if (Directory.Exists(requestDirectory)) Directory.Delete(requestDirectory, recursive: true);
        }
    }

    private static ProcessStartInfo CreateCurrentTestProcessStartInfo(string firstArgument)
    {
        var startInfo = new ProcessStartInfo("dotnet");
        startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);
        startInfo.ArgumentList.Add(firstArgument);
        return startInfo;
    }

    private static void VerifyGoWindowsHostStartup()
    {
        var assembly = typeof(GoPlayRoomHostStartup).Assembly;
        var references = assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToHashSet(StringComparer.Ordinal);
        Require(assembly.GetName().Name == "KifuwarabeGo2026.Reference.PlayRoomGui.Go.Windows" &&
                references.Contains("KifuwarabeGo2026.Reference.PlayRoomGui.Go") &&
                references.Contains("KifuwarabeGo2026.Reference.PlayRoomGui.Go.MonoGame") &&
                !references.Contains("KifuwarabeGo2026.GameOasis.Gui") &&
                !references.Contains("KifuwarabeGo2026.LobbyGui"),
            "The Go Play Room Windows Host must use its MonoGame adapter without depending on the compatibility GUI or Lobby GUI.");

        var request = CreateSavedMatchRequest("windows-host-request");
        var path = Path.Combine(Path.GetTempPath(), $"kifuwarabe-go-host-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, JsonSerializer.Serialize(request));
            var ready = GoPlayRoomHostStartup.Load(["--launch-request", path]);
            Require(ready is { IsReady: true, ExitCode: GoPlayRoomHostExitCodes.Success, Code: "ready" } &&
                    ready.Plan is { RequestId: "windows-host-request", RoomTypeId: PlayRoomIds.Match, BoardSize: 9 },
                $"The Go Play Room Windows Host did not accept a saved Local Match request: {ready.Code}: {ready.Message}");

            File.WriteAllText(path, "not json");
            var unreadable = GoPlayRoomHostStartup.Load(["--launch-request", path]);
            Require(unreadable.ExitCode == GoPlayRoomHostExitCodes.RequestReadFailed && !unreadable.IsReady,
                "The Go Play Room Windows Host did not reject malformed JSON with the read-failure exit code.");

            File.WriteAllText(path, JsonSerializer.Serialize(request with { RoomTypeId = PlayRoomIds.Review }));
            var unsupported = GoPlayRoomHostStartup.Load(["--launch-request", path]);
            Require(unsupported.ExitCode == GoPlayRoomHostExitCodes.RequestRejected &&
                    unsupported.Code == "unsupported-host-room-type",
                "The first Go Play Room Windows Host slice accepted a non-Match request.");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }

        var invalidArguments = GoPlayRoomHostStartup.Load([]);
        Require(invalidArguments.ExitCode == GoPlayRoomHostExitCodes.InvalidArguments,
            "The Go Play Room Windows Host did not reject missing command-line arguments.");
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
        var original = CreateSavedMatchRequest("saved-go-request");

        var savedJson = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PlayRoomLaunchRequest>(savedJson)
            ?? throw new InvalidOperationException("The saved Go launch request was not restored.");
        Require(GoPlayRoomLaunchInterpreter.TryCreate(restored, out var plan, out var errorCode, out var message) && plan is not null,
            $"The restored Go launch request was not interpreted: {errorCode}: {message}");
        var launchPlan = plan ?? throw new InvalidOperationException("The restored Go launch plan was null.");
        Require(launchPlan.BoardSize == 9 && launchPlan.Komi == 7.5m && launchPlan.StartingPlayer == GoStone.White &&
                launchPlan.SetupStones.Count == 2 && launchPlan.MainTime == TimeSpan.FromSeconds(90) &&
                launchPlan.Participants.Count == 2 && launchPlan.PlayerConnections.Count == 1 &&
                launchPlan.PlayerConnections[0].ExecutablePath == "engines/white.exe",
            "The restored Go launch plan did not preserve configuration and participant data.");

        var session = new GoAppSession();
        Require(session.TryApplyPlayRoomLaunchPlan(launchPlan, out var warning),
            $"The restored Go launch plan was not applied to a fresh play-room session: {warning}");
        var view = session.CreatePlayRoomViewState();
        Require(view.BoardSize == 9 && view.CurrentTurn == GoStone.White &&
                view.GetStone(2, 3) == GoStone.Black && view.GetStone(4, 5) == GoStone.White &&
                session.Komi == 7.5m && session.MainTime == TimeSpan.FromSeconds(90) &&
                session.GetPlayerKind(GoStone.White) == GoPlayerKind.Computer &&
                session.GetGtpEngineProfile(GoStone.White).ExecutablePath == "engines/white.exe" &&
                session.GetGtpEngineProfile(GoStone.White).Arguments == "--gtp",
            "A fresh Go play-room session did not start from the restored launch request configuration.");

        var wrongGame = restored with { GameId = GameOasisOfficialNames.Ponnuki };
        Require(!GoPlayRoomLaunchInterpreter.TryCreate(wrongGame, out _, out errorCode, out _) &&
                errorCode == "unsupported-go-launch-game",
            "The Go launch interpreter accepted a request for another game.");
    }

    private static PlayRoomLaunchRequest CreateSavedMatchRequest(string requestId)
    {
        var configuration = new ContractDocument(
            "application/json",
            GameOasisOfficialNames.Go + ".configuration.v1",
            """{"version":1,"boardSize":9,"komi":7.5,"ruleset":"chinese-area","startingPlayer":"white","setupStones":[{"x":2,"y":3,"color":"black"},{"x":4,"y":5,"color":"white"}],"mainTimeMilliseconds":90000}""");
        return new PlayRoomLaunchRequest(
            1,
            requestId,
            PlayRoomIds.Match,
            GameOasisOfficialNames.Go,
            new PlaySpaceTypeId(GameOasisOfficialNames.Go),
            configuration,
            null,
            [
                new PlayRoomParticipant("black", "entry-black", "BLACK", "human", "", null),
                new PlayRoomParticipant(
                    "white",
                    "entry-white",
                    "WHITE ENGINE",
                    "computer",
                    "engine-white",
                    new ContractDocument("application/json", GameOasisOfficialNames.Root + ".gtp-engine-options.v1", "{\"random-move\":\"star\"}"),
                    new ContractDocument(
                        "application/json",
                        PlayerConnectionSchemas.GtpProcessV1,
                        "{\"executablePath\":\"engines/white.exe\",\"workingDirectory\":\"engines\",\"arguments\":\"--gtp\",\"enableGtpLog\":true,\"initialPositionProfileId\":\"auto\"}")),
            ]);
    }

    private static PlayRoomLaunchRequest CreateRequest(string requestId, string roomTypeId, string gameId) =>
        new(1, requestId, roomTypeId, gameId, new PlaySpaceTypeId(gameId),
            new ContractDocument("application/json", gameId + ".configuration.v1", "{}"), null, []);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ControllableProcessLauncher : IPlayRoomProcessLauncher
    {
        private readonly TaskCompletionSource<PlayRoomProcessCompletionResult> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<PlayRoomProcessCompletionResult> LaunchAsync(
            PlayRoomLaunchRequest request,
            CancellationToken cancellationToken = default) => _completion.Task;

        public void Complete(PlayRoomProcessCompletionResult result) => _completion.SetResult(result);
    }
}
