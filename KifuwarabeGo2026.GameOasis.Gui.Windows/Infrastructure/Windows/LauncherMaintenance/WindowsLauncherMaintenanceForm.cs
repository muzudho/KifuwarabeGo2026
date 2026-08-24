namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

internal sealed class WindowsLauncherMaintenanceForm : Form
{
    private readonly WindowsLauncherPackageInstaller installer;
    private readonly WindowsLauncherShortcutStore store;
    private readonly WindowsShellLinkService shellLinks;
    private readonly ListView shortcutList = new();
    private readonly Label installedVersionLabel = new();
    private readonly Label targetLabel = new();
    private readonly Label statusLabel = new();
    private readonly Button installButton = new();
    private readonly Button addButton = new();
    private readonly Button removeButton = new();
    private readonly Button updateShortcutsButton = new();
    private readonly Button startButton = new();
    private readonly Button closeButton = new();
    private List<LauncherShortcutEntry> entries;

    public WindowsLauncherMaintenanceForm(
        WindowsLauncherPackageInstaller installer,
        WindowsLauncherShortcutStore store,
        WindowsShellLinkService shellLinks)
    {
        this.installer = installer;
        this.store = store;
        this.shellLinks = shellLinks;
        entries = store.Load();
        Text = "Kifuwarabe Go 2026 - ランチャー更新";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new System.Drawing.Size(920, 600);
        MinimumSize = new System.Drawing.Size(860, 560);
        Font = new System.Drawing.Font("Meiryo UI", 10f);
        FormBorderStyle = FormBorderStyle.Sizable;

        var title = new Label
        {
            Text = "ランチャー更新",
            AutoSize = true,
            Font = new System.Drawing.Font(Font.FontFamily, 19f, System.Drawing.FontStyle.Bold),
            Location = new System.Drawing.Point(26, 22),
        };
        installedVersionLabel.Location = new System.Drawing.Point(30, 78);
        installedVersionLabel.AutoSize = true;
        targetLabel.Location = new System.Drawing.Point(30, 108);
        targetLabel.AutoSize = true;

        installButton.Text = "最新版を取得して配置";
        installButton.SetBounds(680, 68, 205, 44);
        installButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        installButton.Click += async (_, _) => await InstallLatestAsync();

        var listHeading = new Label
        {
            Text = "ショートカット（最大5件）",
            AutoSize = true,
            Font = new System.Drawing.Font(Font.FontFamily, 12f, System.Drawing.FontStyle.Bold),
            Location = new System.Drawing.Point(28, 155),
        };
        shortcutList.SetBounds(30, 188, 855, 250);
        shortcutList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        shortcutList.View = View.Details;
        shortcutList.FullRowSelect = true;
        shortcutList.HideSelection = false;
        shortcutList.MultiSelect = false;
        shortcutList.Columns.Add("#", 42);
        shortcutList.Columns.Add("表示名", 165);
        shortcutList.Columns.Add("状態", 145);
        shortcutList.Columns.Add("ショートカット", 485);

        addButton.Text = "追加";
        addButton.SetBounds(30, 454, 120, 40);
        addButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        addButton.Click += (_, _) => AddShortcut();
        removeButton.Text = "登録解除";
        removeButton.SetBounds(160, 454, 120, 40);
        removeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        removeButton.Click += (_, _) => RemoveSelected();
        updateShortcutsButton.Text = "上から順に更新";
        updateShortcutsButton.SetBounds(300, 454, 180, 40);
        updateShortcutsButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        updateShortcutsButton.Click += (_, _) => UpdateShortcutsInOrder();
        startButton.Text = "新ランチャーを起動";
        startButton.SetBounds(680, 454, 205, 40);
        startButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        startButton.Click += (_, _) => StartCurrent();

        statusLabel.SetBounds(30, 512, 650, 52);
        statusLabel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        statusLabel.AutoEllipsis = true;
        closeButton.Text = "閉じる";
        closeButton.SetBounds(765, 520, 120, 40);
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        closeButton.Click += (_, _) => Close();

        Controls.AddRange([
            title, installedVersionLabel, targetLabel, installButton, listHeading, shortcutList,
            addButton, removeButton, updateShortcutsButton, startButton, statusLabel, closeButton,
        ]);
        AcceptButton = installButton;
        CancelButton = closeButton;
        RefreshState();
    }

