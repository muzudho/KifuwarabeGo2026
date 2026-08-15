namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>ローカル対局のセットアップ、対局中、終局後に共通する操作 UI を所有します。</summary>
public sealed class LocalMatchScreen
{
    public static LocalMatchScreen Default { get; } = new();

    private LocalMatchScreen()
    {
        StartPlayingButton = new Button(new Rectangle(1658, 920, 154, 56), "START", 0.48f);
        ImportSgfButton = new Button(new Rectangle(1492, 184, 320, 56), "KIFU INPUT (SGF)", 0.34f);
        BackToTitleButton = new Button(new Rectangle(1642, 104, 170, 52), "BACK TO TITLE", 0.32f);
        BlackPlayerKindRow = new PlayerKindSelectionRow(710);
        WhitePlayerKindRow = new PlayerKindSelectionRow(814);
        PonnukiBlackPlayerKindRow = new PlayerKindSelectionRow(646);
        PonnukiWhitePlayerKindRow = new PlayerKindSelectionRow(750);
        BlackSeedAutoChangeButton = new Button(new Rectangle(1164, 870, 307, 32), "BLACK", 0.22f);
        WhiteSeedAutoChangeButton = new Button(new Rectangle(1485, 870, 307, 32), "WHITE", 0.22f);
    }

    public Rectangle LocalUseCardBounds { get; } = new(508, 404, 438, 300);
    public Button StartPlayingButton { get; }
    public Button ImportSgfButton { get; }
    public Button BackToTitleButton { get; }
    public SetupRightSidePanel SetupRightSidePanel { get; } = new();
    public InitialPositionConciergeRightSidePanel InitialPositionConciergeRightSidePanel { get; } = new();
    public PlayerKindSelectionRow BlackPlayerKindRow { get; }
    public PlayerKindSelectionRow WhitePlayerKindRow { get; }
    public PlayerKindSelectionRow PonnukiBlackPlayerKindRow { get; }
    public PlayerKindSelectionRow PonnukiWhitePlayerKindRow { get; }
    public Button BlackSeedAutoChangeButton { get; }
    public Button WhiteSeedAutoChangeButton { get; }

    public GoStone? GetSeedAutoChangeHit(Point point) =>
        BlackSeedAutoChangeButton.IsHit(point) ? GoStone.Black :
        WhiteSeedAutoChangeButton.IsHit(point) ? GoStone.White : null;

    public PlayerKindSelectionRow GetPlayerKindRow(GoStone stone, bool isPonnuki) =>
        (stone, isPonnuki) switch
        {
            (GoStone.Black, false) => BlackPlayerKindRow,
            (GoStone.White, false) => WhitePlayerKindRow,
            (GoStone.Black, true) => PonnukiBlackPlayerKindRow,
            _ => PonnukiWhitePlayerKindRow,
        };

    public int GetHumanPlayerNameCaretIndex(KfwStationeryDrawingTools drawingContext, Point point,
        GoStone stone, string text, bool isPonnuki) =>
        drawingContext.GetTextCaretIndex(point.X, text,
            GetPlayerKindRow(stone, isPonnuki).HumanNameTextBounds, 0.42f);

    public GoStone? GetHumanPlayerNameHit(Point point, GoPlayerKind blackKind, GoPlayerKind whiteKind, bool isPonnuki)
    {
        if (blackKind == GoPlayerKind.Human && GetPlayerKindRow(GoStone.Black, isPonnuki).HumanNameRowBounds.Contains(point))
            return GoStone.Black;

        return whiteKind == GoPlayerKind.Human && GetPlayerKindRow(GoStone.White, isPonnuki).HumanNameRowBounds.Contains(point)
            ? GoStone.White
            : null;
    }

    public Rectangle GetPlayerSelectorBounds(GoStone stone, bool isPonnuki) =>
        PlayerSelectorLayout.CreatePlayerSelector(GetPlayerRowY(stone, isPonnuki)).Bounds;

    public Rectangle GetHandleBounds(GoStone stone, bool isPonnuki) =>
        new(1144, GetPlayerRowY(stone, isPonnuki) + 48, 668, 40);

    public Rectangle GetHandleTextBounds(GoStone stone, bool isPonnuki)
    {
        var bounds = GetHandleBounds(stone, isPonnuki);
        return new Rectangle(1328, bounds.Y + 4, 410, 30);
    }

    public GoStone? GetHandleHit(Point point, bool isPonnuki) =>
        GetHandleBounds(GoStone.Black, isPonnuki).Contains(point) ? GoStone.Black :
        GetHandleBounds(GoStone.White, isPonnuki).Contains(point) ? GoStone.White : null;

    private static int GetPlayerRowY(GoStone stone, bool isPonnuki) =>
        (stone, isPonnuki) switch
        {
            (GoStone.Black, false) => 710,
            (GoStone.White, false) => 814,
            (GoStone.Black, true) => 646,
            _ => 750,
        };
}
