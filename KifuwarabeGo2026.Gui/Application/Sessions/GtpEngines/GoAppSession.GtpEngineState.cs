namespace KifuwarabeGo2026.Gui.Application;

using System.Collections.Generic;
using System.Linq;
using KifuwarabeGo2026.Reference.PlaySpace.Go.GtpExtensions.Engines;
using KifuwarabeGo2026.Shared.Domain;

/// <summary>GTP エンジンのカタログ、選択、編集画面の状態を保持します。</summary>
public sealed partial class GoAppSession
{
    public const int GtpEngineSelectionPageSize = 6;

    private readonly List<GtpEngineProfile> _gtpEngineProfiles = new();
    private readonly List<GtpEngineAppCompatibility> _gtpEngineAppCompatibilities = new();

    public IReadOnlyList<GtpEngineProfile> GtpEngineProfiles => _gtpEngineProfiles;
    public CatalogOrderEditor<GtpEngineProfile> GtpEngineOrderEditor { get; } = new();
    public int SelectedBlackGtpEngineIndex { get; private set; }
    public int SelectedWhiteGtpEngineIndex { get; private set; }
    public bool IsGtpEngineSelectionDialogOpen { get; private set; }
    public bool IsGtpEngineSelectionForCgos { get; private set; }
    public bool IsGtpEngineSelectionForAppProvider => EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider;
    public bool IsGtpEngineManagement => EngineSelectionPurpose == GtpEngineSelectionPurpose.Management;
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
    public bool IsGtpEngineEditDirty { get; private set; }
    public GtpEngineProfile GtpEngineEditDraft { get; private set; } = new();
    public bool IsGtpEngineGuiOptionsDialogOpen { get; private set; }
    public bool IsAppProviderGameSettingsDialogOpen { get; private set; }
    public Dictionary<string, string> GtpEngineGuiOptionsDialogDraft { get; private set; } = [];
    private Dictionary<string, string> GtpEngineGuiOptionsDialogOriginalDraft { get; set; } = [];
    public bool IsGtpEngineGuiOptionsDialogDirty =>
        GtpEngineGuiOptionsDialogDraft.Count != GtpEngineGuiOptionsDialogOriginalDraft.Count ||
        GtpEngineGuiOptionsDialogDraft.Any(pair => !GtpEngineGuiOptionsDialogOriginalDraft.TryGetValue(pair.Key, out var value) || value != pair.Value);
    public bool IsGtpEngineRandomMoveSelectionDialogOpen { get; private set; }
    public int GtpEngineRandomMoveSelectionIndex { get; private set; }
    public GtpEngineGuiOptionSpec? ActiveGtpEngineComboOption { get; private set; }
    public int GtpEngineGuiOptionsPageIndex { get; private set; }
    private IReadOnlyList<GtpEngineGuiOptionSpec> _appProviderGameSettingSpecs = GtpEngineGuiOptions.PonnukiProviderSpecs;
    public IReadOnlyList<GtpEngineGuiOptionSpec> ActiveGtpEngineGuiOptionSpecs => IsAppProviderGameSettingsDialogOpen ? _appProviderGameSettingSpecs : GtpEngineGuiOptions.Specs;
    public int GtpEngineRandomMoveSelectionPageIndex { get; private set; }
    public const int GtpEngineGuiOptionsPageSize = 4;
    public const int GtpEngineComboSelectionPageSize = 4;
    public int GtpEngineSelectionPageIndex { get; private set; }
    public GtpEngineProfile BlackGtpEngineProfile => GetGtpEngineProfile(GoStone.Black);
    public GtpEngineProfile WhiteGtpEngineProfile => GetGtpEngineProfile(GoStone.White);
}
