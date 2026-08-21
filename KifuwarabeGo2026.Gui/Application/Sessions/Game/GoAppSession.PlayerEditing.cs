namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// EntryProfile 編集の下書き。UI はこの下書きだけを変更し、SAVE 時にのみ一覧へ反映する。
/// Identifier はアプリ側で文字種・文字数を検査しない。
/// </summary>
public sealed partial class GoAppSession
{
    public bool IsPlayerEditPanelOpen { get; private set; }
    public int PlayerEditProfileIndex { get; private set; } = -1;
    public EntryProfile PlayerEditDraft { get; private set; } = new();
    public bool IsPlayerEditDirty { get; private set; }
    public bool IsCreatingEngineProfileForPlayerEdit { get; private set; }
    public EntryProfileEditField? ActivePlayerEditField { get; private set; }
    public int PlayerEditCaretIndex { get; private set; }
    public int PlayerEditSelectionStart { get; private set; }
    public int PlayerEditSelectionLength { get; private set; }
    public TextCompositionState PlayerEditComposition { get; private set; } = TextCompositionState.Empty;
    private string PlayerEditOriginalFieldText { get; set; } = "";
    public ClientIdentityProfile PlayerEditClientIdentityDraft { get; private set; } = new();
    private EntryProfile PlayerEditOriginalProfile { get; set; } = new();
    private ClientIdentityProfile PlayerEditOriginalClientIdentityProfile { get; set; } = new();
    private List<ClientIdentityProfile> PlayerEditOriginalClientIdentities { get; set; } = [];

    /// <summary>Entry Profile または表示中の Client Identity に、開始時からの変更があるかを返します。</summary>
    public bool HasPlayerEditChanges =>
        !AreSame(PlayerEditDraft, PlayerEditOriginalProfile) ||
        !AreSame(PlayerEditClientIdentityDraft, PlayerEditOriginalClientIdentityProfile) ||
        PlayerEditOriginalClientIdentities.Any(original =>
            _clientIdentityProfiles.FirstOrDefault(item => item.Id == original.Id) is not { } current || !AreSame(current, original));

