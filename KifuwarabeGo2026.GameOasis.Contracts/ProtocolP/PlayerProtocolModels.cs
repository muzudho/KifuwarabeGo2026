namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>コンシェルジュが互換性と対応ゲームを確認するためのプレイヤー情報です。</summary>
public sealed record PlayerEngineDescriptor(
    PlayerEngineId EngineId,
    string DisplayName,
    ContractVersion ProtocolVersion,
    string ImplementationName,
    string ImplementationVersion,
    IReadOnlyList<PlaySpaceTypeId> SupportedPlaySpaces,
    IReadOnlyList<string> Capabilities);

/// <summary>プレイヤーへ渡す、ゲーム固有型を含まない観測状態です。</summary>
public sealed record PlayerGameObservation(
    GameOasisSessionId SessionId,
    PlaySpaceTypeId PlaySpaceTypeId,
    long Revision,
    long OperationRevision,
    GameOasisOperationalState OperationalState,
    ContractDocument State,
    bool IsTerminal,
    ContractDocument? Outcome);

/// <summary>プレイヤーをゲーム内の役割へ割り当てます。</summary>
public sealed record PlayerSessionStartRequest(
    PlayerBindingId BindingId,
    string RoleId,
    PlayerGameObservation InitialObservation);

/// <summary>プレイヤーが参加開始を受理した結果です。</summary>
public sealed record PlayerSessionStarted(PlayerBindingId BindingId);

/// <summary>現在の観測状態から一つの行動を選ぶよう要求します。</summary>
public sealed record PlayerActionRequest(
    PlayerBindingId BindingId,
    string RoleId,
    PlayerGameObservation Observation);

/// <summary>プレイヤーが選択したゲーム固有の行動です。</summary>
public sealed record PlayerActionSelected(
    PlayerBindingId BindingId,
    long BasedOnRevision,
    ContractDocument Action);

/// <summary>適用された行動と更新状態をプレイヤーへ通知します。</summary>
public sealed record PlayerActionNotification(
    PlayerBindingId BindingId,
    ContractDocument Action,
    bool WasAccepted,
    PlayerGameObservation Observation,
    IReadOnlyList<ContractDocument> Events,
    ProtocolError? Rejection);

/// <summary>行動通知を受理した結果です。</summary>
public sealed record PlayerActionNotified(PlayerBindingId BindingId, long Revision);

/// <summary>プレイヤーのゲーム参加を終了します。</summary>
public sealed record PlayerSessionEndRequest(
    PlayerBindingId BindingId,
    PlayerGameObservation FinalObservation,
    string Reason);

/// <summary>プレイヤーが参加終了を受理した結果です。</summary>
public sealed record PlayerSessionEnded(PlayerBindingId BindingId);
