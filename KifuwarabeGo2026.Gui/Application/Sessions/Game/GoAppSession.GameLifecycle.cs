namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Match;
using System;

/// <summary>ローカル対局の開始と中断を担当します。</summary>
public sealed partial class GoAppSession
{
    public void StartPlaying()
    {
        ResetLiveChartAutoUpdate();
        IsLocalResultSgfSaved = false;
        if (CurrentMode.Kind == GoAppModeKind.GameOver)
            ClearBoard();

        CurrentGameRecord = CreateGameRecordFromCurrentPosition();
        BlackElapsedTime = TimeSpan.Zero;
        WhiteElapsedTime = TimeSpan.Zero;
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
