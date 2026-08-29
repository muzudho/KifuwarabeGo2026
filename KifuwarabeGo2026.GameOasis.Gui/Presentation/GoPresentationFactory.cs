namespace KifuwarabeGo2026.GameOasis.Gui.Presentation;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

/// <summary>Canvas、共通UI道具、囲碁画面Rendererを生成して接続します。</summary>
internal static class GoPresentationFactory
{
    public static GoPresentationServices Create(GraphicsDevice graphicsDevice, ContentManager content,
        ITextRasterizer textRasterizer)
    {
        var canvas = new KfwScreenCanvas(graphicsDevice, content);
        BoardRenderer? boardRenderer = null;
        var boardLensModel = new BoardLensModel(
            BoardRenderer.BoardPoint,
            BoardRenderer.RenGraphCellColor,
            canvas.DrawLine,
            canvas.DrawCircle,
            canvas.FillRectangle,
            canvas.DrawRectangle,
            canvas.DrawEllipseWire,
            (number, center, scale) => boardRenderer!.DrawRenNumber(number, center, scale),
            (ren, value, color, start, cell, outline) => boardRenderer!.DrawRenMetricNumber(ren, value, color, start, cell, outline),
            (parse, metrics, start, cell) => boardRenderer!.DrawDeferredStrongMetrics(parse, metrics, start, cell));
        var coordinateFont = content.Load<SpriteFont>("Fonts/BoardCoordinate");
        boardRenderer = new BoardRenderer(
            boardLensModel,
            canvas.SpriteBatch,
            canvas.Font,
            coordinateFont,
            canvas.SoftCircle,
            graphicsDevice);

        var stationery = new KfwStationeryDrawingTools(canvas, textRasterizer, boardRenderer.DrawStone,
            () => HeadUpDisplayComponent.Default.StickyNoteScreen);
        var moveCommentPanelRenderer = new MoveCommentPanelRenderer(
            graphicsDevice, canvas.SpriteBatch, textRasterizer, stationery);
        var moveTrendChartRenderer = new MoveTrendChartRenderer(moveCommentPanelRenderer);
        var popupTrendChartRenderer = new PopupTrendChartRenderer(moveTrendChartRenderer);
        var cgosWatchingRenderer = new CgosWatchingRenderer(boardRenderer, moveTrendChartRenderer,
            popupTrendChartRenderer, boardRenderer.DrawBoardRenAnalysis);
        var gtpEngineRenderer = new GtpEngineRenderer(graphicsDevice, canvas.SpriteBatch, canvas.Font, textRasterizer);
        var cgosLoginRenderer = new CgosLoginRenderer(gtpEngineRenderer,
            (session, mousePoint) => EditEntryProfile.Default.Draw(
                stationery, session, mousePoint, HeadUpDisplayComponent.Default.StickyNoteScreen));
        var titleScreenRenderer = new TitleScreenRenderer(canvas.DrawEllipseWire, canvas.DrawCircumscribedCircleArc);
        var presentation = new GoPresentationRenderer(stationery, boardRenderer,
            moveTrendChartRenderer, popupTrendChartRenderer, cgosWatchingRenderer,
            gtpEngineRenderer, cgosLoginRenderer, titleScreenRenderer);
        return new GoPresentationServices(canvas, stationery, presentation);
    }
}

internal sealed record GoPresentationServices(
    KfwScreenCanvas Canvas,
    KfwStationeryDrawingTools Stationery,
    GoPresentationRenderer Presentation) : System.IDisposable
{
    public void Dispose()
    {
        Presentation.Dispose();
        Stationery.Dispose();
        Canvas.Dispose();
    }
}
