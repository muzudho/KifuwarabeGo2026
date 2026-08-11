namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;

public sealed record GtpEngineGuiOptionChoice(string Value, bool IsEnabled = true, string DisabledReason = "");

public sealed record GtpEngineGuiOptionSpec(
    string Id,
    string Label,
    string Type,
    string DefaultValue,
    int? Min = null,
    int? Max = null,
    IReadOnlyList<string>? Values = null,
    string Binding = "",
    IReadOnlyList<GtpEngineGuiOptionChoice>? Choices = null);

/// <summary>GUIが編集できる既知のGTPエンジンオプションです。</summary>
public static class GtpEngineGuiOptions
{
    public const int MaximumTextLength = 10_000;
    public const string AvoidEyesId = "AvoidEyes";
    public const string RandomSeedId = "RandomSeed";
    public const string RandomMoveId = "RandomMove";
    public const string EngineTagId = "EngineTag";
    public const string DebugLogFileId = "DebugLogFile";
    public const string ClearCacheId = "ClearCache";
    public const string BoardSizeId = "BoardSize";
    public const string InitialMoveCountId = "InitialMoveCount";
    public const string GtpBoardSizeBinding = "gtp.boardsize";
    public const string NormalRandomMove = "Normal";
    public const string ChebyshevDistanceFromStarRandomMove = "ChebyshevDistanceFromStar";

    public static readonly string[] RandomMoveValues = [NormalRandomMove, ChebyshevDistanceFromStarRandomMove];

    public static readonly GtpEngineGuiOptionSpec[] Specs =
    [
        new(AvoidEyesId, "AvoidEyes", "check", "true"),
        new(RandomSeedId, "RandomSeed", "spin", "0", 0, int.MaxValue),
        new(RandomMoveId, "RandomMove", "combo", ChebyshevDistanceFromStarRandomMove, Values: RandomMoveValues),
        new(EngineTagId, "EngineTag", "string", ""),
        new(DebugLogFileId, "DebugLogFile", "filename", ""),
        new(ClearCacheId, "ClearCache", "button", "false"),
    ];

    public static readonly GtpEngineGuiOptionSpec[] PonnukiProviderSpecs =
    [
        // Compatibility fallback for Providers that do not publish a schema.
        // Do not advertise 13/19 here: only the Provider may claim those sizes.
        new(BoardSizeId, "BoardSize", "combo", "9", Values: ["9"], Binding: GtpBoardSizeBinding,
            Choices: [new("9")]),
        new(InitialMoveCountId, "InitialMoveCount", "spin", "20", 0, 20),
        new(RandomSeedId, "RandomSeed", "spin", "0", 0, int.MaxValue),
    ];

    public static int KnownOptionCount => Specs.Length;

    public static bool IsSupportedBoardSize(int boardSize) => boardSize is 9 or 13 or 19;
}
