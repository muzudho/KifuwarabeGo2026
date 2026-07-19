namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Gui.Domain;
using System;

public readonly record struct GoGameSetupStone
{
    public GoGameSetupStone(GoStone stone, GoPoint point)
    {
        if (stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Setup stone must be black or white.");
        }

        Stone = stone;
        Point = point;
    }

    public GoStone Stone { get; }

    public GoPoint Point { get; }
}
