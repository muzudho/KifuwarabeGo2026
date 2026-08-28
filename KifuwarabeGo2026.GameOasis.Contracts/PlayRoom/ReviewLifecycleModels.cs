namespace KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>Reviewの読み取り専用棋譜上で表示位置を移動する要求です。</summary>
public sealed record ReviewNavigation(string SessionId, int MoveIndex);

public sealed record ReviewViewState(
    string SessionId,
    int MoveIndex,
    ContractDocument GameRecord);

/// <summary>表示中の位置を構造化局面文書としてロビーへ返す要求です。</summary>
public sealed record ReviewPositionSelection(
    string SessionId,
    int MoveIndex,
    ContractDocument Position);

public enum ReviewCompletionStatus
{
    PositionSelected,
    Closed,
}

public sealed record ReviewCompletion(
    string SessionId,
    ReviewCompletionStatus Status,
    int? MoveIndex = null,
    ContractDocument? Position = null);
