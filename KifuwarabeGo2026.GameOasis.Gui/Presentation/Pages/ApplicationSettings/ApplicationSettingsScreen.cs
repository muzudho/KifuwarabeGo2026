namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.ApplicationSettings;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;

public enum ApplicationSettingsPage
{
    Log,
    OtherFolders,
    Other,
    Sound,
}

/// <summary>アプリ設定画面と、タイトル右下の設定操作 UI を所有します。</summary>
public sealed class ApplicationSettingsScreen
{
    public static ApplicationSettingsScreen Default { get; } = new();

    private ApplicationSettingsScreen()
    {
        SettingsButton = new GearButton(new Rectangle(1780, 972, 70, 62));
        UpdateButton = new Button(new Rectangle(1316, 972, 220, 62), "ランチャーを更新", 0.22f);
        OpenLauncherButton = new Button(new Rectangle(1548, 972, 220, 62), "ランチャーを開く", 0.22f);
        BackButton = new Button(new Rectangle(1390, 138, 140, 52), "BACK", 0.32f);
        LogTabButton = new Button(new Rectangle(440, 242, 250, 52), "LOG", 0.34f);
        OtherFoldersTabButton = new Button(new Rectangle(702, 242, 360, 52), "OTHER FOLDERS", 0.34f);
        OtherTabButton = new Button(new Rectangle(1074, 242, 250, 52), "OTHER", 0.34f);
        SoundTabButton = new Button(new Rectangle(1336, 242, 174, 52), "SOUND", 0.34f);
        StoneSoundTestButton = new Button(new Rectangle(1240, 374, 220, 54), "TEST", 0.34f);
        DiscardSoundTestButton = new Button(new Rectangle(1240, 494, 220, 54), "TEST", 0.34f);
        ShutterSoundTestButton = new Button(new Rectangle(1240, 614, 220, 54), "TEST", 0.34f);
        MatchAlarmSoundTestButton = new Button(new Rectangle(1240, 734, 220, 54), "TEST", 0.34f);
        LogRootLink = CreateLink(new Rectangle(440, 374, 1070, 58), "BROWSE");
        SgfFolderLink = CreateLink(new Rectangle(440, 374, 1070, 70), "BROWSE");
        ScreenshotFolderLink = CreateLink(new Rectangle(440, 564, 1070, 70), "BROWSE");
        ApplicationSettingsFileLink = CreateLink(new Rectangle(440, 374, 1070, 70), "OPEN");
        EngineSettingsFileLink = CreateLink(new Rectangle(440, 564, 1070, 70), "OPEN");
        LogItemLinks = new LinkUnderline[5];
        for (var index = 0; index < LogItemLinks.Length; index++)
            LogItemLinks[index] = CreateLink(GetLogItemBounds(index), "OPEN", inset: true);
    }

    public GearButton SettingsButton { get; }
    public Button UpdateButton { get; }
    public Button OpenLauncherButton { get; }
    public Button BackButton { get; }
    public Button LogTabButton { get; }
    public Button OtherFoldersTabButton { get; }
    public Button OtherTabButton { get; }
    public Button SoundTabButton { get; }
    public Button StoneSoundTestButton { get; }
    public Button DiscardSoundTestButton { get; }
    public Button ShutterSoundTestButton { get; }
    public Button MatchAlarmSoundTestButton { get; }
    public LinkUnderline LogRootLink { get; }
    public LinkUnderline SgfFolderLink { get; }
    public LinkUnderline ScreenshotFolderLink { get; }
    public LinkUnderline ApplicationSettingsFileLink { get; }
    public LinkUnderline EngineSettingsFileLink { get; }
    public LinkUnderline[] LogItemLinks { get; }

    public Button GetTabButton(ApplicationSettingsPage page) => page switch
    {
        ApplicationSettingsPage.Log => LogTabButton,
        ApplicationSettingsPage.OtherFolders => OtherFoldersTabButton,
        ApplicationSettingsPage.Other => OtherTabButton,
        _ => SoundTabButton,
    };

