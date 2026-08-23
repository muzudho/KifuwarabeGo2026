namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Player が参照する ClientIdentityProfile の編集状態。</summary>
public sealed partial class GoAppSession
{
    public bool IsClientIdentityProfileSelectionPanelOpen { get; private set; }
    public bool IsClientIdentityProfileEditPanelOpen { get; private set; }
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

    /// <summary>編集開始時から未保存の変更があるかを返します。</summary>
    public bool IsClientIdentityProfileEditDirty =>
        ClientIdentityProfileEditDraft.DisplayName != ClientIdentityProfileEditOriginalProfile.DisplayName ||
        ClientIdentityProfileEditDraft.LoginName != ClientIdentityProfileEditOriginalProfile.LoginName ||
        ClientIdentityProfileEditDraft.LoginPass != ClientIdentityProfileEditOriginalProfile.LoginPass ||
        ClientIdentityProfileEditDraft.Comment != ClientIdentityProfileEditOriginalProfile.Comment;

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

    /// <summary>選択したプロフィールの認証情報を、Entry Profile の入力欄へコピーします。</summary>
    public bool InputSelectedClientIdentityProfileToPlayerEditDraft()
    {
        var owner = GetClientIdentityEditOwner();
        if (ClientIdentityProfileSelectionIndex < 0 || ClientIdentityProfileSelectionIndex >= owner.ClientIdentityProfileIds.Count)
            return false;

        var selectedId = owner.ClientIdentityProfileIds[ClientIdentityProfileSelectionIndex];
        var selected = _clientIdentityProfiles.FirstOrDefault(profile => string.Equals(profile.Id, selectedId, StringComparison.Ordinal));
        if (selected is null) return false;

        PlayerEditClientIdentityDraft.LoginName = selected.LoginName;
        PlayerEditClientIdentityDraft.LoginPass = selected.LoginPass;
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
        (IsClientIdentityProfileEditPanelOpen, ActiveClientIdentityProfileEditField) = (false, null);
        DeactivateWindow(ActiveWindowId.ClientIdentityEdit);
    }

    public bool ReturnToClientIdentityProfileSelectionPanel()
    {
        SaveClientIdentityProfileEditDraft();
        return ReturnToClientIdentityProfileSelectionPanelWithoutSaving();
    }

    public bool ReturnToClientIdentityProfileSelectionPanelWithoutSaving()
    {
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
        return GetPlayerClientIdentityProfiles(player.Id).ToList();
    }

    public bool MoveClientIdentityProfileEditSelection(int step)
    {
        var ids = GetClientIdentityEditOwner().ClientIdentityProfileIds;
        if (ids.Count == 0) return false;
        ClientIdentityProfileEditIndex = (ClientIdentityProfileEditIndex + step + ids.Count) % ids.Count;
        return LoadClientIdentityProfileEditDraft();
    }

    public bool AddClientIdentityProfile()
    {
        var owner = GetClientIdentityEditOwner();
        if (owner.ClientIdentityProfileIds.Count >= 5) return false;
        var target = new ClientIdentityProfile
        {
            DisplayName = "Client Identity",
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

    /// <summary>入力元一覧から、HANDLE と PASSWORD が空の Client Identity を追加します。</summary>
    public bool AddClientIdentityProfileForInput()
    {
        var owner = GetClientIdentityEditOwner();
        if (owner.ClientIdentityProfileIds.Count >= 5) return false;

        var target = new ClientIdentityProfile
        {
            DisplayName = "Client Identity",
            LoginName = "",
            LoginPass = "",
        };
        _clientIdentityProfiles.Add(target);
        owner.ClientIdentityProfileIds.Add(target.Id);
        PlayerEditDraft.ClientIdentityProfileIds = owner.ClientIdentityProfileIds.ToList();
        ClientIdentityProfileEditIndex = owner.ClientIdentityProfileIds.Count - 1;
        ClientIdentityProfileSelectionIndex = ClientIdentityProfileEditIndex;
        return LoadClientIdentityProfileEditDraft();
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

    /// <summary>選択した Client Identity を、この Player の既定入力値として先頭へ移動します。</summary>
    public bool SetSelectedClientIdentityProfileAsDefault()
    {
        var owner = GetClientIdentityEditOwner();
        ClientIdentityProfileEditIndex = ClientIdentityProfileSelectionIndex;
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

    /// <summary>編集画面を開いたときに最初に入力する Client Identity かを判定します。</summary>
    public bool IsClientIdentityProfileDefault(int index) =>
        index == 0 && GetClientIdentityEditOwner().ClientIdentityProfileIds.Count > 0;

    public void SetClientIdentityProfileEditField(ClientIdentityProfileEditField field, string value)
    {
        if (field == ClientIdentityProfileEditField.DisplayName) ClientIdentityProfileEditDraft.DisplayName = value;
        else if (field == ClientIdentityProfileEditField.LoginName) ClientIdentityProfileEditDraft.LoginName = value;
        else if (field == ClientIdentityProfileEditField.LoginPass) ClientIdentityProfileEditDraft.LoginPass = value;
        else if (field == ClientIdentityProfileEditField.Comment) ClientIdentityProfileEditDraft.Comment = value;
    }

    public string GetClientIdentityProfileEditField(ClientIdentityProfileEditField field) => field switch
    {
        ClientIdentityProfileEditField.DisplayName => ClientIdentityProfileEditDraft.DisplayName,
        ClientIdentityProfileEditField.LoginName => ClientIdentityProfileEditDraft.LoginName,
        ClientIdentityProfileEditField.LoginPass => ClientIdentityProfileEditDraft.LoginPass,
        ClientIdentityProfileEditField.Comment => ClientIdentityProfileEditDraft.Comment,
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

    private EntryProfile GetClientIdentityEditOwner() => _playerProfiles[PlayerEditProfileIndex];

    private bool LoadClientIdentityProfileEditDraft()
    {
        var ids = GetClientIdentityEditOwner().ClientIdentityProfileIds;
        if (ids.Count == 0) return false;
        var target = _clientIdentityProfiles.FirstOrDefault(item => string.Equals(item.Id, ids[ClientIdentityProfileEditIndex], StringComparison.Ordinal));
        if (target is null) return false;
        ClientIdentityProfileEditDraft = target.Clone();
        ClientIdentityProfileEditOriginalProfile = ClientIdentityProfileEditDraft.Clone();
        ActiveClientIdentityProfileEditField = null;
        return true;
    }
}

public enum ClientIdentityProfileEditField { DisplayName, LoginName, LoginPass, Comment }
