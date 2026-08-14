namespace KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Application;

/// <summary>エントリープロファイルに付随する選択・編集UIを所有します。</summary>
public sealed class EntryProfilesScreen
{
    public static EntryProfilesScreen Default { get; } = new();

    private EntryProfilesScreen()
    {
        ConnectionSelection = new ClientIdentityConnectionSelectionControls();
        QuickSelection = new QuickClientIdentitySelectionControls();
        ProfileSelection = new ClientIdentityProfileSelectionControls();
        ProfileEdit = new ClientIdentityProfileEditControls();
    }

    public ClientIdentityConnectionSelectionControls ConnectionSelection { get; }
    public QuickClientIdentitySelectionControls QuickSelection { get; }
    public ClientIdentityProfileSelectionControls ProfileSelection { get; }
    public ClientIdentityProfileEditControls ProfileEdit { get; }
}

public sealed class ClientIdentityProfileSelectionControls
{
    public ClientIdentityProfileSelectionControls()
    {
        CloseButton = new Button(new Rectangle(1158, 182, 150, 48), "CLOSE", 0.34f);
        UseButton = new Button(new Rectangle(1320, 182, 150, 48), "USE", 0.34f);
        AddButton = new Button(new Rectangle(466, 820, 180, 48), "ADD", 0.34f);
        DuplicateButton = new Button(new Rectangle(663, 820, 180, 48), "DUPLICATE", 0.30f);
        EditButton = new Button(new Rectangle(860, 820, 180, 48), "EDIT", 0.34f);
        SetDefaultButton = new Button(new Rectangle(1057, 820, 220, 48), "SET DEFAULT", 0.30f);
        DeleteButton = new Button(new Rectangle(1294, 820, 180, 48), "DELETE", 0.34f);
    }

    public Button CloseButton { get; }
    public Button UseButton { get; }
    public Button AddButton { get; }
    public Button DuplicateButton { get; }
    public Button EditButton { get; }
    public Button SetDefaultButton { get; }
    public Button DeleteButton { get; }

    public Rectangle GetItemBounds(int index) => new(466, 290 + index * 92, 1008, 78);

    public int? GetItemHit(Point point, int itemCount)
    {
        for (var index = 0; index < itemCount; index++)
            if (GetItemBounds(index).Contains(point)) return index;
        return null;
    }
}

public sealed class ClientIdentityProfileEditControls
{
    public ClientIdentityProfileEditControls()
    {
        DiscardButton = new Button(new Rectangle(1158, 182, 150, 48), "DISCARD", 0.30f);
        SaveButton = new Button(new Rectangle(1320, 182, 150, 48), "CLOSE", 0.34f);
        UseButton = new Button(new Rectangle(1158, 182, 150, 48), "USE", 0.34f);
        AddCgosButton = new Button(new Rectangle(628, 820, 160, 48), "ADD CGOS", 0.30f);
        AddLocalButton = new Button(new Rectangle(466, 820, 150, 48), "ADD LOCAL", 0.30f);
        RemoveButton = new Button(new Rectangle(962, 820, 150, 48), "REMOVE", 0.34f);
    }

    public Button DiscardButton { get; }
    public Button SaveButton { get; }
    public Button UseButton { get; }
    public Button AddCgosButton { get; }
    public Button AddLocalButton { get; }
    public Button RemoveButton { get; }

    public Rectangle GetFieldTextBounds(ClientIdentityProfileEditField field) => field switch
    {
        ClientIdentityProfileEditField.DisplayName => new Rectangle(760, 365, 600, 42),
        ClientIdentityProfileEditField.LoginName => new Rectangle(760, 365, 600, 42),
        ClientIdentityProfileEditField.LoginPass => new Rectangle(760, 429, 600, 42),
        _ => throw new System.ArgumentOutOfRangeException(nameof(field), field, "Unknown target edit field."),
    };

    public Rectangle GetFieldHoverBounds(ClientIdentityProfileEditField field)
    {
        var textBounds = GetFieldTextBounds(field);
        return new Rectangle(536, textBounds.Y, textBounds.Right - 536, textBounds.Height);
    }

    public ClientIdentityProfileEditField? GetFieldHit(Point point) =>
        GetFieldTextBounds(ClientIdentityProfileEditField.LoginName).Contains(point) ? ClientIdentityProfileEditField.LoginName :
        GetFieldTextBounds(ClientIdentityProfileEditField.LoginPass).Contains(point) ? ClientIdentityProfileEditField.LoginPass : null;

    public void UpdateState(bool isDirty)
    {
        DiscardButton.IsEnabled = isDirty;
        SaveButton.Label = isDirty ? "SAVE & CLOSE" : "CLOSE";
        SaveButton.LabelScale = isDirty ? 0.26f : 0.34f;
    }
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
    private static ClientIdentityProfileSelectionControls Selection => Screen.ProfileSelection;
    private static ClientIdentityProfileEditControls Edit => Screen.ProfileEdit;
    internal static Rectangle ClientIdentityProfileSelectionCloseButtonBounds => Selection.CloseButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionUseButtonBounds => Selection.UseButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionAddButtonBounds => Selection.AddButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionDuplicateButtonBounds => Selection.DuplicateButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionEditButtonBounds => Selection.EditButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionSetDefaultButtonBounds => Selection.SetDefaultButton.Bounds;
    internal static Rectangle ClientIdentityProfileSelectionDeleteButtonBounds => Selection.DeleteButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditCancelButtonBounds => Edit.DiscardButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditSaveButtonBounds => Edit.SaveButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditUseButtonBounds => Edit.UseButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditAddCgosButtonBounds => Edit.AddCgosButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditAddLocalButtonBounds => Edit.AddLocalButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditRemoveButtonBounds => Edit.RemoveButton.Bounds;
    internal static Rectangle ClientIdentityProfileEditFieldTextBounds(int index, ClientIdentityProfileEditField field, bool isLocalMatch) => Edit.GetFieldTextBounds(field);
    internal static Rectangle ClientIdentityFieldHoverBounds(Rectangle textBounds) => new(536, textBounds.Y, textBounds.Right - 536, textBounds.Height);
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
