namespace KifuwarabeGo2026.Application;

using KifuwarabeGo2026.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

/// <summary>
/// GTPプロトコルに対応した思考エンジンのカタログ
/// </summary>
public sealed class GtpEngineCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private GtpEngineCatalog(string listPath, IReadOnlyList<GtpEngineProfile> profiles)
    {
        ListPath = listPath;
        Profiles = profiles;
    }

    public string ListPath { get; }

    public IReadOnlyList<GtpEngineProfile> Profiles { get; }

    public static GtpEngineCatalog LoadFromDefaultLocation()
    {
        var sourceDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Content", "GtpEngines"));
        var directory = Directory.Exists(sourceDirectory)
            ? sourceDirectory
            : Path.Combine(AppContext.BaseDirectory, "Content", "GtpEngines");
        return Load(Path.Combine(directory, "gtp-engine-list.json"));
    }

    public static GtpEngineCatalog Load(string listPath)
    {
        if (!File.Exists(listPath))
        {
            return new GtpEngineCatalog(listPath, new[] { Normalize(CreateDefaultProfile(), AppContext.BaseDirectory) });
        }

        var listDirectory = Path.GetDirectoryName(listPath) ?? AppContext.BaseDirectory;
        var profiles = JsonSerializer.Deserialize<GtpEngineProfileList>(File.ReadAllText(listPath), JsonOptions)?.GtpEngines ?? new();
        var normalizedProfiles = profiles
            .Where(profile => !string.IsNullOrWhiteSpace(profile.ExecutablePath))
            .Select(profile => Normalize(profile, listDirectory))
            .ToList();

        if (normalizedProfiles.Count == 0)
        {
            normalizedProfiles.Add(Normalize(CreateDefaultProfile(), listDirectory));
        }

        return new GtpEngineCatalog(listPath, normalizedProfiles);
    }

    public void Save(IEnumerable<GtpEngineProfile> profiles)
    {
        var listDirectory = Path.GetDirectoryName(ListPath) ?? AppContext.BaseDirectory;
        Directory.CreateDirectory(listDirectory);
        var list = new GtpEngineProfileList
        {
            GtpEngines = profiles
                .Select(profile => ToListEntry(Normalize(profile, listDirectory), listDirectory))
                .ToList(),
        };

        File.WriteAllText(ListPath, JsonSerializer.Serialize(list, JsonOptions));
    }

    private static GtpEngineProfile Normalize(GtpEngineProfile profile, string baseDirectory)
    {
        var normalized = profile.Clone();
        normalized.DisplayName = string.IsNullOrWhiteSpace(normalized.DisplayName)
            ? "Unnamed GTP Engine"
            : normalized.DisplayName.Trim();
        normalized.ExecutablePath = ResolvePath(normalized.ExecutablePath, baseDirectory);
        normalized.WorkingDirectoryModel = normalized.WorkingDirectoryModel.IsEmpty
            ? WorkingDirectoryModel.FromString(Path.GetDirectoryName(normalized.ExecutablePath) ?? baseDirectory)
            : WorkingDirectoryModel.FromString(ResolvePath(normalized.WorkingDirectoryModel.Value, baseDirectory));
        normalized.GuiOptions ??= [];
        if (!normalized.GuiOptions.ContainsKey(GtpEngineGuiOptions.RandomMoveId))
            normalized.GuiOptions[GtpEngineGuiOptions.RandomMoveId] = GtpEngineGuiOptions.ChebyshevDistanceFromStarRandomMove;
        return normalized;
    }

    private static string ResolvePath(string path, string baseDirectory)
    {
        if (Path.IsPathFullyQualified(path) || !HasDirectoryPart(path))
        {
            return path;
        }

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static bool HasDirectoryPart(string path) =>
        path.Contains(Path.DirectorySeparatorChar) ||
        path.Contains(Path.AltDirectorySeparatorChar);

    private static GtpEngineProfile ToListEntry(GtpEngineProfile profile, string listDirectory)
    {
        var entry = profile.Clone();
        entry.ExecutablePath = ToStoredPath(entry.ExecutablePath, listDirectory);
        entry.WorkingDirectoryModel = WorkingDirectoryModel.FromString(ToStoredPath(entry.WorkingDirectoryModel.Value, listDirectory));
        return entry;
    }

    private static string ToStoredPath(string path, string listDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || !HasDirectoryPart(path))
        {
            return path;
        }

        try
        {
            return Path.GetRelativePath(listDirectory, Path.GetFullPath(path));
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return path;
        }
    }

    private static GtpEngineProfile CreateDefaultProfile()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var configuration = new DirectoryInfo(baseDirectory).Parent?.Name ?? "Debug";
        var repositoryRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
        var executableName = OperatingSystem.IsWindows() ? "KifuwarabeGo2026.Engine.exe" : "KifuwarabeGo2026.Engine";
        var engineDirectory = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Engine", "bin", configuration, "net8.0");
        var engineExecutable = Path.Combine(engineDirectory, executableName);
        if (File.Exists(engineExecutable))
        {
            return new GtpEngineProfile
            {
                DisplayName = "Kifuwarabe Star Random GTP",
                ExecutablePath = engineExecutable,
                WorkingDirectoryModel = WorkingDirectoryModel.FromString(engineDirectory),
                Arguments = "",
                EnableGtpLog = true,
            };
        }

        var engineProject = Path.Combine(repositoryRoot, "KifuwarabeGo2026.Engine", "KifuwarabeGo2026.Engine.csproj");
        return new GtpEngineProfile
        {
            DisplayName = "Kifuwarabe Star Random GTP",
            ExecutablePath = "dotnet",
            WorkingDirectoryModel = WorkingDirectoryModel.FromString(repositoryRoot),
            Arguments = $"run --project \"{engineProject}\"",
            EnableGtpLog = true,
        };
    }

    private sealed class GtpEngineProfileList
    {
        public List<GtpEngineProfile> GtpEngines { get; set; } = new();
    }
}
