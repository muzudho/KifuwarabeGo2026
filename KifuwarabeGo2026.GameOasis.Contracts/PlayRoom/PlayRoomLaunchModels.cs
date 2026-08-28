namespace KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ロビーからプレイルームへ渡す、通信方式に依存しない起動要求です。</summary>
public sealed record PlayRoomLaunchRequest(
    int Version,
    string RequestId,
    string RoomTypeId,
    string GameId,
    PlaySpaceTypeId PlaySpaceTypeId,
    ContractDocument Configuration,
    ContractDocument? InitialPosition,
    IReadOnlyList<PlayRoomParticipant> Participants);

/// <summary>プレイルームへ参加する人間またはエンジンの公開記述です。</summary>
public sealed record PlayRoomParticipant(
    string RoleId,
    string EntryId,
    string DisplayName,
    string Kind,
    string EngineProfileId,
    ContractDocument? EngineOptions);

public enum PlayRoomLaunchStatus
{
    Started,
    Deferred,
    Rejected,
    Cancelled,
    Failed,
}

/// <summary>起動結果。別プロセス化後も同じ状態を返せる形にします。</summary>
public sealed record PlayRoomLaunchResult(
    PlayRoomLaunchStatus Status,
    string RequestId,
    string? SessionId = null,
    string? ErrorCode = null,
    string? Message = null)
{
    public bool IsAccepted => Status is PlayRoomLaunchStatus.Started or PlayRoomLaunchStatus.Deferred;

    public static PlayRoomLaunchResult Started(string requestId, string? sessionId = null) =>
        new(PlayRoomLaunchStatus.Started, requestId, sessionId);

    public static PlayRoomLaunchResult Deferred(string requestId, string message) =>
        new(PlayRoomLaunchStatus.Deferred, requestId, Message: message);

    public static PlayRoomLaunchResult Rejected(string requestId, string errorCode, string message) =>
        new(PlayRoomLaunchStatus.Rejected, requestId, ErrorCode: errorCode, Message: message);

    public static PlayRoomLaunchResult Failed(string requestId, string errorCode, string message) =>
        new(PlayRoomLaunchStatus.Failed, requestId, ErrorCode: errorCode, Message: message);
}

public static class PlayRoomIds
{
    public const string Match = "match";
    public const string BoardEditor = "board-editor";
    public const string Review = "review";
}
