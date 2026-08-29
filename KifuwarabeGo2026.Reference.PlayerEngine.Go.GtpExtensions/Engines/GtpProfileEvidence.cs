namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;

/// <summary>
/// Distinguishes a safe fallback, documentation-based expectations, and locally verified behavior.
/// </summary>
public enum GtpProfileEvidence
{
    ConservativeFallback,
    OfficialDocumentationOnly,
    BundledEngineVerified,
}
