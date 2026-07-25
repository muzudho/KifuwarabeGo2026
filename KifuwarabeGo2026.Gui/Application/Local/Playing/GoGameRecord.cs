namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using System;
using System.Collections.Generic;

public sealed class GoGameRecord
{
    private int _boardSize = 19;

    public string GameName { get; set; } = "";

    public string RuleName { get; set; } = "";

    public string BlackPlayerName { get; set; } = "";

    public string WhitePlayerName { get; set; } = "";

    public string BlackRank { get; set; } = "";

    public string WhiteRank { get; set; } = "";

    public string PlayedDate { get; set; } = "";

    public string Result { get; set; } = "";

    public string Place { get; set; } = "";

    public int BoardSize
    {
        get => _boardSize;
        set
        {
            if (value is not (9 or 13 or 19))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Board size must be 9, 13, or 19.");
            }

            _boardSize = value;
        }
    }

    public decimal Komi { get; set; } = 6.5m;

    /// <summary>対局全体の基本持ち時間。SGF では秒単位の TM として保存します。</summary>
    public TimeSpan TimeLimit { get; set; }

    public List<GoGameSetupStone> SetupStones { get; } = new();

    public List<GoGameMove> Moves { get; } = new();

    public GoGameRecord Clone()
    {
        var clone = new GoGameRecord
        {
            GameName = GameName,
            RuleName = RuleName,
            BlackPlayerName = BlackPlayerName,
            WhitePlayerName = WhitePlayerName,
            BlackRank = BlackRank,
            WhiteRank = WhiteRank,
            PlayedDate = PlayedDate,
            Result = Result,
            Place = Place,
            BoardSize = BoardSize,
            Komi = Komi,
            TimeLimit = TimeLimit,
        };

        clone.SetupStones.AddRange(SetupStones);
        clone.Moves.AddRange(Moves);
        return clone;
    }
}
