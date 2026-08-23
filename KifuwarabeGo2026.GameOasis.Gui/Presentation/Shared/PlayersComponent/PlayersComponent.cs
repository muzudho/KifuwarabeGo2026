namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.PlayersComponent;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>
/// ［両対局者名前等コンポーネント］
/// </summary>
public sealed class PlayersComponent
{
    public static PlayersComponent Default { get; } = new();

    private PlayersComponent()
    {
    }

    private static readonly PlayerEngineErrorButton PlayerEngineErrorButton = new();
    private readonly PlayerRow _playerRow = new();
    private readonly AgehamaPlate _agehamaPlate = new();
    public bool GetEngineErrorLogHit(Point point, GoAppSession session)
    {
        if (session.CurrentMode.Kind != GoAppModeKind.Playing || session.EngineErrorStone is not { } errorStone)
        {
            return false;
        }

        var playerBounds = errorStone == GoStone.Black
            ? new Rectangle(1144, LocalMatchPlayRightSidePanel.PlayersY, 668, 88)
            : new Rectangle(1144, LocalMatchPlayRightSidePanel.PlayersY + 96, 668, 88);
        return PlayerEngineErrorButton.GetBounds(playerBounds).Contains(point);
    }

    /// <summary>
    /// 黒番と白番の名前、時間、アゲハマを共通レイアウトで描画します。
    /// </summary>
    public void DrawBothPlayers(KfwStationeryDrawingTools drawingContext,
        int x,
        int y,
        int width,
        string blackName,
        string whiteName,
        TimeSpan? blackElapsed,
        TimeSpan? whiteElapsed,
        TimeSpan? mainTime,
        int blackAgehama,
        int whiteAgehama,
        GoStone currentTurn,
        GoStone? engineErrorStone = null,
        Point? mousePoint = null,
        bool minimal = false,
        TimeSpan? blackLiveElapsed = null,
        TimeSpan? whiteLiveElapsed = null)
    {
        DrawPlayer(drawingContext, new Rectangle(x, y, width, 88), blackName, blackElapsed, blackLiveElapsed, mainTime, blackAgehama, black: true, currentTurn == GoStone.Black, engineErrorStone == GoStone.Black, mousePoint, minimal);
        DrawPlayer(drawingContext, new Rectangle(x, y + 96, width, 88), whiteName, whiteElapsed, whiteLiveElapsed, mainTime, whiteAgehama, black: false, currentTurn == GoStone.White, engineErrorStone == GoStone.White, mousePoint, minimal);
    }

    private void DrawPlayer(KfwStationeryDrawingTools drawingContext,
        Rectangle bounds,
        string playerName,
        TimeSpan? elapsed,
        TimeSpan? liveElapsed,
        TimeSpan? mainTime,
        int agehama,
        bool black,
        bool active,
        bool engineError,
        Point? mousePoint,
        bool minimal)
    {
        _playerRow.Draw(new PlayerRowModel(bounds, playerName, elapsed, liveElapsed, mainTime, agehama, black, active,
                engineError, mousePoint, minimal, RightSidePanelLayout.PrimaryValueX),
            new PlayerRowDrawingCallbacks(bounds => drawingContext.DrawDataRowFrame(bounds), drawingContext.FillRectangle,
                drawingContext.DrawRectangle, drawingContext.DrawStone, drawingContext.DrawIconStone,
                drawingContext.DrawFittedText, FormatElapsedTime, drawingContext.DrawCircleSurface, drawingContext.DrawLine));
    }

    private void DrawAgehamaPlate(KfwStationeryDrawingTools drawingContext, Rectangle bounds, int agehama, bool capturedBlack)
    {
        _agehamaPlate.Draw(bounds, agehama, capturedBlack,
            new AgehamaPlateDrawingCallbacks(drawingContext.DrawCircleSurface, drawingContext.DrawStone, drawingContext.DrawFittedText));
    }

    public void DrawAgehamaSummary(KfwStationeryDrawingTools drawingContext, Rectangle bounds, int blackAgehama, int whiteAgehama)
    {
        DrawAgehamaSummaryRow(drawingContext, new Rectangle(bounds.X, bounds.Y, bounds.Width, 60), "BLACK CAPTURES", blackAgehama, capturedBlack: false);
        DrawAgehamaSummaryRow(drawingContext, new Rectangle(bounds.X, bounds.Y + 68, bounds.Width, 60), "WHITE CAPTURES", whiteAgehama, capturedBlack: true);
    }

    private void DrawAgehamaSummaryRow(KfwStationeryDrawingTools drawingContext, Rectangle bounds, string label, int agehama, bool capturedBlack)
    {
        drawingContext.DrawDataRowFrame(bounds);
        drawingContext.DrawFittedText(label, new Rectangle(bounds.X + 20, bounds.Y + 13, bounds.Width - 190, 34), new Color(204, 211, 206), 0.38f);
        DrawAgehamaPlate(drawingContext, new Rectangle(bounds.Right - 144, bounds.Y + 10, 126, 40), agehama, capturedBlack);
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        var totalHours = (int)elapsed.TotalHours;
        return totalHours > 0
            ? $"{totalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

}
