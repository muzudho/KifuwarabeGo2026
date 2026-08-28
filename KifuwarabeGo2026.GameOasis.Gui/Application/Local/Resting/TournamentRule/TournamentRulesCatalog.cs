namespace KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

/// <summary>Maps the legacy typed Go rules UI to the game-agnostic persistent configuration catalog.</summary>
public sealed class TournamentRulesCatalog : ITournamentRulesCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly PlaySpaceConfigurationCatalog _catalog;

    private TournamentRulesCatalog(PlaySpaceConfigurationCatalog catalog)
    {
        _catalog = catalog;
        Rules = catalog.Profiles.Select(FromProfile).ToList();
    }

    public string ListPath => _catalog.ListPath;
    public IReadOnlyList<TournamentRules> Rules { get; }

    public static TournamentRulesCatalog LoadFromDefaultLocation() =>
        new(PlaySpaceConfigurationCatalog.Load(ApplicationSettingsTournamentRulesStore.Instance));

    public void Save(TournamentRules rules) => _catalog.Save(ToProfile(NormalizeForApplicationSettings(rules)));
    public void Delete(TournamentRules rules) => _catalog.Delete(rules.Id);
    public void SaveOrder(IEnumerable<TournamentRules> rules) =>
        _catalog.SaveOrder(rules.Select(rule => ToProfile(NormalizeForApplicationSettings(rule))));

    public TournamentRules CreateNew(TournamentRules source) => FromProfile(_catalog.CreateNew(
        ToProfile(NormalizeForApplicationSettings(source)), $"New tournament {DateTime.Now:yyyyMMdd-HHmmss}"));

    public TournamentRules Duplicate(TournamentRules source) =>
        FromProfile(_catalog.Duplicate(ToProfile(NormalizeForApplicationSettings(source))));

    private static PlaySpaceConfigurationProfile ToProfile(TournamentRules rules) => new()
    {
        Id = rules.Id,
        DisplayName = rules.DisplayName,
        PlaySpaceId = GameOasisOfficialNames.Go,
        ConfigurationDocument = JsonSerializer.Serialize(rules, JsonOptions),
    };

    private static TournamentRules FromProfile(PlaySpaceConfigurationProfile profile)
    {
        var rules = JsonSerializer.Deserialize<TournamentRules>(profile.ConfigurationDocument, JsonOptions) ?? new();
        rules.Id = profile.Id;
        rules.DisplayName = profile.DisplayName;
        return NormalizeForApplicationSettings(rules);
    }

    private static TournamentRules NormalizeForApplicationSettings(TournamentRules source)
    {
        var rules = source.Clone();
        rules.Id = string.IsNullOrWhiteSpace(rules.Id) ? Guid.NewGuid().ToString("N") : rules.Id.Trim();
        rules.DisplayName = string.IsNullOrWhiteSpace(rules.DisplayName) ? "Unnamed tournament" : rules.DisplayName.Trim();
        rules.BoardSize = rules.BoardSize is 9 or 13 or 19 ? rules.BoardSize : 19;
        rules.Komi = Math.Clamp(rules.Komi, -99.5m, 99.5m);
        var totalSeconds = Math.Clamp(rules.MainTimeMinutes * 60 + rules.MainTimeSeconds, 0, 999 * 3600 + 59 * 60 + 59);
        rules.MainTimeMinutes = totalSeconds / 60;
        rules.MainTimeSeconds = totalSeconds % 60;
        rules.MoveLimit = Math.Clamp(rules.MoveLimit, 0, 9999);
        rules.FilePath = ApplicationSettings.FilePath;
        return rules;
    }

    private sealed class ApplicationSettingsTournamentRulesStore : IPlaySpaceConfigurationProfileStore
    {
        public static ApplicationSettingsTournamentRulesStore Instance { get; } = new();
        public string ListPath => ApplicationSettings.FilePath;
        public IReadOnlyList<PlaySpaceConfigurationProfile> Load() =>
            ApplicationSettings.Current.TournamentRules.Select(rule => ToProfile(NormalizeForApplicationSettings(rule))).ToList();
        public void Save(IEnumerable<PlaySpaceConfigurationProfile> profiles) =>
            ApplicationSettings.SaveTournamentRules(profiles.Select(FromProfile));
    }
}
