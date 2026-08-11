namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Gui.Domain;
using System;
using System.IO;

/// <summary>GTP エンジン編集パネルの開始・終了を管理します。</summary>
public sealed partial class GoAppSession
{
    public void ReplaceSelectedGtpEngine(GtpEngineProfile profile)
    {
        var index = GtpEngineEditProfileIndex;
        if (index >= 0 && index < _gtpEngineProfiles.Count)
            _gtpEngineProfiles[index] = profile.Clone();
    }

    public void SetGtpEngineEditField(GtpEngineProfileEditField field, string text, int caretIndex)
    {
        switch (field)
        {
            case GtpEngineProfileEditField.DisplayName: GtpEngineEditDraft.DisplayName = text; break;
            case GtpEngineProfileEditField.DefaultCgosLoginName: GtpEngineEditDraft.DefaultCgosLoginName = text; break;
            case GtpEngineProfileEditField.DefaultCgosPlainTextPassword: GtpEngineEditDraft.DefaultCgosPlainTextPassword = text; break;
            case GtpEngineProfileEditField.ExecutablePath: GtpEngineEditDraft.ExecutablePath = text; break;
            case GtpEngineProfileEditField.WorkingDirectory: GtpEngineEditDraft.WorkingDirectoryModel = WorkingDirectoryModel.FromString(text); break;
            case GtpEngineProfileEditField.Arguments: GtpEngineEditDraft.Arguments = text; break;
            default: throw new ArgumentOutOfRangeException(nameof(field), field, "GTP engine edit field is out of range.");
        }

        ActiveGtpEngineEditField = field;
        GtpEngineEditCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void BeginGtpEngineEditField(GtpEngineProfileEditField field, int caretIndex)
    {
        ActiveGtpEngineEditField = field;
        GtpEngineEditCaretIndex = Math.Clamp(caretIndex, 0, GetGtpEngineEditFieldText(field).Length);
        GtpEngineEditWarning = "";
    }

    public void EndGtpEngineEditField() => ActiveGtpEngineEditField = null;
    public void SetGtpEngineEditWarning(string warning) => GtpEngineEditWarning = warning;

    public void SetGtpEngineExecutablePathDraft(string executablePath)
    {
        GtpEngineEditDraft.ExecutablePath = executablePath;
        var executeDirectoryName = Path.GetDirectoryName(executablePath);
        GtpEngineEditDraft.WorkingDirectoryModel = executeDirectoryName is null
            ? GtpEngineEditDraft.WorkingDirectoryModel
            : WorkingDirectoryModel.FromString(executeDirectoryName);
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void SetGtpEngineWorkingDirectoryDraft(WorkingDirectoryModel workingDirectory)
    {
        GtpEngineEditDraft.WorkingDirectoryModel = workingDirectory;
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void ToggleGtpEngineEditLog()
    {
        GtpEngineEditDraft.EnableGtpLog = !GtpEngineEditDraft.EnableGtpLog;
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void CycleGtpEngineInitialPositionProfile()
    {
        string[] ids = [BuiltInGtpProfiles.AutoId, GenericGtpProfile.Instance.Id, BuiltInGtpProfiles.KifuwarabeId, BuiltInGtpProfiles.KataGoId, BuiltInGtpProfiles.LeelaZeroId, BuiltInGtpProfiles.GnuGoId];
        var current = Array.FindIndex(ids, id => id.Equals(GtpEngineEditDraft.InitialPositionProfileId, StringComparison.OrdinalIgnoreCase));
        GtpEngineEditDraft.InitialPositionProfileId = ids[(current + 1 + ids.Length) % ids.Length];
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void CycleGtpEngineInitialPositionPreferredMethod()
    {
        InitialPositionMethod?[] methods = [null, InitialPositionMethod.FixedHandicap, InitialPositionMethod.SetFreeHandicap, InitialPositionMethod.LoadSgf, InitialPositionMethod.KifuwarabeAtomicSetup, InitialPositionMethod.SequentialPlay];
        var current = Array.FindIndex(methods, method => method == GtpEngineEditDraft.InitialPositionManualPreferredMethod);
        GtpEngineEditDraft.InitialPositionManualPreferredMethod = methods[(current + 1 + methods.Length) % methods.Length];
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void SaveGtpEngineEditDraft(GtpEngineProfile profile)
    {
        if (IsGtpEngineAddPanelMode)
        {
            _gtpEngineProfiles.Add(profile.Clone());
            GtpEngineEditProfileIndex = _gtpEngineProfiles.Count - 1;
            GtpEngineSelectionPageIndex = (_gtpEngineProfiles.Count - 1) / GtpEngineSelectionPageSize;
            IsGtpEngineAddPanelMode = false;
        }
        else
        {
            ReplaceSelectedGtpEngine(profile);
        }

        GtpEngineEditDraft = _gtpEngineProfiles[GtpEngineEditProfileIndex].Clone();
        GtpEngineEditSaveMessage = "SAVED";
        GtpEngineEditWarning = "";
    }

    public void OpenGtpEngineDeleteConfirmation()
    {
        if (!CanDeleteSelectedGtpEngine)
            return;

        GtpEngineDeleteConfirmationName = _gtpEngineProfiles[GtpEngineDialogSelectionIndex].DisplayName;
        IsGtpEngineDeleteConfirmationOpen = true;
    }

    public void CloseGtpEngineDeleteConfirmation()
    {
        IsGtpEngineDeleteConfirmationOpen = false;
        GtpEngineDeleteConfirmationName = "";
    }

    public void RemoveSelectedGtpEngine()
    {
        if (!CanDeleteSelectedGtpEngine)
            return;

        var removedIndex = GtpEngineDialogSelectionIndex;
        var nextIndex = Math.Clamp(removedIndex, 0, _gtpEngineProfiles.Count - 2);
        _gtpEngineProfiles.RemoveAt(removedIndex);
        SelectedBlackGtpEngineIndex = AdjustGtpEngineSelectionAfterDelete(SelectedBlackGtpEngineIndex, removedIndex, nextIndex);
        SelectedWhiteGtpEngineIndex = AdjustGtpEngineSelectionAfterDelete(SelectedWhiteGtpEngineIndex, removedIndex, nextIndex);
        GtpEngineDialogSelectionIndex = nextIndex;
        CloseGtpEngineDeleteConfirmation();
        GtpEngineSelectionPageIndex = Math.Clamp(
            nextIndex / GtpEngineSelectionPageSize,
            0,
            Math.Max(0, (int)Math.Ceiling(_gtpEngineProfiles.Count / (double)GtpEngineSelectionPageSize) - 1));
    }

    public string GetGtpEngineEditFieldText(GtpEngineProfileEditField field) => field switch
    {
        GtpEngineProfileEditField.DisplayName => GtpEngineEditDraft.DisplayName,
        GtpEngineProfileEditField.DefaultCgosLoginName => GtpEngineEditDraft.DefaultCgosLoginName,
        GtpEngineProfileEditField.DefaultCgosPlainTextPassword => GtpEngineEditDraft.DefaultCgosPlainTextPassword,
        GtpEngineProfileEditField.ExecutablePath => GtpEngineEditDraft.ExecutablePath,
        GtpEngineProfileEditField.WorkingDirectory => GtpEngineEditDraft.WorkingDirectoryModel.Value,
        GtpEngineProfileEditField.Arguments => GtpEngineEditDraft.Arguments,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "GTP engine edit field is out of range."),
    };
    public void OpenGtpEngineEditPanel()
    {
        var index = GtpEngineDialogSelectionIndex;
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            return;

        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = true;
        IsGtpEngineAddPanelMode = false;
        GtpEngineEditProfileIndex = index;
        CloseGtpEngineDeleteConfirmation();
        GtpEngineEditDraft = _gtpEngineProfiles[index].Clone();
        ActiveGtpEngineEditField = null;
        GtpEngineEditCaretIndex = 0;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
    }

    public void OpenGtpEngineAddPanel()
    {
        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = true;
        IsGtpEngineAddPanelMode = true;
        CloseGtpEngineDeleteConfirmation();
        GtpEngineEditDraft = new GtpEngineProfile { DisplayName = "New GTP Engine" };
        ActiveGtpEngineEditField = null;
        GtpEngineEditCaretIndex = 0;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
    }

    public void OpenGtpEngineDuplicatePanel()
    {
        var index = GtpEngineDialogSelectionIndex;
        if (index < 0 || index >= _gtpEngineProfiles.Count)
            return;

        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = true;
        IsGtpEngineAddPanelMode = true;
        GtpEngineEditProfileIndex = index;
        CloseGtpEngineDeleteConfirmation();
        GtpEngineEditDraft = _gtpEngineProfiles[index].Clone();
        GtpEngineEditDraft.DisplayName = string.IsNullOrWhiteSpace(GtpEngineEditDraft.DisplayName)
            ? "Unnamed GTP Engine Copy"
            : $"{GtpEngineEditDraft.DisplayName.Trim()} Copy";
        ActiveGtpEngineEditField = null;
        GtpEngineEditCaretIndex = 0;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
    }

    public void CloseGtpEngineEditPanel()
    {
        var dialogSelectionIndex = GtpEngineEditProfileIndex;
        IsGtpEngineRandomMoveSelectionDialogOpen = false;
        IsGtpEngineGuiOptionsDialogOpen = false;
        GtpEngineGuiOptionsDialogDraft.Clear();
        IsGtpEngineEditPanelOpen = false;
        IsGtpEngineAddPanelMode = false;
        ActiveGtpEngineEditField = null;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
        if (EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider)
            OpenAppProviderGtpEngineSelectionDialog(GtpEngineSelectionAppId);
        else if (IsGtpEngineSelectionForCgos)
            OpenCgosGtpEngineSelectionDialog(GtpEngineSelectionTargetStone);
        else
            OpenGtpEngineSelectionDialog(GtpEngineSelectionTargetStone, GtpEngineSelectionAppId);
        if (dialogSelectionIndex >= 0 && dialogSelectionIndex < _gtpEngineProfiles.Count)
            GtpEngineDialogSelectionIndex = dialogSelectionIndex;
    }
}
