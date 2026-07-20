namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;

public sealed record GtpEngineGuiOptionSpec(
    string Id,
    string Label,
    string Type,
    string DefaultValue,
    int? Min = null,
    int? Max = null,
    IReadOnlyList<string>? Values = null);

/// <summary>GUIが編集できる既知のGTPエンジンオプションです。</summary>
public static class GtpEngineGuiOptions
{
    public const string AvoidEyesId = "AvoidEyes";
    public const string RandomSeedId = "RandomSeed";
    public const string RandomMoveId = "RandomMove";
    public const string EngineTagId = "EngineTag";
    public const string DebugLogFileId = "DebugLogFile";
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
    ];

    public static int KnownOptionCount => Specs.Length;
}
