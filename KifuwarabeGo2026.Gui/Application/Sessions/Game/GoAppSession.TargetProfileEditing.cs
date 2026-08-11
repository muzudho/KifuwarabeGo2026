namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Linq;

/// <summary>Player が参照する TargetProfile の編集状態。</summary>
public sealed partial class GoAppSession
{
    public bool IsTargetProfileEditPanelOpen { get; private set; }
    public int TargetProfileEditIndex { get; private set; }
    public TargetProfile TargetProfileEditDraft { get; private set; } = new();
    public TargetProfileEditField? ActiveTargetProfileEditField { get; private set; }

    public bool OpenTargetProfileEditPanel()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count) return false;
        IsTargetProfileEditPanelOpen = true;
        TargetProfileEditIndex = 0;
        return LoadTargetProfileEditDraft();
    }

    public void CloseTargetProfileEditPanel() =>
        (IsTargetProfileEditPanelOpen, ActiveTargetProfileEditField) = (false, null);

    public bool MoveTargetProfileEditSelection(int step)
    {
        var ids = GetTargetEditOwner().TargetProfileIds;
        if (ids.Count == 0) return false;
        TargetProfileEditIndex = (TargetProfileEditIndex + step + ids.Count) % ids.Count;
        return LoadTargetProfileEditDraft();
    }

    public void AddTargetProfile(bool cgos)
    {
        var owner = GetTargetEditOwner();
        var target = new TargetProfile
        {
            DisplayName = cgos ? "CGOS" : "LocalMatch",
            ConnectionProfileId = cgos ? _cgosConnectionProfiles.ElementAtOrDefault(SelectedCgosConnectionProfileIndex)?.Id ?? "" : "",
            LoginName = owner.Identifier,
        };
        _targetProfiles.Add(target);
        owner.TargetProfileIds.Add(target.Id);
        PlayerEditDraft.TargetProfileIds = owner.TargetProfileIds.ToList();
        TargetProfileEditIndex = owner.TargetProfileIds.Count - 1;
        LoadTargetProfileEditDraft();
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

    public void SaveTargetProfileEditDraft()
    {
        var index = _targetProfiles.FindIndex(target => string.Equals(target.Id, TargetProfileEditDraft.Id, StringComparison.Ordinal));
        if (index < 0) return;
        TargetProfileEditDraft.DisplayName = string.IsNullOrWhiteSpace(TargetProfileEditDraft.DisplayName) ? "New Target" : TargetProfileEditDraft.DisplayName.Trim();
        _targetProfiles[index] = TargetProfileEditDraft.Clone();
    }

    public void CycleTargetProfileConnection(int step)
    {
        if (_cgosConnectionProfiles.Count == 0 || string.IsNullOrEmpty(TargetProfileEditDraft.ConnectionProfileId)) return;
        var index = _cgosConnectionProfiles.FindIndex(profile => string.Equals(profile.Id, TargetProfileEditDraft.ConnectionProfileId, StringComparison.Ordinal));
        TargetProfileEditDraft.ConnectionProfileId = _cgosConnectionProfiles[(Math.Max(0, index) + step + _cgosConnectionProfiles.Count) % _cgosConnectionProfiles.Count].Id;
    }

    public string TargetProfileEditConnectionDisplayName =>
        _cgosConnectionProfiles.FirstOrDefault(profile => string.Equals(profile.Id, TargetProfileEditDraft.ConnectionProfileId, StringComparison.Ordinal))?.DisplayName ?? "LOCAL MATCH";

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
