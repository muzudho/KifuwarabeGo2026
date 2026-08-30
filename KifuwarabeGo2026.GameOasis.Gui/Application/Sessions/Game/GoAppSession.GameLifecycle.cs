namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.PlayRoomEngine.Go.Match;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using System;

/// <summary>ローカル対局の開始と中断を担当します。</summary>
public sealed partial class GoAppSession
{
    /// <summary>対局開始時に固定する LocalMatch 棋譜の初期ファイル名。</summary>
    public string LocalMatchSgfFileName { get; private set; } = "kifuwarabe-go.sgf";

    public void StartPlaying()
    {
        StartPlayingCore(createCompatibilityMatch: true);
    }

    /// <summary>Protocol Sを正本にする対局を、旧MatchSessionを生成せず開始します。</summary>
    public void StartPlayingForGameOasis()
    {
        StartPlayingCore(createCompatibilityMatch: false);
    }

    private void StartPlayingCore(bool createCompatibilityMatch)
    {
        _isGameOasisProjectedLocalGame = false;
        _isGameOasisLocalGame = !createCompatibilityMatch;
        _gameOasisProjectedMoveCount = 0;
        ResetLiveChartAutoUpdate();
        IsLocalResultSgfSaved = false;
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
            ClearBoard();

        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        LocalMatchSgfFileName = LocalMatchSgfFileNameBuilder.Create(
            GetLocalMatchPresentedName(GoStone.Black),
            GetLocalMatchPresentedName(GoStone.White),
            DateTime.Now);
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
        BlackUsedTime = TimeSpan.Zero;
        WhiteUsedTime = TimeSpan.Zero;
        _timingTurn = CurrentTurn;
        _matchSession = createCompatibilityMatch ? new MatchSession(CreateMatchConfiguration()) : null;
        ChangeMode(GoAppModeKind.Playing);
    }

    public void CancelPlaying()
    {
        ChangeMode(GoAppModeKind.Resting);
        IsEngineReady = true;
        IsEngineThinking = false;
        EngineErrorMessage = "";
    }
}
