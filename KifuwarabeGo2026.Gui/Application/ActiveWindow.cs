namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;

/// <summary>入力を受け取れるモーダル画面を識別します。</summary>
public enum ActiveWindowId
{
    None,
    GtpEngineSelection,
    GtpEngineEdit,
    GtpEngineGuiOptions,
    GtpEngineComboSelection,
    GtpEngineDeleteConfirmation,
    PlayerSelection,
    PlayerEdit,
    ClientIdentitySelection,
    ClientIdentityEdit,
    ClientIdentityConnectionSelection,
    QuickClientIdentitySelection,
    TournamentRulesSelection,
    TournamentRulesEdit,
    TournamentRulesDeleteConfirmation,
    CgosAdminPlayerSelection,
    CgosConnectionEdit,
    CatalogOrderEditor,
    TextInput,
    IntegerInput,
    CommentEditor,
    ReviewUnsavedChangesConfirmation,
    ReviewChartPopup,
    InitialPositionConcierge,
    ApplicationSettings,
    BoardEditing,
    VariationEditing,
}

/// <summary>
/// モーダル画面の親子関係を保持します。入力は常に最前面の画面だけへ送ります。
/// </summary>
public sealed class ActiveWindowStack
{
    private readonly List<ActiveWindowId> _items = [];

    public ActiveWindowId Current => _items.Count == 0 ? ActiveWindowId.None : _items[^1];

    public void Activate(ActiveWindowId windowId)
    {
        if (windowId == ActiveWindowId.None)
            return;

        _items.Remove(windowId);
        _items.Add(windowId);
    }

    public void Deactivate(ActiveWindowId windowId) => _items.Remove(windowId);
}
