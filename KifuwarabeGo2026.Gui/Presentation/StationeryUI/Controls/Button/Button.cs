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

    public bool IsHit(Point point) => IsEnabled && Bounds.Contains(point);

    // ========================================
    // 機能
    // ========================================

    public void Draw(Point mousePoint, Action<Rectangle, string, bool, Point, bool, float> drawButton) =>
        (drawButton ?? throw new ArgumentNullException(nameof(drawButton)))(Bounds, Label, false, mousePoint, IsEnabled, LabelScale);
}
