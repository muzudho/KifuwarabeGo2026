namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>
/// 対局者として選択できる登録項目。
/// 人間とコンピューターを同じリストで扱い、コンピューターだけが GTP エンジン設定を参照する。
/// </summary>
public sealed class PlayerProfile
{
    /// <summary>アプリ内部でのみ用いる不変の識別子。</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>画面・SGF に表示する名前。</summary>
    public string DisplayName { get; set; } = "New Player";

    /// <summary>
    /// 外部サービスやファイル名などで利用者が使いたい識別子。
    /// 文字種・文字数はこのアプリでは制限しない。出力先ごとの制約は出力時に扱う。
    /// </summary>
    public string Identifier { get; set; } = "";

    public PlayerProfileKind Kind { get; set; } = PlayerProfileKind.Human;

    /// <summary>Kind が Computer のときに参照する GTP エンジン設定の ID。</summary>
    public string EngineProfileId { get; set; } = "";

    public PlayerProfile Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Identifier = Identifier,
        Kind = Kind,
        EngineProfileId = EngineProfileId,
    };
}

public enum PlayerProfileKind
{
    Human,
    Computer,
}
