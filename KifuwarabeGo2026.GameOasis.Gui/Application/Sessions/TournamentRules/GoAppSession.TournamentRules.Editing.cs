namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;
using System;

/// <summary>大会規定の数値・表示名のポップアップ編集と保存済み化を管理します。</summary>
public sealed partial class GoAppSession
{
    public void SetTournamentRulesNumericSelection(int start, int length) =>
        (TournamentRulesNumericSelectionStart, TournamentRulesNumericSelectionLength) = (start, length);

    public void SetTournamentRulesDisplayNameSelection(int start, int length) =>
        (TournamentRulesDisplayNameSelectionStart, TournamentRulesDisplayNameSelectionLength) = (start, length);

    public void BeginTournamentRulesNumericEdit(TournamentRulesNumericField field, string draft, int caretIndex)
    {
        ActiveTournamentRulesNumericField = field;
        SetTournamentRulesNumericDraft(draft, caretIndex);
    }

    public void SetTournamentRulesNumericDraft(string draft, int caretIndex)
    {
        TournamentRulesNumericDraft = draft;
        TournamentRulesNumericCaretIndex = Math.Clamp(caretIndex, 0, draft.Length);
    }

    public void EndTournamentRulesNumericEdit()
    {
        ActiveTournamentRulesNumericField = null;
        TournamentRulesNumericDraft = "";
        TournamentRulesNumericCaretIndex = 0;
    }

    public void ChangeTournamentDisplayName(string displayName)
    {
        _currentTournamentRules.DisplayName = string.IsNullOrWhiteSpace(displayName)
            ? "Unnamed tournament"
            : displayName.Trim();
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void MarkTournamentRulesSaved()
    {
        if (SelectedTournamentRulesIndex >= 0 && SelectedTournamentRulesIndex < _tournamentRules.Count)
            _tournamentRules[SelectedTournamentRulesIndex] = _currentTournamentRules.Clone();

        TournamentRulesSaveMessage = "SAVED";
    }

    public void ReplaceCurrentTournamentRules(TournamentRules rules)
    {
        ApplyTournamentRules(rules);
        if (SelectedTournamentRulesIndex >= 0 && SelectedTournamentRulesIndex < _tournamentRules.Count)
            _tournamentRules[SelectedTournamentRulesIndex] = _currentTournamentRules.Clone();
    }

    public void SetTournamentRulesDisplayNameDraft(string displayName, int caretIndex)
    {
        TournamentRulesDisplayNameDraft = displayName;
        TournamentRulesDisplayNameCaretIndex = Math.Clamp(caretIndex, 0, displayName.Length);
    }

    public void BeginTournamentRulesDisplayNameEdit()
    {
        TournamentRulesDisplayNameDraft = _currentTournamentRules.DisplayName;
        TournamentRulesDisplayNameCaretIndex = TournamentRulesDisplayNameDraft.Length;
        IsTournamentRulesDisplayNameEditing = true;
        TournamentRulesDisplayNameWarning = "";
    }

    public void EndTournamentRulesDisplayNameEdit()
    {
        IsTournamentRulesDisplayNameEditing = false;
        TournamentRulesDisplayNameWarning = "";
    }

    public void SetTournamentRulesDisplayNameWarning(string warning) =>
        TournamentRulesDisplayNameWarning = warning;
}
