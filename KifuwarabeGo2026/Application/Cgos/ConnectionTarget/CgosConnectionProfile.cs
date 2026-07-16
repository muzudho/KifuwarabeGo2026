namespace KifuwarabeGo2026.Application.Cgos.ConnectionTarget;

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

public enum CgosConnectionFlowKind
{
    ProfileSelection,
    ConnectionStart,
}
