namespace KifuwarabeGo2026.Application.Game;

using KifuwarabeGo2026.Domain;
using System;

public readonly record struct GoGameMove
{
    public GoGameMove(GoStone stone, GoPoint? point)
    {
        if (stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Move stone must be black or white.");
        }

        Stone = stone;
        Point = point;
    }

    public GoStone Stone { get; }

    public GoPoint? Point { get; }

    public bool IsPass => Point is null;
}
