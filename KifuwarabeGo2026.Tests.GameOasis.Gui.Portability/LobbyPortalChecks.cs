namespace KifuwarabeGo2026.Tests.GameOasis.Gui.Portability;

using System;
using System.Linq;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.LobbyGui.Application;
using KifuwarabeGo2026.LobbyGui.MonoGame;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;

internal static class LobbyPortalChecks
{
    public static void Run()
    {
        var navigation = new LobbyNavigationController();
        var input = new LobbyHomeInputCoordinator(navigation);
        var home = input.Home;
        Check(home.Catalog.Count == 4 && home.PageCount == 1 && !home.CanPrevious && !home.CanNext, "Default catalog and disabled pager");
        Check(!input.ChangePage(-1) && !input.ChangePage(1), "Single page must not move");
        var actions = new[] { LobbyHomeAction.OpenLocalMatch, LobbyHomeAction.OpenOnlineMatch,
            LobbyHomeAction.OpenCaptureGame, LobbyHomeAction.OpenReferenceGo };
        for (var slot = 0; slot < 4; slot++)
        {
            input.OpenHome();
            var target = TitleScreen.Default.GetHomeTargetHit(LobbyPortalLayout.Card(slot).Center, home);
            Check(target == home.Select(slot) && input.Activate(target!.Value) == actions[slot], "Visible card must activate its intended entry");
        }
        Check(input.CurrentPage == LobbyPage.Home, "Reference Go must not navigate to the intermediate catalog");
        var expanded = home.Catalog.Concat(home.Catalog.Take(2)).ToArray();
        var paged = new LobbyHomeInputCoordinator(new LobbyNavigationController(), expanded);
        Check(paged.Home.CanNext && paged.ChangePage(1) && paged.Home.PageIndex == 1 && paged.Home.VisibleItems.Count == 2,
            "Six items need a second page with two visible items");
        Check(!paged.Home.CanNext && paged.Home.CanPrevious && !paged.ChangePage(1), "Last page boundary");
        Check(TitleScreen.Default.GetHomeTargetHit(LobbyPortalLayout.Card(0).Center, paged.Home) == expanded[4].Target &&
            TitleScreen.Default.GetHomeTargetHit(LobbyPortalLayout.Card(2).Center, paged.Home) is null, "Hidden slots must not activate old entries");
        Check(paged.ChangePage(-1) && paged.Home.PageIndex == 0, "Previous page");
        paged.Activate(LobbyHomeTarget.CaptureGame);
        Check(!paged.ChangePage(1), "Pager must not operate on child pages");
        var empty = LobbyHomePresenter.Create(99, []);
        Check(empty.PageIndex == 0 && empty.PageCount == 1 && empty.VisibleItems.Count == 0 && empty.Select(0) is null, "Empty catalog");
        Check(home.Select(-1) is null && home.Select(4) is null, "Invalid visible index");

        var controls = new[] { LobbyPortalLayout.Engine, LobbyPortalLayout.Entry, LobbyPortalLayout.Settings,
            LobbyPortalLayout.UpdateInstaller, LobbyPortalLayout.OpenInstaller, LobbyPortalLayout.Previous, LobbyPortalLayout.Next }
            .Concat(Enumerable.Range(0, 4).Select(LobbyPortalLayout.Card)).ToArray();
        for (var i = 0; i < controls.Length; i++)
        {
            Check(LobbyPortalLayout.Panel.Contains(controls[i]), "Controls must stay in portal bounds");
            for (var j = i + 1; j < controls.Length; j++) Check(!controls[i].Intersects(controls[j]), "Portal controls must not overlap");
        }
        Check(TitleScreen.Default.GetHomeTargetHit(new Point(1810, 990)) is null, "Old settings corner must not activate a card");
    }

    private static void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException("Lobby portal: " + message); }
}
