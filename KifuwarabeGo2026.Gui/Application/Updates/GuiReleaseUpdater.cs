namespace KifuwarabeGo2026.Gui.Application.Updates;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>GUI内の更新操作を、固定の共通ランチャーを開く操作へ橋渡しします。</summary>
public static class GuiReleaseUpdater
{
    private const string ReleasesUrl = "https://github.com/muzudho/KifuwarabeGo2026/releases/latest";
    public const string ManualMigrationMessage =
        "共通ランチャーが見つかりません。自動バージョン更新の仕様が変わっている可能性があります。GitHub Releasesを確認してください。\n" + ReleasesUrl;

    public static async Task<GuiReleaseUpdateResult> OpenLauncherAsync(
        Action<GuiReleaseUpdateProgress>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Report(GuiReleaseUpdateStep.CheckingRelease, "共通ランチャーを探しています…", reportProgress);
        var launcher = FindLauncher();
        if (launcher is null) throw new FileNotFoundException(ManualMigrationMessage);

        Report(GuiReleaseUpdateStep.StartingLauncher, "共通ランチャーを前面に起動しています…", reportProgress);
        var process = Process.Start(new ProcessStartInfo(launcher)
        {
            WorkingDirectory = Path.GetDirectoryName(launcher)!,
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Normal,
        });
        if (process is null) throw new InvalidOperationException("共通ランチャーを起動できませんでした。");

        // Process.Startの成功だけでGUIを閉じると、ランチャーが起動直後に失敗した場合も
        // 利用者にはGUIだけが消えたように見えます。短時間だけ生存を確認します。
        await Task.Delay(750, cancellationToken).ConfigureAwait(false);
        process.Refresh();
        if (process.HasExited)
            throw new InvalidOperationException(
                "共通ランチャーが起動直後に終了しました。自動バージョン更新の仕様が変わっている可能性があります。GitHub Releasesを確認してください。\n" + ReleasesUrl);

        Report(GuiReleaseUpdateStep.Completed, "共通ランチャーを前面に起動しました。", reportProgress);
        return GuiReleaseUpdateResult.Started();
    }

    private static string? FindLauncher()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidates = new System.Collections.Generic.List<string>();
        var inheritedLauncherPath = Environment.GetEnvironmentVariable("KIFUWARABE_LAUNCHER_PATH");
        if (!string.IsNullOrWhiteSpace(inheritedLauncherPath)) candidates.Add(inheritedLauncherPath);
        candidates.AddRange([
            Path.Combine(baseDirectory, "KifuwarabeGo2026.Launcher.exe"),
            Path.Combine(baseDirectory, "Launcher", "KifuwarabeGo2026.Launcher.exe"),
        ]);
        for (var directory = Directory.GetParent(baseDirectory); directory is not null; directory = directory.Parent)
            candidates.Add(Path.Combine(directory.FullName, "KifuwarabeGo2026.Launcher.exe"));
        return candidates.Find(File.Exists);
    }

    private static void Report(GuiReleaseUpdateStep step, string message, Action<GuiReleaseUpdateProgress>? reportProgress) =>
        reportProgress?.Invoke(new GuiReleaseUpdateProgress(step, message));
}

public enum GuiReleaseUpdateStep
{
    CheckingRelease,
    StartingLauncher,
    Completed,
}

public sealed record GuiReleaseUpdateProgress(GuiReleaseUpdateStep Step, string Message);

public sealed record GuiReleaseUpdateResult(bool DidStartLauncher, string Message)
{
    public static GuiReleaseUpdateResult Started() => new(true, "LAUNCHER STARTED.");
}
