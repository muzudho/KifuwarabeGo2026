namespace KifuwarabeGo2026.Gui.Gtp;

using KifuwarabeGo2026.GtpExtensions.Sgf;
using System;
using System.IO;
using System.Text;

/// <summary>
/// Materializes an initial-position SGF document for a local engine and deletes it on disposal.
/// </summary>
public sealed class GtpInitialPositionSgfFile : IDisposable
{
    private GtpInitialPositionSgfFile(string filePath)
    {
        FilePath = filePath;
    }

    public string FilePath { get; }

    public static GtpInitialPositionSgfFile Create(
        InitialPositionDocument document,
        string? rootDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        var directory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026", "GtpInitialPositions")
            : Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(directory);

        var baseName = Path.GetFileNameWithoutExtension(document.SuggestedFileName);
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "initial-position";
        }

        var filePath = Path.Combine(directory, $"{baseName}-{Guid.NewGuid():N}.sgf");
        File.WriteAllText(filePath, document.Content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return new GtpInitialPositionSgfFile(filePath);
    }

    public void Dispose()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }
}
