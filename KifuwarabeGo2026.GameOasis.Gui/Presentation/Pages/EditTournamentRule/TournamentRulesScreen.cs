namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>大会ルールの選択画面、選択ダイアログ、削除確認UIを所有します。</summary>
public sealed class TournamentRulesScreen
{
    public static TournamentRulesScreen Default { get; } = new();

    private TournamentRulesScreen()
    {
        BrowseButton = new Button(new Rectangle(1144, 184, 320, 56), "TOURNAMENT SELECT", 0.32f);
        SelectionCancelButton = new Button(new Rectangle(1368, 156, 132, 48), "CANCEL", 0.34f);
        SelectionOkButton = new Button(new Rectangle(1518, 156, 132, 48), "SELECT", 0.34f);
        AddButton = new Button(new Rectangle(270, 874, 100, 44), "ADD", 0.42f);
        EditButton = new Button(new Rectangle(380, 874, 100, 44), "EDIT", 0.42f);
        DuplicateButton = new Button(new Rectangle(490, 874, 120, 44), "DUPLICATE", 0.34f);
        DeleteButton = new Button(new Rectangle(620, 874, 100, 44), "DELETE", 0.42f);
        OrderButton = new Button(new Rectangle(740, 874, 120, 44), "ORDER", 0.38f);
        PreviousPageButton = new Button(new Rectangle(730, 816, 90, 44), "PREV", 0.42f);
        NextPageButton = new Button(new Rectangle(830, 816, 90, 44), "NEXT", 0.42f);
        DeleteCancelButton = new Button(new Rectangle(940, 574, 140, 48), "CANCEL", 0.42f);
        DeleteConfirmButton = new Button(new Rectangle(1104, 574, 140, 48), "DELETE", 0.42f);
    }

    public Rectangle SelectionDialogBounds { get; } = new(230, 126, 1460, 820);
    public Rectangle SelectionListBounds { get; } = new(270, 242, 650, 560);
    public Rectangle SelectionPropertyBounds { get; } = new(950, 270, 700, 532);
    public Rectangle DeleteConfirmationBounds { get; } = new(640, 390, 640, 260);
    public Rectangle AddPanelBounds { get; } = new(430, 126, 1060, 820);
    public Rectangle AddPanelEditorBounds { get; } = new(520, 228, 880, 590);
    public Rectangle AddPanelDisplayNameRowBounds { get; } = new(626, 244, 668, 56);
    public Rectangle AddPanelDisplayNameTextBounds { get; } = new(758, 251, 520, 42);
    public Rectangle AddPanelFileRowBounds { get; } = new(626, 676, 668, 56);
    public Button BrowseButton { get; }
    public Button SelectionCancelButton { get; }
    public Button SelectionOkButton { get; }
    public Button AddButton { get; }
    public Button EditButton { get; }
    public Button DuplicateButton { get; }
    public Button DeleteButton { get; }
    public Button OrderButton { get; }
    public Button PreviousPageButton { get; }
    public Button NextPageButton { get; }
    public Button DeleteCancelButton { get; }
    public Button DeleteConfirmButton { get; }
    public TournamentRuleKomiField KomiField { get; } = new();
    public TournamentRuleMoveLimitField MoveLimitField { get; } = new();
    public TournamentRuleTimeField TimeField { get; } = new();

    public Rectangle GetListItemBounds(int index) =>
        new(SelectionListBounds.X + 16, SelectionListBounds.Y + 16 + index * 88, SelectionListBounds.Width - 32, 72);

    public Rectangle GetPropertyRowBounds(int index) =>
        new(SelectionPropertyBounds.X + 18, SelectionPropertyBounds.Y + 22 + index * 70, SelectionPropertyBounds.Width - 36, 52);

    public int? GetListItemHit(Point point, int pageIndex, int itemCount, int pageSize)
    {
        for (var row = 0; row < pageSize; row++)
        {
            if (!GetListItemBounds(row).Contains(point)) continue;
            var index = pageIndex * pageSize + row;
            return index < itemCount ? index : null;
        }

        return null;
    }

    public void UpdateSelectionState(int itemCount, bool canDelete, int pageIndex, int pageCount)
    {
        EditButton.IsEnabled = itemCount > 0;
        DuplicateButton.IsEnabled = itemCount > 0;
        DeleteButton.IsEnabled = canDelete;
        OrderButton.IsEnabled = itemCount > 1;
        PreviousPageButton.IsEnabled = pageIndex > 0;
        NextPageButton.IsEnabled = pageIndex < pageCount - 1;
    }
}

/// <summary>移行中のrendererが画面所有領域を参照するための名前付きアダプターです。</summary>
internal static class TournamentRulesScreenBounds
{
    private static TournamentRulesScreen Screen => TournamentRulesScreen.Default;

    internal static Rectangle TournamentRulesSelectionDialogBounds => Screen.SelectionDialogBounds;
    internal static Rectangle TournamentRulesSelectionDialogListBounds => Screen.SelectionListBounds;
    internal static Rectangle TournamentRulesSelectionDialogPropertyBounds => Screen.SelectionPropertyBounds;
    internal static Rectangle TournamentRulesSelectionDialogListItemBounds(int index) => Screen.GetListItemBounds(index);
    internal static Rectangle TournamentRulesSelectionDialogPropertyRowBounds(int index) => Screen.GetPropertyRowBounds(index);
    internal static Rectangle TournamentRulesDeleteConfirmationBounds => Screen.DeleteConfirmationBounds;
    internal static Rectangle TournamentRulesAddPanelBounds => Screen.AddPanelBounds;
    internal static Rectangle TournamentRulesAddPanelEditorBounds => Screen.AddPanelEditorBounds;
    internal static Rectangle TournamentRulesAddPanelDisplayNameRowBounds => Screen.AddPanelDisplayNameRowBounds;
    internal static Rectangle TournamentRulesAddPanelDisplayNameTextBounds => Screen.AddPanelDisplayNameTextBounds;
    internal static Rectangle TournamentRulesAddPanelFileRowBounds => Screen.AddPanelFileRowBounds;
}
