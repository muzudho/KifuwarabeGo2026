namespace KifuwarabeGo2026.Gui.Presentation.Pages.ApplicationSettings;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;

/// <summary>アプリ設定画面と、タイトル右下の設定操作 UI を所有します。</summary>
public sealed class ApplicationSettingsScreen
{
    public static ApplicationSettingsScreen Default { get; } = new();

    private ApplicationSettingsScreen()
    {
        SettingsButton = new Button(new Rectangle(1780, 972, 70, 62), "SETTINGS", 0.20f);
        UpdateButton = new Button(new Rectangle(1698, 972, 70, 62), "UPDATE", 0.20f);
        BackButton = new Button(new Rectangle(1390, 138, 140, 52), "BACK", 0.32f);
        LogTabButton = new Button(new Rectangle(440, 242, 250, 52), "LOG", 0.34f);
        OtherFoldersTabButton = new Button(new Rectangle(702, 242, 360, 52), "OTHER FOLDERS", 0.34f);
        OtherTabButton = new Button(new Rectangle(1074, 242, 250, 52), "OTHER", 0.34f);
        LogRootLink = CreateLink(new Rectangle(440, 374, 1070, 58), "BROWSE");
        SgfFolderLink = CreateLink(new Rectangle(440, 374, 1070, 70), "BROWSE");
        ScreenshotFolderLink = CreateLink(new Rectangle(440, 564, 1070, 70), "BROWSE");
        ApplicationSettingsFileLink = CreateLink(new Rectangle(440, 374, 1070, 70), "OPEN");
        EngineSettingsFileLink = CreateLink(new Rectangle(440, 564, 1070, 70), "OPEN");
        LogItemLinks = new LinkUnderline[5];
        for (var index = 0; index < LogItemLinks.Length; index++)
            LogItemLinks[index] = CreateLink(GetLogItemBounds(index), "OPEN", inset: true);
    }

    public Button SettingsButton { get; }
    public Button UpdateButton { get; }
    public Button BackButton { get; }
    public Button LogTabButton { get; }
    public Button OtherFoldersTabButton { get; }
    public Button OtherTabButton { get; }
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
        _ => OtherTabButton,
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
        link.SetActionBadge(ActionBadge.Create(action, bounds, 0.30f));
        return link;
    }
}
