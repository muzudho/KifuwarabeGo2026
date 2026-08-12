namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Player が参照する ClientIdentityProfile の編集状態。</summary>
public sealed partial class GoAppSession
{
    public const int ClientIdentityProfileConnectionSelectionPageSize = 5;
    public bool IsClientIdentityProfileSelectionPanelOpen { get; private set; }
    public bool IsClientIdentityProfileEditPanelOpen { get; private set; }
    public bool IsClientIdentityProfileConnectionSelectionPanelOpen { get; private set; }
    public int ClientIdentityProfileConnectionSelectionIndex { get; private set; }
    public int ClientIdentityProfileConnectionSelectionPageIndex { get; private set; }
    public bool IsQuickClientIdentitySelectionPanelOpen { get; private set; }
    public GoStone QuickClientIdentitySelectionStone { get; private set; }
    public bool QuickClientIdentitySelectionIsCgos { get; private set; }
    public int QuickClientIdentitySelectionIndex { get; private set; }
    public int ClientIdentityProfileEditIndex { get; private set; }
    public int ClientIdentityProfileSelectionIndex { get; private set; }
    public ClientIdentityProfile ClientIdentityProfileEditDraft { get; private set; } = new();
    public ClientIdentityProfileEditField? ActiveClientIdentityProfileEditField { get; private set; }
    public int ClientIdentityProfileEditCaretIndex { get; private set; }
    public int ClientIdentityProfileEditSelectionStart { get; private set; }
    public int ClientIdentityProfileEditSelectionLength { get; private set; }
    private string ClientIdentityProfileEditOriginalFieldText { get; set; } = "";
    private ClientIdentityProfile ClientIdentityProfileEditOriginalProfile { get; set; } = new();

