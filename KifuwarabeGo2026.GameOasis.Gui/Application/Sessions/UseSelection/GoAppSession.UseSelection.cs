namespace KifuwarabeGo2026.GameOasis.Gui.Application;

using System;
using CgosFlowKind = KifuwarabeGo2026.GameOasis.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget.CgosConnectionFlowKind;

/// <summary>アプリ用途の選択と、用途選択へ戻る際の接続状態リセットを管理します。</summary>
public sealed partial class GoAppSession
{
    public GoAppUseKind? UseKind { get; private set; }

    public void SelectUseKind(GoAppUseKind useKind) => UseKind = useKind;

    public void ReturnToUseSelection()
    {
        UseKind = null;
        CgosConnectionFlowKind = CgosFlowKind.ProfileSelection;
        CgosConnectionStatusMessage = "READY";
        CgosConnectionLogDirectory = "";
        CgosConnectionRecentOutput = Array.Empty<string>();
        IsCgosConnectionRunning = false;
        CgosBlackConnectionStatusMessage = "READY";
        CgosBlackConnectionLogDirectory = "";
        CgosBlackConnectionRecentOutput = Array.Empty<string>();
        IsCgosBlackConnectionRunning = false;
        CgosWhiteConnectionStatusMessage = "READY";
        CgosWhiteConnectionLogDirectory = "";
        CgosWhiteConnectionRecentOutput = Array.Empty<string>();
        IsCgosWhiteConnectionRunning = false;
        CgosAdminStatusMessage = "ADMIN READY";
        CgosAdminLogDirectory = "";
        CgosAdminRecentOutput = Array.Empty<string>();
        CgosAdminWaitingPlayers = Array.Empty<string>();
        IsCgosAdminRunning = false;
        CloseCgosConnectionEditPanel();
    }
}
