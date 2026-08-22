namespace KifuwarabeGo2026.GameOasis.Concierge;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolS;

/// <summary>Game Oasisが公開するセッションIDです。</summary>
public readonly record struct GameOasisSessionId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>プレイスペースが登録された結果です。</summary>
public sealed record PlaySpaceRegistered(PlaySpaceDescriptor Descriptor);

/// <summary>Game Oasisセッションが開始された結果です。</summary>
public sealed record GameOasisSessionOpened(
    GameOasisSessionId SessionId,
    PlaySpaceDescriptor PlaySpace,
    PlaySpaceSnapshot InitialSnapshot);

/// <summary>登録解除の結果です。</summary>
public sealed record PlaySpaceUnregistered(PlaySpaceTypeId TypeId);

/// <summary>Game Oasisセッション終了の結果です。</summary>
public sealed record GameOasisSessionClosed(GameOasisSessionId SessionId);

/// <summary>コンシェルジュへ行動適用を依頼します。</summary>
public sealed record ApplyGameOasisActionRequest(
    GameOasisSessionId SessionId,
    ContractDocument Action,
    long ExpectedRevision);
