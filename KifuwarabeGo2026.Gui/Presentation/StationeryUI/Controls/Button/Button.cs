namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;

using Microsoft.Xna.Framework;
using System;

/// <summary>位置、ラベル、有効状態、ヒット判定を所有する文房具 UI のボタンです。</summary>
public sealed class Button
{
    // ========================================
    // 生成
    // ========================================

    #region ［生成　＞　コンストラクター］
    public Button(Rectangle bounds, string label, float labelScale)
    {
        Bounds = bounds;
        Label = label ?? throw new ArgumentNullException(nameof(label));
        LabelScale = labelScale;
    }
    #endregion

    // ========================================
    // データメンバー
    // ========================================

    public Rectangle Bounds { get; set; }
    public string Label { get; set; }
    public float LabelScale { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsPointerOver { get; private set; }
    public bool IsSelected { get; set; }
    public Color FillColor { get; set; } = new Color(36, 48, 58);
    public Color PointerOverFillColor { get; set; } = new Color(58, 82, 94);

    public bool IsHit(Point point) => IsEnabled && Bounds.Contains(point);

    public void UpdatePointer(Point point) => IsPointerOver = IsHit(point);

    // ========================================
    // 機能
    // ========================================

    public void Draw(Point mousePoint, IButtonDrawingSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        UpdatePointer(mousePoint);
        var fill = !IsEnabled ? new Color(24, 27, 31) : IsSelected ? new Color(31, 151, 112) : IsPointerOver ? PointerOverFillColor : FillColor;
        var border = !IsEnabled ? new Color(43, 50, 56) : IsSelected ? new Color(151, 255, 215) : IsPointerOver ? new Color(178, 219, 226) : new Color(126, 150, 164);
        surface.FillRectangle(new Rectangle(Bounds.X + 4, Bounds.Y + 5, Bounds.Width, Bounds.Height), new Color(0, 0, 0, IsEnabled ? 95 : 28));
        surface.FillRectangle(Bounds, fill);
        surface.DrawRectangle(Bounds, 2, border);
        if (IsEnabled)
            surface.DrawRectangle(new Rectangle(Bounds.X + 2, Bounds.Y + 2, Bounds.Width - 4, Bounds.Height - 4), 1,
                IsSelected ? new Color(215, 255, 238, 95) : new Color(255, 255, 255, IsPointerOver ? 70 : 36));

        surface.DrawFittedText(Label, new Rectangle(Bounds.X + 10, Bounds.Y + 5, Bounds.Width - 20, Bounds.Height - 10),
            IsEnabled ? Color.White : new Color(91, 100, 106), LabelScale);
    }
}

public interface IButtonDrawingSurface
{
    void FillRectangle(Rectangle bounds, Color color);
    void DrawRectangle(Rectangle bounds, int thickness, Color color);
    void DrawFittedText(string text, Rectangle bounds, Color color, float scale);
}
