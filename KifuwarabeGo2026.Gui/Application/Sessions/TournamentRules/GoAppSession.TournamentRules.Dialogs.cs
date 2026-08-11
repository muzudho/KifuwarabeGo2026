namespace KifuwarabeGo2026.Gui.Application;

using System;

/// <summary>大会規定の選択・追加・削除ダイアログ状態を管理します。</summary>
public sealed partial class GoAppSession
{
    public void OpenTournamentRulesSelectionDialog()
    {
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesSelectionDialogOpen = true;
        TournamentRulesDialogSelectionIndex = SelectedTournamentRulesIndex;
        TournamentRulesSelectionPageIndex = TournamentRulesDialogSelectionIndex / TournamentRulesSelectionPageSize;
    }

    public void SelectTournamentRulesDialogItem(int index)
    {
        if (index < 0 || index >= _tournamentRules.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Tournament rules index is out of range.");

        TournamentRulesDialogSelectionIndex = index;
    }

    public void CommitTournamentRulesSelectionDialog()
    {
        SelectTournamentRules(TournamentRulesDialogSelectionIndex);
        IsTournamentRulesSelectionDialogOpen = false;
    }

    public void CancelTournamentRulesSelectionDialog()
    {
        TournamentRulesDialogSelectionIndex = SelectedTournamentRulesIndex;
        IsTournamentRulesSelectionDialogOpen = false;
    }

    public void OpenTournamentRulesAddPanel(bool editExisting)
    {
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = false;
        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = true;
        IsTournamentRulesEditPanelMode = editExisting;
        IsTournamentRulesDeleteConfirmationOpen = false;
    }

    public void CloseTournamentRulesAddPanel()
    {
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesEditPanelMode = false;
        OpenTournamentRulesSelectionDialog();
    }

    public bool CanDeleteSelectedTournamentRules =>
        _tournamentRules.Count > 1 &&
        TournamentRulesDialogSelectionIndex >= 0 &&
        TournamentRulesDialogSelectionIndex < _tournamentRules.Count;

    public void OpenTournamentRulesDeleteConfirmation()
    {
        if (!CanDeleteSelectedTournamentRules)
            return;

        TournamentRulesDeleteConfirmationFileName = _tournamentRules[TournamentRulesDialogSelectionIndex].DisplayName;
        IsTournamentRulesDeleteConfirmationOpen = true;
    }

    public void CloseTournamentRulesDeleteConfirmation()
    {
        IsTournamentRulesDeleteConfirmationOpen = false;
        TournamentRulesDeleteConfirmationFileName = "";
    }

    public void RemoveSelectedTournamentRules()
    {
        if (!CanDeleteSelectedTournamentRules)
            return;

        var nextIndex = Math.Clamp(SelectedTournamentRulesIndex, 0, _tournamentRules.Count - 2);
        _tournamentRules.RemoveAt(SelectedTournamentRulesIndex);
        CloseTournamentRulesDeleteConfirmation();
        SelectTournamentRules(nextIndex);
        TournamentRulesSelectionPageIndex = Math.Clamp(
            nextIndex / TournamentRulesSelectionPageSize,
            0,
            Math.Max(0, (int)Math.Ceiling(_tournamentRules.Count / (double)TournamentRulesSelectionPageSize) - 1));
    }

    public void MoveTournamentRulesSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_tournamentRules.Count / (double)TournamentRulesSelectionPageSize));
        TournamentRulesSelectionPageIndex = Math.Clamp(TournamentRulesSelectionPageIndex + step, 0, pageCount - 1);
    }
}
