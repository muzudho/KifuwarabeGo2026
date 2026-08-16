namespace KifuwarabeGo2026.Launcher.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using StationeryButton = KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button.Button;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls;
using KifuwarabeGo2026.Shared;

public sealed class LauncherScreen : IDisposable
{
    private readonly IPlatformServices _platform;
    private readonly LauncherPaths _paths;
    private readonly LauncherSettingsStore _settings;
    private readonly InstalledVersionCatalog _catalog;
    private readonly ProductLauncher _launcher;
    private readonly LauncherUpdateService _updates;
    private readonly KfwStationeryDrawingTools _draw;
    private readonly Action _exitApplication;
    private readonly object _stateGate = new();
    private readonly StationeryButton _start = new(new Rectangle(560, 280, 210, 66), "START", 0.38f);
    private readonly StationeryButton _guiUpdate = new(new Rectangle(800, 280, 250, 66), "GUI UPDATE", 0.34f);
    private readonly StationeryButton _engineFolder = new(new Rectangle(560, 390, 210, 66), "OPEN FOLDER", 0.30f);
    private readonly StationeryButton _engineUpdate = new(new Rectangle(800, 390, 250, 66), "ENGINE UPDATE", 0.30f);
    private readonly StationeryButton _allUpdate = new(new Rectangle(960, 570, 480, 66), "CHECK GUI + ENGINE UPDATES", 0.27f);
    private readonly StationeryButton _versionsButton = new(new Rectangle(480, 570, 430, 66), "MANAGE INSTALLED VERSIONS", 0.27f);
    private readonly StationeryButton _back = new(new Rectangle(1310, 205, 170, 58), "BACK", 0.34f);
    private readonly StationeryButton _open = new(new Rectangle(850, 930, 220, 58), "OPEN FOLDER", 0.28f);
    private readonly StationeryButton _remove = new(new Rectangle(1100, 930, 300, 58), "UNINSTALL", 0.32f);
    private readonly StationeryButton _confirm = new(new Rectangle(1010, 700, 210, 58), "UNINSTALL", 0.28f);
    private readonly StationeryButton _cancel = new(new Rectangle(760, 700, 210, 58), "CANCEL", 0.34f);
    private readonly GearButton _settingsButton = new(new Rectangle(1740, 920, 90, 72));
    private readonly StationeryButton _settingsBack = new(new Rectangle(1310, 205, 170, 58), "BACK", 0.34f);
    private readonly StationeryButton _browseScreenshots = new(new Rectangle(1260, 430, 240, 62), "BROWSE", 0.34f);
    private readonly StationeryButton _openScreenshots = new(new Rectangle(1260, 520, 240, 62), "OPEN FOLDER", 0.28f);
    private readonly StationeryButton _openSettingsFile = new(new Rectangle(1260, 650, 240, 62), "OPEN FILE", 0.32f);
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private GamePadState _previousGamePad;
    private bool _versionsPage;
    private bool _settingsPage;
    private bool _confirming;
    private bool _busy;
    private bool _loadingVersions;
    private float _spinnerAngle;
    private string _status = "READY";
    private IReadOnlyList<InstalledVersion> _installed = [];
    private readonly HashSet<string> _markedForRemoval = new(StringComparer.OrdinalIgnoreCase);
    private int _selectedIndex;
    private int _firstVisible;

    public LauncherScreen(KfwStationeryDrawingTools drawingTools, IPlatformServices platform, HttpClient httpClient, Action exitApplication)
    {
        _draw = drawingTools;
        _exitApplication = exitApplication;
        _platform = platform;
        _paths = new LauncherPaths(platform.LocalApplicationData);
        _settings = new LauncherSettingsStore(_paths);
        var log = new LauncherLog(_paths);
        _catalog = new InstalledVersionCatalog(platform, platform);
        _launcher = new ProductLauncher(_paths, _settings, log, platform);
        _updates = new LauncherUpdateService(new GitHubReleaseClient(httpClient), new PackageInstaller(_paths, httpClient, log), _settings, log);
    }

