namespace KifuwarabeGo2026.Application;

using KifuwarabeGo2026.Application.Game;
using KifuwarabeGo2026.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed class GoAppSession
{
    private GoBoard _board;
    private readonly HashSet<ulong> _positionHashes = new();
    private readonly List<TournamentRules> _tournamentRules = new();
    private readonly List<GtpEngineProfile> _gtpEngineProfiles = new();
    private TournamentRules _currentTournamentRules = new();
    private GoRenParseResult? _cachedRenParseResult;
    private int _cachedRenParseBoardSize;
    private ulong _cachedRenParseHash;
    private readonly Stack<BoardEditingChange> _boardEditingUndoHistory = new();
    private readonly Stack<BoardEditingChange> _boardEditingRedoHistory = new();

    private readonly Dictionary<GoAppModeKind, GoAppMode> _modes = new()
    {
        [GoAppModeKind.Playing] = new PlayingMode(),
        [GoAppModeKind.GameOver] = new GameOverMode(),
        [GoAppModeKind.BoardEditing] = new BoardEditingMode(),
        [GoAppModeKind.Reviewing] = new ReviewingMode(),
        [GoAppModeKind.Resting] = new RestingMode(),
    };

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
        _gtpEngineProfiles.Add(new GtpEngineProfile());
        ResetPositionHistory();
    }

    public GoAppMode CurrentMode { get; private set; }

    public int BoardSize { get; private set; } = 19;

    public IReadOnlyList<TournamentRules> TournamentRulesList => _tournamentRules;

    public int SelectedTournamentRulesIndex { get; private set; }

    public bool IsTournamentRulesSelectionDialogOpen { get; private set; }

    public bool IsTournamentRulesAddPanelOpen { get; private set; }

    public bool IsTournamentRulesEditPanelMode { get; private set; }

    public bool IsTournamentRulesDeleteConfirmationOpen { get; private set; }

    public string TournamentRulesDeleteConfirmationFileName { get; private set; } = "";

    public int TournamentRulesSelectionPageIndex { get; private set; }

    public string TournamentRulesSaveMessage { get; private set; } = "";

    public string TournamentRulesDisplayNameDraft { get; private set; } = "";

    public bool IsTournamentRulesDisplayNameEditing { get; private set; }

    public int TournamentRulesDisplayNameCaretIndex { get; private set; }

    public string TournamentRulesDisplayNameWarning { get; private set; } = "";

    public string TournamentDisplayName => _currentTournamentRules.DisplayName;

    public GoRuleKind RuleKind => _currentTournamentRules.Rule;

    public decimal Komi => _currentTournamentRules.Komi;

    public TimeSpan MainTime => _currentTournamentRules.MainTime;

    public int MoveLimit => _currentTournamentRules.MoveLimit;

    public TournamentRules CurrentTournamentRules => _currentTournamentRules.Clone();

    public GoStone CurrentTurn { get; private set; } = GoStone.Black;

    public GoStone BoardEditingStone { get; private set; } = GoStone.Black;

    public bool CanUndoBoardEditing => _boardEditingUndoHistory.Count > 0;

    public bool CanRedoBoardEditing => _boardEditingRedoHistory.Count > 0;

    public int PlayedMoveCount { get; private set; }

    public int NextMoveNumber => PlayedMoveCount + 1;

    public RenParseDisplayMode RenParseDisplayMode { get; private set; }

    public bool IsRenParseDisplayEnabled => RenParseDisplayMode != RenParseDisplayMode.Off;

    public GoPlayerKind BlackPlayerKind { get; private set; } = GoPlayerKind.Human;

    public GoPlayerKind WhitePlayerKind { get; private set; } = GoPlayerKind.Computer;

    public IReadOnlyList<GtpEngineProfile> GtpEngineProfiles => _gtpEngineProfiles;

    public int SelectedBlackGtpEngineIndex { get; private set; }

    public int SelectedWhiteGtpEngineIndex { get; private set; }

    public bool IsGtpEngineSelectionDialogOpen { get; private set; }

    public GoStone GtpEngineSelectionTargetStone { get; private set; } = GoStone.Black;

    public bool IsGtpEngineDeleteConfirmationOpen { get; private set; }

    public string GtpEngineDeleteConfirmationName { get; private set; } = "";

    public bool IsGtpEngineEditPanelOpen { get; private set; }

    public bool IsGtpEngineAddPanelMode { get; private set; }

    public GtpEngineProfileEditField? ActiveGtpEngineEditField { get; private set; }

    public int GtpEngineEditCaretIndex { get; private set; }

    public string GtpEngineEditWarning { get; private set; } = "";

    public string GtpEngineEditSaveMessage { get; private set; } = "";

    public GtpEngineProfile GtpEngineEditDraft { get; private set; } = new();

    public int GtpEngineSelectionPageIndex { get; private set; }

    public GtpEngineProfile BlackGtpEngineProfile => GetGtpEngineProfile(GoStone.Black);

    public GtpEngineProfile WhiteGtpEngineProfile => GetGtpEngineProfile(GoStone.White);

    public int BlackAgehama { get; private set; }

    public int WhiteAgehama { get; private set; }

    public TimeSpan BlackElapsedTime { get; private set; }

    public TimeSpan WhiteElapsedTime { get; private set; }

    public int BlackStoneCount => _board.CountStones(GoStone.Black);

    public int WhiteStoneCount => _board.CountStones(GoStone.White);

    public GoPoint? KoPoint { get; private set; }

    public int ConsecutivePasses { get; private set; }

    public string GameOverReason { get; private set; } = "";

    public GoStone? Winner { get; private set; }

    public bool IsEngineThinking { get; private set; }

    public bool IsEngineReady { get; private set; } = true;

    public string EngineErrorMessage { get; private set; } = "";

    public string EngineLogPath { get; private set; } = "";

    public GoGameRecord CurrentGameRecord { get; private set; } = new();

    public bool CanAcceptHumanMove =>
        CurrentMode.Kind == GoAppModeKind.Playing &&
        IsEngineReady &&
        !IsEngineThinking &&
        string.IsNullOrWhiteSpace(EngineErrorMessage) &&
        GetPlayerKind(CurrentTurn) == GoPlayerKind.Human;

    public void ChangeMode(GoAppModeKind modeKind)
    {
        CurrentMode = _modes[modeKind];
    }

    public void ToggleRenParseDisplay()
    {
        RenParseDisplayMode = RenParseDisplayMode switch
        {
            RenParseDisplayMode.Off => RenParseDisplayMode.Overlay,
            RenParseDisplayMode.Overlay => RenParseDisplayMode.Graph,
            RenParseDisplayMode.Graph => RenParseDisplayMode.Eye,
            _ => RenParseDisplayMode.Off,
        };
    }

    public GoRenParseResult ParseRens()
    {
        if (_cachedRenParseResult is not null &&
            _cachedRenParseBoardSize == BoardSize &&
            _cachedRenParseHash == _board.CurrentHash)
        {
            return _cachedRenParseResult;
        }

        _cachedRenParseResult = _board.ParseRens();
        _cachedRenParseBoardSize = BoardSize;
        _cachedRenParseHash = _board.CurrentHash;
        return _cachedRenParseResult;
    }

    public void StartPlaying()
    {
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            ClearBoard();
        }

        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        ChangeMode(GoAppModeKind.Playing);
    }

    public void StartBoardEditing()
    {
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
        ChangeMode(GoAppModeKind.BoardEditing);
    }

    public void FinishBoardEditing()
    {
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ChangeMode(GoAppModeKind.Resting);
    }

    public void SetBoardEditingStone(GoStone stone)
    {
        if (stone is not (GoStone.Empty or GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Board editing stone is out of range.");
        }

        BoardEditingStone = stone;
    }

    public bool TryEditBoardStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing)
        {
            return false;
        }

        var oldStone = _board.GetStone(x, y);
        if (oldStone == BoardEditingStone || !_board.TrySetEditedStone(x, y, BoardEditingStone))
        {
            return false;
        }

        _boardEditingUndoHistory.Push(new BoardEditingChange(x, y, oldStone, BoardEditingStone));
        _boardEditingRedoHistory.Clear();
        ResetEditedPositionState();
        return true;
    }

    public bool UndoBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing || _boardEditingUndoHistory.Count == 0)
        {
            return false;
        }

        var change = _boardEditingUndoHistory.Pop();
        if (!_board.TrySetEditedStone(change.X, change.Y, change.OldStone))
        {
            return false;
        }

        _boardEditingRedoHistory.Push(change);
        ResetEditedPositionState();
        return true;
    }

    public bool RedoBoardEditing()
    {
        if (CurrentMode.Kind != GoAppModeKind.BoardEditing || _boardEditingRedoHistory.Count == 0)
        {
            return false;
        }

        var change = _boardEditingRedoHistory.Pop();
        if (!_board.TrySetEditedStone(change.X, change.Y, change.NewStone))
        {
            return false;
        }

        _boardEditingUndoHistory.Push(change);
        ResetEditedPositionState();
        return true;
    }

    public void CancelPlaying()
    {
        ChangeMode(GoAppModeKind.Resting);
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
    }

    public void ReturnToSetup()
    {
        ClearBoard();
        ChangeMode(GoAppModeKind.Resting);
    }

    public void ChangeBoardSize(int boardSize)
    {
        if (boardSize is not (9 or 13 or 19))
        {
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");
        }

        if (BoardSize == boardSize)
        {
            return;
        }

        BoardSize = boardSize;
        _currentTournamentRules.BoardSize = boardSize;
        TournamentRulesSaveMessage = "UNSAVED";
        ClearBoard();
    }

    public void SetTournamentRules(IEnumerable<TournamentRules> rules)
    {
        _tournamentRules.Clear();
        _tournamentRules.AddRange(rules.Select(rule => rule.Clone()));
        if (_tournamentRules.Count == 0)
        {
            _tournamentRules.Add(new TournamentRules());
        }

        SelectTournamentRules(0);
    }

    public void SelectTournamentRules(int index)
    {
        if (index < 0 || index >= _tournamentRules.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "Tournament rules index is out of range.");
        }

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

    public void OpenTournamentRulesSelectionDialog()
    {
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesSelectionDialogOpen = true;
        TournamentRulesSelectionPageIndex = SelectedTournamentRulesIndex / TournamentRulesSelectionPageSize;
    }

    public void CloseTournamentRulesSelectionDialog()
    {
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
        SelectedTournamentRulesIndex >= 0 &&
        SelectedTournamentRulesIndex < _tournamentRules.Count;

    public void OpenTournamentRulesDeleteConfirmation()
    {
        if (!CanDeleteSelectedTournamentRules)
        {
            return;
        }

        var path = _tournamentRules[SelectedTournamentRulesIndex].FilePath;
        TournamentRulesDeleteConfirmationFileName = string.IsNullOrWhiteSpace(path)
            ? _tournamentRules[SelectedTournamentRulesIndex].DisplayName
            : Path.GetFileName(path);
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
        {
            return;
        }

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

    public void ChangeRuleKind(GoRuleKind ruleKind)
    {
        _currentTournamentRules.Rule = ruleKind;
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeKomi(decimal step)
    {
        _currentTournamentRules.Komi = Math.Clamp(_currentTournamentRules.Komi + step, -99.5m, 99.5m);
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeMainTime(TimeSpan step)
    {
        var totalSeconds = Math.Max(0, (int)(_currentTournamentRules.MainTime + step).TotalSeconds);
        _currentTournamentRules.MainTimeMinutes = totalSeconds / 60;
        _currentTournamentRules.MainTimeSeconds = totalSeconds % 60;
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeMoveLimit(int step)
    {
        _currentTournamentRules.MoveLimit = Math.Clamp(_currentTournamentRules.MoveLimit + step, 0, 9999);
        TournamentRulesSaveMessage = "UNSAVED";
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
        {
            _tournamentRules[SelectedTournamentRulesIndex] = _currentTournamentRules.Clone();
        }

        TournamentRulesSaveMessage = "SAVED";
    }

    public void ReplaceCurrentTournamentRules(TournamentRules rules)
    {
        ApplyTournamentRules(rules);
        if (SelectedTournamentRulesIndex >= 0 && SelectedTournamentRulesIndex < _tournamentRules.Count)
        {
            _tournamentRules[SelectedTournamentRulesIndex] = _currentTournamentRules.Clone();
        }
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

    public void SetTournamentRulesDisplayNameWarning(string warning)
    {
        TournamentRulesDisplayNameWarning = warning;
    }

    public void SetPlayerKind(GoStone stone, GoPlayerKind playerKind)
    {
        if (stone == GoStone.Black)
        {
            BlackPlayerKind = playerKind;
            return;
        }

        if (stone == GoStone.White)
        {
            WhitePlayerKind = playerKind;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player kind can be set only for black or white.");
    }

    public void SetGtpEngineProfiles(IEnumerable<GtpEngineProfile> profiles)
    {
        _gtpEngineProfiles.Clear();
        _gtpEngineProfiles.AddRange(profiles.Select(profile => profile.Clone()));
        if (_gtpEngineProfiles.Count == 0)
        {
            _gtpEngineProfiles.Add(new GtpEngineProfile());
        }

        SelectedBlackGtpEngineIndex = 0;
        SelectedWhiteGtpEngineIndex = 0;
    }

    public void SelectGtpEngine(GoStone stone, int index)
    {
        if (index < 0 || index >= _gtpEngineProfiles.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "GTP engine index is out of range.");
        }

        if (stone == GoStone.Black)
        {
            SelectedBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be selected only for black or white.");
    }

    public void OpenGtpEngineSelectionDialog(GoStone stone)
    {
        if (stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be selected only for black or white.");
        }

        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineEditPanelOpen = false;
        IsGtpEngineAddPanelMode = false;
        IsGtpEngineSelectionDialogOpen = true;
        IsGtpEngineDeleteConfirmationOpen = false;
        GtpEngineSelectionTargetStone = stone;
        var selectedIndex = stone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex;
        GtpEngineSelectionPageIndex = selectedIndex / GtpEngineSelectionPageSize;
    }

    public void CloseGtpEngineSelectionDialog()
    {
        IsGtpEngineSelectionDialogOpen = false;
        CloseGtpEngineDeleteConfirmation();
    }

    public void OpenGtpEngineEditPanel()
    {
        var index = SelectedGtpEngineIndex;
        if (index < 0 || index >= _gtpEngineProfiles.Count)
        {
            return;
        }

        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = true;
        IsGtpEngineAddPanelMode = false;
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
        GtpEngineEditDraft = new GtpEngineProfile
        {
            DisplayName = "New GTP Engine",
        };
        ActiveGtpEngineEditField = null;
        GtpEngineEditCaretIndex = 0;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
    }

    public void OpenGtpEngineDuplicatePanel()
    {
        var index = SelectedGtpEngineIndex;
        if (index < 0 || index >= _gtpEngineProfiles.Count)
        {
            return;
        }

        IsTournamentRulesSelectionDialogOpen = false;
        IsTournamentRulesAddPanelOpen = false;
        IsTournamentRulesDeleteConfirmationOpen = false;
        IsGtpEngineSelectionDialogOpen = false;
        IsGtpEngineEditPanelOpen = true;
        IsGtpEngineAddPanelMode = true;
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
        IsGtpEngineEditPanelOpen = false;
        IsGtpEngineAddPanelMode = false;
        ActiveGtpEngineEditField = null;
        GtpEngineEditWarning = "";
        GtpEngineEditSaveMessage = "";
        OpenGtpEngineSelectionDialog(GtpEngineSelectionTargetStone);
    }

    public void MoveGtpEngineSelectionPage(int step)
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(_gtpEngineProfiles.Count / (double)GtpEngineSelectionPageSize));
        GtpEngineSelectionPageIndex = Math.Clamp(GtpEngineSelectionPageIndex + step, 0, pageCount - 1);
    }

    public bool CanDeleteSelectedGtpEngine =>
        _gtpEngineProfiles.Count > 1 &&
        SelectedGtpEngineIndex >= 0 &&
        SelectedGtpEngineIndex < _gtpEngineProfiles.Count;

    public int SelectedGtpEngineIndex =>
        GtpEngineSelectionTargetStone == GoStone.Black ? SelectedBlackGtpEngineIndex : SelectedWhiteGtpEngineIndex;

    public void ReplaceSelectedGtpEngine(GtpEngineProfile profile)
    {
        var index = SelectedGtpEngineIndex;
        if (index < 0 || index >= _gtpEngineProfiles.Count)
        {
            return;
        }

        _gtpEngineProfiles[index] = profile.Clone();
    }

    public void SetGtpEngineEditField(GtpEngineProfileEditField field, string text, int caretIndex)
    {
        switch (field)
        {
            case GtpEngineProfileEditField.DisplayName:
                GtpEngineEditDraft.DisplayName = text;
                break;
            case GtpEngineProfileEditField.ExecutablePath:
                GtpEngineEditDraft.ExecutablePath = text;
                break;
            case GtpEngineProfileEditField.WorkingDirectory:
                GtpEngineEditDraft.WorkingDirectory = text;
                break;
            case GtpEngineProfileEditField.Arguments:
                GtpEngineEditDraft.Arguments = text;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, "GTP engine edit field is out of range.");
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

    public void EndGtpEngineEditField()
    {
        ActiveGtpEngineEditField = null;
    }

    public void SetGtpEngineEditWarning(string warning)
    {
        GtpEngineEditWarning = warning;
    }

    public void SetGtpEngineExecutablePathDraft(string executablePath)
    {
        GtpEngineEditDraft.ExecutablePath = executablePath;
        GtpEngineEditDraft.WorkingDirectory = Path.GetDirectoryName(executablePath) ?? GtpEngineEditDraft.WorkingDirectory;
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void SetGtpEngineWorkingDirectoryDraft(string workingDirectory)
    {
        GtpEngineEditDraft.WorkingDirectory = workingDirectory;
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void ToggleGtpEngineEditLog()
    {
        GtpEngineEditDraft.EnableGtpLog = !GtpEngineEditDraft.EnableGtpLog;
        GtpEngineEditSaveMessage = "UNSAVED";
    }

    public void SaveGtpEngineEditDraft(GtpEngineProfile profile)
    {
        if (IsGtpEngineAddPanelMode)
        {
            _gtpEngineProfiles.Add(profile.Clone());
            SelectGtpEngine(GtpEngineSelectionTargetStone, _gtpEngineProfiles.Count - 1);
            GtpEngineSelectionPageIndex = (_gtpEngineProfiles.Count - 1) / GtpEngineSelectionPageSize;
            IsGtpEngineAddPanelMode = false;
        }
        else
        {
            ReplaceSelectedGtpEngine(profile);
        }

        GtpEngineEditDraft = _gtpEngineProfiles[SelectedGtpEngineIndex].Clone();
        GtpEngineEditSaveMessage = "SAVED";
        GtpEngineEditWarning = "";
    }

    public void OpenGtpEngineDeleteConfirmation()
    {
        if (!CanDeleteSelectedGtpEngine)
        {
            return;
        }

        GtpEngineDeleteConfirmationName = _gtpEngineProfiles[SelectedGtpEngineIndex].DisplayName;
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
        {
            return;
        }

        var removedIndex = SelectedGtpEngineIndex;
        var nextIndex = Math.Clamp(removedIndex, 0, _gtpEngineProfiles.Count - 2);
        _gtpEngineProfiles.RemoveAt(removedIndex);
        SelectedBlackGtpEngineIndex = AdjustGtpEngineSelectionAfterDelete(SelectedBlackGtpEngineIndex, removedIndex, nextIndex);
        SelectedWhiteGtpEngineIndex = AdjustGtpEngineSelectionAfterDelete(SelectedWhiteGtpEngineIndex, removedIndex, nextIndex);
        CloseGtpEngineDeleteConfirmation();
        GtpEngineSelectionPageIndex = Math.Clamp(
            nextIndex / GtpEngineSelectionPageSize,
            0,
            Math.Max(0, (int)Math.Ceiling(_gtpEngineProfiles.Count / (double)GtpEngineSelectionPageSize) - 1));
    }

    public string GetGtpEngineEditFieldText(GtpEngineProfileEditField field) => field switch
    {
        GtpEngineProfileEditField.DisplayName => GtpEngineEditDraft.DisplayName,
        GtpEngineProfileEditField.ExecutablePath => GtpEngineEditDraft.ExecutablePath,
        GtpEngineProfileEditField.WorkingDirectory => GtpEngineEditDraft.WorkingDirectory,
        GtpEngineProfileEditField.Arguments => GtpEngineEditDraft.Arguments,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "GTP engine edit field is out of range."),
    };

    public GtpEngineProfile GetGtpEngineProfile(GoStone stone)
    {
        var index = stone switch
        {
            GoStone.Black => SelectedBlackGtpEngineIndex,
            GoStone.White => SelectedWhiteGtpEngineIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "GTP engine can be read only for black or white."),
        };

        return _gtpEngineProfiles[Math.Clamp(index, 0, _gtpEngineProfiles.Count - 1)].Clone();
    }

    public GoPlayerKind GetPlayerKind(GoStone stone) => stone switch
    {
        GoStone.Black => BlackPlayerKind,
        GoStone.White => WhitePlayerKind,
        _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "Player kind can be read only for black or white."),
    };

    public void SetEngineThinking(bool isThinking)
    {
        IsEngineThinking = isThinking;
    }

    public void SetEngineReady(bool isReady)
    {
        IsEngineReady = isReady;
    }

    public void SetEngineLogPath(string path)
    {
        EngineLogPath = path;
    }

    public void ClearEngineError()
    {
        EngineErrorMessage = "";
    }

    public void SetEngineError(string message)
    {
        EngineErrorMessage = message;
        IsEngineThinking = false;
    }

    public void AddCurrentTurnElapsedTime(TimeSpan elapsed)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing ||
            !IsEngineReady ||
            !string.IsNullOrWhiteSpace(EngineErrorMessage))
        {
            return;
        }

        if (CurrentTurn == GoStone.Black)
        {
            BlackElapsedTime += elapsed;
            return;
        }

        WhiteElapsedTime += elapsed;
    }

    public GoStone GetStone(int x, int y)
    {
        return _board.GetStone(x, y);
    }

    public bool IsSuperKoPoint(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        var trialBoard = _board.Clone();
        return trialBoard.TryPlaceStone(x, y, CurrentTurn, KoPoint, out _, out _) &&
            _positionHashes.Contains(trialBoard.CurrentHash);
    }

    public IEnumerable<GoPoint> EnumerateSuperKoPoints()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            yield break;
        }

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                if (IsSuperKoPoint(x, y))
                {
                    yield return new GoPoint(x, y);
                }
            }
        }
    }

    /// <summary>
    /// 石を置けるか試すぜ（＾▽＾）
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public bool TryPlaceStone(int x, int y)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || !_board.TryPlaceStone(x, y, CurrentTurn, KoPoint, out var capturedStones, out var nextKoPoint))
        {
            return false;
        }

        var placedBy = CurrentTurn;
        CurrentGameRecord.Moves.Add(new GoGameMove(placedBy, new GoPoint(x, y)));
        if (CurrentTurn == GoStone.Black)
        {
            BlackAgehama += capturedStones;
        }
        else
        {
            WhiteAgehama += capturedStones;
        }

        if (_positionHashes.Contains(_board.CurrentHash))
        {
            KoPoint = null;
            ConsecutivePasses = 0;
            Winner = OppositeOf(placedBy);
            GameOverReason = $"{StoneName(placedBy)} SUPER KO LOSS";
            ChangeMode(GoAppModeKind.GameOver);
            return true;
        }

        _positionHashes.Add(_board.CurrentHash);
        KoPoint = nextKoPoint;
        ConsecutivePasses = 0;
        CompleteMoveAndPassTurn();
        return true;
    }

    public bool Pass()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        KoPoint = null;
        ConsecutivePasses++;
        CurrentGameRecord.Moves.Add(new GoGameMove(CurrentTurn, null));
        CompleteMoveAndPassTurn();
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            return true;
        }

        if (ConsecutivePasses >= 2)
        {
            DecidePureGoResult();
            ChangeMode(GoAppModeKind.GameOver);
        }

        return true;
    }

    public bool Resign()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        var resigned = CurrentTurn;
        Winner = OppositeOf(resigned);
        KoPoint = null;
        ConsecutivePasses = 0;
        GameOverReason = $"{StoneName(resigned)} RESIGNS";
        ChangeMode(GoAppModeKind.GameOver);
        return true;
    }

    public bool LoadGameRecordAsInitialPosition(GoGameRecord record, out string warning)
    {
        ArgumentNullException.ThrowIfNull(record);

        var loadedBoard = new GoBoard(record.BoardSize);
        foreach (var setupStone in record.SetupStones)
        {
            if (!loadedBoard.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"Invalid SGF setup stone at {setupStone.Point.X + 1},{setupStone.Point.Y + 1}.";
                return false;
            }
        }

        GoPoint? replayKoPoint = null;
        foreach (var move in record.Moves)
        {
            if (move.Point is not { } point)
            {
                replayKoPoint = null;
                continue;
            }

            if (!loadedBoard.TryPlaceStone(point.X, point.Y, move.Stone, replayKoPoint, out _, out var nextKoPoint))
            {
                warning = $"Illegal SGF move at {point.X + 1},{point.Y + 1}.";
                return false;
            }

            replayKoPoint = nextKoPoint;
        }

        BoardSize = record.BoardSize;
        _currentTournamentRules.BoardSize = BoardSize;
        _currentTournamentRules.Komi = record.Komi;
        TournamentRulesSaveMessage = "UNSAVED";
        _board = loadedBoard;
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
        ChangeMode(GoAppModeKind.Resting);
        warning = "";
        return true;
    }

    private void ClearBoard()
    {
        _board = new GoBoard(BoardSize);
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
        ClearBoardEditingHistory();
    }

    private void ApplyTournamentRules(TournamentRules rules)
    {
        _currentTournamentRules = rules.Clone();
        BoardSize = _currentTournamentRules.BoardSize is 9 or 13 or 19 ? _currentTournamentRules.BoardSize : 19;
        _currentTournamentRules.BoardSize = BoardSize;
        ClearBoard();
    }

    public const int TournamentRulesSelectionPageSize = 6;

    public const int GtpEngineSelectionPageSize = 6;

    private void CompleteMoveAndPassTurn()
    {
        PlayedMoveCount++;
        if (MoveLimit > 0 && PlayedMoveCount >= MoveLimit)
        {
            KoPoint = null;
            DecidePureGoResult();
            ChangeMode(GoAppModeKind.GameOver);
            return;
        }

        PassTurn();
    }

    private void PassTurn()
    {
        CurrentTurn = CurrentTurn == GoStone.Black ? GoStone.White : GoStone.Black;
    }

    private void ResetPositionHistory()
    {
        _positionHashes.Clear();
        _positionHashes.Add(_board.CurrentHash);
    }

    private void ResetEditedPositionState()
    {
        KoPoint = null;
        ConsecutivePasses = 0;
        PlayedMoveCount = 0;
        CurrentTurn = GoStone.Black;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        ResetPositionHistory();
    }

    private void ClearBoardEditingHistory()
    {
        _boardEditingUndoHistory.Clear();
        _boardEditingRedoHistory.Clear();
    }

    private GoGameRecord CreateGameRecordFromCurrentPosition()
    {
        var record = new GoGameRecord
        {
            GameName = "Kifuwarabe Go 2026",
            RuleName = RuleKind.ToString(),
            BoardSize = BoardSize,
            Komi = Komi,
        };

        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var stone = _board.GetStone(x, y);
                if (stone != GoStone.Empty)
                {
                    record.SetupStones.Add(new GoGameSetupStone(stone, new GoPoint(x, y)));
                }
            }
        }

        return record;
    }

    private void DecidePureGoResult()
    {
        var blackStones = BlackStoneCount;
        var whiteStones = WhiteStoneCount;
        Winner = blackStones == whiteStones ? null : blackStones > whiteStones ? GoStone.Black : GoStone.White;
        GameOverReason = Winner is null ? "PURE GO DRAW" : $"PURE GO {StoneName(Winner.Value)} +{Math.Abs(blackStones - whiteStones)}";
    }

    private static GoStone OppositeOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private static string StoneName(GoStone stone) => stone == GoStone.Black ? "BLACK" : "WHITE";

    private static int AdjustGtpEngineSelectionAfterDelete(int selectedIndex, int removedIndex, int fallbackIndex)
    {
        if (selectedIndex == removedIndex)
        {
            return fallbackIndex;
        }

        return selectedIndex > removedIndex ? selectedIndex - 1 : selectedIndex;
    }

    private readonly record struct BoardEditingChange(int X, int Y, GoStone OldStone, GoStone NewStone);
}
