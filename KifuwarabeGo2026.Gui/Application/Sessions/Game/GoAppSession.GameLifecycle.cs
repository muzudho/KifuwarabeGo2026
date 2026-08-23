namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Reference.PlaySpace.Go.LegacyMatch;
using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局の開始と中断を担当します。</summary>
public sealed partial class GoAppSession
{
    /// <summary>対局開始時に固定する LocalMatch 棋譜の初期ファイル名。</summary>
    public string LocalMatchSgfFileName { get; private set; } = "kifuwarabe-go.sgf";

    public void StartPlaying()
    {
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
        _matchSession = new MatchSession(CreateMatchConfiguration());
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
