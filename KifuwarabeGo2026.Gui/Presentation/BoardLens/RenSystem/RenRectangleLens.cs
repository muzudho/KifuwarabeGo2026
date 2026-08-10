namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

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
}
