namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>
/// PlayerProfile 編集の下書き。UI はこの下書きだけを変更し、SAVE 時にのみ一覧へ反映する。
/// Identifier はアプリ側で文字種・文字数を検査しない。
/// </summary>
public sealed partial class GoAppSession
{
    public bool IsPlayerEditPanelOpen { get; private set; }
    public int PlayerEditProfileIndex { get; private set; } = -1;
    public PlayerProfile PlayerEditDraft { get; private set; } = new();
    public bool IsPlayerEditDirty { get; private set; }
    public PlayerProfileEditField? ActivePlayerEditField { get; private set; }
    public int PlayerEditCaretIndex { get; private set; }
    public int PlayerEditSelectionStart { get; private set; }
    public int PlayerEditSelectionLength { get; private set; }
    private string PlayerEditOriginalFieldText { get; set; } = "";

    public bool OpenSelectedPlayerEditPanel()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        PlayerEditProfileIndex = PlayerDialogSelectionIndex;
        PlayerEditDraft = _playerProfiles[PlayerEditProfileIndex].Clone();
        IsPlayerEditDirty = false;
        ActivePlayerEditField = null;
        IsPlayerEditPanelOpen = true;
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

    public void BeginPlayerEditField(PlayerProfileEditField field, int caretIndex)
    {
        ActivePlayerEditField = field;
        PlayerEditOriginalFieldText = GetPlayerEditFieldText(field);
        PlayerEditCaretIndex = Math.Clamp(caretIndex, 0, GetPlayerEditFieldText(field).Length);
        PlayerEditSelectionStart = PlayerEditCaretIndex;
        PlayerEditSelectionLength = 0;
    }

    public void SetPlayerEditFieldText(PlayerProfileEditField field, string value, int caretIndex, int selectionStart, int selectionLength)
    {
        if (field == PlayerProfileEditField.DisplayName)
            SetPlayerEditDisplayName(value);
        else
            SetPlayerEditIdentifier(value);

        PlayerEditCaretIndex = Math.Clamp(caretIndex, 0, value.Length);
        PlayerEditSelectionStart = Math.Clamp(selectionStart, 0, value.Length);
        PlayerEditSelectionLength = Math.Clamp(selectionLength, 0, value.Length - PlayerEditSelectionStart);
    }

    public string GetPlayerEditFieldText(PlayerProfileEditField field) => field switch
    {
        PlayerProfileEditField.DisplayName => PlayerEditDraft.DisplayName,
        PlayerProfileEditField.Identifier => PlayerEditDraft.Identifier,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    public void EndPlayerEditField() => ActivePlayerEditField = null;

    public void CancelPlayerEditField()
    {
        if (ActivePlayerEditField is { } field)
        {
            if (field == PlayerProfileEditField.DisplayName)
                PlayerEditDraft.DisplayName = PlayerEditOriginalFieldText;
            else
                PlayerEditDraft.Identifier = PlayerEditOriginalFieldText;
        }
        ActivePlayerEditField = null;
    }

    public bool SetPlayerEditEngineProfile(string engineProfileId)
    {
        if (PlayerEditDraft.Kind != PlayerProfileKind.Computer || FindGtpEngineIndex(engineProfileId) < 0)
            return false;
        PlayerEditDraft.EngineProfileId = engineProfileId;
        IsPlayerEditDirty = true;
        return true;
    }

    public void CyclePlayerEditEngine(int step)
    {
        if (PlayerEditDraft.Kind != PlayerProfileKind.Computer || _gtpEngineProfiles.Count == 0)
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

    public bool SavePlayerEditDraft()
    {
        if (PlayerEditProfileIndex < 0 || PlayerEditProfileIndex >= _playerProfiles.Count)
            return false;

        var draft = PlayerEditDraft.Clone();
        draft.DisplayName = string.IsNullOrWhiteSpace(draft.DisplayName) ? "New Player" : draft.DisplayName.Trim();
        draft.Identifier ??= "";
        if (draft.Kind == PlayerProfileKind.Computer && FindGtpEngineIndex(draft.EngineProfileId) < 0)
            return false;

        _playerProfiles[PlayerEditProfileIndex] = draft;
        IsPlayerEditDirty = false;
        IsPlayerEditPanelOpen = false;
        ActivePlayerEditField = null;
        ApplySelectedPlayerProfile(GoStone.Black);
        ApplySelectedPlayerProfile(GoStone.White);
        return true;
    }

    public void CancelPlayerEditPanel()
    {
        IsPlayerEditPanelOpen = false;
        IsPlayerEditDirty = false;
        ActivePlayerEditField = null;
    }
}

public enum PlayerProfileEditField
{
    DisplayName,
    Identifier,
}
