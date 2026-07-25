namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

public sealed partial class GoScreenRenderer
{
    private static Rectangle SettingsButtonBounds => new(1780, 972, 70, 62);
    private static Rectangle SettingsBackButtonBounds => new(448, 820, 220, 58);
    private static Rectangle SettingsBrowseButtonBounds => new(1290, 292, 170, 58);
    private static Rectangle SettingsEditButtonBounds => new(1240, 820, 220, 58);
    private static Rectangle SettingsLogItemBounds(int index) => new(470, 430 + index * 48, 970, 42);

    public static bool GetSettingsButtonHit(Point point) => SettingsButtonBounds.Contains(point);
    public static bool GetSettingsBackButtonHit(Point point) => SettingsBackButtonBounds.Contains(point);
    public static bool GetSettingsBrowseButtonHit(Point point) => SettingsBrowseButtonBounds.Contains(point);
    public static bool GetSettingsEditButtonHit(Point point, bool enabled) => enabled && SettingsEditButtonBounds.Contains(point);

    public static int? GetSettingsLogItemHit(Point point, int count)
    {
        for (var index = 0; index < count; index++)
            if (SettingsLogItemBounds(index).Contains(point)) return index;
        return null;
    }

    public void DrawApplicationSettings(Point mousePosition, string logRoot, IReadOnlyList<string> logFiles, int selectedIndex, string message)
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
        DrawText("LOG FOLDER", new Vector2(470, 276), new Color(180, 195, 195), 0.42f);
        DrawDataRowFrame(new Rectangle(470, 292, 800, 58));
        DrawFittedText(logRoot, new Rectangle(490, 302, 760, 38), Color.White, 0.40f);
        DrawCommandButton(SettingsBrowseButtonBounds, "BROWSE", false, mousePoint, scale: 0.34f);
        DrawText("RECENT GUI LOGS", new Vector2(470, 382), new Color(180, 195, 195), 0.42f);
        for (var index = 0; index < logFiles.Count; index++)
        {
            var bounds = SettingsLogItemBounds(index);
            var selected = index == selectedIndex;
            FillRect(bounds, selected ? new Color(46, 77, 72) : bounds.Contains(mousePoint) ? new Color(36, 50, 58) : new Color(24, 31, 37));
            DrawRect(bounds, selected ? 2 : 1, selected ? new Color(99, 223, 185) : new Color(82, 111, 114));
            DrawFittedText(Path.GetFileName(logFiles[index]), new Rectangle(bounds.X + 16, bounds.Y + 5, bounds.Width - 32, 30), Color.White, 0.36f);
        }
        if (!string.IsNullOrWhiteSpace(message))
            DrawFittedText(message, new Rectangle(470, 780, 720, 30), new Color(255, 205, 140), 0.34f);
        DrawCommandButton(SettingsBackButtonBounds, "BACK", false, mousePoint, scale: 0.40f);
        DrawCommandButton(SettingsEditButtonBounds, "EDIT IN CODE", false, mousePoint, enabled: selectedIndex >= 0, scale: 0.34f);
        _spriteBatch.End();
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
