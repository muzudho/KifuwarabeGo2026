namespace KifuwarabeGo2026.GameOasis.Concierge;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

/// <summary>ゲームマスター実装が登録された結果です。</summary>
public sealed record GameMasterRegistered(GameMasterEngineDescriptor Descriptor);

/// <summary>ゲームマスター実装がゲームへ割り当てられた結果です。</summary>
public sealed record GameMasterBound(
    GameMasterBindingId BindingId,
    GameMasterEngineId GameMasterEngineId,
    GameOasisSessionId SessionId);

/// <summary>ゲームマスターの運営参加が終了した結果です。</summary>
public sealed record GameMasterUnbound(GameMasterBindingId BindingId);

/// <summary>運営命令の要求、実行、通知が完了した結果です。</summary>
public sealed record GameMasterTurnCompleted(
    GameMasterCommandResult Result,
    IReadOnlyList<GameMasterBindingId> NotificationFailures,
    IReadOnlyList<GameMasterBindingId> EndFailures,
    IReadOnlyList<PlayerBindingId> PlayerNotificationFailures,
    ProtocolError? PlayerBroadcastError);
