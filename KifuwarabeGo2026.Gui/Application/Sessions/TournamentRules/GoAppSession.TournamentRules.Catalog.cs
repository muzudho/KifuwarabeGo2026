namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>大会規定セットの読込、選択、追加、並び替えを管理します。</summary>
public sealed partial class GoAppSession
{
    public void SetTournamentRules(IEnumerable<TournamentRules> rules)
    {
        _tournamentRules.Clear();
        _tournamentRules.AddRange(rules.Select(rule => rule.Clone()));
        if (_tournamentRules.Count == 0)
            _tournamentRules.Add(new TournamentRules());

        SelectTournamentRules(0);
    }

    public void OpenTournamentRulesOrderEditor()
    {
        TournamentRulesOrderEditor.Open(_tournamentRules, TournamentRulesDialogSelectionIndex, TournamentRulesSelectionPageSize);
        ActivateWindow(ActiveWindowId.CatalogOrderEditor);
    }

    public void CancelTournamentRulesOrderEditor()
    {
        TournamentRulesOrderEditor.Cancel();
        DeactivateWindow(ActiveWindowId.CatalogOrderEditor);
    }

    public IReadOnlyList<TournamentRules> CommitTournamentRulesOrderEditor()
    {
        var selectedRules = SelectedTournamentRulesIndex >= 0 && SelectedTournamentRulesIndex < _tournamentRules.Count
            ? _tournamentRules[SelectedTournamentRulesIndex]
            : null;
        var dialogRules = TournamentRulesDialogSelectionIndex >= 0 && TournamentRulesDialogSelectionIndex < _tournamentRules.Count
            ? _tournamentRules[TournamentRulesDialogSelectionIndex]
            : null;
        var orderedRules = TournamentRulesOrderEditor.Commit();
        DeactivateWindow(ActiveWindowId.CatalogOrderEditor);
        _tournamentRules.Clear();
        _tournamentRules.AddRange(orderedRules);
        SelectedTournamentRulesIndex = selectedRules is null ? 0 : Math.Max(0, _tournamentRules.IndexOf(selectedRules));
        TournamentRulesDialogSelectionIndex = dialogRules is null
            ? SelectedTournamentRulesIndex
            : Math.Max(0, _tournamentRules.IndexOf(dialogRules));
        TournamentRulesSelectionPageIndex = TournamentRulesDialogSelectionIndex / TournamentRulesSelectionPageSize;
        return _tournamentRules.Select(rule => rule.Clone()).ToArray();
    }

    public void SelectTournamentRules(int index)
    {
        if (index < 0 || index >= _tournamentRules.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "Tournament rules index is out of range.");

        SelectedTournamentRulesIndex = index;
        ApplyTournamentRules(_tournamentRules[index]);
        TournamentRulesSaveMessage = "";
        IsTournamentRulesDeleteConfirmationOpen = false;
    }

    public void AddAndSelectTournamentRules(TournamentRules rules)
    {
        _tournamentRules.Add(rules.Clone());
        SelectTournamentRules(_tournamentRules.Count - 1);
    }
}
