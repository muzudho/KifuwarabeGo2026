namespace KifuwarabeGo2026.PlayRoom.Launching;

using KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;
using System;
using System.Collections.Generic;

/// <summary>起動要求を同一プロセスの既存プレイルームへ接続する段階移行用実装です。</summary>
public sealed class InProcessPlayRoomLauncher : IPlayRoomLauncher
{
    private readonly Dictionary<(string RoomTypeId, string GameId), Func<PlayRoomLaunchRequest, PlayRoomLaunchResult>> _handlers = new();

    public void Register(string roomTypeId, string gameId, Func<PlayRoomLaunchRequest, PlayRoomLaunchResult> handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomTypeId);
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        ArgumentNullException.ThrowIfNull(handler);
        if (!_handlers.TryAdd((roomTypeId, gameId), handler))
            throw new InvalidOperationException($"A play-room handler is already registered: {roomTypeId}/{gameId}");
    }

    public PlayRoomLaunchResult Launch(PlayRoomLaunchRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Version != 1)
            return PlayRoomLaunchResult.Rejected(request.RequestId, "unsupported-launch-version", "Only play-room launch version 1 is supported.");
        if (string.IsNullOrWhiteSpace(request.RequestId))
            return PlayRoomLaunchResult.Rejected("", "missing-request-id", "A play-room request ID is required.");
        if (!_handlers.TryGetValue((request.RoomTypeId, request.GameId), out var handler))
            return PlayRoomLaunchResult.Rejected(request.RequestId, "play-room-not-registered", $"No play-room is registered for {request.RoomTypeId}/{request.GameId}.");

        try { return handler(request); }
        catch (OperationCanceledException)
        {
            return new(PlayRoomLaunchStatus.Cancelled, request.RequestId, ErrorCode: "play-room-launch-cancelled", Message: "The play-room launch was cancelled.");
        }
        catch (Exception exception)
        {
            return PlayRoomLaunchResult.Failed(request.RequestId, "play-room-launch-failed", exception.Message);
        }
    }
}
