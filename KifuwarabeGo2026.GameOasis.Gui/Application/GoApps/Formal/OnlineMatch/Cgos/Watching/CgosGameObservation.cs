namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.FormalAdapter.Cgos.Observability;
using KifuwarabeGo2026.FormalAdapter.Cgos.Go;
using KifuwarabeGo2026.FormalAdapter.Cgos.Compatibility;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// CGOS 通信ログから復元した観戦用の対局状態です。
/// </summary>
public sealed class CgosGameObservation
{
    private GoBoard _board = new(9);
    private GoBoard? _replayBoard;
    private GoPoint? _koPoint;
    private readonly List<GoGameMove> _moves = [];
    private int? _replayMoveIndex;
    private DateTimeOffset _lastClockSyncAt = DateTimeOffset.UtcNow;
    private bool _receivedStructuredNotifications;
    private readonly CgosGoEventProjector _eventProjector = new();

    public bool IsStarted { get; private set; }
    public bool IsFinished { get; private set; }
    public int GameId { get; private set; }
    public int BoardSize => _board.Size;
    public decimal Komi { get; private set; }
    public string WhitePlayerName { get; private set; } = "-";
    public string BlackPlayerName { get; private set; } = "-";
    public GoStone CurrentTurn { get; private set; } = GoStone.Black;
    public int MoveCount { get; private set; }
    public string Result { get; private set; } = "";
    public DateTime StartedAt { get; private set; }
    public TimeSpan MainTime { get; private set; }
    public TimeSpan BlackRemainingTime { get; private set; }
    public TimeSpan WhiteRemainingTime { get; private set; }
    public TimeSpan BlackElapsedTime => MainTime - BlackRemainingTime;
    public TimeSpan WhiteElapsedTime => MainTime - WhiteRemainingTime;
    public TimeSpan BlackLiveElapsedTime => GetLiveElapsedTime(GoStone.Black, BlackElapsedTime);
    public TimeSpan WhiteLiveElapsedTime => GetLiveElapsedTime(GoStone.White, WhiteElapsedTime);
    public int BlackAgehama { get; private set; }
    public int WhiteAgehama { get; private set; }
    public IReadOnlyList<GoGameMove> Moves => _moves;
    public GoMoveAnalysis? LatestAnalysis => _moves.Count == 0 ? null : _moves[^1].Analysis;
    public bool IsReplayMode => _replayMoveIndex is not null;
    public int DisplayMoveIndex => _replayMoveIndex ?? MoveCount;

    public GoStone GetStone(int x, int y) => (_replayBoard ?? _board).GetStone(x, y);
    public GoStone GetLiveStone(int x, int y) => _board.GetStone(x, y);
    public bool CanPlayLiveMove(int x, int y, GoStone stone)
    {
        if (!IsStarted || IsFinished || IsReplayMode || stone != CurrentTurn) return false;
        var candidate = BuildBoardAt(_moves.Count);
        return candidate.TryPlaceStone(x, y, stone, _koPoint, out _, out _);
    }

    /// <summary>
    /// CGOS は自分が返した着手を play コマンドでは送り返さないため、
    /// GUI から送信できた人間の着手を観戦盤へ直ちに反映します。
    /// </summary>
    public bool ApplyHumanMove(GoStone stone, string vertex)
    {
        if (!IsStarted || IsFinished || IsReplayMode || stone != CurrentTurn) return false;

        var currentRemainingTime = stone == GoStone.Black ? BlackRemainingTime : WhiteRemainingTime;
        var elapsedSinceClockSync = DateTimeOffset.UtcNow - _lastClockSyncAt;
        var remainingTime = currentRemainingTime - elapsedSinceClockSync;
        if (remainingTime < TimeSpan.Zero) remainingTime = TimeSpan.Zero;
        var remainingTimeMilliseconds = ((long)remainingTime.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);

        if (GtpCoordinate.IsPass(vertex))
        {
            ApplyMove(stone, vertex, remainingTimeMilliseconds, null);
            return true;
        }

        return ApplyMove(stone, vertex, remainingTimeMilliseconds, null);
    }
    public GoGameMove? LatestMove => _moves.Count == 0 ? null : _moves[^1];

    public GoStone GetPlayerColor(string loginName)
    {
        if (string.Equals(BlackPlayerName, loginName, StringComparison.OrdinalIgnoreCase)) return GoStone.Black;
        if (string.Equals(WhitePlayerName, loginName, StringComparison.OrdinalIgnoreCase)) return GoStone.White;
        return GoStone.Empty;
    }

