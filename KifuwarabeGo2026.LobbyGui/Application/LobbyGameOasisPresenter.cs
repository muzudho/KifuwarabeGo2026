namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>公開Protocol GカタログをLobbyのGame Oasis一覧表示へ投影します。</summary>
public static class LobbyGameOasisPresenter
{
    public const int MaximumVisibleItems = 4;

    public static LobbyGameOasisPresentation Create(IReadOnlyList<GuiPlaySpaceEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new LobbyGameOasisPresentation(
            entries.Take(MaximumVisibleItems)
                .Select(entry => new LobbyGameOasisItem(
                    entry.TypeId,
                    entry.DisplayName,
                    entry.ImplementationName,
                    entry.ImplementationVersion))
                .ToArray(),
            Math.Max(0, entries.Count - MaximumVisibleItems));
    }
}

public sealed record LobbyGameOasisPresentation(
    IReadOnlyList<LobbyGameOasisItem> VisibleItems,
    int RemainingItemCount)
{
    public bool IsLoading => VisibleItems.Count == 0;

    public LobbyGameOasisSelectionIntent? Select(int visibleIndex) =>
        visibleIndex >= 0 && visibleIndex < VisibleItems.Count
            ? new LobbyGameOasisSelectionIntent(VisibleItems[visibleIndex].TypeId)
            : null;
}

public sealed record LobbyGameOasisItem(
    PlaySpaceTypeId TypeId,
    string DisplayName,
    string ImplementationName,
    string ImplementationVersion);

public sealed record LobbyGameOasisSelectionIntent(PlaySpaceTypeId PlaySpaceTypeId);
