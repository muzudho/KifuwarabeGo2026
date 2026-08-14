namespace KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>ポン抜きゲームで使用するアプリプロバイダーを選択する画面です。</summary>
public sealed class PonnukiProviderSelectionScreen
{
    public static PonnukiProviderSelectionScreen Default { get; } = new();

    private PonnukiProviderSelectionScreen()
    {
        Headline = new Headline("ポン抜きゲーム", new Vector2(500, 350), Color.White, 0.62f);
        ProviderLabel = new Headline("APP PROVIDER ENGINE", new Vector2(530, 416), new Color(255, 190, 92), 0.42f);
        BackButton = new Button(new Rectangle(1260, 316, 152, 54), "BACK", 0.36f);
        RecheckButton = new Button(new Rectangle(828, 826, 340, 54), "RECHECK PROVIDER", 0.30f);
        StartButton = new Button(new Rectangle(1198, 826, 152, 54), "NEXT", 0.40f);
        ProviderLinkUnderline = new LinkUnderline(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 })
        {
            Bounds = ProviderTextBounds,
        };
    }

    public Headline Headline { get; }

    #region ［Provider］
    public Headline ProviderLabel { get; }
    public Rectangle ProviderDisplayBounds { get; } = new(570, 466, 780, 56);
    public Rectangle ProviderTextBounds { get; } = new(712, 473, 638, 42);
    private LinkUnderline ProviderLinkUnderline { get; }
    public bool IsProviderLinkHit(Point point) => ProviderLinkUnderline.IsHit(point);
    #endregion

    public Rectangle CapabilityStatusBounds { get; } = new(570, 794, 780, 26);
    public Button BackButton { get; }
    public Button RecheckButton { get; }
    public Button StartButton { get; }

    /// <summary>ポン抜きプロバイダー選択画面を描画します。</summary>
    public void Draw(GoAppSession session, Point mousePoint, int activeTabIndex, bool isProviderLoading,
        PonnukiProviderSelectionDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(draw);

        Headline.Draw(draw.HeadlineSurface);
        ProviderLabel.Draw(draw.HeadlineSurface);
        draw.DrawDynamicText("アプリ提供エンジン", new Rectangle(950, 414, 330, 34), new Color(210, 214, 207), 0.32f);

        var textBounds = ProviderTextBounds;
        var hovered = !isProviderLoading && textBounds.Contains(mousePoint);
        draw.DrawText("PROVIDER", new Vector2(ProviderDisplayBounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        draw.DrawDynamicText(session.SelectedAppProviderEngineDisplayName, textBounds, Color.White, 0.34f);
        ProviderLinkUnderline.Bounds = textBounds;
        ProviderLinkUnderline.SetActionBadge(ActionBadge.Create("CHANGE", textBounds));
        ProviderLinkUnderline.UpdatePointer(mousePoint);
        ProviderLinkUnderline.Draw(draw.UnderlineSurface,
            new ActionBadgeDrawingCallbacks(draw.DrawRoundedFill, draw.DrawSharpCenteredFittedText));
        if (hovered)
        {
            draw.DrawStickyNote(
                StickyNoteKind.AppProviderEngineHint,
                new Vector2(textBounds.Right, textBounds.Center.Y),
                new Color(185, 196, 255),
                new Color(116, 145, 178),
                "APP PROVIDER ENGINE とは？",
                ["このＧＵＩの代わりにアプリ実行を", "担当してくれるエンジンです。"]);
        }
        if (isProviderLoading)
        {
            draw.DrawFittedText("LOADING PROVIDERS", textBounds, new Color(255, 210, 128), 0.30f);
            DrawLoadingSpinner(new Vector2(textBounds.Right - 22, textBounds.Center.Y), draw.DrawLine);
        }

        var capabilityColor = session.IsAppProviderCapabilityConfirmed
            ? new Color(99, 223, 185)
            : session.IsAppProviderCapabilityCheckRunning
                ? new Color(255, 210, 128)
                : session.AppProviderCapabilityStatus == "NOT CHECKED" ? new Color(180, 195, 195) : new Color(255, 145, 151);
        draw.DrawFittedText(session.AppProviderCapabilityStatus, CapabilityStatusBounds, capabilityColor, 0.30f);

        RecheckButton.IsEnabled = session.CanUseSelectedAppProvider && !session.IsAppProviderCapabilityCheckRunning;
        RecheckButton.IsSelected = activeTabIndex == 1;
        RecheckButton.Draw(mousePoint, draw.ButtonSurface);
        StartButton.Label = session.CanStartSelectedAppProvider ? "NEXT" : session.CanUseSelectedAppProvider ? "CHECK REQUIRED" : "ENGINE REQUIRED";
        StartButton.LabelScale = session.CanStartSelectedAppProvider ? 0.40f : 0.23f;
        StartButton.IsEnabled = session.CanStartSelectedAppProvider;
        StartButton.IsSelected = activeTabIndex == 2;
        StartButton.Draw(mousePoint, draw.ButtonSurface);
        BackButton.IsSelected = activeTabIndex == 3;
        BackButton.Draw(mousePoint, draw.ButtonSurface);

        DrawTabHints(session, activeTabIndex, isProviderLoading, draw.DrawTabNavigationHint);
    }

    private static void DrawLoadingSpinner(Vector2 center, Action<Vector2, Vector2, float, Color> drawLine)
    {
        const int segmentCount = 12;
        var head = (int)(Environment.TickCount64 / 70 % segmentCount);
        for (var index = 0; index < segmentCount; index++)
        {
            var angle = MathF.Tau * index / segmentCount;
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            var distance = (head - index + segmentCount) % segmentCount;
            var alpha = (byte)Math.Clamp(235 - distance * 15, 70, 235);
            drawLine(center + direction * 11, center + direction * 22, 4, new Color(147, 244, 200, (int)alpha));
        }
    }

    private void DrawTabHints(GoAppSession session, int activeTabIndex, bool isProviderLoading,
        Action<Rectangle, int, int, int> drawTabNavigationHint)
    {
        var stops = new[]
        {
            (Index: 0, Bounds: ProviderDisplayBounds, Enabled: !isProviderLoading),
            (Index: 1, Bounds: RecheckButton.Bounds, Enabled: session.CanUseSelectedAppProvider && !session.IsAppProviderCapabilityCheckRunning),
            (Index: 2, Bounds: StartButton.Bounds, Enabled: session.CanStartSelectedAppProvider),
            (Index: 3, Bounds: BackButton.Bounds, Enabled: true),
        }.Where(stop => stop.Enabled).ToArray();
        var activeStopIndex = Array.FindIndex(stops, stop => stop.Index == activeTabIndex);
        for (var index = 0; index < stops.Length; index++)
            drawTabNavigationHint(stops[index].Bounds, index, activeStopIndex, stops.Length);
    }
}

/// <summary>ポン抜きプロバイダー選択画面が必要とする描画機能です。</summary>
public sealed record PonnukiProviderSelectionDrawingCallbacks(
    StationeryDrawingContext HeadlineSurface,
    StationeryDrawingContext ButtonSurface,
    StationeryDrawingContext UnderlineSurface,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<Rectangle, int, Color> DrawRoundedFill,
    Action<string, Rectangle, Color, float> DrawSharpCenteredFittedText,
    Action<StickyNoteKind, Vector2, Color, Color, string, IReadOnlyList<string>> DrawStickyNote,
    Action<Rectangle, int, int, int> DrawTabNavigationHint);
