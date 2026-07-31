namespace KifuwarabeGo2026.GtpExtensions.Strategies;

using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.GtpExtensions.Protocol;
using KifuwarabeGo2026.Shared.Domain;
using System.Globalization;

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

    public bool CanApply(InitialPositionRequest request, InitialPositionClassification classification)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(classification);
        return classification.Kind != InitialPositionKind.HistorySensitivePosition;
    }

    public IReadOnlyList<string> BuildCommands(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var commands = new List<string>
        {
            $"boardsize {request.BoardSize}",
            $"komi {request.Komi.ToString(CultureInfo.InvariantCulture)}",
            "clear_board",
        };

        foreach (var setupStone in request.SetupStones)
        {
            var color = setupStone.Stone == GoStone.Black ? "black" : "white";
            var vertex = GtpCoordinate.FormatVertex(setupStone.Point, request.BoardSize);
            commands.Add($"play {color} {vertex}");
        }

        return commands;
    }
}
