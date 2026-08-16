namespace KifuwarabeGo2026.Gui.Application.Updates;

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>GUI内の更新操作を、固定の共通ランチャーを開く操作へ橋渡しします。</summary>
public static class GuiReleaseUpdater
{
    public const string ManualMigrationMessage =
        "共通ランチャーが見つかりません。GitHub Releasesから最新版を手動で再インストールし、KifuwarabeGo2026.Launcher.exeを起動してください。";

    public static Task<GuiReleaseUpdateResult> DownloadLatestAndStartAsync(
        Action<GuiReleaseUpdateProgress>? reportProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Report(GuiReleaseUpdateStep.CheckingRelease, "共通ランチャーを探しています…", reportProgress);
        var launcher = FindLauncher();
        if (launcher is null) throw new FileNotFoundException(ManualMigrationMessage);

        Report(GuiReleaseUpdateStep.StartingUpdatedGui, "共通ランチャーを起動しています…", reportProgress);
        var process = Process.Start(new ProcessStartInfo(launcher)
        {
            WorkingDirectory = Path.GetDirectoryName(launcher)!,
            UseShellExecute = true,
        });
        if (process is null) throw new InvalidOperationException("共通ランチャーを起動できませんでした。");
        Report(GuiReleaseUpdateStep.Completed, "共通ランチャーを起動しました。ランチャーからGUIを更新してください。", reportProgress);
        return Task.FromResult(GuiReleaseUpdateResult.Started("Launcher"));
    }

    private static string? FindLauncher()
    {
        var baseDirectory = AppContext.BaseDirectory;
        string[] candidates =
        [
            Path.Combine(baseDirectory, "KifuwarabeGo2026.Launcher.exe"),
            Path.Combine(baseDirectory, "Launcher", "KifuwarabeGo2026.Launcher.exe"),
            Path.Combine(Directory.GetParent(baseDirectory)?.FullName ?? baseDirectory, "KifuwarabeGo2026.Launcher.exe"),
        ];
        return Array.Find(candidates, File.Exists);
    }

    private static void Report(GuiReleaseUpdateStep step, string message, Action<GuiReleaseUpdateProgress>? reportProgress) =>
        reportProgress?.Invoke(new GuiReleaseUpdateProgress(step, message));
}

public enum GuiReleaseUpdateStep
{
    CheckingRelease,
    DownloadingPackage,
    ExtractingPackage,
    VerifyingPackage,
    StartingUpdatedGui,
    Completed,
}

public sealed record GuiReleaseUpdateProgress(GuiReleaseUpdateStep Step, string Message);

public sealed record GuiReleaseUpdateResult(bool DidStartUpdatedGui, string Message)
{
    public static GuiReleaseUpdateResult Started(string tag) => new(true, $"{tag} STARTED.");
    public static GuiReleaseUpdateResult AlreadyLatest(string tag) => new(false, $"ALREADY ON THE LATEST RELEASE ({tag}).");
}
