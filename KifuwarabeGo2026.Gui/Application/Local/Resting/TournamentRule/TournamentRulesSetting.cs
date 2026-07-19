namespace KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

/// <summary>
/// ［大会ルール設定］画面の処理
/// </summary>
public sealed class TournamentRulesSetting
{
    private const int MaxDisplayNameLength = 80;

    private readonly GoAppSession _session;
    private readonly TournamentRulesCatalog _catalog;
    private readonly Action _browseTournamentRules;
    private readonly Func<TournamentRules, string?> _browseTournamentRulesFilePath;
    private readonly TextBoxController _displayNameTextBox = new(MaxDisplayNameLength);
    private KeyboardState _previousKeyboard;

    public TournamentRulesSetting(
        GoAppSession session,
        TournamentRulesCatalog catalog,
        Action browseTournamentRules,
        Func<TournamentRules, string?> browseTournamentRulesFilePath)
    {
        _session = session;
        _catalog = catalog;
        _browseTournamentRules = browseTournamentRules;
        _browseTournamentRulesFilePath = browseTournamentRulesFilePath;
    }

    public void UpdateByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!_session.IsTournamentRulesAddPanelOpen)
        {
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.IsTournamentRulesDisplayNameEditing)
        {
            HandleDisplayNameKeyboard(keyboard, gameTime);
            _previousKeyboard = keyboard;
            return;
        }

        UpdateBoardSizeByKeyboard(keyboard);

        if (IsNewKeyPress(keyboard, Keys.F5))
        {
            SaveCurrentTournamentRules();
        }

        _previousKeyboard = keyboard;
    }

    public bool TryInputCharacter(char character)
    {
        if (!_session.IsTournamentRulesAddPanelOpen || !_session.IsTournamentRulesDisplayNameEditing)
        {
            return false;
        }

        if (!_displayNameTextBox.TryInputCharacter(character))
        {
            _session.SetTournamentRulesDisplayNameWarning("Display name is too long.");
            return true;
        }

        SyncDisplayNameDraft();
        UpdateDisplayNameWarning();
        return true;
    }

    public bool TryHandleMouseClick(Point point, Func<Point, string, int>? getDisplayNameCaretIndex = null)
    {
        if (_session.IsTournamentRulesSelectionDialogOpen)
        {
            return TryHandleTournamentRulesSelectionDialogClick(point);
        }

        if (_session.IsTournamentRulesAddPanelOpen)
        {
            return TryHandleTournamentRulesAddPanelClick(point, getDisplayNameCaretIndex);
        }

        if (GoScreenRenderer.GetTournamentRulesBrowseButtonHit(point))
        {
            _browseTournamentRules();
            return true;
        }

        return false;
    }

    private bool TryHandleTournamentRulesAddPanelClick(Point point, Func<Point, string, int>? getDisplayNameCaretIndex)
    {
        if (GoScreenRenderer.GetTournamentRulesAddPanelCloseButtonHit(point))
        {
            CancelDisplayNameEdit();
            _session.CloseTournamentRulesAddPanel();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesAddPanelDisplayNameBoxHit(point))
        {
            MoveOrBeginDisplayNameEdit(point, getDisplayNameCaretIndex);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesAddPanelFileBrowseButtonHit(point))
        {
            BrowseFilePath();
            return true;
        }

        if (GoScreenRenderer.GetRuleKindButtonHit(point) is { } ruleKind)
        {
            _session.ChangeRuleKind(ruleKind);
            return true;
        }

        if (GoScreenRenderer.GetBoardSizeButtonHit(point, _session.CurrentMode.Kind) is { } boardSize)
        {
            _session.ChangeBoardSize(boardSize);
            return true;
        }

        if (GoScreenRenderer.GetKomiStepButtonHit(point) is { } komiStep)
        {
            _session.ChangeKomi(komiStep);
            return true;
        }

        if (GoScreenRenderer.GetMainTimeStepButtonHit(point) is { } mainTimeStep)
        {
            _session.ChangeMainTime(mainTimeStep);
            return true;
        }

        if (GoScreenRenderer.GetMoveLimitStepButtonHit(point) is { } moveLimitStep)
        {
            _session.ChangeMoveLimit(moveLimitStep);
            return true;
        }

        if (GoScreenRenderer.GetSaveTournamentRulesButtonHit(point))
        {
            SaveCurrentTournamentRules();
            return true;
        }

        return false;
    }

    private bool TryHandleTournamentRulesSelectionDialogClick(Point point)
    {
        if (_session.IsTournamentRulesDeleteConfirmationOpen)
        {
            return TryHandleTournamentRulesDeleteConfirmationClick(point);
        }

        if (GoScreenRenderer.TryGetTournamentRulesSelectionDialogPathCopyText(point, _session, out var path))
        {
            SystemClipboard.SetText(path);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogCancelButtonHit(point))
        {
            _session.CancelTournamentRulesSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogOkButtonHit(point))
        {
            _session.CommitTournamentRulesSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogAddButtonHit(point))
        {
            CreateNewTournamentRules();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogEditButtonHit(point))
        {
            EditSelectedTournamentRules();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogDuplicateButtonHit(point))
        {
            DuplicateSelectedTournamentRules();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogDeleteButtonHit(point, _session.CanDeleteSelectedTournamentRules))
        {
            _session.OpenTournamentRulesDeleteConfirmation();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogPreviousPageButtonHit(point))
        {
            _session.MoveTournamentRulesSelectionPage(-1);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogNextPageButtonHit(point))
        {
            _session.MoveTournamentRulesSelectionPage(1);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogListItemHit(point, _session) is { } index)
        {
            _session.SelectTournamentRulesDialogItem(index);
            return true;
        }

        return true;
    }

    private bool TryHandleTournamentRulesDeleteConfirmationClick(Point point)
    {
        if (GoScreenRenderer.GetTournamentRulesDeleteConfirmationCancelButtonHit(point))
        {
            _session.CloseTournamentRulesDeleteConfirmation();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesDeleteConfirmationConfirmButtonHit(point))
        {
            DeleteSelectedTournamentRules();
            return true;
        }

        return true;
    }

    private void CreateNewTournamentRules()
    {
        var rules = _catalog.CreateNew(_session.CurrentTournamentRules);
        _session.AddAndSelectTournamentRules(rules);
        _session.OpenTournamentRulesAddPanel(editExisting: false);
        BeginDisplayNameEdit();
        _session.MarkTournamentRulesSaved();
    }

    private void EditSelectedTournamentRules()
    {
        if (_session.TournamentRulesDialogSelectionIndex < 0 || _session.TournamentRulesDialogSelectionIndex >= _session.TournamentRulesList.Count)
        {
            return;
        }

        _session.SelectTournamentRules(_session.TournamentRulesDialogSelectionIndex);
        _session.OpenTournamentRulesAddPanel(editExisting: true);
    }

    private void DuplicateSelectedTournamentRules()
    {
        if (_session.TournamentRulesDialogSelectionIndex < 0 || _session.TournamentRulesDialogSelectionIndex >= _session.TournamentRulesList.Count)
        {
            return;
        }

        var rules = _catalog.Duplicate(_session.TournamentRulesList[_session.TournamentRulesDialogSelectionIndex]);
        _session.AddAndSelectTournamentRules(rules);
        _session.OpenTournamentRulesAddPanel(editExisting: false);
        BeginDisplayNameEdit();
        _session.MarkTournamentRulesSaved();
    }

    private void DeleteSelectedTournamentRules()
    {
        if (!_session.CanDeleteSelectedTournamentRules)
        {
            _session.CloseTournamentRulesDeleteConfirmation();
            return;
        }

        try
        {
            _session.SelectTournamentRules(_session.TournamentRulesDialogSelectionIndex);
            _catalog.Delete(_session.TournamentRulesList[_session.SelectedTournamentRulesIndex]);
            _session.RemoveSelectedTournamentRules();
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _session.CloseTournamentRulesDeleteConfirmation();
            _session.SetTournamentRulesDisplayNameWarning("Rules file could not be deleted.");
        }
    }

    private void BeginDisplayNameEdit()
    {
        BeginDisplayNameEdit(_session.TournamentDisplayName.Length);
    }

    private void BeginDisplayNameEdit(int caretIndex)
    {
        _displayNameTextBox.Begin(_session.TournamentDisplayName, caretIndex);
        SyncDisplayNameDraft();
        _session.BeginTournamentRulesDisplayNameEdit();
        _session.SetTournamentRulesDisplayNameDraft(_displayNameTextBox.Text, _displayNameTextBox.CaretIndex);
        UpdateDisplayNameWarning();
    }

    private void MoveOrBeginDisplayNameEdit(Point point, Func<Point, string, int>? getDisplayNameCaretIndex)
    {
        var text = _session.IsTournamentRulesDisplayNameEditing
            ? _displayNameTextBox.Text
            : _session.TournamentDisplayName;
        var caretIndex = getDisplayNameCaretIndex?.Invoke(point, text) ?? text.Length;

        if (_session.IsTournamentRulesDisplayNameEditing)
        {
            _displayNameTextBox.SetCaretIndex(caretIndex);
            SyncDisplayNameDraft();
            return;
        }

        BeginDisplayNameEdit(caretIndex);
    }

    private void HandleDisplayNameKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        switch (_displayNameTextBox.HandleKeyboard(keyboard, _previousKeyboard, gameTime))
        {
            case TextBoxKeyboardAction.Commit:
                CommitDisplayNameEdit();
                break;
            case TextBoxKeyboardAction.Cancel:
                CancelDisplayNameEdit();
                break;
            default:
                SyncDisplayNameDraft();
                UpdateDisplayNameWarning();
                break;
        }
    }

    private void CommitDisplayNameEdit()
    {
        if (!TryApplyDisplayName())
        {
            return;
        }

        _session.EndTournamentRulesDisplayNameEdit();
        _displayNameTextBox.Clear();
    }

    private void CancelDisplayNameEdit()
    {
        if (!_session.IsTournamentRulesDisplayNameEditing)
        {
            return;
        }

        _session.EndTournamentRulesDisplayNameEdit();
        _displayNameTextBox.Clear();
    }

    private bool TryApplyDisplayName()
    {
        if (string.IsNullOrWhiteSpace(_displayNameTextBox.Text))
        {
            _session.SetTournamentRulesDisplayNameWarning("Display name is required.");
            return false;
        }

        _session.ChangeTournamentDisplayName(_displayNameTextBox.Text);
        _session.SetTournamentRulesDisplayNameDraft(_session.TournamentDisplayName, _session.TournamentDisplayName.Length);
        return true;
    }

    private void UpdateDisplayNameWarning()
    {
        _session.SetTournamentRulesDisplayNameWarning(string.IsNullOrWhiteSpace(_displayNameTextBox.Text) ? "Display name is required." : "");
    }

    private void SyncDisplayNameDraft()
    {
        _session.SetTournamentRulesDisplayNameDraft(_displayNameTextBox.Text, _displayNameTextBox.CaretIndex);
    }

    private void BrowseFilePath()
    {
        if (_session.IsTournamentRulesDisplayNameEditing)
        {
            if (!TryApplyDisplayName())
            {
                return;
            }

            _session.EndTournamentRulesDisplayNameEdit();
            _displayNameTextBox.Clear();
        }

        var targetPath = _browseTournamentRulesFilePath(_session.CurrentTournamentRules);
        if (string.IsNullOrWhiteSpace(targetPath))
        {
            return;
        }

        try
        {
            var savedRules = _catalog.SaveAsFilePath(_session.CurrentTournamentRules, targetPath);
            _session.ReplaceCurrentTournamentRules(savedRules);
            _session.MarkTournamentRulesSaved();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _session.SetTournamentRulesDisplayNameWarning("File path could not be saved.");
        }
    }

    private void UpdateBoardSizeByKeyboard(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1))
        {
            _session.ChangeBoardSize(9);
        }
        else if (keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2))
        {
            _session.ChangeBoardSize(13);
        }
        else if (keyboard.IsKeyDown(Keys.D3) || keyboard.IsKeyDown(Keys.NumPad3))
        {
            _session.ChangeBoardSize(19);
        }
    }

    private void SaveCurrentTournamentRules()
    {
        if (_session.IsTournamentRulesDisplayNameEditing && !TryApplyDisplayName())
        {
            return;
        }

        _catalog.Save(_session.CurrentTournamentRules);
        _session.MarkTournamentRulesSaved();
    }

    private bool IsNewKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
}
