namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.GameOasis.Gui.Application.LauncherMaintenance;
using System;
using System.IO;
using System.Windows.Forms;

public sealed class WindowsLauncherMaintenanceService : ILauncherMaintenanceService
{
    public bool IsSupported => true;

    public string UnsupportedReason => string.Empty;

    public void ShowInteractiveUpdater()
    {
        var answer = MessageBox.Show(
            "ランチャーを最新版にします。\n\n更新中はランチャーを終了しておいてください。\n続けますか？",
            "ランチャーを最新にします",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
        {
            ShowNotUpdatedMessage();
            return;
        }

        var installer = new WindowsLauncherPackageInstaller();
        LauncherInstallResult result;
        try
        {
            using var progress = new WindowsLauncherUpdateProgressForm(installer);
            if (progress.ShowDialog() != DialogResult.OK || progress.Result is null)
            {
                ShowNotUpdatedMessage(progress.FailureMessage);
                return;
            }
            result = progress.Result;
        }
        catch (Exception exception)
        {
            ShowNotUpdatedMessage(exception.Message);
            return;
        }

        var createShortcut = MessageBox.Show(
            $"ランチャー v{result.Version} に更新しました。\n\nショートカットをデスクトップに作りますか？",
            "ランチャー更新完了",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (createShortcut == DialogResult.Yes)
        {
            try
            {
                var shortcutPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                    "Kifuwarabe Go 2026 Launcher.lnk");
                new WindowsShellLinkService().CreateOrReplaceLauncherShortcut(shortcutPath, result.ExecutablePath);
                MessageBox.Show(
                    $"デスクトップにショートカットを作りました。\n\n{shortcutPath}",
                    "ショートカット作成完了",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    $"ランチャーの更新は完了しましたが、ショートカットを作れませんでした。\n\n{exception.Message}",
                    "ショートカット作成失敗",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }

    private static void ShowNotUpdatedMessage(string? detail = null)
    {
        var message = "更新はしていません。詳しくは https://github.com/muzudho/KifuwarabeGo2026 を確認してください。";
        if (!string.IsNullOrWhiteSpace(detail)) message += "\n\n" + detail;
        MessageBox.Show(message, "ランチャー更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
