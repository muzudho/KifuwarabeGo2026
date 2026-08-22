namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ゲーム設定の検証結果です。</summary>
/// <param name="IsValid">セッション生成に利用できる設定か。</param>
/// <param name="Issues">検出された問題。正常時は空です。</param>
public sealed record PlaySpaceConfigurationValidation(
    bool IsValid,
    IReadOnlyList<ProtocolError> Issues);

/// <summary>セッション生成結果です。</summary>
public sealed record PlaySpaceSessionCreated(
    PlaySpaceSessionId SessionId,
    PlaySpaceSnapshot InitialSnapshot);

/// <summary>特定リビジョン時点のゲーム固有状態です。</summary>
public sealed record PlaySpaceSnapshot(
    PlaySpaceSessionId SessionId,
    long Revision,
    ContractDocument State,
    bool IsTerminal,
    ContractDocument? Outcome);

/// <summary>行動適用の結果です。</summary>
public sealed record PlaySpaceActionApplied(
    bool IsAccepted,
    PlaySpaceSnapshot Snapshot,
    IReadOnlyList<ContractDocument> Events,
    ProtocolError? Rejection);

/// <summary>セッション破棄の結果です。</summary>
public sealed record PlaySpaceSessionClosed(PlaySpaceSessionId SessionId);
