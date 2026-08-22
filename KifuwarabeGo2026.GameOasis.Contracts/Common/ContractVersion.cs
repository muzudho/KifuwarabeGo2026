namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>製品バージョンとは独立して管理する境界契約のバージョンです。</summary>
public readonly record struct ContractVersion(int Major, int Minor)
{
    /// <summary>最初のGame Oasis公開契約バージョンです。</summary>
    public static ContractVersion V1_0 { get; } = new(1, 0);

    /// <summary>同じメジャーバージョンの契約を解釈できるか判定します。</summary>
    public bool IsCompatibleWith(ContractVersion other) => Major == other.Major;

    /// <inheritdoc />
    public override string ToString() => $"{Major}.{Minor}";
}
