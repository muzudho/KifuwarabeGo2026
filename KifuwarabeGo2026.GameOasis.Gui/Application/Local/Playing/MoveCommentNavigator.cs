namespace KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;

using System;
using System.Collections.Generic;

/// <summary>
/// コメント付き着手の件数、通し番号、前後の着手番号を求めます。
/// </summary>
public static class MoveCommentNavigator
{
    public static int Count(IReadOnlyList<GoGameMove> moves, int maximumMoveNumber = int.MaxValue)
    {
        var count = 0;
        var limit = Math.Min(moves.Count, Math.Max(0, maximumMoveNumber));
        for (var index = 0; index < limit; index++)
        {
            if (!string.IsNullOrWhiteSpace(moves[index].Comment))
            {
                count++;
            }
        }

        return count;
    }

    public static int GetOrdinal(
        IReadOnlyList<GoGameMove> moves,
        int moveNumber,
        int maximumMoveNumber = int.MaxValue)
    {
        if (moveNumber <= 0 || moveNumber > Math.Min(moves.Count, maximumMoveNumber))
        {
            return 0;
        }

        var ordinal = 0;
        for (var index = 0; index < moveNumber; index++)
        {
            if (!string.IsNullOrWhiteSpace(moves[index].Comment))
            {
                ordinal++;
            }
        }

        return string.IsNullOrWhiteSpace(moves[moveNumber - 1].Comment)
            ? 0
            : ordinal;
    }

    public static int? FindAdjacent(
        IReadOnlyList<GoGameMove> moves,
        int currentMoveNumber,
        int direction,
        int maximumMoveNumber = int.MaxValue)
    {
        if (direction == 0)
        {
            return null;
        }

        var limit = Math.Min(moves.Count, Math.Max(0, maximumMoveNumber));
        if (direction < 0)
        {
            for (var index = Math.Min(currentMoveNumber - 2, limit - 1); index >= 0; index--)
            {
                if (!string.IsNullOrWhiteSpace(moves[index].Comment))
                {
                    return index + 1;
                }
            }
        }
        else
        {
            for (var index = Math.Max(0, currentMoveNumber); index < limit; index++)
            {
                if (!string.IsNullOrWhiteSpace(moves[index].Comment))
                {
                    return index + 1;
                }
            }
        }

        return null;
    }
}
