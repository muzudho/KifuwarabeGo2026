namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting;
using KifuwarabeGo2026.Reference.PlayerEngine.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Reference.PlayDomain.Go;

/// <summary>アプリケーションセッションの共通初期化を行います。</summary>
public sealed partial class GoAppSession
{
    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
        _gtpEngineProfiles.Add(new GtpEngineProfile());
        _gtpEngineAppCompatibilities.Add(new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY FORMAL APP"));
        ResetPositionHistory();
    }
}
