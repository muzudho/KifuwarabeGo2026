namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

public sealed partial class GoScreenRenderer
{
    /// <summary>選択中の Board Lens を描画します。</summary>
    private void DrawBoardRenAnalysis(RenParseDisplayMode displayMode, int boardSize, Func<int, int, GoStone> getStone, Func<GoRenParseResult> parseRens, Action drawPlacedStones, Vector2 start, float cell)
    {
        if (displayMode == RenParseDisplayMode.Off)
        {
            drawPlacedStones();
            return;
        }

        var renParse = parseRens();
        if (displayMode == RenParseDisplayMode.Overlay)
        {
            drawPlacedStones();
            DrawRenBoundaries(renParse, start, cell);
            DrawRenNumbers(renParse, start, cell);
            return;
        }
        if (displayMode == RenParseDisplayMode.Graph)
        {
            DrawRenGraphCells(boardSize, getStone, start, cell);
            DrawRenBoundaries(renParse, start, cell);
            DrawRenRepresentativeNumbers(renParse, start, cell);
            return;
        }
        if (displayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            var nodes = CreateRenGraphNodes(renParse, start, cell, displayMode == RenParseDisplayMode.Eye);
            FillRect(BoardBounds, new Color(56, 145, 129));
            DrawRenGraphEdges(nodes, renParse.Edges, cell);
            DrawRenGraphNodes(nodes, cell);
            return;
        }

        DrawRenGraphCells(boardSize, getStone, start, cell);
        DrawRenBoundaries(renParse, start, cell);
        if (displayMode == RenParseDisplayMode.RenArea)
        {
            DrawRenAreaNumbers(renParse, start, cell);
            return;
        }
        DrawRenBoundaryLens(renParse, displayMode, start, cell);
    }
}
