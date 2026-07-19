namespace KifuwarabeGo2026.Gui.Application.Cgos.Watching;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Gtp;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// CGOS 通信ログから復元した観戦用の対局状態です。
/// </summary>
public sealed class CgosGameObservation
{
    private GoBoard _board = new(9);
    private GoPoint? _koPoint;
    private readonly List<GoGameMove> _moves = [];

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
    public int BlackAgehama { get; private set; }
    public int WhiteAgehama { get; private set; }

    public GoStone GetStone(int x, int y) => _board.GetStone(x, y);

    /// <summary>
    /// 現在の観戦盤面を連解析します。
    /// </summary>
    public GoRenParseResult ParseRens() => _board.ParseRens();

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

        var generated = displayLine[(marker + 14)..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (generated.Length >= 3 && generated[1].Equals("move:", StringComparison.OrdinalIgnoreCase))
            return ApplyMove(ParseStone(generated[0]), generated[2], null);

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
            return ApplyMove(ParseStone(parts[1]), parts[2], parts.Length >= 4 ? parts[3] : null);
        }
        else if (parts[0].Equals("gameover", StringComparison.OrdinalIgnoreCase))
        {
            IsFinished = true;
            Result = parts.Length > 1 ? string.Join(' ', parts[1..]) : "GAME OVER";
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
        Result = "";
        IsFinished = false;
        IsStarted = true;
        StartedAt = DateTime.Now;

        for (var index = 7; index + 1 < parts.Length; index += 2)
        {
            ApplyMove(CurrentTurn, parts[index], parts[index + 1]);
        }
    }

    /// <summary>
    /// 着手を適用します。
    /// </summary>
    /// <param name="stone"></param>
    /// <param name="vertex"></param>
    /// <returns></returns>
    private bool ApplyMove(GoStone stone, string vertex, string? remainingTimeMilliseconds)
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

        _moves.Add(new GoGameMove(stone, movePoint));
        if (long.TryParse(remainingTimeMilliseconds, out var remainingMilliseconds))
        {
            var remaining = TimeSpan.FromMilliseconds(Math.Clamp(remainingMilliseconds, 0, (long)MainTime.TotalMilliseconds));
            if (stone == GoStone.Black)
                BlackRemainingTime = remaining;
            else
                WhiteRemainingTime = remaining;
        }
        MoveCount++;
        CurrentTurn = stone == GoStone.Black ? GoStone.White : GoStone.Black;
        return movePoint is not null;
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
