namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Application.Profiles;
using KifuwarabeGo2026.LobbyEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>ロビーの表示状態と利用者コマンドを描画ループから分離します。</summary>
public sealed class LobbyGuiController : ILobbyGuiCommands
{
    private readonly ILobbyEngine _engine;
    private readonly string _applicationSettingsPath;
    private readonly Func<string?> _communicationWarningProvider;

    public LobbyGuiController(
        ILobbyEngine engine,
        string applicationSettingsPath,
        Func<string?>? communicationWarningProvider = null)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _applicationSettingsPath = applicationSettingsPath ?? throw new ArgumentNullException(nameof(applicationSettingsPath));
        _communicationWarningProvider = communicationWarningProvider ?? (() => null);
    }

    public LobbyViewState LoadViewState()
    {
        var state = _engine.LoadState();
        return new LobbyViewState(
            state.GtpEngines.Select(profile => profile.Clone()).ToArray(),
            state.Entries.Select(profile => profile.Clone()).ToArray(),
            state.ClientIdentities.Select(profile => profile.Clone()).ToArray(),
            state.CgosConnections.ToArray(),
            _applicationSettingsPath,
            state.GtpEngineListPath,
            state.DuplicateGtpEngineIdsRepaired,
            _communicationWarningProvider());
    }

    public void SaveGtpEngines(IEnumerable<GtpEngineProfile> profiles) => _engine.SaveGtpEngines(profiles);
    public void SaveEntries(IEnumerable<EntryProfile> profiles) => _engine.SaveEntries(profiles);
    public void SaveClientIdentities(IEnumerable<ClientIdentityProfile> profiles) => _engine.SaveClientIdentities(profiles);
    public void SaveEntriesAndClientIdentities(IEnumerable<EntryProfile> entries, IEnumerable<ClientIdentityProfile> clientIdentities) =>
        _engine.SaveEntriesAndClientIdentities(entries, clientIdentities);
    public void SaveCgosConnections(IEnumerable<CgosConnectionProfile> profiles) => _engine.SaveCgosConnections(profiles);
}
