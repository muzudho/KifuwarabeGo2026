namespace KifuwarabeGo2026.Application;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class TournamentRulesCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private TournamentRulesCatalog(string listPath, IReadOnlyList<TournamentRules> rules)
    {
        ListPath = listPath;
        Rules = rules;
    }

    public string ListPath { get; }

    public IReadOnlyList<TournamentRules> Rules { get; }

    public static TournamentRulesCatalog LoadFromDefaultLocation()
    {
        var sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "TournamentRules"));
        var directory = Directory.Exists(sourceDirectory)
            ? sourceDirectory
            : Path.Combine(AppContext.BaseDirectory, "Content", "TournamentRules");
        return Load(Path.Combine(directory, "tournament-rules-list.json"));
    }

    public static TournamentRulesCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
        {
            return new TournamentRulesCatalog(listPath, new[] { CreateDefaultRules() });
        }

        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var entries = JsonSerializer.Deserialize<TournamentRulesList>(File.ReadAllText(listPath), JsonOptions)?.TournamentRules ?? new();
        var rules = new List<TournamentRules>();
        foreach (var entry in entries.Where(entry => !string.IsNullOrWhiteSpace(entry.FilePath)))
        {
            var path = Path.GetFullPath(Path.Combine(listDirectory, entry.FilePath));
            if (!File.Exists(path))
            {
                continue;
            }

            var rule = JsonSerializer.Deserialize<TournamentRules>(File.ReadAllText(path), JsonOptions);
            if (rule is null)
            {
                continue;
            }

            rule.FilePath = path;
            if (!string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                rule.DisplayName = entry.DisplayName;
            }

            rules.Add(Normalize(rule));
        }

        if (rules.Count == 0)
        {
            rules.Add(CreateDefaultRules());
        }

        return new TournamentRulesCatalog(listPath, rules);
    }

    public void Save(TournamentRules rules)
    {
        if (string.IsNullOrWhiteSpace(rules.FilePath))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(rules.FilePath) ?? AppContext.BaseDirectory);
        File.WriteAllText(rules.FilePath, JsonSerializer.Serialize(Normalize(rules.Clone()), JsonOptions));
        UpdateListEntry(rules.FilePath, Normalize(rules.Clone()));
    }

    public TournamentRules SaveAsFilePath(TournamentRules rules, string filePath)
    {
        if (!TryValidateFilePath(rules, filePath, out var targetPath, out var warning))
        {
            throw new InvalidOperationException(warning);
        }

        var savedRules = rules.Clone();
        var oldPath = savedRules.FilePath;
        if (!string.Equals(Path.GetFullPath(oldPath), Path.GetFullPath(targetPath), StringComparison.OrdinalIgnoreCase))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath) ?? AppContext.BaseDirectory);
            File.WriteAllText(targetPath, JsonSerializer.Serialize(Normalize(savedRules.Clone()), JsonOptions));
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }

            savedRules.FilePath = targetPath;
            UpdateListEntry(oldPath, savedRules);
            return savedRules;
        }

        savedRules.FilePath = targetPath;
        Save(savedRules);
        UpdateListEntry(oldPath, savedRules);
        return savedRules;
    }

    public bool TryValidateFilePath(TournamentRules rules, string filePath, out string targetPath, out string warning)
    {
        targetPath = "";
        if (string.IsNullOrWhiteSpace(rules.FilePath))
        {
            warning = "Rules file path is missing.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            warning = "File path is required.";
            return false;
        }

        var fileName = Path.GetFileName(filePath);
        if (!fileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            filePath += ".json";
            fileName = Path.GetFileName(filePath);
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            warning = "File name contains invalid characters.";
            return false;
        }

        if (fileName.EndsWith(' ') || fileName.EndsWith('.'))
        {
            warning = "File name cannot end with a space or dot.";
            return false;
        }

        if (IsReservedWindowsFileName(fileName))
        {
            warning = "File name is reserved by Windows.";
            return false;
        }

        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            warning = "Rules directory is required.";
            return false;
        }

        try
        {
            targetPath = Path.GetFullPath(filePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            warning = "File path is invalid.";
            return false;
        }

        if (targetPath.Length >= 260)
        {
            warning = "File path is too long.";
            return false;
        }

        var currentFullPath = Path.GetFullPath(rules.FilePath);
        if (File.Exists(targetPath) && !string.Equals(targetPath, currentFullPath, StringComparison.OrdinalIgnoreCase))
        {
            warning = "File already exists.";
            return false;
        }

        warning = "";
        return true;
    }

    public TournamentRules CreateNew(TournamentRules source)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(listDirectory);

        var rules = Normalize(source.Clone());
        rules.DisplayName = CreateNewDisplayName();
        rules.FilePath = CreateUniqueRulesPath(listDirectory);
        Save(rules);
        AppendListEntry(rules);
        return rules;
    }

    private void AppendListEntry(TournamentRules rules)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        var list = File.Exists(ListPath)
            ? JsonSerializer.Deserialize<TournamentRulesList>(File.ReadAllText(ListPath), JsonOptions) ?? new TournamentRulesList()
            : new TournamentRulesList();
        list.TournamentRules.Add(new TournamentRulesListEntry
        {
            DisplayName = rules.DisplayName,
            FilePath = Path.GetRelativePath(listDirectory, rules.FilePath),
        });

        File.WriteAllText(ListPath, JsonSerializer.Serialize(list, JsonOptions));
    }

    private void UpdateListEntry(string oldPath, TournamentRules rules)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        var list = File.Exists(ListPath)
            ? JsonSerializer.Deserialize<TournamentRulesList>(File.ReadAllText(ListPath), JsonOptions) ?? new TournamentRulesList()
            : new TournamentRulesList();
        var oldRelativePath = Path.GetRelativePath(listDirectory, oldPath);
        var newRelativePath = Path.GetRelativePath(listDirectory, rules.FilePath);
        var entry = list.TournamentRules.FirstOrDefault(entry =>
            string.Equals(entry.FilePath, oldRelativePath, StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            list.TournamentRules.Add(new TournamentRulesListEntry
            {
                DisplayName = rules.DisplayName,
                FilePath = newRelativePath,
            });
        }
        else
        {
            entry.DisplayName = rules.DisplayName;
            entry.FilePath = newRelativePath;
        }

        File.WriteAllText(ListPath, JsonSerializer.Serialize(list, JsonOptions));
    }

    private static TournamentRules Normalize(TournamentRules rules)
    {
        rules.DisplayName = string.IsNullOrWhiteSpace(rules.DisplayName) ? "Unnamed tournament" : rules.DisplayName.Trim();
        rules.BoardSize = rules.BoardSize is 9 or 13 or 19 ? rules.BoardSize : 19;
        rules.Komi = Math.Clamp(rules.Komi, -99.5m, 99.5m);

        var totalSeconds = Math.Max(0, rules.MainTimeMinutes * 60 + rules.MainTimeSeconds);
        rules.MainTimeMinutes = totalSeconds / 60;
        rules.MainTimeSeconds = totalSeconds % 60;
        rules.MoveLimit = Math.Clamp(rules.MoveLimit, 0, 9999);
        return rules;
    }

    private static TournamentRules CreateDefaultRules() => new()
    {
        DisplayName = "Default 19-ro",
        Rule = GoRuleKind.PureGo,
        BoardSize = 19,
        Komi = 6.5m,
        MainTimeMinutes = 0,
        MainTimeSeconds = 0,
        MoveLimit = 400,
    };

    private static string CreateNewDisplayName() => $"New tournament {DateTime.Now:yyyyMMdd-HHmmss}";

    private static string CreateUniqueRulesPath(string directory)
    {
        var baseName = $"tournament-rules-custom-{DateTime.Now:yyyyMMdd-HHmmss}";
        var path = Path.Combine(directory, $"{baseName}.json");
        var suffix = 2;
        while (File.Exists(path))
        {
            path = Path.Combine(directory, $"{baseName}-{suffix}.json");
            suffix++;
        }

        return path;
    }

    private static bool IsReservedWindowsFileName(string fileName)
    {
        var stem = Path.GetFileNameWithoutExtension(fileName).TrimEnd(' ', '.');
        if (string.IsNullOrEmpty(stem))
        {
            return false;
        }

        var upper = stem.ToUpperInvariant();
        return upper is "CON" or "PRN" or "AUX" or "NUL"
            || IsReservedWindowsDeviceName(upper, "COM")
            || IsReservedWindowsDeviceName(upper, "LPT");
    }

    private static bool IsReservedWindowsDeviceName(string stem, string prefix) =>
        stem.Length == 4 &&
        stem.StartsWith(prefix, StringComparison.Ordinal) &&
        stem[3] is >= '1' and <= '9';

    private sealed class TournamentRulesList
    {
        public List<TournamentRulesListEntry> TournamentRules { get; set; } = new();
    }

    private sealed class TournamentRulesListEntry
    {
        public string DisplayName { get; set; } = "";

        public string FilePath { get; set; } = "";
    }
}
