namespace KifuwarabeGo2026.Engine.AppProvider.Tsumego;

/// <summary>詰碁アプリを提供します。</summary>
internal sealed class TsumegoAppProvider : IAppProvider
{
    public string AppId => "tsumego";
    public string DisplayName => "詰碁";
    public bool IsAvailable => false;
}
