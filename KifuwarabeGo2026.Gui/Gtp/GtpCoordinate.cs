namespace KifuwarabeGo2026.Gui.Gtp;

using KifuwarabeGo2026.Shared.Domain;

public static class GtpCoordinate
{
    public static string FormatVertex(GoPoint point, int boardSize) =>
        global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.FormatVertex(point, boardSize);

    public static bool TryParseVertex(string text, int boardSize, out GoPoint point) =>
        global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.TryParseVertex(text, boardSize, out point);

    public static bool IsPass(string text) => global::KifuwarabeGo2026.GtpExtensions.Protocol.GtpCoordinate.IsPass(text);
}
