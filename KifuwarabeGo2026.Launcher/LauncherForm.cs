namespace KifuwarabeGo2026.Launcher;

using System.Diagnostics;

internal sealed class LauncherForm : Form
{
    private readonly LauncherPaths _paths = new();
    private readonly LauncherSettingsStore _settings;
    private readonly InstalledVersionCatalog _catalog;
    private readonly ProductLauncher _productLauncher;
    private readonly LauncherUpdateService _updates;
    private readonly ListView _versions = new();
    private readonly Label _guiVersion = new() { AutoSize = true };
    private readonly Label _engineVersion = new() { AutoSize = true };
    private readonly Label _status = new() { AutoSize = true, Text = "準備完了" };
    private readonly Label _summary = new() { AutoSize = true };
    private readonly List<Button> _updateButtons = [];

    public LauncherForm()
    {
        _settings = new LauncherSettingsStore(_paths);
        var log = new LauncherLog(_paths);
        _catalog = new InstalledVersionCatalog();
        _productLauncher = new ProductLauncher(_paths, _settings, log);
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
        _updates = new LauncherUpdateService(new GitHubReleaseClient(http), new PackageInstaller(_paths, http, log), _settings, log);

        Text = "KIFUWARABE GO 2026 Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(900, 600);
        Size = new Size(1050, 700);

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(14) };
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        main.Controls.Add(new Label { Text = "KIFUWARABE GO 2026", AutoSize = true, Font = new Font(Font.FontFamily, 18, FontStyle.Bold) });
        main.Controls.Add(BuildProductPanel());
        main.Controls.Add(BuildVersionList());
        main.Controls.Add(BuildFooter());
        Controls.Add(main);
        RefreshAll();
    }

    private Control BuildProductPanel()
    {
        var panel = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 5, Padding = new Padding(0, 14, 0, 14) };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        panel.Controls.Add(new Label { Text = "GUI", AutoSize = true }, 0, 0);
        panel.Controls.Add(_guiVersion, 1, 0);
        panel.Controls.Add(MakeButton("START", (_, _) => ShowResult(_productLauncher.StartGui().Message)), 2, 0);
        panel.Controls.Add(MakeUpdateButton("GUI UPDATE", LauncherProduct.Gui), 3, 0);
        panel.Controls.Add(new Label { Text = "ENGINE", AutoSize = true }, 0, 1);
        panel.Controls.Add(_engineVersion, 1, 1);
        panel.Controls.Add(MakeButton("OPEN FOLDER", (_, _) => OpenCurrentEngine()), 2, 1);
        panel.Controls.Add(MakeUpdateButton("ENGINE UPDATE", LauncherProduct.Engine), 3, 1);
        panel.Controls.Add(MakeButton("CHECK ALL UPDATES", async (_, _) => { await UpdateAsync(LauncherProduct.Gui); await UpdateAsync(LauncherProduct.Engine); }), 4, 0);
        return panel;
    }

    private Control BuildVersionList()
    {
        _versions.Dock = DockStyle.Fill;
        _versions.View = View.Details;
        _versions.FullRowSelect = true;
        _versions.MultiSelect = true;
        _versions.Columns.Add("製品", 150);
        _versions.Columns.Add("バージョン", 120);
        _versions.Columns.Add("サイズ", 100);
        _versions.Columns.Add("状態", 130);
        _versions.Columns.Add("保存場所", 500);
        return _versions;
    }

    private Control BuildFooter()
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 8, 0, 0) };
        panel.Controls.Add(MakeButton("選択したバージョンをアンインストール", (_, _) => UninstallSelected()));
        panel.Controls.Add(MakeButton("保存場所を開く", (_, _) => OpenSelectedFolder()));
        panel.Controls.Add(MakeButton("再読み込み", (_, _) => RefreshAll()));
        panel.Controls.Add(MakeButton("ログを開く", (_, _) => OpenLog()));
        panel.Controls.Add(_summary);
        panel.Controls.Add(_status);
        return panel;
    }

    private Button MakeButton(string text, EventHandler action) { var button = new Button { Text = text, AutoSize = true }; button.Click += action; return button; }
    private Button MakeUpdateButton(string text, LauncherProduct product) { var button = MakeButton(text, async (_, _) => await UpdateAsync(product)); _updateButtons.Add(button); return button; }

    private async Task UpdateAsync(LauncherProduct product)
    {
        SetUpdating(true);
        try { var version = await _updates.UpdateAsync(product, message => BeginInvoke(() => _status.Text = message)); ShowResult($"{product.DisplayName()} v{version} の更新が完了しました。"); }
        catch (Exception exception) { ShowResult($"更新に失敗しました。\n{exception.Message}\n\nログ: {_paths.LogFile}", MessageBoxIcon.Error); }
        finally { SetUpdating(false); RefreshAll(); }
    }

    private void SetUpdating(bool updating) { foreach (var button in _updateButtons) button.Enabled = !updating; _status.Text = updating ? "更新中…" : "準備完了"; }

    private void RefreshAll()
    {
        var settings = _settings.Load();
        _guiVersion.Text = VersionLabel(settings.GuiCurrentVersion);
        _engineVersion.Text = VersionLabel(settings.EngineCurrentVersion);
        _versions.Items.Clear();
        var installed = _catalog.ReadAll();
        foreach (var version in installed)
        {
            var item = new ListViewItem(version.ProductName) { Tag = version };
            item.SubItems.Add(version.Version); item.SubItems.Add(FormatSize(version.SizeInBytes)); item.SubItems.Add(version.Protection); item.SubItems.Add(version.DirectoryPath);
            if (!version.CanUninstall) item.ForeColor = SystemColors.GrayText;
            _versions.Items.Add(item);
        }
        _summary.Text = $"{installed.Count} 個 / {FormatSize(installed.Sum(item => item.SizeInBytes))}";
    }

    private void OpenCurrentEngine()
    {
        var directory = _productLauncher.CurrentDirectory(LauncherProduct.Engine);
        if (directory is null || !Directory.Exists(directory)) { ShowResult("Engineがインストールされていません。", MessageBoxIcon.Information); return; }
        Process.Start(new ProcessStartInfo("explorer.exe", directory) { UseShellExecute = true });
    }

    private void UninstallSelected()
    {
        var selected = _versions.SelectedItems.Cast<ListViewItem>().Select(item => (InstalledVersion)item.Tag!).ToArray();
        if (selected.Length == 0) { ShowResult("アンインストールするバージョンを選択してください。", MessageBoxIcon.Information); return; }
        if (selected.Any(item => !item.CanUninstall)) { ShowResult("現在版、直前版、または実行中の版はアンインストールできません。", MessageBoxIcon.Warning); return; }
        var details = string.Join(Environment.NewLine, selected.Select(item => $"・{item.ProductName} {item.Version} ({FormatSize(item.SizeInBytes)})"));
        if (MessageBox.Show(this, $"次のバージョンを完全に削除します。\n\n{details}", "アンインストールの確認", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
        var failures = new List<string>();
        foreach (var item in selected) try { _catalog.Uninstall(item); } catch (Exception exception) { failures.Add($"{item.ProductName} {item.Version}: {exception.Message}"); }
        RefreshAll();
        if (failures.Count > 0) ShowResult(string.Join(Environment.NewLine, failures), MessageBoxIcon.Error);
    }

    private void OpenSelectedFolder() { if (_versions.SelectedItems.Count != 1) return; var item = (InstalledVersion)_versions.SelectedItems[0].Tag!; if (Directory.Exists(item.DirectoryPath)) Process.Start(new ProcessStartInfo("explorer.exe", item.DirectoryPath) { UseShellExecute = true }); }
    private void OpenLog() { Directory.CreateDirectory(Path.GetDirectoryName(_paths.LogFile)!); if (!File.Exists(_paths.LogFile)) File.WriteAllText(_paths.LogFile, ""); Process.Start(new ProcessStartInfo(_paths.LogFile) { UseShellExecute = true }); }
    private void ShowResult(string message, MessageBoxIcon icon = MessageBoxIcon.Information) => MessageBox.Show(this, message, "KifuwarabeGo2026 Launcher", MessageBoxButtons.OK, icon);
    private static string VersionLabel(string? version) => string.IsNullOrWhiteSpace(version) ? "未インストール" : "v" + version.TrimStart('v', 'V');
    private static string FormatSize(long bytes) { string[] units = ["B", "KB", "MB", "GB", "TB"]; var value = (double)bytes; var unit = 0; while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; } return $"{value:0.#} {units[unit]}"; }
}
