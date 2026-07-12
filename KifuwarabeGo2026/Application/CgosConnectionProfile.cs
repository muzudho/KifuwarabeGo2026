namespace KifuwarabeGo2026.Application;

public sealed record CgosConnectionProfile(
    string DisplayName,
    string Host,
    int Port,
    string Role,
    string Note);

public enum CgosConnectionProfileEditField
{
    DisplayName,
    Host,
    Port,
    Role,
    Note,
}
