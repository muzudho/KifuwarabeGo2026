namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Strategies;

using System.Globalization;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

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
        "kfw-begin-position", "kfw-add-black", "kfw-add-white", "kfw-set-to-play", "kfw-commit-position", "kfw-abort-position",
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
            "kfw-begin-position",
        };
        foreach (var setupStone in request.SetupStones)
        {
            var command = setupStone.Stone == GoStone.Black ? "kfw-add-black" : "kfw-add-white";
            commands.Add($"{command} {GtpCoordinate.FormatVertex(setupStone.Point, request.BoardSize)}");
        }

        commands.Add($"kfw-set-to-play {(request.StartingTurn == GoStone.Black ? "black" : "white")}");
        commands.Add("kfw-commit-position");
        return commands;
    }

    public InitialPositionVerificationResult VerifySuccessfulResponse() =>
        new(InitialPositionVerificationStatus.Verified, "The engine atomically committed every setup command.");
}
