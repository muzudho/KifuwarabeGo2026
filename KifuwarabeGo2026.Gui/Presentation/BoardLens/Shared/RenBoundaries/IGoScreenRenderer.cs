namespace KifuwarabeGo2026.Gui.Presentation.BoardLens.Shared.RenBoundaries;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

/// <summary>連の境界 Lens が GoScreenRenderer へ要求する最小限の公開描画操作です。</summary>
public interface IGoScreenRenderer
{
    Vector2 GetBoardPoint(Vector2 start, float cell, int x, int y);
    Color GetRenGraphCellColor(GoStone stone);
    void DrawBoardLensLine(Vector2 start, Vector2 end, float thickness, Color color);
    void DrawBoardLensCircle(Vector2 center, float radius, Color color);
    void FillBoardLensRectangle(Rectangle bounds, Color color);
    void DrawRenBoundaryPointMetric(GoRen ren, int value, Color valueColor, Vector2 start, float cell, Color? outlineColor);
    void DrawDeferredStrongBoundaryMetrics(GoRenParseResult renParse, IReadOnlyList<(int RenNumber, int Value, Color Color, Color Outline)> metrics, Vector2 start, float cell);
}
