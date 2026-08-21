namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.SelectConnection;
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
using KifuwarabeGo2026.Gui.Presentation.Title;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>各画面 Renderer の所有と、盤面・レビュー画面の描画順序を担当します。</summary>
public sealed class GoPresentationRenderer : System.IDisposable
{
    private readonly KfwStationeryDrawingTools _drawingContext;
    private readonly BoardRenderer _boardRenderer;
    private readonly MoveTrendChartRenderer _moveTrendChartRenderer;
    private readonly PopupTrendChartRenderer _popupTrendChartRenderer;
    private readonly CgosWatchingRenderer _cgosWatchingRenderer;
    private readonly GtpEngineRenderer _gtpEngineRenderer;
    private readonly CgosLoginRenderer _cgosLoginRenderer;
    private readonly TitleScreenRenderer _titleScreenRenderer;

    public GoPresentationRenderer(KfwStationeryDrawingTools drawingContext, BoardRenderer boardRenderer,
        MoveTrendChartRenderer moveTrendChartRenderer, PopupTrendChartRenderer popupTrendChartRenderer,
        CgosWatchingRenderer cgosWatchingRenderer, GtpEngineRenderer gtpEngineRenderer,
        CgosLoginRenderer cgosLoginRenderer, TitleScreenRenderer titleScreenRenderer)
    {
        _drawingContext = drawingContext;
        _boardRenderer = boardRenderer;
        _moveTrendChartRenderer = moveTrendChartRenderer;
        _popupTrendChartRenderer = popupTrendChartRenderer;
        _cgosWatchingRenderer = cgosWatchingRenderer;
        _gtpEngineRenderer = gtpEngineRenderer;
        _cgosLoginRenderer = cgosLoginRenderer;
        _titleScreenRenderer = titleScreenRenderer;
    }

    public void DrawTitle(GoAppSession session, Point mousePoint, TitleMenuPage page,
        int appProviderTabIndex, bool isAppProviderLoading) =>
        _titleScreenRenderer.DrawScreen(_drawingContext, _gtpEngineRenderer, session, mousePoint,
            page, appProviderTabIndex, isAppProviderLoading);

    public void DrawCgosWatch(GoAppSession session, CgosGameObservation observation, Point mousePoint) =>
        CgosWatchPage.Default.Draw(_cgosWatchingRenderer, _drawingContext, session, observation, mousePoint);

    public void DrawCgosLogin(GoAppSession session, Point mousePoint) =>
        CgosLoginPage.Default.Draw(_cgosLoginRenderer, _drawingContext, session, mousePoint);

    public void DrawCgosConnectionSelection(GoAppSession session, Point mousePoint) =>
        CgosSelectConnectionPage.Default.Draw(_cgosLoginRenderer, _drawingContext, session, mousePoint);

    public int GetCgosConnectionEditPanelCaretIndex(Point point, CgosConnectionProfileEditField field, string text) =>
        _cgosLoginRenderer.GetCgosConnectionEditPanelCaretIndex(point, field, text);

    public int GetCgosCredentialCaretIndex(Point point, GoStone stone, CgosPlayerCredentialField field, string text) =>
        _cgosLoginRenderer.GetCgosCredentialCaretIndex(point, stone, field, text);

    public bool TryToggleCgosPasswordVisibility(Point point, bool player2Enabled) =>
        _cgosLoginRenderer.TryToggleCgosPasswordVisibility(point, player2Enabled);

    public int GetGtpEngineEditPanelCaretIndex(Point point, GtpEngineProfileEditField field, string text) =>
        _gtpEngineRenderer.GetGtpEngineEditPanelCaretIndex(point, field, text);

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
            _cgosWatchingRenderer.DrawBroadcastStatusBadge(_drawingContext,
                session.IsLocalReplayMode ? "REPLAY" : "CURRENT", session.IsReviewChartPopupOpen);
        if (!session.IsReviewChartPopupOpen)
        {
            RightSidePanel.Default.Draw(_drawingContext, _moveTrendChartRenderer, session,
                backgroundMousePoint, liveBoardPreview, initialPositionConcierge);
            if (session.IsLocalReplayMode && session.CurrentMode.Kind == GoAppModeKind.Playing)
                _popupTrendChartRenderer.DrawReplayNavigationControls(_drawingContext,
                    session.LocalDisplayMoveIndex, session.CurrentGameRecord.Moves.Count, backgroundMousePoint,
                    showBackToLive: true, backToLiveLabel: "BACK TO CURRENT");
            else if (session.CanOpenLocalChartPopup || session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                _popupTrendChartRenderer.DrawReplayEditIconButton(_drawingContext, backgroundMousePoint);
            TournamentRulesPresenter.Default.Draw(_drawingContext, session, mousePoint);
            SelectEntryPresenter.Default.Draw(_drawingContext, session, mousePoint);
            EditEntryProfile.Default.Draw(_drawingContext, session, mousePoint, HeadUpDisplayComponent.Default.StickyNoteScreen);
            EntryProfilesPresenter.Default.DrawPanels(_drawingContext, session, mousePoint);
            _gtpEngineRenderer.Draw(_drawingContext, session, mousePoint);
        }
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing && session.IsReviewChartPopupOpen)
            _popupTrendChartRenderer.DrawReview(_drawingContext, session, mousePoint);
        else if (session.CanOpenLocalChartPopup && session.IsReviewChartPopupOpen)
            _popupTrendChartRenderer.DrawLocal(_drawingContext, session, mousePoint);
        _drawingContext.End();
    }

    public void Dispose() => _boardRenderer.Dispose();
}
