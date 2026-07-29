namespace KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.Gui.Application;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TournamentRulesCatalog
{
    private TournamentRulesCatalog(string settingsPath, IReadOnlyList<TournamentRules> rules)
    {
        ListPath = settingsPath;
        Rules = rules;
    }

    public string ListPath { get; }

    public IReadOnlyList<TournamentRules> Rules { get; }

    public static TournamentRulesCatalog LoadFromDefaultLocation()
    {
        var rules = ApplicationSettings.Current.TournamentRules
            .Select(NormalizeForApplicationSettings)
            .ToList();
        return new TournamentRulesCatalog(ApplicationSettings.FilePath, rules);
    }

    public void Save(TournamentRules rules)
    {
        var savedRules = ApplicationSettings.Current.TournamentRules
            .Select(NormalizeForApplicationSettings)
            .ToList();
        var normalized = NormalizeForApplicationSettings(rules);
        var index = savedRules.FindIndex(candidate =>
            candidate.Id.Equals(normalized.Id, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            savedRules[index] = normalized;
        }
        else
        {
            savedRules.Add(normalized);
        }

        ApplicationSettings.SaveTournamentRules(savedRules);
    }

    public void Delete(TournamentRules rules)
    {
        var savedRules = ApplicationSettings.Current.TournamentRules
            .Where(candidate => !candidate.Id.Equals(rules.Id, StringComparison.OrdinalIgnoreCase))
            .Select(NormalizeForApplicationSettings)
            .ToList();
        ApplicationSettings.SaveTournamentRules(savedRules);
    }

    public void SaveOrder(IEnumerable<TournamentRules> rules) =>
        ApplicationSettings.SaveTournamentRules(
            rules.Select(NormalizeForApplicationSettings).ToList());

    public TournamentRules CreateNew(TournamentRules source)
    {
        var rules = NormalizeForApplicationSettings(source);
        rules.Id = Guid.NewGuid().ToString("N");
        rules.DisplayName = $"New tournament {DateTime.Now:yyyyMMdd-HHmmss}";
        Save(rules);
        return rules;
    }

    public TournamentRules Duplicate(TournamentRules source)
    {
        var rules = NormalizeForApplicationSettings(source);
        rules.Id = Guid.NewGuid().ToString("N");
        rules.DisplayName = $"{rules.DisplayName} Copy";
        Save(rules);
        return rules;
    }

    private static TournamentRules NormalizeForApplicationSettings(TournamentRules source)
    {
        var rules = source.Clone();
        rules.Id = string.IsNullOrWhiteSpace(rules.Id) ? Guid.NewGuid().ToString("N") : rules.Id.Trim();
        rules.DisplayName = string.IsNullOrWhiteSpace(rules.DisplayName) ? "Unnamed tournament" : rules.DisplayName.Trim();
        rules.BoardSize = rules.BoardSize is 9 or 13 or 19 ? rules.BoardSize : 19;
        rules.Komi = Math.Clamp(rules.Komi, -99.5m, 99.5m);
        var totalSeconds = Math.Clamp(
            rules.MainTimeMinutes * 60 + rules.MainTimeSeconds,
            0,
            999 * 3600 + 59 * 60 + 59);
        rules.MainTimeMinutes = totalSeconds / 60;
        rules.MainTimeSeconds = totalSeconds % 60;
        rules.MoveLimit = Math.Clamp(rules.MoveLimit, 0, 9999);
        rules.FilePath = ApplicationSettings.FilePath;
        return rules;
    }
}
