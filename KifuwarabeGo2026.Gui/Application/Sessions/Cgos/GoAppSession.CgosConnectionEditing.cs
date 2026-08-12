namespace KifuwarabeGo2026.Gui.Application;

using System;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;

/// <summary>CGOS 接続プロファイルの編集パネルを管理します。</summary>
public sealed partial class GoAppSession
{
    private static CgosConnectionProfile CreateDefaultCgosConnectionProfile() =>
        new("New CGOS Connection", "uec-go.com", 6809, "PRACTICE", "CGOS practice server") { Event = "PRACTICE" };

    public void OpenCgosConnectionEditPanel() => OpenCgosConnectionEditPanelCore(false, SelectedCgosConnectionProfile);

    public void OpenCgosConnectionAddPanel() => OpenCgosConnectionEditPanelCore(true, CreateDefaultCgosConnectionProfile());

    public void OpenCgosConnectionDuplicatePanel()
    {
        if (_cgosConnectionProfiles.Count == 0) return;
        var source = SelectedCgosConnectionProfile;
        OpenCgosConnectionEditPanelCore(true, source with
        {
            DisplayName = string.IsNullOrWhiteSpace(source.DisplayName) ? "Unnamed CGOS Connection Copy" : $"{source.DisplayName.Trim()} Copy",
        });
        _cgosConnectionEditSource = source;
    }

    private void OpenCgosConnectionEditPanelCore(bool isAdd, CgosConnectionProfile draft)
    {
        IsCgosConnectionEditPanelOpen = true;
        ActivateWindow(ActiveWindowId.CgosConnectionEdit);
        IsCgosConnectionAddPanelMode = isAdd;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = draft;
        CgosConnectionEditDraft = draft;
        CgosConnectionPortDraft = draft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void CloseCgosConnectionEditPanel()
    {
        IsCgosConnectionEditPanelOpen = false;
        DeactivateWindow(ActiveWindowId.CgosConnectionEdit);
        IsCgosConnectionAddPanelMode = false;
        ActiveCgosConnectionEditField = null;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void BeginCgosConnectionEditField(CgosConnectionProfileEditField field, int caretIndex)
    {
        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, GetCgosConnectionEditFieldText(field).Length);
        CgosConnectionEditWarning = "";
    }

    public void EndCgosConnectionEditField() => ActiveCgosConnectionEditField = null;
    public void SetCgosConnectionEditWarning(string warning) => CgosConnectionEditWarning = warning;

    public void SetCgosConnectionEditField(CgosConnectionProfileEditField field, string text, int caretIndex)
    {
        CgosConnectionEditDraft = field switch
        {
            CgosConnectionProfileEditField.DisplayName => CgosConnectionEditDraft with { DisplayName = text },
            CgosConnectionProfileEditField.Host => CgosConnectionEditDraft with { Host = text },
            CgosConnectionProfileEditField.Port => CgosConnectionEditDraft,
            CgosConnectionProfileEditField.Event => CgosConnectionEditDraft with { Event = text },
            CgosConnectionProfileEditField.Round => CgosConnectionEditDraft with { Round = text },
            CgosConnectionProfileEditField.Note => CgosConnectionEditDraft with { Note = text },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "CGOS connection edit field is out of range."),
        };
        if (field == CgosConnectionProfileEditField.Port) CgosConnectionPortDraft = text;
        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        CgosConnectionEditSaveMessage = "UNSAVED";
    }

    public void SaveCgosConnectionEditDraft(CgosConnectionProfile profile)
    {
        if (IsCgosConnectionAddPanelMode)
        {
            _cgosConnectionProfiles.Add(profile);
            SelectedCgosConnectionProfileIndex = _cgosConnectionProfiles.Count - 1;
            CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
            IsCgosConnectionAddPanelMode = false;
        }
        else _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex] = profile;

        CgosConnectionEditDraft = _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex];
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditSaveMessage = "SAVED";
        CgosConnectionEditWarning = "";
    }

    public void RemoveSelectedCgosConnectionProfile()
    {
        if (!CanDeleteSelectedCgosConnectionProfile) return;
        var removedIndex = SelectedCgosConnectionProfileIndex;
        var nextIndex = Math.Clamp(removedIndex, 0, _cgosConnectionProfiles.Count - 2);
        _cgosConnectionProfiles.RemoveAt(removedIndex);
        SelectedCgosConnectionProfileIndex = nextIndex;
        CgosConnectionSelectionPageIndex = Math.Clamp(nextIndex / CgosConnectionSelectionPageSize, 0, Math.Max(0, GetCgosConnectionSelectionPageCount() - 1));
    }

    public void SetCgosConnectionEditSelection(int start, int length) =>
        (CgosConnectionEditSelectionStart, CgosConnectionEditSelectionLength) = (start, length);
}
