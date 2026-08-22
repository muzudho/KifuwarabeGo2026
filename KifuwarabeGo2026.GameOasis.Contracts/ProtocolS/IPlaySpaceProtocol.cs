namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>
/// ゲームコンシェルジュと独立したプレイスペース実装を接続するProtocol Sです。
/// 同一プロセス実装と通信アダプターのどちらでも実装できます。
/// </summary>
public interface IPlaySpaceProtocol
{
    /// <summary>実装と対応能力を取得します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceDescriptor>> DescribeAsync(
        CancellationToken cancellationToken = default);

    /// <summary>ゲーム設定のスキーマを取得します。</summary>
    ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(
        CancellationToken cancellationToken = default);

    /// <summary>ゲーム設定を検証します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceConfigurationValidation>> ValidateConfigurationAsync(
        ValidatePlaySpaceConfigurationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>設定から新しいセッションを生成します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceSessionCreated>> CreateSessionAsync(
        CreatePlaySpaceSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>現在のゲーム状態を取得します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceSnapshot>> GetSnapshotAsync(
        GetPlaySpaceSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>行動を適用し、更新された状態とイベントを取得します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceActionApplied>> ApplyActionAsync(
        ApplyPlaySpaceActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>セッションを破棄します。</summary>
    ValueTask<ProtocolResponse<PlaySpaceSessionClosed>> CloseSessionAsync(
        ClosePlaySpaceSessionRequest request,
        CancellationToken cancellationToken = default);
}
