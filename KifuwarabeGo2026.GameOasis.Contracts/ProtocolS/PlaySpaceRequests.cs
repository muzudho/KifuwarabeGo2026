namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ゲーム設定を検証する要求です。</summary>
public sealed record ValidatePlaySpaceConfigurationRequest(ContractDocument Configuration);

/// <summary>設定から新しいプレイスペースセッションを生成する要求です。</summary>
public sealed record CreatePlaySpaceSessionRequest(ContractDocument Configuration);

/// <summary>現在状態を取得する要求です。</summary>
public sealed record GetPlaySpaceSnapshotRequest(PlaySpaceSessionId SessionId);

/// <summary>プレイヤーまたはゲームマスターの行動を適用する要求です。</summary>
public sealed record ApplyPlaySpaceActionRequest(
    PlaySpaceSessionId SessionId,
    ContractDocument Action,
    long ExpectedRevision);

/// <summary>プレイスペースセッションを破棄する要求です。</summary>
public sealed record ClosePlaySpaceSessionRequest(PlaySpaceSessionId SessionId);
