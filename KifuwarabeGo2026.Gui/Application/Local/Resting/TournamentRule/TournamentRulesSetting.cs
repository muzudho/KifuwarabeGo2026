namespace KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

/// <summary>
/// ［大会ルール設定］画面の処理
/// </summary>
public sealed class TournamentRulesSetting
{
    private const int MaxDisplayNameLength = 80;

    private readonly GoAppSession _session;
    private readonly TournamentRulesCatalog _catalog;
    private readonly Action _browseTournamentRules;
    private readonly Action _beginDiscardTransition;
    private readonly IClipboardService _clipboardService;
    private readonly TextBoxController _displayNameTextBox = new(MaxDisplayNameLength);
    private readonly TextBoxController _mainTimeHoursTextBox = new(3);
    private readonly TextBoxController _mainTimeMinutesTextBox = new(2);
    private readonly TextBoxController _mainTimeSecondsTextBox = new(2);
    private readonly TextBoxController _moveLimitTextBox = new(4);

    /// <summary>
    /// ［コミ］の入力欄のコントローラー
    /// </summary>
    private readonly TextBoxController _komiTextBox = new(5);
    private readonly TextBoxController _moveLimitInputTextBox = new(4);
    private readonly TextBoxController[] _timeInputTextBoxes = [new(3), new(2), new(2)];
    private KeyboardState _previousKeyboard;

    public bool IsKomiInputOpen { get; private set; }
    public string KomiInputText => _komiTextBox.Text;
    public int KomiInputCaretIndex => _komiTextBox.CaretIndex;
    public int KomiInputSelectionStart => _komiTextBox.SelectionStart;
    public int KomiInputSelectionLength => _komiTextBox.SelectionLength;
    public string KomiInputMessage { get; private set; } = "RANGE  0.0 .. 99.5";
    public bool IsMoveLimitInputOpen { get; private set; }
    public string MoveLimitInputText => _moveLimitInputTextBox.Text;
    public int MoveLimitInputCaretIndex => _moveLimitInputTextBox.CaretIndex;
    public int MoveLimitInputSelectionStart => _moveLimitInputTextBox.SelectionStart;
    public int MoveLimitInputSelectionLength => _moveLimitInputTextBox.SelectionLength;
    public string MoveLimitInputMessage { get; private set; } = "RANGE  0 .. 9999";
    public bool IsTimeInputOpen { get; private set; }
    public int ActiveTimeInputPart { get; private set; }
    public string[] TimeInputTexts => _timeInputTextBoxes.Select(box => box.Text).ToArray();
    public int[] TimeInputCaretIndices => _timeInputTextBoxes.Select(box => box.CaretIndex).ToArray();
    public string TimeInputMessage { get; private set; } = "HOURS 0..999     MINUTES / SECONDS 0..59";

    public TournamentRulesSetting(
        GoAppSession session,
        TournamentRulesCatalog catalog,
        Action browseTournamentRules,
        Action beginDiscardTransition,
        IClipboardService clipboardService)
    {
        _session = session;
        _catalog = catalog;
        _browseTournamentRules = browseTournamentRules;
        _beginDiscardTransition = beginDiscardTransition;
        _clipboardService = clipboardService;
    }

