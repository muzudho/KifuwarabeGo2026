namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.Gui.Presentation.Shared.LiveBoardPreview;
using KifuwarabeGo2026.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;

/// <summary>各画面 Renderer の所有と、盤面・レビュー画面の描画順序を担当します。</summary>
public sealed class GoPresentationRenderer
{
    private readonly StationeryDrawingContext _drawingContext;
    private readonly BoardRenderer _boardRenderer;
    private readonly MoveTrendChartRenderer _moveTrendChartRenderer;
    private readonly PopupTrendChartRenderer _popupTrendChartRenderer;

    public GoPresentationRenderer(StationeryDrawingContext drawingContext, BoardRenderer boardRenderer,
        MoveTrendChartRenderer moveTrendChartRenderer, PopupTrendChartRenderer popupTrendChartRenderer,
        CgosWatchingRenderer cgosWatchingRenderer, GtpEngineRenderer gtpEngineRenderer,
        CgosLoginRenderer cgosLoginRenderer, TitleScreenRenderer titleScreenRenderer)
    {
        _drawingContext = drawingContext;
        _boardRenderer = boardRenderer;
        _moveTrendChartRenderer = moveTrendChartRenderer;
        _popupTrendChartRenderer = popupTrendChartRenderer;
        CgosWatchingRenderer = cgosWatchingRenderer;
        GtpEngineRenderer = gtpEngineRenderer;
        CgosLoginRenderer = cgosLoginRenderer;
        TitleScreenRenderer = titleScreenRenderer;
    }

    public CgosWatchingRenderer CgosWatchingRenderer { get; }
    public GtpEngineRenderer GtpEngineRenderer { get; }
    public CgosLoginRenderer CgosLoginRenderer { get; }
    public TitleScreenRenderer TitleScreenRenderer { get; }

    public void Draw(GoAppSession session, Point mousePosition,
        LiveBoardPreviewModel? liveBoardPreview = null,
        InitialPositionConciergeView? initialPositionConcierge = null)
    {
        var mousePoint = _drawingContext.ToVirtualPoint(mousePosition);
        _drawingContext.Begin();
        _drawingContext.DrawBackground();
        var modalOpen = session.IsTournamentRulesSelectionDialogOpen || session.IsTournamentRulesAddPanelOpen ||
                        session.IsPlayerSelectionDialogOpen || session.IsPlayerEditPanelOpen ||
                        session.IsClientIdentityProfileSelectionPanelOpen || session.IsClientIdentityProfileEditPanelOpen ||
                        session.IsGtpEngineSelectionDialogOpen || session.IsGtpEngineEditPanelOpen ||
                        session.IsAppProviderGameSettingsDialogOpen;
        var backgroundMousePoint = modalOpen ? new Point(-1, -1) : mousePoint;
        _boardRenderer.Draw(_drawingContext, session, backgroundMousePoint);
        if (session.CurrentMode.Kind == GoAppModeKind.Playing && session.CanOpenLocalChartPopup)
            CgosWatchingRenderer.DrawBroadcastStatusBadge(_drawingContext,
                session.IsLocalReplayMode ? "REPLAY" : "CURRENT", session.IsReviewChartPopupOpen);
        if (!session.IsReviewChartPopupOpen)
        {
            RightSidePanel.Default.Draw(_drawingContext, _moveTrendChartRenderer, session,
                backgroundMousePoint, liveBoardPreview, initialPositionConcierge);
            if (session.IsLocalReplayMode)
                _popupTrendChartRenderer.DrawReplayNavigationControls(_drawingContext,
                    session.LocalDisplayMoveIndex, session.CurrentGameRecord.Moves.Count, backgroundMousePoint,
                    session.CurrentMode.Kind == GoAppModeKind.Playing, "BACK TO CURRENT");
            else if (session.CanOpenLocalChartPopup || session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                _popupTrendChartRenderer.DrawReplayEditIconButton(_drawingContext, backgroundMousePoint);
            TournamentRulesPresenter.Default.Draw(_drawingContext, session, mousePoint);
            SelectEntryPresenter.Default.Draw(_drawingContext, session, mousePoint);
            EditEntryProfile.Default.Draw(_drawingContext, session, mousePoint, HeadUpDisplayComponent.Default.StickyNoteScreen);
            EntryProfilesPresenter.Default.DrawPanels(_drawingContext, session, mousePoint);
            GtpEngineRenderer.Draw(_drawingContext, session, mousePoint);
        }
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing && session.IsReviewChartPopupOpen)
            _popupTrendChartRenderer.DrawReview(_drawingContext, session, mousePoint);
        else if (session.CanOpenLocalChartPopup && session.IsReviewChartPopupOpen)
            _popupTrendChartRenderer.DrawLocal(_drawingContext, session, mousePoint);
        _drawingContext.End();
    }
}