    private async Task InstallLatestAsync()
    {
        SetBusy(true);
        try
        {
            var progress = new Progress<string>(message => statusLabel.Text = message);
            var result = await installer.InstallLatestAsync(progress);
            statusLabel.Text = $"配置完了: ランチャー v{result.Version}";
        }
        catch (Exception exception)
        {
            statusLabel.Text = "更新失敗: " + exception.Message;
            MessageBox.Show(this, exception.Message, "ランチャー更新失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
            RefreshState();
        }
    }

    private void AddShortcut()
    {
        if (entries.Count >= WindowsLauncherShortcutStore.MaximumCount)
        {
            MessageBox.Show(this, "登録できるショートカットは5件までです。", "登録上限", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dialog = new OpenFileDialog
        {
            Title = "ランチャーのWindowsショートカットを選択してください",
            Filter = "Windows shortcut (*.lnk)|*.lnk",
            CheckFileExists = true,
            Multiselect = false,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        var path = Path.GetFullPath(dialog.FileName);
        if (entries.Any(entry => string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "そのショートカットは登録済みです。", "重複登録", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var target = shellLinks.ReadTarget(path);
            if (!string.Equals(Path.GetFileName(target), "KifuwarabeGo2026.Launcher.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("選択したショートカットはKifuwarabeGo2026.Launcher.exeを指していません。");
            entries.Add(new LauncherShortcutEntry(
                Guid.NewGuid().ToString("N"),
                path,
                Path.GetFileNameWithoutExtension(path),
                target));
            store.Save(entries);
            statusLabel.Text = "ショートカットを登録しました。";
            RefreshState();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "登録失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RemoveSelected()
    {
        if (shortcutList.SelectedIndices.Count != 1) return;
        entries.RemoveAt(shortcutList.SelectedIndices[0]);
        store.Save(entries);
        statusLabel.Text = "登録を解除しました。ショートカットファイル自体は削除していません。";
        RefreshState();
    }

    private void UpdateShortcutsInOrder()
    {
        if (!File.Exists(installer.CurrentExecutable))
        {
            MessageBox.Show(this, "先に［最新版を取得して配置］を実行してください。", "ランチャー未配置", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var updated = 0;
        var skipped = 0;
        var failed = 0;
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            entries[index] = entry with { LastResult = "確認中" };
            store.Save(entries);
            RefreshState();
            shortcutList.EnsureVisible(index);
            System.Windows.Forms.Application.DoEvents();

            var answer = MessageBox.Show(
                this,
                $"{index + 1}. {entry.DisplayName}\n{entry.Path}\n\nリンク先を新しいランチャーへ更新しますか？",
                "ショートカットを更新しますか？",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question);
            if (answer == DialogResult.Cancel)
            {
                entries[index] = entry with { LastResult = "確認待ち" };
                store.Save(entries);
                statusLabel.Text = $"中止しました。更新済み {updated}、スキップ {skipped}、失敗 {failed}";
                RefreshState();
                return;
            }
            if (answer == DialogResult.No)
            {
                entries[index] = entry with { LastResult = "今回は更新しない" };
                skipped++;
            }
            else
            {
                entries[index] = entry with { LastResult = "更新中" };
                store.Save(entries);
                RefreshState();
                System.Windows.Forms.Application.DoEvents();
                try
                {
                    shellLinks.RewriteLauncherTarget(entry.Path, entry.LastKnownTarget, installer.CurrentExecutable);
                    entries[index] = entry with
                    {
                        LastKnownTarget = installer.CurrentExecutable,
                        LastResult = "更新済み",
                    };
                    updated++;
                }
                catch (FileNotFoundException)
                {
                    entries[index] = entry with { LastResult = "ファイルなし" };
                    failed++;
                }
                catch (InvalidOperationException)
                {
                    entries[index] = entry with { LastResult = "対象変更済み" };
                    failed++;
                }
                catch (Exception)
                {
                    entries[index] = entry with { LastResult = "更新失敗" };
                    failed++;
                }
            }
            store.Save(entries);
            RefreshState();
            System.Windows.Forms.Application.DoEvents();
        }
        statusLabel.Text = $"完了しました。更新済み {updated}、スキップ {skipped}、失敗 {failed}";
    }

    private void StartCurrent()
    {
        try
        {
            installer.StartCurrent();
            statusLabel.Text = "新しいランチャーを起動しました。";
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, "起動失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetBusy(bool busy)
    {
        installButton.Enabled = !busy;
        addButton.Enabled = !busy;
        removeButton.Enabled = !busy;
        updateShortcutsButton.Enabled = !busy;
        startButton.Enabled = !busy && File.Exists(installer.CurrentExecutable);
        closeButton.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private void RefreshState()
    {
        installedVersionLabel.Text = "所定フォルダーの現在版: " + (installer.InstalledVersion is { } version ? "v" + version : "未配置");
        targetLabel.Text = "配置先: " + installer.CurrentExecutable;
        shortcutList.BeginUpdate();
        shortcutList.Items.Clear();
        for (var index = 0; index < WindowsLauncherShortcutStore.MaximumCount; index++)
        {
            if (index < entries.Count)
            {
                var entry = entries[index];
                shortcutList.Items.Add(new ListViewItem([
                    (index + 1).ToString(), entry.DisplayName, entry.LastResult, entry.Path,
                ]));
            }
            else
            {
                shortcutList.Items.Add(new ListViewItem([(index + 1).ToString(), "未登録", "-", ""]));
            }
        }
        shortcutList.EndUpdate();
        startButton.Enabled = File.Exists(installer.CurrentExecutable);
        removeButton.Enabled = entries.Count > 0;
        updateShortcutsButton.Enabled = entries.Count > 0 && File.Exists(installer.CurrentExecutable);
        addButton.Enabled = entries.Count < WindowsLauncherShortcutStore.MaximumCount;
    }
}
