namespace KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.InitialPosition;

using System.Collections.ObjectModel;

/// <summary>
/// Contains a structured position-verification result suitable for later GUI diagnosis.
/// </summary>
public sealed class InitialPositionVerificationResult
{
    private readonly ReadOnlyCollection<string> _expectedVertices;
    private readonly ReadOnlyCollection<string> _actualVertices;

    public InitialPositionVerificationResult(
        InitialPositionVerificationStatus status,
        string detail,
        IEnumerable<string>? expectedVertices = null,
        IEnumerable<string>? actualVertices = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detail);
        Status = status;
        Detail = detail;
        _expectedVertices = Array.AsReadOnly(expectedVertices?.ToArray() ?? []);
        _actualVertices = Array.AsReadOnly(actualVertices?.ToArray() ?? []);
    }

    public InitialPositionVerificationStatus Status { get; }

    public string Detail { get; }

    public IReadOnlyList<string> ExpectedVertices => _expectedVertices;

    public IReadOnlyList<string> ActualVertices => _actualVertices;
}
