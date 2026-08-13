namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;
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
    private string PlayerEditOriginalFieldText { get; set; } = "";
    public ClientIdentityProfile PlayerEditClientIdentityDraft { get; private set; } = new();

    public bool OpenSelectedPlayerEditPanel()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        PlayerEditProfileIndex = PlayerDialogSelectionIndex;
        PlayerEditDraft = _playerProfiles[PlayerEditProfileIndex].Clone();
        IsPlayerEditDirty = false;
        ActivePlayerEditField = null;
        ClientIdentityProfileEditIndex = 0;
        LoadPlayerEditClientIdentityDraft();
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
        ActivePlayerEditField = field;
        PlayerEditOriginalFieldText = GetPlayerEditFieldText(field);
        PlayerEditCaretIndex = Math.Clamp(caretIndex, 0, GetPlayerEditFieldText(field).Length);
        PlayerEditSelectionStart = PlayerEditCaretIndex;
        PlayerEditSelectionLength = 0;
    }

    public void SetPlayerEditFieldText(EntryProfileEditField field, string value, int caretIndex, int selectionStart, int selectionLength)
    {
        if (field == EntryProfileEditField.DisplayName) SetPlayerEditDisplayName(value);
        else if (field == EntryProfileEditField.Identifier) SetPlayerEditIdentifier(value);
        else if (field == EntryProfileEditField.ClientIdentityHandle) PlayerEditClientIdentityDraft.LoginName = value;
        else PlayerEditClientIdentityDraft.LoginPass = value;

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
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    public void EndPlayerEditField() => ActivePlayerEditField = null;

    public void CancelPlayerEditField()
    {
        if (ActivePlayerEditField is { } field)
        {
            if (field == EntryProfileEditField.DisplayName) PlayerEditDraft.DisplayName = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.Identifier) PlayerEditDraft.Identifier = PlayerEditOriginalFieldText;
            else if (field == EntryProfileEditField.ClientIdentityHandle) PlayerEditClientIdentityDraft.LoginName = PlayerEditOriginalFieldText;
            else PlayerEditClientIdentityDraft.LoginPass = PlayerEditOriginalFieldText;
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

    /// <summary>Local Match はパスワードを送らないため、入力欄では利用しません。</summary>
    public bool IsPlayerEditClientIdentityLocalMatch => string.IsNullOrEmpty(PlayerEditClientIdentityDraft.ConnectionProfileId);

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

        var identityIndex = _clientIdentityProfiles.FindIndex(profile => string.Equals(profile.Id, PlayerEditClientIdentityDraft.Id, StringComparison.Ordinal));
        if (identityIndex >= 0) _clientIdentityProfiles[identityIndex] = PlayerEditClientIdentityDraft.Clone();
        _playerProfiles[PlayerEditProfileIndex] = draft;
        IsPlayerEditDirty = false;
        IsPlayerEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.PlayerEdit);
        IsCreatingEngineProfileForPlayerEdit = false;
        ActivePlayerEditField = null;
        ApplySelectedEntryProfile(GoStone.Black);
        ApplySelectedEntryProfile(GoStone.White);
        return true;
    }

    public void CancelPlayerEditPanel()
    {
        IsPlayerEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.PlayerEdit);
        IsPlayerEditDirty = false;
        IsCreatingEngineProfileForPlayerEdit = false;
        ActivePlayerEditField = null;
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
}

public enum EntryProfileEditField
{
    DisplayName,
    Identifier,
    ClientIdentityHandle,
    ClientIdentityPassword,
}
