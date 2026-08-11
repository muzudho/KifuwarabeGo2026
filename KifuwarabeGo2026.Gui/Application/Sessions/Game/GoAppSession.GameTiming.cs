namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Shared.Domain;
using System;

/// <summary>ローカル対局の黒白経過時間を管理します。</summary>
public sealed partial class GoAppSession
{
    public TimeSpan BlackElapsedTime { get; private set; }
    public TimeSpan WhiteElapsedTime { get; private set; }

    public void AddCurrentTurnElapsedTime(TimeSpan elapsed)
    {
        if (CurrentMode.Kind != GoAppModeKind.Playing ||
            !IsEngineReady ||
            !string.IsNullOrWhiteSpace(EngineErrorMessage))
        {
            return;
        }

        if (CurrentTurn == GoStone.Black)
        {
            BlackElapsedTime += elapsed;
            return;
        }

        WhiteElapsedTime += elapsed;
    }
}
