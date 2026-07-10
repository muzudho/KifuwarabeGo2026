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
    }

    private static TournamentRules Normalize(TournamentRules rules)
    {
        rules.DisplayName = string.IsNullOrWhiteSpace(rules.DisplayName) ? "Unnamed tournament" : rules.DisplayName.Trim();
        rules.BoardSize = rules.BoardSize is 9 or 13 or 19 ? rules.BoardSize : 19;
        rules.Komi = Math.Clamp(rules.Komi, -99.5m, 99.5m);

        var totalSeconds = Math.Max(0, rules.MainTimeMinutes * 60 + rules.MainTimeSeconds);
        rules.MainTimeMinutes = totalSeconds / 60;
        rules.MainTimeSeconds = totalSeconds % 60;
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
    };

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
