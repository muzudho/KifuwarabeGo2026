namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;
using System;

/// <summary>
/// タイトル画面の背景の描画
/// </summary>
public sealed partial class GoScreenRenderer
{
    // SpriteBatchのAlphaBlendはプリマルチプライドAlphaを前提とするため、
    // RGBもAlphaに合わせて変換してから渡す。
    private static readonly Color TitleGoEquipmentThinColor = Color.FromNonPremultiplied(112, 231, 235, 140);
    private static readonly Color TitleGoEquipmentOutlineColor = Color.FromNonPremultiplied(112, 231, 235, 170);

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

        //// 右の碁笥の蓋の描画
        //DrawRightGoBowlLid();

        //// 右の石１の描画
        //DrawRightStone1();

        //// 右の石２の描画
        //DrawRightStone2();

        //// 右の石３の描画
        //DrawRightStone3();

        //// 右の碁笥の本体の描画
        //DrawRightGoBowlBody();
    }

    /// <summary>
    /// 左の碁笥の蓋の描画
    /// </summary>
    private void DrawLeftGoBowlLid()
    {
        var lidCenter = new Vector2(187, 329);
        DrawEllipseWire(lidCenter, 268, 98, TitleGoEquipmentOutlineColor, 4, 0f);                    // 外側の輪郭
        DrawEllipseWire(lidCenter + new Vector2(0, 7), 246, 68, TitleGoEquipmentThinColor, 1, 0f);  // 内側の傾斜
        DrawEllipseWire(lidCenter + new Vector2(0, -8), 199, 41, TitleGoEquipmentThinColor, 1, 0f); // 蓋の底
    }

    /// <summary>左の石１の描画</summary>
    private void DrawLeftStone1()
    {
        DrawEllipseWire(new Vector2(87, 490), 70, 32, TitleGoEquipmentOutlineColor, 4, -0.10f);
        DrawEllipseWire(new Vector2(82.03f, 490.50f), 36.4f, 16, TitleGoEquipmentThinColor, 1, -0.10f);
    }

    /// <summary>左の石２の描画</summary>
    private void DrawLeftStone2()
    {
        DrawEllipseWire(new Vector2(213, 560), 64, 64, TitleGoEquipmentOutlineColor, 4, 0f);
        DrawEllipseWire(new Vector2(205, 554), 26, 26, TitleGoEquipmentThinColor, 1, 0f);
    }

    /// <summary>左の石３の描画</summary>
    private void DrawLeftStone3()
    {
        DrawEllipseWire(new Vector2(181, 720), 68, 30, TitleGoEquipmentOutlineColor, 4, -0.68f);
        DrawEllipseWire(new Vector2(177.11f, 723.14f), 35.36f, 15, TitleGoEquipmentThinColor, 1, -0.68f);
    }

    /// <summary>左の碁笥の本体の描画</summary>
    private void DrawLeftGoBowlBody()
    {
        var center = new Vector2(187, 904);
        var mouthCenter = center + new Vector2(0, -69);

        // 見本の黄色い線：円の上側を描かず、口の少し下から画面下へ深く膨らむ弧だけを描く。
        //DrawWireRectangle(
        //    center: mouthCenter + new Vector2(0, 90),
        //    width: 300,
        //    height: 175,
        //    color: TitleGoEquipmentOutlineColor,
        //    thickness: 4,
        //    rotation: 0f);
        //DrawCircumscribedCircleWire(
        //    center: mouthCenter + new Vector2(0, 90 - 30),
        //    width: 300,
        //    height: 0, // 175,
        //    color: TitleGoEquipmentOutlineColor,
        //    thickness: 4,
        //    rotation: 0f);
        DrawCircumscribedCircleArc(
            center: mouthCenter + new Vector2(0, 90 - 30),
            width: 300,
            height: 0, // 175,
            color: TitleGoEquipmentOutlineColor,
            thickness: 4,
            rotation: 0f,
            startAngle: (11f + 0.1f) * MathF.PI / 6f, // 2時
            endAngle: (7f - 0.1f) * MathF.PI / 6f);  // 10時

        DrawEllipseWire(mouthCenter, 300 - 25, 76, TitleGoEquipmentOutlineColor, 4, 0f);                // 口の輪郭

        DrawEllipseWire(center + new Vector2(0, 21), 292, 70, TitleGoEquipmentThinColor, 1, 0f);     // 下側の膨らみ

        DrawEllipseWire(center + new Vector2(0, 86 + 20), 175, 34, TitleGoEquipmentThinColor, 1, 0f);     // 高台
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
        DrawInscribedEllipseArc(center + new Vector2(0, -69), 332, 324, bowlAccent, 4, 0.035f, 0f, MathF.PI);
    }

    /// <summary>
    /// （1）中心、幅、高さ、回転角で定義した矩形の輪郭を描きます。
    /// </summary>
    private void DrawWireRectangle(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation)
    {
        var halfWidth = width * 0.5f;
        var halfHeight = height * 0.5f;
        var cosRotation = MathF.Cos(rotation);
        var sinRotation = MathF.Sin(rotation);
        Vector2 Transform(float x, float y) => center + new Vector2(
            x * cosRotation - y * sinRotation,
            x * sinRotation + y * cosRotation);

        var topLeft = Transform(-halfWidth, -halfHeight);
        var topRight = Transform(halfWidth, -halfHeight);
        var bottomRight = Transform(halfWidth, halfHeight);
        var bottomLeft = Transform(-halfWidth, halfHeight);
        DrawLine(topLeft, topRight, thickness, color);
        DrawLine(topRight, bottomRight, thickness, color);
        DrawLine(bottomRight, bottomLeft, thickness, color);
        DrawLine(bottomLeft, topLeft, thickness, color);
    }

    /// <summary>
    /// （2）中心、幅、高さ、回転角が同じ矩形の内接楕円を描きます。
    /// </summary>
    private void DrawInscribedEllipseWire(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation)
        => DrawInscribedEllipseArc(center, width, height, color, thickness, rotation, 0f, MathHelper.TwoPi);

    /// <summary>
    /// （4）中心、幅、高さ、回転角で定義した矩形の四隅を通る外接円を描きます。
    /// 円の直径は矩形の対角線の長さです。
    /// </summary>
    private void DrawCircumscribedCircleWire(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation)
        => DrawCircumscribedCircleArc(
            center,
            width,
            height,
            color,
            thickness,
            rotation,
            0f,
            MathHelper.TwoPi);

    /// <summary>
    /// （5）矩形の外接円のうち、StartからEndまでの角度だけを表示します。
    /// 角度範囲外の線分は描画しません。
    /// </summary>
    private void DrawCircumscribedCircleArc(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation,
        float startAngle,
        float endAngle)
    {
        var diameter = MathF.Sqrt(width * width + height * height);
        DrawInscribedEllipseArc(
            center,
            diameter,
            diameter,
            color,
            thickness,
            rotation,
            startAngle,
            endAngle);
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
        => DrawInscribedEllipseWire(center, width, height, color, thickness, rotation);

    /// <summary>
    /// （3）矩形の内接楕円のうち、StartからEndまでの角度だけを表示します。
    /// 角度範囲外の線分は描画しません。
    /// </summary>
    /// <param name="center"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="color"></param>
    /// <param name="thickness"></param>
    /// <param name="rotation"></param>
    /// <param name="startAngle"></param>
    /// <param name="endAngle"></param>
    private void DrawInscribedEllipseArc(
        Vector2 center,
        float width,
        float height,
        Color color,
        int thickness,
        float rotation,
        float startAngle,
        float endAngle)
    {
        const int segments = 128;
        var cosRotation = MathF.Cos(rotation);
        var sinRotation = MathF.Sin(rotation);
        Vector2 Transform(float angle)
        {
            var x = MathF.Cos(angle) * width * 0.5f;
            var y = MathF.Sin(angle) * height * 0.5f;
            return center + new Vector2(x * cosRotation - y * sinRotation, x * sinRotation + y * cosRotation);
        }

        // MonoGameのSpriteBatchには楕円弧プリミティブがないため、まず楕円を
        // 一周分の線分へ分割し、指定角度の範囲に含まれる線分だけを表示する。
        var drawWholeEllipse = MathF.Abs(endAngle - startAngle) >= MathHelper.TwoPi - 0.0001f;
        var normalizedStart = NormalizeEllipseAngle(startAngle);
        var normalizedEnd = NormalizeEllipseAngle(endAngle);
        for (var i = 0; i < segments; i++)
        {
            var segmentStart = MathHelper.TwoPi * i / segments;
            var segmentEnd = MathHelper.TwoPi * (i + 1) / segments;
            var segmentMiddle = (segmentStart + segmentEnd) * 0.5f;
            if (!drawWholeEllipse && !IsEllipseAngleVisible(segmentMiddle, normalizedStart, normalizedEnd))
                continue;

            DrawLine(Transform(segmentStart), Transform(segmentEnd), thickness, color);
        }
    }

    private static float NormalizeEllipseAngle(float angle)
    {
        angle %= MathHelper.TwoPi;
        return angle < 0f ? angle + MathHelper.TwoPi : angle;
    }

    private static bool IsEllipseAngleVisible(float angle, float startAngle, float endAngle)
    {
        angle = NormalizeEllipseAngle(angle);
        return startAngle <= endAngle
            ? angle >= startAngle && angle <= endAngle
            : angle >= startAngle || angle <= endAngle;
    }
}
