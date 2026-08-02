namespace KifuwarabeGo2026.Gui.Application.Local.Playing;

using KifuwarabeGo2026.Shared.Domain;
using System;

public readonly record struct GoGameMove
{
    public GoGameMove(
        GoStone stone,
        GoPoint? point,
        string comment = "",
        GoMoveAnalysis? analysis = null,
        string? commonAnalysisJson = null,
        string? legacyKifuwarabeAnalysisJson = null)
    {
        if (stone is not (GoStone.Black or GoStone.White))
        {
            throw new ArgumentOutOfRangeException(nameof(stone), stone, "Move stone must be black or white.");
        }

        Stone = stone;
        Point = point;
        Comment = comment ?? "";
        Analysis = analysis;
        CommonAnalysisJson = commonAnalysisJson;
        LegacyKifuwarabeAnalysisJson = legacyKifuwarabeAnalysisJson;
    }

    public GoStone Stone { get; }

    public GoPoint? Point { get; }

    public string Comment { get; }

    public GoMoveAnalysis? Analysis { get; }

    /// <summary>
    /// Original CC JSON read from SGF. It is retained verbatim so richer analysis
    /// fields that this application does not understand survive a load/save cycle.
    /// </summary>
    public string? CommonAnalysisJson { get; }

    /// <summary>
    /// Unreadable KFW or legacy KFA JSON retained as a last-resort lossless fallback.
    /// Readable data is migrated to CC; unreadable legacy KFA is renamed to KFW when saved.
    /// </summary>
    public string? LegacyKifuwarabeAnalysisJson { get; }

    public bool IsPass => Point is null;
}
