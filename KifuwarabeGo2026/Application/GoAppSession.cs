namespace KifuwarabeGo2026.Application;

using KifuwarabeGo2026.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Application.Local.Playing;
using KifuwarabeGo2026.Application.Local.Resting;
using KifuwarabeGo2026.Application.Local.Resting.TournamentRule;
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
    private readonly List<CgosConnectionProfile> _cgosConnectionProfiles = new();
    private CgosConnectionProfile _cgosConnectionEditSource = CreateDefaultCgosConnectionProfile();
    private TournamentRules _currentTournamentRules = new();
    private GoRenParseResult? _cachedRenParseResult;
    private int _cachedRenParseBoardSize;
    private ulong _cachedRenParseHash;
    private readonly Stack<BoardEditingChange> _boardEditingUndoHistory = new();
    private readonly Stack<BoardEditingChange> _boardEditingRedoHistory = new();
    private GoGameRecord? _reviewGameRecord;
    private DateTime? _cgosBlackConnectionStartedAt;
    private DateTime? _cgosWhiteConnectionStartedAt;

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

    public GoAppUseKind? UseKind { get; private set; }

    public IReadOnlyList<CgosConnectionProfile> CgosConnectionProfiles => _cgosConnectionProfiles;

    public CgosConnectionFlowKind CgosConnectionFlowKind { get; private set; }

    public string CgosConnectionStatusMessage { get; private set; } = "READY";

    public string CgosConnectionLogDirectory { get; private set; } = "";

    public IReadOnlyList<string> CgosConnectionRecentOutput { get; private set; } = Array.Empty<string>();

    public string CgosBlackConnectionStatusMessage { get; private set; } = "READY";

    public string CgosBlackConnectionLogDirectory { get; private set; } = "";

    public IReadOnlyList<string> CgosBlackConnectionRecentOutput { get; private set; } = Array.Empty<string>();

    public bool IsCgosBlackConnectionRunning { get; private set; }

    public string CgosBlackConnectionElapsedDisplay =>
        FormatCgosConnectionElapsedDisplay(_cgosBlackConnectionStartedAt, IsCgosBlackConnectionRunning);

    public string CgosWhiteConnectionStatusMessage { get; private set; } = "READY";

    public string CgosWhiteConnectionLogDirectory { get; private set; } = "";

    public IReadOnlyList<string> CgosWhiteConnectionRecentOutput { get; private set; } = Array.Empty<string>();

    public bool IsCgosWhiteConnectionRunning { get; private set; }

    public string CgosWhiteConnectionElapsedDisplay =>
        FormatCgosConnectionElapsedDisplay(_cgosWhiteConnectionStartedAt, IsCgosWhiteConnectionRunning);

    public string CgosAdminStatusMessage { get; private set; } = "ADMIN READY";

    public string CgosAdminLogDirectory { get; private set; } = "";

    public IReadOnlyList<string> CgosAdminRecentOutput { get; private set; } = Array.Empty<string>();

    public bool IsCgosAdminRunning { get; private set; }

    public int? SelectedCgosBlackGtpEngineIndex { get; private set; } = 0;

    public int? SelectedCgosWhiteGtpEngineIndex { get; private set; } = 0;

    public GtpEngineProfile? SelectedCgosBlackGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosBlackGtpEngineIndex);

    public GtpEngineProfile? SelectedCgosWhiteGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosWhiteGtpEngineIndex);

    public bool HasSelectedCgosGtpEngine => SelectedCgosBlackGtpEngineProfile is not null || SelectedCgosWhiteGtpEngineProfile is not null;

    public bool IsAnyCgosProcessRunning => IsCgosConnectionRunning || IsCgosBlackConnectionRunning || IsCgosWhiteConnectionRunning || IsCgosAdminRunning;

    public bool IsCgosConnectionRunning { get; private set; }

    public int SelectedCgosConnectionProfileIndex { get; private set; }

    public CgosConnectionProfile SelectedCgosConnectionProfile => _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex];

    public bool IsCgosConnectionEditPanelOpen { get; private set; }

    public bool IsCgosConnectionAddPanelMode { get; private set; }

    public CgosConnectionProfileEditField? ActiveCgosConnectionEditField { get; private set; }

    public int CgosConnectionEditCaretIndex { get; private set; }

    public CgosConnectionProfile CgosConnectionEditDraft { get; private set; } = CreateDefaultCgosConnectionProfile();

    public string CgosConnectionPortDraft { get; private set; } = "6809";

    public string CgosConnectionEditWarning { get; private set; } = "";

    public string CgosConnectionEditSaveMessage { get; private set; } = "";

    public int CgosConnectionSelectionPageIndex { get; private set; }

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

    public bool HasReviewGameRecord => _reviewGameRecord is not null;

    public int ReviewMoveIndex { get; private set; }

    public int ReviewMoveCount => _reviewGameRecord?.Moves.Count ?? 0;

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

    public void SelectUseKind(GoAppUseKind useKind)
    {
        UseKind = useKind;
    }

    public void ReturnToUseSelection()
    {
        UseKind = null;
        CgosConnectionFlowKind = CgosConnectionFlowKind.ProfileSelection;
        CgosConnectionStatusMessage = "READY";
        CgosConnectionLogDirectory = "";
        CgosConnectionRecentOutput = Array.Empty<string>();
        IsCgosConnectionRunning = false;
        CgosBlackConnectionStatusMessage = "READY";
        CgosBlackConnectionLogDirectory = "";
        CgosBlackConnectionRecentOutput = Array.Empty<string>();
        IsCgosBlackConnectionRunning = false;
        CgosWhiteConnectionStatusMessage = "READY";
        CgosWhiteConnectionLogDirectory = "";
        CgosWhiteConnectionRecentOutput = Array.Empty<string>();
        IsCgosWhiteConnectionRunning = false;
        CgosAdminStatusMessage = "ADMIN READY";
        CgosAdminLogDirectory = "";
        CgosAdminRecentOutput = Array.Empty<string>();
        IsCgosAdminRunning = false;
        CloseCgosConnectionEditPanel();
    }

    public void SelectCgosConnectionProfile(int index)
    {
        if (index < 0 || index >= _cgosConnectionProfiles.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "CGOS connection profile index is out of range.");
        }

        SelectedCgosConnectionProfileIndex = index;
        CgosConnectionSelectionPageIndex = index / CgosConnectionSelectionPageSize;
        CgosConnectionStatusMessage = "READY";
    }

    public void OpenCgosConnectionStartScreen()
    {
        if (_cgosConnectionProfiles.Count == 0)
        {
            return;
        }

        CloseCgosConnectionEditPanel();
        CgosConnectionFlowKind = CgosConnectionFlowKind.ConnectionStart;
        CgosConnectionStatusMessage = "READY";
    }

    public void ReturnToCgosConnectionProfiles()
    {
        CgosConnectionFlowKind = CgosConnectionFlowKind.ProfileSelection;
        CgosConnectionStatusMessage = "READY";
    }

    public void RequestCgosConnectionStart()
    {
        if (CgosConnectionFlowKind != CgosConnectionFlowKind.ConnectionStart)
        {
            return;
        }

        CgosConnectionStatusMessage = "CONNECT REQUESTED";
    }

    public void MoveCgosGtpEngineSelection(GoStone stone, int step)
    {
        if (IsAnyCgosProcessRunning || _gtpEngineProfiles.Count == 0)
        {
            return;
        }

        var selectedIndex = GetSelectedCgosGtpEngineIndex(stone);
        var baseIndex = selectedIndex ?? (step < 0 ? _gtpEngineProfiles.Count : -1);
        SetSelectedCgosGtpEngineIndex(stone, Math.Clamp(baseIndex + step, 0, _gtpEngineProfiles.Count - 1));
        CgosConnectionStatusMessage = "READY";
    }

    public bool CanMoveCgosGtpEngineSelection(GoStone stone, int step)
    {
        if (IsAnyCgosProcessRunning || _gtpEngineProfiles.Count == 0)
        {
            return false;
        }

        var selectedIndex = GetSelectedCgosGtpEngineIndex(stone);
        var baseIndex = selectedIndex ?? (step < 0 ? _gtpEngineProfiles.Count : -1);
        return Math.Clamp(baseIndex + step, 0, _gtpEngineProfiles.Count - 1) != selectedIndex;
    }

    public void ClearCgosGtpEngineSelection(GoStone stone)
    {
        if (IsAnyCgosProcessRunning)
        {
            return;
        }

        SetSelectedCgosGtpEngineIndex(stone, null);
        CgosConnectionStatusMessage = HasSelectedCgosGtpEngine ? "READY" : "SELECT ENGINE";
    }

    public bool CanClearCgosGtpEngineSelection(GoStone stone) =>
        !IsAnyCgosProcessRunning &&
        GetSelectedCgosGtpEngineIndex(stone) is not null;

    public void SetCgosConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        CgosConnectionStatusMessage = statusMessage;
        IsCgosConnectionRunning = isRunning;
        CgosConnectionLogDirectory = logDirectory;
        CgosConnectionRecentOutput = recentOutput;
    }

    public void SetCgosBlackConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        if (isRunning && !IsCgosBlackConnectionRunning)
        {
            _cgosBlackConnectionStartedAt = DateTime.Now;
        }

        CgosBlackConnectionStatusMessage = statusMessage;
        IsCgosBlackConnectionRunning = isRunning;
        CgosBlackConnectionLogDirectory = logDirectory;
        CgosBlackConnectionRecentOutput = recentOutput;
    }

    public void SetCgosWhiteConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        if (isRunning && !IsCgosWhiteConnectionRunning)
        {
            _cgosWhiteConnectionStartedAt = DateTime.Now;
        }

        CgosWhiteConnectionStatusMessage = statusMessage;
        IsCgosWhiteConnectionRunning = isRunning;
        CgosWhiteConnectionLogDirectory = logDirectory;
        CgosWhiteConnectionRecentOutput = recentOutput;
    }

    private static string FormatCgosConnectionElapsedDisplay(DateTime? startedAt, bool isRunning)
    {
        if (!isRunning || startedAt is null)
        {
            return "";
        }

        var elapsedSeconds = Math.Max(0, (int)(DateTime.Now - startedAt.Value).TotalSeconds);
        return $"WAIT {elapsedSeconds / 60:00}:{elapsedSeconds % 60:00} / 15s";
    }

    public void SetCgosAdminProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        CgosAdminStatusMessage = statusMessage;
        IsCgosAdminRunning = isRunning;
        CgosAdminLogDirectory = logDirectory;
        CgosAdminRecentOutput = recentOutput;
    }

    public void SetCgosConnectionProfiles(IEnumerable<CgosConnectionProfile> profiles)
    {
        _cgosConnectionProfiles.Clear();
        _cgosConnectionProfiles.AddRange(profiles);
        if (_cgosConnectionProfiles.Count == 0)
        {
            _cgosConnectionProfiles.Add(new CgosConnectionProfile("練習", "uec-go.com", 6809, "PRACTICE", "CGOS practice server"));
        }

        SelectedCgosConnectionProfileIndex = Math.Clamp(SelectedCgosConnectionProfileIndex, 0, _cgosConnectionProfiles.Count - 1);
        CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
    }

    public void OpenCgosConnectionEditPanel()
    {
        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = false;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = SelectedCgosConnectionProfile;
        CgosConnectionEditDraft = SelectedCgosConnectionProfile;
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void OpenCgosConnectionAddPanel()
    {
        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = true;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = CreateDefaultCgosConnectionProfile();
        CgosConnectionEditDraft = _cgosConnectionEditSource;
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void OpenCgosConnectionDuplicatePanel()
    {
        if (_cgosConnectionProfiles.Count == 0)
        {
            return;
        }

        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = true;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = SelectedCgosConnectionProfile;
        CgosConnectionEditDraft = _cgosConnectionEditSource with
        {
            DisplayName = string.IsNullOrWhiteSpace(_cgosConnectionEditSource.DisplayName)
                ? "Unnamed CGOS Connection Copy"
                : $"{_cgosConnectionEditSource.DisplayName.Trim()} Copy",
        };
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void CloseCgosConnectionEditPanel()
    {
        IsCgosConnectionEditPanelOpen = false;
        IsCgosConnectionAddPanelMode = false;
        ActiveCgosConnectionEditField = null;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void BeginCgosConnectionEditField(CgosConnectionProfileEditField field, int caretIndex)
    {
        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, GetCgosConnectionEditFieldText(field).Length);
        CgosConnectionEditWarning = "";
    }

    public void EndCgosConnectionEditField()
    {
        ActiveCgosConnectionEditField = null;
    }

    public void SetCgosConnectionEditField(CgosConnectionProfileEditField field, string text, int caretIndex)
    {
        CgosConnectionEditDraft = field switch
        {
            CgosConnectionProfileEditField.DisplayName => CgosConnectionEditDraft with { DisplayName = text },
            CgosConnectionProfileEditField.Host => CgosConnectionEditDraft with { Host = text },
            CgosConnectionProfileEditField.Port => CgosConnectionEditDraft,
            CgosConnectionProfileEditField.Role => CgosConnectionEditDraft with { Role = text },
            CgosConnectionProfileEditField.Note => CgosConnectionEditDraft with { Note = text },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "CGOS connection edit field is out of range."),
        };
        if (field == CgosConnectionProfileEditField.Port)
        {
            CgosConnectionPortDraft = text;
        }

        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        CgosConnectionEditSaveMessage = "UNSAVED";
    }

    public void SetCgosConnectionEditWarning(string warning)
    {
        CgosConnectionEditWarning = warning;
    }

    public void SaveCgosConnectionEditDraft(CgosConnectionProfile profile)
    {
        if (IsCgosConnectionAddPanelMode)
        {
            _cgosConnectionProfiles.Add(profile);
            SelectedCgosConnectionProfileIndex = _cgosConnectionProfiles.Count - 1;
            CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
            IsCgosConnectionAddPanelMode = false;
        }
        else
        {
            _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex] = profile;
        }

        CgosConnectionEditDraft = _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex];
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditSaveMessage = "SAVED";
        CgosConnectionEditWarning = "";
    }

    public void RemoveSelectedCgosConnectionProfile()
    {
        if (!CanDeleteSelectedCgosConnectionProfile)
        {
            return;
        }

        var removedIndex = SelectedCgosConnectionProfileIndex;
        var nextIndex = Math.Clamp(removedIndex, 0, _cgosConnectionProfiles.Count - 2);
        _cgosConnectionProfiles.RemoveAt(removedIndex);
        SelectedCgosConnectionProfileIndex = nextIndex;
        CgosConnectionSelectionPageIndex = Math.Clamp(
            nextIndex / CgosConnectionSelectionPageSize,
            0,
            Math.Max(0, GetCgosConnectionSelectionPageCount() - 1));
    }

    public void MoveCgosConnectionSelectionPage(int step)
    {
        CgosConnectionSelectionPageIndex = Math.Clamp(
            CgosConnectionSelectionPageIndex + step,
            0,
            GetCgosConnectionSelectionPageCount() - 1);
    }

    public int GetCgosConnectionSelectionPageCount() =>
        Math.Max(1, (int)Math.Ceiling(_cgosConnectionProfiles.Count / (double)CgosConnectionSelectionPageSize));

    public bool CanDeleteSelectedCgosConnectionProfile =>
        _cgosConnectionProfiles.Count > 1 &&
        SelectedCgosConnectionProfileIndex >= 0 &&
        SelectedCgosConnectionProfileIndex < _cgosConnectionProfiles.Count;

    public bool CanMoveCgosConnectionSelectionPage(int step) =>
        Math.Clamp(CgosConnectionSelectionPageIndex + step, 0, GetCgosConnectionSelectionPageCount() - 1) != CgosConnectionSelectionPageIndex;

    public string GetCgosConnectionEditFieldText(CgosConnectionProfileEditField field) => field switch
    {
        CgosConnectionProfileEditField.DisplayName => CgosConnectionEditDraft.DisplayName,
        CgosConnectionProfileEditField.Host => CgosConnectionEditDraft.Host,
        CgosConnectionProfileEditField.Port => CgosConnectionPortDraft,
        CgosConnectionProfileEditField.Role => CgosConnectionEditDraft.Role,
        CgosConnectionProfileEditField.Note => CgosConnectionEditDraft.Note,
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "CGOS connection edit field is out of range."),
    };

    public IEnumerable<int> GetVisibleCgosConnectionProfileIndexes()
    {
        var startIndex = CgosConnectionSelectionPageIndex * CgosConnectionSelectionPageSize;
        var endIndex = Math.Min(startIndex + CgosConnectionSelectionPageSize, _cgosConnectionProfiles.Count);
        for (var i = startIndex; i < endIndex; i++)
        {
            yield return i;
        }
    }

    public void ToggleRenParseDisplay()
    {
        RenParseDisplayMode = RenParseDisplayMode switch
        {
            RenParseDisplayMode.Off => RenParseDisplayMode.Overlay,
            RenParseDisplayMode.Overlay => RenParseDisplayMode.Graph,
            RenParseDisplayMode.Graph => RenParseDisplayMode.GraphStep2,
            RenParseDisplayMode.GraphStep2 => RenParseDisplayMode.Eye,
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

    public bool StartReviewingGameRecord(GoGameRecord record, out string warning)
    {
        ArgumentNullException.ThrowIfNull(record);

        _reviewGameRecord = record.Clone();
        ReviewMoveIndex = 0;
        if (!ApplyReviewPosition(record.Moves.Count, out warning))
        {
            _reviewGameRecord = null;
            ReviewMoveIndex = 0;
            return false;
        }

        if (!ApplyReviewPosition(0, out warning))
        {
            _reviewGameRecord = null;
            ReviewMoveIndex = 0;
            return false;
        }

        ChangeMode(GoAppModeKind.Reviewing);
        return true;
    }

    public bool MoveReview(int step, out string warning)
    {
        warning = "";
        if (CurrentMode.Kind != GoAppModeKind.Reviewing || _reviewGameRecord is null)
        {
            return false;
        }

        return ApplyReviewPosition(Math.Clamp(ReviewMoveIndex + step, 0, ReviewMoveCount), out warning);
    }

    public bool StartReviewingStoredGameRecord(out string warning)
    {
        warning = "";
        if (_reviewGameRecord is null)
        {
            warning = "No SGF review record is loaded.";
            return false;
        }

        if (!ApplyReviewPosition(Math.Clamp(ReviewMoveIndex, 0, ReviewMoveCount), out warning))
        {
            return false;
        }

        ChangeMode(GoAppModeKind.Reviewing);
        return true;
    }

    public void FinishReviewing()
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

    public void ClearSgfGameRecord()
    {
        _reviewGameRecord = null;
        ReviewMoveIndex = 0;
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
        SelectedCgosBlackGtpEngineIndex = 0;
        SelectedCgosWhiteGtpEngineIndex = 0;
    }

    private GtpEngineProfile? GetCgosGtpEngineProfile(int? index) =>
        index is { } selectedIndex && selectedIndex >= 0 && selectedIndex < _gtpEngineProfiles.Count
            ? _gtpEngineProfiles[selectedIndex]
            : null;

    private int? GetSelectedCgosGtpEngineIndex(GoStone stone) =>
        stone switch
        {
            GoStone.Black => SelectedCgosBlackGtpEngineIndex,
            GoStone.White => SelectedCgosWhiteGtpEngineIndex,
            _ => throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white."),
        };

    private void SetSelectedCgosGtpEngineIndex(GoStone stone, int? index)
    {
        if (stone == GoStone.Black)
        {
            SelectedCgosBlackGtpEngineIndex = index;
            return;
        }

        if (stone == GoStone.White)
        {
            SelectedCgosWhiteGtpEngineIndex = index;
            return;
        }

        throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS GTP engine can be selected only for black or white.");
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

    public bool Pass(string comment = "")
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return false;
        }

        KoPoint = null;
        ConsecutivePasses++;
        CurrentGameRecord.Moves.Add(new GoGameMove(CurrentTurn, null, comment));
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

    public string GetOwnEyeForcedPassComment()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
        {
            return "";
        }

        var renParse = _board.ParseRens();
        var hasLegalMove = false;
        for (var y = 0; y < BoardSize; y++)
        {
            for (var x = 0; x < BoardSize; x++)
            {
                var trialBoard = _board.Clone();
                if (!trialBoard.TryPlaceStone(x, y, CurrentTurn, KoPoint, out _, out _))
                {
                    continue;
                }

                hasLegalMove = true;
                if (!_board.IsEyeFor(renParse, x, y, CurrentTurn))
                {
                    return "";
                }
            }
        }

        return hasLegalMove ? "自分の目に打つしかなかったのでパスした。" : "";
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
        _reviewGameRecord = null;
        ReviewMoveIndex = 0;
        ChangeMode(GoAppModeKind.Resting);
        warning = "";
        return true;
    }

    private bool ApplyReviewPosition(int moveCount, out string warning)
    {
        if (_reviewGameRecord is null)
        {
            warning = "No SGF game record is loaded.";
            return false;
        }

        var record = _reviewGameRecord;
        var loadedBoard = new GoBoard(record.BoardSize);
        foreach (var setupStone in record.SetupStones)
        {
            if (!loadedBoard.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"Invalid SGF setup stone at {setupStone.Point.X + 1},{setupStone.Point.Y + 1}.";
                return false;
            }
        }

        var blackAgehama = 0;
        var whiteAgehama = 0;
        var consecutivePasses = 0;
        GoPoint? replayKoPoint = null;
        GoPoint? currentKoPoint = null;
        var clampedMoveCount = Math.Clamp(moveCount, 0, record.Moves.Count);
        for (var i = 0; i < clampedMoveCount; i++)
        {
            var move = record.Moves[i];
            if (move.Point is not { } point)
            {
                replayKoPoint = null;
                currentKoPoint = null;
                consecutivePasses++;
                continue;
            }

            if (!loadedBoard.TryPlaceStone(point.X, point.Y, move.Stone, replayKoPoint, out var capturedStones, out var nextKoPoint))
            {
                warning = $"Illegal SGF move at {point.X + 1},{point.Y + 1}.";
                return false;
            }

            if (move.Stone == GoStone.Black)
            {
                blackAgehama += capturedStones;
            }
            else
            {
                whiteAgehama += capturedStones;
            }

            replayKoPoint = nextKoPoint;
            currentKoPoint = nextKoPoint;
            consecutivePasses = 0;
        }

        BoardSize = record.BoardSize;
        _currentTournamentRules.BoardSize = BoardSize;
        _currentTournamentRules.Komi = record.Komi;
        TournamentRulesSaveMessage = "UNSAVED";
        _board = loadedBoard;
        CurrentTurn = GetReviewCurrentTurn(record, clampedMoveCount);
        BlackAgehama = blackAgehama;
        WhiteAgehama = whiteAgehama;
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        KoPoint = currentKoPoint;
        ConsecutivePasses = consecutivePasses;
        PlayedMoveCount = clampedMoveCount;
        ReviewMoveIndex = clampedMoveCount;
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateReviewGameRecord(record, clampedMoveCount);
        ResetPositionHistory();
        ClearBoardEditingHistory();
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

    public const int CgosConnectionSelectionPageSize = 3;

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

    private static GoGameRecord CreateReviewGameRecord(GoGameRecord source, int moveCount)
    {
        var record = new GoGameRecord
        {
            GameName = source.GameName,
            RuleName = source.RuleName,
            BoardSize = source.BoardSize,
            Komi = source.Komi,
        };

        record.SetupStones.AddRange(source.SetupStones);
        for (var i = 0; i < Math.Clamp(moveCount, 0, source.Moves.Count); i++)
        {
            record.Moves.Add(source.Moves[i]);
        }

        return record;
    }

    private static GoStone GetReviewCurrentTurn(GoGameRecord record, int moveCount)
    {
        if (moveCount < record.Moves.Count)
        {
            return record.Moves[moveCount].Stone;
        }

        if (moveCount > 0)
        {
            var lastStone = record.Moves[moveCount - 1].Stone;
            return lastStone == GoStone.Black ? GoStone.White : GoStone.Black;
        }

        return GoStone.Black;
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

    private static CgosConnectionProfile CreateDefaultCgosConnectionProfile() =>
        new("New CGOS Connection", "uec-go.com", 6809, "PRACTICE", "CGOS practice server");

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
