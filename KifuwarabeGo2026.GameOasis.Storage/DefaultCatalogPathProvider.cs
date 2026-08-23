namespace KifuwarabeGo2026.GameOasis.Storage;

using KifuwarabeGo2026.GameOasis.Application.Storage;
using System;
using System.IO;

public sealed class DefaultCatalogPathProvider : ICatalogPathProvider
{
    private readonly string _baseDirectory;

    public DefaultCatalogPathProvider(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? AppContext.BaseDirectory;
        GtpEngineListPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KifuwarabeGo2026", "GtpEngines", "gtp-engine-list.json");
    }

    public string GtpEngineListPath { get; }

    public string? FindDevelopmentGtpEngineListPath()
    {
        var directory = new DirectoryInfo(_baseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "KifuwarabeGo2026.slnx")))
            {
                var path = Path.Combine(directory.FullName, "KifuwarabeGo2026.Gui", "Content",
                    "GtpEngines", "gtp-engine-list.json");
                return File.Exists(path) ? path : null;
            }
            directory = directory.Parent;
        }
        return null;
    }
}
