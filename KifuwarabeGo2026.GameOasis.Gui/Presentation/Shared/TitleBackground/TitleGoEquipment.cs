namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.TitleBackground;

using Microsoft.Xna.Framework;
using System;

/// <summary>タイトル画面左側の碁笥と石の背景意匠を所有します。</summary>
public sealed class TitleGoEquipment
{
    private static readonly Color ThinColor = Color.FromNonPremultiplied(112, 231, 235, 140);
    private static readonly Color OutlineColor = Color.FromNonPremultiplied(112, 231, 235, 170);

    public void Draw(TitleGoEquipmentDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(draw);
        var lid = new Vector2(187, 329);
        draw.DrawEllipse(lid, 268, 98, OutlineColor, 4, 0f);
        draw.DrawEllipse(lid + new Vector2(0, 7), 246, 68, ThinColor, 1, 0f);
        draw.DrawEllipse(lid + new Vector2(0, -8), 199, 41, ThinColor, 1, 0f);
        DrawStone(new Vector2(87, 490), 70, 32, new Vector2(82.03f, 490.50f), 36.4f, 16, -0.10f, draw);
        DrawStone(new Vector2(213, 560), 64, 64, new Vector2(205, 554), 26, 26, 0f, draw);
        DrawStone(new Vector2(181, 720), 68, 30, new Vector2(177.11f, 723.14f), 35.36f, 15, -0.68f, draw);
        var center = new Vector2(187, 904);
        var mouth = center + new Vector2(0, -69);
        draw.DrawCircumscribedCircleArc(mouth + new Vector2(0, 60), 300, 0, OutlineColor, 4, 0f,
            (11f + 0.1f) * MathF.PI / 6f, (7f - 0.1f) * MathF.PI / 6f);
        draw.DrawEllipse(mouth, 275, 76, OutlineColor, 4, 0f);
        draw.DrawEllipse(center + new Vector2(0, 21), 292, 70, ThinColor, 1, 0f);
        draw.DrawEllipse(center + new Vector2(0, 106), 175, 34, ThinColor, 1, 0f);
    }

    private static void DrawStone(Vector2 outerCenter, float outerWidth, float outerHeight, Vector2 innerCenter,
        float innerWidth, float innerHeight, float rotation, TitleGoEquipmentDrawingCallbacks draw)
    {
        draw.DrawEllipse(outerCenter, outerWidth, outerHeight, OutlineColor, 4, rotation);
        draw.DrawEllipse(innerCenter, innerWidth, innerHeight, ThinColor, 1, rotation);
    }
}

public sealed record TitleGoEquipmentDrawingCallbacks(
    Action<Vector2, float, float, Color, int, float> DrawEllipse,
    Action<Vector2, float, float, Color, int, float, float, float> DrawCircumscribedCircleArc);
