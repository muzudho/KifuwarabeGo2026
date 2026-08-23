namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>ローカル対局・CGOSに割り当てるGTPエンジンを選択します。</summary>
public sealed partial class GoAppSession
{
    public bool IsGtpEngineCompatibilityLoading { get; private set; }

    public void OpenGtpEngineSelectionDialog(GoStone stone, string appId = "play")
    {
        IsGtpEngineSelectionForCgos = false;
        EngineSelectionPurpose = GtpEngineSelectionPurpose.LocalPlayer;
        GtpEngineSelectionAppId = appId;
        OpenGtpEngineSelectionDialogCore(stone);
    }

    public void BeginGtpEngineCompatibilityLoading()
    {
        IsGtpEngineCompatibilityLoading = true;
        _gtpEngineAppCompatibilities.Clear();
        _gtpEngineAppCompatibilities.AddRange(
            _gtpEngineProfiles.Select(_ => new GtpEngineAppCompatibility(
                GtpEngineAppCompatibilityKind.CheckFailed,
                "CHECKING...")));
    }

    public void OpenCgosGtpEngineSelectionDialog(GoStone stone)
    {
        IsGtpEngineSelectionForCgos = true;
        EngineSelectionPurpose = GtpEngineSelectionPurpose.CgosPlayer;
        GtpEngineSelectionAppId = "play";
        OpenGtpEngineSelectionDialogCore(stone);
    }

    public void OpenPlayerEditGtpEngineSelectionDialog()
    {
        IsGtpEngineSelectionForCgos = false;
        EngineSelectionPurpose = GtpEngineSelectionPurpose.PlayerEdit;
        GtpEngineSelectionAppId = "play";
        OpenGtpEngineSelectionDialogCore(GoStone.Black);
    }

    public void OpenAppProviderGtpEngineSelectionDialog(string appId)
    {
        IsGtpEngineSelectionForCgos = false;
        EngineSelectionPurpose = GtpEngineSelectionPurpose.AppProvider;
        GtpEngineSelectionAppId = appId;
        OpenGtpEngineSelectionDialogCore(GoStone.Empty);
    }

    public void OpenGtpEngineManagementDialog()
    {
        IsGtpEngineSelectionForCgos = false;
        EngineSelectionPurpose = GtpEngineSelectionPurpose.Management;
        GtpEngineSelectionAppId = "";
        OpenGtpEngineSelectionDialogCore(GoStone.Empty);
    }

