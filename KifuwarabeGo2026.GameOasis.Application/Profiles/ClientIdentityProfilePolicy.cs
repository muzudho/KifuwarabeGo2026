namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>クライアント識別情報の正規化と永続的な同値判定を所有します。</summary>
public static class ClientIdentityProfilePolicy
{
    public static ClientIdentityProfile Normalize(
        ClientIdentityProfile profile,
        IReadOnlyDictionary<string, string>? connectionNamesById = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var normalized = profile.Clone();
        normalized.Id = string.IsNullOrWhiteSpace(normalized.Id) ? Guid.NewGuid().ToString("N") : normalized.Id;
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName) ? "New Client Identity" : normalized.DisplayName.Trim();
        if (string.Equals(normalized.DisplayName, "CGOS", StringComparison.Ordinal))
            normalized.DisplayName = "OnlineMatch (CGOS)";
        normalized.ConnectionProfileId ??= "";
        normalized.LoginName ??= "";
        normalized.LoginPass ??= "";
        normalized.Comment ??= "";
        if (string.IsNullOrWhiteSpace(normalized.Comment) &&
            !string.IsNullOrWhiteSpace(normalized.ConnectionProfileId) &&
            connectionNamesById?.GetValueOrDefault(normalized.ConnectionProfileId) is { } connectionName)
            normalized.Comment = connectionName;
        return normalized;
    }

    public static bool ListsAreEqual(IReadOnlyList<ClientIdentityProfile> left, IReadOnlyList<ClientIdentityProfile> right) =>
        left.Count == right.Count && left.Zip(right).All(pair =>
            pair.First.Id == pair.Second.Id &&
            pair.First.DisplayName == pair.Second.DisplayName &&
            pair.First.ConnectionProfileId == pair.Second.ConnectionProfileId &&
            pair.First.LoginName == pair.Second.LoginName &&
            pair.First.LoginPass == pair.Second.LoginPass &&
            pair.First.Comment == pair.Second.Comment);
}
