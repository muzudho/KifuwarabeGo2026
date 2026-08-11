namespace KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

public sealed record CgosConnectionProfile(
    string DisplayName,
    string Host,
    int Port,
    string Round,
    string Note)
{
    /// <summary>TargetProfile から参照するための不変 ID。</summary>
    public string Id { get; init; } = "";

    /// <summary>接続先のプロトコル種別。現状は CGOS だけだが、永続形式は将来の拡張に備える。</summary>
    public ConnectionProfileKind Kind { get; init; } = ConnectionProfileKind.Cgos;

    /// <summary>重複候補の検出に使う正規化済み endpoint。Player/Target の参照キーには使わない。</summary>
    public string EndpointKey { get; init; } = "";

    /// <summary>
    /// 接続先で開催される大会などのイベント名です。
    /// </summary>
    public string Event { get; init; } = "";
}

public enum ConnectionProfileKind { Cgos }

public enum CgosConnectionProfileEditField
{
    DisplayName,
    Host,
    Port,
    Event,
    Round,
    Note,
}

public enum CgosPlayerCredentialField
{
    LoginName,
    Password,
}

/// <summary>
/// ＣＧＯＳへの接続画面のフローの種類
/// </summary>
public enum CgosConnectionFlowKind
{
    /// <summary>
    /// プロファイル選択
    /// </summary>
    ProfileSelection,

    /// <summary>
    /// 接続開始
    /// </summary>
    ConnectionStart,

    /// <summary>
    /// 観戦中
    /// </summary>
    Watching,

    /// <summary>
    /// 結果表示
    /// </summary>
    Result,
}
