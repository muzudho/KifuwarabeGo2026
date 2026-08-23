namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;
using System.Linq;

/// <summary>ローカル対局・CGOS・App Providerで共用するGTPエンジン一覧を管理します。</summary>
public sealed partial class GoAppSession
{
    public void SetGtpEngineProfiles(IEnumerable<GtpEngineProfile> profiles)
    {
        _gtpEngineProfiles.Clear();
        _gtpEngineProfiles.AddRange(profiles.Select(profile => profile.Clone()));
        if (_gtpEngineProfiles.Count == 0)
        {
            _gtpEngineProfiles.Add(new GtpEngineProfile
            {
                DisplayName = "GTP Engine Not Configured",
                ExecutablePath = "",
                WorkingDirectoryModel = WorkingDirectoryModel.Empty,
            });
        }

        SelectedBlackGtpEngineIndex = 0;
        SelectedWhiteGtpEngineIndex = 0;
        SelectedAppProviderEngineIndex = -1;
        SelectedCgosBlackGtpEngineIndex = 0;
        SelectedCgosWhiteGtpEngineIndex = 0;
        _gtpEngineAppCompatibilities.Clear();
        _gtpEngineAppCompatibilities.AddRange(_gtpEngineProfiles.Select(
            _ => new GtpEngineAppCompatibility(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY FORMAL APP")));
        SetCgosPlayerCredentials(GoStone.Black, _gtpEngineProfiles[0].DefaultCgosLoginName, _gtpEngineProfiles[0].DefaultCgosPlainTextPassword);
        SetCgosPlayerCredentials(GoStone.White, _gtpEngineProfiles[0].DefaultCgosLoginName, _gtpEngineProfiles[0].DefaultCgosPlainTextPassword);
    }

    public void OpenGtpEngineOrderEditor()
    {
        GtpEngineOrderEditor.Open(_gtpEngineProfiles, GtpEngineDialogSelectionIndex, GtpEngineSelectionPageSize, selectInitially: false);
        ActivateWindow(ActiveWindowId.CatalogOrderEditor);
    }

    public void CancelGtpEngineOrderEditor()
    {
        GtpEngineOrderEditor.Cancel();
        DeactivateWindow(ActiveWindowId.CatalogOrderEditor);
    }

    public IReadOnlyList<GtpEngineProfile> CommitGtpEngineOrderEditor()
    {
        var black = GetGtpEngineProfileOrNull(SelectedBlackGtpEngineIndex);
        var white = GetGtpEngineProfileOrNull(SelectedWhiteGtpEngineIndex);
        var cgosBlack = GetGtpEngineProfileOrNull(SelectedCgosBlackGtpEngineIndex);
        var cgosWhite = GetGtpEngineProfileOrNull(SelectedCgosWhiteGtpEngineIndex);
        var dialog = GetGtpEngineProfileOrNull(GtpEngineDialogSelectionIndex);
        var orderedProfiles = GtpEngineOrderEditor.Commit();
        DeactivateWindow(ActiveWindowId.CatalogOrderEditor);
        _gtpEngineProfiles.Clear();
        _gtpEngineProfiles.AddRange(orderedProfiles);
        SelectedBlackGtpEngineIndex = GetReorderedGtpEngineIndex(black);
        SelectedWhiteGtpEngineIndex = GetReorderedGtpEngineIndex(white);
        SelectedCgosBlackGtpEngineIndex = GetReorderedGtpEngineIndex(cgosBlack);
        SelectedCgosWhiteGtpEngineIndex = GetReorderedGtpEngineIndex(cgosWhite);
        GtpEngineDialogSelectionIndex = GetReorderedGtpEngineIndex(dialog);
        GtpEngineSelectionPageIndex = GtpEngineDialogSelectionIndex / GtpEngineSelectionPageSize;
        return _gtpEngineProfiles.Select(profile => profile.Clone()).ToArray();
    }

    private GtpEngineProfile? GetGtpEngineProfileOrNull(int? index) =>
        index is { } value && value >= 0 && value < _gtpEngineProfiles.Count
            ? _gtpEngineProfiles[value]
            : null;

    private int GetReorderedGtpEngineIndex(GtpEngineProfile? profile)
    {
        var index = profile is null ? -1 : _gtpEngineProfiles.IndexOf(profile);
        return index >= 0 ? index : 0;
    }
}
