namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.GtpExtensions.Engines;
using System;

/// <summary>GTP エンジン編集パネルの開始・終了を管理します。</summary>
public sealed partial class GoAppSession
{
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