    public void Update()
    {
        if (_loadingVersions) _spinnerAngle = (_spinnerAngle + 0.11f) % MathHelper.TwoPi;
        var mouse = Mouse.GetState();
        var keyboard = Keyboard.GetState();
        var gamePad = GamePad.GetState(PlayerIndex.One);
        var point = _draw.ToVirtualPoint(mouse.Position);
        var click = mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Released;
        if (_confirming) UpdateConfirmation(point, click, keyboard, gamePad);
        else if (_settingsPage) UpdateSettings(point, click, keyboard, gamePad);
        else if (_versionsPage) UpdateVersions(point, click, keyboard, gamePad);
        else UpdateMain(point, click, keyboard, gamePad);
        _previousMouse = mouse;
        _previousKeyboard = keyboard;
        _previousGamePad = gamePad;
    }

    public void Draw()
    {
        var mouse = _draw.ToVirtualPoint(Mouse.GetState().Position);
        _draw.Begin();
        _draw.DrawBackground();
        _draw.DrawText("KIFUWARABE GO 2026", new Vector2(120, 92), new Color(244, 238, 218), 0.92f);
        _draw.DrawText("COMMON LAUNCHER", new Vector2(124, 158), new Color(99, 223, 185), 0.38f);
        if (_settingsPage) DrawSettings(mouse); else if (_versionsPage) DrawVersions(mouse); else DrawMain(mouse);
        DrawStatus();
        if (_confirming) DrawConfirmation(mouse);
        _draw.End();
    }

    private void DrawMain(Point mouse)
    {
        var settings = _settings.Load();
        DrawProductRow("GUI", settings.GuiCurrentVersion, 280);
        DrawProductRow("ENGINE", settings.EngineCurrentVersion, 390);
        _draw.DrawText("COMMON OPERATIONS", new Vector2(480, 510), new Color(99, 223, 185), 0.38f);
        SetButtonsEnabled(!_busy, _start, _guiUpdate, _engineFolder, _engineUpdate, _allUpdate, _versionsButton);
        _start.Draw(mouse, _draw); _guiUpdate.Draw(mouse, _draw); _engineFolder.Draw(mouse, _draw);
        _engineUpdate.Draw(mouse, _draw); _allUpdate.Draw(mouse, _draw); _versionsButton.Draw(mouse, _draw);
        _draw.DrawFittedText("S: START   G: GUI UPDATE   E: ENGINE UPDATE   A: ALL   I: VERSIONS",
            new Rectangle(480, 665, 980, 55), new Color(150, 171, 178), 0.56f);
        _draw.DrawFittedText("CTRL + P: SCREENSHOT (SHARED APPLICATION SETTING)",
            new Rectangle(480, 720, 980, 55), new Color(150, 171, 178), 0.52f);
        _settingsButton.Draw(_draw, mouse);
    }

    private void DrawSettings(Point mouse)
    {
        _draw.DrawText("APPLICATION SETTINGS", new Vector2(120, 220), Color.White, 0.62f);
        _draw.DrawText("SCREENSHOT FOLDER", new Vector2(180, 370), new Color(99, 223, 185), 0.42f);
        _draw.DrawDataRowFrame(new Rectangle(160, 420, 1360, 82));
        _draw.DrawFittedText(ApplicationFamilySettings.ScreenshotSaveDirectory,
            new Rectangle(200, 438, 1010, 46), Color.White, 0.48f);
        _browseScreenshots.Draw(mouse, _draw);
        _openScreenshots.Draw(mouse, _draw);
        _draw.DrawText("SHARED SETTINGS FILE", new Vector2(180, 600), new Color(99, 223, 185), 0.42f);
        _draw.DrawDataRowFrame(new Rectangle(160, 640, 1360, 82));
        _draw.DrawFittedText(ApplicationFamilySettings.FilePath,
            new Rectangle(200, 658, 1010, 46), Color.White, 0.44f);
        _openSettingsFile.Draw(mouse, _draw);
        var closeBounds = new Rectangle(180, 770, 36, 36);
        DrawCheckbox(closeBounds, ApplicationFamilySettings.CloseLauncherAfterStartingGui, enabled: true);
        _draw.DrawFittedText("CLOSE LAUNCHER AFTER STARTING GUI",
            new Rectangle(240, 758, 780, 60), Color.White, 0.54f);
        _settingsBack.Draw(mouse, _draw);
        _draw.DrawFittedText("THIS SETTING IS SHARED BY THE LAUNCHER AND EVERY GUI VERSION.",
            new Rectangle(380, 870, 1120, 72), new Color(150, 171, 178), 0.42f);
    }

