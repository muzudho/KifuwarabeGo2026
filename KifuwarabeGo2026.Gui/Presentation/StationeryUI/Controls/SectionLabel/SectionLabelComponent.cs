namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SectionLabel;

using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;

/// <summary>対象区画とラベル自身の位置、サイズ、表示方向を生成時から所有する文房具UIです。</summary>
public sealed class SectionLabelComponent
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　ファクトリーメソッド］
    public static SectionLabelComponent CreateVertical(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        StationeryDrawingContext drawingContext,
        int labelWidth = 38,
        int labelGap = 8)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var usesSplitHorizontalText = drawingContext.MeasureText(text).X * VerticalScale > targetSectionBounds.Height - 12;
        var actualWidth = usesSplitHorizontalText ? Math.Max(88, labelWidth * 2) : labelWidth;
        var bounds = new Rectangle(
            targetSectionBounds.X - actualWidth - labelGap,
            targetSectionBounds.Y,
            actualWidth,
            targetSectionBounds.Height);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Vertical, usesSplitHorizontalText);
    }

    public static SectionLabelComponent CreateHorizontal(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        int labelHeight = 38,
        int labelGap = 8)
    {
        var bounds = new Rectangle(
            targetSectionBounds.X,
            targetSectionBounds.Y - labelHeight - labelGap,
            targetSectionBounds.Width,
            labelHeight);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Horizontal, usesSplitHorizontalText: false);
    }

    public static SectionLabelComponent CreateVerticalOverlay(
        Rectangle targetSectionBounds,
        string text,
        Color accentColor,
        Color textColor,
        StationeryDrawingContext drawingContext,
        int labelWidth = 38,
        int leftProtrusion = 4)
    {
        ArgumentNullException.ThrowIfNull(drawingContext);
        var usesSplitHorizontalText = drawingContext.MeasureText(text).X * VerticalScale > targetSectionBounds.Height - 12;
        var actualWidth = usesSplitHorizontalText ? Math.Max(88, labelWidth * 2) : labelWidth;
        var bounds = new Rectangle(
            targetSectionBounds.X - leftProtrusion,
            targetSectionBounds.Y,
            actualWidth,
            targetSectionBounds.Height);
        return new SectionLabelComponent(
            targetSectionBounds, bounds, text, accentColor, textColor,
            SectionLabelDirection.Vertical, usesSplitHorizontalText);
    }
    #endregion

    #region ［生成　＞　コンストラクター］
    private SectionLabelComponent(
        Rectangle targetSectionBounds,
        Rectangle bounds,
        string text,
        Color accentColor,
        Color textColor,
        SectionLabelDirection direction,
        bool usesSplitHorizontalText)
    {
        TargetSectionBounds = targetSectionBounds;
        Bounds = bounds;
        Text = text ?? throw new ArgumentNullException(nameof(text));
        AccentColor = accentColor;
        TextColor = textColor;
        Direction = direction;
        _usesSplitHorizontalText = usesSplitHorizontalText;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    private const float VerticalScale = 0.38f;
    private readonly bool _usesSplitHorizontalText;

    public Rectangle TargetSectionBounds { get; }
    public Rectangle Bounds { get; }
    public string Text { get; }
    public Color AccentColor { get; }
    public Color TextColor { get; }
    public SectionLabelDirection Direction { get; }

    // ========================================
    // 機能
    // ========================================

    public void Draw(StationeryDrawingContext draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        DrawSurface(draw);
        if (Direction == SectionLabelDirection.Horizontal)
        {
            draw.DrawFittedText(Text, new Rectangle(Bounds.X + 8, Bounds.Y + 4, Bounds.Width - 16, Bounds.Height - 8), TextColor, 0.36f);
            return;
        }

        if (!_usesSplitHorizontalText)
        {
            var center = new Vector2(Bounds.Center.X, Bounds.Center.Y);
            draw.DrawRotatedCenteredText(Text, center + new Vector2(2, 2), new Color(0, 0, 0, 125), VerticalScale);
            draw.DrawRotatedCenteredText(Text, center, TextColor, VerticalScale);
            return;
        }

        var (firstLine, secondLine) = Split(Text);
        var lineHeight = Math.Max(1, (Bounds.Height - 12) / 2);
        draw.DrawFittedText(firstLine, new Rectangle(Bounds.X + 6, Bounds.Y + 5, Bounds.Width - 12, lineHeight), TextColor, 0.30f);
        draw.DrawFittedText(secondLine, new Rectangle(Bounds.X + 6, Bounds.Y + 7 + lineHeight, Bounds.Width - 12, lineHeight), TextColor, 0.30f);
    }

    private void DrawSurface(StationeryDrawingContext draw)
    {
        draw.FillRectangle(Bounds, new Color(AccentColor, 150));
        draw.DrawRectangle(Bounds, 1, new Color(AccentColor, 230));
    }

    private static (string FirstLine, string SecondLine) Split(string title)
    {
        var splitAt = title.LastIndexOf(' ');
        if (splitAt > 0 && splitAt < title.Length - 1) return (title[..splitAt], title[(splitAt + 1)..]);
        var middle = Math.Max(1, title.Length / 2);
        return (title[..middle], title[middle..]);
    }
}

public enum SectionLabelDirection
{
    Horizontal,
    Vertical,
}
