namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.GameOasis.Storage;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Composes physical storage and GUI-owned connection profiles for persistent entry catalogs.</summary>
public static class EntryCatalogComposition
{
    public static EntryCatalog LoadEntries(IEnumerable<GtpEngineProfile> engines) =>
        EntryCatalog.LoadFromDefaultLocation(CatalogDocumentStorage.Default, CatalogDocumentStorage.Paths, engines);

    public static ClientIdentityCatalog LoadClientIdentities(
        IEnumerable<EntryProfile> entries,
        IEnumerable<GtpEngineProfile> engines,
        IEnumerable<CgosConnectionProfile> connections)
    {
        var connectionNamesById = connections
            .GroupBy(profile => profile.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().DisplayName, StringComparer.Ordinal);
        return ClientIdentityCatalog.LoadFromDefaultLocation(
            CatalogDocumentStorage.Default, CatalogDocumentStorage.Paths,
            entries, engines, connectionNamesById);
    }
}
