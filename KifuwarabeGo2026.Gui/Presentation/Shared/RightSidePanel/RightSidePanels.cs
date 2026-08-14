namespace KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Intermission;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;

public static class RightSidePanelFrame
{
    public static readonly Rectangle Bounds = new(1102, 78, 760, 924);

    public static void Draw(GoScreenRenderer renderer)
    {
        var panel = Bounds;
        renderer.FillRectangle(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        renderer.FillRectangle(panel, new Color(21, 25, 32, 236));
        renderer.DrawRectangle(panel, 2, new Color(82, 111, 114));
    }
}

/// <summary>現在のアプリケーション状態に対応する右側パネルを選択して描画します。</summary>
public sealed class RightSidePanel
{
    public static RightSidePanel Default { get; } = new();

    private RightSidePanel()
    {
    }

    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint,
        LiveBoardPreview? liveBoardPreview, InitialPositionConciergeView? initialPositionConcierge)
    {
        RightSidePanelFrame.Draw(renderer);

        if (initialPositionConcierge is { IsVisible: true })
        {
            LocalMatchScreen.Default.InitialPositionConciergeRightSidePanel.Draw(renderer, initialPositionConcierge, mousePoint);
            return;
        }

        switch (session.CurrentMode.Kind)
        {
            case GoAppModeKind.Playing:
                LocalMatchPlayPage.Default.RightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.GameOver:
                LocalMatchScreen.Default.GameOverRightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.BoardEditing:
                BoardAndReviewScreen.Default.BoardEditingRightSidePanel.Draw(renderer, session, mousePoint);
                return;
            case GoAppModeKind.VariationEditing:
                BoardAndReviewScreen.Default.VariationEditingRightSidePanel.Draw(renderer, session, mousePoint, liveBoardPreview);
                return;
            case GoAppModeKind.Reviewing:
                BoardAndReviewScreen.Default.ReviewingRightSidePanel.Draw(renderer, session, mousePoint);
                return;
        }

        if (session.UseKind == GoAppUseKind.LocalApps)
            LocalMatchIntermissionPage.Default.RightSidePanel.Draw(renderer, session, mousePoint);
        else
            LocalMatchScreen.Default.SetupRightSidePanel.Draw(renderer, session, mousePoint);
    }
}

public sealed class LocalMatchPlayRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) =>
        LocalMatchPlayPage.Default.DrawRightSidePanelContent(renderer, session, mousePoint);
}

public sealed class LocalMatchIntermissionRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) =>
        LocalMatchIntermissionPage.Default.DrawRightSidePanelContent(renderer, session, mousePoint);
}

public sealed class SetupRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawSetupRightSidePanelContent(session, mousePoint);
}

public sealed class GameOverRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawGameOverRightSidePanelContent(session, mousePoint);
}

public sealed class BoardEditingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawBoardEditingRightSidePanelContent(session, mousePoint);
}

public sealed class VariationEditingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint, LiveBoardPreview? preview) =>
        renderer.DrawVariationEditingRightSidePanelContent(session, mousePoint, preview);
}

public sealed class ReviewingRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePoint) => renderer.DrawReviewingRightSidePanelContent(session, mousePoint);
}

public sealed class InitialPositionConciergeRightSidePanel
{
    public void Draw(GoScreenRenderer renderer, InitialPositionConciergeView view, Point mousePoint) =>
        renderer.DrawInitialPositionConciergeContent(view, mousePoint);
}
