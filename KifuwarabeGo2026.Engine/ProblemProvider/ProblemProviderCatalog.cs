namespace KifuwarabeGo2026.Engine.ProblemProvider;

using KifuwarabeGo2026.Engine.ProblemProvider.NextMove;
using KifuwarabeGo2026.Engine.ProblemProvider.Ponnuki;
using KifuwarabeGo2026.Engine.ProblemProvider.Tsumego;

/// <summary>
/// KifuwarabeGo2026.Engine が内蔵する問題提供者の一覧です。
/// </summary>
internal static class ProblemProviderCatalog
{
    internal static IReadOnlyList<IProblemProvider> All { get; } =
    [
        new PonnukiProblemProvider(),
        new TsumegoProblemProvider(),
        new NextMoveProblemProvider(),
    ];
}
