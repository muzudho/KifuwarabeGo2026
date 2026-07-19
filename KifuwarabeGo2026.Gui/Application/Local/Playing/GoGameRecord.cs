namespace KifuwarabeGo2026.Application.Local.Playing;

using System;
using System.Collections.Generic;

public sealed class GoGameRecord
{
    private int _boardSize = 19;

    public string GameName { get; set; } = "";

    public string RuleName { get; set; } = "";

    public string BlackPlayerName { get; set; } = "";

    public string WhitePlayerName { get; set; } = "";

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
            BoardSize = BoardSize,
            Komi = Komi,
        };

        clone.SetupStones.AddRange(SetupStones);
        clone.Moves.AddRange(Moves);
        return clone;
    }
}
