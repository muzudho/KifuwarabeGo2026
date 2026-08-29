namespace KifuwarabeGo2026.FormalAdapter.Gtp.PlayerEngine;

using System.Text;

public interface IGtpSgfFileStore
{
    ValueTask<IGtpSgfFileLease> MaterializeAsync(string content, CancellationToken cancellationToken = default);
}

public interface IGtpSgfFileLease : IAsyncDisposable
{
    string FilePath { get; }
}

/// <summary>SGFを一時ファイル化し、リース破棄時にそのファイルだけを削除します。</summary>
public sealed class TemporaryGtpSgfFileStore(string? rootDirectory = null) : IGtpSgfFileStore
{
    public static TemporaryGtpSgfFileStore Shared { get; } = new();

    public async ValueTask<IGtpSgfFileLease> MaterializeAsync(string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var directory = string.IsNullOrWhiteSpace(rootDirectory)
            ? Path.Combine(Path.GetTempPath(), "KifuwarabeGo2026", "GtpInitialPositions")
            : Path.GetFullPath(rootDirectory);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"position-{Guid.NewGuid():N}.sgf");
        await File.WriteAllTextAsync(path, content, new UTF8Encoding(false), cancellationToken);
        return new Lease(path);
    }

    private sealed class Lease(string filePath) : IGtpSgfFileLease
    {
        public string FilePath { get; } = filePath;
        public ValueTask DisposeAsync()
        {
            if (File.Exists(FilePath)) File.Delete(FilePath);
            return ValueTask.CompletedTask;
        }
    }
}
