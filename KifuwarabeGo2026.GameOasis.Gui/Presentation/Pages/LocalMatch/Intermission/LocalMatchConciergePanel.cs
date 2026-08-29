namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.LocalMatch.Intermission;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using Microsoft.Xna.Framework;

/// <summary>
/// Draws the non-board workspace shown while a local match is being prepared.
/// The board belongs to the game space and is intentionally absent here.
/// </summary>
public static class LocalMatchConciergePanel
{
    private static readonly Rectangle Bounds = new(54, 50, 980, 980);

    public static void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session)
    {
        DrawFrame(drawingContext);

        drawingContext.DrawFittedText("MATCH CONCIERGE", new Rectangle(104, 96, 820, 72),
            new Color(147, 244, 200), 0.72f);
        drawingContext.DrawFittedText("PREPARE YOUR NEXT GAME", new Rectangle(106, 166, 760, 38),
            new Color(158, 174, 181), 0.32f);

        DrawStatusCard(drawingContext, session);
        DrawPlayerCard(drawingContext, session, GoStone.Black, 430);
        DrawPlayerCard(drawingContext, session, GoStone.White, 566);
        DrawGuide(drawingContext, session);
    }

    private static void DrawFrame(KfwStationeryDrawingTools drawingContext)
    {
        drawingContext.FillRectangle(new Rectangle(Bounds.X + 18, Bounds.Y + 22, Bounds.Width, Bounds.Height),
            new Color(0, 0, 0, 125));
        drawingContext.FillRectangle(Bounds, new Color(18, 27, 32, 238));
        drawingContext.DrawRectangle(Bounds, 3, new Color(62, 112, 105));
        drawingContext.FillRectangle(new Rectangle(Bounds.X, Bounds.Y, 9, Bounds.Height), new Color(62, 112, 105));
        drawingContext.FillRectangle(new Rectangle(Bounds.X + 9, Bounds.Y, Bounds.Width - 9, 5),
            new Color(147, 244, 200, 90));
    }

    private static void DrawStatusCard(KfwStationeryDrawingTools drawingContext, GoAppSession session)
    {
        var card = new Rectangle(104, 242, 880, 142);
        var ready = session.CanStartPlaying;
        var accent = ready ? new Color(99, 223, 185) : new Color(255, 205, 112);
        drawingContext.FillRectangle(card, new Color(26, 38, 43, 232));
        drawingContext.DrawRectangle(card, 2, new Color(61, 88, 91));
        drawingContext.FillRectangle(new Rectangle(card.X, card.Y, 8, card.Height), accent);
        drawingContext.DrawFittedText(
            session.UseKind == GoAppUseKind.LocalApps ? "LOCAL APP MATCH" : session.TournamentDisplayName,
            new Rectangle(140, 264, 610, 44), Color.White, 0.42f);
        drawingContext.DrawFittedText(
            session.UseKind == GoAppUseKind.LocalApps
                ? "PONNUKI / APP PROVIDER"
                : $"{session.RuleKind}  /  {session.BoardSize} x {session.BoardSize}  /  KOMI {session.Komi:0.0}",
            new Rectangle(140, 318, 610, 34), new Color(180, 195, 195), 0.30f);
        drawingContext.FillRoundedRectangle(new Rectangle(780, 276, 166, 58), 8, new Color(accent, 42));
        drawingContext.DrawCenteredFittedText(ready ? "READY" : "SETUP REQUIRED",
            new Rectangle(792, 286, 142, 38), accent, ready ? 0.42f : 0.27f);
    }

    private static void DrawPlayerCard(KfwStationeryDrawingTools drawingContext, GoAppSession session,
        GoStone stone, int y)
    {
        var black = stone == GoStone.Black;
        var card = new Rectangle(104, y, 880, 108);
        drawingContext.FillRectangle(card, new Color(23, 32, 40, 220));
        drawingContext.DrawRectangle(card, 2, new Color(55, 68, 91));
        drawingContext.DrawIconStone(new Vector2(158, card.Center.Y), 25, black);
        drawingContext.DrawFittedText(black ? "BLACK PLAYER" : "WHITE PLAYER",
            new Rectangle(206, y + 17, 250, 30), new Color(158, 174, 181), 0.28f);
        drawingContext.DrawFittedText(session.GetLocalPlayerName(stone),
            new Rectangle(206, y + 50, 700, 40), Color.White, 0.40f);
    }

    private static void DrawGuide(KfwStationeryDrawingTools drawingContext, GoAppSession session)
    {
        drawingContext.DrawFittedText("BEFORE YOU START", new Rectangle(104, 728, 420, 40),
            new Color(147, 201, 190), 0.34f);
        DrawGuideRow(drawingContext, 790, "1", "CHOOSE THE GAME RULES", true);
        DrawGuideRow(drawingContext, 850, "2", "CONFIRM BOTH PLAYERS", session.CanStartPlaying);
        DrawGuideRow(drawingContext, 910, "3", "ENTER THE GAME SPACE", false);
        drawingContext.DrawFittedText("THE BOARD WILL OPEN AFTER START.",
            new Rectangle(650, 936, 300, 30), new Color(255, 205, 112), 0.25f);
    }

    private static void DrawGuideRow(KfwStationeryDrawingTools drawingContext, int y, string number,
        string text, bool complete)
    {
        var accent = complete ? new Color(99, 223, 185) : new Color(100, 110, 145);
        drawingContext.FillRoundedRectangle(new Rectangle(106, y, 42, 42), 21, accent);
        drawingContext.DrawCenteredFittedText(number, new Rectangle(116, y + 7, 22, 26),
            new Color(15, 20, 31), 0.34f);
        drawingContext.DrawFittedText(text, new Rectangle(174, y + 5, 590, 34),
            complete ? Color.White : new Color(180, 195, 195), 0.31f);
    }
}
