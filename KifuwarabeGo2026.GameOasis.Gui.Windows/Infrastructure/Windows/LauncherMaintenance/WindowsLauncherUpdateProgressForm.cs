namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class WindowsLauncherUpdateProgressForm : Form
{
    private readonly WindowsLauncherPackageInstaller installer;
    private readonly Label statusLabel = new();

    public WindowsLauncherUpdateProgressForm(WindowsLauncherPackageInstaller installer)
    {
        this.installer = installer;
        Text = "ランチャーを更新しています";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(620, 150);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ControlBox = false;
        Font = new Font("Meiryo UI", 10f);
        statusLabel.SetBounds(28, 30, 564, 74);
        statusLabel.Text = "最新版を確認しています…";
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        Controls.Add(statusLabel);
        Shown += async (_, _) => await RunUpdateAsync();
    }

    public LauncherInstallResult? Result { get; private set; }

    public string FailureMessage { get; private set; } = string.Empty;

    private async Task RunUpdateAsync()
    {
        try
        {
            var progress = new Progress<string>(message => statusLabel.Text = message);
            Result = await installer.InstallLatestAsync(progress);
            DialogResult = DialogResult.OK;
        }
        catch (Exception exception)
        {
            FailureMessage = exception.Message;
            DialogResult = DialogResult.Abort;
        }
        Close();
    }
}
