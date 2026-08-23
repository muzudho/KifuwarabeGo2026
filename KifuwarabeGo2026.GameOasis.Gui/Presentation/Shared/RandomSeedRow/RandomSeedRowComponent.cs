namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RandomSeedRow;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>Local Match と Ponnuki で共有するランダムシード入力行です。</summary>
public sealed class RandomSeedRowComponent
{
    public static RandomSeedRowComponent LocalMatch { get; } = new(includeProvider: false);
    public static RandomSeedRowComponent Ponnuki { get; } = new(includeProvider: true);
    public static RandomSeedRowComponent Cgos { get; } = new(includeProvider: false);

    private readonly LinkUnderline? _providerLink;
    private readonly LinkUnderline _blackLink;
    private readonly LinkUnderline _whiteLink;
    private readonly LinkUnderline _cgosBlackLink = CreateLink(new Rectangle(436, 710, 264, 32));
    private readonly LinkUnderline _cgosWhiteLink = CreateLink(new Rectangle(894, 710, 264, 32));

    private RandomSeedRowComponent(bool includeProvider)
    {
        if (includeProvider)
        {
            _providerLink = CreateLink(new Rectangle(1248, 870, 116, 32));
            _blackLink = CreateLink(new Rectangle(1408, 870, 170, 32));
            _whiteLink = CreateLink(new Rectangle(1622, 870, 170, 32));
        }
        else
        {
            _blackLink = CreateLink(new Rectangle(1240, 870, 225, 32));
            _whiteLink = CreateLink(new Rectangle(1560, 870, 225, 32));
        }
    }

    public RandomSeedRowTarget? GetHit(Point point, RandomSeedRowModel model)
    {
        if (_providerLink is not null && model.ProviderVisible && _providerLink.IsHit(point))
            return RandomSeedRowTarget.Provider;
        if (model.BlackVisible && _blackLink.IsHit(point)) return RandomSeedRowTarget.Black;
        if (model.WhiteVisible && _whiteLink.IsHit(point)) return RandomSeedRowTarget.White;
        return null;
    }

    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePoint, RandomSeedRowModel model)
    {
        drawingContext.DrawVerticalResultSection(new Rectangle(1144, 856, 668, 52),
            "RANDOM SEED", new Color(112, 76, 48), labelWidth: 56);
        if (_providerLink is not null && model.ProviderVisible)
        {
            drawingContext.DrawFittedText("PROVIDER", new Rectangle(1164, 874, 76, 22),
                new Color(180, 195, 195), 0.22f);
            DrawLink(drawingContext, mousePoint, _providerLink, model.ProviderValue, iconBlack: null);
        }
        if (model.BlackVisible) DrawLink(drawingContext, mousePoint, _blackLink, model.BlackValue, iconBlack: true);
        if (model.WhiteVisible) DrawLink(drawingContext, mousePoint, _whiteLink, model.WhiteValue, iconBlack: false);
    }

    public GoStone? GetCgosHit(Point point, bool blackEnabled, bool whiteEnabled) =>
        blackEnabled && _cgosBlackLink.IsHit(point) ? GoStone.Black :
        whiteEnabled && _cgosWhiteLink.IsHit(point) ? GoStone.White : null;

    public void DrawCgos(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        bool blackVisible, string blackValue, bool blackEnabled,
        bool whiteVisible, string whiteValue, bool whiteEnabled)
    {
        if (blackVisible) DrawCgosLink(drawingContext, mousePoint, _cgosBlackLink, blackValue, blackEnabled);
        if (whiteVisible) DrawCgosLink(drawingContext, mousePoint, _cgosWhiteLink, whiteValue, whiteEnabled);
    }

    private static void DrawCgosLink(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        LinkUnderline link, string value, bool enabled)
    {
        drawingContext.DrawFittedText("RANDOM SEED", new Rectangle(link.Bounds.X - 132, link.Bounds.Y + 5, 120, 22),
            enabled ? new Color(180, 195, 195) : new Color(90, 100, 104), 0.23f);
        link.UpdatePointer(enabled ? mousePoint : new Point(-1, -1));
        drawingContext.DrawFittedText(link.GetDisplayText(value),
            new Rectangle(link.Bounds.X + 6, link.Bounds.Y + 2, link.Bounds.Width - 12, 26),
            enabled ? Color.White : new Color(100, 108, 112), 0.29f);
        link.Draw(drawingContext);
    }

    private static LinkUnderline CreateLink(Rectangle bounds)
    {
        var link = new LinkUnderline(new RoundUnderline()) { Bounds = bounds, Placeholder = "AUTO" };
        link.SetActionBadge(ActionBadgeComponent.Create("CHANGE", bounds, 0.24f));
        return link;
    }

    private static void DrawLink(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        LinkUnderline link, string value, bool? iconBlack)
    {
        link.UpdatePointer(mousePoint);
        if (iconBlack is { } black)
            drawingContext.DrawIconStone(new Vector2(link.Bounds.X - (_includeProviderOffset(link) ? 24 : 28), link.Bounds.Center.Y), 12, black);
        drawingContext.DrawFittedText(link.GetDisplayText(value),
            new Rectangle(link.Bounds.X + 6, link.Bounds.Y + 2,
                Math.Max(1, link.Bounds.Width - 12), 26), Color.White, 0.29f);
        link.Draw(drawingContext);
    }

    private static bool _includeProviderOffset(LinkUnderline link) => link.Bounds.Width == 170;
}

public readonly record struct RandomSeedRowModel(
    bool ProviderVisible,
    string ProviderValue,
    bool BlackVisible,
    string BlackValue,
    bool WhiteVisible,
    string WhiteValue);

public enum RandomSeedRowTarget { Provider, Black, White }
