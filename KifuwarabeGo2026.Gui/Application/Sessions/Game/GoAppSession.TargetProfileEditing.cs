namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Player が参照する TargetProfile の編集状態。</summary>
public sealed partial class GoAppSession
{
    public const int TargetProfileConnectionSelectionPageSize = 5;
    public bool IsTargetProfileEditPanelOpen { get; private set; }
    public bool IsTargetProfileConnectionSelectionPanelOpen { get; private set; }
    public int TargetProfileConnectionSelectionIndex { get; private set; }
    public int TargetProfileConnectionSelectionPageIndex { get; private set; }
    public bool IsQuickTargetSelectionPanelOpen { get; private set; }
    public GoStone QuickTargetSelectionStone { get; private set; }
    public bool QuickTargetSelectionIsCgos { get; private set; }
    public int QuickTargetSelectionIndex { get; private set; }
    public int TargetProfileEditIndex { get; private set; }
    public TargetProfile TargetProfileEditDraft { get; private set; } = new();
    public TargetProfileEditField? ActiveTargetProfileEditField { get; private set; }
    public int TargetProfileEditCaretIndex { get; private set; }
    public int TargetProfileEditSelectionStart { get; private set; }
    public int TargetProfileEditSelectionLength { get; private set; }
    private string TargetProfileEditOriginalFieldText { get; set; } = "";

