namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>プレイスペースの種類を表す、実装者が定める安定したIDです。</summary>
public readonly record struct PlaySpaceTypeId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>Game Oasisが利用者側へ公開するセッションIDです。</summary>
public readonly record struct GameOasisSessionId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
