namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// タイトル画面の背景の描画
/// </summary>
public sealed partial class GoScreenRenderer
{
    /// <summary>
    /// 背景の描画
    /// </summary>
    private void DrawTitleGoEquipment()
    {
        // 左の碁笥の蓋の描画
        DrawLeftGoBowlLid();

        // 左の石１の描画
        DrawLeftStone1();

        // 左の石２の描画
        DrawLeftStone2();

        // 左の石３の描画
        DrawLeftStone3();

        // 左の碁笥の本体の描画
        DrawLeftGoBowlBody();

        // 右の碁笥の蓋の描画
        DrawRightGoBowlLid();

        // 右の石１の描画
        DrawRightStone1();

        // 右の石２の描画
        DrawRightStone2();

        // 右の石３の描画
        DrawRightStone3();

        // 右の碁笥の本体の描画
        DrawRightGoBowlBody();
    }

    /// <summary>
    /// 左の碁笥の蓋の描画
    /// </summary>
    private void DrawLeftGoBowlLid()
    {
        var bowlWire = new Color(55, 145, 141, 92);
        var bowlAccent = new Color(85, 194, 179, 115);
        var lidCenter = new Vector2(187, 329);
        DrawEllipseWire(lidCenter, 268, 98, bowlAccent, 4, 0.035f);                    // outer contour
        DrawEllipseWire(lidCenter + new Vector2(0, 7), 246, 68, bowlWire, 1, 0.035f); // slope
        DrawEllipseWire(lidCenter + new Vector2(0, -8), 199, 41, bowlWire, 1, 0.035f); // inner bottom
    }

    /// <summary>左の石１の描画</summary>
    private void DrawLeftStone1()
    {
        var color = new Color(93, 186, 179, 125);
        DrawEllipseWire(new Vector2(87, 490), 70, 32, color, 4, -0.10f);
        DrawEllipseWire(new Vector2(82.03f, 490.50f), 36.4f, 16, color * 0.78f, 1, -0.10f);
    }

    /// <summary>左の石２の描画</summary>
    private void DrawLeftStone2()
    {
        var color = new Color(93, 186, 179, 125);
        DrawEllipseWire(new Vector2(213, 560), 64, 28, color, 4, 1.18f);
        DrawEllipseWire(new Vector2(211.10f, 555.38f), 33.28f, 14, color * 0.78f, 1, 1.18f);
    }

    /// <summary>左の石３の描画</summary>
    private void DrawLeftStone3()
    {
        var color = new Color(93, 186, 179, 125);
        DrawEllipseWire(new Vector2(181, 720), 68, 30, color, 4, -0.68f);
        DrawEllipseWire(new Vector2(177.11f, 723.14f), 35.36f, 15, color * 0.78f, 1, -0.68f);
    }

    /// <summary>左の碁笥の本体の描画</summary>
    private void DrawLeftGoBowlBody()
    {
        var center = new Vector2(187, 904);
        var bowlWire = new Color(55, 145, 141, 92);
        var bowlAccent = new Color(85, 194, 179, 115);
        DrawEllipseWire(center + new Vector2(0, -69), 300, 76, bowlAccent, 4, 0.035f); // mouth contour
        DrawEllipseWire(center + new Vector2(0, -61), 260, 54, bowlWire, 1, 0.035f);   // mouth slope
        DrawEllipseWire(center + new Vector2(0, 21), 300, 70, bowlWire, 1, 0.035f);    // lower latitude
        DrawEllipseWire(center + new Vector2(0, 86), 175, 34, bowlWire, 1, 0.035f);    // foot
        DrawEllipseArc(center + new Vector2(0, -69), 332, 324, bowlAccent, 4, 0.035f, 0f, MathF.PI);
    }

