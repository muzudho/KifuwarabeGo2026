namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>
/// 境界を越えて受け渡す、形式を自己記述するテキスト文書です。
/// ゲーム固有型をContractsへ持ち込まず、JSONなどのスキーマを独立して発展させられます。
/// </summary>
/// <param name="MediaType">`application/json`などのメディアタイプ。</param>
/// <param name="SchemaId">文書の意味と版を識別する安定したID。</param>
/// <param name="Content">指定された形式の文書内容。</param>
public sealed record ContractDocument(string MediaType, string SchemaId, string Content);
