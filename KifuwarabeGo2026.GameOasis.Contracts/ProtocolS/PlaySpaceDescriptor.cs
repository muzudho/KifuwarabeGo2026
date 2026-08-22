namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

using KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>コンシェルジュが実装を選択し、互換性を確認するための自己記述情報です。</summary>
/// <param name="TypeId">プレイスペース種別の安定したID。</param>
/// <param name="DisplayName">人間向けの表示名。</param>
/// <param name="ProtocolVersion">実装するProtocol Sの契約バージョン。</param>
/// <param name="ImplementationName">実装製品またはパッケージの名前。</param>
/// <param name="ImplementationVersion">実装側のバージョン。</param>
/// <param name="Capabilities">対応機能を表す安定したIDの一覧。</param>
public sealed record PlaySpaceDescriptor(
    PlaySpaceTypeId TypeId,
    string DisplayName,
    ContractVersion ProtocolVersion,
    string ImplementationName,
    string ImplementationVersion,
    IReadOnlyList<string> Capabilities);
