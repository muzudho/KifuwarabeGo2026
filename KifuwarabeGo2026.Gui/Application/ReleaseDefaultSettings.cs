namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// リリースへ同梱する default-settings.json の内容です。
/// 利用者が保存する設定とは分離し、新規環境の初期値としてだけ使用します。
/// </summary>
public sealed class ReleaseDefaultSettings
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public TournamentRuleDefaultSettings TournamentRuleSettings { get; set; } = new();

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public EngineDefaultSettings EngineSettings { get; set; } = new();

    public CgosConnectionDefaultSettings CgosConnectionSettings { get; set; } = new();

    public static string FilePath { get; } = FindFilePath();

    public static ReleaseDefaultSettings Current { get; } = Load();

    private static ReleaseDefaultSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new ReleaseDefaultSettings();
            }

            var settings = JsonSerializer.Deserialize<ReleaseDefaultSettings>(
                File.ReadAllText(FilePath),
                JsonOptions);
            if (settings is null)
            {
                return new ReleaseDefaultSettings();
            }

            if (settings.SchemaVersion is < 1 or > CurrentSchemaVersion)
            {
                return new ReleaseDefaultSettings();
            }

            settings.TournamentRuleSettings ??= new TournamentRuleDefaultSettings();
            settings.EngineSettings ??= new EngineDefaultSettings();
            settings.CgosConnectionSettings ??= new CgosConnectionDefaultSettings();
            settings.TournamentRuleSettings.TournamentRules ??= [];
            settings.EngineSettings.GtpEngines ??= [];
            settings.CgosConnectionSettings.CgosConnections ??= [];
            return settings;
        }
        catch (Exception)
        {
            // A missing or damaged release-default file must not prevent startup.
            return new ReleaseDefaultSettings();
        }
    }

    private static string FindFilePath()
    {
        var installedPath = Path.Combine(AppContext.BaseDirectory, "default-settings.json");
        if (File.Exists(installedPath))
        {
            return installedPath;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var repositoryPath = Path.Combine(
                directory.FullName,
                "KifuwarabeGo2026.Gui",
                "default-settings.json");
            if (File.Exists(repositoryPath))
            {
                return repositoryPath;
            }

            directory = directory.Parent;
        }

        return installedPath;
    }
}

public sealed class TournamentRuleDefaultSettings
{
    public List<TournamentRules> TournamentRules { get; set; } = [];
}

public sealed class EngineDefaultSettings
{
    public List<GtpEngineProfile> GtpEngines { get; set; } = [];
}

public sealed class CgosConnectionDefaultSettings
{
    public List<CgosConnectionProfile> CgosConnections { get; set; } = [];
}