    public bool OpenClientIdentityProfileSelectionPanel()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count) return false;
        var ids = GetClientIdentityEditOwner().ClientIdentityProfileIds;
        if (ids.Count == 0) return false;
        ClientIdentityProfileSelectionIndex = Math.Clamp(ClientIdentityProfileEditIndex, 0, ids.Count - 1);
        IsClientIdentityProfileSelectionPanelOpen = true;
        ActivateWindow(ActiveWindowId.ClientIdentitySelection);
        return true;
    }

    public void CloseClientIdentityProfileSelectionPanel()
    {
        IsClientIdentityProfileSelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentitySelection);
    }

    public void SelectClientIdentityProfile(int index)
    {
        var count = GetClientIdentityEditOwner().ClientIdentityProfileIds.Count;
        if (index < 0 || index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        ClientIdentityProfileSelectionIndex = index;
    }

    public bool CommitClientIdentityProfileSelection()
    {
        ClientIdentityProfileEditIndex = ClientIdentityProfileSelectionIndex;
        if (!UseClientIdentityProfile()) return false;
        IsClientIdentityProfileSelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentitySelection);
        return true;
    }

    public bool OpenClientIdentityProfileEditPanel()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count) return false;
        ClientIdentityProfileEditIndex = ClientIdentityProfileSelectionIndex;
        IsClientIdentityProfileSelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentitySelection);
        IsClientIdentityProfileEditPanelOpen = true;
        ActivateWindow(ActiveWindowId.ClientIdentityEdit);
        var opened = LoadClientIdentityProfileEditDraft();
        if (opened) ClientIdentityProfileEditOriginalProfile = ClientIdentityProfileEditDraft.Clone();
        return opened;
    }

    public void CloseClientIdentityProfileEditPanel()
    {
        (IsClientIdentityProfileEditPanelOpen, IsClientIdentityProfileConnectionSelectionPanelOpen, ActiveClientIdentityProfileEditField) = (false, false, null);
        DeactivateWindow(ActiveWindowId.ClientIdentityEdit);
        DeactivateWindow(ActiveWindowId.ClientIdentityConnectionSelection);
    }

    public bool ReturnToClientIdentityProfileSelectionPanel()
    {
        SaveClientIdentityProfileEditDraft();
        ClientIdentityProfileSelectionIndex = ClientIdentityProfileEditIndex;
        IsClientIdentityProfileEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentityEdit);
        IsClientIdentityProfileSelectionPanelOpen = true;
        ActivateWindow(ActiveWindowId.ClientIdentitySelection);
        return true;
    }

    public void CancelClientIdentityProfileEdit()
    {
        var index = _clientIdentityProfiles.FindIndex(profile => string.Equals(profile.Id, ClientIdentityProfileEditOriginalProfile.Id, StringComparison.Ordinal));
        if (index >= 0) _clientIdentityProfiles[index] = ClientIdentityProfileEditOriginalProfile.Clone();
        ClientIdentityProfileEditDraft = ClientIdentityProfileEditOriginalProfile.Clone();
        ActiveClientIdentityProfileEditField = null;
        ClientIdentityProfileSelectionIndex = ClientIdentityProfileEditIndex;
        IsClientIdentityProfileEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentityEdit);
        IsClientIdentityProfileSelectionPanelOpen = true;
        ActivateWindow(ActiveWindowId.ClientIdentitySelection);
    }

    public bool OpenQuickClientIdentitySelectionPanel(GoStone stone, bool cgos)
    {
        var targets = GetQuickClientIdentitySelectionTargets(stone, cgos);
        if (targets.Count == 0) return false;
        QuickClientIdentitySelectionStone = stone;
        QuickClientIdentitySelectionIsCgos = cgos;
        var current = cgos ? GetSelectedCgosClientIdentityProfile(stone)?.Id : GetSelectedLocalMatchClientIdentityProfile(stone)?.Id;
        QuickClientIdentitySelectionIndex = Math.Max(0, targets.FindIndex(target => string.Equals(target.Id, current, StringComparison.Ordinal)));
        IsQuickClientIdentitySelectionPanelOpen = true;
        ActivateWindow(ActiveWindowId.QuickClientIdentitySelection);
        return true;
    }

    public void CancelQuickClientIdentitySelectionPanel()
    {
        IsQuickClientIdentitySelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.QuickClientIdentitySelection);
    }

    public void SelectQuickClientIdentity(int index)
    {
        if (index < 0 || index >= GetQuickClientIdentitySelectionTargets(QuickClientIdentitySelectionStone, QuickClientIdentitySelectionIsCgos).Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        QuickClientIdentitySelectionIndex = index;
    }

    public bool CommitQuickClientIdentitySelection()
    {
        var targets = GetQuickClientIdentitySelectionTargets(QuickClientIdentitySelectionStone, QuickClientIdentitySelectionIsCgos);
        if (QuickClientIdentitySelectionIndex < 0 || QuickClientIdentitySelectionIndex >= targets.Count) return false;
        var selected = QuickClientIdentitySelectionIsCgos
            ? TrySelectCgosClientIdentityProfile(QuickClientIdentitySelectionStone, targets[QuickClientIdentitySelectionIndex].Id)
            : TrySelectLocalMatchClientIdentityProfile(QuickClientIdentitySelectionStone, targets[QuickClientIdentitySelectionIndex].Id);
        if (selected)
        {
            IsQuickClientIdentitySelectionPanelOpen = false;
            DeactivateWindow(ActiveWindowId.QuickClientIdentitySelection);
        }
        return selected;
    }

    public List<ClientIdentityProfile> GetQuickClientIdentitySelectionTargets(GoStone stone, bool cgos)
    {
        var player = cgos
            ? stone == GoStone.Black ? SelectedCgosBlackEntryProfile : SelectedCgosWhiteEntryProfile
            : GetSelectedEntryProfile(stone);
        if (player is null) return [];
        var connectionId = cgos ? SelectedCgosConnectionProfile.Id : "";
        return GetPlayerClientIdentityProfiles(player.Id)
            .Where(target => cgos
                ? string.Equals(target.ConnectionProfileId, connectionId, StringComparison.Ordinal)
                : string.IsNullOrEmpty(target.ConnectionProfileId))
            .ToList();
    }

    public bool MoveClientIdentityProfileEditSelection(int step)
    {
        var ids = GetClientIdentityEditOwner().ClientIdentityProfileIds;
        if (ids.Count == 0) return false;
        ClientIdentityProfileEditIndex = (ClientIdentityProfileEditIndex + step + ids.Count) % ids.Count;
        return LoadClientIdentityProfileEditDraft();
    }

    public bool AddClientIdentityProfile(bool cgos)
    {
        var owner = GetClientIdentityEditOwner();
        if (owner.ClientIdentityProfileIds.Count >= 5) return false;
        var target = new ClientIdentityProfile
        {
            DisplayName = cgos ? "OnlineMatch (CGOS)" : "LocalMatch",
            ConnectionProfileId = cgos ? _cgosConnectionProfiles.ElementAtOrDefault(SelectedCgosConnectionProfileIndex)?.Id ?? "" : "",
            LoginName = new string(owner.Identifier.Where(character => !char.IsWhiteSpace(character)).ToArray()),
        };
        _clientIdentityProfiles.Add(target);
        owner.ClientIdentityProfileIds.Add(target.Id);
        PlayerEditDraft.ClientIdentityProfileIds = owner.ClientIdentityProfileIds.ToList();
        ClientIdentityProfileEditIndex = owner.ClientIdentityProfileIds.Count - 1;
        ClientIdentityProfileSelectionIndex = ClientIdentityProfileEditIndex;
        LoadClientIdentityProfileEditDraft();
        return true;
    }

    /// <summary>選択中の Client Identity を独立した設定として複製します。</summary>
    public bool DuplicateSelectedClientIdentityProfile()
    {
        var owner = GetClientIdentityEditOwner();
        if (ClientIdentityProfileSelectionIndex < 0 ||
            ClientIdentityProfileSelectionIndex >= owner.ClientIdentityProfileIds.Count ||
            owner.ClientIdentityProfileIds.Count >= 5)
        {
            return false;
        }

        var sourceId = owner.ClientIdentityProfileIds[ClientIdentityProfileSelectionIndex];
        var source = _clientIdentityProfiles.FirstOrDefault(profile =>
            string.Equals(profile.Id, sourceId, StringComparison.Ordinal));
        if (source is null)
            return false;

        var duplicate = source.Clone();
        duplicate.Id = Guid.NewGuid().ToString("N");
        duplicate.DisplayName = string.IsNullOrWhiteSpace(source.DisplayName)
            ? "Client Identity Copy"
            : $"{source.DisplayName.Trim()} Copy";
        _clientIdentityProfiles.Add(duplicate);
        var duplicateIndex = ClientIdentityProfileSelectionIndex + 1;
        owner.ClientIdentityProfileIds.Insert(duplicateIndex, duplicate.Id);
        PlayerEditDraft.ClientIdentityProfileIds = owner.ClientIdentityProfileIds.ToList();
        ClientIdentityProfileEditIndex = duplicateIndex;
        ClientIdentityProfileSelectionIndex = duplicateIndex;
        return LoadClientIdentityProfileEditDraft();
    }

    public bool RemoveClientIdentityProfile()
    {
        var owner = GetClientIdentityEditOwner();
        if (owner.ClientIdentityProfileIds.Count <= 1) return false;
        var id = owner.ClientIdentityProfileIds[ClientIdentityProfileEditIndex];
        owner.ClientIdentityProfileIds.RemoveAt(ClientIdentityProfileEditIndex);
        if (!_playerProfiles.Any(player => player.ClientIdentityProfileIds.Contains(id, StringComparer.Ordinal)))
            _clientIdentityProfiles.RemoveAll(target => string.Equals(target.Id, id, StringComparison.Ordinal));
        PlayerEditDraft.ClientIdentityProfileIds = owner.ClientIdentityProfileIds.ToList();
        ClientIdentityProfileEditIndex = Math.Clamp(ClientIdentityProfileEditIndex, 0, owner.ClientIdentityProfileIds.Count - 1);
        ClientIdentityProfileSelectionIndex = ClientIdentityProfileEditIndex;
        return LoadClientIdentityProfileEditDraft();
    }

    public bool RemoveSelectedClientIdentityProfile()
    {
        ClientIdentityProfileEditIndex = ClientIdentityProfileSelectionIndex;
        return RemoveClientIdentityProfile();
    }

    /// <summary>選択 Target をこの Player の既定の使用先として先頭へ移動する。</summary>
    public bool UseClientIdentityProfile()
    {
        var owner = GetClientIdentityEditOwner();
        if (ClientIdentityProfileEditIndex < 0 || ClientIdentityProfileEditIndex >= owner.ClientIdentityProfileIds.Count)
            return false;

        var id = owner.ClientIdentityProfileIds[ClientIdentityProfileEditIndex];
        owner.ClientIdentityProfileIds.RemoveAt(ClientIdentityProfileEditIndex);
        owner.ClientIdentityProfileIds.Insert(0, id);
        PlayerEditDraft.ClientIdentityProfileIds = owner.ClientIdentityProfileIds.ToList();
        ClientIdentityProfileEditIndex = 0;
        ClientIdentityProfileSelectionIndex = 0;
        return LoadClientIdentityProfileEditDraft();
    }

    public bool IsClientIdentityProfileInUse(int index) =>
        index == 0 && GetClientIdentityEditOwner().ClientIdentityProfileIds.Count > 0;

    public void SetClientIdentityProfileEditField(ClientIdentityProfileEditField field, string value)
    {
        if (field == ClientIdentityProfileEditField.DisplayName) ClientIdentityProfileEditDraft.DisplayName = value;
        else if (field == ClientIdentityProfileEditField.LoginName) ClientIdentityProfileEditDraft.LoginName = value;
        else ClientIdentityProfileEditDraft.LoginPass = value;
    }

    public string GetClientIdentityProfileEditField(ClientIdentityProfileEditField field) => field switch
    {
        ClientIdentityProfileEditField.DisplayName => ClientIdentityProfileEditDraft.DisplayName,
        ClientIdentityProfileEditField.LoginName => ClientIdentityProfileEditDraft.LoginName,
        ClientIdentityProfileEditField.LoginPass => ClientIdentityProfileEditDraft.LoginPass,
        _ => "",
    };

    public void BeginClientIdentityProfileEditField(ClientIdentityProfileEditField field, int caretIndex)
    {
        ActiveClientIdentityProfileEditField = field;
        ClientIdentityProfileEditOriginalFieldText = GetClientIdentityProfileEditField(field);
        ClientIdentityProfileEditCaretIndex = Math.Clamp(caretIndex, 0, ClientIdentityProfileEditOriginalFieldText.Length);
        ClientIdentityProfileEditSelectionStart = ClientIdentityProfileEditCaretIndex;
        ClientIdentityProfileEditSelectionLength = 0;
    }

    public void SetClientIdentityProfileEditFieldText(ClientIdentityProfileEditField field, string value, int caretIndex, int selectionStart, int selectionLength)
    {
        SetClientIdentityProfileEditField(field, value);
        ClientIdentityProfileEditCaretIndex = Math.Clamp(caretIndex, 0, value.Length);
        ClientIdentityProfileEditSelectionStart = Math.Clamp(selectionStart, 0, value.Length);
        ClientIdentityProfileEditSelectionLength = Math.Clamp(selectionLength, 0, value.Length - ClientIdentityProfileEditSelectionStart);
    }

    public void EndClientIdentityProfileEditField() => ActiveClientIdentityProfileEditField = null;

    public void CancelClientIdentityProfileEditField()
    {
        if (ActiveClientIdentityProfileEditField is { } field)
            SetClientIdentityProfileEditField(field, ClientIdentityProfileEditOriginalFieldText);
        ActiveClientIdentityProfileEditField = null;
    }

    public void SaveClientIdentityProfileEditDraft()
    {
        var index = _clientIdentityProfiles.FindIndex(target => string.Equals(target.Id, ClientIdentityProfileEditDraft.Id, StringComparison.Ordinal));
        if (index < 0) return;
        ClientIdentityProfileEditDraft.DisplayName = string.IsNullOrWhiteSpace(ClientIdentityProfileEditDraft.DisplayName) ? "New Target" : ClientIdentityProfileEditDraft.DisplayName.Trim();
        _clientIdentityProfiles[index] = ClientIdentityProfileEditDraft.Clone();
    }

    public bool OpenClientIdentityProfileConnectionSelectionPanel()
    {
        if (_cgosConnectionProfiles.Count == 0 || string.IsNullOrEmpty(ClientIdentityProfileEditDraft.ConnectionProfileId))
            return false;

        ClientIdentityProfileConnectionSelectionIndex = Math.Max(0, _cgosConnectionProfiles.FindIndex(profile =>
            string.Equals(profile.Id, ClientIdentityProfileEditDraft.ConnectionProfileId, StringComparison.Ordinal)));
        ClientIdentityProfileConnectionSelectionPageIndex = ClientIdentityProfileConnectionSelectionIndex / ClientIdentityProfileConnectionSelectionPageSize;
        IsClientIdentityProfileConnectionSelectionPanelOpen = true;
        ActivateWindow(ActiveWindowId.ClientIdentityConnectionSelection);
        return true;
    }

    public void CancelClientIdentityProfileConnectionSelectionPanel()
    {
        IsClientIdentityProfileConnectionSelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentityConnectionSelection);
    }

    public void SelectClientIdentityProfileConnection(int index)
    {
        if (index < 0 || index >= _cgosConnectionProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        ClientIdentityProfileConnectionSelectionIndex = index;
    }

    public bool CommitClientIdentityProfileConnectionSelection()
    {
        if (ClientIdentityProfileConnectionSelectionIndex < 0 || ClientIdentityProfileConnectionSelectionIndex >= _cgosConnectionProfiles.Count)
            return false;
        ClientIdentityProfileEditDraft.ConnectionProfileId = _cgosConnectionProfiles[ClientIdentityProfileConnectionSelectionIndex].Id;
        IsClientIdentityProfileConnectionSelectionPanelOpen = false;
        DeactivateWindow(ActiveWindowId.ClientIdentityConnectionSelection);
        return true;
    }

    public void MoveClientIdentityProfileConnectionSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)ClientIdentityProfileConnectionSelectionPageSize));
        ClientIdentityProfileConnectionSelectionPageIndex = Math.Clamp(ClientIdentityProfileConnectionSelectionPageIndex + step, 0, pageCount - 1);
    }

    public int ClientIdentityProfileConnectionSelectionPageCount =>
        Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)ClientIdentityProfileConnectionSelectionPageSize));

    public string ClientIdentityProfileEditConnectionDisplayName =>
        GetClientIdentityProfileConnectionDisplayName(ClientIdentityProfileEditDraft);

    public string GetClientIdentityProfileConnectionDisplayName(ClientIdentityProfile target) =>
        _cgosConnectionProfiles.FirstOrDefault(profile => string.Equals(profile.Id, target.ConnectionProfileId, StringComparison.Ordinal))?.DisplayName ?? "LOCAL MATCH";

    private EntryProfile GetClientIdentityEditOwner() => _playerProfiles[PlayerEditProfileIndex];

    private bool LoadClientIdentityProfileEditDraft()
    {
        var ids = GetClientIdentityEditOwner().ClientIdentityProfileIds;
        if (ids.Count == 0) return false;
        var target = _clientIdentityProfiles.FirstOrDefault(item => string.Equals(item.Id, ids[ClientIdentityProfileEditIndex], StringComparison.Ordinal));
        if (target is null) return false;
        ClientIdentityProfileEditDraft = target.Clone();
        ActiveClientIdentityProfileEditField = null;
        return true;
    }
}

public enum ClientIdentityProfileEditField { DisplayName, LoginName, LoginPass }
