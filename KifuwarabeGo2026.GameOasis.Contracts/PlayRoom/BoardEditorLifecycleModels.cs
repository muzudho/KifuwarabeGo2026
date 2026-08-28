namespace KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>独立プレイルームが起動要求を受理し、操作可能になったことを示します。</summary>
public sealed record PlayRoomReady(string RequestId, string SessionId, string RoomTypeId);

public enum BoardEditorCompletionStatus
{
    Adopted,
    Discarded,
    Closed,
}

/// <summary>Board Editorからロビーへ返す、採用または破棄の結果です。</summary>
public sealed record BoardEditorCompletion(
    string SessionId,
    BoardEditorCompletionStatus Status,
    ContractDocument? Position = null);

public sealed record BoardEditorPositionUpdate(string SessionId, ContractDocument Position);
public sealed record PlayRoomSessionCommand(string SessionId);
