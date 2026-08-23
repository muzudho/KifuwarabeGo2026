namespace KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.InitialPosition;

using KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;

/// <summary>
/// Supplies host-created artifacts and engine-specific command formatting to a strategy.
/// </summary>
public sealed record InitialPositionStrategyContext(
    string? SgfDocumentPath = null,
    GtpFilePathArgumentStyle FilePathStyle = GtpFilePathArgumentStyle.Auto,
    int? LoadSgfMoveNumber = null);
