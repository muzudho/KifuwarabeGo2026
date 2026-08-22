namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>
/// GUIとゲームコンシェルジュを接続するProtocol Gです。
/// 描画座標やUI部品ではなく、意味を持つ操作と表示可能な状態を受け渡します。
/// </summary>
public interface IGuiProtocol
{
    /// <summary>選択可能なプレイスペースを取得します。</summary>
    ValueTask<ProtocolResponse<IReadOnlyList<GuiPlaySpaceEntry>>> GetPlaySpacesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>選択したプレイスペースの設定スキーマを取得します。</summary>
    ValueTask<ProtocolResponse<ContractDocument>> GetConfigurationSchemaAsync(
        PlaySpaceTypeId playSpaceTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>ゲーム設定から新しいGame Oasisセッションを開始します。</summary>
    ValueTask<ProtocolResponse<GuiSessionOpened>> OpenSessionAsync(
        GuiOpenSessionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>GUI向けの現在状態を取得します。</summary>
    ValueTask<ProtocolResponse<GuiGameSnapshot>> GetSnapshotAsync(
        GuiGetSnapshotRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>利用者の意味的な行動を送信します。</summary>
    ValueTask<ProtocolResponse<GuiActionSubmitted>> SubmitActionAsync(
        GuiSubmitActionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Game Oasisセッションを終了します。</summary>
    ValueTask<ProtocolResponse<GuiSessionClosed>> CloseSessionAsync(
        GuiCloseSessionRequest request,
        CancellationToken cancellationToken = default);
}
