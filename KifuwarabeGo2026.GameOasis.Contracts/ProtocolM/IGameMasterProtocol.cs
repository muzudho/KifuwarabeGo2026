namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolM;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>ゲームコンシェルジュと人間またはコンピューターのゲームマスター実装を接続するProtocol Mです。</summary>
public interface IGameMasterProtocol
{
    /// <summary>ゲームマスター実装の識別情報と能力を取得します。</summary>
    ValueTask<ProtocolResponse<GameMasterEngineDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>一つのゲームへの運営参加を開始します。</summary>
    ValueTask<ProtocolResponse<GameMasterSessionStarted>> StartSessionAsync(
        GameMasterSessionStartRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>現在状態に対する運営命令を一つ選ぶよう要求します。</summary>
    ValueTask<ProtocolResponse<GameMasterCommandSelected>> SelectCommandAsync(
        GameMasterCommandRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>運営命令の実行結果を通知します。</summary>
    ValueTask<ProtocolResponse<GameMasterCommandNotified>> NotifyCommandAsync(
        GameMasterCommandNotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>ゲームへの運営参加を終了します。</summary>
    ValueTask<ProtocolResponse<GameMasterSessionEnded>> EndSessionAsync(
        GameMasterSessionEndRequest request,
        CancellationToken cancellationToken = default);
}
