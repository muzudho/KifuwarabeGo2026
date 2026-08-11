namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.GtpExtensions.Engines;
using KifuwarabeGo2026.GtpExtensions.InitialPosition;
using KifuwarabeGo2026.Shared.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public sealed partial class GoAppSession
{
    private GoBoard _board;
    private readonly HashSet<ulong> _positionHashes = new();
    private readonly List<TournamentRules> _tournamentRules = new();
    private readonly List<GtpEngineProfile> _gtpEngineProfiles = new();
    private readonly List<GtpEngineAppCompatibility> _gtpEngineAppCompatibilities = new();
    private readonly List<CgosConnectionProfile> _cgosConnectionProfiles = new();
    private CgosConnectionProfile _cgosConnectionEditSource = CreateDefaultCgosConnectionProfile();
    private TournamentRules _currentTournamentRules = new();
    private DateTime? _cgosBlackConnectionStartedAt;
    private DateTime? _cgosWhiteConnectionStartedAt;

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
        _gtpEngineProfiles.Add(new GtpEngineProfile());
        _gtpEngineAppCompatibilities.Add(new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY FORMAL APP"));
        ResetPositionHistory();
    }

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

    public string CgosBlackConnectionElapsedDisplay =>
        FormatCgosConnectionElapsedDisplay(_cgosBlackConnectionStartedAt, IsCgosBlackConnectionRunning);

    public string CgosBlackGtpResponseWaitDisplay { get; private set; } = "";

    public string CgosWhiteConnectionStatusMessage { get; private set; } = "READY";

    public string CgosWhiteConnectionLogDirectory { get; private set; } = "";

    public IReadOnlyList<string> CgosWhiteConnectionRecentOutput { get; private set; } = Array.Empty<string>();

    public bool IsCgosWhiteConnectionRunning { get; private set; }

    public bool IsCgosPlayer2InputEnabled { get; private set; }

    public string CgosWhiteConnectionElapsedDisplay =>
        FormatCgosConnectionElapsedDisplay(_cgosWhiteConnectionStartedAt, IsCgosWhiteConnectionRunning);

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

    public bool CanSendCgosAdminMatch =>
        IsCgosAdminRunning &&
        CgosAdminWaitingPlayers.Count >= 2 &&
        !CgosAdminWhitePlayerName.Equals(CgosAdminBlackPlayerName, StringComparison.OrdinalIgnoreCase);

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

    public int BoardSize { get; private set; } = 19;

    public IReadOnlyList<TournamentRules> TournamentRulesList => _tournamentRules;

    public CatalogOrderEditor<TournamentRules> TournamentRulesOrderEditor { get; } = new();

    public int SelectedTournamentRulesIndex { get; private set; }

    public bool IsTournamentRulesSelectionDialogOpen { get; private set; }

    public int TournamentRulesDialogSelectionIndex { get; private set; }

    public bool IsTournamentRulesAddPanelOpen { get; private set; }

    public bool IsTournamentRulesEditPanelMode { get; private set; }

    public bool IsTournamentRulesDeleteConfirmationOpen { get; private set; }

    public string TournamentRulesDeleteConfirmationFileName { get; private set; } = "";

    public int TournamentRulesSelectionPageIndex { get; private set; }

    public string TournamentRulesSaveMessage { get; private set; } = "";

    public string TournamentRulesDisplayNameDraft { get; private set; } = "";

    public bool IsTournamentRulesDisplayNameEditing { get; private set; }

    public int TournamentRulesDisplayNameCaretIndex { get; private set; }
    public int TournamentRulesDisplayNameSelectionStart { get; private set; }
    public int TournamentRulesDisplayNameSelectionLength { get; private set; }

    public string TournamentRulesDisplayNameWarning { get; private set; } = "";

    public TournamentRulesNumericField? ActiveTournamentRulesNumericField { get; private set; }

    public string TournamentRulesNumericDraft { get; private set; } = "";

    public int TournamentRulesNumericCaretIndex { get; private set; }
    public int TournamentRulesNumericSelectionStart { get; private set; }
    public int TournamentRulesNumericSelectionLength { get; private set; }

    public string TournamentDisplayName => _currentTournamentRules.DisplayName;

    public GoRuleKind RuleKind => _currentTournamentRules.Rule;

    public decimal Komi => _currentTournamentRules.Komi;

    public TimeSpan MainTime => _currentTournamentRules.MainTime;

    public int MoveLimit => _currentTournamentRules.MoveLimit;

    public TournamentRules CurrentTournamentRules => _currentTournamentRules.Clone();

    public IReadOnlyList<GtpEngineProfile> GtpEngineProfiles => _gtpEngineProfiles;

    public CatalogOrderEditor<GtpEngineProfile> GtpEngineOrderEditor { get; } = new();

    public int SelectedBlackGtpEngineIndex { get; private set; }

    public int SelectedWhiteGtpEngineIndex { get; private set; }

    public bool IsGtpEngineSelectionDialogOpen { get; private set; }

    public bool IsGtpEngineSelectionForCgos { get; private set; }

    public bool IsGtpEngineSelectionForAppProvider =>
        EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider;

    public GtpEngineSelectionPurpose EngineSelectionPurpose { get; private set; }

    public string GtpEngineSelectionAppId { get; private set; } = "play";

    public int GtpEngineDialogSelectionIndex { get; private set; }

    private int GtpEngineEditProfileIndex { get; set; } = -1;

    public GoStone GtpEngineSelectionTargetStone { get; private set; } = GoStone.Black;

    public bool IsGtpEngineDeleteConfirmationOpen { get; private set; }

    public string GtpEngineDeleteConfirmationName { get; private set; } = "";

    public bool IsGtpEngineEditPanelOpen { get; private set; }

    public bool IsGtpEngineAddPanelMode { get; private set; }

    public GtpEngineProfileEditField? ActiveGtpEngineEditField { get; private set; }

    public int GtpEngineEditCaretIndex { get; private set; }
    public int GtpEngineEditSelectionStart { get; private set; }
    public int GtpEngineEditSelectionLength { get; private set; }

    public string GtpEngineEditWarning { get; private set; } = "";

    public string GtpEngineEditSaveMessage { get; private set; } = "";

    public GtpEngineProfile GtpEngineEditDraft { get; private set; } = new();

    public bool IsGtpEngineGuiOptionsDialogOpen { get; private set; }

    public bool IsAppProviderGameSettingsDialogOpen { get; private set; }

    public Dictionary<string, string> GtpEngineGuiOptionsDialogDraft { get; private set; } = [];

    public bool IsGtpEngineRandomMoveSelectionDialogOpen { get; private set; }

    public int GtpEngineRandomMoveSelectionIndex { get; private set; }

    public GtpEngineGuiOptionSpec? ActiveGtpEngineComboOption { get; private set; }

    public int GtpEngineGuiOptionsPageIndex { get; private set; }
    private IReadOnlyList<GtpEngineGuiOptionSpec> _appProviderGameSettingSpecs = GtpEngineGuiOptions.PonnukiProviderSpecs;

    public IReadOnlyList<GtpEngineGuiOptionSpec> ActiveGtpEngineGuiOptionSpecs =>
        IsAppProviderGameSettingsDialogOpen ? _appProviderGameSettingSpecs : GtpEngineGuiOptions.Specs;

    public int GtpEngineRandomMoveSelectionPageIndex { get; private set; }

    public const int GtpEngineGuiOptionsPageSize = 4;

    public const int GtpEngineComboSelectionPageSize = 4;

    public int GtpEngineSelectionPageIndex { get; private set; }

    public GtpEngineProfile BlackGtpEngineProfile => GetGtpEngineProfile(GoStone.Black);

    public GtpEngineProfile WhiteGtpEngineProfile => GetGtpEngineProfile(GoStone.White);

    public void SetCgosConnectionProfiles(IEnumerable<CgosConnectionProfile> profiles)
    {
        _cgosConnectionProfiles.Clear();
        _cgosConnectionProfiles.AddRange(profiles);
        if (_cgosConnectionProfiles.Count == 0)
        {
            _cgosConnectionProfiles.Add(new CgosConnectionProfile("練習", "uec-go.com", 6809, "PRACTICE", "CGOS practice server"));
        }

        SelectedCgosConnectionProfileIndex = Math.Clamp(SelectedCgosConnectionProfileIndex, 0, _cgosConnectionProfiles.Count - 1);
        CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
    }

    public void OpenCgosConnectionEditPanel()
    {
        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = false;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = SelectedCgosConnectionProfile;
        CgosConnectionEditDraft = SelectedCgosConnectionProfile;
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void OpenCgosConnectionAddPanel()
    {
        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = true;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = CreateDefaultCgosConnectionProfile();
        CgosConnectionEditDraft = _cgosConnectionEditSource;
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void OpenCgosConnectionDuplicatePanel()
    {
        if (_cgosConnectionProfiles.Count == 0)
        {
            return;
        }

        IsCgosConnectionEditPanelOpen = true;
        IsCgosConnectionAddPanelMode = true;
        ActiveCgosConnectionEditField = null;
        _cgosConnectionEditSource = SelectedCgosConnectionProfile;
        CgosConnectionEditDraft = _cgosConnectionEditSource with
        {
            DisplayName = string.IsNullOrWhiteSpace(_cgosConnectionEditSource.DisplayName)
                ? "Unnamed CGOS Connection Copy"
                : $"{_cgosConnectionEditSource.DisplayName.Trim()} Copy",
        };
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditCaretIndex = 0;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void CloseCgosConnectionEditPanel()
    {
        IsCgosConnectionEditPanelOpen = false;
        IsCgosConnectionAddPanelMode = false;
        ActiveCgosConnectionEditField = null;
        CgosConnectionEditWarning = "";
        CgosConnectionEditSaveMessage = "";
    }

    public void BeginCgosConnectionEditField(CgosConnectionProfileEditField field, int caretIndex)
    {
        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, GetCgosConnectionEditFieldText(field).Length);
        CgosConnectionEditWarning = "";
    }

    public void EndCgosConnectionEditField()
    {
        ActiveCgosConnectionEditField = null;
    }

    public void SetCgosConnectionEditField(CgosConnectionProfileEditField field, string text, int caretIndex)
    {
        CgosConnectionEditDraft = field switch
        {
            CgosConnectionProfileEditField.DisplayName => CgosConnectionEditDraft with { DisplayName = text },
            CgosConnectionProfileEditField.Host => CgosConnectionEditDraft with { Host = text },
            CgosConnectionProfileEditField.Port => CgosConnectionEditDraft,
            CgosConnectionProfileEditField.Event => CgosConnectionEditDraft with { Event = text },
            CgosConnectionProfileEditField.Round => CgosConnectionEditDraft with { Round = text },
            CgosConnectionProfileEditField.Note => CgosConnectionEditDraft with { Note = text },
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "CGOS connection edit field is out of range."),
        };
        if (field == CgosConnectionProfileEditField.Port)
        {
            CgosConnectionPortDraft = text;
        }

        ActiveCgosConnectionEditField = field;
        CgosConnectionEditCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        CgosConnectionEditSaveMessage = "UNSAVED";
    }

    public void SetCgosConnectionEditWarning(string warning)
    {
        CgosConnectionEditWarning = warning;
    }

    public void SaveCgosConnectionEditDraft(CgosConnectionProfile profile)
    {
        if (IsCgosConnectionAddPanelMode)
        {
            _cgosConnectionProfiles.Add(profile);
            SelectedCgosConnectionProfileIndex = _cgosConnectionProfiles.Count - 1;
            CgosConnectionSelectionPageIndex = SelectedCgosConnectionProfileIndex / CgosConnectionSelectionPageSize;
            IsCgosConnectionAddPanelMode = false;
        }
        else
        {
            _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex] = profile;
        }

        CgosConnectionEditDraft = _cgosConnectionProfiles[SelectedCgosConnectionProfileIndex];
        CgosConnectionPortDraft = CgosConnectionEditDraft.Port.ToString();
        CgosConnectionEditSaveMessage = "SAVED";
        CgosConnectionEditWarning = "";
    }

    public void RemoveSelectedCgosConnectionProfile()
    {
        if (!CanDeleteSelectedCgosConnectionProfile)
        {
            return;
        }

        var removedIndex = SelectedCgosConnectionProfileIndex;
        var nextIndex = Math.Clamp(removedIndex, 0, _cgosConnectionProfiles.Count - 2);
        _cgosConnectionProfiles.RemoveAt(removedIndex);
        SelectedCgosConnectionProfileIndex = nextIndex;
        CgosConnectionSelectionPageIndex = Math.Clamp(
            nextIndex / CgosConnectionSelectionPageSize,
            0,
            Math.Max(0, GetCgosConnectionSelectionPageCount() - 1));
    }

    /// <summary>
    /// CGOSプレイヤー用の共通GTPエンジン選択ダイアログを開きます。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログを開きます。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログの編集内容を破棄します。
    /// </summary>
    /// <summary>
    /// GUIオプションダイアログの編集内容をエンジン設定へ反映します。
    /// </summary>
    /// <summary>次回対局用button予約を、使用したエンジンプロファイルから取り除きます。</summary>
    public bool ConsumeQueuedGtpEngineButtonsForComputerPlayers()
    {
        var consumed = false;
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
        {
            if (GetPlayerKind(stone) != GoPlayerKind.Computer) continue;
            var profile = GetGtpEngineProfile(stone);
            foreach (var option in GtpEngineGuiOptions.Specs.Where(option => option.Type == "button"))
            {
                consumed |= bool.TryParse(profile.GuiOptions.GetValueOrDefault(option.Id), out var queued) && queued;
                profile.GuiOptions[option.Id] = "false";
            }
        }

        return consumed;
    }

    public const int TournamentRulesSelectionPageSize = 6;

    public const int GtpEngineSelectionPageSize = 6;

    public const int CgosConnectionSelectionPageSize = 5;

    public const int CgosAdminPlayerSelectionPageSize = 6;

    private static CgosConnectionProfile CreateDefaultCgosConnectionProfile() =>
        new("New CGOS Connection", "uec-go.com", 6809, "PRACTICE", "CGOS practice server") { Event = "PRACTICE" };

    public void SetCgosConnectionEditSelection(int start, int length) =>
        (CgosConnectionEditSelectionStart, CgosConnectionEditSelectionLength) = (start, length);

    public void SetTournamentRulesNumericSelection(int start, int length) =>
        (TournamentRulesNumericSelectionStart, TournamentRulesNumericSelectionLength) = (start, length);

    public void SetTournamentRulesDisplayNameSelection(int start, int length) =>
        (TournamentRulesDisplayNameSelectionStart, TournamentRulesDisplayNameSelectionLength) = (start, length);

    public void SetGtpEngineEditSelection(int start, int length) =>
        (GtpEngineEditSelectionStart, GtpEngineEditSelectionLength) = (start, length);

    private static int AdjustGtpEngineSelectionAfterDelete(int selectedIndex, int removedIndex, int fallbackIndex)
    {
        if (selectedIndex == removedIndex)
        {
            return fallbackIndex;
        }

        return selectedIndex > removedIndex ? selectedIndex - 1 : selectedIndex;
    }

}
