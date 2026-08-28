namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.PlayRoom.Launching;
using System;

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
    }

    private static PlayRoomLaunchRequest CreateRequest(string requestId, string roomTypeId, string gameId) =>
        new(1, requestId, roomTypeId, gameId, new PlaySpaceTypeId(gameId),
            new ContractDocument("application/json", gameId + ".configuration.v1", "{}"), null, []);

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
