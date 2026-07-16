namespace KifuwarabeGo2026.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

public sealed class CgosConnectionCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private CgosConnectionCatalog(string listPath, IReadOnlyList<CgosConnectionProfile> profiles)
    {
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }

    public IReadOnlyList<CgosConnectionProfile> Profiles { get; }

    public static CgosConnectionCatalog LoadFromDefaultLocation()
    {
        var sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "CgosConnections"));
        var directory = Directory.Exists(sourceDirectory)
            ? sourceDirectory
            : Path.Combine(AppContext.BaseDirectory, "Content", "CgosConnections");
        return Load(Path.Combine(directory, "cgos-connection-list.json"));
    }

    public static CgosConnectionCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
        {
            return new CgosConnectionCatalog(listPath, CreateDefaultProfiles());
        }

        var profiles = JsonSerializer.Deserialize<CgosConnectionProfileList>(File.ReadAllText(listPath), JsonOptions)?.CgosConnections ?? new();
        var normalizedProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.Host))
            .Select(Normalize)
            .ToList();

        if (normalizedProfiles.Count == 0)
        {
            normalizedProfiles.AddRange(CreateDefaultProfiles());
        }

        return new CgosConnectionCatalog(listPath, normalizedProfiles);
    }

    public void Save(IEnumerable<CgosConnectionProfile> profiles)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(listDirectory);
        var list = new CgosConnectionProfileList
        {
            CgosConnections = profiles.Select(Normalize).ToList(),
        };

        File.WriteAllText(ListPath, JsonSerializer.Serialize(list, JsonOptions));
    }

    private static CgosConnectionProfile Normalize(CgosConnectionProfile profile)
    {
        var displayName = string.IsNullOrWhiteSpace(profile.DisplayName)
            ? "Unnamed CGOS Connection"
            : profile.DisplayName.Trim();
        var host = string.IsNullOrWhiteSpace(profile.Host)
            ? "uec-go.com"
            : profile.Host.Trim();
        var role = string.IsNullOrWhiteSpace(profile.Role)
            ? "PRACTICE"
            : profile.Role.Trim();
        return profile with
        {
            DisplayName = displayName,
            Host = host,
            Port = Math.Clamp(profile.Port, 1, 65535),
            Role = role,
            Note = profile.Note.Trim(),
        };
    }

    private static IReadOnlyList<CgosConnectionProfile> CreateDefaultProfiles() =>
    [
        new("練習", "uec-go.com", 6809, "PRACTICE", "CGOS practice server"),
        new("2026年大会予選", "uec-go.com", 6809, "QUALIFIER", "CGF Open 2026 preliminary connection"),
        new("2026年大会本戦", "uec-go.com", 6809, "FINAL", "CGF Open 2026 final connection"),
    ];

    private sealed class CgosConnectionProfileList
    {
        public List<CgosConnectionProfile> CgosConnections { get; set; } = new();
    }
}
