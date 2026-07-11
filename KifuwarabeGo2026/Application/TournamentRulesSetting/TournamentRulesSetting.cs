namespace KifuwarabeGo2026.Application.TournamentRulesSetting;

using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

/// <summary>
/// ［大会ルール設定］画面の処理
/// </summary>
public sealed class TournamentRulesSetting
{
    private const int MaxFileNameLength = 255;

    private readonly GoAppSession _session;
    private readonly TournamentRulesCatalog _catalog;
    private readonly Action _browseTournamentRules;
    private readonly TextBoxController _fileNameTextBox = new(MaxFileNameLength);
    private KeyboardState _previousKeyboard;

    public TournamentRulesSetting(GoAppSession session, TournamentRulesCatalog catalog, Action browseTournamentRules)
    {
        _session = session;
        _catalog = catalog;
        _browseTournamentRules = browseTournamentRules;
    }

    public void UpdateByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!_session.IsTournamentRulesAddPanelOpen)
        {
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.IsTournamentRulesFileNameEditing)
        {
            HandleFileNameKeyboard(keyboard, gameTime);
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
        if (!_session.IsTournamentRulesAddPanelOpen || !_session.IsTournamentRulesFileNameEditing)
        {
            return false;
        }

        if (!_fileNameTextBox.TryInputCharacter(character))
        {
            _session.SetTournamentRulesFileNameWarning("File name is too long.");
            return true;
        }

        SyncFileNameDraft();
        UpdateFileNameWarning();
        return true;
    }

    public bool TryHandleMouseClick(Point point)
    {
        if (_session.IsTournamentRulesSelectionDialogOpen)
        {
            return TryHandleTournamentRulesSelectionDialogClick(point);
        }

        if (_session.IsTournamentRulesAddPanelOpen)
        {
            return TryHandleTournamentRulesAddPanelClick(point);
        }

        if (GoScreenRenderer.GetTournamentRulesBrowseButtonHit(point))
        {
            _browseTournamentRules();
            return true;
        }

        return false;
    }

    private bool TryHandleTournamentRulesAddPanelClick(Point point)
    {
        if (GoScreenRenderer.GetTournamentRulesAddPanelCloseButtonHit(point))
        {
            CancelFileNameEdit();
            _session.CloseTournamentRulesAddPanel();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesAddPanelFileNameBoxHit(point))
        {
            BeginFileNameEdit();
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
        if (GoScreenRenderer.TryGetTournamentRulesSelectionDialogPathCopyText(point, _session, out var path))
        {
            SystemClipboard.SetText(path);
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogCloseButtonHit(point))
        {
            _session.CloseTournamentRulesSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetTournamentRulesSelectionDialogAddButtonHit(point))
        {
            CreateNewTournamentRules();
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
            _session.SelectTournamentRules(index);
            return true;
        }

        return true;
    }

    private void CreateNewTournamentRules()
    {
        var rules = _catalog.CreateNew(_session.CurrentTournamentRules);
        _session.AddAndSelectTournamentRules(rules);
        _session.OpenTournamentRulesAddPanel();
        BeginFileNameEdit();
        _session.MarkTournamentRulesSaved();
    }

    private void BeginFileNameEdit()
    {
        var fileName = string.IsNullOrWhiteSpace(_session.CurrentTournamentRules.FilePath)
            ? ""
            : System.IO.Path.GetFileName(_session.CurrentTournamentRules.FilePath);
        _fileNameTextBox.Begin(fileName);
        SyncFileNameDraft();
        _session.BeginTournamentRulesFileNameEdit();
        _session.SetTournamentRulesFileNameDraft(_fileNameTextBox.Text, _fileNameTextBox.CaretIndex);
        UpdateFileNameWarning();
    }

    private void HandleFileNameKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        switch (_fileNameTextBox.HandleKeyboard(keyboard, _previousKeyboard, gameTime))
        {
            case TextBoxKeyboardAction.Commit:
                CommitFileNameEdit();
                break;
            case TextBoxKeyboardAction.Cancel:
                CancelFileNameEdit();
                break;
            default:
                SyncFileNameDraft();
                UpdateFileNameWarning();
                break;
        }
    }

    private void CommitFileNameEdit()
    {
        if (!TryApplyFileName())
        {
            return;
        }

        _session.EndTournamentRulesFileNameEdit();
        _fileNameTextBox.Clear();
    }

    private void CancelFileNameEdit()
    {
        if (!_session.IsTournamentRulesFileNameEditing)
        {
            return;
        }

        _session.EndTournamentRulesFileNameEdit();
        _fileNameTextBox.Clear();
    }

    private bool TryApplyFileName()
    {
        var rules = _session.CurrentTournamentRules;
        if (!_catalog.TryValidateFileName(rules, _fileNameTextBox.Text, out _, out var warning))
        {
            _session.SetTournamentRulesFileNameWarning(warning);
            return false;
        }

        try
        {
            var savedRules = _catalog.SaveAsFileName(rules, _fileNameTextBox.Text);
            _session.ReplaceCurrentTournamentRules(savedRules);
            _session.SetTournamentRulesFileNameDraft(System.IO.Path.GetFileName(savedRules.FilePath), System.IO.Path.GetFileName(savedRules.FilePath).Length);
            _session.MarkTournamentRulesSaved();
            return true;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or NotSupportedException)
        {
            _session.SetTournamentRulesFileNameWarning("File name could not be saved.");
            return false;
        }
    }

    private void UpdateFileNameWarning()
    {
        _catalog.TryValidateFileName(_session.CurrentTournamentRules, _fileNameTextBox.Text, out _, out var warning);
        _session.SetTournamentRulesFileNameWarning(warning);
    }

    private void SyncFileNameDraft()
    {
        _session.SetTournamentRulesFileNameDraft(_fileNameTextBox.Text, _fileNameTextBox.CaretIndex);
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
        if (_session.IsTournamentRulesFileNameEditing && !TryApplyFileName())
        {
            return;
        }

        _catalog.Save(_session.CurrentTournamentRules);
        _session.MarkTournamentRulesSaved();
    }

    private bool IsNewKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
}
