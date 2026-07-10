namespace KifuwarabeGo2026.Application;

using KifuwarabeGo2026.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class GoAppSession
{
    private GoBoard _board;
    private readonly HashSet<ulong> _positionHashes = new();
    private readonly List<TournamentRules> _tournamentRules = new();
    private TournamentRules _currentTournamentRules = new();

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
        ResetPositionHistory();
    }

    public GoAppMode CurrentMode { get; private set; }

    public int BoardSize { get; private set; } = 19;

    public IReadOnlyList<TournamentRules> TournamentRulesList => _tournamentRules;

    public int SelectedTournamentRulesIndex { get; private set; }

    public string TournamentRulesSaveMessage { get; private set; } = "";

    public string TournamentDisplayName => _currentTournamentRules.DisplayName;

    public GoRuleKind RuleKind => _currentTournamentRules.Rule;

    public decimal Komi => _currentTournamentRules.Komi;

    public TimeSpan MainTime => _currentTournamentRules.MainTime;

    public TournamentRules CurrentTournamentRules => _currentTournamentRules.Clone();

    public GoStone CurrentTurn { get; private set; } = GoStone.Black;

    public GoPlayerKind BlackPlayerKind { get; private set; } = GoPlayerKind.Human;

    public GoPlayerKind WhitePlayerKind { get; private set; } = GoPlayerKind.Computer;

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

    public void StartPlaying()
    {
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            ClearBoard();
        }

        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        ChangeMode(GoAppModeKind.Playing);
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

    public void MarkTournamentRulesSaved()
    {
        if (SelectedTournamentRulesIndex >= 0 && SelectedTournamentRulesIndex < _tournamentRules.Count)
        {
            _tournamentRules[SelectedTournamentRulesIndex] = _currentTournamentRules.Clone();
        }

        TournamentRulesSaveMessage = "SAVED";
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
        EngineLogPath = "";
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
        PassTurn();
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
        PassTurn();
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
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        ResetPositionHistory();
    }

    private void ApplyTournamentRules(TournamentRules rules)
    {
        _currentTournamentRules = rules.Clone();
        BoardSize = _currentTournamentRules.BoardSize is 9 or 13 or 19 ? _currentTournamentRules.BoardSize : 19;
        _currentTournamentRules.BoardSize = BoardSize;
        ClearBoard();
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

    private void DecidePureGoResult()
    {
        var blackStones = BlackStoneCount;
        var whiteStones = WhiteStoneCount;
        Winner = blackStones == whiteStones ? null : blackStones > whiteStones ? GoStone.Black : GoStone.White;
        GameOverReason = Winner is null ? "PURE GO DRAW" : $"PURE GO {StoneName(Winner.Value)} +{Math.Abs(blackStones - whiteStones)}";
    }

    private static GoStone OppositeOf(GoStone stone) => stone == GoStone.Black ? GoStone.White : GoStone.Black;

    private static string StoneName(GoStone stone) => stone == GoStone.Black ? "BLACK" : "WHITE";
}
