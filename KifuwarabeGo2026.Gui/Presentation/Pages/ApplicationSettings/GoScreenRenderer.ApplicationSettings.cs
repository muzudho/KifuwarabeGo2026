namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.Pages.ApplicationSettings;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
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
    public void DrawApplicationSettings(Point mousePosition, ApplicationSettingsPage page, string logRoot, string sgfSaveDirectory, string screenshotSaveDirectory, string applicationSettingsPath, string engineSettingsPath, IReadOnlyList<string> logFiles, int selectedIndex, string message)
    {
        var screen = ApplicationSettingsScreen.Default;
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);
        _spriteBatch.Begin(samplerState: Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));
        DrawBackground();
        var panel = new Rectangle(390, 100, 1180, 860);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 246));
        DrawRect(panel, 2, new Color(82, 111, 114));
        DrawText("APPLICATION SETTINGS", new Vector2(440, 148), new Color(244, 238, 218), 0.78f);
        screen.BackButton.Draw(mousePoint, _stationeryDrawingContext);
        DrawSettingsTabs(page, mousePoint);
        if (page == ApplicationSettingsPage.Log)
        {
            foreach (var link in screen.LogItemLinks)
                link.ActionBadge?.Hide();

            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 180), "ROOT FOLDER", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, logRoot, screen.LogRootLink, mousePoint);
            DrawFittedText("GUI   " + Path.Combine(logRoot, "Gui"), new Rectangle(440, 452, 1050, 24), new Color(180, 195, 195), 0.30f);
            DrawFittedText("CGOS  " + Path.Combine(logRoot, "Cgos"), new Rectangle(440, 480, 1050, 24), new Color(180, 195, 195), 0.30f);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 282), "RECENT GUI LOGS", new Color(76, 91, 126));
            for (var index = 0; index < logFiles.Count; index++)
            {
                var link = screen.LogItemLinks[index];
                var bounds = link.Bounds;
                var selected = index == selectedIndex;
                var hovered = bounds.Contains(mousePoint);
                DrawFittedText(Path.GetFileName(logFiles[index]), new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, 28), Color.White, 0.31f);
                link.UpdatePointer(mousePoint);
                link.SetSelected(selected);
                link.Draw(_stationeryDrawingContext);
                if (selected && hovered)
                {
                    link.ActionBadge?.Show();
                    link.ActionBadge?.Draw(new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText));
                }
            }
        }
        else if (page == ApplicationSettingsPage.OtherFolders)
        {
            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "SGF", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, string.IsNullOrWhiteSpace(sgfSaveDirectory) ? "(NOT SET - FIRST SAVE WILL BE REMEMBERED)" : sgfSaveDirectory, screen.SgfFolderLink, mousePoint);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 174), "SCREENSHOT", new Color(76, 91, 126));
            DrawSettingsValueField(string.Empty, screenshotSaveDirectory, screen.ScreenshotFolderLink, mousePoint);
            DrawFittedText("Ctrl + P captures the whole game window, including its frame.", new Rectangle(440, 656, 1020, 28), new Color(180, 195, 195), 0.30f);
        }
        else
        {
            DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "APPLICATION", new Color(67, 112, 118));
            DrawSettingsValueField(string.Empty, applicationSettingsPath, screen.ApplicationSettingsFileLink, mousePoint);
            DrawVerticalResultSection(new Rectangle(440, 548, 1070, 152), "ENGINE", new Color(76, 91, 126));
            DrawSettingsValueField(string.Empty, engineSettingsPath, screen.EngineSettingsFileLink, mousePoint);
        }
        if (!string.IsNullOrWhiteSpace(message))
            DrawFittedText(message, new Rectangle(440, 910, 780, 24), new Color(255, 205, 140), 0.28f);
        _spriteBatch.End();
    }

    private void DrawSettingsTabs(ApplicationSettingsPage selectedPage, Point mousePoint)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
        {
            var bounds = ApplicationSettingsScreen.Default.GetTabButton(page).Bounds;
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
        LinkUnderline link,
        Point mousePoint)
    {
        var bounds = link.Bounds;
        var hovered = bounds.Contains(mousePoint);
        if (!string.IsNullOrWhiteSpace(label))
            DrawText(label, new Vector2(bounds.X + 8, bounds.Y), new Color(180, 195, 195), 0.34f);
        var valueY = string.IsNullOrWhiteSpace(label) ? bounds.Y + 16 : bounds.Y + 30;
        DrawFittedText(value, new Rectangle(bounds.X + 8, valueY, bounds.Width - (hovered ? 132 : 16), 28), Color.White, 0.32f);
        link.UpdatePointer(mousePoint);
        link.Draw(_stationeryDrawingContext, new ActionBadgeDrawingCallbacks(DrawRoundedFill, DrawSharpCenteredFittedText));
    }

    private void DrawSettingsButton(Point mousePoint)
    {
        var bounds = ApplicationSettingsScreen.Default.SettingsButton.Bounds;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);
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
