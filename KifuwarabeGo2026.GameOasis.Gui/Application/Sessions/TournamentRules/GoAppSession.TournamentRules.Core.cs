namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>大会規定の盤サイズ、ルール、コミ、持ち時間、手数制限を変更します。</summary>
public sealed partial class GoAppSession
{
    private void ApplyTournamentRules(TournamentRules rules)
    {
        _currentTournamentRules = rules.Clone();
        BoardSize = _currentTournamentRules.BoardSize is 9 or 13 or 19 ? _currentTournamentRules.BoardSize : 19;
        _currentTournamentRules.BoardSize = BoardSize;
        ClearBoard();
    }

    public void ChangeBoardSize(int boardSize)
    {
        if (boardSize is not (9 or 13 or 19))
            throw new ArgumentOutOfRangeException(nameof(boardSize), boardSize, "Board size must be 9, 13, or 19.");

        if (BoardSize == boardSize)
            return;

        BoardSize = boardSize;
        _currentTournamentRules.BoardSize = boardSize;
        TournamentRulesSaveMessage = "UNSAVED";
        ClearBoard();
    }

    public void ChangeRuleKind(GoRuleKind ruleKind)
    {
        _currentTournamentRules.Rule = ruleKind;
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeKomi(decimal step)
    {
        _currentTournamentRules.Komi = Math.Clamp(_currentTournamentRules.Komi + step, -99.5m, 99.5m);
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeMainTime(TimeSpan step)
    {
        var totalSeconds = Math.Clamp(
            (int)(_currentTournamentRules.MainTime + step).TotalSeconds,
            0,
            999 * 3600 + 59 * 60 + 59);
        SetMainTime(totalSeconds);
    }

    public void SetMainTime(int totalSeconds)
    {
        totalSeconds = Math.Clamp(totalSeconds, 0, 999 * 3600 + 59 * 60 + 59);
        _currentTournamentRules.MainTimeMinutes = totalSeconds / 60;
        _currentTournamentRules.MainTimeSeconds = totalSeconds % 60;
        TournamentRulesSaveMessage = "UNSAVED";
    }

    public void ChangeMoveLimit(int step) => SetMoveLimit(_currentTournamentRules.MoveLimit + step);

    public void SetMoveLimit(int moveLimit)
    {
        _currentTournamentRules.MoveLimit = Math.Clamp(moveLimit, 0, 9999);
        TournamentRulesSaveMessage = "UNSAVED";
    }
}
