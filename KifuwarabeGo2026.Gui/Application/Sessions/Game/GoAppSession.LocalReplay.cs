namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>対局中・終局後に表示するローカル棋譜の一時シーク位置を管理します。</summary>
public sealed partial class GoAppSession
{
    private GoBoard? _localReplayBoard;
    private int? _localReplayMoveIndex;

    public bool IsLocalReplayMode =>
        CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver &&
        _localReplayMoveIndex is not null;

    public int LocalDisplayMoveIndex =>
        _localReplayMoveIndex ?? CurrentGameRecord.Moves.Count;

    /// <summary>終局後レビューの終端にある、棋譜の着手ではない結果表示位置です。</summary>
    public bool IsLocalResultPosition =>
        CurrentMode.Kind == GoAppModeKind.GameOver && _localReplayMoveIndex is null;

    public int LocalReviewTimelineIndex =>
        IsLocalResultPosition ? CurrentGameRecord.Moves.Count + 1 : LocalDisplayMoveIndex;

    public int LocalReviewTimelineMaximum => CurrentGameRecord.Moves.Count + 1;

    public void SeekLocalReplay(int moveIndex)
    {
        if (CurrentMode.Kind is not (GoAppModeKind.Playing or GoAppModeKind.GameOver) ||
            !CanOpenLocalChartPopup)
        {
            return;
        }

        var clampedMoveIndex = Math.Clamp(moveIndex, 0, CurrentGameRecord.Moves.Count);
        if (CurrentMode.Kind == GoAppModeKind.Playing && clampedMoveIndex == CurrentGameRecord.Moves.Count)
        {
            ReturnLocalReplayToLive();
            return;
        }

        _localReplayMoveIndex = clampedMoveIndex;
        _localReplayBoard = BuildLocalReplayBoard(clampedMoveIndex);
    }

    public void SeekLocalReviewTimeline(int timelineIndex)
    {
        if (CurrentMode.Kind != GoAppModeKind.GameOver) return;
        var target = Math.Clamp(timelineIndex, 0, LocalReviewTimelineMaximum);
        if (target == LocalReviewTimelineMaximum)
        {
            ReturnLocalReplayToLive();
            return;
        }
        SeekLocalReplay(target);
    }

    public void ReturnLocalReplayToLive()
    {
        _localReplayMoveIndex = null;
        _localReplayBoard = null;
    }

    public GoStone GetDisplayStone(int x, int y) =>
        (_localReplayBoard ?? _board).GetStone(x, y);

    public GoRenParseResult ParseDisplayRens() =>
        _localReplayBoard?.ParseRens() ?? ParseRens();

    private GoBoard BuildLocalReplayBoard(int moveIndex)
    {
        var board = new GoBoard(CurrentGameRecord.BoardSize);
        foreach (var setupStone in CurrentGameRecord.SetupStones)
            board.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone);

        GoPoint? koPoint = null;
        for (var index = 0; index < moveIndex; index++)
        {
            var move = CurrentGameRecord.Moves[index];
            if (move.Point is not { } point)
            {
                koPoint = null;
                continue;
            }

            if (board.TryPlaceStone(point.X, point.Y, move.Stone, koPoint, out _, out var nextKoPoint))
                koPoint = nextKoPoint;
        }

        return board;
    }
}
