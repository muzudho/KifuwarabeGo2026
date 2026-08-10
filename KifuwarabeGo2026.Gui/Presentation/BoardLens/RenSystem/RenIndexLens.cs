namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    /// <summary>REN INDEX LENS の連番号描画です。</summary>
    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = RenNumberScale(cell);
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
                DrawRenNumber(renParse.GetRenNumber(x, y), BoardPoint(start, cell, x, y), scale);
        }
    }
}
