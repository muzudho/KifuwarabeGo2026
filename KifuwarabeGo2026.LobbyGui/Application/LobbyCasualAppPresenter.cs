namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>Casual Appページの表示内容と、互換Hostへ委譲する画面種別を構成します。</summary>
public static class LobbyCasualAppPresenter
{
    private const string ComingSoonMessage = "COMING SOON";
    private const string ComingSoonDescription =
        "問題集と問題一覧は、ここからディレクトリーのように開いていく予定です。";

    public static LobbyCasualAppPresentation? Create(LobbyPage page) => page switch
    {
        LobbyPage.CaptureGame => CreateProviderSelection(page, "ポン抜きゲーム", "CAPTURE GAME"),
        LobbyPage.Tsumego => CreateComingSoon(page, "詰碁", "LIFE & DEATH"),
        LobbyPage.NextMove => CreateComingSoon(page, "次の一手問題", "NEXT MOVE"),
        _ => null,
    };

    private static LobbyCasualAppPresentation CreateProviderSelection(
        LobbyPage page,
        string title,
        string caption) =>
        new(page, title, caption, CreateBreadcrumb(caption), LobbyCasualAppContent.ProviderSelection, null, null);

    private static LobbyCasualAppPresentation CreateComingSoon(
        LobbyPage page,
        string title,
        string caption) =>
        new(page, title, caption, CreateBreadcrumb(caption), LobbyCasualAppContent.ComingSoon,
            ComingSoonMessage, ComingSoonDescription);

    private static string CreateBreadcrumb(string caption) => $"HOME  >  CASUAL APPS  >  {caption}";
}

public sealed record LobbyCasualAppPresentation(
    LobbyPage Page,
    string Title,
    string Caption,
    string Breadcrumb,
    LobbyCasualAppContent Content,
    string? StatusMessage,
    string? Description);

public enum LobbyCasualAppContent
{
    ProviderSelection,
    ComingSoon,
}
