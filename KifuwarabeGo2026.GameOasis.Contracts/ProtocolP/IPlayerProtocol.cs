namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolP;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ゲームコンシェルジュと人間またはコンピューターのプレイヤー実装を接続するProtocol Pです。</summary>
public interface IPlayerProtocol
{
    /// <summary>プレイヤー実装の識別情報と能力を取得します。</summary>
    ValueTask<ProtocolResponse<PlayerEngineDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>一つのゲームと役割への参加を開始します。</summary>
    ValueTask<ProtocolResponse<PlayerSessionStarted>> StartSessionAsync(
        PlayerSessionStartRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>現在状態に対する行動を一つ選ぶよう要求します。</summary>
    ValueTask<ProtocolResponse<PlayerActionSelected>> SelectActionAsync(
        PlayerActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>適用された行動と更新状態を通知します。</summary>
    ValueTask<ProtocolResponse<PlayerActionNotified>> NotifyActionAsync(
        PlayerActionNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>停止、再開、裁定など、着手以外による状態変更を通知します。</summary>
    ValueTask<ProtocolResponse<PlayerStateNotified>> NotifyStateAsync(
        PlayerStateNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>ゲームへの参加を終了します。</summary>
    ValueTask<ProtocolResponse<PlayerSessionEnded>> EndSessionAsync(
        PlayerSessionEndRequest request,
        CancellationToken cancellationToken = default);
}
