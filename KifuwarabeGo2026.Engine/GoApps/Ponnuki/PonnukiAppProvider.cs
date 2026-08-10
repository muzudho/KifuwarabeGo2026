namespace KifuwarabeGo2026.Engine.GoApps.Ponnuki;

using KifuwarabeGo2026.Engine.GoApps;

/// <summary>
/// ポン抜きマイクロゲームの初期局面、進行、終局判定、結果を提供します。
/// </summary>
internal sealed class PonnukiAppProvider : IAppProvider
{
    public string AppId => "ponnuki";
    public string DisplayName => "ポン抜きゲーム";
    public bool IsAvailable => false;
}
