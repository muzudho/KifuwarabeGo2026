namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;
using System.Collections.Generic;

/// <summary>ロビー画面へ投影する、盤面や棋譜を含まない開始前状態です。</summary>
public sealed record LobbyViewState(
    IReadOnlyList<TournamentRules> TournamentRules,
    IReadOnlyList<GtpEngineProfile> GtpEngines,
    IReadOnlyList<EntryProfile> Entries,
    IReadOnlyList<ClientIdentityProfile> ClientIdentities,
    IReadOnlyList<CgosConnectionProfile> CgosConnections,
    string ApplicationSettingsPath,
    string GtpEngineSettingsPath,
    bool DuplicateGtpEngineIdsRepaired,
    string? CommunicationWarning = null);
