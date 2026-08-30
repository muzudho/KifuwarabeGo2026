namespace KifuwarabeGo2026.LobbyGui.Application;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.GameOasis.Contracts.ProtocolG;

/// <summary>公開Protocol GカタログをLobbyのGame Oasis一覧表示へ投影します。</summary>
public static class LobbyGameOasisPresenter
{
    public const int MaximumVisibleItems = 4;
    public const string Breadcrumb = "GAME OASIS  >  SELECT PLAY-SPACE";
    public const string LoadingMessage = "CONNECTING TO GAME OASIS...";
    public const string ImplementationLabel = "IMPLEMENTATION";
    public const string OpenLabel = "OPEN  >";

    public static LobbyGameOasisPresentation Create(IReadOnlyList<GuiPlaySpaceEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        return new LobbyGameOasisPresentation(
            entries.Take(MaximumVisibleItems)
                .Select(CreateItem)
                .ToArray(),
            Math.Max(0, entries.Count - MaximumVisibleItems),
            Breadcrumb,
            LoadingMessage,
            ImplementationLabel,
            OpenLabel);
    }

    private static LobbyGameOasisItem CreateItem(GuiPlaySpaceEntry entry)
    {
        var (firstLine, secondLine) = SplitImplementationName(entry.ImplementationName);
        return new LobbyGameOasisItem(
            entry.TypeId,
            entry.DisplayName,
            firstLine,
            secondLine,
            $"v{entry.ImplementationVersion}");
    }

    internal static (string FirstLine, string SecondLine) SplitImplementationName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return ("-", "");
        var center = value.Length / 2;
        var separators = value.Select((character, index) => (character, index))
            .Where(item => item.character == '.' && item.index > 0 && item.index < value.Length - 1)
            .Select(item => item.index)
            .ToArray();
        if (separators.Length == 0) return (value, "");
        var split = separators.MinBy(index => Math.Abs(index - center));
        return (value[..split], value[(split + 1)..]);
    }
}

public sealed record LobbyGameOasisPresentation(
    IReadOnlyList<LobbyGameOasisItem> VisibleItems,
    int RemainingItemCount,
    string Breadcrumb,
    string LoadingMessage,
    string ImplementationLabel,
    string OpenLabel)
{
    public bool IsLoading => VisibleItems.Count == 0;
    public string? RemainingMessage => RemainingItemCount > 0
        ? $"+ {RemainingItemCount} MORE PLAY-SPACES"
        : null;

    public LobbyGameOasisSelectionIntent? Select(int visibleIndex) =>
        visibleIndex >= 0 && visibleIndex < VisibleItems.Count
            ? new LobbyGameOasisSelectionIntent(VisibleItems[visibleIndex].TypeId)
            : null;
}

public sealed record LobbyGameOasisItem(
    PlaySpaceTypeId TypeId,
    string DisplayName,
    string ImplementationFirstLine,
    string ImplementationSecondLine,
    string VersionLabel);

public sealed record LobbyGameOasisSelectionIntent(PlaySpaceTypeId PlaySpaceTypeId);