    public string GetOpponentName(string loginName) => GetPlayerColor(loginName) switch
    {
        GoStone.Black => WhitePlayerName,
        GoStone.White => BlackPlayerName,
        _ => "-",
    };

    public void Reset()
    {
        _board = new GoBoard(9);
        _replayBoard = null;
        _koPoint = null;
        _moves.Clear();
        _replayMoveIndex = null;
        IsStarted = false;
        IsFinished = false;
        GameId = 0;
        Komi = 0m;
        WhitePlayerName = "-";
        BlackPlayerName = "-";
        CurrentTurn = GoStone.Black;
        MoveCount = 0;
        Result = "";
        StartedAt = default;
        MainTime = TimeSpan.Zero;
        BlackRemainingTime = TimeSpan.Zero;
        WhiteRemainingTime = TimeSpan.Zero;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        _lastClockSyncAt = DateTimeOffset.UtcNow;
        _receivedStructuredNotifications = false;
        _eventProjector.Reset();
    }

    /// <summary>
    /// 現在の観戦盤面を連解析します。
    /// </summary>
    public GoRenParseResult ParseRens() => (_replayBoard ?? _board).ParseRens();

    public void SeekReplay(int moveIndex)
    {
        if (!IsStarted)
        {
            return;
        }

        var clampedMoveIndex = Math.Clamp(moveIndex, 0, _moves.Count);
        if (clampedMoveIndex == _moves.Count)
        {
            ReturnToLive();
            return;
        }

        _replayMoveIndex = clampedMoveIndex;
        _replayBoard = BuildBoardAt(clampedMoveIndex);
    }

    public void ReturnToLive()
    {
        _replayMoveIndex = null;
        _replayBoard = null;
    }

    /// <summary>
    /// 現在の CGOS 対局を SGF 出力用の棋譜へ変換します。
    /// </summary>
    public GoGameRecord CreateGameRecord()
    {
        var record = new GoGameRecord
        {
            GameName = $"CGOS {GameId}: {BlackPlayerName} vs {WhitePlayerName} {Result}".Trim(),
            RuleName = "CGOS",
            BlackPlayerName = BlackPlayerName,
            WhitePlayerName = WhitePlayerName,
            BoardSize = BoardSize,
            Komi = Komi,
            TimeLimit = MainTime,
            Result = Result,
        };
        record.Moves.AddRange(_moves);
        return record;
    }

    /// <summary>
    /// 通信プロセスの表示行を観戦状態へ反映します。
    /// </summary>
    public bool ProcessLogLine(string displayLine)
    {
        if (CgosNotificationJsonLines.TryParse(displayLine, out var notification) && notification is not null)
        {
            _receivedStructuredNotifications = true;
            return ProcessNotification(notification);
        }

        return !_receivedStructuredNotifications &&
               CgosLegacyLogNotificationAdapter.TryParse(displayLine, out var legacyNotification) &&
               legacyNotification is not null &&
               ProcessNotification(legacyNotification);
    }

    private bool ProcessNotification(CgosNotification notification)
    {
        if (!_eventProjector.TryProject(notification, out var gameEvent) || gameEvent is null) return false;
        switch (gameEvent)
        {
            case CgosGoSetup setup:
                ProcessSetup(setup);
                return false;
            case CgosGoMove move:
                return ApplyMove(
                    move.Color == CgosGoColor.Black ? GoStone.Black : GoStone.White,
                    move.Vertex.Text,
                    move.TimeLeftMilliseconds?.ToString(CultureInfo.InvariantCulture),
                    move.AnalysisJson,
                    move.Vertex.IsPass ? null : new GoPoint(move.Vertex.X!.Value, move.Vertex.Y!.Value),
                    vertexWasProjected: true);
            case CgosGoGameOver gameOver:
                IsFinished = true;
                Result = gameOver.Result;
                ReturnToLive();
                return false;
            default:
                return false;
        }
    }

    private void ProcessSetup(CgosGoSetup setup)
    {
        if (setup.BoardSize is not (9 or 13 or 19) || (IsStarted && GameId == setup.GameId)) return;
        InitializeGame(
            setup.GameId,
            setup.BoardSize,
            setup.Komi,
            setup.MainTimeMilliseconds,
            setup.WhitePlayer,
            setup.BlackPlayer);
        foreach (var move in setup.MoveHistory)
            ApplyMove(
                move.Color == CgosGoColor.Black ? GoStone.Black : GoStone.White,
                move.Vertex.Text,
                move.TimeLeftMilliseconds?.ToString(CultureInfo.InvariantCulture),
                move.AnalysisJson,
                move.Vertex.IsPass ? null : new GoPoint(move.Vertex.X!.Value, move.Vertex.Y!.Value),
                vertexWasProjected: true);
    }