    public bool OpenTargetProfileEditPanel()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count) return false;
        IsTargetProfileEditPanelOpen = true;
        TargetProfileEditIndex = Math.Max(0, GetTargetEditOwner().TargetProfileIds.FindIndex(id => string.Equals(id, PlayerEditDraft.TargetProfileIds.FirstOrDefault(), StringComparison.Ordinal)));
        return LoadTargetProfileEditDraft();
    }

    public void CloseTargetProfileEditPanel() =>
        (IsTargetProfileEditPanelOpen, IsTargetProfileConnectionSelectionPanelOpen, ActiveTargetProfileEditField) = (false, false, null);

    public bool OpenQuickTargetSelectionPanel(GoStone stone, bool cgos)
    {
        var targets = GetQuickTargetSelectionTargets(stone, cgos);
        if (targets.Count == 0) return false;
        QuickTargetSelectionStone = stone;
        QuickTargetSelectionIsCgos = cgos;
        var current = cgos ? GetSelectedCgosTargetProfile(stone)?.Id : GetSelectedLocalMatchTargetProfile(stone)?.Id;
        QuickTargetSelectionIndex = Math.Max(0, targets.FindIndex(target => string.Equals(target.Id, current, StringComparison.Ordinal)));
        IsQuickTargetSelectionPanelOpen = true;
        return true;
    }

    public void CancelQuickTargetSelectionPanel() => IsQuickTargetSelectionPanelOpen = false;

    public void SelectQuickTarget(int index)
    {
        if (index < 0 || index >= GetQuickTargetSelectionTargets(QuickTargetSelectionStone, QuickTargetSelectionIsCgos).Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        QuickTargetSelectionIndex = index;
    }

    public bool CommitQuickTargetSelection()
    {
        var targets = GetQuickTargetSelectionTargets(QuickTargetSelectionStone, QuickTargetSelectionIsCgos);
        if (QuickTargetSelectionIndex < 0 || QuickTargetSelectionIndex >= targets.Count) return false;
        var selected = QuickTargetSelectionIsCgos
            ? TrySelectCgosTargetProfile(QuickTargetSelectionStone, targets[QuickTargetSelectionIndex].Id)
            : TrySelectLocalMatchTargetProfile(QuickTargetSelectionStone, targets[QuickTargetSelectionIndex].Id);
        if (selected) IsQuickTargetSelectionPanelOpen = false;
        return selected;
    }

    public List<TargetProfile> GetQuickTargetSelectionTargets(GoStone stone, bool cgos)
    {
        var player = cgos
            ? stone == GoStone.Black ? SelectedCgosBlackPlayerProfile : SelectedCgosWhitePlayerProfile
            : GetSelectedPlayerProfile(stone);
        if (player is null) return [];
        var connectionId = cgos ? SelectedCgosConnectionProfile.Id : "";
        return GetPlayerTargetProfiles(player.Id)
            .Where(target => cgos
                ? string.Equals(target.ConnectionProfileId, connectionId, StringComparison.Ordinal)
                : string.IsNullOrEmpty(target.ConnectionProfileId))
            .ToList();
    }

    public bool MoveTargetProfileEditSelection(int step)
    {
        var ids = GetTargetEditOwner().TargetProfileIds;
        if (ids.Count == 0) return false;
        TargetProfileEditIndex = (TargetProfileEditIndex + step + ids.Count) % ids.Count;
        return LoadTargetProfileEditDraft();
    }

    public bool AddTargetProfile(bool cgos)
    {
        var owner = GetTargetEditOwner();
        if (owner.TargetProfileIds.Count >= 5) return false;
        var target = new TargetProfile
        {
            DisplayName = cgos ? "OnlineMatch (CGOS)" : "LocalMatch",
            ConnectionProfileId = cgos ? _cgosConnectionProfiles.ElementAtOrDefault(SelectedCgosConnectionProfileIndex)?.Id ?? "" : "",
            LoginName = owner.Identifier,
        };
        _targetProfiles.Add(target);
        owner.TargetProfileIds.Add(target.Id);
        PlayerEditDraft.TargetProfileIds = owner.TargetProfileIds.ToList();
        TargetProfileEditIndex = owner.TargetProfileIds.Count - 1;
        LoadTargetProfileEditDraft();
        return true;
    }

    public bool RemoveTargetProfile()
    {
        var owner = GetTargetEditOwner();
        if (owner.TargetProfileIds.Count <= 1) return false;
        var id = owner.TargetProfileIds[TargetProfileEditIndex];
        owner.TargetProfileIds.RemoveAt(TargetProfileEditIndex);
        if (!_playerProfiles.Any(player => player.TargetProfileIds.Contains(id, StringComparer.Ordinal)))
            _targetProfiles.RemoveAll(target => string.Equals(target.Id, id, StringComparison.Ordinal));
        PlayerEditDraft.TargetProfileIds = owner.TargetProfileIds.ToList();
        TargetProfileEditIndex = Math.Clamp(TargetProfileEditIndex, 0, owner.TargetProfileIds.Count - 1);
        return LoadTargetProfileEditDraft();
    }

    /// <summary>選択 Target をこの Player の既定の使用先として先頭へ移動する。</summary>
    public bool UseTargetProfile()
    {
        var owner = GetTargetEditOwner();
        if (TargetProfileEditIndex < 0 || TargetProfileEditIndex >= owner.TargetProfileIds.Count)
            return false;

        var id = owner.TargetProfileIds[TargetProfileEditIndex];
        owner.TargetProfileIds.RemoveAt(TargetProfileEditIndex);
        owner.TargetProfileIds.Insert(0, id);
        PlayerEditDraft.TargetProfileIds = owner.TargetProfileIds.ToList();
        TargetProfileEditIndex = 0;
        return LoadTargetProfileEditDraft();
    }

    public bool IsTargetProfileInUse(int index) =>
        index == 0 && GetTargetEditOwner().TargetProfileIds.Count > 0;

    public void SetTargetProfileEditField(TargetProfileEditField field, string value)
    {
        if (field == TargetProfileEditField.DisplayName) TargetProfileEditDraft.DisplayName = value;
        else if (field == TargetProfileEditField.LoginName) TargetProfileEditDraft.LoginName = value;
        else TargetProfileEditDraft.LoginPass = value;
    }

    public string GetTargetProfileEditField(TargetProfileEditField field) => field switch
    {
        TargetProfileEditField.DisplayName => TargetProfileEditDraft.DisplayName,
        TargetProfileEditField.LoginName => TargetProfileEditDraft.LoginName,
        TargetProfileEditField.LoginPass => TargetProfileEditDraft.LoginPass,
        _ => "",
    };

    public void BeginTargetProfileEditField(TargetProfileEditField field, int caretIndex)
    {
        ActiveTargetProfileEditField = field;
        TargetProfileEditOriginalFieldText = GetTargetProfileEditField(field);
        TargetProfileEditCaretIndex = Math.Clamp(caretIndex, 0, TargetProfileEditOriginalFieldText.Length);
        TargetProfileEditSelectionStart = TargetProfileEditCaretIndex;
        TargetProfileEditSelectionLength = 0;
    }

    public void SetTargetProfileEditFieldText(TargetProfileEditField field, string value, int caretIndex, int selectionStart, int selectionLength)
    {
        SetTargetProfileEditField(field, value);
        TargetProfileEditCaretIndex = Math.Clamp(caretIndex, 0, value.Length);
        TargetProfileEditSelectionStart = Math.Clamp(selectionStart, 0, value.Length);
        TargetProfileEditSelectionLength = Math.Clamp(selectionLength, 0, value.Length - TargetProfileEditSelectionStart);
    }

    public void EndTargetProfileEditField() => ActiveTargetProfileEditField = null;

    public void CancelTargetProfileEditField()
    {
        if (ActiveTargetProfileEditField is { } field)
            SetTargetProfileEditField(field, TargetProfileEditOriginalFieldText);
        ActiveTargetProfileEditField = null;
    }

    public void SaveTargetProfileEditDraft()
    {
        var index = _targetProfiles.FindIndex(target => string.Equals(target.Id, TargetProfileEditDraft.Id, StringComparison.Ordinal));
        if (index < 0) return;
        TargetProfileEditDraft.DisplayName = string.IsNullOrWhiteSpace(TargetProfileEditDraft.DisplayName) ? "New Target" : TargetProfileEditDraft.DisplayName.Trim();
        _targetProfiles[index] = TargetProfileEditDraft.Clone();
    }

    public bool OpenTargetProfileConnectionSelectionPanel()
    {
        if (_cgosConnectionProfiles.Count == 0 || string.IsNullOrEmpty(TargetProfileEditDraft.ConnectionProfileId))
            return false;

        TargetProfileConnectionSelectionIndex = Math.Max(0, _cgosConnectionProfiles.FindIndex(profile =>
            string.Equals(profile.Id, TargetProfileEditDraft.ConnectionProfileId, StringComparison.Ordinal)));
        TargetProfileConnectionSelectionPageIndex = TargetProfileConnectionSelectionIndex / TargetProfileConnectionSelectionPageSize;
        IsTargetProfileConnectionSelectionPanelOpen = true;
        return true;
    }

    public void CancelTargetProfileConnectionSelectionPanel() => IsTargetProfileConnectionSelectionPanelOpen = false;

    public void SelectTargetProfileConnection(int index)
    {
        if (index < 0 || index >= _cgosConnectionProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        TargetProfileConnectionSelectionIndex = index;
    }

    public bool CommitTargetProfileConnectionSelection()
    {
        if (TargetProfileConnectionSelectionIndex < 0 || TargetProfileConnectionSelectionIndex >= _cgosConnectionProfiles.Count)
            return false;
        TargetProfileEditDraft.ConnectionProfileId = _cgosConnectionProfiles[TargetProfileConnectionSelectionIndex].Id;
        IsTargetProfileConnectionSelectionPanelOpen = false;
        return true;
    }

    public void MoveTargetProfileConnectionSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)TargetProfileConnectionSelectionPageSize));
        TargetProfileConnectionSelectionPageIndex = Math.Clamp(TargetProfileConnectionSelectionPageIndex + step, 0, pageCount - 1);
    }

    public int TargetProfileConnectionSelectionPageCount =>
        Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)TargetProfileConnectionSelectionPageSize));

    public string TargetProfileEditConnectionDisplayName =>
        GetTargetProfileConnectionDisplayName(TargetProfileEditDraft);

    public string GetTargetProfileConnectionDisplayName(TargetProfile target) =>
        _cgosConnectionProfiles.FirstOrDefault(profile => string.Equals(profile.Id, target.ConnectionProfileId, StringComparison.Ordinal))?.DisplayName ?? "LOCAL MATCH";

    private PlayerProfile GetTargetEditOwner() => _playerProfiles[PlayerEditProfileIndex];

    private bool LoadTargetProfileEditDraft()
    {
        var ids = GetTargetEditOwner().TargetProfileIds;
        if (ids.Count == 0) return false;
        var target = _targetProfiles.FirstOrDefault(item => string.Equals(item.Id, ids[TargetProfileEditIndex], StringComparison.Ordinal));
        if (target is null) return false;
        TargetProfileEditDraft = target.Clone();
        ActiveTargetProfileEditField = null;
        return true;
    }
}

public enum TargetProfileEditField { DisplayName, LoginName, LoginPass }
