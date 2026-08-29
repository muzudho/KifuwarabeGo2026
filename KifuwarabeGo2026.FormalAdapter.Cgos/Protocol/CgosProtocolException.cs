namespace KifuwarabeGo2026.FormalAdapter.Cgos.Protocol;

public sealed class CgosProtocolException : FormatException
{
    public CgosProtocolException(string message, string line) : base($"{message} Line: '{line}'.")
    {
        Line = line;
    }

    public string Line { get; }
}
