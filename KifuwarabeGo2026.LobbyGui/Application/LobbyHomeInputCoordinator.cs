namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>Homeの意味上のヒット対象を、Lobby遷移または外側で実行する操作Intentへ変換します。</summary>
public sealed class LobbyHomeInputCoordinator
{
    private readonly LobbyNavigationController _navigation;
    private readonly IReadOnlyList<LobbyHomeItem> _catalog;
    public int PageIndex { get; private set; }
    public LobbyHomePresentation Home => LobbyHomePresenter.Create(PageIndex, _catalog);

    public LobbyHomeInputCoordinator(LobbyNavigationController navigation, IReadOnlyList<LobbyHomeItem>? catalog = null)
    {
        _navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
        _catalog = (catalog ?? LobbyHomePresenter.Catalog).ToArray();
    }

    public LobbyPage CurrentPage => _navigation.CurrentPage;

    public void OpenHome() => _navigation.OpenHome();

    public bool ChangePage(int direction)
    {
        if (CurrentPage != LobbyPage.Home) return false;
        var next = Math.Clamp(PageIndex + Math.Sign(direction), 0, Home.PageCount - 1);
        if (next == PageIndex) return false;
        PageIndex = next;
        return true;
    }

    public bool TryOpenCasualApp(int appIndex) => _navigation.TryOpenCasualApp(appIndex);

    public LobbyHomeAction Activate(LobbyHomeTarget target)
    {
        if (_navigation.CurrentPage != LobbyPage.Home)
            return LobbyHomeAction.None;

        switch (target)
        {
            case LobbyHomeTarget.LocalMatch:
                return LobbyHomeAction.OpenLocalMatch;
            case LobbyHomeTarget.ReferenceGo:
                return LobbyHomeAction.OpenReferenceGo;
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
    OpenReferenceGo,
}
