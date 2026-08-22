namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>境界を越えて通知できる、機械判定可能なエラーです。</summary>
/// <param name="Code">実装間で合意する安定したエラーコード。</param>
/// <param name="Message">人間向けの診断メッセージ。</param>
/// <param name="Details">任意の構造化された詳細情報。</param>
public sealed record ProtocolError(
    string Code,
    string Message,
    ContractDocument? Details = null);
