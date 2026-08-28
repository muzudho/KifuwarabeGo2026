namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.LobbyEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>ロビーの表示状態と利用者コマンドを、描画ループから分離します。</summary>
public sealed class LobbyGuiController : ILobbyGuiCommands
{
    private readonly ILobbyEngine _engine;
    private readonly ITournamentRulesCatalog _tournamentRules;

    public LobbyGuiController(ILobbyEngine engine, ITournamentRulesCatalog tournamentRules)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _tournamentRules = tournamentRules ?? throw new ArgumentNullException(nameof(tournamentRules));
    }

    public ITournamentRulesCatalog TournamentRules => _tournamentRules;

    public static LobbyGuiController CreateDefault()
    {
        var releaseDefaultDirectory = Path.GetDirectoryName(ReleaseDefaultSettings.FilePath) ?? AppContext.BaseDirectory;
        var engine = InProcessLobbyEngine.CreateDefault(
            ApplicationSettingsCgosConnectionStore.Instance,
            ReleaseDefaultSettings.Current.EngineSettings.GtpEngines,
            releaseDefaultDirectory);
        return new LobbyGuiController(engine, TournamentRulesCatalog.LoadFromDefaultLocation());
    }

    public LobbyViewState LoadViewState()
    {
        var state = _engine.LoadState();
        return new LobbyViewState(
            _tournamentRules.Rules.Select(rule => rule.Clone()).ToArray(),
            state.GtpEngines.Select(profile => profile.Clone()).ToArray(),
            state.Entries.Select(profile => profile.Clone()).ToArray(),
            state.ClientIdentities.Select(profile => profile.Clone()).ToArray(),
            state.CgosConnections.ToArray(),
            ApplicationSettings.FilePath,
            state.GtpEngineListPath,
            state.DuplicateGtpEngineIdsRepaired);
    }

    public void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles) => _engine.SaveGtpEngines(profiles);
    public void SaveEntries(IEnumerable<EntryProfile> profiles) => _engine.SaveEntries(profiles);
    public void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles) => _engine.SaveClientIdentities(profiles);
    public void SaveEntriesAndClientIdentities(IEnumerable<EntryProfile> entries, IEnumerable<ClientIdentityProfile> clientIdentities) =>
        _engine.SaveEntriesAndClientIdentities(entries, clientIdentities);
    public void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles) => _engine.SaveCgosConnections(profiles);
}
