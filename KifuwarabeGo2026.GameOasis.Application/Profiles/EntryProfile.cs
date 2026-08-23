namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>対局者として選択できる、人間またはコンピューターの常用登録項目です。</summary>
public sealed class EntryProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "New Player";
    public string Identifier { get; set; } = "";
    public EntryProfileKind Kind { get; set; } = EntryProfileKind.Human;
    public string EngineProfileId { get; set; } = "";

    // 既存player-list.jsonとの互換性を維持する永続キーです。
    [JsonPropertyName("targetProfileIds")]
    public List<string> ClientIdentityProfileIds { get; set; } = [];

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
