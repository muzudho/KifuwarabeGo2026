namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;

using Microsoft.Xna.Framework;
using System;
using System.Diagnostics;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.TableRowLabel;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.SectionLabel;

/// <summary>［ポップアップトレンドチャート　＞　着手コメントパネル］</summary>
public sealed class MoveCommentPanelComponent
{
    private const double SectionLabelAnimationSeconds = 0.35;
    private static readonly Color SectionLabelAccentColor = new(5, 10, 18);
    private static readonly Color SectionLabelTextColor = new(170, 184, 188);

    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    internal MoveCommentPanelComponent()
    {
        HeadingLabel = new TableRowLabel(string.Empty, Rectangle.Empty, new Color(255, 215, 92), 0.27f);
        PreviousMoveButton = new Button(Rectangle.Empty, "< PREV", 0.19f);
        NextMoveButton = new Button(Rectangle.Empty, "NEXT >", 0.19f);
        EditButton = new Button(Rectangle.Empty, "EDIT", 0.17f);
        PreviousPageButton = new Button(Rectangle.Empty, "< PAGE", 0.20f);
        NextPageButton = new Button(Rectangle.Empty, "PAGE >", 0.20f);
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    public TableRowLabel HeadingLabel { get; }
    public Rectangle Bounds { get; } = new(1060, 205, 680, 740);
    public SectionLabelComponent? SectionLabel { get; private set; }
    public Button PreviousMoveButton { get; }
    public Button NextMoveButton { get; }
    public Button EditButton { get; }
    public Button PreviousPageButton { get; }
    public Button NextPageButton { get; }
    private bool? _lastPanelVisible;
    private long _sectionLabelAnimationStartedAt;
    private Rectangle _sectionLabelAnimationFrom;

    // ========================================
    // 機能
    // ========================================

    public void DrawSectionLabel(KfwStationeryDrawingTools drawingContext, bool isPanelVisible)
    {
        var targetSectionBounds = isPanelVisible
            ? new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 330)
            : new Rectangle(50, 630, 1, 300);
        var targetLabel = isPanelVisible
            ? SectionLabelComponent.CreateVerticalOverlay(
                targetSectionBounds,
                "MOVE COMMENT PANEL",
                SectionLabelAccentColor,
                SectionLabelTextColor,
                drawingContext,
                labelWidth: 38,
                leftProtrusion: 4)
            : SectionLabelComponent.CreateVertical(
                targetSectionBounds,
                "MOVE COMMENT PANEL",
                SectionLabelAccentColor,
                SectionLabelTextColor,
                drawingContext,
                labelWidth: 38,
                labelGap: 8);

        if (_lastPanelVisible is null)
        {
            _lastPanelVisible = isPanelVisible;
            _sectionLabelAnimationFrom = targetLabel.Bounds;
        }
        else if (_lastPanelVisible != isPanelVisible)
        {
            _lastPanelVisible = isPanelVisible;
            _sectionLabelAnimationFrom = SectionLabel?.Bounds ?? targetLabel.Bounds;
            _sectionLabelAnimationStartedAt = Stopwatch.GetTimestamp();
        }

        var elapsed = _sectionLabelAnimationStartedAt == 0
            ? SectionLabelAnimationSeconds
            : Stopwatch.GetElapsedTime(_sectionLabelAnimationStartedAt).TotalSeconds;
        var amount = Math.Clamp(elapsed / SectionLabelAnimationSeconds, 0.0, 1.0);
        amount = amount * amount * (3.0 - 2.0 * amount);
        var animatedBounds = Lerp(_sectionLabelAnimationFrom, targetLabel.Bounds, amount);
        SectionLabel = SectionLabelComponent.CreateVerticalAt(
            targetSectionBounds,
            animatedBounds,
            "MOVE COMMENT PANEL",
            SectionLabelAccentColor,
            SectionLabelTextColor,
            drawingContext);
        SectionLabel.EnableVisibilityPin(isPanelVisible);
        SectionLabel.Draw(drawingContext);
    }

    public bool IsVisibilityPinHit(Point point) => SectionLabel?.IsVisibilityPinHit(point) ?? false;

    public Rectangle GetBodyBounds(Rectangle bounds)
    {
        var expanded = IsExpanded(bounds);
        var top = bounds.Y + (expanded ? 148 : 102);
        var footerHeight = expanded ? 92 : 56;
        return new Rectangle(bounds.X + 36, top, bounds.Width - 72, Math.Max(1, bounds.Bottom - top - footerHeight));
    }

    public void UpdateLayout(Rectangle bounds)
    {
        var expanded = IsExpanded(bounds);
        HeadingLabel.Bounds = expanded
            ? new(bounds.X + 24, bounds.Y + 82, bounds.Width - 650, 52)
            : new(bounds.X + 20, bounds.Y + 58, bounds.Width - 440, 36);
        HeadingLabel.Scale = expanded ? 0.46f : 0.27f;
        PreviousMoveButton.Bounds = expanded ? new(bounds.Right - 326, bounds.Y + 78, 140, 56) : new(bounds.Right - 206, bounds.Y + 58, 92, 36);
        NextMoveButton.Bounds = expanded ? new(bounds.Right - 174, bounds.Y + 78, 140, 56) : new(bounds.Right - 104, bounds.Y + 58, 92, 36);
        EditButton.Bounds = expanded ? new(bounds.Right - 478, bounds.Y + 78, 140, 56) : new(bounds.Right - 308, bounds.Y + 58, 92, 36);
        PreviousPageButton.Bounds = expanded ? new(bounds.Right - 330, bounds.Bottom - 76, 140, 56) : new(bounds.Right - 170, bounds.Bottom - 46, 70, 36);
        NextPageButton.Bounds = expanded ? new(bounds.Right - 174, bounds.Bottom - 76, 140, 56) : new(bounds.Right - 92, bounds.Bottom - 46, 70, 36);
        PreviousMoveButton.LabelScale = NextMoveButton.LabelScale = expanded ? 0.28f : 0.19f;
        EditButton.LabelScale = expanded ? 0.25f : 0.17f;
        PreviousPageButton.LabelScale = NextPageButton.LabelScale = expanded ? 0.30f : 0.20f;
    }

    public int? GetPageStepButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return PreviousPageButton.IsHit(point) ? -1 : NextPageButton.IsHit(point) ? 1 : null;
    }

    public int? GetMoveStepButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return PreviousMoveButton.IsHit(point) ? -1 : NextMoveButton.IsHit(point) ? 1 : null;
    }

    public bool IsEditButtonHit(Point point, Rectangle bounds)
    {
        UpdateLayout(bounds);
        return EditButton.IsHit(point);
    }

    private static bool IsExpanded(Rectangle bounds) => bounds.Width > 1000 || bounds.Height > 600;

    private static Rectangle Lerp(Rectangle from, Rectangle to, double amount) => new(
        (int)Math.Round(from.X + (to.X - from.X) * amount),
        (int)Math.Round(from.Y + (to.Y - from.Y) * amount),
        (int)Math.Round(from.Width + (to.Width - from.Width) * amount),
        (int)Math.Round(from.Height + (to.Height - from.Height) * amount));
}
