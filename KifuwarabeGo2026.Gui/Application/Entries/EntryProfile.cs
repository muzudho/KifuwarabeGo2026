namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// 対局者として選択できる登録項目。
/// 人間とコンピューターを同じリストで扱い、コンピューターだけが GTP エンジン設定を参照する。
/// </summary>
public sealed class EntryProfile
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

    public EntryProfileKind Kind { get; set; } = EntryProfileKind.Human;

    /// <summary>Kind が Computer のときに参照する GTP エンジン設定の ID。</summary>
    public string EngineProfileId { get; set; } = "";

    /// <summary>この Player が利用できる ClientIdentityProfile の ID。一つの Target は Player を逆参照しない。</summary>
    // Keep the persisted key stable so existing player-list.json files remain readable.
    [JsonPropertyName("targetProfileIds")]
    public List<string> ClientIdentityProfileIds { get; set; } = new();

    public EntryProfile Clone() => new()
    {
        Id = Id,
        DisplayName = DisplayName,
        Identifier = Identifier,
        Kind = Kind,
        EngineProfileId = EngineProfileId,
        ClientIdentityProfileIds = new List<string>(ClientIdentityProfileIds ?? []),
    };
}

public enum EntryProfileKind
{
    Human,
    Computer,
}