    public ApplicationSettingsPage? GetTabHit(Point point)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
            if (GetTabButton(page).IsHit(point)) return page;
        return null;
    }

    public int? GetLogItemHit(Point point, int count)
    {
        for (var index = 0; index < Math.Min(count, LogItemLinks.Length); index++)
            if (LogItemLinks[index].IsHit(point)) return index;
        return null;
    }

    public bool IsSelectedLogOpenBadgeHit(Point point, int selectedIndex) =>
        selectedIndex >= 0 && selectedIndex < LogItemLinks.Length &&
        LogItemLinks[selectedIndex].ActionBadge is { IsVisible: true } badge && badge.Bounds.Contains(point);

    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePosition, ApplicationSettingsPage page,
        string logRoot, string sgfSaveDirectory, string screenshotSaveDirectory,
        string applicationSettingsPath, string engineSettingsPath,
        IReadOnlyList<string> logFiles, int selectedIndex, string message)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        drawingContext.DrawBackground();
        var panel = new Rectangle(390, 100, 1180, 860);
        drawingContext.FillRectangle(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        drawingContext.FillRectangle(panel, new Color(21, 25, 32, 246));
        drawingContext.DrawRectangle(panel, 2, new Color(82, 111, 114));
        drawingContext.DrawText("APPLICATION SETTINGS", new Vector2(440, 148), new Color(244, 238, 218), 0.78f);
        BackButton.Draw(mousePoint, drawingContext);
        DrawTabs(drawingContext, page, mousePoint);

        if (page == ApplicationSettingsPage.Log)
        {
            foreach (var link in LogItemLinks) link.ActionBadge?.Hide();
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 354, 1070, 180), "ROOT FOLDER", new Color(67, 112, 118));
            DrawValueField(drawingContext, string.Empty, logRoot, LogRootLink, mousePoint);
            drawingContext.DrawFittedText("GUI   " + Path.Combine(logRoot, "Gui"), new Rectangle(440, 452, 1050, 24), new Color(180, 195, 195), 0.30f);
            drawingContext.DrawFittedText("CGOS  " + Path.Combine(logRoot, "Cgos"), new Rectangle(440, 480, 1050, 24), new Color(180, 195, 195), 0.30f);
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 548, 1070, 282), "RECENT GUI LOGS", new Color(76, 91, 126));
            for (var index = 0; index < logFiles.Count; index++)
            {
                var link = LogItemLinks[index];
                var bounds = link.Bounds;
                var selected = index == selectedIndex;
                var hovered = bounds.Contains(mousePoint);
                drawingContext.DrawFittedText(Path.GetFileName(logFiles[index]), new Rectangle(bounds.X + 8, bounds.Y + 6, bounds.Width - 16, 28), Color.White, 0.31f);
                link.UpdatePointer(mousePoint);
                link.SetSelected(selected);
                link.Draw(drawingContext);
                if (selected && hovered)
                {
                    link.ActionBadge?.Show();
                    link.ActionBadge?.Draw(drawingContext);
                }
            }
        }
        else if (page == ApplicationSettingsPage.OtherFolders)
        {
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "SGF", new Color(67, 112, 118));
            DrawValueField(drawingContext, string.Empty, string.IsNullOrWhiteSpace(sgfSaveDirectory) ? "(NOT SET - FIRST SAVE WILL BE REMEMBERED)" : sgfSaveDirectory, SgfFolderLink, mousePoint);
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 548, 1070, 174), "SCREENSHOT", new Color(76, 91, 126));
            DrawValueField(drawingContext, string.Empty, screenshotSaveDirectory, ScreenshotFolderLink, mousePoint);
            drawingContext.DrawFittedText("Ctrl + P captures the whole game window, including its frame.", new Rectangle(440, 656, 1020, 28), new Color(180, 195, 195), 0.30f);
        }
        else if (page == ApplicationSettingsPage.Other)
        {
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 354, 1070, 152), "APPLICATION", new Color(67, 112, 118));
            DrawValueField(drawingContext, string.Empty, applicationSettingsPath, ApplicationSettingsFileLink, mousePoint);
            drawingContext.DrawVerticalResultSection(new Rectangle(440, 548, 1070, 152), "ENGINE", new Color(76, 91, 126));
            DrawValueField(drawingContext, string.Empty, engineSettingsPath, EngineSettingsFileLink, mousePoint);
        }
        else
        {
            DrawSoundTestRow(drawingContext, mousePoint, new Rectangle(440, 354, 1070, 94), "STONE", "Stone placement sound", StoneSoundTestButton, new Color(67, 112, 118));
            DrawSoundTestRow(drawingContext, mousePoint, new Rectangle(440, 474, 1070, 94), "DISCARD", "Discard and screen transition sound", DiscardSoundTestButton, new Color(76, 91, 126));
            DrawSoundTestRow(drawingContext, mousePoint, new Rectangle(440, 594, 1070, 94), "SHUTTER", "Screenshot shutter sound", ShutterSoundTestButton, new Color(105, 91, 72));
            DrawSoundTestRow(drawingContext, mousePoint, new Rectangle(440, 714, 1070, 94), "MATCH ALARM", "Upcoming match call alarm", MatchAlarmSoundTestButton, new Color(87, 107, 82));
            drawingContext.DrawFittedText("If you cannot hear a test, check the Windows volume mixer and this app's output device.",
                new Rectangle(440, 842, 1070, 34), new Color(255, 205, 140), 0.30f);
        }

        if (!string.IsNullOrWhiteSpace(message))
            drawingContext.DrawFittedText(message, new Rectangle(440, 910, 780, 24), new Color(255, 205, 140), 0.28f);
        drawingContext.End();
    }

    public void DrawSettingsButton(KfwStationeryDrawingTools drawingContext, Point mousePoint)
    {
        SettingsButton.Draw(drawingContext, mousePoint);
    }

    private void DrawTabs(KfwStationeryDrawingTools drawingContext, ApplicationSettingsPage selectedPage, Point mousePoint)
    {
        foreach (var page in Enum.GetValues<ApplicationSettingsPage>())
        {
            var bounds = GetTabButton(page).Bounds;
            drawingContext.FillRectangle(bounds, page == selectedPage ? new Color(31, 49, 49) : bounds.Contains(mousePoint) ? new Color(29, 39, 46) : new Color(21, 28, 34));
            var label = page switch { ApplicationSettingsPage.Log => "LOG", ApplicationSettingsPage.OtherFolders => "OTHER FOLDERS", ApplicationSettingsPage.Other => "OTHER", _ => "SOUND" };
            drawingContext.DrawFittedText(label, new Rectangle(bounds.X + 18, bounds.Y + 9, bounds.Width - 36, 30), Color.White, 0.34f);
            drawingContext.FillRoundedRectangle(new Rectangle(bounds.X + 10, bounds.Bottom - 7, bounds.Width - 20, 5), 2,
                page == selectedPage ? new Color(99, 223, 185) : bounds.Contains(mousePoint) ? new Color(185, 196, 255) : new Color(58, 78, 86));
        }
    }

    private static void DrawSoundTestRow(KfwStationeryDrawingTools drawingContext, Point mousePoint, Rectangle bounds,
        string heading, string description, Button testButton, Color accent)
    {
        drawingContext.DrawVerticalResultSection(bounds, heading, accent);
        drawingContext.DrawFittedText(description, new Rectangle(bounds.X + 48, bounds.Y + 30, 700, 34), Color.White, 0.38f);
        testButton.Draw(mousePoint, drawingContext);
    }

    private static void DrawValueField(KfwStationeryDrawingTools drawingContext, string label, string value, LinkUnderline link, Point mousePoint)
    {
        var bounds = link.Bounds;
        var hovered = bounds.Contains(mousePoint);
        if (!string.IsNullOrWhiteSpace(label))
            drawingContext.DrawText(label, new Vector2(bounds.X + 8, bounds.Y), new Color(180, 195, 195), 0.34f);
        var valueY = string.IsNullOrWhiteSpace(label) ? bounds.Y + 16 : bounds.Y + 30;
        drawingContext.DrawFittedText(value, new Rectangle(bounds.X + 8, valueY, bounds.Width - (hovered ? 132 : 16), 28), Color.White, 0.32f);
        link.UpdatePointer(mousePoint);
        link.Draw(drawingContext);
    }

    private static Rectangle GetLogItemBounds(int index) => new(440, 570 + index * 58, 1070, 48);

    private static LinkUnderline CreateLink(Rectangle bounds, string action, bool inset = false)
    {
        var linkBounds = inset ? new Rectangle(bounds.X + 8, bounds.Y, bounds.Width - 16, bounds.Height + 1) : bounds;
        var link = new LinkUnderline(new RoundUnderline
        {
            TopOffset = -7,
            Thickness = inset ? 5 : 6,
            Radius = inset ? 2 : 3,
        }) { Bounds = linkBounds };
        link.SetActionBadge(ActionBadgeComponent.Create(action, bounds, 0.30f));
        return link;
    }
}
