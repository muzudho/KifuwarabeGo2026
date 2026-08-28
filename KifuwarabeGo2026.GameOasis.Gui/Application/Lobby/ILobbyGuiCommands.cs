namespace KifuwarabeGo2026.GameOasis.Gui.Application.Lobby;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using System.Collections.Generic;

/// <summary>ロビーGUIの操作を永続化へ接続するコマンド境界です。</summary>
public interface ILobbyGuiCommands
{
    LobbyViewState LoadViewState();
    void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles);
    void SaveEntries(IEnumerable<EntryProfile> profiles);
    void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles);
    void SaveEntriesAndClientIdentities(
        IEnumerable<EntryProfile> entries,
        IEnumerable<ClientIdentityProfile> clientIdentities);
    void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles);
}
