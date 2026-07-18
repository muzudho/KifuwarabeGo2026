namespace KifuwarabeGo2026.Infrastructure.Logging;

using System;
using System.IO;
using System.Text;

/// <summary>
/// GTP通信ログとは別に、アプリケーション側で発生したエラーを起動単位で記録します。
/// </summary>
public static class ApplicationErrorLog
{
    private static readonly object SyncRoot = new();

    public static string FilePath { get; private set; } = "";

    public static void Initialize(DateTimeOffset startedAt)
    {
        lock (SyncRoot)
        {
            var directory = Path.Combine(AppContext.BaseDirectory, "logs", "errors");
            Directory.CreateDirectory(directory);
            FilePath = Path.Combine(directory, $"application-error-{startedAt:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(
                FilePath,
                $"# Kifuwarabe Go 2026 application error log{Environment.NewLine}" +
                $"# started {startedAt:O}{Environment.NewLine}",
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }

    public static void Write(string category, string message, Exception? exception = null)
    {
        lock (SyncRoot)
        {
            if (string.IsNullOrWhiteSpace(FilePath))
            {
                Initialize(DateTimeOffset.Now);
            }

            var entry = new StringBuilder()
                .AppendLine()
                .Append('[').Append(DateTimeOffset.Now.ToString("O")).Append("] ").AppendLine(category)
                .AppendLine(message);
            if (exception is not null)
            {
                entry.AppendLine(exception.ToString());
            }

            File.AppendAllText(FilePath, entry.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
    }
}
