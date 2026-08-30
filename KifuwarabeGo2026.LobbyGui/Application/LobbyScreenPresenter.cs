namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>開始前Lobbyの現在ページと各ページの表示モデルを、単一の描画入力へ構成します。</summary>
public static class LobbyScreenPresenter
{
    public static LobbyScreenPresentation Create(
        LobbyPage currentPage,
        IReadOnlyList<GuiPlaySpaceEntry> gameOasisEntries)
    {
        ArgumentNullException.ThrowIfNull(gameOasisEntries);
        return new(
            currentPage,
            LobbyHomePresenter.Create(),
            LobbyGameOasisPresenter.Create(gameOasisEntries),
            LobbyCasualAppPresenter.Create(currentPage));
    }
}

/// <summary>描画フレームワークに依存しない、開始前Lobby画面全体の表示モデルです。</summary>
public sealed record LobbyScreenPresentation(
    LobbyPage CurrentPage,
    LobbyHomePresentation Home,
    LobbyGameOasisPresentation GameOasis,
    LobbyCasualAppPresentation? CasualApp);
