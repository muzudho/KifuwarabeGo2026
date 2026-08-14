namespace KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>エントリープロファイルに付随する選択・編集UIを所有します。</summary>
public sealed class EntryProfilesScreen
{
    public static EntryProfilesScreen Default { get; } = new();

    private EntryProfilesScreen()
    {
        ConnectionSelection = new ClientIdentityConnectionSelectionControls();
        QuickSelection = new QuickClientIdentitySelectionControls();
    }

    public ClientIdentityConnectionSelectionControls ConnectionSelection { get; }
    public QuickClientIdentitySelectionControls QuickSelection { get; }
}

public sealed class ClientIdentityConnectionSelectionControls
{
    public ClientIdentityConnectionSelectionControls()
    {
        CancelButton = new Button(new Rectangle(1050, 236, 140, 48), "CANCEL", 0.34f);
        SelectButton = new Button(new Rectangle(1202, 236, 170, 48), "SELECT", 0.34f);
        PreviousButton = new Button(new Rectangle(1060, 798, 120, 44), "PREV", 0.34f);
        NextButton = new Button(new Rectangle(1192, 798, 120, 44), "NEXT", 0.34f);
    }

    public Rectangle PanelBounds { get; } = new(510, 210, 900, 660);
    public Button CancelButton { get; }
    public Button SelectButton { get; }
    public Button PreviousButton { get; }
    public Button NextButton { get; }

    public Rectangle GetItemBounds(int slot) => new(544, 332 + slot * 82, 832, 70);

    public int? GetItemHit(Point point, int pageIndex, int pageSize, int itemCount)
    {
        var start = pageIndex * pageSize;
        for (var slot = 0; slot < pageSize; slot++)
        {
            var index = start + slot;
            if (index >= itemCount) break;
            if (GetItemBounds(slot).Contains(point)) return index;
        }
        return null;
    }
}

public sealed class QuickClientIdentitySelectionControls
{
    public QuickClientIdentitySelectionControls()
    {
        CancelButton = new Button(new Rectangle(1030, 272, 140, 48), "CANCEL", 0.34f);
        SelectButton = new Button(new Rectangle(1182, 272, 140, 48), "SELECT", 0.34f);
    }

    public Rectangle PanelBounds { get; } = new(560, 245, 800, 560);
    public Button CancelButton { get; }
    public Button SelectButton { get; }

    public Rectangle GetItemBounds(int index) => new(592, 392 + index * 72, 736, 62);

    public int? GetItemHit(Point point, int itemCount)
    {
        for (var index = 0; index < itemCount; index++)
            if (GetItemBounds(index).Contains(point)) return index;
        return null;
    }
}

internal static class EntryProfilesScreenBounds
{
    private static EntryProfilesScreen Screen => EntryProfilesScreen.Default;
    private static ClientIdentityConnectionSelectionControls Connection => Screen.ConnectionSelection;
    private static QuickClientIdentitySelectionControls Quick => Screen.QuickSelection;
    internal static Rectangle ClientIdentityProfileConnectionSelectionPanelBounds => Connection.PanelBounds;
    internal static Rectangle ClientIdentityProfileConnectionSelectionCancelButtonBounds => Connection.CancelButton.Bounds;
    internal static Rectangle ClientIdentityProfileConnectionSelectionSelectButtonBounds => Connection.SelectButton.Bounds;
    internal static Rectangle ClientIdentityProfileConnectionSelectionPreviousButtonBounds => Connection.PreviousButton.Bounds;
    internal static Rectangle ClientIdentityProfileConnectionSelectionNextButtonBounds => Connection.NextButton.Bounds;
    internal static Rectangle ClientIdentityProfileConnectionSelectionItemBounds(int slot) => Connection.GetItemBounds(slot);
    internal static Rectangle QuickClientIdentitySelectionPanelBounds => Quick.PanelBounds;
    internal static Rectangle QuickClientIdentitySelectionCancelButtonBounds => Quick.CancelButton.Bounds;
    internal static Rectangle QuickClientIdentitySelectionSelectButtonBounds => Quick.SelectButton.Bounds;
    internal static Rectangle QuickClientIdentitySelectionItemBounds(int index) => Quick.GetItemBounds(index);
}
