namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public sealed partial class GoScreenRenderer
{
    private void DrawRenParseOverlay(GoAppSession session, Vector2 start, float cell)
    {
        if (session.RenParseDisplayMode != RenParseDisplayMode.RenArea)
            return;

        var renParse = session.ParseRens();
        DrawRenBoundaries(renParse, start, cell);
        DrawRenNumbers(renParse, start, cell);
    }

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
