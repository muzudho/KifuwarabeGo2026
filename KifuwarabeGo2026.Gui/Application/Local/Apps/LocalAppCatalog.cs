namespace KifuwarabeGo2026.Gui.Application.Local.Apps;

public sealed record LocalAppDescriptor(
    string Id,
    int Version,
    int BoardSize,
    bool RequiresBlackAndWhitePlayers,
    int InitialRandomMoveCount);

public static class LocalAppCatalog
{
    public static LocalAppDescriptor Ponnuki { get; } = new(
        "ponnuki",
        Version: 1,
        BoardSize: 9,
        RequiresBlackAndWhitePlayers: true,
        InitialRandomMoveCount: 20);
}
