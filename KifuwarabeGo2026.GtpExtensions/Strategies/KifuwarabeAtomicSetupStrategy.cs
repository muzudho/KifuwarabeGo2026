namespace KifuwarabeGo2026.GtpExtensions.Strategies;

using System.Globalization;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Protocol;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Uses Kifuwarabe's transactional extension to reproduce an arbitrary setup position atomically.
/// </summary>
public sealed class KifuwarabeAtomicSetupStrategy : IInitialPositionStrategy
{
    public static KifuwarabeAtomicSetupStrategy Instance { get; } = new();

    private KifuwarabeAtomicSetupStrategy()
    {
    }

    public InitialPositionMethod Method => InitialPositionMethod.KifuwarabeAtomicSetup;

    public string DisplayName => "きふわらべ原子的指定局面";

    public IReadOnlyList<string> RequiredCommands { get; } =
    [
        "begin_position", "add_black", "add_white", "set_to_play", "commit_position", "abort_position",
    ];

    public bool CanApply(InitialPositionRequest request, InitialPositionClassification classification)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind != InitialPositionKind.HistorySensitivePosition;
    }

    public IReadOnlyList<string> BuildCommands(
        InitialPositionRequest request,
        InitialPositionStrategyContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        var classification = InitialPositionClassifier.Classify(request);
        if (!CanApply(request, classification))
            throw new InvalidOperationException("The atomic setup extension cannot reproduce a history-sensitive position.");

        var commands = new List<string>
        {
            $"boardsize {request.BoardSize}",
            $"komi {request.Komi.ToString(CultureInfo.InvariantCulture)}",
            "begin_position",
        };
        foreach (var setupStone in request.SetupStones)
        {
            var command = setupStone.Stone == GoStone.Black ? "add_black" : "add_white";
            commands.Add($"{command} {GtpCoordinate.FormatVertex(setupStone.Point, request.BoardSize)}");
        }

        commands.Add($"set_to_play {(request.StartingTurn == GoStone.Black ? "black" : "white")}");
        commands.Add("commit_position");
        return commands;
    }

    public InitialPositionVerificationResult VerifySuccessfulResponse() =>
        new(InitialPositionVerificationStatus.Verified, "The engine atomically committed every setup command.");
}
