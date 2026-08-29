namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go.LegacyMatch;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System.Collections.Generic;

/// <summary>対局ルールを MatchSession に委譲する着手・パス・終局処理を担当します。</summary>
public sealed partial class GoAppSession
{
    private MatchSession? _matchSession;

    public bool IsMatchBackedLocalGame => _matchSession is not null;
    public MatchSnapshot? CurrentMatchSnapshot => _matchSession?.Snapshot;

    private MatchConfiguration CreateMatchConfiguration()
    {
        var setupStones = new List<MatchSetupStone>();
        for (var y = 0; y < BoardSize; y++)
        for (var x = 0; x < BoardSize; x++)
        {
            var stone = _board.GetStone(x, y);
            if (stone != GoStone.Empty)
                setupStones.Add(new MatchSetupStone(stone, new GoPoint(x, y)));
        }

        return new MatchConfiguration(BoardSize, MoveLimit, CurrentTurn, setupStones);
    }

    private bool TryPlaceStoneWithMatch(int x, int y, GoMoveAnalysis? analysis, string comment, string? commonAnalysisJson)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || _matchSession is null)
            return false;

        var result = _matchSession.Play(new GoPoint(x, y));
        if (!result.Succeeded)
            return false;

        CurrentGameRecord.Moves.Add(new GoGameMove(result.PlayedBy, result.Point, comment, analysis, commonAnalysisJson,
            timeLeftAfterMove: GetRemainingTimeAfterMove(result.PlayedBy)));
        if (result.PlayedBy == GoStone.Black)
            BlackAgehama += result.CapturedStones;
        else
            WhiteAgehama += result.CapturedStones;

        ApplyMatchSnapshot(result.Snapshot);
        CompleteMatchBackedActionIfNeeded(result.Snapshot, result.PlayedBy);
        return true;
    }

    private bool PassWithMatch(string comment, GoMoveAnalysis? analysis, string? commonAnalysisJson)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || _matchSession is null)
            return false;

        var result = _matchSession.Pass();
        if (!result.Succeeded)
            return false;

        CurrentGameRecord.Moves.Add(new GoGameMove(result.PlayedBy, null, comment, analysis, commonAnalysisJson,
            timeLeftAfterMove: GetRemainingTimeAfterMove(result.PlayedBy)));
        ApplyMatchSnapshot(result.Snapshot);
        CompleteMatchBackedActionIfNeeded(result.Snapshot, result.PlayedBy);
        return true;
    }

    private bool ResignWithMatch()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing || _matchSession is null)
            return false;

        var result = _matchSession.Resign();
        if (!result.Succeeded)
            return false;

        ApplyMatchSnapshot(result.Snapshot);
        CompleteMatchBackedActionIfNeeded(result.Snapshot, result.PlayedBy);
        return true;
    }

    private void ApplyMatchSnapshot(MatchSnapshot snapshot)
    {
        var board = new GoBoard(snapshot.BoardSize);
        for (var y = 0; y < snapshot.BoardSize; y++)
        for (var x = 0; x < snapshot.BoardSize; x++)
        {
            var stone = snapshot.GetStone(new GoPoint(x, y));
            if (stone != GoStone.Empty)
                board.TrySetSetupStone(x, y, stone);
        }

        _board = board;
        CurrentTurn = snapshot.CurrentTurn;
        KoPoint = snapshot.KoPoint;
        ConsecutivePasses = snapshot.ConsecutivePasses;
        PlayedMoveCount = snapshot.MoveCount;
    }

    private void CompleteMatchBackedActionIfNeeded(MatchSnapshot snapshot, GoStone playedBy)
    {
        if (snapshot.IsAwaitingResult)
        {
            DecidePureGoResult();
            ChangeMode(GoAppModeKind.GameOver);
            return;
        }

        if (!snapshot.IsCompleted)
            return;

        Winner = snapshot.Winner;
        GameOverReason = snapshot.EndReason switch
        {
            MatchEndReason.Resignation => $"{StoneName(playedBy)} RESIGNS",
            MatchEndReason.SuperKoViolation => $"{StoneName(playedBy)} SUPER KO LOSS",
            _ => "MATCH COMPLETED",
        };
        ChangeMode(GoAppModeKind.GameOver);
    }
}
