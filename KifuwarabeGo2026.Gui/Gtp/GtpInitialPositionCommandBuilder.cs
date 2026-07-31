namespace KifuwarabeGo2026.Gui.Gtp;

using KifuwarabeGo2026.Match;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Converts a Match initial position into commands for a local GTP engine.
/// </summary>
public static class GtpInitialPositionCommandBuilder
{
    public static IReadOnlyList<string> Build(MatchSnapshot snapshot, decimal komi)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var commands = new List<string>
        {
            $"boardsize {snapshot.BoardSize}",
            $"komi {komi.ToString(CultureInfo.InvariantCulture)}",
            "clear_board",
        };

        foreach (var setupStone in snapshot.SetupStones)
        {
            var color = setupStone.Stone == GoStone.Black ? "black" : "white";
            var vertex = GtpCoordinate.FormatVertex(setupStone.Point, snapshot.BoardSize);
            commands.Add($"play {color} {vertex}");
        }

        return commands;
    }
}
