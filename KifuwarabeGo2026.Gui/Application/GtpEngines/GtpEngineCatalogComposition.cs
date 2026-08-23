namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GameOasis.Storage;
using System;
using System.IO;

/// <summary>Composes release defaults and physical storage for the Game Oasis engine catalog.</summary>
public static class GtpEngineCatalogComposition
{
    public static GtpEngineCatalog LoadFromDefaultLocation()
    {
        var defaultDirectory = Path.GetDirectoryName(ReleaseDefaultSettings.FilePath) ?? AppContext.BaseDirectory;
        return GtpEngineCatalog.LoadFromDefaultLocation(
            CatalogDocumentStorage.Default,
            CatalogDocumentStorage.Paths,
            ReleaseDefaultSettings.Current.EngineSettings.GtpEngines,
            defaultDirectory);
    }
}
