namespace KifuwarabeGo2026.Gui.Application;

using System;
using System.Collections.Generic;
using System.Linq;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>CGOS 接続・管理・編集画面の状態を保持します。</summary>
public sealed partial class GoAppSession
{
    public const int CgosConnectionSelectionPageSize = 5;
    public const int CgosAdminPlayerSelectionPageSize = 6;

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
    public string CgosBlackEntryProfileId { get; private set; } = "";
    public string CgosWhiteEntryProfileId { get; private set; } = "";
    public string CgosBlackClientIdentityProfileId { get; private set; } = "";
    public string CgosWhiteClientIdentityProfileId { get; private set; } = "";
    public EntryProfile? SelectedCgosBlackEntryProfile => FindEntryProfile(CgosBlackEntryProfileId);
    public EntryProfile? SelectedCgosWhiteEntryProfile => FindEntryProfile(CgosWhiteEntryProfileId);
    public GtpEngineProfile? SelectedCgosBlackGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosBlackGtpEngineIndex);
    public GtpEngineProfile? SelectedCgosWhiteGtpEngineProfile => GetCgosGtpEngineProfile(SelectedCgosWhiteGtpEngineIndex);
    public bool HasSelectedCgosGtpEngine => SelectedCgosBlackGtpEngineProfile is not null || SelectedCgosWhiteGtpEngineProfile is not null;
    public bool IsAnyCgosProcessRunning => IsCgosConnectionRunning || IsCgosBlackConnectionRunning || IsCgosWhiteConnectionRunning || IsCgosAdminRunning;
    public bool IsCgosGameInProgress { get; private set; }
    public bool IsCgosPracticeUnexpectedGameInProgress { get; private set; }
    public bool IsCgosPracticeResignConfirmationPending { get; private set; }
    public bool IsCgosPracticeResignRequested { get; private set; }
    public int CgosPracticeUnexpectedGameId { get; private set; }
    public string CgosPracticeUnexpectedOpponent { get; private set; } = "-";
    public string CgosPracticeUnexpectedColor { get; private set; } = "-";
    public int CgosPracticeUnexpectedMoveCount { get; private set; }
    public string CgosPracticeUnexpectedTimeDisplay { get; private set; } = "";
    public bool IsCgosConnectionRunning { get; private set; }

    public void SetCgosPracticeUnexpectedGame(
        bool inProgress,
        int gameId,
        string opponent,
        GoStone color,
        int moveCount,
        TimeSpan remainingTime)
    {
        IsCgosPracticeUnexpectedGameInProgress = inProgress;
        CgosPracticeUnexpectedGameId = gameId;
        CgosPracticeUnexpectedOpponent = string.IsNullOrWhiteSpace(opponent) ? "-" : opponent;
        CgosPracticeUnexpectedColor = color switch
        {
            GoStone.Black => "BLACK",
            GoStone.White => "WHITE",
            _ => "-",
        };
        CgosPracticeUnexpectedMoveCount = moveCount;
        CgosPracticeUnexpectedTimeDisplay = $"{Math.Max(0, (int)remainingTime.TotalMinutes):00}:{Math.Max(0, remainingTime.Seconds):00}";
        if (!inProgress)
        {
            IsCgosPracticeResignConfirmationPending = false;
            IsCgosPracticeResignRequested = false;
        }
    }

    public void RequestCgosPracticeResignConfirmation()
    {
        if (IsCgosPracticeUnexpectedGameInProgress && !IsCgosPracticeResignRequested)
            IsCgosPracticeResignConfirmationPending = true;
    }

    public void CancelCgosPracticeResignConfirmation() => IsCgosPracticeResignConfirmationPending = false;

    public void MarkCgosPracticeResignRequested()
    {
        IsCgosPracticeResignConfirmationPending = false;
        IsCgosPracticeResignRequested = true;
    }
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
    public bool IsCgosConnectionEditDirty =>
        CgosConnectionEditDraft != _cgosConnectionEditSource ||
        CgosConnectionPortDraft != _cgosConnectionEditSource.Port.ToString();
    public int CgosConnectionSelectionPageIndex { get; private set; }

    public bool TrySelectCgosEntryProfile(GoStone stone, string playerProfileId)
    {
        var player = FindEntryProfile(playerProfileId);
        var isPrimaryHuman = stone == GoStone.Black && player?.Kind == EntryProfileKind.Human;
        var isEngine = player?.Kind == EntryProfileKind.Computer && FindGtpEngineIndex(player.EngineProfileId) >= 0;
        if (player is null || (!isPrimaryHuman && !isEngine) ||
            GetPlayerClientIdentityProfiles(player.Id).Count == 0)
        {
            return false;
        }

        var engineIndex = player.Kind == EntryProfileKind.Computer ? FindGtpEngineIndex(player.EngineProfileId) : -1;
        if (stone == GoStone.Black)
        {
            CgosBlackEntryProfileId = player.Id;
            SelectedCgosBlackGtpEngineIndex = engineIndex;
        }
        else if (stone == GoStone.White)
        {
            CgosWhiteEntryProfileId = player.Id;
            SelectedCgosWhiteGtpEngineIndex = engineIndex;
        }
        else throw new ArgumentOutOfRangeException(nameof(stone), stone, "CGOS player can be selected only for black or white.");

        SetDefaultCgosTarget(stone, player);
        ApplyCgosClientIdentityCredentials(stone);
        return true;
    }

    public bool TrySelectCgosClientIdentityProfile(GoStone stone, string targetProfileId)
    {
        var player = stone == GoStone.Black ? SelectedCgosBlackEntryProfile : SelectedCgosWhiteEntryProfile;
        if (player is null || !GetPlayerClientIdentityProfiles(player.Id).Any(target =>
                string.Equals(target.Id, targetProfileId, StringComparison.Ordinal)))
            return false;

        if (stone == GoStone.Black) CgosBlackClientIdentityProfileId = targetProfileId;
        else if (stone == GoStone.White) CgosWhiteClientIdentityProfileId = targetProfileId;
        else return false;
        ApplyCgosClientIdentityCredentials(stone);
        return true;
    }

    private void SetDefaultCgosTarget(GoStone stone, EntryProfile player)
    {
        var targetId = GetPlayerClientIdentityProfiles(player.Id).FirstOrDefault()?.Id ?? "";
        if (stone == GoStone.Black) CgosBlackClientIdentityProfileId = targetId;
        else CgosWhiteClientIdentityProfileId = targetId;
    }
}
