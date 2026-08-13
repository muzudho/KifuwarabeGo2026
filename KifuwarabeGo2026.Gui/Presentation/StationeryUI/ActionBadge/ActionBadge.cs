namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.ActionBadge;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// アンダーライン項目にホバーしたとき、右端へ表示するアクション名のバッジです。
/// </summary>
public sealed class ActionBadge
{
    public string Label { get; private set; } = string.Empty;

    public Rectangle Bounds { get; private set; }

    public bool IsVisible { get; private set; }

    /// <summary>バッジ内ラベルの文字倍率です。</summary>
    public float TextScale { get; set; } = 0.34f;

    /// <summary>項目の右端に合わせた標準位置でバッジを表示します。</summary>
    public void Show(string label, Rectangle anchorBounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(label);
        Label = label;
        Bounds = GetBounds(label, anchorBounds);
        IsVisible = true;
    }

    public void Hide() => IsVisible = false;

    public void Draw(ActionBadgeDrawingCallbacks callbacks)
    {
        ArgumentNullException.ThrowIfNull(callbacks);
        if (!IsVisible) return;

        callbacks.DrawRoundedFill(Bounds, 6, new Color(185, 196, 255));
        callbacks.DrawCenteredText(Label, Bounds, new Color(15, 20, 31), TextScale);
    }

    /// <summary>標準バッジの大きさと、アンダーライン右端に対する配置を返します。</summary>
    public static Rectangle GetBounds(string label, Rectangle anchorBounds)
    {
        var width = label switch
        {
            "EDIT" => 70,
            "SELECT" or "TOGGLE" => 88,
            _ => 100,
        };
        var height = label == "EDIT" ? 23 : 26;
        var rightMargin = label == "EDIT" ? 6 : 8;
        var bottomMargin = label == "EDIT" ? 2 : 2;
        return new Rectangle(anchorBounds.Right - width - rightMargin, anchorBounds.Bottom - height - bottomMargin, width, height);
    }
}

public sealed record ActionBadgeDrawingCallbacks(
    Action<Rectangle, int, Color> DrawRoundedFill,
    Action<string, Rectangle, Color, float> DrawCenteredText);
