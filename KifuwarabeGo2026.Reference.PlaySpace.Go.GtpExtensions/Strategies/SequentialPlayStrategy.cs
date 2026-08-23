namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Strategies;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>
/// Reproduces setup stones as sequential play commands for compatibility with existing behavior.
/// </summary>
public sealed class SequentialPlayStrategy : IInitialPositionStrategy
{
    public static SequentialPlayStrategy Instance { get; } = new();

    private SequentialPlayStrategy()
    {
    }

    public InitialPositionMethod Method => InitialPositionMethod.SequentialPlay;

    public string DisplayName => "連続着手による互換設定";

    public IReadOnlyList<string> RequiredCommands { get; } = ["play"];

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

        var commands = InitialPositionCommandPreamble.Create(request);

        foreach (var setupStone in request.SetupStones)
        {
            var color = setupStone.Stone == GoStone.Black ? "black" : "white";
            var vertex = GtpCoordinate.FormatVertex(setupStone.Point, request.BoardSize);
            commands.Add($"play {color} {vertex}");
        }

        return commands;
    }
}
