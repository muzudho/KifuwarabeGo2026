namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>大会ルール選択・編集画面の状態を保持します。</summary>
public sealed partial class GoAppSession
{
    private readonly List<TournamentRules> _tournamentRules = new();
    private TournamentRules _currentTournamentRules = new();

    public IReadOnlyList<TournamentRules> TournamentRulesList => _tournamentRules;
    public CatalogOrderEditor<TournamentRules> TournamentRulesOrderEditor { get; } = new();
    public int SelectedTournamentRulesIndex { get; private set; }
    public bool IsTournamentRulesSelectionDialogOpen { get; private set; }
    public int TournamentRulesDialogSelectionIndex { get; private set; }
    public bool IsTournamentRulesAddPanelOpen { get; private set; }
    public bool IsTournamentRulesEditPanelMode { get; private set; }
    public bool IsTournamentRulesDeleteConfirmationOpen { get; private set; }
    public string TournamentRulesDeleteConfirmationFileName { get; private set; } = "";
    public int TournamentRulesSelectionPageIndex { get; private set; }
    public string TournamentRulesSaveMessage { get; private set; } = "";
    public string TournamentRulesDisplayNameDraft { get; private set; } = "";
    public bool IsTournamentRulesDisplayNameEditing { get; private set; }
    public int TournamentRulesDisplayNameCaretIndex { get; private set; }
    public int TournamentRulesDisplayNameSelectionStart { get; private set; }
    public int TournamentRulesDisplayNameSelectionLength { get; private set; }
    public string TournamentRulesDisplayNameWarning { get; private set; } = "";
    public TournamentRulesNumericField? ActiveTournamentRulesNumericField { get; private set; }
    public string TournamentRulesNumericDraft { get; private set; } = "";
    public int TournamentRulesNumericCaretIndex { get; private set; }
    public int TournamentRulesNumericSelectionStart { get; private set; }
    public int TournamentRulesNumericSelectionLength { get; private set; }
    public string TournamentDisplayName => _currentTournamentRules.DisplayName;
    public GoRuleKind RuleKind => _currentTournamentRules.Rule;
    public decimal Komi => _currentTournamentRules.Komi;
    public TimeSpan MainTime => _currentTournamentRules.MainTime;
    public int MoveLimit => _currentTournamentRules.MoveLimit;
    public TournamentRules CurrentTournamentRules => _currentTournamentRules.Clone();
}
