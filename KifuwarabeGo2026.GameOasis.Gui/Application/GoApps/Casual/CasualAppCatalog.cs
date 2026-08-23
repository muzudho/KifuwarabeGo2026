namespace KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Casual;

public sealed record CasualAppDescriptor(
    string Id,
    int Version,
    int BoardSize,
    bool RequiresBlackAndWhitePlayers,
    int InitialRandomMoveCount);

public static class CasualAppCatalog
{
    public static CasualAppDescriptor Ponnuki { get; } = new(
        "ponnuki",
        Version: 1,
        BoardSize: 9,
        RequiresBlackAndWhitePlayers: true,
        InitialRandomMoveCount: 20);
}
