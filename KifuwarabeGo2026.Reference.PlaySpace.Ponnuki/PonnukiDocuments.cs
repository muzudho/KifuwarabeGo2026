namespace KifuwarabeGo2026.Reference.PlaySpace.Ponnuki;

internal sealed record PonnukiConfigurationDocument(
    int Version = 1,
    int BoardSize = 9,
    int InitialMoveCount = 20,
    int? RandomSeed = null,
    int CaptureTarget = 20,
    string? StartingPlayer = null,
    IReadOnlyList<PonnukiSetupStoneDocument>? SetupStones = null);

internal sealed record PonnukiSetupStoneDocument(int X, int Y, string Color);

internal sealed record PonnukiActionDocument(
    int Version,
    string Type,
    string Player,
    int? X = null,
    int? Y = null);

internal sealed record PonnukiPointDocument(int X, int Y);

internal sealed record PonnukiStateDocument(
    int Version,
    int BoardSize,
    IReadOnlyList<PonnukiPointDocument> Black,
    IReadOnlyList<PonnukiPointDocument> White,
    string NextToPlay,
    int BlackCaptures,
    int WhiteCaptures,
    int CaptureTarget,
    int RandomSeed,
    PonnukiPointDocument? KoPoint,
    bool IsTerminal);

internal sealed record PonnukiEventDocument(
    int Version,
    string Type,
    string PlayedBy,
    PonnukiPointDocument? Point,
    int CapturedStones,
    string NextToPlay);

internal sealed record PonnukiOutcomeDocument(
    int Version,
    string Winner,
    string Reason,
    int BlackCaptures,
    int WhiteCaptures);
