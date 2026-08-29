namespace KifuwarabeGo2026.FormalAdapter.Sgf.Document;

public sealed class SgfParseException : FormatException
{
    public SgfParseException(string message, int offset)
        : base($"{message} Offset: {offset}.")
    {
        Offset = offset;
    }

    public int Offset { get; }
}
