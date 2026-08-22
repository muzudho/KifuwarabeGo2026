namespace KifuwarabeGo2026.GameOasis.Contracts.Common;

/// <summary>コンシェルジュが管理するゲームの運営状態です。</summary>
public enum GameOasisOperationalState
{
    /// <summary>プレイヤーの行動を受け付ける通常状態です。</summary>
    Running,

    /// <summary>ゲームマスターによって一時停止された状態です。</summary>
    Paused,
}

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

/// <summary>登録されたプレイヤーエンジンの安定したIDです。</summary>
public readonly record struct PlayerEngineId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>プレイヤーエンジンを一つのゲームと役割へ割り当てた参加IDです。</summary>
public readonly record struct PlayerBindingId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>登録されたゲームマスター実装の安定したIDです。</summary>
public readonly record struct GameMasterEngineId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}

/// <summary>ゲームマスター実装を一つのゲームへ割り当てた参加IDです。</summary>
public readonly record struct GameMasterBindingId(string Value)
{
    /// <inheritdoc />
    public override string ToString() => Value;
}
