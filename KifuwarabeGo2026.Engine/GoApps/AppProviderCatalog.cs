namespace KifuwarabeGo2026.Engine.GoApps;

using KifuwarabeGo2026.Engine.GoApps.Casual.NextMove;
using KifuwarabeGo2026.Engine.GoApps.Casual.Ponnuki;
using KifuwarabeGo2026.Engine.GoApps.Casual.Tsumego;

/// <summary>
/// KifuwarabeGo2026.Engine が内蔵するGo App提供者の一覧です。
/// </summary>
internal static class AppProviderCatalog
{
    internal static IReadOnlyList<IAppProvider> All { get; } =
    [
        new PonnukiAppProvider(),
        new TsumegoAppProvider(),
        new NextMoveAppProvider(),
    ];
}
