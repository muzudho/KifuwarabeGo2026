namespace KifuwarabeGo2026.GtpExtensions.InitialPosition;

/// <summary>
/// Represents one host-materialized document that is deleted when disposed.
/// </summary>
public interface IInitialPositionDocumentLease : IAsyncDisposable
{
    string FilePath { get; }
}
