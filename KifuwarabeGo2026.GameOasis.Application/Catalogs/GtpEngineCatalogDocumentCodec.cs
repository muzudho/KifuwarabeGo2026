namespace KifuwarabeGo2026.GameOasis.Application.Catalogs;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record GtpEngineCatalogDocumentResult(IReadOnlyList<GtpEngineProfile> Profiles, bool RequiresSave, bool DuplicateIdsRepaired);

public static class GtpEngineCatalogDocumentCodec
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public static GtpEngineCatalogDocumentResult Deserialize(string json, string listDirectory)
    {
        var profiles = JsonSerializer.Deserialize<Document>(json, JsonOptions)?.GtpEngines ?? [];
        var requiresSave = profiles.Any(profile => string.IsNullOrWhiteSpace(profile.Id));
        var duplicateIdsRepaired = false;
        var usedIds = new HashSet<string>(StringComparer.Ordinal);
        var normalized = profiles.Where(profile => !string.IsNullOrWhiteSpace(profile.ExecutablePath))
            .Select(profile => GtpEngineProfilePolicy.Normalize(profile, listDirectory))
            .Select(profile =>
            {
                if (usedIds.Add(profile.Id)) return profile;
                profile.Id = CreateUniqueId(usedIds);
                requiresSave = true;
                duplicateIdsRepaired = true;
                return profile;
            }).ToList();
        return new(normalized, requiresSave, duplicateIdsRepaired);
    }

    public static string Serialize(IEnumerable<GtpEngineProfile> profiles, string listDirectory) =>
        JsonSerializer.Serialize(new Document
        {
            GtpEngines = profiles.Select(profile => ToStoredEntry(
                GtpEngineProfilePolicy.Normalize(profile, listDirectory), listDirectory)).ToList(),
        }, JsonOptions);

    private static string CreateUniqueId(ISet<string> usedIds)
    {
        string id;
        do id = Guid.NewGuid().ToString("N"); while (!usedIds.Add(id));
        return id;
    }

    private static GtpEngineProfile ToStoredEntry(GtpEngineProfile profile, string listDirectory)
    {
        var entry = profile.Clone();
        entry.ExecutablePath = ToStoredPath(entry.ExecutablePath, listDirectory);
        entry.WorkingDirectoryModel = WorkingDirectoryModel.FromString(ToStoredPath(entry.WorkingDirectoryModel.Value, listDirectory));
        return entry;
    }

    private static string ToStoredPath(string path, string listDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || !GtpEngineProfilePolicy.HasDirectoryPart(path)) return path;
        try { return Path.GetRelativePath(listDirectory, Path.GetFullPath(path)); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return path; }
    }

    private sealed class Document { public List<GtpEngineProfile> GtpEngines { get; set; } = []; }
}
