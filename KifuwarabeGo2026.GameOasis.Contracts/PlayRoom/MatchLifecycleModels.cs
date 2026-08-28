namespace KifuwarabeGo2026.GameOasis.Contracts.PlayRoom;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>対局画面から権威ある対局進行側へ渡す、ゲーム非依存の意味的な操作です。</summary>
public enum MatchActionKind
{
    PlayPoint,
    Pass,
    Resign,
}

public sealed record MatchActionRequest(
    string SessionId,
    string ActionId,
    string PlayerRoleId,
    MatchActionKind Kind,
    int? X = null,
    int? Y = null);

public sealed record MatchActionAccepted(
    string SessionId,
    string ActionId);

/// <summary>Concierge / Play Space が確定した局面を対局画面へ投影する通知です。</summary>
public sealed record MatchStateUpdate(
    string SessionId,
    long Revision,
    ContractDocument State);

public sealed record MatchViewState(
    string SessionId,
    long Revision,
    ContractDocument State);

public sealed record MatchCompletionCommand(
    string SessionId,
    ContractDocument FinalState,
    string? WinnerRoleId = null,
    string? Reason = null);

public enum MatchCompletionStatus
{
    Finished,
    Closed,
}

public sealed record MatchCompletion(
    string SessionId,
    MatchCompletionStatus Status,
    ContractDocument? FinalState = null,
    string? WinnerRoleId = null,
    string? Reason = null);
