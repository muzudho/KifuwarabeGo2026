namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>REN RECTANGLE LENS の代表連番号描画です。</summary>
    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++)
        for (var x = 0; x < renParse.Size; x++)
        {
            var number = renParse.GetRenNumber(x, y);
            if (drawn[number]) continue;
            drawn[number] = true;
            DrawRenNumber(number, BoardPoint(start, cell, x, y), scale);
        }
    }

    /// <summary>REN RECTANGLE LENS の連境界を描画します。</summary>
    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var size = renParse.Size;
        var halfCell = cell * 0.5f;
        var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f));
        var color = new Color(255, 238, 0, 238);

        for (var y = 0; y < size; y++)
        for (var x = 0; x < size; x++)
        {
            var renNumber = renParse.GetRenNumber(x, y);
            var center = BoardPoint(start, cell, x, y);
            var left = center.X - halfCell;
            var top = center.Y - halfCell;
            var right = center.X + halfCell;
            var bottom = center.Y + halfCell;
            if (x == 0 || renParse.GetRenNumber(x - 1, y) != renNumber)
                FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
            if (y == 0 || renParse.GetRenNumber(x, y - 1) != renNumber)
                FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
            if (x == size - 1)
                FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
            if (y == size - 1)
                FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
        }
    }
}
