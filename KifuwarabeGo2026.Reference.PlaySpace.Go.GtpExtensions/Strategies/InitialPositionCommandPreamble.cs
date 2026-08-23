namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Strategies;

using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;
using System.Globalization;

internal static class InitialPositionCommandPreamble
{
    public static List<string> Create(InitialPositionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return
        [
            $"boardsize {request.BoardSize}",
            $"komi {request.Komi.ToString(CultureInfo.InvariantCulture)}",
            "clear_board",
        ];
    }
}
