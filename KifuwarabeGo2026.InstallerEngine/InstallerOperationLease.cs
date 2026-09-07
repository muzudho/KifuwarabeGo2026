namespace KifuwarabeGo2026.InstallerEngine;

/// <summary>Serializes package mutation and launch across management and launch-only processes.</summary>
internal sealed class InstallerOperationLease : IDisposable
{
    private readonly FileStream stream;
    private InstallerOperationLease(FileStream stream) => this.stream = stream;
    public static InstallerOperationLease Acquire(InstallerPaths paths)
    {
        Directory.CreateDirectory(paths.Root);
        try
        {
            return new(new FileStream(Path.Combine(paths.Root, "installer-operation.lock"),
                FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException exception)
        {
            throw new IOException("インストーラーが更新または管理処理を実行中です。完了後にもう一度［ロビーを起動］を押してください。", exception);
        }
    }
    public void Dispose() => stream.Dispose();
}
