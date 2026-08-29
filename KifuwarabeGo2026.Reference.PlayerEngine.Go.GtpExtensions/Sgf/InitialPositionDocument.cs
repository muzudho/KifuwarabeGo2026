namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Sgf;

/// <summary>
/// Contains a host-independent SGF document that can be materialized by a GUI or server.
/// </summary>
public sealed record InitialPositionDocument(string SuggestedFileName, string Content)
{
    public const string EncodingName = "UTF-8";
}
