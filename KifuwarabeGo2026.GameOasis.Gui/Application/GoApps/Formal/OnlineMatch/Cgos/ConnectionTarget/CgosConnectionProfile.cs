namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

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
