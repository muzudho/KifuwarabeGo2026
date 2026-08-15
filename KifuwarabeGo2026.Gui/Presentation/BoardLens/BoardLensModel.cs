namespace KifuwarabeGo2026.Gui.Presentation.BoardLens;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

/// <summary>Board Lens の座標、配色、および描画操作をまとめた描画モデルです。</summary>
public sealed class BoardLensModel
{
    private readonly Func<Vector2, float, int, int, Vector2> _getBoardPoint;
    private readonly Func<GoStone, Color> _getCellColor;
    private readonly Action<Vector2, Vector2, float, Color> _drawLine;
    private readonly Action<Vector2, float, Color> _drawCircle;
    private readonly Action<Rectangle, Color> _fillRectangle;
    private readonly Action<Rectangle, int, Color> _drawRectangle;
    private readonly Action<Vector2, float, float, Color, int, float> _drawEllipseWire;
    private readonly Action<int, Vector2, float> _drawRenNumber;
    private readonly Action<GoRen, int, Color, Vector2, float, Color?> _drawMetric;
    private readonly Action<GoRenParseResult, IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)>, Vector2, float> _drawDeferredMetrics;

    internal BoardLensModel(
        Func<Vector2, float, int, int, Vector2> getBoardPoint,
        Func<GoStone, Color> getCellColor,
        Action<Vector2, Vector2, float, Color> drawLine,
        Action<Vector2, float, Color> drawCircle,
        Action<Rectangle, Color> fillRectangle,
        Action<Rectangle, int, Color> drawRectangle,
        Action<Vector2, float, float, Color, int, float> drawEllipseWire,
        Action<int, Vector2, float> drawRenNumber,
        Action<GoRen, int, Color, Vector2, float, Color?> drawMetric,
        Action<GoRenParseResult, IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)>, Vector2, float> drawDeferredMetrics)
    {
        _getBoardPoint = getBoardPoint;
        _getCellColor = getCellColor;
        _drawLine = drawLine;
        _drawCircle = drawCircle;
        _fillRectangle = fillRectangle;
        _drawRectangle = drawRectangle;
        _drawEllipseWire = drawEllipseWire;
        _drawRenNumber = drawRenNumber;
        _drawMetric = drawMetric;
        _drawDeferredMetrics = drawDeferredMetrics;
    }

    public Vector2 GetBoardPoint(Vector2 start, float cell, int x, int y) => _getBoardPoint(start, cell, x, y);
    public Color GetRenGraphCellColor(GoStone stone) => _getCellColor(stone);
    public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawLine(start, end, thickness, color);
    public void DrawCircle(Vector2 center, float radius, Color color) => _drawCircle(center, radius, color);
    public void FillRectangle(Rectangle bounds, Color color) => _fillRectangle(bounds, color);
    public void DrawRectangle(Rectangle bounds, int thickness, Color color) => _drawRectangle(bounds, thickness, color);
    public void DrawEllipseWire(Vector2 center, float radiusX, float radiusY, Color color, int thickness, float rotation) =>
        _drawEllipseWire(center, radiusX, radiusY, color, thickness, rotation);
    public void DrawRenNumber(int number, Vector2 center, float scale) => _drawRenNumber(number, center, scale);
    public void DrawRenBoundaryPointMetric(GoRen ren, int value, Color color, Vector2 start, float cell, Color? outline) =>
        _drawMetric(ren, value, color, start, cell, outline);
    public void DrawDeferredStrongBoundaryMetrics(GoRenParseResult parse,
        IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)> metrics, Vector2 start, float cell) =>
        _drawDeferredMetrics(parse, metrics, start, cell);
}
