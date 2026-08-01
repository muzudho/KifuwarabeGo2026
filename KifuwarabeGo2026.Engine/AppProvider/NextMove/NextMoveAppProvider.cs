namespace KifuwarabeGo2026.Engine.AppProvider.NextMove;

/// <summary>次の一手アプリを提供します。</summary>
internal sealed class NextMoveAppProvider : IAppProvider
{
    public string AppId => "next-move";
    public string DisplayName => "次の一手問題";
    public bool IsAvailable => false;
}
