namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

/// <summary>ローカル対局の対局中ページを描画します。</summary>
public sealed class LocalMatchPlayPage
{
    public static LocalMatchPlayPage Default { get; } = new();

    private LocalMatchPlayPage()
    {
        PassButton = new Button(new Rectangle(1144, 920, 320, 72), "PASS", 0.62f);
        ResignButton = new Button(new Rectangle(1492, 920, 320, 72), "RESIGN", 0.62f);
        CancelButton = new Button(new Rectangle(1144, 920, 668, 72), "CANCEL", 0.62f);
    }

    public Button PassButton { get; }
    public Button ResignButton { get; }
    public Button CancelButton { get; }
    public LocalMatchPlayRightSidePanel RightSidePanel { get; } = new();

    internal void DrawRightSidePanelContent(GoScreenRenderer renderer, GoAppSession session, Point mousePoint)
    {
        renderer.DrawVerticalResultSection(new Rectangle(1144, 132, 668, 200), "PLAYERS", new Color(76, 91, 126));
        renderer.DrawBothPlayersComponent(
            1144,
            GoScreenRenderer.PlayingPlayersY,
            668,
            session.GetLocalPlayerName(GoStone.Black),
            session.GetLocalPlayerName(GoStone.White),
            session.BlackUsedTime,
            session.WhiteUsedTime,
            session.MainTime,
            session.BlackAgehama,
            session.WhiteAgehama,
            session.CurrentTurn,
            session.EngineErrorStone,
            mousePoint,
            minimal: true,
            blackLiveElapsed: session.BlackElapsedTime,
            whiteLiveElapsed: session.WhiteElapsedTime);

        renderer.DrawVerticalResultSection(new Rectangle(1144, 344, 668, 110), "FACTS", new Color(66, 104, 116));
        renderer.DrawInfoStrip(1144, 363, "NEXT", GoScreenRenderer.GetMoveThinkingText(session));
        renderer.DrawLocalTrendChart(session, mousePoint);
        renderer.DrawVerticalResultSection(new Rectangle(1144, 780, 668, 120), "REVIEW", new Color(76, 91, 126));
        renderer.DrawLocalPlayingBoardLensButtonStrip(session.IsRenParseDisplayEnabled, mousePoint);
        renderer.DrawVerticalResultSection(new Rectangle(1144, 916, 668, 76), "ACTION", new Color(91, 82, 105));

        var drawingContext = renderer.StationeryDrawingContext;
        if (session.CanAcceptHumanMove)
        {
            PassButton.Draw(mousePoint, drawingContext);
            ResignButton.Draw(mousePoint, drawingContext);
        }
        else
        {
            CancelButton.Draw(mousePoint, drawingContext);
        }
    }
}