    private void OpenGtpEngineSelectionDialogCore(GoStone stone)
    {
        if (EngineSelectionPurpose is not (GtpEngineSelectionPurpose.AppProvider or GtpEngineSelectionPurpose.PlayerEdit or GtpEngineSelectionPurpose.Management) &&
            stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be selected only for black or white.");
        }

        IsTournamentRulesSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.TournamentRulesSelection);
        IsTournamentRulesAddPanelOpen = false;
        DeactivateWindow(ActiveWindowId.TournamentRulesEdit);
        IsTournamentRulesDeleteConfirmationOpen = false;
        DeactivateWindow(ActiveWindowId.TournamentRulesDeleteConfirmation);
        IsGtpEngineEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineEdit);
        IsGtpEngineAddPanelMode = false;
        IsGtpEngineSelectionDialogOpen = true;
        ActivateWindow(ActiveWindowId.GtpEngineSelection);
        IsGtpEngineDeleteConfirmationOpen = false;
        GtpEngineSelectionTargetStone = stone;
        var selectedIndex = SelectedGtpEngineIndex;
        if (EngineSelectionPurpose == GtpEngineSelectionPurpose.LocalPlayer &&
            !CanSelectGtpEngineForCurrentApp(selectedIndex))
        {
            if (stone == GoStone.Black)
                SelectedBlackGtpEngineIndex = -1;
            else
                SelectedWhiteGtpEngineIndex = -1;
            selectedIndex = -1;
        }

        if (EngineSelectionPurpose == GtpEngineSelectionPurpose.Management)
            selectedIndex = _gtpEngineProfiles.Count > 0 ? 0 : -1;
        GtpEngineDialogSelectionIndex = EngineSelectionPurpose is GtpEngineSelectionPurpose.AppProvider or GtpEngineSelectionPurpose.PlayerEdit or GtpEngineSelectionPurpose.Management
            ? selectedIndex >= 0 && selectedIndex < _gtpEngineProfiles.Count ? selectedIndex : -1
            : CanSelectGtpEngineForCurrentApp(selectedIndex) ? selectedIndex : -1;
        GtpEngineSelectionPageIndex = Math.Max(0, selectedIndex) / GtpEngineSelectionPageSize;
    }

    public void SelectGtpEngineDialogItem(int index)
    {
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "GTP engine index is out of range.");
        if (EngineSelectionPurpose is GtpEngineSelectionPurpose.AppProvider or GtpEngineSelectionPurpose.Management || CanSelectGtpEngineForCurrentApp(index))
            GtpEngineDialogSelectionIndex = index;
    }

    public void CommitGtpEngineSelectionDialog()
    {
        if (!CanCommitGtpEngineSelection)
            return;

        if (EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider)
            SelectAppProviderEngine(GtpEngineDialogSelectionIndex);
        else if (EngineSelectionPurpose == GtpEngineSelectionPurpose.PlayerEdit)
            SetPlayerEditEngineProfile(_gtpEngineProfiles[GtpEngineDialogSelectionIndex].Id);
        else
            SelectGtpEngine(GtpEngineSelectionTargetStone, GtpEngineDialogSelectionIndex);
        IsGtpEngineSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineSelection);
        CloseGtpEngineDeleteConfirmation();
    }

    public void CancelGtpEngineSelectionDialog()
    {
        IsGtpEngineSelectionDialogOpen = false;
        DeactivateWindow(ActiveWindowId.GtpEngineSelection);
        CloseGtpEngineDeleteConfirmation();
    }

    public void MoveGtpEngineSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_gtpEngineProfiles.Count / (double)GtpEngineSelectionPageSize));
        GtpEngineSelectionPageIndex = Math.Clamp(GtpEngineSelectionPageIndex + step, 0, pageCount - 1);
    }

    public bool CanDeleteSelectedGtpEngine =>
        _gtpEngineProfiles.Count > 1 &&
        GtpEngineDialogSelectionIndex >= 0 &&
        GtpEngineDialogSelectionIndex < _gtpEngineProfiles.Count;

    public int SelectedGtpEngineIndex => EngineSelectionPurpose switch
    {
        GtpEngineSelectionPurpose.AppProvider => SelectedAppProviderEngineIndex,
        GtpEngineSelectionPurpose.CgosPlayer => GetSelectedCgosGtpEngineIndex(GtpEngineSelectionTargetStone) ?? -1,
        GtpEngineSelectionPurpose.PlayerEdit => FindGtpEngineIndex(PlayerEditDraft.EngineProfileId),
        _ => GtpEngineSelectionTargetStone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex,
    };

    public bool CanCommitGtpEngineSelection => CanSelectGtpEngineForCurrentApp(GtpEngineDialogSelectionIndex);

    public bool CanSelectGtpEngineForCurrentApp(int index) =>
        index >= 0 &&
        index < _gtpEngineProfiles.Count &&
        index < _gtpEngineAppCompatibilities.Count &&
        _gtpEngineAppCompatibilities[index].CanSelect;

    public GtpEngineAppCompatibility GetGtpEngineAppCompatibility(int index) =>
        index >= 0 && index < _gtpEngineAppCompatibilities.Count
            ? _gtpEngineAppCompatibilities[index]
            : new(GtpEngineAppCompatibilityKind.CheckFailed, "NOT CHECKED");

    public void SetGtpEngineAppCompatibilities(IEnumerable<GtpEngineAppCompatibility> compatibilities)
    {
        IsGtpEngineCompatibilityLoading = false;
        _gtpEngineAppCompatibilities.Clear();
        _gtpEngineAppCompatibilities.AddRange(compatibilities);
    }

    private GtpEngineProfile? GetCgosGtpEngineProfile(int? index) =>
        index is { } selectedIndex && selectedIndex >= 0 && selectedIndex < _gtpEngineProfiles.Count
            ? _gtpEngineProfiles[selectedIndex]
            : null;

    private int? GetSelectedCgosGtpEngineIndex(GoStone stone) => stone switch
    {
        GoStone.Black => SelectedCgosBlackGtpEngineIndex,
        GoStone.White => SelectedCgosWhiteGtpEngineIndex,
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white."),
    };

    private void SetSelectedCgosGtpEngineIndex(GoStone stone, int? index)
    {
        if (stone == GoStone.Black)
        {
            SelectedCgosBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedCgosWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white.");
    }

    public void SelectGtpEngine(GoStone stone, int index)
    {
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "GTP engine index is out of range.");

        if (IsGtpEngineSelectionForCgos)
        {
            SetSelectedCgosGtpEngineIndex(stone, index);
            var target = GetSelectedCgosClientIdentityProfile(stone);
            SetCgosPlayerCredentials(
                stone,
                target?.LoginName ?? _gtpEngineProfiles[index].DefaultCgosLoginName,
                target?.LoginPass ?? _gtpEngineProfiles[index].DefaultCgosPlainTextPassword);
            return;
        }

        if (stone == GoStone.Black)
        {
            SelectedBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be selected only for black or white.");
    }

    public GtpEngineProfile GetGtpEngineProfile(GoStone stone)
    {
        var index = stone switch
        {
            GoStone.Black => SelectedBlackGtpEngineIndex,
            GoStone.White => SelectedWhiteGtpEngineIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be read only for black or white."),
        };

        return _gtpEngineProfiles[Math.Clamp(index, 0, _gtpEngineProfiles.Count - 1)].Clone();
    }
}
