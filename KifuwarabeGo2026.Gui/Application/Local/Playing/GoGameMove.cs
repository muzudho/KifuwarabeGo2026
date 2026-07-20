namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Shared.Domain;
using System;

public readonly record struct GoGameMove
{
    public GoGameMove(GoStone stone, GoPoint? point, string comment = "", GoMoveAnalysis? analysis = null)
    {
        if (stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Move stone must be black or white.");
        }

        Stone = stone;
        Point = point;
        Comment = comment ?? "";
        Analysis = analysis;
    }

    public GoStone Stone { get; }

    public GoPoint? Point { get; }

    public string Comment { get; }

    public GoMoveAnalysis? Analysis { get; }

    public bool IsPass => Point is null;
}
