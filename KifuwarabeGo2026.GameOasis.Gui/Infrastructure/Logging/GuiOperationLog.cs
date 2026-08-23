namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Logging;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using System;
using System.IO;
using System.Text;

/// <summary>Records user actions and application-driven transitions for one application session.</summary>
public static class GuiOperationLog
{
    private static readonly object SyncRoot = new();
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);
    private const long MaximumFileBytes = 5 * 1024 * 1024;
    private static string _baseFilePath = "";
    private static int _partNumber = 1;

    public static string FilePath { get; private set; } = "";

    public static void Initialize(DateTimeOffset startedAt)
    {
        lock (SyncRoot)
        {
            var directory = Path.Combine(ApplicationSettings.Current.LogRootDirectory, "Gui");
            Directory.CreateDirectory(directory);
            _baseFilePath = Path.Combine(directory, $"gui-operation-{startedAt:yyyyMMdd-HHmmss-fff}");
            _partNumber = 1;
            FilePath = _baseFilePath + ".log";
            File.WriteAllText(FilePath,
                $"# Kifuwarabe Go 2026 GUI operation log{Environment.NewLine}" +
                $"# session-started {startedAt:O}{Environment.NewLine}" +
                $"# Entries marked USER describe input; APP entries describe automatic behavior.{Environment.NewLine}", Utf8);
        }
    }

    public static void User(string action, string detail = "") => Write("USER", action, detail);

    public static void App(string action, string detail = "") => Write("APP", action, detail);

    public static void Close()
    {
        if (!string.IsNullOrWhiteSpace(FilePath))
            App("Application session ended");
    }

    private static void Write(string source, string action, string detail)
    {
        lock (SyncRoot)
        {
            if (string.IsNullOrWhiteSpace(FilePath))
                Initialize(DateTimeOffset.Now);

            var suffix = string.IsNullOrWhiteSpace(detail) ? "" : " | " + detail.Replace("\r", " ").Replace("\n", " ");
            var entry = $"[{DateTimeOffset.Now:O}] [{source}] {action}{suffix}{Environment.NewLine}";
            if (File.Exists(FilePath) && new FileInfo(FilePath).Length + Utf8.GetByteCount(entry) > MaximumFileBytes)
            {
                _partNumber++;
                FilePath = $"{_baseFilePath}-part{_partNumber:000}.log";
                File.WriteAllText(FilePath,
                    $"# Kifuwarabe Go 2026 GUI operation log (continued, part {_partNumber}){Environment.NewLine}", Utf8);
            }
            File.AppendAllText(FilePath, entry, Utf8);
        }
    }
}
