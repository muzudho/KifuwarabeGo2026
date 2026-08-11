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

    public bool OpenSelectedPlayerEditPanel()
    {
        if (PlayerDialogSelectionIndex < 0 || PlayerDialogSelectionIndex >= _playerProfiles.Count)
            return false;

        PlayerEditProfileIndex = PlayerDialogSelectionIndex;
        PlayerEditDraft = _playerProfiles[PlayerEditProfileIndex].Clone();
        IsPlayerEditDirty = false;
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
        ApplySelectedPlayerProfile(GoStone.Black);
        ApplySelectedPlayerProfile(GoStone.White);
        return true;
    }

    public void CancelPlayerEditPanel()
    {
        IsPlayerEditPanelOpen = false;
        IsPlayerEditDirty = false;
    }
}
