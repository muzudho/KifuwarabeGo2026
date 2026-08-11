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
    private readonly List<GtpEngineProfile> _gtpEngineProfiles = new();
    private readonly List<GtpEngineAppCompatibility> _gtpEngineAppCompatibilities = new();

    public GoAppSession()
    {
        CurrentMode = _modes[GoAppModeKind.Resting];
        _board = new GoBoard(BoardSize);
        _gtpEngineProfiles.Add(new GtpEngineProfile());
        _gtpEngineAppCompatibilities.Add(new(GtpEngineAppCompatibilityKind.LegacyPlay, "LEGACY FORMAL APP"));
        ResetPositionHistory();
    }

    public int BoardSize { get; private set; } = 19;

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
    public const int TournamentRulesSelectionPageSize = 6;

    public const int GtpEngineSelectionPageSize = 6;

    public const int CgosConnectionSelectionPageSize = 5;

    public const int CgosAdminPlayerSelectionPageSize = 6;

    private static CgosConnectionProfile CreateDefaultCgosConnectionProfile() =>
        new("New CGOS Connection", "uec-go.com", 6809, "PRACTICE", "CGOS practice server") { Event = "PRACTICE" };

}
