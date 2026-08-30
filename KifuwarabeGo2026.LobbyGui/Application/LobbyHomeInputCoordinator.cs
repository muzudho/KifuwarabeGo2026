namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>Homeの意味上のヒット対象を、Lobby遷移または外側で実行する操作Intentへ変換します。</summary>
public sealed class LobbyHomeInputCoordinator
{
    private readonly LobbyNavigationController _navigation;

    public LobbyHomeInputCoordinator(LobbyNavigationController navigation)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
    }

    public LobbyPage CurrentPage => _navigation.CurrentPage;

    public void OpenHome() => _navigation.OpenHome();

    public bool TryOpenCasualApp(int appIndex) => _navigation.TryOpenCasualApp(appIndex);

    public LobbyHomeAction Activate(LobbyHomeTarget target)
    {
        if (_navigation.CurrentPage != LobbyPage.Home)
            return LobbyHomeAction.None;

        switch (target)
        {
            case LobbyHomeTarget.LocalMatch:
                return LobbyHomeAction.OpenLocalMatch;
            case LobbyHomeTarget.OnlineMatch:
                return LobbyHomeAction.OpenOnlineMatch;
            case LobbyHomeTarget.EngineProfiles:
                return LobbyHomeAction.ManageEngineProfiles;
            case LobbyHomeTarget.EntryProfiles:
                return LobbyHomeAction.ManageEntryProfiles;
            case LobbyHomeTarget.GamePlatform:
                _navigation.OpenGameOasis();
                return LobbyHomeAction.OpenGameOasis;
            case LobbyHomeTarget.CaptureGame:
                _navigation.TryOpenCasualApp(0);
                return LobbyHomeAction.OpenCaptureGame;
            default:
                return LobbyHomeAction.None;
        }
    }
}

public enum LobbyHomeAction
{
    None,
    OpenLocalMatch,
    OpenOnlineMatch,
    ManageEngineProfiles,
    ManageEntryProfiles,
    OpenGameOasis,
    OpenCaptureGame,
}
