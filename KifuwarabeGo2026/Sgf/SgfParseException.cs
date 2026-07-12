namespace KifuwarabeGo2026.Sgf;

using System;

public sealed class SgfParseException : FormatException
{
    public SgfParseException(string message)
        : base(message)
    {
    }
}
