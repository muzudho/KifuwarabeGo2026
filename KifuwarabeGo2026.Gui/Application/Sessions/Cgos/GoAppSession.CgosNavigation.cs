namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using CgosFlowKind = KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget.CgosConnectionFlowKind;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>CGOSの接続プロファイル選択、画面遷移、接続開始要求を管理します。</summary>
public sealed partial class GoAppSession
{
    private static string FormatCgosConnectionElapsedDisplay(DateTime? startedAt, bool isRunning)
    {
        if (!isRunning || startedAt is null)
            return "";

        var elapsedSeconds = Math.Max(0, (int)(DateTime.Now - startedAt.Value).TotalSeconds);
        return $"RUN {elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}";
    }

    public void SelectCgosConnectionProfile(int index)
    {
        if (index < 0 || index >= _cgosConnectionProfiles.Count)
            throw new ArgumentOutOfRangeException(nameof(index), index, "CGOS connection profile index is out of range.");

        SelectedCgosConnectionProfileIndex = index;
        CgosConnectionSelectionPageIndex = index / CgosConnectionSelectionPageSize;
        ApplyCgosClientIdentityCredentials(GoStone.Black);
        ApplyCgosClientIdentityCredentials(GoStone.White);
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

    public void SetCgosConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        CgosConnectionStatusMessage = statusMessage;
        IsCgosConnectionRunning = isRunning;
        CgosConnectionLogDirectory = logDirectory;
        CgosConnectionRecentOutput = recentOutput;
    }

    public void SetCgosBlackConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput, string gtpResponseWaitDisplay = "")
    {
        if (isRunning && !IsCgosBlackConnectionRunning)
            _cgosBlackConnectionStartedAt = DateTime.Now;
        CgosBlackConnectionStatusMessage = statusMessage;
        IsCgosBlackConnectionRunning = isRunning;
        CgosBlackConnectionLogDirectory = logDirectory;
        CgosBlackConnectionRecentOutput = recentOutput;
        CgosBlackGtpResponseWaitDisplay = gtpResponseWaitDisplay;
    }

    public void SetCgosWhiteConnectionProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput, string gtpResponseWaitDisplay = "")
    {
        if (isRunning && !IsCgosWhiteConnectionRunning)
            _cgosWhiteConnectionStartedAt = DateTime.Now;
        CgosWhiteConnectionStatusMessage = statusMessage;
        IsCgosWhiteConnectionRunning = isRunning;
        CgosWhiteConnectionLogDirectory = logDirectory;
        CgosWhiteConnectionRecentOutput = recentOutput;
        CgosWhiteGtpResponseWaitDisplay = gtpResponseWaitDisplay;
    }

    public void SetCgosAdminProcessStatus(string statusMessage, bool isRunning, string logDirectory, IReadOnlyList<string> recentOutput)
    {
        CgosAdminStatusMessage = statusMessage;
        IsCgosAdminRunning = isRunning;
        CgosAdminLogDirectory = logDirectory;
        CgosAdminRecentOutput = recentOutput;
    }
}
