namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using System.Text.Json;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

public sealed record GoLaunchSetupStone(GoPoint Point, GoStone Stone);

/// <summary>保存・転送可能な起動要求から得た、Lobby内部型に依存しない囲碁GUI開始Planです。</summary>
public sealed record GoPlayRoomLaunchPlan(
    string RequestId,
    string RoomTypeId,
    GoPlayRoomActivity Activity,
    int BoardSize,
    decimal Komi,
    string Ruleset,
    GoStone StartingPlayer,
    IReadOnlyList<GoLaunchSetupStone> SetupStones,
    TimeSpan MainTime,
    IReadOnlyList<PlayRoomParticipant> Participants);

public static class GoPlayRoomLaunchInterpreter
{
    public static bool TryCreate(
        PlayRoomLaunchRequest request,
        out GoPlayRoomLaunchPlan? plan,
        out string errorCode,
        out string message)
    {
        ArgumentNullException.ThrowIfNull(request);
        plan = null;
        errorCode = "";
        message = "";

        if (request.Version != 1)
            return Fail("unsupported-launch-version", "Only play-room launch version 1 is supported.", out errorCode, out message);
        if (!string.Equals(request.GameId, GameOasisOfficialNames.Go, StringComparison.Ordinal) ||
            request.PlaySpaceTypeId.Value != GameOasisOfficialNames.Go)
            return Fail("unsupported-go-launch-game", "The launch request is not for the reference Go play room.", out errorCode, out message);
        if (request.RoomTypeId is not (PlayRoomIds.Match or PlayRoomIds.BoardEditor or PlayRoomIds.Review))
            return Fail("unsupported-go-room-type", $"Room type '{request.RoomTypeId}' is not supported by the Go play room.", out errorCode, out message);
        if (request.Configuration.MediaType != "application/json" ||
            request.Configuration.SchemaId != GameOasisOfficialNames.Go + ".configuration.v1")
            return Fail("unsupported-go-configuration", "The launch request does not contain a supported Go configuration.", out errorCode, out message);

        try
        {
            using var document = JsonDocument.Parse(request.Configuration.Content);
            var root = document.RootElement;
            var version = root.GetProperty("version").GetInt32();
            var boardSize = root.GetProperty("boardSize").GetInt32();
            var komi = root.GetProperty("komi").GetDecimal();
            var ruleset = root.GetProperty("ruleset").GetString() ?? "";
            var startingPlayerText = root.GetProperty("startingPlayer").GetString();
            var mainTimeMilliseconds = root.TryGetProperty("mainTimeMilliseconds", out var mainTimeElement)
                ? mainTimeElement.GetInt64()
                : 0L;
            if (version != 1)
                return Fail("unsupported-go-configuration-version", "Only Go configuration version 1 is supported.", out errorCode, out message);
            if (boardSize is not (9 or 13 or 19))
                return Fail("invalid-go-board-size", "Go board size must be 9, 13, or 19.", out errorCode, out message);
            if (komi is < -100m or > 100m)
                return Fail("invalid-go-komi", "Go komi must be between -100 and 100.", out errorCode, out message);
            if (startingPlayerText is not ("black" or "white"))
                return Fail("invalid-go-starting-player", "Go starting player must be black or white.", out errorCode, out message);
            if (mainTimeMilliseconds < 0)
                return Fail("invalid-go-main-time", "Go main time cannot be negative.", out errorCode, out message);

            var setupStones = new List<GoLaunchSetupStone>();
            var occupied = new HashSet<GoPoint>();
            if (root.TryGetProperty("setupStones", out var setupElement))
            foreach (var value in setupElement.EnumerateArray())
            {
                var point = new GoPoint(value.GetProperty("x").GetInt32(), value.GetProperty("y").GetInt32());
                var color = value.GetProperty("color").GetString();
                if (point.X < 0 || point.X >= boardSize || point.Y < 0 || point.Y >= boardSize)
                    return Fail("invalid-go-setup-point", $"Setup point ({point.X},{point.Y}) is outside the board.", out errorCode, out message);
                if (!occupied.Add(point))
                    return Fail("duplicate-go-setup-point", $"Setup point ({point.X},{point.Y}) is duplicated.", out errorCode, out message);
                if (color is not ("black" or "white"))
                    return Fail("invalid-go-setup-stone", "A setup stone must be black or white.", out errorCode, out message);
                setupStones.Add(new(point, color == "black" ? GoStone.Black : GoStone.White));
            }

            var activity = request.RoomTypeId switch
            {
                PlayRoomIds.BoardEditor => GoPlayRoomActivity.BoardEditing,
                PlayRoomIds.Review => GoPlayRoomActivity.Reviewing,
                _ => GoPlayRoomActivity.Playing,
            };
            plan = new GoPlayRoomLaunchPlan(
                request.RequestId,
                request.RoomTypeId,
                activity,
                boardSize,
                komi,
                ruleset,
                startingPlayerText == "black" ? GoStone.Black : GoStone.White,
                setupStones,
                TimeSpan.FromMilliseconds(mainTimeMilliseconds),
                request.Participants.ToArray());
            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return Fail("invalid-go-configuration", exception.Message, out errorCode, out message);
        }
    }

    private static bool Fail(string code, string detail, out string errorCode, out string message)
    {
        errorCode = code;
        message = detail;
        return false;
    }
}
