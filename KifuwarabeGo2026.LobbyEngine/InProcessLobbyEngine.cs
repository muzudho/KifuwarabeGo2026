namespace KifuwarabeGo2026.LobbyEngine;

using KifuwarabeGo2026.GameOasis.Application.Catalogs;
using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Application.Storage;
using KifuwarabeGo2026.GameOasis.Storage;

/// <summary>既存のApplicationとStorageを同一プロセス内で利用するロビーエンジンです。</summary>
public sealed class InProcessLobbyEngine : ILobbyEngine
{
    private readonly ICatalogDocumentStore _documentStore;
    private readonly ICatalogPathProvider _paths;
    private readonly ICgosConnectionProfileStore _cgosConnectionStore;
    private readonly IReadOnlyList<GtpEngineProfile> _releaseDefaultGtpEngines;
    private readonly string _releaseDefaultDirectory;
    private GtpEngineCatalog? _gtpEngineCatalog;
    private EntryCatalog? _entryCatalog;
    private ClientIdentityCatalog? _clientIdentityCatalog;
    private CgosConnectionCatalog? _cgosConnectionCatalog;

    public InProcessLobbyEngine(
        ICatalogDocumentStore documentStore,
        ICatalogPathProvider paths,
        ICgosConnectionProfileStore cgosConnectionStore,
        IEnumerable<GtpEngineProfile> releaseDefaultGtpEngines,
        string releaseDefaultDirectory)
    {
        _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _cgosConnectionStore = cgosConnectionStore ?? throw new ArgumentNullException(nameof(cgosConnectionStore));
        _releaseDefaultGtpEngines = releaseDefaultGtpEngines?.Select(profile => profile.Clone()).ToArray()
            ?? throw new ArgumentNullException(nameof(releaseDefaultGtpEngines));
        _releaseDefaultDirectory = Path.GetFullPath(releaseDefaultDirectory ?? throw new ArgumentNullException(nameof(releaseDefaultDirectory)));
    }

    public static InProcessLobbyEngine CreateDefault(
        ICgosConnectionProfileStore cgosConnectionStore,
        IEnumerable<GtpEngineProfile> releaseDefaultGtpEngines,
        string releaseDefaultDirectory) =>
        new(
            CatalogDocumentStorage.Default,
            CatalogDocumentStorage.Paths,
            cgosConnectionStore,
            releaseDefaultGtpEngines,
            releaseDefaultDirectory);

    public LobbyState LoadState()
    {
        _gtpEngineCatalog = GtpEngineCatalog.LoadFromDefaultLocation(
            _documentStore,
            _paths,
            _releaseDefaultGtpEngines,
            _releaseDefaultDirectory);
        _cgosConnectionCatalog = CgosConnectionCatalog.Load(_cgosConnectionStore);
        _entryCatalog = EntryCatalog.LoadFromDefaultLocation(
            _documentStore,
            _paths,
            _gtpEngineCatalog.Profiles);

        var connectionNamesById = _cgosConnectionCatalog.Profiles
            .GroupBy(profile => profile.Id, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().DisplayName, StringComparer.Ordinal);
        _clientIdentityCatalog = ClientIdentityCatalog.LoadFromDefaultLocation(
            _documentStore,
            _paths,
            _entryCatalog.Profiles,
            _gtpEngineCatalog.Profiles,
            connectionNamesById);

        if (_clientIdentityCatalog.EntryProfilesChanged)
            _entryCatalog.Save(_clientIdentityCatalog.EntryProfiles);

        return new LobbyState(
            _gtpEngineCatalog.Profiles.Select(profile => profile.Clone()).ToArray(),
            _clientIdentityCatalog.EntryProfiles.Select(profile => profile.Clone()).ToArray(),
            _clientIdentityCatalog.Profiles.Select(profile => profile.Clone()).ToArray(),
            _cgosConnectionCatalog.Profiles.ToArray(),
            _gtpEngineCatalog.ListPath,
            _entryCatalog.ListPath,
            _clientIdentityCatalog.ListPath,
            _cgosConnectionCatalog.ListPath,
            _gtpEngineCatalog.DuplicateIdsRepaired);
    }

    public void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles) =>
        RequireLoaded(_gtpEngineCatalog, nameof(LoadState)).Save(Clone(profiles));

    public void SaveEntries(IEnumerable<EntryProfile> profiles) =>
        RequireLoaded(_entryCatalog, nameof(LoadState)).Save(Clone(profiles));

    public void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles) =>
        RequireLoaded(_clientIdentityCatalog, nameof(LoadState)).Save(Clone(profiles));

    public void SaveEntriesAndClientIdentities(
        IEnumerable<EntryProfile> entries,
        IEnumerable<ClientIdentityProfile> clientIdentities)
    {
        // Targetを先に保存し、Entryが未保存のTargetを参照する瞬間を避けます。
        SaveClientIdentities(clientIdentities);
        SaveEntries(entries);
    }

    public void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles) =>
        RequireLoaded(_cgosConnectionCatalog, nameof(LoadState)).Save(profiles.ToArray());

    private static T RequireLoaded<T>(T? value, string loadMethod) where T : class =>
        value ?? throw new InvalidOperationException($"Call {loadMethod} before using the lobby engine.");

    private static GtpEngineProfile[] Clone(IEnumerable<GtpEngineProfile> profiles) =>
        profiles.Select(profile => profile.Clone()).ToArray();

    private static EntryProfile[] Clone(IEnumerable<EntryProfile> profiles) =>
        profiles.Select(profile => profile.Clone()).ToArray();

    private static ClientIdentityProfile[] Clone(IEnumerable<ClientIdentityProfile> profiles) =>
        profiles.Select(profile => profile.Clone()).ToArray();
}
