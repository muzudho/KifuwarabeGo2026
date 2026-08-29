namespace KifuwarabeGo2026.Reference.PlayRoomEngine.Go;

internal sealed record GoConfigurationDocument(
    int Version = 1,
    int BoardSize = 19,
    decimal Komi = 6.5m,
    string Ruleset = "chinese-area",
    string StartingPlayer = "black",
    IReadOnlyList<GoSetupStoneDocument>? SetupStones = null,
    long? MainTimeMilliseconds = null);

internal sealed record GoSetupStoneDocument(int X, int Y, string Color);

internal sealed record GoActionDocument(
    int Version,
    string Type,
    string Player,
    int? X = null,
    int? Y = null);

internal sealed record GoPointDocument(int X, int Y);

internal sealed record GoMoveDocument(string Player, string Type, int? X = null, int? Y = null, long? TimeLeftMilliseconds = null);

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
    bool IsTerminal,
    IReadOnlyList<GoPointDocument> SetupBlack,
    IReadOnlyList<GoPointDocument> SetupWhite,
    IReadOnlyList<GoMoveDocument> MoveHistory,
    long? MainTimeMilliseconds,
    long? BlackTimeLeftMilliseconds,
    long? WhiteTimeLeftMilliseconds);

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
