namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.GameOasis.Gui.Gtp;
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
        var marker = displayLine.IndexOf("] > ", StringComparison.Ordinal);
        if (marker >= 0)
            return ProcessServerCommand(displayLine[(marker + 4)..]);

        marker = displayLine.IndexOf("] # Generated ", StringComparison.Ordinal);
        if (marker < 0) return false;

        var generated = displayLine[(marker + 14)..].Split(' ', 4, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (generated.Length >= 3 && generated[1].Equals("move:", StringComparison.OrdinalIgnoreCase))
            return ApplyMove(ParseStone(generated[0]), generated[2], null, generated.Length >= 4 ? generated[3] : null);

        return false;
    }

    private bool ProcessServerCommand(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return false;

        if (parts[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
        {
            ProcessSetup(parts);
        }
        else if (parts[0].Equals("play", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
        {
            var playParts = commandLine.Split(' ', 5, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return ApplyMove(
                ParseStone(playParts[1]),
                playParts[2],
                playParts.Length >= 4 ? playParts[3] : null,
                playParts.Length >= 5 ? playParts[4] : null);
        }
        else if (parts[0].Equals("gameover", StringComparison.OrdinalIgnoreCase))
        {
            IsFinished = true;
            Result = parts.Length > 1 ? string.Join(' ', parts[1..]) : "GAME OVER";
            ReturnToLive();
        }

        return false;
    }

    private void ProcessSetup(string[] parts)
    {
        if (parts.Length < 7 || !int.TryParse(parts[1], out var gameId) ||
            !int.TryParse(parts[2], out var boardSize) || boardSize is not (9 or 13 or 19))
        {
            return;
        }

        if (IsStarted && GameId == gameId)
        {
            return;
        }

        _board = new GoBoard(boardSize);
        _koPoint = null;
        GameId = gameId;
        Komi = decimal.TryParse(parts[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var komi) ? komi : 0m;
        WhitePlayerName = StripRank(parts[5]);
        BlackPlayerName = StripRank(parts[6]);
        CurrentTurn = GoStone.Black;
        MoveCount = 0;
        var mainTimeMilliseconds = long.TryParse(parts[4], out var parsedMainTime) ? Math.Max(0, parsedMainTime) : 0;
        MainTime = TimeSpan.FromMilliseconds(mainTimeMilliseconds);
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

        for (var index = 7; index + 1 < parts.Length; index += 2)
        {
            ApplyMove(CurrentTurn, parts[index], parts[index + 1], null);
        }
    }

    /// <summary>
    /// 着手を適用します。
    /// </summary>
    /// <param name="stone"></param>
    /// <param name="vertex"></param>
    /// <returns></returns>
    private bool ApplyMove(GoStone stone, string vertex, string? remainingTimeMilliseconds, string? analysisJson)
    {
        if (!IsStarted || IsFinished || stone == GoStone.Empty || stone != CurrentTurn) return false;

        GoPoint? movePoint = null;
        if (!GtpCoordinate.IsPass(vertex))
        {
            if (!GtpCoordinate.TryParseVertex(vertex, BoardSize, out var point) ||
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

    private static GoStone ParseStone(string text) => text.ToLowerInvariant() switch
    {
        "b" or "black" => GoStone.Black,
        "w" or "white" => GoStone.White,
        _ => GoStone.Empty,
    };

    private static string StripRank(string text)
    {
        var rankStart = text.LastIndexOf('(');
        return rankStart > 0 && text.EndsWith(')') ? text[..rankStart] : text;
    }
}
