namespace KifuwarabeGo2026.LobbyEngine;

using KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>ロビーGUIがカタログの読込と保存を依頼する同一プロセス境界です。</summary>
public interface ILobbyEngine
{
    LobbyState LoadState();
    void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles);
    void SaveEntries(IEnumerable<EntryProfile> profiles);
    void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles);
    void SaveEntriesAndClientIdentities(
        IEnumerable<EntryProfile> entries,
        IEnumerable<ClientIdentityProfile> clientIdentities);
    void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles);
}
