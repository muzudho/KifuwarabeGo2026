namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>プレイスペースの種類を表す、実装者が定める安定したIDです。</summary>
public readonly record struct PlaySpaceTypeId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>生成されたプレイスペースセッションを識別するIDです。</summary>
public readonly record struct PlaySpaceSessionId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
