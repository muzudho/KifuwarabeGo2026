namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

public enum ApplicationSettingsPage
{
    Log,
    OtherFolders,
    Other,
}

public sealed partial class GoScreenRenderer
{
    private readonly ActionBadge[] _settingsLogActionBadges =
    [
        ActionBadge.Create("OPEN", SettingsLogItemBounds(0), 0.30f),
        ActionBadge.Create("OPEN", SettingsLogItemBounds(1), 0.30f),
        ActionBadge.Create("OPEN", SettingsLogItemBounds(2), 0.30f),
        ActionBadge.Create("OPEN", SettingsLogItemBounds(3), 0.30f),
        ActionBadge.Create("OPEN", SettingsLogItemBounds(4), 0.30f),
    ];

    private static Rectangle SettingsButtonBounds => new(1780, 972, 70, 62);
    private static Rectangle UpdateButtonBounds => new(1698, 972, 70, 62);
    private static Rectangle SettingsBackButtonBounds => new(1390, 138, 140, 52);
    private static Rectangle SettingsLogRootFieldBounds => new(440, 374, 1070, 58);
    private static Rectangle SettingsSgfFieldBounds => new(440, 374, 1070, 70);
    private static Rectangle SettingsScreenshotFieldBounds => new(440, 564, 1070, 70);
    private static Rectangle SettingsApplicationSettingsFileFieldBounds => new(440, 374, 1070, 70);
    private static Rectangle SettingsEngineSettingsFileFieldBounds => new(440, 564, 1070, 70);
    private static Rectangle SettingsLogItemBounds(int index) => new(440, 570 + index * 58, 1070, 48);
    private static Rectangle SettingsTabBounds(ApplicationSettingsPage page) => page switch
    {
        ApplicationSettingsPage.Log => new Rectangle(440, 242, 250, 52),
        ApplicationSettingsPage.OtherFolders => new Rectangle(702, 242, 360, 52),
        _ => new Rectangle(1074, 242, 250, 52),
    };

    public static bool GetSettingsButtonHit(Point point) => SettingsButtonBounds.Contains(point);
    public static bool GetUpdateButtonHit(Point point) => UpdateButtonBounds.Contains(point);
    public static bool GetSettingsBackButtonHit(Point point) => SettingsBackButtonBounds.Contains(point);
    public static bool GetSettingsBrowseButtonHit(Point point) => SettingsLogRootFieldBounds.Contains(point);
    public static bool GetSettingsSgfBrowseButtonHit(Point point) => SettingsSgfFieldBounds.Contains(point);
    public static bool GetSettingsScreenshotBrowseButtonHit(Point point) => SettingsScreenshotFieldBounds.Contains(point);
    public bool GetSettingsOpenButtonHit(Point point, int selectedIndex) =>
        selectedIndex >= 0 &&
        selectedIndex < _settingsLogActionBadges.Length &&
        _settingsLogActionBadges[selectedIndex].IsVisible &&
        _settingsLogActionBadges[selectedIndex].Bounds.Contains(point);
    public static bool GetSettingsOpenApplicationSettingsFolderButtonHit(Point point) =>
        SettingsApplicationSettingsFileFieldBounds.Contains(point);
    public static bool GetSettingsOpenEngineSettingsFolderButtonHit(Point point) =>
        SettingsEngineSettingsFileFieldBounds.Contains(point);

