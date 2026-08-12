namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting;
using System.Collections.Generic;

/// <summary>アプリの盤面モード遷移と、遷移時の共通後始末を管理します。</summary>
public sealed partial class GoAppSession
{
    private readonly Dictionary<GoAppModeKind, GoAppMode> _modes = new()
    {
        [GoAppModeKind.Playing] = new PlayingMode(),
        [GoAppModeKind.GameOver] = new GameOverMode(),
        [GoAppModeKind.BoardEditing] = new BoardEditingMode(),
        [GoAppModeKind.VariationEditing] = new VariationEditingMode(),
        [GoAppModeKind.Reviewing] = new ReviewingMode(),
        [GoAppModeKind.Resting] = new RestingMode(),
    };

    public GoAppMode CurrentMode { get; private set; }

    public void ChangeMode(GoAppModeKind modeKind)
    {
        CurrentMode = _modes[modeKind];
        if (modeKind != GoAppModeKind.Playing)
            ReturnLocalReplayToLive();

        if (modeKind != GoAppModeKind.Reviewing)
            CloseReviewChartPopup();
    }
}
