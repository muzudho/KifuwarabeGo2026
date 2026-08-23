namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>通常対局の着手、パス、投了、ローカルアプリ対局の終局を担当します。</summary>
public sealed partial class GoAppSession
{
    public bool TryPlaceStone(int x, int y, GoMoveAnalysis? analysis = null, string comment = "", string? commonAnalysisJson = null)
    {
        if (_isGameOasisLocalGame)
            return false;
        if (_matchSession is not null)
            return TryPlaceStoneWithMatch(x, y, analysis, comment, commonAnalysisJson);

        if (CurrentMode.Kind != GoAppModeKind.Playing ||
            !_board.TryPlaceStone(x, y, CurrentTurn, KoPoint, out var capturedStones, out var nextKoPoint))
        {
            return false;
        }

        var placedBy = CurrentTurn;
        CurrentGameRecord.Moves.Add(new GoGameMove(placedBy, new GoPoint(x, y), comment, analysis, commonAnalysisJson,
            timeLeftAfterMove: GetRemainingTimeAfterMove(placedBy)));
        if (placedBy == GoStone.Black)
            BlackAgehama += capturedStones;
        else
            WhiteAgehama += capturedStones;

        if (_positionHashes.Contains(_board.CurrentHash))
        {
            KoPoint = null;
            ConsecutivePasses = 0;
            Winner = OppositeOf(placedBy);
            GameOverReason = $"{StoneName(placedBy)} SUPER KO LOSS";
            ChangeMode(GoAppModeKind.GameOver);
            return true;
        }

        _positionHashes.Add(_board.CurrentHash);
        KoPoint = nextKoPoint;
        ConsecutivePasses = 0;
        CompleteMoveAndPassTurn();
        return true;
    }

    public bool Pass(string comment = "", GoMoveAnalysis? analysis = null, string? commonAnalysisJson = null)
    {
        if (_isGameOasisLocalGame)
            return false;
        if (_matchSession is not null)
            return PassWithMatch(comment, analysis, commonAnalysisJson);

        if (CurrentMode.Kind != GoAppModeKind.Playing)
            return false;

        KoPoint = null;
        ConsecutivePasses++;
        CurrentGameRecord.Moves.Add(new GoGameMove(CurrentTurn, null, comment, analysis, commonAnalysisJson,
            timeLeftAfterMove: GetRemainingTimeAfterMove(CurrentTurn)));
        CompleteMoveAndPassTurn();
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
            return true;

        if (ConsecutivePasses >= 2)
        {
            DecidePureGoResult();
            ChangeMode(GoAppModeKind.GameOver);
        }

        return true;
    }

    public string GetOwnEyeForcedPassComment()
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing)
            return "";

        var renParse = _board.ParseRens();
        var hasLegalMove = false;
        for (var y = 0; y < BoardSize; y++)
        for (var x = 0; x < BoardSize; x++)
        {
            var trialBoard = _board.Clone();
            if (!trialBoard.TryPlaceStone(x, y, CurrentTurn, KoPoint, out _, out _))
                continue;

            hasLegalMove = true;
            if (!_board.IsEyeFor(renParse, x, y, CurrentTurn))
                return "";
        }

        return hasLegalMove ? "自分の目に打つしかなかったのでパスした。" : "";
    }

    public bool Resign()
    {
        if (_isGameOasisLocalGame)
            return false;
        if (_matchSession is not null)
            return ResignWithMatch();

        if (CurrentMode.Kind != GoAppModeKind.Playing)
            return false;

        var resigned = CurrentTurn;
        Winner = OppositeOf(resigned);
        KoPoint = null;
        ConsecutivePasses = 0;
        GameOverReason = $"{StoneName(resigned)} RESIGNS";
        ChangeMode(GoAppModeKind.GameOver);
        return true;
    }

    public void CompleteLocalApp(GoStone? winner, string reason)
    {
        if (UseKind != GoAppUseKind.LocalApps || CurrentMode.Kind != GoAppModeKind.Playing)
            return;

        Winner = winner;
        GameOverReason = string.IsNullOrWhiteSpace(reason) ? "APP COMPLETED" : reason;
        CurrentGameRecord.Result = winner switch
        {
            GoStone.Black => "B+App",
            GoStone.White => "W+App",
            _ => "0",
        };
        ChangeMode(GoAppModeKind.GameOver);
    }

    private void CompleteMoveAndPassTurn()
    {
        PlayedMoveCount++;
        if (MoveLimit > 0 && PlayedMoveCount >= MoveLimit)
        {
            KoPoint = null;
            DecidePureGoResult();
            ChangeMode(GoAppModeKind.GameOver);
            return;
        }

        PassTurn();
    }

    private void DecidePureGoResult()
    {
        var blackStones = BlackStoneCount;
        var whiteStones = WhiteStoneCount;
        Winner = blackStones == whiteStones ? null : blackStones > whiteStones ? GoStone.Black : GoStone.White;
        GameOverReason = Winner is null
            ? "PURE GO DRAW"
            : $"PURE GO {StoneName(Winner.Value)} +{Math.Abs(blackStones - whiteStones)}";
    }
}
