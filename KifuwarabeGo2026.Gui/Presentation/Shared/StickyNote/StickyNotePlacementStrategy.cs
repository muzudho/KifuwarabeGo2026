namespace KifuwarabeGo2026.Gui.Presentation.Shared.StickyNote;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>StickyNote を表示する画面文脈です。未登録の文脈では表示しません。</summary>
public enum StickyNoteScreenId
{
    Unknown,
    TitleHome,
    EntryProfileEdit,
    ClientIdentitySelection,
    ClientIdentityEdit,
    QuickClientIdentitySelection,
    GtpEngineSelection,
    TournamentRulesSelection,
}

/// <summary>画面内の案内の識別子です。</summary>
public enum StickyNoteKind
{
    TitleFormalAppsHint,
    TitleCasualAppsHint,
    TitleLocalMatchHint,
    TitleOnlineMatchHint,
    TitleSettingsHint,
    TitlePonnukiPreview,
    EntryProfileFieldHint,
    ClientIdentityHandleHint,
    QuickClientIdentityHandleHint,
    GtpEnginePathHint,
    TournamentRulesPathHint,
}

public readonly record struct StickyNotePlacement(Rectangle Bounds, Vector2 ConnectorEnd);

public readonly record struct StickyNotePlacementContext(Vector2 ConnectorStart, Rectangle? AnchorBounds = null);

public interface IStickyNotePlacementStrategy
{
    bool TryGetPlacement(StickyNotePlacementContext context, out StickyNotePlacement placement);
}

/// <summary>
/// パンくずに対応する画面IDから、案内付箋の表示位置を一意に解決します。
/// 組み合わせが未登録なら非表示にするため、新画面追加時に付箋が漏れて表示されません。
/// </summary>
public static class StickyNotePlacementStrategies
{
    private static readonly IReadOnlyDictionary<(StickyNoteScreenId Screen, StickyNoteKind Kind), IStickyNotePlacementStrategy> Strategies =
        new Dictionary<(StickyNoteScreenId, StickyNoteKind), IStickyNotePlacementStrategy>
        {
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitleFormalAppsHint)] = Fixed(new Rectangle(70, 270, 390, 190), bounds => new Vector2(bounds.Right, bounds.Center.Y)),
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitleCasualAppsHint)] = Fixed(new Rectangle(1412, 270, 420, 190), bounds => new Vector2(bounds.Left, bounds.Center.Y)),
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitleLocalMatchHint)] = Fixed(new Rectangle(70, 370, 400, 174), bounds => new Vector2(bounds.Right, bounds.Center.Y)),
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitleOnlineMatchHint)] = Fixed(new Rectangle(70, 554, 400, 174), bounds => new Vector2(bounds.Right, bounds.Center.Y)),
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitleSettingsHint)] = Fixed(new Rectangle(1412, 760, 400, 160), bounds => new Vector2(bounds.Left, bounds.Center.Y)),
            [(StickyNoteScreenId.TitleHome, StickyNoteKind.TitlePonnukiPreview)] = Fixed(new Rectangle(1412, 390, 420, 174), bounds => new Vector2(bounds.Left, bounds.Center.Y)),
            [(StickyNoteScreenId.EntryProfileEdit, StickyNoteKind.EntryProfileFieldHint)] = Fixed(new Rectangle(1452, 418, 408, 122), bounds => new Vector2(bounds.Left, bounds.Center.Y)),
            [(StickyNoteScreenId.ClientIdentityEdit, StickyNoteKind.ClientIdentityHandleHint)] = Fixed(new Rectangle(1452, 418, 408, 122), bounds => new Vector2(bounds.Left, bounds.Center.Y)),
            [(StickyNoteScreenId.QuickClientIdentitySelection, StickyNoteKind.QuickClientIdentityHandleHint)] = Fixed(new Rectangle(560, 824, 800, 130), bounds => new Vector2(bounds.Center.X, bounds.Top)),
            [(StickyNoteScreenId.GtpEngineSelection, StickyNoteKind.GtpEnginePathHint)] = AtScreenBottom(),
            [(StickyNoteScreenId.TournamentRulesSelection, StickyNoteKind.TournamentRulesPathHint)] = AtScreenBottom(),
        };

    public static bool TryGetPlacement(
        StickyNoteScreenId screen,
        StickyNoteKind kind,
        StickyNotePlacementContext context,
        out StickyNotePlacement placement)
    {
        if (Strategies.TryGetValue((screen, kind), out var strategy))
            return strategy.TryGetPlacement(context, out placement);

        placement = default;
        return false;
    }

    private static IStickyNotePlacementStrategy Fixed(Rectangle bounds, Func<Rectangle, Vector2> connectorEnd) =>
        new FixedStickyNotePlacementStrategy(bounds, connectorEnd);

    private static IStickyNotePlacementStrategy AtScreenBottom() => new ScreenBottomStickyNotePlacementStrategy();

    private sealed class FixedStickyNotePlacementStrategy(Rectangle bounds, Func<Rectangle, Vector2> connectorEnd) : IStickyNotePlacementStrategy
    {
        public bool TryGetPlacement(StickyNotePlacementContext context, out StickyNotePlacement placement)
        {
            placement = new StickyNotePlacement(bounds, connectorEnd(bounds));
            return true;
        }
    }

    private sealed class ScreenBottomStickyNotePlacementStrategy : IStickyNotePlacementStrategy
    {
        public bool TryGetPlacement(StickyNotePlacementContext context, out StickyNotePlacement placement)
        {
            if (context.AnchorBounds is not { } anchorBounds)
            {
                placement = default;
                return false;
            }

            const int height = 370;
            var bounds = new Rectangle(anchorBounds.X, 1080 - height - 10, anchorBounds.Width, height);
            placement = new StickyNotePlacement(bounds, new Vector2(bounds.Center.X, bounds.Top));
            return true;
        }
    }
}
