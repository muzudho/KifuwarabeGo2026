namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>開始前Lobby画面のページ遷移を、描画とPlay Room状態から独立して管理します。</summary>
public sealed class LobbyNavigationController
{
    public LobbyPage CurrentPage { get; private set; } = LobbyPage.Home;

    public void OpenHome() => CurrentPage = LobbyPage.Home;

    public void OpenGameOasis() => CurrentPage = LobbyPage.GameOasis;

    public bool TryOpenCasualApp(int appIndex)
    {
        CurrentPage = appIndex switch
        {
            0 => LobbyPage.CaptureGame,
            1 => LobbyPage.Tsumego,
            2 => LobbyPage.NextMove,
            _ => CurrentPage,
        };
        return appIndex is >= 0 and <= 2;
    }
}

/// <summary>Play Room開始前にLobbyが所有するトップレベルページです。</summary>
public enum LobbyPage
{
    Home,
    GameOasis,
    CaptureGame,
    Tsumego,
    NextMove,
}
