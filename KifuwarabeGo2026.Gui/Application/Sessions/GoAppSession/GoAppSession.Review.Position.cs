namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>レビュー棋譜を指定手数まで再生し、表示用の盤面状態を作ります。</summary>
public sealed partial class GoAppSession
{
    private bool ApplyReviewPosition(int moveCount, out string warning)
    {
        if (_reviewGameRecord is null)
        {
            warning = "No SGF game record is loaded.";
            return false;
        }

        return ApplyRecordPosition(_reviewGameRecord, moveCount, out warning);
    }

    /// <summary>
    /// 指定した棋譜を指定手数まで再生し、現在の盤面・表示レコードを更新します。
    /// レビュー用正本を差し替えず、変化図編集からも安全に利用できます。
    /// </summary>
    private bool ApplyRecordPosition(GoGameRecord record, int moveCount, out string warning)
    {
        var loadedBoard = new GoBoard(record.BoardSize);
        foreach (var setupStone in record.SetupStones)
        {
            if (!loadedBoard.TrySetSetupStone(setupStone.Point.X, setupStone.Point.Y, setupStone.Stone))
            {
                warning = $"Invalid SGF setup stone at {setupStone.Point.X + 1},{setupStone.Point.Y + 1}.";
                return false;
            }
        }

        var blackAgehama = 0;
        var whiteAgehama = 0;
        var consecutivePasses = 0;
        GoPoint? replayKoPoint = null;
        GoPoint? currentKoPoint = null;
        TimeSpan? blackTimeLeft = null;
        TimeSpan? whiteTimeLeft = null;
        var clampedMoveCount = Math.Clamp(moveCount, 0, record.Moves.Count);
        for (var i = 0; i < clampedMoveCount; i++)
        {
            var move = record.Moves[i];
            if (move.Stone == GoStone.Black && move.TimeLeftAfterMove is { } blackLeft) blackTimeLeft = blackLeft;
            if (move.Stone == GoStone.White && move.TimeLeftAfterMove is { } whiteLeft) whiteTimeLeft = whiteLeft;
            if (move.Point is not { } point)
            {
                replayKoPoint = null;
                currentKoPoint = null;
                consecutivePasses++;
                continue;
            }

            if (!loadedBoard.TryPlaceStone(point.X, point.Y, move.Stone, replayKoPoint, out var capturedStones, out var nextKoPoint))
            {
                warning = $"Illegal SGF move at {point.X + 1},{point.Y + 1}.";
                return false;
            }

            if (move.Stone == GoStone.Black)
                blackAgehama += capturedStones;
            else
                whiteAgehama += capturedStones;

            replayKoPoint = nextKoPoint;
            currentKoPoint = nextKoPoint;
            consecutivePasses = 0;
        }

        BoardSize = record.BoardSize;
        _currentTournamentRules.BoardSize = BoardSize;
        _currentTournamentRules.Komi = record.Komi;
        TournamentRulesSaveMessage = "UNSAVED";
        _board = loadedBoard;
        CurrentTurn = GetReviewCurrentTurn(record, clampedMoveCount);
        BlackAgehama = blackAgehama;
        WhiteAgehama = whiteAgehama;
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        KoPoint = currentKoPoint;
        ConsecutivePasses = consecutivePasses;
        PlayedMoveCount = clampedMoveCount;
        ReviewMoveIndex = clampedMoveCount;
        ReviewBlackUsedTime = GetUsedTime(record.TimeLimit, blackTimeLeft);
        ReviewWhiteUsedTime = GetUsedTime(record.TimeLimit, whiteTimeLeft);
        Winner = null;
        GameOverReason = "";
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
        CurrentGameRecord = CreateReviewGameRecord(record, clampedMoveCount);
        ResetPositionHistory();
        ClearBoardEditingHistory();
        warning = "";
        return true;
    }

    private static TimeSpan? GetUsedTime(TimeSpan limit, TimeSpan? timeLeft) =>
        limit > TimeSpan.Zero && timeLeft is { } remaining
            ? (remaining >= limit ? TimeSpan.Zero : limit - remaining)
            : null;

    private static GoGameRecord CreateReviewGameRecord(GoGameRecord source, int moveCount)
    {
        var record = new GoGameRecord
        {
            GameName = source.GameName,
            RuleName = source.RuleName,
            BlackPlayerName = source.BlackPlayerName,
            WhitePlayerName = source.WhitePlayerName,
            BlackRank = source.BlackRank,
            WhiteRank = source.WhiteRank,
            PlayedDate = source.PlayedDate,
            Result = source.Result,
            Place = source.Place,
            BoardSize = source.BoardSize,
            Komi = source.Komi,
            TimeLimit = source.TimeLimit,
        };

        record.SetupStones.AddRange(source.SetupStones);
        for (var i = 0; i < Math.Clamp(moveCount, 0, source.Moves.Count); i++)
            record.Moves.Add(source.Moves[i]);

        return record;
    }

    private static GoStone GetReviewCurrentTurn(GoGameRecord record, int moveCount)
    {
        if (moveCount < record.Moves.Count)
            return record.Moves[moveCount].Stone;

        if (moveCount > 0)
        {
            var lastStone = record.Moves[moveCount - 1].Stone;
            return lastStone == GoStone.Black ? GoStone.White : GoStone.Black;
        }

        return GoStone.Black;
    }
}