    private void DrawProductRow(string product, string? version, int y)
    {
        _draw.DrawDataRowFrame(new Rectangle(120, y - 8, 1360, 82));
        if (product == "GUI") DrawBoardIcon(new Rectangle(150, y + 2, 54, 54));
        else DrawRobotIcon(new Rectangle(150, y + 2, 54, 54));
        _draw.DrawText(product, new Vector2(230, y + 8), Color.White, 0.48f);
        _draw.DrawText(string.IsNullOrWhiteSpace(version) ? "NOT INSTALLED" : "v" + version.TrimStart('v', 'V'),
            new Vector2(400, y + 8), new Color(178, 219, 226), 0.44f);
    }

    private void DrawBoardIcon(Rectangle bounds)
    {
        var color = new Color(178, 219, 226);
        _draw.DrawRectangle(bounds, 3, color);
        for (var index = 1; index < 4; index++)
        {
            var offset = index * bounds.Width / 4;
            _draw.DrawLine(new Vector2(bounds.X + offset, bounds.Y + 5), new Vector2(bounds.X + offset, bounds.Bottom - 5), 2, color);
            _draw.DrawLine(new Vector2(bounds.X + 5, bounds.Y + offset), new Vector2(bounds.Right - 5, bounds.Y + offset), 2, color);
        }
        _draw.DrawCircle(new Vector2(bounds.Center.X, bounds.Center.Y), 6, new Color(99, 223, 185));
    }

    private void DrawRobotIcon(Rectangle bounds)
    {
        var color = new Color(178, 219, 226);
        var face = new Rectangle(bounds.X + 3, bounds.Y + 11, bounds.Width - 6, bounds.Height - 14);
        _draw.DrawRectangle(face, 3, color);
        _draw.DrawLine(new Vector2(bounds.Center.X, bounds.Y + 11), new Vector2(bounds.Center.X, bounds.Y + 2), 3, color);
        _draw.DrawCircle(new Vector2(bounds.Center.X, bounds.Y + 2), 4, new Color(99, 223, 185));
        _draw.DrawCircle(new Vector2(face.X + 14, face.Y + 15), 5, color);
        _draw.DrawCircle(new Vector2(face.Right - 14, face.Y + 15), 5, color);
        _draw.DrawLine(new Vector2(face.X + 14, face.Bottom - 10), new Vector2(face.Right - 14, face.Bottom - 10), 3, color);
    }

    private void DrawCheckbox(Rectangle bounds, bool isChecked, bool enabled)
    {
        var color = enabled ? new Color(178, 219, 226) : new Color(92, 112, 118);
        _draw.DrawRectangle(bounds, 3, color);
        if (!isChecked) return;
        var checkColor = new Color(99, 223, 185);
        _draw.DrawLine(new Vector2(bounds.X + 6, bounds.Y + 16), new Vector2(bounds.X + 13, bounds.Bottom - 6), 4, checkColor);
        _draw.DrawLine(new Vector2(bounds.X + 13, bounds.Bottom - 6), new Vector2(bounds.Right - 5, bounds.Y + 6), 4, checkColor);
    }

    private void DrawVersions(Point mouse)
    {
        _draw.DrawText("INSTALLED VERSIONS", new Vector2(120, 220), Color.White, 0.56f);
        const int visible = 9;
        for (var row = 0; row < visible; row++)
        {
            var index = _firstVisible + row;
            if (index >= _installed.Count) break;
            var item = _installed[index];
            var bounds = new Rectangle(120, 290 + row * 65, 1360, 56);
            var marked = _markedForRemoval.Contains(Identity(item));
            _draw.DrawDataRowFrame(bounds, active: index == _selectedIndex || marked, hovered: bounds.Contains(mouse));
            DrawCheckbox(new Rectangle(145, bounds.Y + 13, 30, 30), marked, item.CanUninstall);
            if (item.Product == InstalledProduct.Engine) DrawRobotIcon(new Rectangle(190, bounds.Y + 9, 38, 38));
            else DrawBoardIcon(new Rectangle(190, bounds.Y + 9, 38, 38));
            _draw.DrawFittedText(item.ProductName, new Rectangle(245, bounds.Y + 4, 150, 48), Color.White, 0.64f);
            _draw.DrawFittedText(item.Version, new Rectangle(410, bounds.Y + 4, 160, 48), new Color(178, 219, 226), 0.64f);
            _draw.DrawFittedText(FormatSize(item.SizeInBytes), new Rectangle(590, bounds.Y + 4, 140, 48), Color.White, 0.60f);
            _draw.DrawFittedText(string.IsNullOrEmpty(item.Protection) ? "AVAILABLE" : item.Protection,
                new Rectangle(750, bounds.Y + 4, 210, 48), item.CanUninstall ? new Color(151, 255, 215) : new Color(255, 190, 150), 0.54f);
            _draw.DrawFittedText(item.DirectoryPath, new Rectangle(980, bounds.Y + 8, 470, 40), new Color(150, 171, 178), 0.24f);
        }
        _back.Draw(mouse, _draw); _open.Draw(mouse, _draw);
        _remove.IsEnabled = RemovalTargets.Count > 0 && !_busy;
        _remove.Draw(mouse, _draw);
        _draw.DrawFittedText("CLICK/SPACE: MARK   UP/DOWN: MOVE   O: OPEN   DELETE: UNINSTALL   ESC: BACK",
            new Rectangle(120, 850, 1360, 58), new Color(150, 171, 178), 0.50f);
        if (_loadingVersions) DrawLoadingSpinner();
    }

