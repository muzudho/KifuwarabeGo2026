namespace KifuwarabeGo2026.Application.Cgos.Watching;

using KifuwarabeGo2026.Domain;
using KifuwarabeGo2026.Gtp;
using System;
using System.Globalization;

/// <summary>
/// CGOS 通信ログから復元した観戦用の対局状態です。
/// </summary>
public sealed class CgosGameObservation
{
    private GoBoard _board = new(9);
    private GoPoint? _koPoint;

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

    public GoStone GetStone(int x, int y) => _board.GetStone(x, y);

    /// <summary>
    /// 通信プロセスの表示行を観戦状態へ反映します。
    /// </summary>
    public void ProcessLogLine(string displayLine)
    {
        var marker = displayLine.IndexOf("] > ", StringComparison.Ordinal);
        if (marker >= 0)
        {
            ProcessServerCommand(displayLine[(marker + 4)..]);
            return;
        }

        marker = displayLine.IndexOf("] # Generated ", StringComparison.Ordinal);
        if (marker < 0)
        {
            return;
        }

        var generated = displayLine[(marker + 14)..].Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (generated.Length >= 3 && generated[1].Equals("move:", StringComparison.OrdinalIgnoreCase))
        {
            ApplyMove(ParseStone(generated[0]), generated[2]);
        }
    }

    private void ProcessServerCommand(string commandLine)
    {
        var parts = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return;
        }

        if (parts[0].Equals("setup", StringComparison.OrdinalIgnoreCase))
        {
            ProcessSetup(parts);
        }
        else if (parts[0].Equals("play", StringComparison.OrdinalIgnoreCase) && parts.Length >= 3)
        {
            ApplyMove(ParseStone(parts[1]), parts[2]);
        }
        else if (parts[0].Equals("gameover", StringComparison.OrdinalIgnoreCase))
        {
            IsFinished = true;
            Result = parts.Length > 1 ? string.Join(' ', parts[1..]) : "GAME OVER";
        }
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
        Result = "";
        IsFinished = false;
        IsStarted = true;

        for (var index = 7; index + 1 < parts.Length; index += 2)
        {
            ApplyMove(CurrentTurn, parts[index]);
        }
    }

    private void ApplyMove(GoStone stone, string vertex)
    {
        if (!IsStarted || IsFinished || stone == GoStone.Empty || stone != CurrentTurn)
        {
            return;
        }

        if (!GtpCoordinate.IsPass(vertex))
        {
            if (!GtpCoordinate.TryParseVertex(vertex, BoardSize, out var point) ||
                !_board.TryPlaceStone(point.X, point.Y, stone, _koPoint, out _, out var nextKoPoint))
            {
                return;
            }

            _koPoint = nextKoPoint;
        }
        else
        {
            _koPoint = null;
        }

        MoveCount++;
        CurrentTurn = stone == GoStone.Black ? GoStone.White : GoStone.Black;
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
