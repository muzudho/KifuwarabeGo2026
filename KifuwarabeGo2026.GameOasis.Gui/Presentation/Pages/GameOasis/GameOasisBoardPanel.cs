namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.GameOasis;

using KifuwarabeGo2026.GameOasis.Contracts.Common;
using KifuwarabeGo2026.Reference.GUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

public static class GameOasisBoardPanel
{
    public static readonly Rectangle PassBounds = new(1144, 828, 320, 72);
    public static readonly Rectangle ResignBounds = new(1492, 828, 320, 72);
    public static readonly Rectangle CloseBounds = new(1144, 920, 668, 72);

    public static bool IsPassHit(Point point) => PassBounds.Contains(point);
    public static bool IsResignHit(Point point) => ResignBounds.Contains(point);
    public static bool IsCloseHit(Point point) => CloseBounds.Contains(point);

    public static void Draw(
        KfwStationeryDrawingTools drawingContext,
        GuiBoardView board,
        Point mousePoint,
        bool canPlayPoint,
        bool canPass,
        bool canResign,
        bool canClose,
        ProtocolError? error)
    {
        RightSidePanelFrame.Draw(drawingContext);
        drawingContext.DrawVerticalResultSection(new Rectangle(1144, 132, 668, 180), "GAME OASIS", new Color(76, 91, 126));
        drawingContext.DrawInfoStrip(1144, 156, "GAME", GetGameLabel(board));
        drawingContext.DrawInfoStrip(1144, 214, "STATE", board.IsTerminal ? "TERMINAL" : $"{board.NextToPlay.ToUpperInvariant()} TO PLAY");
        drawingContext.DrawInfoStrip(1144, 272, "REVISION", board.Revision.ToString());

        drawingContext.DrawVerticalResultSection(new Rectangle(1144, 344, 668, 220), "POSITION", new Color(66, 104, 116));
        drawingContext.DrawInfoStrip(1144, 372, "BOARD", $"{board.BoardSize} x {board.BoardSize}");
        drawingContext.DrawInfoStrip(1144, 430, "BLACK CAPTURES", board.BlackCaptures.ToString());
        drawingContext.DrawInfoStrip(1144, 488, "WHITE CAPTURES", board.WhiteCaptures.ToString());

        if (error is not null)
        {
            drawingContext.DrawVerticalResultSection(new Rectangle(1144, 590, 668, 150), "MESSAGE", new Color(119, 72, 67));
            drawingContext.DrawFittedText($"{error.Code}: {error.Message}", new Rectangle(1164, 630, 628, 82), new Color(255, 198, 174), 0.28f);
        }

        if ((canPass || canResign) && !board.IsTerminal)
        {
            drawingContext.DrawVerticalResultSection(new Rectangle(1144, 824, 668, 76), "ACTION", new Color(91, 82, 105));
            drawingContext.DrawButton(PassBounds, "PASS", false, mousePoint, canPass, 0.62f);
            drawingContext.DrawButton(ResignBounds, "RESIGN", false, mousePoint, canResign, 0.62f);
        }

        drawingContext.DrawButton(CloseBounds, "CLOSE SESSION", false, mousePoint, canClose, 0.48f);
    }

    private static string GetGameLabel(GuiBoardView board) => board.PlaySpaceTypeId.Value switch
    {
        GameOasisOfficialNames.Go => "GO",
        GameOasisOfficialNames.Ponnuki => "PONNUKI",
        _ => board.PlaySpaceTypeId.Value,
    };
}
