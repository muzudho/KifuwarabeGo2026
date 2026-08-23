namespace KifuwarabeGo2026.Reference.Communication.Gtp.Protocol;

/// <summary>
/// Formats command arguments while rejecting line injection.
/// </summary>
public static class GtpCommandArgument
{
    public static string FormatFilePath(string path, GtpFilePathArgumentStyle style)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.IndexOfAny(['\r', '\n', '\0']) >= 0)
        {
            throw new ArgumentException("A GTP file path cannot contain a line break or NUL.", nameof(path));
        }

        if (path.Contains('"', StringComparison.Ordinal))
        {
            throw new ArgumentException("A GTP file path containing a double quote is not supported.", nameof(path));
        }

        var resolvedStyle = style == GtpFilePathArgumentStyle.Auto
            ? path.Any(char.IsWhiteSpace)
                ? GtpFilePathArgumentStyle.DoubleQuoted
                : GtpFilePathArgumentStyle.Unquoted
            : style;

        return resolvedStyle switch
        {
            GtpFilePathArgumentStyle.Unquoted => path,
            GtpFilePathArgumentStyle.DoubleQuoted => $"\"{path}\"",
            _ => throw new ArgumentOutOfRangeException(nameof(style), style, "Unknown GTP file path style."),
        };
    }
}
