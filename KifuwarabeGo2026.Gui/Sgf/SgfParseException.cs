namespace KifuwarabeGo2026.Gui.Sgf;

using System;

public sealed class SgfParseException : FormatException
{
    public SgfParseException(string message)
        : base(message)
    {
    }
}
