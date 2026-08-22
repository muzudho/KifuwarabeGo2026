namespace KifuwarabeGo2026.GameOasis.Concierge;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

/// <summary>プレイヤーエンジンが登録された結果です。</summary>
public sealed record PlayerRegistered(PlayerEngineDescriptor Descriptor);

/// <summary>プレイヤーエンジンがゲーム内の役割へ割り当てられた結果です。</summary>
public sealed record PlayerBound(
    PlayerBindingId BindingId,
    PlayerEngineId PlayerEngineId,
    GameOasisSessionId SessionId,
    string RoleId);

/// <summary>プレイヤー割り当ての終了結果です。</summary>
public sealed record PlayerUnbound(PlayerBindingId BindingId);

/// <summary>行動適用後のプレイヤー通知失敗です。</summary>
public sealed record PlayerNotificationFailure(
    PlayerBindingId BindingId,
    ProtocolError Error);

/// <summary>プレイヤーへ要求した一手を適用し、参加者へ通知した結果です。</summary>
public sealed record PlayerTurnCompleted(
    PlayerBindingId ActingBindingId,
    GameOasisActionApplied Applied,
    IReadOnlyList<PlayerNotificationFailure> NotificationFailures);
