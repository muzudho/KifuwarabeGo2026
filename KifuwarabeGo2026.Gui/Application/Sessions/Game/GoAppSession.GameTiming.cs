namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局の黒白経過時間を管理します。</summary>
public sealed partial class GoAppSession
{
    public TimeSpan BlackElapsedTime { get; private set; }
    public TimeSpan WhiteElapsedTime { get; private set; }
    public TimeSpan BlackUsedTime { get; private set; }
    public TimeSpan WhiteUsedTime { get; private set; }
    private GoStone? _timingTurn;

    public void AddCurrentTurnElapsedTime(TimeSpan elapsed)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing ||
            !IsEngineReady ||
            !string.IsNullOrWhiteSpace(EngineErrorMessage))
        {
            return;
        }

        if (_timingTurn != CurrentTurn)
        {
            if (_timingTurn == GoStone.Black) BlackUsedTime = BlackElapsedTime;
            if (_timingTurn == GoStone.White) WhiteUsedTime = WhiteElapsedTime;
            _timingTurn = CurrentTurn;
        }

        if (CurrentTurn == GoStone.Black)
        {
            BlackElapsedTime += elapsed;
            return;
        }

        WhiteElapsedTime += elapsed;
    }

    private TimeSpan? GetRemainingTimeAfterMove(GoStone stone)
    {
        if (MainTime <= TimeSpan.Zero) return null;
        var elapsed = stone == GoStone.Black ? BlackElapsedTime : WhiteElapsedTime;
        return elapsed >= MainTime ? TimeSpan.Zero : MainTime - elapsed;
    }
}
