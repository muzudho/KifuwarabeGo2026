namespace KifuwarabeGo2026.Application;

public sealed record CgosConnectionProfile(
    string DisplayName,
    string Host,
    int Port,
    string Role,
    string Note);
