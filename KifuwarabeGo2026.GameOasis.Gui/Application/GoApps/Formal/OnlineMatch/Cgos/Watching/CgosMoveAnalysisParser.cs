namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;

using KifuwarabeGo2026.FormalAdapter.Gtp.Protocol;
using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;

internal static class CgosMoveAnalysisParser
{
    public static GoMoveAnalysis? Parse(string? json, string playedVertex) =>
        CgosGtpMoveAnalysisParser.Parse(json, playedVertex) is { } analysis
            ? new GoMoveAnalysis(analysis.WinRate, analysis.PrincipalVariation, analysis.Score, analysis.Visits)
            : null;

    public static string ParseComment(string? json) => CgosGtpMoveAnalysisParser.ParseComment(json);
}
