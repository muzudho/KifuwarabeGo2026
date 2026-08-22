namespace KifuwarabeGo2026.Reference.PlaySpace.Go;

internal sealed record GoConfigurationDocument(
    int Version = 1,
    int BoardSize = 19,
    decimal Komi = 6.5m,
    string Ruleset = "chinese-area",
    string StartingPlayer = "black",
    IReadOnlyList<GoSetupStoneDocument>? SetupStones = null);

internal sealed record GoSetupStoneDocument(int X, int Y, string Color);

internal sealed record GoActionDocument(
    int Version,
    string Type,
    string Player,
    int? X = null,
    int? Y = null);

internal sealed record GoPointDocument(int X, int Y);

internal sealed record GoStateDocument(
    int Version,
    int BoardSize,
    string Ruleset,
    decimal Komi,
    IReadOnlyList<GoPointDocument> Black,
    IReadOnlyList<GoPointDocument> White,
    string NextToPlay,
    int BlackCaptures,
    int WhiteCaptures,
    int ConsecutivePasses,
    GoPointDocument? KoPoint,
    bool IsTerminal);

internal sealed record GoEventDocument(
    int Version,
    string Type,
    string PlayedBy,
    GoPointDocument? Point,
    int CapturedStones,
    string NextToPlay);

internal sealed record GoOutcomeDocument(
    int Version,
    string Kind,
    string? Winner,
    string Reason,
    decimal? BlackScore = null,
    decimal? WhiteScore = null,
    decimal? Margin = null);