    public void UpdateByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!_session.IsTournamentRulesAddPanelOpen)
        {
            _previousKeyboard = keyboard;
            return;
        }

        if (IsTimeInputOpen)
        {
            var action = _timeInputTextBoxes[ActiveTimeInputPart].HandleKeyboard(keyboard, _previousKeyboard, gameTime, _clipboardService,
                pasteCharacterFilter: char.IsAsciiDigit);
            if (action == TextBoxKeyboardAction.Commit) CommitTimeInput();
            else if (action == TextBoxKeyboardAction.Cancel) CancelTimeInput();
            _previousKeyboard = keyboard;
            return;
        }

        if (IsKomiInputOpen)
        {
            var action = _komiTextBox.HandleKeyboard(keyboard, _previousKeyboard, gameTime, _clipboardService,
                pasteCharacterFilter: character => char.IsAsciiDigit(character) || character == '.');
            if (action == TextBoxKeyboardAction.Commit) CommitKomiInput();
            else if (action == TextBoxKeyboardAction.Cancel) CancelKomiInput();
            _previousKeyboard = keyboard;
            return;
        }

        if (IsMoveLimitInputOpen)
        {
            var action = _moveLimitInputTextBox.HandleKeyboard(keyboard, _previousKeyboard, gameTime, _clipboardService,
                pasteCharacterFilter: char.IsAsciiDigit);
            if (action == TextBoxKeyboardAction.Commit) CommitMoveLimitInput();
            else if (action == TextBoxKeyboardAction.Cancel) CancelMoveLimitInput();
            _previousKeyboard = keyboard;
            return;
        }

        if (IsNewKeyPress(keyboard, Keys.Tab))
        {
            MoveTextBoxFocus(IsShiftDown(keyboard) ? -1 : 1);
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.IsTournamentRulesDisplayNameEditing)
        {
            HandleDisplayNameKeyboard(keyboard, gameTime);
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.ActiveTournamentRulesNumericField is { } numericField)
        {
            HandleNumericKeyboard(numericField, keyboard, gameTime);
            _previousKeyboard = keyboard;
            return;
        }

        UpdateBoardSizeByKeyboard(keyboard);

        if (IsNewKeyPress(keyboard, Keys.F5))
        {
            if (SaveCurrentTournamentRules())
                _session.CloseTournamentRulesAddPanel();
        }

        _previousKeyboard = keyboard;
    }

    public void SynchronizeKeyboardState(KeyboardState keyboard) =>
        _previousKeyboard = keyboard;

    public bool TryInputCharacter(char character)
    {
        if (!_session.IsTournamentRulesAddPanelOpen)
        {
            return false;
        }

        if (IsTimeInputOpen)
        {
            if (char.IsAsciiDigit(character)) _timeInputTextBoxes[ActiveTimeInputPart].TryInputCharacter(character);
            return true;
        }

        if (IsKomiInputOpen)
        {
            if (char.IsAsciiDigit(character) || character == '.') _komiTextBox.TryInputCharacter(character);
            return true;
        }

        if (IsMoveLimitInputOpen)
        {
            if (char.IsAsciiDigit(character)) _moveLimitInputTextBox.TryInputCharacter(character);
            return true;
        }

        if (_session.ActiveTournamentRulesNumericField is { } numericField)
        {
            if (!char.IsAsciiDigit(character))
            {
                return true;
            }

            var controller = GetNumericController(numericField);
            controller.TryInputCharacter(character);
            SyncNumericDraft(numericField);
            return true;
        }

        if (!_session.IsTournamentRulesDisplayNameEditing)
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

    public bool TryHandleMouseClick(
        Point point,
        Func<Point, string, int>? getDisplayNameCaretIndex = null,
        Func<Point, TournamentRulesNumericField, string, int>? getNumericCaretIndex = null)
    {
        if (_session.IsTournamentRulesSelectionDialogOpen)
        {
            return TryHandleTournamentRulesSelectionDialogClick(point);
        }

        if (_session.IsTournamentRulesAddPanelOpen)
        {
            return TryHandleTournamentRulesAddPanelClick(point, getDisplayNameCaretIndex, getNumericCaretIndex);
        }

        if (GoScreenRenderer.GetTournamentRulesBrowseButtonHit(point))
        {
            _browseTournamentRules();
            return true;
        }

        return false;
    }

    public void OpenKomiInput()
    {
        CommitNumericEdit();
        _komiTextBox.Begin(_session.Komi.ToString("0.0"));
        KomiInputMessage = "RANGE  0.0 .. 99.5     STEP 0.5";
        IsKomiInputOpen = true;
        _session.ActivateModalWindow(ActiveWindowId.IntegerInput);
    }

    public void BeginKomiInputSelection(int caretIndex, bool extendSelection) => _komiTextBox.BeginMouseSelection(caretIndex, extendSelection);

    public void ChangeKomiInput(decimal step)
    {
        var value = decimal.TryParse(_komiTextBox.Text, out var current) ? current : _session.Komi;
        _komiTextBox.Begin(decimal.Clamp(value + step, 0m, 99.5m).ToString("0.0"));
    }

    public void CommitKomiInput()
    {
        if (!decimal.TryParse(_komiTextBox.Text, out var value) || value < 0m || value > 99.5m || value * 2m != decimal.Truncate(value * 2m))
        {
            KomiInputMessage = "ENTER 0.0 .. 99.5 IN 0.5 STEPS";
            return;
        }
        _session.ChangeKomi(value - _session.Komi);
        CancelKomiInput();
    }

    public void CancelKomiInput()
    {
        IsKomiInputOpen = false;
        _komiTextBox.Clear();
        KomiInputMessage = "RANGE  0.0 .. 99.5";
        _session.DeactivateModalWindow(ActiveWindowId.IntegerInput);
    }

    public void OpenMoveLimitInput()
    {
        CommitNumericEdit();
        _moveLimitInputTextBox.Begin(_session.MoveLimit.ToString());
        MoveLimitInputMessage = "RANGE  0 .. 9999";
        IsMoveLimitInputOpen = true;
        _session.ActivateModalWindow(ActiveWindowId.IntegerInput);
    }

    public void BeginMoveLimitInputSelection(int caretIndex, bool extendSelection) => _moveLimitInputTextBox.BeginMouseSelection(caretIndex, extendSelection);

    public void ChangeMoveLimitInput(int step)
    {
        var value = int.TryParse(_moveLimitInputTextBox.Text, out var current) ? current : _session.MoveLimit;
        _moveLimitInputTextBox.Begin(Math.Clamp(value + step, 0, 9999).ToString());
    }

    public void CommitMoveLimitInput()
    {
        if (!int.TryParse(_moveLimitInputTextBox.Text, out var value) || value is < 0 or > 9999)
        {
            MoveLimitInputMessage = "ENTER 0 .. 9999";
            return;
        }
        _session.SetMoveLimit(value);
        CancelMoveLimitInput();
    }

    public void CancelMoveLimitInput()
    {
        IsMoveLimitInputOpen = false;
        _moveLimitInputTextBox.Clear();
        MoveLimitInputMessage = "RANGE  0 .. 9999";
        _session.DeactivateModalWindow(ActiveWindowId.IntegerInput);
    }

    public void OpenTimeInput()
    {
        CommitNumericEdit();
        var time = _session.MainTime;
        _timeInputTextBoxes[0].Begin(((int)time.TotalHours).ToString("00"));
        _timeInputTextBoxes[1].Begin(time.Minutes.ToString("00"));
        _timeInputTextBoxes[2].Begin(time.Seconds.ToString("00"));
        ActiveTimeInputPart = 0;
        TimeInputMessage = "HOURS 0..999     MINUTES / SECONDS 0..59";
        IsTimeInputOpen = true;
        _session.ActivateModalWindow(ActiveWindowId.IntegerInput);
    }

    public void BeginTimeInputSelection(int part, int caretIndex)
    {
        ActiveTimeInputPart = Math.Clamp(part, 0, 2);
        _timeInputTextBoxes[ActiveTimeInputPart].BeginMouseSelection(caretIndex, IsShiftDown());
    }

    public void ChangeTimeInput(int part, int step)
    {
        part = Math.Clamp(part, 0, 2);
        var current = int.TryParse(_timeInputTextBoxes[part].Text, out var value) ? value : 0;
        var maximum = part == 0 ? 999 : 59;
        _timeInputTextBoxes[part].Begin(Math.Clamp(current + step, 0, maximum).ToString("00"));
        ActiveTimeInputPart = part;
    }

    public void CommitTimeInput()
    {
        if (!int.TryParse(_timeInputTextBoxes[0].Text, out var hours) || hours is < 0 or > 999 ||
            !int.TryParse(_timeInputTextBoxes[1].Text, out var minutes) || minutes is < 0 or > 59 ||
            !int.TryParse(_timeInputTextBoxes[2].Text, out var seconds) || seconds is < 0 or > 59)
        {
            TimeInputMessage = "ENTER hhh:mm:ss  (HOURS 0..999, MINUTES / SECONDS 0..59)";
            return;
        }
        _session.SetMainTime(hours * 3600 + minutes * 60 + seconds);
        CancelTimeInput();
    }

    public void CancelTimeInput()
    {
        IsTimeInputOpen = false;
        foreach (var textBox in _timeInputTextBoxes) textBox.Clear();
        TimeInputMessage = "HOURS 0..999     MINUTES / SECONDS 0..59";
        _session.DeactivateModalWindow(ActiveWindowId.IntegerInput);
    }

    private bool TryHandleTournamentRulesAddPanelClick(
        Point point,
        Func<Point, string, int>? getDisplayNameCaretIndex,
        Func<Point, TournamentRulesNumericField, string, int>? getNumericCaretIndex)
    {
        if (GoScreenRenderer.GetTournamentRulesAddPanelCloseButtonHit(point) && _session.IsTournamentRulesDirty)
        {
            CommitNumericEdit();
            CancelDisplayNameEdit();
            _session.CloseTournamentRulesAddPanel();
            _beginDiscardTransition();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesAddPanelDisplayNameBoxHit(point))
        {
            CommitNumericEdit();
            MoveOrBeginDisplayNameEdit(point, getDisplayNameCaretIndex);
            return true;
        }

        if (TournamentRuleEditorLayout.GetRuleKindButtonHit(point) is { } ruleKind)
        {
            CommitNumericEdit();
            _session.ChangeRuleKind(ruleKind);
            return true;
        }

        if (TournamentRuleEditorLayout.GetBoardSizeButtonHit(point, _session.CurrentMode.Kind) is { } boardSize)
        {
            _session.ChangeBoardSize(boardSize);
            return true;
        }

        if (TournamentRuleKomiField.IsTextBoxHit(point))
        {
            OpenKomiInput();
            return true;
        }

        if (TournamentRuleEditorLayout.IsTimeTextBoxHit(point))
        {
            OpenTimeInput();
            return true;
        }

        if (TournamentRuleEditorLayout.IsMoveLimitTextBoxHit(point))
        {
            OpenMoveLimitInput();
            return true;
        }

        if (GoScreenRenderer.GetSaveTournamentRulesButtonHit(point))
        {
            if (_session.IsTournamentRulesDirty)
            {
                if (SaveCurrentTournamentRules()) _session.CloseTournamentRulesAddPanel();
            }
            else _session.CloseTournamentRulesAddPanel();
            return true;
        }

        return false;
    }

    private bool TryHandleTournamentRulesSelectionDialogClick(Point point)
    {
        if (_session.TournamentRulesOrderEditor.IsOpen)
        {
            return TryHandleTournamentRulesOrderEditorClick(point);
        }

        if (_session.IsTournamentRulesDeleteConfirmationOpen)
        {
            return TryHandleTournamentRulesDeleteConfirmationClick(point);
        }

        if (GoScreenRenderer.TryGetTournamentRulesSelectionDialogPathCopyText(point, _session, out var path))
        {
            _clipboardService.TrySetText(path);
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

        if (_session.TournamentRulesList.Count > 1 &&
            GoScreenRenderer.GetTournamentRulesSelectionDialogOrderButtonHit(point))
        {
            _session.OpenTournamentRulesOrderEditor();
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

    private bool TryHandleTournamentRulesOrderEditorClick(Point point)
    {
        var editor = _session.TournamentRulesOrderEditor;
        if (GoScreenRenderer.GetCatalogOrderCancelButtonHit(point) && editor.HasChanges)
        {
            _session.CancelTournamentRulesOrderEditor();
            _beginDiscardTransition();
            return true;
        }

        if (GoScreenRenderer.GetCatalogOrderSaveButtonHit(point))
        {
            if (editor.HasChanges)
            {
                var rules = _session.CommitTournamentRulesOrderEditor();
                _catalog.SaveOrder(rules);
            }
            else _session.CancelTournamentRulesOrderEditor();
            return true;
        }

        var moveStep = GoScreenRenderer.GetCatalogOrderMoveStep(point, editor.PageSize);
        if (moveStep == int.MinValue)
            editor.MoveSelectedToTop();
        else if (moveStep != 0)
            editor.MoveSelected(moveStep);
        else if (GoScreenRenderer.GetCatalogOrderPageStep(point) is var pageStep && pageStep != 0)
            editor.MoveVisiblePages(pageStep);
        else if (GoScreenRenderer.GetCatalogOrderCardHit(point, editor) is { } index)
            editor.BeginDrag(index);

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
            _displayNameTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            SyncDisplayNameDraft();
            return;
        }

        BeginDisplayNameEdit(caretIndex);
        _displayNameTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        SyncDisplayNameDraft();
    }

    private void HandleDisplayNameKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        switch (_displayNameTextBox.HandleKeyboard(keyboard, _previousKeyboard, gameTime, _clipboardService))
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
        _session.SetTournamentRulesDisplayNameSelection(_displayNameTextBox.SelectionStart, _displayNameTextBox.SelectionLength);
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

    private bool SaveCurrentTournamentRules()
    {
        if (!CommitNumericEdit())
        {
            return false;
        }

        if (_session.IsTournamentRulesDisplayNameEditing && !TryApplyDisplayName())
        {
            return false;
        }

        _catalog.Save(_session.CurrentTournamentRules);
        _session.MarkTournamentRulesSaved();
        GuiOperationLog.User("Saved tournament rules", $"name={_session.CurrentTournamentRules.DisplayName}; path={_catalog.ListPath}");
        return true;
    }

    private void BeginNumericEdit(TournamentRulesNumericField field)
    {
        CommitNumericEdit();
        CancelDisplayNameEdit();
        var controller = GetNumericController(field);
        var text = field switch
        {
            TournamentRulesNumericField.MainTimeHours => ((int)_session.MainTime.TotalHours).ToString("00"),
            TournamentRulesNumericField.MainTimeMinutes => _session.MainTime.Minutes.ToString("00"),
            TournamentRulesNumericField.MainTimeSeconds => _session.MainTime.Seconds.ToString("00"),
            _ => _session.MoveLimit.ToString(),
        };
        controller.Begin(text);
        _session.BeginTournamentRulesNumericEdit(field, controller.Text, controller.CaretIndex);
        _session.SetTournamentRulesDisplayNameWarning("");
    }

    private void BeginOrMoveNumericEdit(
        Point point,
        TournamentRulesNumericField field,
        Func<Point, TournamentRulesNumericField, string, int>? getCaretIndex)
    {
        if (_session.ActiveTournamentRulesNumericField == field)
        {
            var activeController = GetNumericController(field);
            var caret = getCaretIndex?.Invoke(point, field, activeController.Text) ?? activeController.Text.Length;
            activeController.BeginMouseSelection(caret, IsShiftDown());
            SyncNumericDraft(field);
            return;
        }

        BeginNumericEdit(field);
        var controller = GetNumericController(field);
        var caretIndex = getCaretIndex?.Invoke(point, field, controller.Text) ?? controller.Text.Length;
        controller.SetCaretIndex(caretIndex);
        controller.BeginMouseSelection(caretIndex, extendSelection: false);
        SyncNumericDraft(field);
    }

    public void UpdateMouseSelection(
        Point point,
        Func<Point, string, int> getDisplayNameCaretIndex,
        Func<Point, TournamentRulesNumericField, string, int> getNumericCaretIndex)
    {
        if (_displayNameTextBox.IsMouseSelecting)
        {
            _displayNameTextBox.UpdateMouseSelection(getDisplayNameCaretIndex(point, _displayNameTextBox.Text));
            SyncDisplayNameDraft();
        }
        else if (_session.ActiveTournamentRulesNumericField is { } field)
        {
            var controller = GetNumericController(field);
            if (controller.IsMouseSelecting)
            {
                controller.UpdateMouseSelection(getNumericCaretIndex(point, field, controller.Text));
                SyncNumericDraft(field);
            }
        }
    }

    public void EndMouseSelection()
    {
        _displayNameTextBox.EndMouseSelection();
        _mainTimeHoursTextBox.EndMouseSelection();
        _mainTimeMinutesTextBox.EndMouseSelection();
        _mainTimeSecondsTextBox.EndMouseSelection();
        _moveLimitTextBox.EndMouseSelection();
    }

    private static bool IsShiftDown()
    {
        var keyboard = Keyboard.GetState();
        return keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
    }

    private static bool IsShiftDown(KeyboardState keyboard) =>
        keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

    private void MoveTextBoxFocus(int step)
    {
        var currentIndex = _session.IsTournamentRulesDisplayNameEditing
            ? 0
            : _session.ActiveTournamentRulesNumericField switch
            {
                TournamentRulesNumericField.MainTimeHours => 1,
                TournamentRulesNumericField.MainTimeMinutes => 2,
                TournamentRulesNumericField.MainTimeSeconds => 3,
                _ => step > 0 ? -1 : 0,
            };

        if (_session.IsTournamentRulesDisplayNameEditing)
        {
            if (!TryApplyDisplayName())
            {
                return;
            }

            _session.EndTournamentRulesDisplayNameEdit();
            _displayNameTextBox.Clear();
        }
        else if (!CommitNumericEdit())
        {
            return;
        }

        var nextIndex = (currentIndex + step + 4) % 4;
        switch (nextIndex)
        {
            case 0:
                BeginDisplayNameEdit();
                break;
            case 1:
                BeginNumericEdit(TournamentRulesNumericField.MainTimeHours);
                break;
            case 2:
                BeginNumericEdit(TournamentRulesNumericField.MainTimeMinutes);
                break;
            case 3:
                BeginNumericEdit(TournamentRulesNumericField.MainTimeSeconds);
                break;
        }
    }

    private void HandleNumericKeyboard(TournamentRulesNumericField field, KeyboardState keyboard, GameTime gameTime)
    {
        var controller = GetNumericController(field);
        switch (controller.HandleKeyboard(
                    keyboard,
                    _previousKeyboard,
                    gameTime,
                    _clipboardService,
                    pasteCharacterFilter: character =>
                        char.IsAsciiDigit(character)))
        {
            case TextBoxKeyboardAction.Commit:
                CommitNumericEdit();
                break;
            case TextBoxKeyboardAction.Cancel:
                controller.Clear();
                _session.EndTournamentRulesNumericEdit();
                _session.SetTournamentRulesDisplayNameWarning("");
                break;
            default:
                SyncNumericDraft(field);
                break;
        }
    }

    private bool CommitNumericEdit()
    {
        if (_session.ActiveTournamentRulesNumericField is not { } field)
        {
            return true;
        }

        var controller = GetNumericController(field);
        var valid = field switch
        {
            TournamentRulesNumericField.MainTimeHours => int.TryParse(controller.Text, out var hours)
                && hours is >= 0 and <= 999
                && ApplyMainTimePart(field, hours),
            TournamentRulesNumericField.MainTimeMinutes or TournamentRulesNumericField.MainTimeSeconds
                => int.TryParse(controller.Text, out var part)
                && part is >= 0 and <= 59
                && ApplyMainTimePart(field, part),
            TournamentRulesNumericField.MoveLimit => int.TryParse(controller.Text, out var moves)
                && moves is >= 0 and <= 9999
                && ApplyMoveLimit(moves),
            _ => false,
        };

        if (!valid)
        {
            _session.SetTournamentRulesDisplayNameWarning(
                field == TournamentRulesNumericField.MoveLimit
                    ? "Moves must be 0-9999."
                    : field == TournamentRulesNumericField.MainTimeHours
                        ? "Hours must be 0-999."
                        : "Minutes and seconds must be 0-59.");
            return false;
        }

        controller.Clear();
        _session.EndTournamentRulesNumericEdit();
        _session.SetTournamentRulesDisplayNameWarning("");
        return true;
    }

    private bool ApplyMainTime(int totalSeconds)
    {
        _session.SetMainTime(totalSeconds);
        return true;
    }

    private bool ApplyMoveLimit(int moveLimit)
    {
        _session.SetMoveLimit(moveLimit);
        return true;
    }

    private void SyncNumericDraft(TournamentRulesNumericField field)
    {
        var controller = GetNumericController(field);
        _session.SetTournamentRulesNumericDraft(controller.Text, controller.CaretIndex);
        _session.SetTournamentRulesNumericSelection(controller.SelectionStart, controller.SelectionLength);
    }

    private bool ApplyMainTimePart(TournamentRulesNumericField field, int value)
    {
        var time = _session.MainTime;
        var hours = field == TournamentRulesNumericField.MainTimeHours ? value : (int)time.TotalHours;
        var minutes = field == TournamentRulesNumericField.MainTimeMinutes ? value : time.Minutes;
        var seconds = field == TournamentRulesNumericField.MainTimeSeconds ? value : time.Seconds;
        return ApplyMainTime(hours * 3600 + minutes * 60 + seconds);
    }

    private TextBoxController GetNumericController(TournamentRulesNumericField field) => field switch
    {
        TournamentRulesNumericField.MainTimeHours => _mainTimeHoursTextBox,
        TournamentRulesNumericField.MainTimeMinutes => _mainTimeMinutesTextBox,
        TournamentRulesNumericField.MainTimeSeconds => _mainTimeSecondsTextBox,
        _ => _moveLimitTextBox,
    };

    private bool IsNewKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
}
