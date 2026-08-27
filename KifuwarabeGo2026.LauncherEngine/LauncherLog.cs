namespace KifuwarabeGo2026.LauncherEngine;

internal sealed class LauncherLog(LauncherPaths paths)
{
    private readonly object _gate = new();
    public string FilePath => paths.LogFile;
    public void Write(string message)
    {
        lock (_gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(paths.LogFile)!);
            File.AppendAllText(paths.LogFile, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
    }
}
