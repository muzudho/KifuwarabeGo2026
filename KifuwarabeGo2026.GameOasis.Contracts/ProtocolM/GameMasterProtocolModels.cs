namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ゲームマスター実装の識別情報と能力です。</summary>
public sealed record GameMasterEngineDescriptor(
    GameMasterEngineId EngineId,
    string DisplayName,
    ContractVersion ProtocolVersion,
    string ImplementationName,
    string ImplementationVersion,
    IReadOnlyList<string> Capabilities);

/// <summary>ゲームマスターが運営判断に使用するゲーム状態です。</summary>
public sealed record GameMasterGameObservation(
    GameOasisSessionId SessionId,
    PlaySpaceTypeId PlaySpaceTypeId,
    long Revision,
    long OperationRevision,
    GameOasisOperationalState OperationalState,
    ContractDocument State,
    bool IsTerminal,
    ContractDocument? Outcome);

/// <summary>ゲームマスターをゲームへ割り当てる要求です。</summary>
public sealed record GameMasterSessionStartRequest(
    GameMasterBindingId BindingId,
    GameMasterGameObservation InitialObservation);

/// <summary>ゲームマスターが運営参加開始を受理した結果です。</summary>
public sealed record GameMasterSessionStarted(GameMasterBindingId BindingId);

/// <summary>現在状態に対する運営命令を求める要求です。</summary>
public sealed record GameMasterCommandRequest(
    GameMasterBindingId BindingId,
    GameMasterGameObservation Observation);

/// <summary>ゲームマスターが選択した運営命令です。</summary>
public sealed record GameMasterCommandSelected(
    GameMasterBindingId BindingId,
    long BasedOnRevision,
    long BasedOnOperationRevision,
    GameMasterCommand Command);

/// <summary>Game Oasis共通の運営命令です。</summary>
public sealed record GameMasterCommand(
    string Name,
    string Reason,
    ContractDocument? Parameters = null);

/// <summary>運営命令を実行した結果です。</summary>
public sealed record GameMasterCommandResult(
    GameMasterBindingId BindingId,
    GameOasisSessionId SessionId,
    string CommandName,
    bool WasAccepted,
    ProtocolError? Rejection);

/// <summary>ゲームマスターへ通知する運営命令の実行結果です。</summary>
public sealed record GameMasterCommandNotification(
    GameMasterBindingId BindingId,
    GameMasterCommandResult Result,
    GameMasterGameObservation Observation);

/// <summary>ゲームマスターが実行結果通知を受理した結果です。</summary>
public sealed record GameMasterCommandNotified(GameMasterBindingId BindingId);

/// <summary>ゲームマスターの運営参加を終了する要求です。</summary>
public sealed record GameMasterSessionEndRequest(
    GameMasterBindingId BindingId,
    GameMasterGameObservation FinalObservation,
    string Reason);

/// <summary>ゲームマスターが運営参加終了を受理した結果です。</summary>
public sealed record GameMasterSessionEnded(GameMasterBindingId BindingId);
