namespace KifuwarabeGo2026.Reference.PlayRoomGui.Go;

using KifuwarabeGo2026.Reference.PlayDomain.Go;
using KifuwarabeGo2026.Reference.PlayRoomGui.Common;

/// <summary>Protocol Gの公開盤面を囲碁Play Room GUIの表示状態へ投影します。</summary>
public static class GuiBoardViewAdapter
{
    public static GoPlayRoomViewState Create(GuiBoardView board, GoPlayRoomActivity activity)
    {
        ArgumentNullException.ThrowIfNull(board);

        var intersections = new GoStone[checked(board.BoardSize * board.BoardSize)];
        foreach (var point in board.Black)
            intersections[(point.Y * board.BoardSize) + point.X] = GoStone.Black;
        foreach (var point in board.White)
            intersections[(point.Y * board.BoardSize) + point.X] = GoStone.White;

        var lastMove = board.MoveHistory.Count == 0 ? null : board.MoveHistory[^1].Point;
        return GoPlayRoomViewState.Capture(
            board.IsTerminal ? GoPlayRoomActivity.GameOver : activity,
            board.BoardSize,
            (x, y) => intersections[(y * board.BoardSize) + x],
            string.Equals(board.NextToPlay, "white", StringComparison.Ordinal)
                ? GoStone.White
                : GoStone.Black,
            board.MoveHistory.Count,
            board.BlackCaptures,
            board.WhiteCaptures,
            board.KoPoint is { } ko ? new GoPoint(ko.X, ko.Y) : null,
            null,
            board.IsTerminal ? board.Outcome?.Content : "",
            board.MoveHistory.Count,
            board.MoveHistory.Count,
            lastMove is { } movePoint ? new GoPoint(movePoint.X, movePoint.Y) : null);
    }
}