    public bool OpenSelectedPlayerEditPanel()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        PlayerEditProfileIndex = PlayerDialogSelectionIndex;
        PlayerEditDraft = _playerProfiles[PlayerEditProfileIndex].Clone();
        PlayerEditOriginalProfile = PlayerEditDraft.Clone();
        IsPlayerEditDirty = false;
        ActivePlayerEditField = null;
        ClientIdentityProfileEditIndex = 0;
        LoadPlayerEditClientIdentityDraft();
        PlayerEditOriginalClientIdentityProfile = PlayerEditClientIdentityDraft.Clone();
        PlayerEditOriginalClientIdentities = PlayerEditDraft.ClientIdentityProfileIds
            .Select(id => _clientIdentityProfiles.First(profile => profile.Id == id).Clone()).ToList();
        IsPlayerEditPanelOpen = true;
        ActivateWindow(ActiveWindowId.PlayerEdit);
        return true;
    }

    public void SetPlayerEditDisplayName(string value)
    {
        PlayerEditDraft.DisplayName = value;
        IsPlayerEditDirty = true;
    }

    public void SetPlayerEditIdentifier(string value)
    {
        PlayerEditDraft.Identifier = value;
        IsPlayerEditDirty = true;
    }

    public void BeginPlayerEditField(EntryProfileEditField field, int caretIndex)
    {
        if (ActivePlayerEditField != field) PlayerEditComposition = TextCompositionState.Empty;
        ActivePlayerEditField = field;
        PlayerEditOriginalFieldText = GetPlayerEditFieldText(field);
        PlayerEditCaretIndex = Math.Clamp(caretIndex, 0, GetPlayerEditFieldText(field).Length);
        PlayerEditSelectionStart = PlayerEditCaretIndex;
        PlayerEditSelectionLength = 0;
    }

    public void SetPlayerEditComposition(TextCompositionState composition) =>
        PlayerEditComposition = ActivePlayerEditField is null ? TextCompositionState.Empty : composition;

    public void SetPlayerEditFieldText(EntryProfileEditField field, string value, int caretIndex, int selectionStart, int selectionLength)
    {
        if (field == EntryProfileEditField.DisplayName) SetPlayerEditDisplayName(value);
        else if (field == EntryProfileEditField.Identifier) SetPlayerEditIdentifier(value);
        else if (field == EntryProfileEditField.ClientIdentityHandle) PlayerEditClientIdentityDraft.LoginName = value;
        else if (field == EntryProfileEditField.ClientIdentityPassword) PlayerEditClientIdentityDraft.LoginPass = value;
        else if (field == EntryProfileEditField.ClientIdentityComment) PlayerEditClientIdentityDraft.Comment = value;

        PlayerEditCaretIndex = Math.Clamp(caretIndex, 0, value.Length);
        PlayerEditSelectionStart = Math.Clamp(selectionStart, 0, value.Length);
        PlayerEditSelectionLength = Math.Clamp(selectionLength, 0, value.Length - PlayerEditSelectionStart);
    }

    public string GetPlayerEditFieldText(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => PlayerEditDraft.DisplayName,
        EntryProfileEditField.Identifier => PlayerEditDraft.Identifier,
        EntryProfileEditField.ClientIdentityHandle => PlayerEditClientIdentityDraft.LoginName,
        EntryProfileEditField.ClientIdentityPassword => PlayerEditClientIdentityDraft.LoginPass,
        EntryProfileEditField.ClientIdentityComment => PlayerEditClientIdentityDraft.Comment,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    public void EndPlayerEditField()
    {
        ActivePlayerEditField = null;
        PlayerEditComposition = TextCompositionState.Empty;
        PlayerEditComposition = TextCompositionState.Empty;
    }

    public void CancelPlayerEditField()
    {
        if (ActivePlayerEditField is { } field)
        {
            if (field == EntryProfileEditField.DisplayName) PlayerEditDraft.DisplayName = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.Identifier) PlayerEditDraft.Identifier = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.ClientIdentityHandle) PlayerEditClientIdentityDraft.LoginName = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.ClientIdentityPassword) PlayerEditClientIdentityDraft.LoginPass = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.ClientIdentityComment) PlayerEditClientIdentityDraft.Comment = PlayerEditOriginalFieldText;
        }
        ActivePlayerEditField = null;
    }

    public bool SetPlayerEditEngineProfile(string engineProfileId)
    {
        if (PlayerEditDraft.Kind != EntryProfileKind.Computer || FindGtpEngineIndex(engineProfileId) < 0)
            return false;
        PlayerEditDraft.EngineProfileId = engineProfileId;
        IsPlayerEditDirty = true;
        return true;
    }

    public void OpenNewEngineProfileForPlayerEdit()
    {
        if (PlayerEditDraft.Kind != EntryProfileKind.Computer) return;
        IsCreatingEngineProfileForPlayerEdit = true;
        OpenGtpEngineAddPanel();
    }

    public void CompleteNewEngineProfileForPlayerEdit(string engineProfileId)
    {
        if (!IsCreatingEngineProfileForPlayerEdit) return;
        IsCreatingEngineProfileForPlayerEdit = false;
        SetPlayerEditEngineProfile(engineProfileId);
    }

    public void CancelNewEngineProfileForPlayerEdit() => IsCreatingEngineProfileForPlayerEdit = false;

    public void CyclePlayerEditEngine(int step)
    {
        if (PlayerEditDraft.Kind != EntryProfileKind.Computer || _gtpEngineProfiles.Count == 0)
            return;

        var current = FindGtpEngineIndex(PlayerEditDraft.EngineProfileId);
        var next = (Math.Max(0, current) + step + _gtpEngineProfiles.Count) % _gtpEngineProfiles.Count;
        SetPlayerEditEngineProfile(_gtpEngineProfiles[next].Id);
    }

    public string PlayerEditEngineDisplayName
    {
        get
        {
            var index = FindGtpEngineIndex(PlayerEditDraft.EngineProfileId);
            return index >= 0 ? _gtpEngineProfiles[index].DisplayName : "ENGINE NOT FOUND";
        }
    }

    public string PlayerEditClientIdentityHandle
    {
        get
        {
            var targetId = PlayerEditDraft.ClientIdentityProfileIds.FirstOrDefault();
            var target = _clientIdentityProfiles.FirstOrDefault(item => string.Equals(item.Id, targetId, StringComparison.Ordinal));
            return target?.LoginName ?? "NO HANDLE";
        }
    }

    public string PlayerEditClientIdentityPassword => PlayerEditClientIdentityDraft.LoginPass;

    public IReadOnlyList<ClientIdentityProfile> PlayerEditClientIdentities => PlayerEditDraft.ClientIdentityProfileIds
        .Select(id => id == PlayerEditClientIdentityDraft.Id
            ? PlayerEditClientIdentityDraft
            : _clientIdentityProfiles.First(profile => profile.Id == id)).ToArray();

    public bool SelectPlayerEditClientIdentity(int index)
    {
        if (index < 0 || index >= PlayerEditDraft.ClientIdentityProfileIds.Count) return false;
        CommitPlayerEditClientIdentityDraft();
        ClientIdentityProfileEditIndex = index;
        var loaded = LoadPlayerEditClientIdentityDraft();
        if (loaded) PlayerEditOriginalClientIdentityProfile = PlayerEditClientIdentityDraft.Clone();
        return loaded;
    }

    public bool SetPlayerEditKind(EntryProfileKind kind)
    {
        if (PlayerEditDraft.Kind == kind) return true;

        if (kind == EntryProfileKind.Computer)
        {
            if (_gtpEngineProfiles.Count == 0) return false;
            if (FindGtpEngineIndex(PlayerEditDraft.EngineProfileId) < 0)
                PlayerEditDraft.EngineProfileId = _gtpEngineProfiles[0].Id;
        }

        PlayerEditDraft.Kind = kind;
        ActivePlayerEditField = null;
        PlayerEditComposition = TextCompositionState.Empty;
        IsPlayerEditDirty = true;
        return true;
    }

    public bool AddPlayerEditClientIdentity()
    {
        if (PlayerEditDraft.ClientIdentityProfileIds.Count >= 5) return false;
        CommitPlayerEditClientIdentityDraft();
        var identity = new ClientIdentityProfile { DisplayName = "Client Identity" };
        _clientIdentityProfiles.Add(identity);
        PlayerEditDraft.ClientIdentityProfileIds.Add(identity.Id);
        ClientIdentityProfileEditIndex = PlayerEditDraft.ClientIdentityProfileIds.Count - 1;
        LoadPlayerEditClientIdentityDraft();
        PlayerEditOriginalClientIdentityProfile = new ClientIdentityProfile { Id = identity.Id };
        IsPlayerEditDirty = true;
        return true;
    }

    public bool RemovePlayerEditClientIdentity(int index)
    {
        if (PlayerEditDraft.ClientIdentityProfileIds.Count <= 1 || index < 0 || index >= PlayerEditDraft.ClientIdentityProfileIds.Count) return false;
        CommitPlayerEditClientIdentityDraft();
        var id = PlayerEditDraft.ClientIdentityProfileIds[index];
        PlayerEditDraft.ClientIdentityProfileIds.RemoveAt(index);
        if (!PlayerEditOriginalClientIdentities.Any(item => item.Id == id)) _clientIdentityProfiles.RemoveAll(item => item.Id == id);
        ClientIdentityProfileEditIndex = Math.Clamp(index, 0, PlayerEditDraft.ClientIdentityProfileIds.Count - 1);
        LoadPlayerEditClientIdentityDraft();
        PlayerEditOriginalClientIdentityProfile = PlayerEditClientIdentityDraft.Clone();
        IsPlayerEditDirty = true;
        return true;
    }

    public void ReloadPlayerEditClientIdentityDraft() => LoadPlayerEditClientIdentityDraft();

    public bool SavePlayerEditDraft()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count)
            return false;

        var draft = PlayerEditDraft.Clone();
        draft.DisplayName = string.IsNullOrWhiteSpace(draft.DisplayName) ? "New Player" : draft.DisplayName.Trim();
        draft.Identifier ??= "";
        if (draft.Kind == EntryProfileKind.Computer && FindGtpEngineIndex(draft.EngineProfileId) < 0)
            return false;

        CommitPlayerEditClientIdentityDraft();
        _playerProfiles[PlayerEditProfileIndex] = draft;
        var referencedIds = _playerProfiles.SelectMany(profile => profile.ClientIdentityProfileIds).ToHashSet(StringComparer.Ordinal);
        _clientIdentityProfiles.RemoveAll(profile => !referencedIds.Contains(profile.Id));
        IsPlayerEditDirty = false;
        IsPlayerEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.PlayerEdit);
        IsCreatingEngineProfileForPlayerEdit = false;
        ActivePlayerEditField = null;
        PlayerEditComposition = TextCompositionState.Empty;
        ApplySelectedEntryProfile(GoStone.Black);
        ApplySelectedEntryProfile(GoStone.White);
        return true;
    }

    public void CancelPlayerEditPanel()
    {
        foreach (var original in PlayerEditOriginalClientIdentities)
        {
            var index = _clientIdentityProfiles.FindIndex(item => item.Id == original.Id);
            if (index >= 0) _clientIdentityProfiles[index] = original.Clone();
        }
        var originalIds = PlayerEditOriginalClientIdentities.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        _clientIdentityProfiles.RemoveAll(item => PlayerEditDraft.ClientIdentityProfileIds.Contains(item.Id) && !originalIds.Contains(item.Id));
        IsPlayerEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.PlayerEdit);
        IsPlayerEditDirty = false;
        IsCreatingEngineProfileForPlayerEdit = false;
        ActivePlayerEditField = null;
        PlayerEditComposition = TextCompositionState.Empty;
    }

    private void CommitPlayerEditClientIdentityDraft()
    {
        var index = _clientIdentityProfiles.FindIndex(profile => profile.Id == PlayerEditClientIdentityDraft.Id);
        if (index >= 0) _clientIdentityProfiles[index] = PlayerEditClientIdentityDraft.Clone();
    }

    private bool LoadPlayerEditClientIdentityDraft()
    {
        if (ClientIdentityProfileEditIndex < 0 || ClientIdentityProfileEditIndex >= PlayerEditDraft.ClientIdentityProfileIds.Count) return false;
        var id = PlayerEditDraft.ClientIdentityProfileIds[ClientIdentityProfileEditIndex];
        var profile = _clientIdentityProfiles.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.Ordinal));
        if (profile is null) return false;
        PlayerEditClientIdentityDraft = profile.Clone();
        return true;
    }

    private static bool AreSame(EntryProfile left, EntryProfile right) =>
        left.Id == right.Id &&
        left.DisplayName == right.DisplayName &&
        left.Identifier == right.Identifier &&
        left.Kind == right.Kind &&
        left.EngineProfileId == right.EngineProfileId &&
        left.ClientIdentityProfileIds.SequenceEqual(right.ClientIdentityProfileIds, StringComparer.Ordinal);

    private static bool AreSame(ClientIdentityProfile left, ClientIdentityProfile right) =>
        left.Id == right.Id &&
        left.DisplayName == right.DisplayName &&
        left.ConnectionProfileId == right.ConnectionProfileId &&
        left.LoginName == right.LoginName &&
        left.LoginPass == right.LoginPass &&
        left.Comment == right.Comment;
}

public enum EntryProfileEditField
{
    DisplayName,
    Identifier,
    ClientIdentityHandle,
    ClientIdentityPassword,
    ClientIdentityComment,
}
