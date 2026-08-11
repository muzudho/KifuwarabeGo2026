namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>CGOS 接続・管理・編集画面の状態を保持します。</summary>
public sealed partial class GoAppSession
{
    private readonly List<CgosConnectionProfile> _cgosConnectionProfiles = new();
    private CgosConnectionProfile _cgosConnectionEditSource = CreateDefaultCgosConnectionProfile();
    private DateTime? _cgosBlackConnectionStartedAt;
    private DateTime? _cgosWhiteConnectionStartedAt;

    public IReadOnlyList<CgosConnectionProfile> CgosConnectionProfiles => _cgosConnectionProfiles;
    public CatalogOrderEditor<CgosConnectionProfile> CgosConnectionOrderEditor { get; } = new();
    public CgosConnectionFlowKind CgosConnectionFlowKind { get; private set; }
    public string CgosConnectionStatusMessage { get; private set; } = "READY";
    public string CgosConnectionLogDirectory { get; private set; } = "";
    public IReadOnlyList<string> CgosConnectionRecentOutput { get; private set; } = Array.Empty<string>();
    public string CgosBlackConnectionStatusMessage { get; private set; } = "READY";
    public string CgosBlackConnectionLogDirectory { get; private set; } = "";
    public IReadOnlyList<string> CgosBlackConnectionRecentOutput { get; private set; } = Array.Empty<string>();
    public bool IsCgosBlackConnectionRunning { get; private set; }
    public string CgosBlackConnectionElapsedDisplay => FormatCgosConnectionElapsedDisplay(_cgosBlackConnectionStartedAt, IsCgosBlackConnectionRunning);
    public string CgosBlackGtpResponseWaitDisplay { get; private set; } = "";
    public string CgosWhiteConnectionStatusMessage { get; private set; } = "READY";
    public string CgosWhiteConnectionLogDirectory { get; private set; } = "";
    public IReadOnlyList<string> CgosWhiteConnectionRecentOutput { get; private set; } = Array.Empty<string>();
    public bool IsCgosWhiteConnectionRunning { get; private set; }
    public bool IsCgosPlayer2InputEnabled { get; private set; }
    public string CgosWhiteConnectionElapsedDisplay => FormatCgosConnectionElapsedDisplay(_cgosWhiteConnectionStartedAt, IsCgosWhiteConnectionRunning);
    public string CgosWhiteGtpResponseWaitDisplay { get; private set; } = "";
    public string CgosAdminStatusMessage { get; private set; } = "ADMIN READY";
    public string CgosAdminLogDirectory { get; private set; } = "";
    public IReadOnlyList<string> CgosAdminRecentOutput { get; private set; } = Array.Empty<string>();
    public bool IsCgosAdminRunning { get; private set; }
    public bool IsCgosAdminInputEnabled { get; private set; }
    public IReadOnlyList<string> CgosAdminWaitingPlayers { get; private set; } = Array.Empty<string>();
    public int CgosAdminWhitePlayerIndex { get; private set; }
    public int CgosAdminBlackPlayerIndex { get; private set; } = 1;
    public string CgosAdminWhitePlayerName => GetCgosAdminWaitingPlayer(CgosAdminWhitePlayerIndex);
    public string CgosAdminBlackPlayerName => GetCgosAdminWaitingPlayer(CgosAdminBlackPlayerIndex);
    public bool IsCgosAdminPlayerSelectionDialogOpen { get; private set; }
    public GoStone CgosAdminPlayerSelectionTarget { get; private set; } = GoStone.White;
    public int CgosAdminPlayerDialogSelectionIndex { get; private set; }
    public int CgosAdminPlayerSelectionPageIndex { get; private set; }
    public bool CanSendCgosAdminMatch => IsCgosAdminRunning && CgosAdminWaitingPlayers.Count >= 2 && !CgosAdminWhitePlayerName.Equals(CgosAdminBlackPlayerName, StringComparison.OrdinalIgnoreCase);
    public int? SelectedCgosBlackGtpEngineIndex { get; private set; } = 0;
    public int? SelectedCgosWhiteGtpEngineIndex { get; private set; } = 0;
    public GtpEngineProfile? SelectedCgosBlackGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosBlackGtpEngineIndex);
    public GtpEngineProfile? SelectedCgosWhiteGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosWhiteGtpEngineIndex);
    public bool HasSelectedCgosGtpEngine => SelectedCgosBlackGtpEngineProfile is not null || SelectedCgosWhiteGtpEngineProfile is not null;
    public bool IsAnyCgosProcessRunning => IsCgosConnectionRunning || IsCgosBlackConnectionRunning || IsCgosWhiteConnectionRunning || IsCgosAdminRunning;
    public bool IsCgosGameInProgress { get; private set; }
    public bool IsCgosConnectionRunning { get; private set; }
    public int SelectedCgosConnectionProfileIndex { get; private set; }
    public CgosConnectionProfile SelectedCgosConnectionProfile => _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex];
    public bool IsCgosConnectionEditPanelOpen { get; private set; }
    public bool IsCgosConnectionAddPanelMode { get; private set; }
    public CgosConnectionProfileEditField? ActiveCgosConnectionEditField { get; private set; }
    public int CgosConnectionEditCaretIndex { get; private set; }
    public int CgosConnectionEditSelectionStart { get; private set; }
    public int CgosConnectionEditSelectionLength { get; private set; }
    public CgosConnectionProfile CgosConnectionEditDraft { get; private set; } = CreateDefaultCgosConnectionProfile();
    public string CgosConnectionPortDraft { get; private set; } = "6809";
    public string CgosConnectionEditWarning { get; private set; } = "";
    public string CgosConnectionEditSaveMessage { get; private set; } = "";
    public int CgosConnectionSelectionPageIndex { get; private set; }
}
