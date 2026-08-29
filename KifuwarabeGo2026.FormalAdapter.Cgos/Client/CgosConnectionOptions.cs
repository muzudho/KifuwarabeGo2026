namespace KifuwarabeGo2026.FormalAdapter.Cgos.Client;

public sealed record CgosConnectionOptions(
    string Host,
    int Port,
    TimeSpan? ConnectTimeout = null,
    TimeSpan? FirstServerLineTimeout = null);

public sealed record CgosCredentials(string Username, string Password);