    private void DrawLoadingSpinner()
    {
        _draw.FillRectangle(new Rectangle(650, 430, 620, 220), new Color(12, 18, 23, 235));
        _draw.DrawRectangle(new Rectangle(650, 430, 620, 220), 2, new Color(82, 111, 114));
        var center = new Vector2(960, 510);
        for (var index = 0; index < 12; index++)
        {
            var angle = _spinnerAngle + MathHelper.TwoPi * index / 12f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var color = index < 4 ? new Color(99, 223, 185) : index < 8 ? new Color(178, 219, 226) : new Color(68, 91, 98);
            _draw.DrawLine(center + direction * 28, center + direction * 58, 7, color);
        }
        _draw.DrawFittedText("SCANNING INSTALLED VERSIONS...", new Rectangle(720, 585, 480, 42), Color.White, 0.46f);
    }

    private void DrawStatus()
    {
        string text; lock (_stateGate) text = _status;
        _draw.FillRectangle(new Rectangle(0, 1020, 1920, 60), new Color(12, 18, 23, 235));
        _draw.DrawFittedText(text, new Rectangle(120, 1022, 1680, 54), _busy ? new Color(255, 225, 128) : new Color(151, 255, 215), 0.60f);
    }

    private void DrawConfirmation(Point mouse)
    {
        _draw.FillRectangle(new Rectangle(0, 0, 1920, 1080), new Color(0, 0, 0, 170));
        var bounds = new Rectangle(570, 390, 780, 400);
        _draw.FillRectangle(bounds, new Color(21, 25, 32, 252)); _draw.DrawRectangle(bounds, 2, new Color(255, 145, 151));
        _draw.DrawText("UNINSTALL VERSION?", new Vector2(620, 440), new Color(244, 238, 218), 0.54f);
        var targets = RemovalTargets;
        var size = targets.Sum(item => item.SizeInBytes);
        _draw.DrawFittedText(targets.Count == 0 ? "NO VERSION" : $"{targets.Count} VERSION(S)  {FormatSize(size)} WILL BE RELEASED",
            new Rectangle(640, 535, 640, 70), Color.White, 0.38f);
        _draw.DrawFittedText("THIS CANNOT BE UNDONE.", new Rectangle(640, 615, 640, 40), new Color(255, 180, 170), 0.32f);
        _cancel.Draw(mouse, _draw); _confirm.Draw(mouse, _draw);
    }

    private void UpdateMain(Point point, bool click, KeyboardState keyboard, GamePadState gamePad)
    {
        if (_busy) return;
        if ((click && _start.IsHit(point)) || Pressed(keyboard, Keys.S)) StartGui();
        else if ((click && _guiUpdate.IsHit(point)) || Pressed(keyboard, Keys.G)) _ = UpdateProductAsync(LauncherProduct.Gui);
        else if ((click && _engineUpdate.IsHit(point)) || Pressed(keyboard, Keys.E)) _ = UpdateProductAsync(LauncherProduct.Engine);
        else if ((click && _allUpdate.IsHit(point)) || Pressed(keyboard, Keys.A)) _ = UpdateAllAsync();
        else if ((click && _engineFolder.IsHit(point)) || Pressed(keyboard, Keys.O)) OpenCurrentEngine();
        else if ((click && _versionsButton.IsHit(point)) || Pressed(keyboard, Keys.I) || GamePadPressed(gamePad, Buttons.A)) { _versionsPage = true; _ = LoadVersionsAsync(); }
        else if (click && _settingsButton.IsHit(point)) _settingsPage = true;
    }

