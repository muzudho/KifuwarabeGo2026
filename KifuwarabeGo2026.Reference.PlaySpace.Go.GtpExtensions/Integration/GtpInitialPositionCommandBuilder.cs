namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Integration;

using KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Strategies;
using System.Collections.Generic;

/// <summary>
/// Converts a Match initial position into commands for a local GTP engine.
/// </summary>
public static class GtpInitialPositionCommandBuilder
{
    public static IReadOnlyList<string> Build(MatchSnapshot snapshot, decimal komi)
    {
        var request = InitialPositionRequest.FromSnapshot(snapshot, komi);
        return SequentialPlayStrategy.Instance.BuildCommands(request);
    }
}