    /// <summary>右の碁笥の蓋の描画</summary>
    private void DrawRightGoBowlLid()
    {
        var bowlWire = new Color(190, 126, 65, 92);
        var bowlAccent = new Color(224, 157, 76, 115);
        var lidCenter = new Vector2(1733, 329);
        DrawEllipseWire(lidCenter, 268, 98, bowlAccent, 4, 0.035f);
        DrawEllipseWire(lidCenter + new Vector2(0, 7), 246, 68, bowlWire, 1, 0.035f);
        DrawEllipseWire(lidCenter + new Vector2(0, -8), 199, 41, bowlWire, 1, 0.035f);
    }

    /// <summary>右の石１の描画</summary>
    private void DrawRightStone1()
    {
        var color = new Color(229, 183, 112, 130);
        DrawEllipseWire(new Vector2(1833, 490), 70, 32, color, 4, 0.10f);
        DrawEllipseWire(new Vector2(1828.03f, 489.50f), 36.4f, 16, color * 0.78f, 1, 0.10f);
    }

    /// <summary>右の石２の描画</summary>
    private void DrawRightStone2()
    {
        var color = new Color(229, 183, 112, 130);
        DrawEllipseWire(new Vector2(1707, 560), 64, 28, color, 4, -1.18f);
        DrawEllipseWire(new Vector2(1705.10f, 564.62f), 33.28f, 14, color * 0.78f, 1, -1.18f);
    }

    /// <summary>右の石３の描画</summary>
    private void DrawRightStone3()
    {
        var color = new Color(229, 183, 112, 130);
        DrawEllipseWire(new Vector2(1739, 720), 68, 30, color, 4, 0.68f);
        DrawEllipseWire(new Vector2(1735.11f, 716.86f), 35.36f, 15, color * 0.78f, 1, 0.68f);
    }

    /// <summary>右の碁笥の本体の描画</summary>
    private void DrawRightGoBowlBody()
    {
        var center = new Vector2(1733, 904);
        var bowlWire = new Color(190, 126, 65, 92);
        var bowlAccent = new Color(224, 157, 76, 115);
        DrawEllipseWire(center + new Vector2(0, -69), 300, 76, bowlAccent, 4, 0.035f);
        DrawEllipseWire(center + new Vector2(0, -61), 260, 54, bowlWire, 1, 0.035f);
        DrawEllipseWire(center + new Vector2(0, 21), 300, 70, bowlWire, 1, 0.035f);
        DrawEllipseWire(center + new Vector2(0, 86), 175, 34, bowlWire, 1, 0.035f);
        DrawEllipseArc(center + new Vector2(0, -69), 332, 324, bowlAccent, 4, 0.035f, 0f, MathF.PI);
    }

    /// <summary>
    /// 楕円
    /// </summary>
    /// <param name="center"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    /// <param name="rotation"></param>
    private void DrawEllipseWire(Vector2 center, float width, float height, Color color, int thickness, float rotation)
        => DrawEllipseArc(center, width, height, color, thickness, rotation, 0f, MathHelper.TwoPi);

    /// <summary>
    /// 弧の描画
    /// </summary>
    /// <param name="center"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    /// <param name="rotation"></param>
    /// <param name="startAngle"></param>
    /// <param name="endAngle"></param>
    private void DrawEllipseArc(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation,
        float startAngle,
        float endAngle)
    {
        const int segments = 48;
        var cosRotation = MathF.Cos(rotation);
        var sinRotation = MathF.Sin(rotation);
        Vector2 Transform(float angle)
        {
            var x = MathF.Cos(angle) * width * 0.5f;
            var y = MathF.Sin(angle) * height * 0.5f;
            return center + new Vector2(x * cosRotation - y * sinRotation, x * sinRotation + y * cosRotation);
        }

        var previous = Transform(startAngle);
        for (var i = 1; i <= segments; i++)
        {
            var next = Transform(MathHelper.Lerp(startAngle, endAngle, i / (float)segments));
            DrawLine(previous, next, thickness, color);
            previous = next;
        }
    }
}
