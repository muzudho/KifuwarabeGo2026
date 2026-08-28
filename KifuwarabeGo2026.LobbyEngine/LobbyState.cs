namespace KifuwarabeGo2026.LobbyEngine;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>ロビーの起動時に読み込む、描画技術に依存しないカタログ状態です。</summary>
public sealed record LobbyState(
    IReadOnlyList<GtpEngineProfile> GtpEngines,
    IReadOnlyList<EntryProfile> Entries,
    IReadOnlyList<ClientIdentityProfile> ClientIdentities,
    IReadOnlyList<CgosConnectionProfile> CgosConnections,
    string GtpEngineListPath,
    string EntryListPath,
    string ClientIdentityListPath,
    string CgosConnectionListPath,
    bool DuplicateGtpEngineIdsRepaired);
