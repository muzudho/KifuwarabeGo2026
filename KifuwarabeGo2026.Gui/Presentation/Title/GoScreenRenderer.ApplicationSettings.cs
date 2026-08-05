namespace KifuwarabeGo2026.Gui.Presentation;

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
    private static Rectangle SettingsButtonBounds => new(1780, 972, 70, 62);
    private static Rectangle SettingsBackButtonBounds => new(1328, 178, 132, 48);
    private static Rectangle SettingsBrowseButtonBounds => new(1290, 370, 170, 58);
    private static Rectangle SettingsSgfBrowseButtonBounds => new(1290, 342, 170, 58);
    private static Rectangle SettingsScreenshotBrowseButtonBounds => new(1290, 502, 170, 58);
    private static Rectangle SettingsEditButtonBounds => new(1240, 820, 220, 58);
    private static Rectangle SettingsOpenApplicationSettingsFolderButtonBounds => new(1290, 342, 170, 46);
    private static Rectangle SettingsOpenEngineSettingsFolderButtonBounds => new(1290, 462, 170, 46);
    private static Rectangle SettingsLogItemBounds(int index) => new(470, 520 + index * 52, 990, 44);
    private static Rectangle SettingsTabBounds(ApplicationSettingsPage page) => page switch
    {
        ApplicationSettingsPage.Log => new Rectangle(470, 250, 260, 48),
        ApplicationSettingsPage.OtherFolders => new Rectangle(738, 250, 360, 48),
        _ => new Rectangle(1106, 250, 260, 48),
    };

    public static bool GetSettingsButtonHit(Point point) => SettingsButtonBounds.Contains(point);
    public static bool GetSettingsBackButtonHit(Point point) => SettingsBackButtonBounds.Contains(point);
    public static bool GetSettingsBrowseButtonHit(Point point) => SettingsBrowseButtonBounds.Contains(point);
    public static bool GetSettingsSgfBrowseButtonHit(Point point) => SettingsSgfBrowseButtonBounds.Contains(point);
    public static bool GetSettingsScreenshotBrowseButtonHit(Point point) => SettingsScreenshotBrowseButtonBounds.Contains(point);
    public static bool GetSettingsEditButtonHit(Point point, bool enabled) => enabled && SettingsEditButtonBounds.Contains(point);
    public static bool GetSettingsOpenApplicationSettingsFolderButtonHit(Point point) =>
        SettingsOpenApplicationSettingsFolderButtonBounds.Contains(point);
    public static bool GetSettingsOpenEngineSettingsFolderButtonHit(Point point) =>
        SettingsOpenEngineSettingsFolderButtonBounds.Contains(point);

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
        var panel = new Rectangle(420, 150, 1080, 758);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 246));
        DrawRect(panel, 2, new Color(82, 111, 114));
        DrawText("APPLICATION SETTINGS", new Vector2(468, 196), new Color(244, 238, 218), 0.82f);
        DrawCommandButton(SettingsBackButtonBounds, "BACK", false, mousePoint, scale: 0.32f);
        DrawSettingsTabs(page, mousePoint);
        if (page == ApplicationSettingsPage.Log)
        {
            DrawText("LOG ROOT FOLDER", new Vector2(470, 326), new Color(180, 195, 195), 0.42f);
            DrawDataRowFrame(new Rectangle(470, 370, 800, 58));
            DrawFittedText(logRoot, new Rectangle(490, 380, 760, 38), Color.White, 0.40f);
            DrawCommandButton(SettingsBrowseButtonBounds, "BROWSE", false, mousePoint, scale: 0.34f);
            DrawFittedText("GUI   " + Path.Combine(logRoot, "Gui"), new Rectangle(470, 438, 970, 26), new Color(180, 195, 195), 0.32f);
            DrawFittedText("CGOS  " + Path.Combine(logRoot, "Cgos"), new Rectangle(470, 464, 970, 26), new Color(180, 195, 195), 0.32f);
            DrawText("RECENT GUI LOGS", new Vector2(470, 500), new Color(180, 195, 195), 0.38f);
            for (var index = 0; index < logFiles.Count; index++)
            {
                var bounds = SettingsLogItemBounds(index);
                var selected = index == selectedIndex;
                FillRect(bounds, selected ? new Color(46, 77, 72) : bounds.Contains(mousePoint) ? new Color(36, 50, 58) : new Color(24, 31, 37));
                DrawRect(bounds, selected ? 2 : 1, selected ? new Color(99, 223, 185) : new Color(82, 111, 114));
                DrawFittedText(Path.GetFileName(logFiles[index]), new Rectangle(bounds.X + 16, bounds.Y + 8, bounds.Width - 32, 28), Color.White, 0.31f);
            }
            DrawCommandButton(SettingsEditButtonBounds, "EDIT IN CODE", false, mousePoint, enabled: selectedIndex >= 0, scale: 0.34f);
        }
        else if (page == ApplicationSettingsPage.OtherFolders)
        {
            DrawText("SGF SAVE FOLDER", new Vector2(470, 326), new Color(180, 195, 195), 0.38f);
            DrawDataRowFrame(new Rectangle(470, 342, 800, 58));
            DrawFittedText(string.IsNullOrWhiteSpace(sgfSaveDirectory) ? "(NOT SET - FIRST SAVE WILL BE REMEMBERED)" : sgfSaveDirectory, new Rectangle(490, 352, 760, 38), Color.White, 0.36f);
            DrawCommandButton(SettingsSgfBrowseButtonBounds, "BROWSE", false, mousePoint, scale: 0.34f);
            DrawText("SCREENSHOT SAVE FOLDER", new Vector2(470, 468), new Color(180, 195, 195), 0.38f);
            DrawDataRowFrame(new Rectangle(470, 502, 800, 58));
            DrawFittedText(screenshotSaveDirectory, new Rectangle(490, 512, 760, 38), Color.White, 0.36f);
            DrawCommandButton(SettingsScreenshotBrowseButtonBounds, "BROWSE", false, mousePoint, scale: 0.34f);
            DrawText("Ctrl + P captures the whole game window, including its frame.", new Vector2(470, 590), new Color(180, 195, 195), 0.34f);
        }
        else
        {
            DrawText("APPLICATION SETTINGS FILE", new Vector2(470, 326), new Color(180, 195, 195), 0.34f);
            DrawFittedText(applicationSettingsPath, new Rectangle(470, 356, 800, 24), Color.White, 0.30f);
            DrawCommandButton(SettingsOpenApplicationSettingsFolderButtonBounds, "OPEN FOLDER", false, mousePoint, scale: 0.28f);
            DrawText("ENGINE SETTINGS FILE", new Vector2(470, 446), new Color(180, 195, 195), 0.34f);
            DrawFittedText(engineSettingsPath, new Rectangle(470, 476, 800, 24), Color.White, 0.30f);
            DrawCommandButton(SettingsOpenEngineSettingsFolderButtonBounds, "OPEN FOLDER", false, mousePoint, scale: 0.28f);
        }
        if (!string.IsNullOrWhiteSpace(message))
            DrawFittedText(message, new Rectangle(470, 862, 720, 20), new Color(255, 205, 140), 0.28f);
        _spriteBatch.End();
    }

    private void DrawSettingsTabs(ApplicationSettingsPage selectedPage, Point mousePoint)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
        {
            var bounds = SettingsTabBounds(page);
            var selected = page == selectedPage;
            FillRect(bounds, selected ? new Color(46, 77, 72) : bounds.Contains(mousePoint) ? new Color(36, 50, 58) : new Color(24, 31, 37));
            DrawRect(bounds, selected ? 2 : 1, selected ? new Color(99, 223, 185) : new Color(82, 111, 114));
            var label = page switch
            {
                ApplicationSettingsPage.Log => "LOG",
                ApplicationSettingsPage.OtherFolders => "OTHER FOLDERS",
                _ => "OTHER",
            };
            DrawFittedText(label, new Rectangle(bounds.X + 18, bounds.Y + 9, bounds.Width - 36, 30), Color.White, 0.34f);
        }
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