    private void UpdateSettings(Point point, bool click, KeyboardState keyboard, GamePadState gamePad)
    {
        if ((click && _settingsBack.IsHit(point)) || Pressed(keyboard, Keys.Escape) || GamePadPressed(gamePad, Buttons.B)) { _settingsPage = false; return; }
        if (click && _browseScreenshots.IsHit(point))
        {
            var selected = _platform.SelectFolder("Select the folder for screenshots.", ApplicationFamilySettings.ScreenshotSaveDirectory);
            if (selected is not null)
            {
                try { ApplicationFamilySettings.SaveScreenshotDirectory(selected); SetStatus("SCREENSHOT FOLDER SAVED: " + selected); }
                catch (Exception exception) { SetStatus("SETTINGS SAVE FAILED: " + exception.Message); }
            }
        }
        else if (click && _openScreenshots.IsHit(point))
            SetStatus(_platform.OpenFolder(ApplicationFamilySettings.ScreenshotSaveDirectory) ? "SCREENSHOT FOLDER OPENED" : "SCREENSHOT FOLDER COULD NOT BE OPENED");
        else if (click && _openSettingsFile.IsHit(point))
            SetStatus(_platform.OpenFile(ApplicationFamilySettings.FilePath) ? "SETTINGS FILE OPENED" : "SETTINGS FILE COULD NOT BE OPENED");
        else if (click && new Rectangle(170, 748, 870, 80).Contains(point))
        {
            try
            {
                var value = !ApplicationFamilySettings.CloseLauncherAfterStartingGui;
                ApplicationFamilySettings.SaveCloseLauncherAfterStartingGui(value);
                SetStatus(value ? "LAUNCHER WILL CLOSE AFTER GUI START" : "LAUNCHER WILL REMAIN OPEN AFTER GUI START");
            }
            catch (Exception exception) { SetStatus("SETTINGS SAVE FAILED: " + exception.Message); }
        }
    }

    private void UpdateVersions(Point point, bool click, KeyboardState keyboard, GamePadState gamePad)
    {
        if (_loadingVersions)
        {
            if ((click && _back.IsHit(point)) || Pressed(keyboard, Keys.Escape) || GamePadPressed(gamePad, Buttons.B)) _versionsPage = false;
            return;
        }
        for (var row = 0; row < 9; row++) if (click && new Rectangle(120, 290 + row * 65, 1360, 56).Contains(point)) { Select(_firstVisible + row); ToggleMark(); }
        if (Pressed(keyboard, Keys.Up) || GamePadPressed(gamePad, Buttons.DPadUp)) Select(_selectedIndex - 1);
        if (Pressed(keyboard, Keys.Down) || GamePadPressed(gamePad, Buttons.DPadDown)) Select(_selectedIndex + 1);
        if (Pressed(keyboard, Keys.Space) || GamePadPressed(gamePad, Buttons.A)) ToggleMark();
        if ((click && _back.IsHit(point)) || Pressed(keyboard, Keys.Escape) || GamePadPressed(gamePad, Buttons.B)) _versionsPage = false;
        else if ((click && _open.IsHit(point)) || Pressed(keyboard, Keys.O)) OpenSelected();
        else if ((click && _remove.IsHit(point)) || Pressed(keyboard, Keys.Delete)) { if (RemovalTargets.Count > 0) _confirming = true; }
    }

    private void UpdateConfirmation(Point point, bool click, KeyboardState keyboard, GamePadState gamePad)
    {
        if ((click && _cancel.IsHit(point)) || Pressed(keyboard, Keys.Escape) || GamePadPressed(gamePad, Buttons.B)) _confirming = false;
        else if ((click && _confirm.IsHit(point)) || Pressed(keyboard, Keys.Enter) || GamePadPressed(gamePad, Buttons.A))
        {
            var targets = RemovalTargets;
            _confirming = false;
            if (targets.Count == 0) return;
            var failures = new List<string>();
            foreach (var target in targets)
            {
                try { _catalog.Uninstall(target); }
                catch (Exception exception) { failures.Add($"{target.ProductName} {target.Version}: {exception.Message}"); }
            }
            _markedForRemoval.Clear();
            SetStatus(failures.Count == 0
                ? $"UNINSTALLED {targets.Count} VERSION(S), RELEASED {FormatSize(targets.Sum(item => item.SizeInBytes))}"
                : $"UNINSTALLED {targets.Count - failures.Count}; FAILED {failures.Count}: {failures[0]}");
            RefreshVersions();
        }
    }

