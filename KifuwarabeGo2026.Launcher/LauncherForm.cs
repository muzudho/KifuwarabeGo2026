namespace KifuwarabeGo2026.Launcher;

using System.Diagnostics;

internal sealed class LauncherForm : Form
{
    private readonly InstalledVersionCatalog _catalog = new();
    private readonly ListView _versions = new();
    private readonly Label _summary = new();

    public LauncherForm()
    {
        Text = "KIFUWARABE GO 2026 Launcher - インストール済みバージョン";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(850, 430);
        Size = new Size(980, 560);

        var description = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(12),
            Text = "使わなくなったバージョンを選択して、ストレージから削除できます。",
        };

        _versions.Dock = DockStyle.Fill;
        _versions.View = View.Details;
        _versions.FullRowSelect = true;
        _versions.MultiSelect = true;
        _versions.HideSelection = false;
        _versions.Columns.Add("製品", 150);
        _versions.Columns.Add("バージョン", 130);
        _versions.Columns.Add("サイズ", 110);
        _versions.Columns.Add("状態", 130);
        _versions.Columns.Add("保存場所", 420);

        var refresh = new Button { Text = "再読み込み", AutoSize = true };
        refresh.Click += (_, _) => RefreshVersions();
        var openFolder = new Button { Text = "保存場所を開く", AutoSize = true };
        openFolder.Click += (_, _) => OpenSelectedFolder();
        var uninstall = new Button { Text = "選択したバージョンをアンインストール", AutoSize = true };
        uninstall.Click += (_, _) => UninstallSelected();

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(8),
        };
        buttons.Controls.Add(uninstall);
        buttons.Controls.Add(openFolder);
        buttons.Controls.Add(refresh);
        buttons.Controls.Add(_summary);

        Controls.Add(_versions);
        Controls.Add(buttons);
        Controls.Add(description);
        RefreshVersions();
    }

    private void RefreshVersions()
    {
        _versions.Items.Clear();
        var installed = _catalog.ReadAll();
        foreach (var version in installed)
        {
            var item = new ListViewItem(version.ProductName) { Tag = version };
            item.SubItems.Add(version.Version);
            item.SubItems.Add(FormatSize(version.SizeInBytes));
            item.SubItems.Add(version.Protection);
            item.SubItems.Add(version.DirectoryPath);
            if (!version.CanUninstall) item.ForeColor = SystemColors.GrayText;
            _versions.Items.Add(item);
        }

        _summary.Text = $"{installed.Count} 個 / {FormatSize(installed.Sum(version => version.SizeInBytes))}";
    }

    private void UninstallSelected()
    {
        var selected = _versions.SelectedItems.Cast<ListViewItem>()
            .Select(item => (InstalledVersion)item.Tag!)
            .ToArray();
        if (selected.Length == 0)
        {
            MessageBox.Show(this, "アンインストールするバージョンを選択してください。", "KifuwarabeGo2026 Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var protectedVersions = selected.Where(version => !version.CanUninstall).ToArray();
        if (protectedVersions.Length > 0)
        {
            MessageBox.Show(this, "現在使用中、実行中、またはロールバック用のバージョンは選択から外してください。", "アンインストールできません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var message = string.Join(Environment.NewLine, selected.Select(version => $"・{version.ProductName} {version.Version} ({FormatSize(version.SizeInBytes)})"));
        if (MessageBox.Show(this, $"次のバージョンを完全に削除します。元に戻せません。\n\n{message}\n\nアンインストールしますか？", "アンインストールの確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        var failures = new List<string>();
        foreach (var version in selected)
        {
            try { _catalog.Uninstall(version); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                failures.Add($"{version.ProductName} {version.Version}: {exception.Message}");
            }
        }
        RefreshVersions();
        if (failures.Count > 0)
            MessageBox.Show(this, "削除できなかったバージョンがあります。実行中のアプリを終了して再試行してください。\n\n" + string.Join(Environment.NewLine, failures), "アンインストール結果", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private void OpenSelectedFolder()
    {
        if (_versions.SelectedItems.Count != 1) return;
        var version = (InstalledVersion)_versions.SelectedItems[0].Tag!;
        if (Directory.Exists(version.DirectoryPath))
            Process.Start(new ProcessStartInfo("explorer.exe", version.DirectoryPath) { UseShellExecute = true });
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; }
        return $"{value:0.#} {units[unit]}";
    }
}
