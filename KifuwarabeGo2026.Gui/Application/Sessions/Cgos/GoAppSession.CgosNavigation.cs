namespace KifuwarabeGo2026.Gui.Application;

using System;
using CgosFlowKind = KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget.CgosConnectionFlowKind;

/// <summary>CGOSの接続プロファイル選択、画面遷移、接続開始要求を管理します。</summary>
public sealed partial class GoAppSession
{
    public void SelectCgosConnectionProfile(int index)
    {
        if (index < 0 || index >= _cgosConnectionProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "CGOS connection profile index is out of range.");

        SelectedCgosConnectionProfileIndex = index;
        CgosConnectionSelectionPageIndex = index / CgosConnectionSelectionPageSize;
        CgosConnectionStatusMessage = "READY";
    }

    public void OpenCgosConnectionStartScreen()
    {
        if (_cgosConnectionProfiles.Count == 0)
            return;

        CloseCgosConnectionEditPanel();
        CgosConnectionFlowKind = CgosFlowKind.ConnectionStart;
        CgosConnectionStatusMessage = "READY";
    }

    public void OpenCgosWatchingScreen() => CgosConnectionFlowKind = CgosFlowKind.Watching;
    public void OpenCgosResultScreen() => CgosConnectionFlowKind = CgosFlowKind.Result;
    public void ReturnToCgosConnectionScreen() => CgosConnectionFlowKind = CgosFlowKind.ConnectionStart;

    public void ToggleCgosPlayer2Input()
    {
        if (!IsCgosWhiteConnectionRunning)
            IsCgosPlayer2InputEnabled = !IsCgosPlayer2InputEnabled;
    }

    public void ToggleCgosAdminInput()
    {
        if (!IsCgosAdminRunning)
            IsCgosAdminInputEnabled = !IsCgosAdminInputEnabled;
    }

    public void SetCgosGameInProgress(bool inProgress) => IsCgosGameInProgress = inProgress;

    public void ReturnToCgosConnectionProfiles()
    {
        CgosConnectionFlowKind = CgosFlowKind.ProfileSelection;
        CgosConnectionStatusMessage = "READY";
    }

    public void RequestCgosConnectionStart()
    {
        if (CgosConnectionFlowKind == CgosFlowKind.ConnectionStart)
            CgosConnectionStatusMessage = "CONNECT REQUESTED";
    }
}
