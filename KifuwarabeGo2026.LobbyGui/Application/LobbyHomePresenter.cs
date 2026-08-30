namespace KifuwarabeGo2026.LobbyGui.Application;

/// <summary>Lobby Homeの文言と意味上の強調を、描画フレームワークに依存せず構成します。</summary>
public static class LobbyHomePresenter
{
    private static readonly LobbyHomePresentation Presentation = new(
        "左で対局候補を準備し、利用するアプリを選べます。",
        [
            new(LobbyHomeTarget.EngineProfiles, "エンジン登録", "REGISTER ENGINES", LobbyHomeAccent.Engine),
            new(LobbyHomeTarget.EntryProfiles, "エントリー登録", "REGISTER ENTRIES", LobbyHomeAccent.Entry),
            new(LobbyHomeTarget.LocalMatch, "Local Match", "PLAY / REVIEW", LobbyHomeAccent.Formal),
            new(LobbyHomeTarget.OnlineMatch, "Online Match (CGOS)", "WATCH / CONNECT", LobbyHomeAccent.Formal),
            new(LobbyHomeTarget.CaptureGame, "ポン抜きゲーム", "CAPTURE GAME", LobbyHomeAccent.Casual),
            new(LobbyHomeTarget.GamePlatform, "Kifuwarabe Game Oasis", "REFERENCE PLAY-SPACES", LobbyHomeAccent.Platform),
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

    public static LobbyHomePresentation Create() => Presentation;
}

public sealed record LobbyHomePresentation(
    string Guidance,
    IReadOnlyList<LobbyHomeItem> Items,
    IReadOnlyList<LobbyHomeHint> Hints)
{
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
