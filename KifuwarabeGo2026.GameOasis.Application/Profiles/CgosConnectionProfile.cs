namespace KifuwarabeGo2026.GameOasis.Application.Profiles;

/// <summary>Persistent connection destination used by CGOS clients.</summary>
public sealed record CgosConnectionProfile(
    string DisplayName,
    string Host,
    int Port,
    string Round,
    string Note)
{
    public string Id { get; init; } = "";
    public ConnectionProfileKind Kind { get; init; } = ConnectionProfileKind.Cgos;
    public string EndpointKey { get; init; } = "";
    public string Event { get; init; } = "";
}

public enum ConnectionProfileKind { Cgos }
