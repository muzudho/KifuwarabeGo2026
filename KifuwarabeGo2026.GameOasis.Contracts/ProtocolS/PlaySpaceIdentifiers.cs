namespace KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>生成されたプレイスペースセッションを識別するIDです。</summary>
public readonly record struct PlaySpaceSessionId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
