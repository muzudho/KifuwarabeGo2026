namespace KifuwarabeGo2026.Application.Cgos.ConnectionTarget;

public sealed record CgosConnectionProfile(
    string DisplayName,
    string Host,
    int Port,
    string Role,
    string Note);

public enum CgosConnectionProfileEditField
{
    DisplayName,
    Host,
    Port,
    Role,
    Note,
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
