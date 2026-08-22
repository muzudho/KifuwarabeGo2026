namespace KifuwarabeGo2026.GameOasis.Concierge;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>プレイスペースが登録された結果です。</summary>
public sealed record PlaySpaceRegistered(PlaySpaceDescriptor Descriptor);

/// <summary>Game Oasisセッションが開始された結果です。</summary>
public sealed record GameOasisSessionOpened(
    GameOasisSessionId SessionId,
    PlaySpaceDescriptor PlaySpace,
    GameOasisSnapshot InitialSnapshot);

/// <summary>コンシェルジュが公開する、Protocol Sの内部IDを含まない状態です。</summary>
public sealed record GameOasisSnapshot(
    GameOasisSessionId SessionId,
    PlaySpaceTypeId PlaySpaceTypeId,
    long Revision,
    long OperationRevision,
    GameOasisOperationalState OperationalState,
    ContractDocument State,
    bool IsTerminal,
    ContractDocument? Outcome);

/// <summary>コンシェルジュによる行動適用結果です。</summary>
public sealed record GameOasisActionApplied(
    bool IsAccepted,
    GameOasisSnapshot Snapshot,
    IReadOnlyList<ContractDocument> Events,
    ProtocolError? Rejection);

/// <summary>登録解除の結果です。</summary>
public sealed record PlaySpaceUnregistered(PlaySpaceTypeId TypeId);

/// <summary>Game Oasisセッション終了の結果です。</summary>
public sealed record GameOasisSessionClosed(GameOasisSessionId SessionId);

/// <summary>コンシェルジュへ行動適用を依頼します。</summary>
public sealed record ApplyGameOasisActionRequest(
    GameOasisSessionId SessionId,
    ContractDocument Action,
    long ExpectedRevision);

/// <summary>コンシェルジュが所有するゲーム運営状態の変更要求です。</summary>
public sealed record ApplyGameOasisOperationRequest(
    GameOasisSessionId SessionId,
    string OperationName,
    long ExpectedOperationRevision,
    ContractDocument? Parameters = null);

/// <summary>ゲーム運営状態の変更結果です。</summary>
public sealed record GameOasisOperationApplied(
    bool IsAccepted,
    GameOasisSnapshot Snapshot,
    ProtocolError? Rejection);
