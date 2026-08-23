namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>エントリー登録の正規化と永続的な同値判定を所有します。</summary>
public static class EntryProfilePolicy
{
    public static EntryProfile Normalize(EntryProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "New Player" : normalized.DisplayName.Trim();
        normalized.Identifier ??= "";
        normalized.EngineProfileId ??= "";
        normalized.ClientIdentityProfileIds = (normalized.ClientIdentityProfileIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Kind == EntryProfileKind.Human) normalized.EngineProfileId = "";
        return normalized;
    }

    public static bool ListsAreEqual(IReadOnlyList<EntryProfile> left, IReadOnlyList<EntryProfile> right) =>
        left.Count == right.Count && left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.DisplayName == pair.Second.DisplayName &&
            pair.First.Identifier == pair.Second.Identifier &&
            pair.First.Kind == pair.Second.Kind &&
            pair.First.EngineProfileId == pair.Second.EngineProfileId &&
            pair.First.ClientIdentityProfileIds.SequenceEqual(pair.Second.ClientIdentityProfileIds, StringComparer.Ordinal));
}
