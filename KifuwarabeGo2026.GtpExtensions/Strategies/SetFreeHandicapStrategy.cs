namespace KifuwarabeGo2026.GtpExtensions.Strategies;

using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Protocol;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Reproduces explicitly positioned black setup stones with set_free_handicap.
/// </summary>
public sealed class SetFreeHandicapStrategy : IInitialPositionStrategy
{
    public static SetFreeHandicapStrategy Instance { get; } = new();

    private SetFreeHandicapStrategy()
    {
    }

    public InitialPositionMethod Method => InitialPositionMethod.SetFreeHandicap;

    public string DisplayName => "自由置き碁（set_free_handicap）";

    public IReadOnlyList<string> RequiredCommands { get; } = ["set_free_handicap"];

    public bool CanApply(InitialPositionRequest request, InitialPositionClassification classification)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(classification);
        return (classification.Kind is InitialPositionKind.StandardFixedHandicap or InitialPositionKind.SpecifiedBlackHandicap) &&
            classification.BlackStoneCount > 0 &&
            classification.WhiteStoneCount == 0 &&
            request.StartingTurn == GoStone.White;
    }

    public IReadOnlyList<string> BuildCommands(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var classification = InitialPositionClassifier.Classify(request);
        if (!CanApply(request, classification))
        {
            throw new InvalidOperationException("set_free_handicap cannot reproduce this initial position.");
        }

        var vertices = request.SetupStones
            .Select(setupStone => GtpCoordinate.FormatVertex(setupStone.Point, request.BoardSize));
        var commands = InitialPositionCommandPreamble.Create(request);
        commands.Add($"set_free_handicap {string.Join(' ', vertices)}");
        return commands;
    }
}