    public static ApplicationSettingsPage? GetSettingsTabHit(Point point)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
            if (SettingsTabBounds(page).Contains(point)) return page;
        return null;
    }

    public static int? GetSettingsLogItemHit(Point point, int count)
    {
        for (var index = 0; index < count; index++)
            if (SettingsLogItemBounds(index).Contains(point)) return index;
        return null;
    }

    public void DrawApplicationSettings(Point mousePosition, ApplicationSettingsPage page, string logRoot, string sgfSaveDirectory, string screenshotSaveDirectory, string applicationSettingsPath, string engineSettingsPath, IReadOnlyList<string> logFiles, int selectedIndex, string message)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(samplerState: Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        DrawBackground();
        var panel = new Rectangle(390, 100, 1180, 860);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 246));
        DrawRect(panel, 2, new Color(82, 111, 114));
        DrawText("APPLICATION SETTINGS", new Vector2(440, 148), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(SettingsBackButtonBounds, "BACK", false, mousePoint, scale: 0.32f);
        DrawSettingsTabs(page, mousePoint);
        if (page == ApplicationSettingsPage.Log)
        {
            foreach (var badge in _settingsLogActionBadges)
                badge.Hide();

            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 180), "ROOT FOLDER", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, logRoot, SettingsLogRootFieldBounds, "BROWSE", mousePoint);
            DrawFittedText("GUI   " + Path.Combine(logRoot, "Gui"), new Rectangle(440, 452, 1050, 24), new Color(180, 195, 195), 0.30f);
            DrawFittedText("CGOS  " + Path.Combine(logRoot, "Cgos"), new Rectangle(440, 480, 1050, 24), new Color(180, 195, 195), 0.30f);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 282), "RECENT GUI LOGS", new Color(76, 91, 126));
            for (var index = 0; index < logFiles.Count; index++)
            {
                var bounds = SettingsLogItemBounds(index);
                var selected = index == selectedIndex;
                var hovered = bounds.Contains(mousePoint);
                DrawFittedText(Path.GetFileName(logFiles[index]), new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, 28), Color.White, 0.31f);
                _settingsLogLinkUnderline.Bounds = new Rectangle(bounds.X + 8, bounds.Y, bounds.Width - 16, bounds.Height + 1);
                _settingsLogLinkUnderline.UpdatePointer(mousePoint);
                _settingsLogLinkUnderline.SetSelected(selected);
                _settingsLogLinkUnderline.Draw(
                    this);
                if (selected && hovered && index < _settingsLogActionBadges.Length)
                {
                    var badge = _settingsLogActionBadges[index];
                    badge.Show();
                    badge.Draw(new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText));
                }
            }
        }
        else if (page == ApplicationSettingsPage.OtherFolders)
        {
            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "SGF", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, string.IsNullOrWhiteSpace(sgfSaveDirectory) ? "(NOT SET - FIRST SAVE WILL BE REMEMBERED)" : sgfSaveDirectory, SettingsSgfFieldBounds, "BROWSE", mousePoint);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 174), "SCREENSHOT", new Color(76, 91, 126));
            DrawSettingsValueField(string.Empty, screenshotSaveDirectory, SettingsScreenshotFieldBounds, "BROWSE", mousePoint);
            DrawFittedText("Ctrl + P captures the whole game window, including its frame.", new Rectangle(440, 656, 1020, 28), new Color(180, 195, 195), 0.30f);
        }
        else
        {
            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "APPLICATION", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, applicationSettingsPath, SettingsApplicationSettingsFileFieldBounds, "OPEN", mousePoint);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 152), "ENGINE", new Color(76, 91, 126));
            DrawSettingsValueField(string.Empty, engineSettingsPath, SettingsEngineSettingsFileFieldBounds, "OPEN", mousePoint);
        }
        if (!string.IsNullOrWhiteSpace(message))
            DrawFittedText(message, new Rectangle(440, 910, 780, 24), new Color(255, 205, 140), 0.28f);
        _spriteBatch.End();
    }

    private void DrawSettingsTabs(ApplicationSettingsPage selectedPage, Point mousePoint)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
        {
            var bounds = SettingsTabBounds(page);
            var selected = page == selectedPage;
            FillRect(bounds, selected ? new Color(31, 49, 49) : bounds.Contains(mousePoint) ? new Color(29, 39, 46) : new Color(21, 28, 34));
            var label = page switch
            {
                ApplicationSettingsPage.Log => "LOG",
                ApplicationSettingsPage.OtherFolders => "OTHER FOLDERS",
                _ => "OTHER",
            };
            DrawFittedText(label, new Rectangle(bounds.X + 18, bounds.Y + 9, bounds.Width - 36, 30), Color.White, 0.34f);
            DrawRoundedFill(new Rectangle(bounds.X + 10, bounds.Bottom - 7, bounds.Width - 20, 5), 2,
                selected ? new Color(99, 223, 185) : bounds.Contains(mousePoint) ? new Color(185, 196, 255) : new Color(58, 78, 86));
        }
    }

    private void DrawSettingsValueField(
        string label,
        string value,
        Rectangle bounds,
        string action,
        Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        if (!string.IsNullOrWhiteSpace(label))
            DrawText(label, new Vector2(bounds.X + 8, bounds.Y), new Color(180, 195, 195), 0.34f);
        var valueY = string.IsNullOrWhiteSpace(label) ? bounds.Y + 16 : bounds.Y + 30;
        DrawFittedText(value, new Rectangle(bounds.X + 8, valueY, bounds.Width - (hovered ? 132 : 16), 28), Color.White, 0.32f);
        _settingsValueLinkUnderline.Bounds = new Rectangle(bounds.X + 8, bounds.Y, bounds.Width - 16, bounds.Height);
        _settingsValueLinkUnderline.SetActionBadge(ActionBadge.Create(action, bounds, 0.30f));
        _settingsValueLinkUnderline.UpdatePointer(mousePoint);
        _settingsValueLinkUnderline.Draw(this, new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText));
    }

    private void DrawSettingsButton(Point mousePoint)
    {
        var hovered = SettingsButtonBounds.Contains(mousePoint);
        FillRect(SettingsButtonBounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(SettingsButtonBounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var center = new Vector2(SettingsButtonBounds.Center.X, SettingsButtonBounds.Center.Y);
        var color = hovered ? new Color(99, 223, 185) : new Color(180, 195, 195);
        DrawCircle(center, 16, color);
        DrawCircle(center, 7, new Color(24, 31, 37));
        for (var index = 0; index < 8; index++)
        {
            var angle = MathHelper.TwoPi * index / 8f;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            DrawLine(center + direction * 15, center + direction * 24, 6, color);
        }
    }
}