    private void InitializeGame(
        int gameId,
        int boardSize,
        decimal komi,
        long mainTimeMilliseconds,
        string whitePlayer,
        string blackPlayer)
    {
        _board = new GoBoard(boardSize);
        _koPoint = null;
        GameId = gameId;
        Komi = komi;
        WhitePlayerName = StripRank(whitePlayer);
        BlackPlayerName = StripRank(blackPlayer);
        CurrentTurn = GoStone.Black;
        MoveCount = 0;
        MainTime = TimeSpan.FromMilliseconds(Math.Max(0, mainTimeMilliseconds));
        BlackRemainingTime = MainTime;
        WhiteRemainingTime = MainTime;
        BlackAgehama = 0;
        WhiteAgehama = 0;
        _moves.Clear();
        ReturnToLive();
        Result = "";
        IsFinished = false;
        IsStarted = true;
        StartedAt = DateTime.Now;
        _lastClockSyncAt = DateTimeOffset.UtcNow;

    }

    /// <summary>
    /// 着手を適用します。
    /// </summary>
    /// <param name="stone"></param>
    /// <param name="vertex"></param>
    /// <returns></returns>
    private bool ApplyMove(
        GoStone stone,
        string vertex,
        string? remainingTimeMilliseconds,
        string? analysisJson,
        GoPoint? projectedPoint = null,
        bool vertexWasProjected = false)
    {
        if (!IsStarted || IsFinished || stone == GoStone.Empty || stone != CurrentTurn) return false;

        GoPoint? movePoint = null;
        var isPass = vertexWasProjected ? projectedPoint is null : GtpCoordinate.IsPass(vertex);
        if (!isPass)
        {
            var point = projectedPoint ?? default;
            if ((!vertexWasProjected && !GtpCoordinate.TryParseVertex(vertex, BoardSize, out point)) ||
                !_board.TryPlaceStone(point.X, point.Y, stone, _koPoint, out var capturedStones, out var nextKoPoint))
                return false;

            _koPoint = nextKoPoint;
            movePoint = point;
            if (stone == GoStone.Black)
                BlackAgehama += capturedStones;
            else
                WhiteAgehama += capturedStones;
        }
        else
        {
            _koPoint = null;
        }

        var analysis = CgosMoveAnalysisParser.Parse(analysisJson, vertex);
        var comment = CgosMoveAnalysisParser.ParseComment(analysisJson);
        TimeSpan? timeLeftAfterMove = null;
        if (long.TryParse(remainingTimeMilliseconds, out var remainingMilliseconds))
        {
            timeLeftAfterMove = TimeSpan.FromMilliseconds(Math.Clamp(remainingMilliseconds, 0, (long)MainTime.TotalMilliseconds));
            if (stone == GoStone.Black)
                BlackRemainingTime = timeLeftAfterMove.Value;
            else
                WhiteRemainingTime = timeLeftAfterMove.Value;
        }
        _moves.Add(new GoGameMove(
            stone,
            movePoint,
            comment,
            analysis,
            commonAnalysisJson: string.IsNullOrWhiteSpace(analysisJson) ? null : analysisJson,
            timeLeftAfterMove: timeLeftAfterMove));
        MoveCount++;
        CurrentTurn = stone == GoStone.Black ? GoStone.White : GoStone.Black;
        _lastClockSyncAt = DateTimeOffset.UtcNow;
        return movePoint is not null;
    }

    private TimeSpan GetLiveElapsedTime(GoStone stone, TimeSpan serverElapsed)
    {
        if (!IsStarted || IsFinished || CurrentTurn != stone)
        {
            return serverElapsed;
        }

        var liveElapsed = serverElapsed + (DateTimeOffset.UtcNow - _lastClockSyncAt);
        return liveElapsed < TimeSpan.Zero ? TimeSpan.Zero : liveElapsed;
    }

    private GoBoard BuildBoardAt(int moveIndex)
    {
        var board = new GoBoard(BoardSize);
        GoPoint? koPoint = null;
        for (var index = 0; index < moveIndex; index++)
        {
            var move = _moves[index];
            if (move.Point is not { } point)
            {
                koPoint = null;
                continue;
            }

            if (board.TryPlaceStone(
                    point.X,
                    point.Y,
                    move.Stone,
                    koPoint,
                    out _,
                    out var nextKoPoint))
            {
                koPoint = nextKoPoint;
            }
        }

        return board;
    }

    private static string StripRank(string text)
    {
        var rankStart = text.LastIndexOf('(');
        return rankStart > 0 && text.EndsWith(')') ? text[..rankStart] : text;
    }
}
