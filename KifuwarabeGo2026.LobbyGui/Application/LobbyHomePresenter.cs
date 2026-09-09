namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>Lobby Homeの文言と意味上の強調を、描画フレームワークに依存せず構成します。</summary>
public static class LobbyHomePresenter
{
    public const int PageSize = 4;
    public static IReadOnlyList<LobbyHomeItem> Catalog { get; } = Array.AsReadOnly(new LobbyHomeItem[]
    {
        new(LobbyHomeTarget.LocalMatch, "囲碁ローカルマッチ", "GTP対応エンジンや人間で対局", LobbyHomeAccent.Formal),
        new(LobbyHomeTarget.OnlineMatch, "囲碁オンラインマッチ（CGOS）", "GTP対応エンジンをCGOSへ接続・観戦", LobbyHomeAccent.Formal),
        new(LobbyHomeTarget.CaptureGame, "ポン抜き", "石を取る囲碁ゲーム", LobbyHomeAccent.Casual),
        new(LobbyHomeTarget.ReferenceGo, "コンピューター囲碁サンプル", "開発者向けのプレイルーム・リファレンス実装", LobbyHomeAccent.Platform),
    });
    private static readonly LobbyHomePresentation Presentation = new(
        "遊ぶプレイルームを選んでください。参加するエンジンやエントリーは上のメニューで準備できます。",
        [
            new(LobbyHomeTarget.EngineProfiles, "エンジン登録", "REGISTER ENGINES", LobbyHomeAccent.Engine),
            new(LobbyHomeTarget.EntryProfiles, "エントリー登録", "REGISTER ENTRIES", LobbyHomeAccent.Entry),
            .. Catalog,
        ],
        [
            new(LobbyHomeTarget.EntrySettings, "ENTRY SETTINGS とは？", ["エンジンを登録し、", "対局へ参加させる候補を準備します！"], LobbyHomeAccent.Engine),
            new(LobbyHomeTarget.FormalApps, "FORMAL APPS とは？", ["他の人が作った GTP対応の", "コンピュータ碁の思考エンジンを", "動かせるよう、", "有名なエンジンの拡張仕様は", "取り込んでいます！"], LobbyHomeAccent.Formal),
            new(LobbyHomeTarget.CasualApps, "CASUAL APPS とは？", ["独自実装で", "機能追加を進めます！"], LobbyHomeAccent.Casual),
            new(LobbyHomeTarget.GamePlatform, "GAME PLATFORM とは？", ["Replaceable play-spaces connect through Game Oasis."], LobbyHomeAccent.Platform),
            new(LobbyHomeTarget.Settings, "SETTINGS とは？", ["アプリケーションを設定します！"], LobbyHomeAccent.Settings),
            new(LobbyHomeTarget.LocalMatch, "LOCAL MATCH とは？", ["ローカルPCで、人間や碁エンジンが", "対局！ など。"], LobbyHomeAccent.Formal),
            new(LobbyHomeTarget.OnlineMatch, "ONLINE MATCH とは？", ["インターネット上の碁サーバーにお邪魔して", "碁エンジンが対局！"], LobbyHomeAccent.Formal),
            new(LobbyHomeTarget.EngineProfiles, "ENGINE PROFILES とは？", ["GTPエンジンの起動設定を管理します。"], LobbyHomeAccent.Engine),
            new(LobbyHomeTarget.EntryProfiles, "ENTRY PROFILES とは？", ["対局へ参加させる候補を準備します。"], LobbyHomeAccent.Entry),
        ]);

    public static LobbyHomePresentation Create(int pageIndex = 0, IReadOnlyList<LobbyHomeItem>? catalog = null)
    {
        var entries = (catalog ?? Catalog).ToArray();
        var pageCount = Math.Max(1, (entries.Length + PageSize - 1) / PageSize);
        return Presentation with { Catalog = entries, PageIndex = Math.Clamp(pageIndex, 0, pageCount - 1) };
    }
}

public sealed record LobbyHomePresentation(
    string Guidance,
    IReadOnlyList<LobbyHomeItem> Items,
    IReadOnlyList<LobbyHomeHint> Hints)
{
    public IReadOnlyList<LobbyHomeItem> Catalog { get; init; } = [];
    public int PageIndex { get; init; }
    public int PageCount => Math.Max(1, (Catalog.Count + LobbyHomePresenter.PageSize - 1) / LobbyHomePresenter.PageSize);
    public bool CanPrevious => PageIndex > 0;
    public bool CanNext => PageIndex + 1 < PageCount;
    public IReadOnlyList<LobbyHomeItem> VisibleItems => Catalog.Skip(PageIndex * LobbyHomePresenter.PageSize).Take(LobbyHomePresenter.PageSize).ToArray();
    public LobbyHomeTarget? Select(int visibleIndex) => visibleIndex >= 0 && visibleIndex < VisibleItems.Count ? VisibleItems[visibleIndex].Target : null;

    public LobbyHomeItem GetItem(LobbyHomeTarget target) =>
        Items.First(item => item.Target == target);

    public LobbyHomeHint GetHint(LobbyHomeTarget target) =>
        Hints.First(hint => hint.Target == target);
}

public sealed record LobbyHomeItem(
    LobbyHomeTarget Target,
    string Title,
    string Caption,
    LobbyHomeAccent Accent);

public sealed record LobbyHomeHint(
    LobbyHomeTarget Target,
    string Heading,
    IReadOnlyList<string> BodyLines,
    LobbyHomeAccent Accent);

public enum LobbyHomeTarget
{
    EntrySettings,
    FormalApps,
    CasualApps,
    GamePlatform,
    LocalMatch,
    OnlineMatch,
    EngineProfiles,
    EntryProfiles,
    CaptureGame,
    Settings,
    ReferenceGo,
}

public enum LobbyHomeAccent
{
    Formal,
    Casual,
    Platform,
    Engine,
    Entry,
    Settings,
}
