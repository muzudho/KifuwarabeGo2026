namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

internal sealed class WindowsInstallerShortcutStore
{
    public const int MaximumCount = 5;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    private readonly string filePath;

    public WindowsInstallerShortcutStore(string? localApplicationData = null)
    {
        var root = Path.Combine(
            localApplicationData ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KifuwarabeGo2026");
        filePath = Path.Combine(root, "launcher-shortcuts.json");
    }

    public string FilePath => filePath;

    public List<InstallerShortcutEntry> Load()
    {
        if (!File.Exists(filePath)) return [];
        try
        {
            var document = JsonSerializer.Deserialize<InstallerShortcutDocument>(File.ReadAllText(filePath), Options);
            if (document?.SchemaVersion != 1) return [];
            var unique = new List<InstallerShortcutEntry>();
            foreach (var item in document.Items ?? [])
            {
                if (string.IsNullOrWhiteSpace(item.Path) ||
                    unique.Any(existing => string.Equals(existing.Path, item.Path, StringComparison.OrdinalIgnoreCase)))
                    continue;
                unique.Add(item with { Path = Path.GetFullPath(item.Path) });
                if (unique.Count == MaximumCount) break;
            }
            return unique;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            return [];
        }
    }

    public void Save(IReadOnlyList<InstallerShortcutEntry> entries)
    {
        if (entries.Count > MaximumCount) throw new InvalidOperationException($"At most {MaximumCount} shortcuts can be registered.");
        var duplicate = entries
            .GroupBy(entry => Path.GetFullPath(entry.Path), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null) throw new InvalidOperationException("The same shortcut cannot be registered twice.");

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        var temporary = filePath + ".tmp";
        var json = JsonSerializer.Serialize(new InstallerShortcutDocument(1, [.. entries]), Options);
        File.WriteAllText(temporary, json);
        if (File.Exists(filePath)) File.Replace(temporary, filePath, null);
        else File.Move(temporary, filePath);
    }
}

internal sealed record InstallerShortcutDocument(int SchemaVersion, List<InstallerShortcutEntry> Items);

internal sealed record InstallerShortcutEntry(
    string Id,
    string Path,
    string DisplayName,
    string LastKnownTarget,
    string LastResult = "確認待ち");