    private async Task UpdateProductAsync(LauncherProduct product)
    {
        _busy = true;
        try { var version = await _updates.UpdateAsync(product, SetStatus); SetStatus($"{product.DisplayName()} v{version} UPDATE COMPLETE"); }
        catch (Exception exception) { SetStatus("UPDATE FAILED: " + exception.Message); }
        finally { _busy = false; RefreshVersions(); }
    }

    private async Task LoadVersionsAsync()
    {
        if (_loadingVersions) return;
        _busy = true;
        _loadingVersions = true;
        SetStatus("SCANNING INSTALLED VERSIONS...");
        try
        {
            var versions = await Task.Run(_catalog.ReadAll);
            _installed = versions;
            _markedForRemoval.IntersectWith(_installed.Where(item => item.CanUninstall).Select(Identity));
            Select(Math.Min(_selectedIndex, _installed.Count - 1));
            SetStatus($"FOUND {_installed.Count} INSTALLED VERSION(S)");
        }
        catch (Exception exception) { SetStatus("VERSION SCAN FAILED: " + exception.Message); }
        finally { _loadingVersions = false; _busy = false; }
    }

    private async Task UpdateAllAsync() { await UpdateProductAsync(LauncherProduct.Gui); await UpdateProductAsync(LauncherProduct.Engine); }
    private void StartGui() { var result = _launcher.StartGui(); SetStatus(result.Message); if (result.Success && ApplicationFamilySettings.CloseLauncherAfterStartingGui) _exitApplication(); }
    private void OpenCurrentEngine() { var directory = _launcher.CurrentDirectory(LauncherProduct.Engine); SetStatus(directory is not null && _platform.OpenFolder(directory) ? "ENGINE FOLDER OPENED" : "ENGINE IS NOT INSTALLED"); }
    private void OpenSelected() { var item = Selected; if (item is not null) SetStatus(_platform.OpenFolder(item.DirectoryPath) ? "FOLDER OPENED" : "FOLDER COULD NOT BE OPENED"); }
    private void RefreshVersions() { _installed = _catalog.ReadAll(); _markedForRemoval.IntersectWith(_installed.Where(item => item.CanUninstall).Select(Identity)); Select(Math.Min(_selectedIndex, _installed.Count - 1)); }
    private void ToggleMark() { var item = Selected; if (item?.CanUninstall != true) return; var key = Identity(item); if (!_markedForRemoval.Add(key)) _markedForRemoval.Remove(key); }
    private void Select(int index) { _selectedIndex = _installed.Count == 0 ? 0 : Math.Clamp(index, 0, _installed.Count - 1); if (_selectedIndex < _firstVisible) _firstVisible = _selectedIndex; if (_selectedIndex >= _firstVisible + 9) _firstVisible = _selectedIndex - 8; }
    private InstalledVersion? Selected => _installed.Count == 0 ? null : _installed[Math.Clamp(_selectedIndex, 0, _installed.Count - 1)];
    private IReadOnlyList<InstalledVersion> RemovalTargets => _installed.Where(item => item.CanUninstall && _markedForRemoval.Contains(Identity(item))).ToArray();
    private static string Identity(InstalledVersion item) => $"{item.Product}|{item.DirectoryPath}";
    private void SetStatus(string text) { lock (_stateGate) _status = text; }
    public void ShowStatus(string text) => SetStatus(text);
    private bool Pressed(KeyboardState state, Keys key) => state.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    private bool GamePadPressed(GamePadState state, Buttons button) => state.IsButtonDown(button) && _previousGamePad.IsButtonUp(button);
    private static void SetButtonsEnabled(bool enabled, params StationeryButton[] buttons) { foreach (var button in buttons) button.IsEnabled = enabled; }
    private static string FormatSize(long bytes) { string[] units = ["B", "KB", "MB", "GB", "TB"]; var value = (double)bytes; var unit = 0; while (value >= 1024 && unit < units.Length - 1) { value /= 1024; unit++; } return $"{value:0.#} {units[unit]}"; }
    public void Dispose() => _draw.Dispose();
}
