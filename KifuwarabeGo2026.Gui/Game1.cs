namespace KifuwarabeGo2026.Gui;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Connect;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.GoApps.Casual.Ponnuki;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Application.Updates;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Infrastructure.FileSystem;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Infrastructure;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.BoardLens;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.SelectConnection;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Watch;
using KifuwarabeGo2026.Gui.Presentation.GoApps.Formal.LocalMatch.Interval.TournamentRules;
using KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Pages.BoardAndReview;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation.Shared.CgosMatchNotification;
using KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;
using KifuwarabeGo2026.Gui.Presentation.Shared.HeadUpDisplay;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.Shared.SavingOverlay;
using KifuwarabeGo2026.Gui.Presentation.Pages.ApplicationSettings;
using KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;
using KifuwarabeGo2026.Gui.Presentation.Pages.Title;
using KifuwarabeGo2026.Gui.Presentation.Title;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Intermission;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart.MoveCommentPanel;
using KifuwarabeGo2026.Gui.Presentation.Pages.MoveTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.PopupTrendChart;
using KifuwarabeGo2026.Gui.Presentation.Pages.Board;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;
using KifuwarabeGo2026.Gui.Presentation.Shared.TextAreaDialog;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.PopupNumberUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.SpinButton;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.MessageDialog;
using KifuwarabeGo2026.Gui.Sgf;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch.Play;
using KifuwarabeGo2026.Gui.Presentation.Shared.PlayersComponent;
using KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using KifuwarabeGo2026.Gui.Presentation.Shared.LiveBoardPreview;

public class Game1 : Game
{
    private const string ProductTitle = "Kifuwarabe Go 2026";
    private readonly GraphicsDeviceManager _graphics;
    private readonly IClipboardService _clipboardService;
    private readonly ITextCompositionService _textCompositionService;
    private readonly IFileDialogService _fileDialogService;
    private readonly IDesktopLauncher _desktopLauncher;
    private readonly ITextRasterizer _textRasterizer;
    private readonly IWindowIconService _windowIconService;
    private readonly IInitialWindowLayoutService _initialWindowLayoutService;
    private readonly IPlatformExecutableService _platformExecutableService;
    private readonly IWindowScreenshotService _windowScreenshotService;
    private readonly GoAppSession _session = new();
    private readonly TournamentRulesCatalog _tournamentRulesCatalog;
    private readonly GtpEngineCatalog _gtpEngineCatalog;
    private readonly EntryCatalog _playerCatalog;
    private readonly ClientIdentityCatalog _targetCatalog;
    private readonly CgosConnectionCatalog _cgosConnectionCatalog;
    private readonly TournamentRulesSetting _tournamentRulesSetting;
    private readonly PlayingScene _playingScene;
    private readonly CgosConnectionProcess _cgosBlackConnectionProcess;
    private readonly CgosConnectionProcess _cgosWhiteConnectionProcess;
    private readonly CgosConnectionProcess _cgosAdminProcess;
    private readonly CgosGameObservation _cgosGameObservation = new();
    private GoAppSession? _variationSession;
    private GoPresentationServices? _presentationServices;
    private SoundEffect? _placeStoneSound;
    private SoundEffectInstance? _placeStoneSoundInstance;
    private SoundEffect? _upcomingMatchChime;
    private SoundEffectInstance? _upcomingMatchChimeInstance;
    private SoundEffect? _screenshotShutterSound;
    private SoundEffectInstance? _screenshotShutterSoundInstance;
    private SoundEffect? _screenTransitionSound;
    private SoundEffectInstance? _screenTransitionSoundInstance;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private KeyboardState _previousScreenshotKeyboard;
    private KeyboardState _previousGtpEngineKeyboard;
    private readonly TextBoxController _gtpEngineEditTextBox = new(520);
    private readonly TextBoxController _gtpEngineIntegerOptionTextBox = new(11);
    private GtpEngineGuiOptionSpec? _activeGtpEngineIntegerOption;
    private GoStone? _activeLocalMatchRandomSeedStone;
    private KeyboardState _previousGtpEngineIntegerKeyboard;
    private string _gtpEngineIntegerInputMessage = "";
    private readonly TextBoxController _gtpEngineStringOptionTextBox = new(GtpEngineGuiOptions.MaximumTextLength);
    private GtpEngineGuiOptionSpec? _activeGtpEngineStringOption;
    private KeyboardState _previousGtpEngineStringKeyboard;
    private string _gtpEngineStringInputMessage = "";
    private TextCompositionState _gtpEngineStringComposition = TextCompositionState.Empty;
    private TextCompositionDiagnostics _textCompositionDiagnostics = TextCompositionDiagnostics.Empty;
    private readonly TextBoxController _commentTextArea = new(50_000);
    private string _commentEditorInitialText = "";
    private bool _isCommentEditorOpen;
    private int _commentEditorMoveIndex;
    private GoAppSession? _commentEditorSession;
    private KeyboardState _previousCommentEditorKeyboard;
    private TextCompositionState _commentEditorComposition = TextCompositionState.Empty;
    private string? _reviewSgfFilePath;
    private bool _isReviewUnsavedChangesConfirmationOpen;
    private ReviewExitAction? _pendingReviewExitAction;
    private readonly TextBoxController _humanPlayerNameTextBox = new(80);
    private KeyboardState _previousHumanPlayerNameKeyboard;
    private readonly TextBoxController _localMatchHandleTextBox = new(240);
    private KeyboardState _previousLocalMatchHandleKeyboard;
    private readonly TextBoxController _playerEditTextBox = new(240);
    private KeyboardState _previousPlayerEditKeyboard;
    private readonly TextBoxController _targetProfileEditTextBox = new(240);
    private KeyboardState _previousClientIdentityProfileEditKeyboard;
    private bool _receivedClientIdentityTextInput;
    private KeyboardState _previousCgosConnectionKeyboard;
    private readonly TextBoxController _cgosConnectionEditTextBox = new(240);
    private KeyboardState _previousCgosCredentialKeyboard;
    private readonly TextBoxController _cgosCredentialTextBox = new(240);
    private bool _isApplicationSettingsOpen;
    private ApplicationSettingsPage _applicationSettingsPage = ApplicationSettingsPage.Log;
    private TitleMenuPage _titleMenuPage = TitleMenuPage.Home;
    private int _appProviderTabIndex;
    private readonly List<string> _guiLogFiles = new();
    private int _selectedGuiLogIndex = -1;
    private string _applicationSettingsMessage = "";
    private string _lastScreenState = "Title";
    private bool _inputArmed;
    private CgosMatchNotificationMode _cgosMatchNotificationMode;
    private DateTimeOffset _cgosMatchNotificationStartedAt;
    private int _cgosMatchNotificationGameId;
    private double _inputClockSeconds;
    private double _screenshotEffectStartedAt = double.NegativeInfinity;
    private double _screenTransitionStartedAt = double.NegativeInfinity;
    private double _boardLensBannerStartedAt = double.NegativeInfinity;
    private bool _initialWindowLayoutPending = true;
    private WindowClientSize _lastLoggedWindowClientSize = new(-1, -1);
    private Point _lastLoggedWindowPosition = new(int.MinValue, int.MinValue);
    private Keys? _reviewRepeatKey;
    private double _reviewKeyboardNextRepeatAt;
    private int? _reviewMouseRepeatCommand;
    private double _reviewMouseNextRepeatAt;
    private bool _reviewPopupSeekDragging;
    private double _lastReviewPopupSeekClickAt = double.NegativeInfinity;
    private Point _lastReviewPopupSeekClickPoint;
    private int? _lastReadOnlyChartPopupSeekMoveIndex;
    private GoGameRecord? _lastAutoSavedLocalGameRecord;
    private int? _lastAutoSavedCgosGameId;
    private PonnukiProviderGameSession? _ponnukiProviderGameSession;
    private int _ponnukiProviderObservedMoveCount;
    private Task<GtpEngineAppCompatibility[]>? _appProviderSelectionLoadTask;
    private Task<GtpEngineAppCompatibility[]>? _gtpEngineSelectionLoadTask;
    private string _appProviderSelectionLoadAppId = "";
    private Task<(bool IsSupported, string Message)>? _restoredAppProviderCheckTask;
    private string _restoredAppProviderCheckPath = "";
    private Task<PonnukiPositionProvider.GameSettingsEvaluation>? _appProviderSettingsEvaluationTask;
    private int _appProviderSettingsEvaluationGeneration;
    private int _appProviderSettingsEvaluationTaskGeneration;
    private string _appProviderSettingsEvaluationPath = "";
    private Task? _catalogSaveTask;
    private string _catalogSaveMessage = "";
    private Task<GuiReleaseUpdateResult>? _guiReleaseUpdateTask;
    private MessageDialog? _messageDialog;

    private const double CgosMatchCountdownSeconds = 10d;
    private const double CgosMatchFadeSeconds = 1.2d;
    private const double CgosMatchButtonDelaySeconds = 0.30d;
    private const double ReviewRepeatInitialDelaySeconds = 0.42d;
    private const double ReviewRepeatIntervalSeconds = 0.075d;
    private const double ReviewPopupDoubleClickSeconds = 0.36d;
    private const int ReviewPopupDoubleClickDistance = 18;
    private const double ScreenshotEffectDurationSeconds = 0.42d;
    private const double ScreenTransitionDurationSeconds = 1.5d;
    private const double BoardLensBannerDurationSeconds = 2.2d;
    private const double BoardLensBannerCompactStartSeconds = 1.35d;
    private const double BoardLensBannerCompactDurationSeconds = 0.55d;

    private bool IsScreenTransitionActive =>
        _inputClockSeconds - _screenTransitionStartedAt is >= 0d and < ScreenTransitionDurationSeconds;

    public Game1(
        IClipboardService clipboardService,
        ITextCompositionService textCompositionService,
        IFileDialogService fileDialogService,
        IDesktopLauncher desktopLauncher,
        ITextRasterizer textRasterizer,
        IWindowIconService windowIconService,
        IInitialWindowLayoutService initialWindowLayoutService,
        IPlatformExecutableService platformExecutableService,
        IWindowScreenshotService windowScreenshotService)
    {
        _clipboardService = clipboardService;
        _textCompositionService = textCompositionService;
        _textCompositionService.CompositionChanged += OnTextCompositionChanged;
        _textCompositionService.DiagnosticsChanged += OnTextCompositionDiagnosticsChanged;
        _fileDialogService = fileDialogService;
        _desktopLauncher = desktopLauncher;
        _textRasterizer = textRasterizer;
        _windowIconService = windowIconService;
        _initialWindowLayoutService = initialWindowLayoutService;
        _platformExecutableService = platformExecutableService;
        _windowScreenshotService = windowScreenshotService;
        _cgosBlackConnectionProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "BlackPlayer");
        _cgosWhiteConnectionProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "WhitePlayer");
        _cgosAdminProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "Admin");
        _tournamentRulesCatalog = TournamentRulesCatalog.LoadFromDefaultLocation();
        _gtpEngineCatalog = GtpEngineCatalog.LoadFromDefaultLocation();
        _cgosConnectionCatalog = CgosConnectionCatalog.LoadFromDefaultLocation();
        _playerCatalog = EntryCatalog.LoadFromDefaultLocation(_gtpEngineCatalog.Profiles);
        _targetCatalog = ClientIdentityCatalog.LoadFromDefaultLocation(
            _playerCatalog.Profiles,
            _gtpEngineCatalog.Profiles,
            _cgosConnectionCatalog.Profiles);
        if (_targetCatalog.EntryProfilesChanged)
            _playerCatalog.Save(_targetCatalog.EntryProfiles);
        _session.SetTournamentRules(_tournamentRulesCatalog.Rules);
        _session.SetGtpEngineProfiles(_gtpEngineCatalog.Profiles);
        _session.SetEntryProfiles(_targetCatalog.EntryProfiles);
        _session.SetClientIdentityProfiles(_targetCatalog.Profiles);
        ApplicationSettings.Current.LastSelectedAppProviderEnginePaths.TryGetValue("ponnuki", out var lastPonnukiProviderPath);
        if (_session.RestoreAppProviderEngine(lastPonnukiProviderPath) && _session.CanUseSelectedAppProvider)
        {
            _session.SetAppProviderCapability(false, "CHECKING PROVIDER...");
            _restoredAppProviderCheckPath = _session.SelectedAppProviderEngine.ExecutablePath;
            _restoredAppProviderCheckTask = PonnukiPositionProvider.CheckCapabilityAsync(_session.SelectedAppProviderEngine);
        }
        _session.SetCgosConnectionProfiles(_cgosConnectionCatalog.Profiles);
        RefreshSgfAutoSaveState();
        _tournamentRulesSetting = new TournamentRulesSetting(
            _session,
            _tournamentRulesCatalog,
            OpenTournamentRulesSelectionDialog,
            BeginDiscardTransition,
            _clipboardService,
            point => TournamentRulesPresenter.Default.IsSettingsFileHit(point),
            OpenTournamentRulesSettingsFile);
        _playingScene = new PlayingScene(
            _session,
            PlayPlaceStoneSound,
            () => _gtpEngineCatalog.Save(_session.GtpEngineProfiles),
            OpenGtpLog);

        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = VirtualScreen.Width;
        _graphics.PreferredBackBufferHeight = VirtualScreen.Height;
        _graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = CreateWindowTitle();
        Window.AllowUserResizing = true;
        Window.TextInput += OnTextInput;
        Window.ClientSizeChanged += OnWindowClientSizeChanged;
        Deactivated += OnGameDeactivated;
        RefreshGuiLogFiles();
    }

    private static string CreateWindowTitle()
    {
        var version = typeof(Game1).Assembly.GetName().Version;
        return version is null
            ? ProductTitle
            : $"{ProductTitle} | v{version.Major}.{version.Minor}.{version.Build}";
    }

    protected override void LoadContent()
    {
        _windowIconService.TryApply(Window.Handle);
        _presentationServices = GoPresentationFactory.Create(GraphicsDevice, Content, _textRasterizer);
        _placeStoneSound = CreatePlaceStoneSound();
        _placeStoneSoundInstance = _placeStoneSound.CreateInstance();
        _upcomingMatchChime = CreateUpcomingMatchChime();
        _upcomingMatchChimeInstance = _upcomingMatchChime.CreateInstance();
        _screenshotShutterSound = CreateScreenshotShutterSound();
        _screenshotShutterSoundInstance = _screenshotShutterSound.CreateInstance();
        _screenTransitionSound = CreateScreenTransitionSound();
        _screenTransitionSoundInstance = _screenTransitionSound.CreateInstance();
    }

    protected override void Update(GameTime gameTime)
    {
        _textCompositionService.Update();
        ApplyInitialWindowLayout();
        LogWindowPositionChange();
        _inputClockSeconds = gameTime.TotalGameTime.TotalSeconds;
        CompleteAppProviderSelectionLoading();
        CompleteGuiReleaseUpdate();
        CompleteGtpEngineSelectionLoading();
        CompleteRestoredAppProviderCheck();
        CompleteAppProviderSettingsEvaluation();
        CompleteCatalogSave();
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        SynchronizeOrArmWindowInput(keyboard, mouse);
        var acceptsInput = IsActive && _inputArmed;
        LogAutomaticScreenTransition();
        if (IsScreenTransitionActive)
            acceptsInput = false;
        if (acceptsInput && GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
        }

        if (acceptsInput)
            UpdateScreenshotKeyboardInput(keyboard);

        if (acceptsInput && _activeGtpEngineIntegerOption is not null)
        {
            UpdateGtpEngineIntegerInputKeyboard(keyboard, gameTime);
            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        if (acceptsInput && _activeGtpEngineStringOption is not null)
        {
            UpdateGtpEngineStringInputKeyboard(keyboard, gameTime);
            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        if (acceptsInput && _isCommentEditorOpen)
        {
            UpdateCommentEditorKeyboard(keyboard, gameTime);
            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        if (acceptsInput && _isReviewUnsavedChangesConfirmationOpen)
        {
            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        if (_session.UseKind is null)
        {
            UpdateAppProviderSelectionKeyboard(keyboard);
            UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
        }

        if (_session.UseKind is not (GoAppUseKind.LocalPlay or GoAppUseKind.LocalApps))
        {
            if (_session.UseKind == GoAppUseKind.CgosClient)
            {
                UpdateCgosConnectionProcessStatus();
                UpdateCgosAdminProcessStatus();
                UpdateCgosGameObservation();
                UpdateCgosMatchNotification();

                if (_variationSession is not null || _session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                    UpdateGlobalKeyboardInput(keyboard);
                else
                    // ［CGOS　＞　観戦画面］キーボード入力
                    UpdateCgosWatchingKeyboardInput(keyboard);

                UpdateCgosConnectionEditPanelByKeyboard(keyboard, gameTime);
                UpdateCgosCredentialByKeyboard(keyboard, gameTime);
                UpdateClientIdentityProfileEditTextBox(keyboard, gameTime);
                UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
            }

            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        _playingScene.Update();
        UpdatePonnukiProviderGame();
        _session.AddCurrentTurnElapsedTime(gameTime.ElapsedGameTime);
        UpdateGlobalKeyboardInput(keyboard);
        UpdatePlayerEditTextBox(keyboard, gameTime);
        UpdateClientIdentityProfileEditTextBox(keyboard, gameTime);
        UpdateHumanPlayerNameTextBox(keyboard, gameTime);
        UpdateLocalMatchHandleTextBox(keyboard, gameTime);

        if (acceptsInput && _session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
            _tournamentRulesSetting.UpdateByKeyboard(keyboard, gameTime);
        }
        UpdateMouseInput();
        TryAutoSaveCompletedLocalGame();

        base.Update(gameTime);
    }

    private void ApplyInitialWindowLayout()
    {
        if (!_initialWindowLayoutPending)
            return;

        var preferredSize = new WindowClientSize(VirtualScreen.Width, VirtualScreen.Height);
        if (!_initialWindowLayoutService.TryGetInitialClientSize(Window.Handle, preferredSize, out var initialSize))
            return;

        _initialWindowLayoutPending = false;
        GuiOperationLog.App(
            "Initial window layout",
            $"preferred={preferredSize.Width}x{preferredSize.Height}; selected={initialSize.Width}x{initialSize.Height}");
        if (initialSize == preferredSize)
            return;

        _graphics.PreferredBackBufferWidth = initialSize.Width;
        _graphics.PreferredBackBufferHeight = initialSize.Height;
        _graphics.ApplyChanges();
        _initialWindowLayoutService.CenterWindowInWorkingArea(Window.Handle);
    }

    private void OnWindowClientSizeChanged(object? sender, EventArgs e)
    {
        var clientBounds = Window.ClientBounds;
        var clientSize = new WindowClientSize(clientBounds.Width, clientBounds.Height);
        if (clientSize == _lastLoggedWindowClientSize)
            return;

        _lastLoggedWindowClientSize = clientSize;
        GuiOperationLog.App("Window client size changed", $"client={clientSize.Width}x{clientSize.Height}");
    }

    private void LogWindowPositionChange()
    {
        var position = Window.Position;
        if (position == _lastLoggedWindowPosition)
            return;

        _lastLoggedWindowPosition = position;
        GuiOperationLog.App("Window position changed", $"position={position.X},{position.Y}");
    }

    private void OnGameDeactivated(object? sender, EventArgs e)
    {
        _inputArmed = false;
    }

    private void SynchronizeOrArmWindowInput(KeyboardState keyboard, MouseState mouse)
    {
        if (!IsActive || !_inputArmed)
        {
            _previousMouse = mouse;
            _previousKeyboard = keyboard;
            _previousScreenshotKeyboard = keyboard;
            _previousGtpEngineIntegerKeyboard = keyboard;
            _previousGtpEngineKeyboard = keyboard;
            _previousHumanPlayerNameKeyboard = keyboard;
            _previousLocalMatchHandleKeyboard = keyboard;
            _previousCgosConnectionKeyboard = keyboard;
            _previousCgosCredentialKeyboard = keyboard;
            _tournamentRulesSetting.SynchronizeKeyboardState(keyboard);
        }

        if (IsActive && !_inputArmed &&
            keyboard.GetPressedKeyCount() == 0 &&
            mouse.LeftButton == ButtonState.Released &&
            mouse.MiddleButton == ButtonState.Released &&
            mouse.RightButton == ButtonState.Released)
        {
            _inputArmed = true;
        }
    }

    private void UpdateGlobalKeyboardInput(KeyboardState keyboard)
    {
        if (!IsActive || !_inputArmed) return;

        if (_playingScene.IsInitialPositionConciergeVisible)
        {
            if (IsNewGlobalKeyPress(keyboard, Keys.Escape))
                _playingScene.CancelInitialPositionConcierge();
            else if (IsNewGlobalKeyPress(keyboard, Keys.Up) || IsNewGlobalKeyPress(keyboard, Keys.Left))
                _playingScene.SelectPreviousInitialPositionEngine();
            else if (IsNewGlobalKeyPress(keyboard, Keys.Down) || IsNewGlobalKeyPress(keyboard, Keys.Right))
                _playingScene.SelectNextInitialPositionEngine();
            else if (IsNewGlobalKeyPress(keyboard, Keys.Space))
                _playingScene.TryAnotherSelectedInitialPositionMethod();
            else if (IsNewGlobalKeyPress(keyboard, Keys.Enter))
                _playingScene.ContinueSelectedInitialPositionMethod();
            else if (IsNewGlobalKeyPress(keyboard, Keys.L))
                _playingScene.OpenInitialPositionLog();

            _previousKeyboard = keyboard;
            return;
        }

        if (_session.IsReviewChartPopupOpen &&
            _session.CurrentMode.Kind != GoAppModeKind.Reviewing &&
            (IsNewGlobalKeyPress(keyboard, Keys.Enter) || IsNewGlobalKeyPress(keyboard, Keys.Escape)))
        {
            _session.CloseReviewChartPopup();
            ResetReadOnlyChartPopupDoubleClick();
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.CurrentMode.Kind != GoAppModeKind.Reviewing &&
            TryHandleReadOnlyChartKeyboardInput(keyboard))
        {
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.CurrentMode.Kind is GoAppModeKind.Reviewing or GoAppModeKind.GameOver && TryHandleReviewKeyboardInput(keyboard))
        {
            _previousKeyboard = keyboard;
            return;
        }
        if (_session.CurrentMode.Kind != GoAppModeKind.Reviewing)
        {
            _reviewRepeatKey = null;
        }

        if (_session.CurrentMode.Kind == GoAppModeKind.BoardEditing)
        {
            var isControlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
            if (isControlDown && IsNewGlobalKeyPress(keyboard, Keys.Z))
            {
                _session.UndoBoardEditing();
                _previousKeyboard = keyboard;
                return;
            }

            if (isControlDown && IsNewGlobalKeyPress(keyboard, Keys.Y))
            {
                _session.RedoBoardEditing();
                _previousKeyboard = keyboard;
                return;
            }
        }

        var canHandleBoardLens = CanHandleBoardLensShortcut(_session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.Reviewing);
        if (canHandleBoardLens && keyboard.IsKeyDown(Keys.L) && _previousKeyboard.IsKeyUp(Keys.L))
        {
            ToggleBoardLens();
        }
        else if (canHandleBoardLens && IsNewBoardLensNextKeyPress(keyboard))
        {
            TryStepBoardLens(1);
        }
        else if (canHandleBoardLens && IsNewBoardLensPreviousKeyPress(keyboard))
        {
            TryStepBoardLens(-1);
        }
        else if (canHandleBoardLens && IsNewBoardLensExitKeyPress(keyboard))
        {
            TryDeactivateBoardLens();
        }

        _previousKeyboard = keyboard;
    }

    /// <summary>
    /// CGOS 観戦・結果画面で、通信を伴わないローカル表示操作を処理します。
    /// </summary>
    private void UpdateCgosWatchingKeyboardInput(KeyboardState keyboard)
    {
        if (!IsActive || !_inputArmed) return;

        if (_session.IsReviewChartPopupOpen &&
            (IsNewGlobalKeyPress(keyboard, Keys.Enter) || IsNewGlobalKeyPress(keyboard, Keys.Escape)))
        {
            _session.CloseReviewChartPopup();
            ResetReadOnlyChartPopupDoubleClick();
            _previousKeyboard = keyboard;
            return;
        }

        var canToggle = _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result;
        var canHandleBoardLens = CanHandleBoardLensShortcut(canToggle);
        if (canHandleBoardLens && keyboard.IsKeyDown(Keys.L) && _previousKeyboard.IsKeyUp(Keys.L))
            ToggleBoardLens();
        else if (canHandleBoardLens && IsNewBoardLensNextKeyPress(keyboard))
            TryStepBoardLens(1);
        else if (canHandleBoardLens && IsNewBoardLensPreviousKeyPress(keyboard))
            TryStepBoardLens(-1);
        else if (canHandleBoardLens && IsNewBoardLensExitKeyPress(keyboard))
            TryDeactivateBoardLens();

        _previousKeyboard = keyboard;
    }

    private bool IsNewGlobalKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    private bool IsNewBoardLensExitKeyPress(KeyboardState keyboard) =>
        IsNewGlobalKeyPress(keyboard, Keys.D1) || IsNewGlobalKeyPress(keyboard, Keys.NumPad1);

    private bool IsNewBoardLensNextKeyPress(KeyboardState keyboard) =>
        IsNewGlobalKeyPress(keyboard, Keys.K);

    private bool IsNewBoardLensPreviousKeyPress(KeyboardState keyboard) =>
        IsNewGlobalKeyPress(keyboard, Keys.J);

    private bool TryHandleReviewKeyboardInput(KeyboardState keyboard)
    {
        if (_session.IsReviewChartPopupOpen &&
            (IsNewGlobalKeyPress(keyboard, Keys.Enter) || IsNewGlobalKeyPress(keyboard, Keys.Escape)))
        {
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            return true;
        }

        var command = GetReviewKeyboardCommand(keyboard);
        if (command is null)
        {
            _reviewRepeatKey = null;
            return false;
        }

        var (key, navigation) = command.Value;
        if (_reviewRepeatKey != key)
        {
            _reviewRepeatKey = key;
            _reviewKeyboardNextRepeatAt = _inputClockSeconds + ReviewRepeatInitialDelaySeconds;
            ExecuteReviewNavigation(navigation);
            return true;
        }

        if (_inputClockSeconds >= _reviewKeyboardNextRepeatAt)
        {
            _reviewKeyboardNextRepeatAt = _inputClockSeconds + ReviewRepeatIntervalSeconds;
            ExecuteReviewNavigation(navigation);
            return true;
        }

        return false;
    }

    private bool TryHandleReadOnlyChartKeyboardInput(KeyboardState keyboard)
    {
        var navigationVisible =
            _session.IsReviewChartPopupOpen ||
            (IsLocalPlayUseKind() && _session.IsLocalReplayMode) ||
            (_session.UseKind == GoAppUseKind.CgosClient && _cgosGameObservation.IsReplayMode);
        if (!navigationVisible || !TryGetReadOnlyChartNavigation(out var currentMoveIndex, out var maximumMoveIndex))
        {
            _reviewRepeatKey = null;
            return false;
        }

        var command = GetReviewKeyboardCommand(keyboard);
        if (command is null)
        {
            _reviewRepeatKey = null;
            return false;
        }

        var (key, navigation) = command.Value;
        if (_reviewRepeatKey == key && _inputClockSeconds < _reviewKeyboardNextRepeatAt)
            return true;

        _reviewKeyboardNextRepeatAt = _inputClockSeconds +
            (_reviewRepeatKey == key ? ReviewRepeatIntervalSeconds : ReviewRepeatInitialDelaySeconds);
        _reviewRepeatKey = key;
        var targetMoveIndex = navigation switch
        {
            int.MinValue => 0,
            int.MaxValue => maximumMoveIndex,
            _ => Math.Clamp(currentMoveIndex + navigation, 0, maximumMoveIndex),
        };
        SeekReadOnlyChartPopup(targetMoveIndex);
        return true;
    }

    private static (Keys Key, int Navigation)? GetReviewKeyboardCommand(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.Home)) return (Keys.Home, int.MinValue);
        if (keyboard.IsKeyDown(Keys.End)) return (Keys.End, int.MaxValue);
        if (keyboard.IsKeyDown(Keys.Left)) return (Keys.Left, -1);
        if (keyboard.IsKeyDown(Keys.Right)) return (Keys.Right, 1);
        if (keyboard.IsKeyDown(Keys.Down)) return (Keys.Down, -10);
        if (keyboard.IsKeyDown(Keys.Up)) return (Keys.Up, 10);
        if (keyboard.IsKeyDown(Keys.PageDown)) return (Keys.PageDown, -50);
        if (keyboard.IsKeyDown(Keys.PageUp)) return (Keys.PageUp, 50);
        return null;
    }

    private void ExecuteReviewNavigation(int navigation)
    {
        if (_session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            var target = navigation switch
            {
                int.MinValue => 0,
                int.MaxValue => _session.LocalReviewTimelineMaximum,
                _ => Math.Clamp(_session.LocalReviewTimelineIndex + navigation, 0, _session.LocalReviewTimelineMaximum),
            };
            _session.SeekLocalReviewTimeline(target);
            return;
        }

        if (navigation == int.MinValue)
        {
            MoveReview(-_session.ReviewTimelineIndex);
        }
        else if (navigation == int.MaxValue)
        {
            MoveReview(_session.ReviewTimelineMaximum - _session.ReviewTimelineIndex);
        }
        else
        {
            MoveReview(navigation);
        }
    }

    /// <summary>
    /// Board Lens のショートカットを盤面を扱う画面だけへ限定する。
    /// テキスト入力中は、文字キーを UI の入力戦略へ完全に委譲する。
    /// </summary>
    private bool CanHandleBoardLensShortcut(bool isBoardInteractionScreen) =>
        isBoardInteractionScreen &&
        !_session.IsPlayerSelectionDialogOpen &&
        !_session.IsPlayerEditPanelOpen &&
        !_session.IsClientIdentityProfileSelectionPanelOpen &&
        !_session.IsClientIdentityProfileEditPanelOpen &&
        !_session.IsGtpEngineEditPanelOpen &&
        !_session.IsCgosConnectionEditPanelOpen &&
        _session.ActiveGtpEngineEditField is null &&
        _session.ActivePlayerEditField is null &&
        _session.ActiveClientIdentityProfileEditField is null &&
        _session.ActiveCgosConnectionEditField is null &&
        _session.ActiveCgosCredentialStone is null &&
        _session.ActiveHumanPlayerNameStone is null &&
        _session.ActiveLocalMatchHandleStone is null &&
        !_session.IsTournamentRulesDisplayNameEditing &&
        _session.ActiveTournamentRulesNumericField is null;

    private void ToggleBoardLens()
    {
        _session.ToggleRenParseDisplay();
        _boardLensBannerStartedAt = _inputClockSeconds;
    }

    private void TryDeactivateBoardLens()
    {
        if (_session.TryDeactivateBoardLens())
            _boardLensBannerStartedAt = _inputClockSeconds;
    }

    private void TrySwitchBoardLensFamily()
    {
        if (_session.TrySwitchBoardLensFamily())
            _boardLensBannerStartedAt = _inputClockSeconds;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));
        HeadUpDisplayComponent.Default.StickyNoteScreen = GetStickyNoteScreen();
        var backgroundMousePosition = _activeGtpEngineIntegerOption is not null || _activeGtpEngineStringOption is not null
            ? new Point(-1, -1)
            : Mouse.GetState().Position;
        if (_session.UseKind is null)
        {
            if (_presentationServices is not null)
            {
                if (_isApplicationSettingsOpen)
                    ApplicationSettingsScreen.Default.Draw(_presentationServices.Stationery, backgroundMousePosition, _applicationSettingsPage, ApplicationSettings.Current.LogRootDirectory, ApplicationSettings.Current.SgfSaveDirectory, ApplicationSettings.Current.ScreenshotSaveDirectory, ApplicationSettings.FilePath, _gtpEngineCatalog.ListPath, _guiLogFiles, _selectedGuiLogIndex, _applicationSettingsMessage);
                else
                    _presentationServices.Presentation.DrawTitle(_session, backgroundMousePosition, _titleMenuPage,
                        _appProviderTabIndex, _appProviderSelectionLoadTask is not null);
            }
        }
        else if (_variationSession is not null)
        {
            if (_presentationServices is not null)
            {
                _presentationServices.Presentation.Draw(
                    _variationSession,
                    backgroundMousePosition,
                    CreateLiveBoardPreview());
            }
        }
        else if (_session.UseKind == GoAppUseKind.CgosClient)
        {
            if (_presentationServices is not null)
            {
                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                {
                _presentationServices.Presentation.Draw(_session, backgroundMousePosition);
                }
                else if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
                {
                _presentationServices.Presentation.DrawCgosWatch(_session, _cgosGameObservation, backgroundMousePosition);
                }
                else if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
                {
                _presentationServices.Presentation.DrawCgosLogin(_session, backgroundMousePosition);
                }
                else
                {
                _presentationServices.Presentation.DrawCgosConnectionSelection(_session, backgroundMousePosition);
                }
            }
        }
        else
        {
            if (_presentationServices is not null)
            {
                _presentationServices.Presentation.Draw(
                    _session,
                    backgroundMousePosition,
                    initialPositionConcierge: _playingScene.InitialPositionConciergeView);
            }
        }

        if (_presentationServices is not null &&
            _session.UseKind == GoAppUseKind.CgosClient &&
            _cgosMatchNotificationMode != CgosMatchNotificationMode.None)
        {
            var notificationAge = GetCgosMatchNotificationAge();
            var notificationOpacity =
                (float)Math.Clamp(notificationAge.TotalSeconds / CgosMatchFadeSeconds, 0d, 1d);
            var buttonsEnabled =
                notificationAge.TotalSeconds >= CgosMatchButtonDelaySeconds;
            CgosMatchNotification.Default.Draw(
                _presentationServices.Stationery,
                Mouse.GetState().Position,
                _cgosMatchNotificationMode == CgosMatchNotificationMode.Deferred,
                _cgosGameObservation.IsFinished,
                GetCgosMatchSecondsRemaining(notificationAge),
                notificationOpacity,
                notificationOpacity,
                buttonsEnabled,
                _session.CgosConnectionFlowKind != CgosConnectionFlowKind.Watching);
        }

        if (_presentationServices is not null)
        {
            var boardLensBannerAge = _inputClockSeconds - _boardLensBannerStartedAt;
            if (boardLensBannerAge >= 0d && _session.IsRenParseDisplayEnabled)
            {
                var opacity = Math.Clamp(boardLensBannerAge / 0.12d, 0d, 1d);
                var compactProgress = Math.Clamp(
                    (boardLensBannerAge - BoardLensBannerCompactStartSeconds) /
                    BoardLensBannerCompactDurationSeconds,
                    0d,
                    1d);
                BoardLensBanner.Draw(_presentationServices.Stationery,
                    _session.BoardLensDisplayName,
                    _session.BoardLensAlias,
                    _session.BoardLensGuide,
                    (float)opacity,
                    (float)compactProgress);
            }
            else if (boardLensBannerAge >= 0d && boardLensBannerAge < BoardLensBannerDurationSeconds)
            {
                var opacity = Math.Clamp(
                    (BoardLensBannerDurationSeconds - boardLensBannerAge) / 0.35d,
                    0d,
                    1d);
                BoardLensBanner.Draw(_presentationServices.Stationery,
                    _session.BoardLensDisplayName,
                    _session.BoardLensAlias,
                    _session.BoardLensGuide,
                    (float)opacity,
                    0f);
            }

            var screenshotEffectAge = _inputClockSeconds - _screenshotEffectStartedAt;
            if (screenshotEffectAge >= 0d && screenshotEffectAge < ScreenshotEffectDurationSeconds)
                HeadUpDisplayComponent.Default.ScreenshotEffect.Draw(_presentationServices.Stationery, (float)(screenshotEffectAge / ScreenshotEffectDurationSeconds));

            var screenTransitionAge = _inputClockSeconds - _screenTransitionStartedAt;
            if (screenTransitionAge >= 0d && screenTransitionAge < ScreenTransitionDurationSeconds)
                HeadUpDisplayComponent.Default.ScreenTransition.Draw(_presentationServices.Stationery, (float)(screenTransitionAge / ScreenTransitionDurationSeconds));
        }

        if (_presentationServices is not null && _activeGtpEngineIntegerOption is { } integerOption)
            HeadUpDisplayComponent.Default.PopupNumberUnderline.Draw(_presentationServices.Stationery,
                backgroundMousePosition,
                integerOption.Label,
                _gtpEngineIntegerOptionTextBox.Text,
                _gtpEngineIntegerOptionTextBox.CaretIndex,
                _gtpEngineIntegerOptionTextBox.SelectionStart,
                _gtpEngineIntegerOptionTextBox.SelectionLength,
                _gtpEngineIntegerInputMessage,
                new PopupNumberUnderlineOptions(
                    Caption: _activeLocalMatchRandomSeedStone is null ? null : "RANDOM SEED INPUT",
                    ShowTitle: _activeLocalMatchRandomSeedStone is null,
                    AllowEmpty: _activeLocalMatchRandomSeedStone is not null));

        // ［大会ルール設定　＞　コミ］
        if (_presentationServices is not null && _tournamentRulesSetting.IsMoveLimitInputOpen)
            HeadUpDisplayComponent.Default.PopupNumberUnderline.Draw(_presentationServices.Stationery,
                backgroundMousePosition,
                "MOVES",
                _tournamentRulesSetting.MoveLimitInputText,
                _tournamentRulesSetting.MoveLimitInputCaretIndex,
                _tournamentRulesSetting.MoveLimitInputSelectionStart,
                _tournamentRulesSetting.MoveLimitInputSelectionLength,
                _tournamentRulesSetting.MoveLimitInputMessage,
                new PopupNumberUnderlineOptions(true, Caption: "MOVES INPUT", ShowTitle: false,
                    SpinButtons:
                    [
                        new SpinButton(new Rectangle(700, 516, 82, 100), "100"),
                        new SpinButton(new Rectangle(812, 516, 82, 100), "10"),
                        new SpinButton(new Rectangle(924, 516, 82, 100), "1"),
                    ]));

        if (_presentationServices is not null && _tournamentRulesSetting.IsTimeInputOpen)
            HeadUpDisplayComponent.Default.PopupTimeUnderline.Draw(_presentationServices.Stationery,
                backgroundMousePosition,
                _tournamentRulesSetting.TimeInputTexts,
                _tournamentRulesSetting.TimeInputCaretIndices,
                _tournamentRulesSetting.ActiveTimeInputPart,
                _tournamentRulesSetting.TimeInputMessage);

        if (_presentationServices is not null && _tournamentRulesSetting.IsKomiInputOpen)
            HeadUpDisplayComponent.Default.PopupNumberUnderline.Draw(_presentationServices.Stationery,
                backgroundMousePosition,
                "KOMI",
                _tournamentRulesSetting.KomiInputText,
                _tournamentRulesSetting.KomiInputCaretIndex,
                _tournamentRulesSetting.KomiInputSelectionStart,
                _tournamentRulesSetting.KomiInputSelectionLength,
                _tournamentRulesSetting.KomiInputMessage,
                new PopupNumberUnderlineOptions(true, "0.5", "KOMI INPUT", ShowTitle: false,
                    SpinButtons: [new SpinButton(new Rectangle(700, 516, 82, 100), "0.5")]));

        if (_presentationServices is not null && _activeGtpEngineStringOption is { } stringOption)
            HeadUpDisplayComponent.Default.TextInputDialog.Draw(_presentationServices.Stationery,
                Mouse.GetState().Position,
                stringOption.Label,
                _gtpEngineStringOptionTextBox.Text,
                _gtpEngineStringOptionTextBox.CaretIndex,
                _gtpEngineStringOptionTextBox.SelectionStart,
                _gtpEngineStringOptionTextBox.SelectionLength,
                _gtpEngineStringInputMessage,
                showDefaultButton: true,
                composition: _gtpEngineStringComposition,
                compositionDiagnostics: _textCompositionDiagnostics,
                showCompositionDiagnostics: _textCompositionService.SupportsDiagnosticAdornment);

        if (_presentationServices is not null && _isCommentEditorOpen)
            TextAreaDialog.Default.Draw(_presentationServices.Stationery,
                Mouse.GetState().Position,
                _commentEditorMoveIndex == 0 ? "ROOT COMMENT (INITIAL POSITION)" : $"MOVE {_commentEditorMoveIndex} COMMENT",
                _commentTextArea.Text,
                _commentTextArea.CaretIndex,
                _commentTextArea.SelectionStart,
                _commentTextArea.SelectionLength,
                "COMMENT IS SAVED AS STANDARD SGF C[] TEXT.",
                _commentTextArea.Text != _commentEditorInitialText,
                _commentEditorComposition,
                _textCompositionDiagnostics,
                _textCompositionService.SupportsDiagnosticAdornment);

        if (_presentationServices is not null && _isReviewUnsavedChangesConfirmationOpen)
            HeadUpDisplayComponent.Default.ReviewUnsavedChangesConfirmation.Draw(_presentationServices.Stationery, Mouse.GetState().Position);

        if (_presentationServices is not null && _messageDialog is not null)
            _messageDialog.Draw(_presentationServices.Stationery, Mouse.GetState().Position);

        if (_presentationServices is not null && IsCatalogSaveInProgress)
            SavingOverlay.Default.Draw(_presentationServices.Stationery, _catalogSaveMessage);

        var virtualMousePosition = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, Mouse.GetState().Position);
        var hideBreadcrumbForReviewControls =
            _session.CurrentMode.Kind is GoAppModeKind.Reviewing or GoAppModeKind.GameOver &&
            PopupTrendChartRenderer.IsBottomNavigationControlsNearby(virtualMousePosition);
        if (_presentationServices is not null)
            HeadUpDisplayComponent.Default.Breadcrumb.Draw(_presentationServices.Stationery,
                GetScreenBreadcrumb(), visible: !hideBreadcrumbForReviewControls);

        base.Draw(gameTime);
    }

    private void UpdateMouseInput()
    {
        if (!IsActive || !_inputArmed || IsCatalogSaveInProgress) return;

        var mouse = Mouse.GetState();
        var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
        if (_messageDialog is not null)
        {
            if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed && _messageDialog.IsCloseHit(point))
            {
                _messageDialog = null;
                _session.DeactivateModalWindow(ActiveWindowId.MessageDialog);
            }
            _previousMouse = mouse;
            return;
        }
        if (_isCommentEditorOpen)
        {
            var commentDialog = TextAreaDialog.Default;
            Mouse.SetCursor(commentDialog.IsTextBoxHit(point) ? MouseCursor.IBeam : MouseCursor.Arrow);
            if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
            {
                commentDialog.SetHasChanges(_commentTextArea.Text != _commentEditorInitialText);
                if (commentDialog.IsTextBoxHit(point) && _presentationServices is not null)
                {
                    _commentTextArea.BeginMouseSelection(
                        commentDialog.GetCaretIndex(_presentationServices.Stationery, point, _commentTextArea.Text),
                        IsShiftDown());
                }
                else if (commentDialog.ApplyButton.IsHit(point))
                {
                    if (_commentTextArea.Text != _commentEditorInitialText) CommitCommentEditor(saveToFile: true);
                    else CancelCommentEditor();
                }
                else if (commentDialog.DiscardButton.IsHit(point))
                {
                    CancelCommentEditor();
                    BeginDiscardTransition();
                }
            }
            else if (mouse.LeftButton == ButtonState.Pressed && _previousMouse.LeftButton == ButtonState.Pressed &&
                     _commentTextArea.IsMouseSelecting && _presentationServices is not null)
            {
                _commentTextArea.UpdateMouseSelection(
                    commentDialog.GetCaretIndex(_presentationServices.Stationery, point, _commentTextArea.Text));
            }
            else if (mouse.LeftButton == ButtonState.Released)
            {
                _commentTextArea.EndMouseSelection();
            }
            _previousMouse = mouse;
            return;
        }
        if (_isReviewUnsavedChangesConfirmationOpen)
        {
            if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
            {
                if (HeadUpDisplayComponent.Default.ReviewUnsavedChangesConfirmation.SaveButton.IsHit(point) == true) SavePendingReviewExit();
                else if (HeadUpDisplayComponent.Default.ReviewUnsavedChangesConfirmation.DiscardButton.IsHit(point) == true) CompletePendingReviewExit(discardChanges: true);
                else if (HeadUpDisplayComponent.Default.ReviewUnsavedChangesConfirmation.CancelButton.IsHit(point) == true) CancelPendingReviewExit();
            }
            _previousMouse = mouse;
            return;
        }
        UpdateTextBoxMouseDrag(mouse, point);
        var isGtpEngineOptionInputPopupOpen =
            _activeGtpEngineIntegerOption is not null ||
            _activeGtpEngineStringOption is not null;
        if (isGtpEngineOptionInputPopupOpen)
        {
            Mouse.SetCursor(MouseCursor.Arrow);
        }
        else
        {
            UpdateCatalogOrderDrag(mouse, point);
        var engineErrorLogHovered = _session.UseKind == GoAppUseKind.LocalPlay &&
            PlayersComponent.Default.GetEngineErrorLogHit(point, _session);
        var boardLensButtonHovered = _session.CurrentMode.Kind == GoAppModeKind.Reviewing &&
            BoardAndReviewScreen.Default.Review.BoardLensButton.IsHit(point);
        var boardLensFamilyButtonHovered = _session.CurrentMode.Kind == GoAppModeKind.Reviewing &&
            BoardAndReviewScreen.Default.Review.BoardLensNextButton.IsHit(point);
        var boardLensExitButtonHovered = _session.CurrentMode.Kind == GoAppModeKind.Reviewing &&
            BoardAndReviewScreen.Default.Review.BoardLensExitButton.IsHit(point);
        var boardLensPreviousButtonHovered = _session.CurrentMode.Kind == GoAppModeKind.Reviewing &&
            BoardAndReviewScreen.Default.Review.BoardLensPreviousButton.IsHit(point);
        Mouse.SetCursor(engineErrorLogHovered || boardLensButtonHovered || boardLensFamilyButtonHovered || boardLensExitButtonHovered || boardLensPreviousButtonHovered
            ? MouseCursor.Hand
            : MouseCursor.Arrow);
        if (_variationSession is null)
        {
            UpdateReviewMouseRepeat(mouse, point);
            UpdateReviewPopupSeekDrag(mouse, point);
        }
        }

        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            GuiOperationLog.User("Mouse click", $"screen={GetCurrentScreenState()} x={point.X} y={point.Y}");
            if (TryHandleActiveWindowClick(point))
            {
                _previousMouse = mouse;
                return;
            }

            if (_tournamentRulesSetting.IsTimeInputOpen)
            {
                if (HeadUpDisplayComponent.Default.PopupTimeUnderline.IsTextBoxHit(point, out var part) == true)
                    _tournamentRulesSetting.BeginTimeInputSelection(part,
                        HeadUpDisplayComponent.Default.PopupTimeUnderline.GetCaretIndex(_presentationServices!.Stationery, part, point, _tournamentRulesSetting.TimeInputTexts[part]));
                else
                {
                    var spinHandled = false;
                    if (_presentationServices is not null)
                    {
                        for (var index = 0; index < 3; index++)
                        {
                            var spin = HeadUpDisplayComponent.Default.PopupTimeUnderline.SpinButtons[index];
                            var step = index == 0 ? 1 : 1;
                            if (spin.UpButton.IsHit(point)) { _tournamentRulesSetting.ChangeTimeInput(index, step); spinHandled = true; break; }
                            if (spin.DownButton.IsHit(point)) { _tournamentRulesSetting.ChangeTimeInput(index, -step); spinHandled = true; break; }
                        }
                    }
                    if (!spinHandled && HeadUpDisplayComponent.Default.PopupTimeUnderline.OkButton.IsHit(point) == true)
                        _tournamentRulesSetting.CommitTimeInput();
                    else if (!spinHandled && HeadUpDisplayComponent.Default.PopupTimeUnderline.CancelButton.IsHit(point) == true)
                        _tournamentRulesSetting.CancelTimeInput();
                }
                _previousMouse = mouse;
                return;
            }

            if (_tournamentRulesSetting.IsMoveLimitInputOpen)
            {
                if (HeadUpDisplayComponent.Default.PopupNumberUnderline.IsTextBoxHit(point) == true)
                    _tournamentRulesSetting.BeginMoveLimitInputSelection(
                        HeadUpDisplayComponent.Default.PopupNumberUnderline.GetCaretIndex(_presentationServices!.Stationery, point, _tournamentRulesSetting.MoveLimitInputText), IsShiftDown());
                else
                {
                    var spinHandled = false;
                    if (HeadUpDisplayComponent.Default.PopupNumberUnderline.SpinButtons.Count >= 3)
                    for (var index = 0; index < 3; index++)
                    {
                        var step = index switch { 0 => 100, 1 => 10, _ => 1 };
                        var spinButton = HeadUpDisplayComponent.Default.PopupNumberUnderline.SpinButtons[index];
                        if (spinButton.UpButton.IsHit(point))
                        {
                            _tournamentRulesSetting.ChangeMoveLimitInput(step);
                            spinHandled = true;
                            break;
                        }
                        if (spinButton.DownButton.IsHit(point))
                        {
                            _tournamentRulesSetting.ChangeMoveLimitInput(-step);
                            spinHandled = true;
                            break;
                        }
                    }
                    if (!spinHandled && HeadUpDisplayComponent.Default.PopupNumberUnderline.OkButton.IsHit(point) == true)
                        _tournamentRulesSetting.CommitMoveLimitInput();
                    else if (!spinHandled && HeadUpDisplayComponent.Default.PopupNumberUnderline.CancelButton.IsHit(point) == true)
                        _tournamentRulesSetting.CancelMoveLimitInput();
                }
                _previousMouse = mouse;
                return;
            }

            if (_tournamentRulesSetting.IsKomiInputOpen)
            {
                if (HeadUpDisplayComponent.Default.PopupNumberUnderline.IsTextBoxHit(point) == true)
                    _tournamentRulesSetting.BeginKomiInputSelection(
                        HeadUpDisplayComponent.Default.PopupNumberUnderline.GetCaretIndex(_presentationServices!.Stationery, point, _tournamentRulesSetting.KomiInputText), IsShiftDown());
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.StepUpButton?.IsHit(point) == true)
                    _tournamentRulesSetting.ChangeKomiInput(0.5m);
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.StepDownButton?.IsHit(point) == true)
                    _tournamentRulesSetting.ChangeKomiInput(-0.5m);
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.OkButton.IsHit(point) == true)
                    _tournamentRulesSetting.CommitKomiInput();
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.CancelButton.IsHit(point) == true)
                    _tournamentRulesSetting.CancelKomiInput();
                _previousMouse = mouse;
                return;
            }
            if (_activeGtpEngineIntegerOption is not null)
            {
                if (HeadUpDisplayComponent.Default.PopupNumberUnderline.IsTextBoxHit(point) == true)
                {
                    _gtpEngineIntegerOptionTextBox.BeginMouseSelection(
                        HeadUpDisplayComponent.Default.PopupNumberUnderline.GetCaretIndex(_presentationServices!.Stationery, point, _gtpEngineIntegerOptionTextBox.Text),
                        IsShiftDown());
                }
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.OkButton.IsHit(point) == true)
                    CommitGtpEngineIntegerInput();
                else if (HeadUpDisplayComponent.Default.PopupNumberUnderline.CancelButton.IsHit(point) == true)
                    CancelGtpEngineIntegerInput();
                _previousMouse = mouse;
                return;
            }
            if (_activeGtpEngineStringOption is not null)
            {
                if (_presentationServices is not null && TextInputDialog.IsTextBoxHit(point))
                {
                    _gtpEngineStringOptionTextBox.BeginMouseSelection(
                        HeadUpDisplayComponent.Default.TextInputDialog.GetCaretIndex(_presentationServices!.Stationery, point, _gtpEngineStringOptionTextBox.Text),
                        IsShiftDown());
                }
                else if (TextInputDialog.IsDefaultButtonHit(point))
                    RestoreGtpEngineStringInputDefault();
                else if (TextInputDialog.IsOkButtonHit(point))
                    CommitGtpEngineStringInput();
                else if (TextInputDialog.IsCancelButtonHit(point))
                    CancelGtpEngineStringInput();
                _previousMouse = mouse;
                return;
            }
            if (_session.UseKind is null)
            {
                if (_isApplicationSettingsOpen)
                {
                    HandleApplicationSettingsClick(point);
                }
                else if (_session.IsGtpEngineEditPanelOpen)
                {
                    TryHandleGtpEngineEditPanelClick(point);
                }
                else if (_session.IsGtpEngineSelectionDialogOpen)
                {
                    TryHandleGtpEngineSelectionDialogClick(point);
                }
                else if (TryHandleTitleMenuClick(point))
                {
                }
                else if (ApplicationSettingsScreen.Default.UpdateButton.IsHit(point))
                {
                    BeginGuiReleaseUpdate();
                }
                else if (ApplicationSettingsScreen.Default.SettingsButton.IsHit(point))
                {
                    GuiOperationLog.User("Pressed Settings button");
                    _isApplicationSettingsOpen = true;
                    _session.ActivateModalWindow(ActiveWindowId.ApplicationSettings);
                    RefreshGuiLogFiles();
                }

                _previousMouse = mouse;
                return;
            }

            // ［CGOS　＞　観戦画面］マウス入力
            if (_session.IsReviewChartPopupOpen &&
                _session.CurrentMode.Kind != GoAppModeKind.Reviewing)
            {
                HandleReadOnlyChartPopupClick(point);
                _previousMouse = mouse;
                return;
            }

            if (_variationSession is not null)
            {
                TryHandleVariationEditingClick(point);
                _previousMouse = mouse;
                return;
            }

            var isReplayNavigationVisible =
                (IsLocalPlayUseKind() && _session.IsLocalReplayMode) ||
                (_session.UseKind == GoAppUseKind.CgosClient && _cgosGameObservation.IsReplayMode);
            var isVariationEditVisible =
                isReplayNavigationVisible ||
                _session.CurrentMode.Kind == GoAppModeKind.Reviewing ||
                (IsLocalPlayUseKind() &&
                 _session.CanOpenLocalChartPopup) ||
                (_session.UseKind == GoAppUseKind.CgosClient &&
                 (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result) &&
                 _cgosGameObservation.IsStarted);
            var canReturnReplayToLive =
                (IsLocalPlayUseKind() &&
                 _session.CurrentMode.Kind == GoAppModeKind.Playing &&
                 _session.IsLocalReplayMode) ||
                (_session.UseKind == GoAppUseKind.CgosClient &&
                 !_cgosGameObservation.IsFinished &&
                 _cgosGameObservation.IsReplayMode);
            if (canReturnReplayToLive && PopupTrendChartRenderer.GetReplayBackToLiveButtonHit(point))
            {
                if (IsLocalPlayUseKind() &&
                    _session.CurrentMode.Kind == GoAppModeKind.Playing)
                {
                    _session.ReturnLocalReplayToLive();
                }
                else if (_session.UseKind == GoAppUseKind.CgosClient &&
                         !_cgosGameObservation.IsFinished)
                {
                    _cgosGameObservation.ReturnToLive();
                }

                _previousMouse = mouse;
                return;
            }
            if (isVariationEditVisible && PopupTrendChartRenderer.GetReplayEditButtonHit(point))
            {
                StartVariationEditingFromDisplayedPosition();
                _previousMouse = mouse;
                return;
            }
            if (isReplayNavigationVisible &&
                PopupTrendChartRenderer.GetReplayStepButtonHit(point) is { } replayStep &&
                TryGetReadOnlyChartNavigation(out var replayMoveIndex, out var replayMaximumMoveIndex))
            {
                var targetMoveIndex = replayStep switch
                {
                    int.MinValue => 0,
                    int.MaxValue => replayMaximumMoveIndex,
                    _ => Math.Clamp(replayMoveIndex + replayStep, 0, replayMaximumMoveIndex),
                };
                SeekReadOnlyChartPopup(targetMoveIndex);
                _previousMouse = mouse;
                return;
            }

            if (_session.UseKind == GoAppUseKind.CgosClient)
            {
                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing && TryHandleReviewClick(point))
                {
                    _previousMouse = mouse;
                    return;
                }

                // 前面の GTP ダイアログは、背後に残る PLAYER SELECT より先に入力を受け取る。
                // PLAYER 編集からエンジン選択を開くと、背後のダイアログの状態は意図的に保持される。
                if (TryHandleGtpEngineEditPanelClick(point) || TryHandleGtpEngineSelectionDialogClick(point))
                {
                    _previousMouse = mouse;
                    return;
                }

                if (_session.IsPlayerSelectionDialogOpen)
                {
                    TryHandlePlayerSelectionDialogClick(point);
                    _previousMouse = mouse;
                    return;
                }

                if (_session.IsCgosAdminPlayerSelectionDialogOpen)
                {
                    if (CgosLoginPage.Default.PlayerDialogCancelButton.IsHit(point))
                    {
                        _session.CancelCgosAdminPlayerSelectionDialog();
                    }
                    else if (CgosLoginPage.Default.PlayerDialogSelectButton.IsHit(point))
                    {
                        _session.CommitCgosAdminPlayerSelectionDialog();
                    }
                    else if (CgosLoginPage.Default.PlayerDialogPreviousButton.IsHit(point))
                    {
                        _session.MoveCgosAdminPlayerSelectionPage(-1);
                    }
                    else if (CgosLoginPage.Default.PlayerDialogNextButton.IsHit(point))
                    {
                        _session.MoveCgosAdminPlayerSelectionPage(1);
                    }
                    else if (CgosLoginRenderer.GetCgosAdminPlayerDialogItemHit(point, _session) is { } playerIndex)
                    {
                        _session.SelectCgosAdminPlayerDialogItem(playerIndex);
                    }

                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    _session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                    MoveCommentPanelRenderer.GetCgosCommentMoveStepButtonHit(point) is { } cgosCommentMoveStep)
                {
                    TrySeekReadOnlyAdjacentComment(cgosCommentMoveStep);
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    _session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                    MoveCommentPanelRenderer.GetCgosCommentPageStepButtonHit(point) is { } cgosCommentPageStep)
                {
                    _session.ChangeCommentPage(cgosCommentPageStep);
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    MoveTrendChartRenderer.GetCgosMoveInformationDisplayModeButtonHit(point) is { } cgosInformationMode)
                {
                    _session.SetMoveInformationDisplayMode(cgosInformationMode);
                    GuiOperationLog.User("Changed CGOS move information display", $"mode={cgosInformationMode}");
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    MoveTrendChartRenderer.GetCgosTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode) is { } trendMode)
                {
                    _session.SetMoveTrendDisplayMode(trendMode);
                    GuiOperationLog.User("Changed CGOS trend display", $"mode={trendMode}");
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    PopupTrendChartRenderer.GetCgosLiveChartPopupOpenHit(point))
                {
                    ResetReadOnlyChartPopupDoubleClick();
                    _session.OpenCgosLiveChartPopup();
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching &&
                    CgosWatchPage.Default.LeaveViewButton.IsHit(point))
                {
                    RestoreCgosMatchNotificationAfterLeavingView();
                    _session.ReturnToCgosConnectionScreen();
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Result)
                {
                    var cgosWatchingScreen = CgosWatchPage.Default;
                    if (cgosWatchingScreen.ReviewButton.IsHit(point))
                    {
                        StartReviewingGameRecord(_cgosGameObservation.CreateGameRecord(), "CGOS review");
                    }
                    else if (_session.IsSgfAutoSaveAvailable &&
                             cgosWatchingScreen.ExportSgfButton.IsHit(point))
                    {
                        ToggleSgfAutoSave();
                        if (_session.IsSgfAutoSaveEnabled)
                        {
                            _lastAutoSavedCgosGameId = null;
                            TryAutoSaveCgosGame();
                        }
                    }
                    else if (!_session.IsSgfAutoSaveAvailable &&
                             cgosWatchingScreen.ExportSgfButton.IsHit(point))
                    {
                        ExportSgf(
                            _cgosGameObservation.CreateGameRecord(),
                            CgosSgfFileNameBuilder.Create(_session.SelectedCgosConnectionProfile, _cgosGameObservation));
                    }
                    else if (cgosWatchingScreen.LeaveViewButton.IsHit(point))
                    {
                        _session.ReturnToCgosConnectionScreen();
                    }

                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching)
                {
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionOrderEditor.IsOpen)
                {
                    TryHandleCgosConnectionOrderEditorClick(point);
                    _previousMouse = mouse;
                    return;
                }

                if (TryHandleCgosConnectionEditPanelClick(point))
                {
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
                {
                    if (_session.IsQuickClientIdentitySelectionPanelOpen)
                    {
                        var quickSelection = EntryProfilesScreen.Default.QuickSelection;
                        if (quickSelection.CancelButton.IsHit(point)) _session.CancelQuickClientIdentitySelectionPanel();
                        else if (quickSelection.SelectButton.IsHit(point)) _session.CommitQuickClientIdentitySelection();
                        else if (quickSelection.GetItemHit(point, _session.GetQuickClientIdentitySelectionTargets(_session.QuickClientIdentitySelectionStone, _session.QuickClientIdentitySelectionIsCgos).Count) is { } targetIndex) _session.SelectQuickClientIdentity(targetIndex);
                    }
                    else if (CgosLoginRenderer.GetCgosCredentialFieldHit(point) is { } credential &&
                        (credential.Stone == GoStone.Black || _session.IsCgosPlayer2InputEnabled))
                    {
                        BeginOrMoveCgosCredentialEdit(point, credential.Stone, credential.Field);
                    }
                    else
                    {
                        EndCgosCredentialEdit();
                        CgosLoginPage.Default.UpdateGameInProgressButtons(_session.IsCgosGameInProgress);
                        if (CgosLoginPage.Default.BackButton.IsHit(point))
                        {
                            if (_session.IsAnyCgosProcessRunning) _ = DisconnectAllCgosProcessesAsync();
                            _session.ReturnToCgosConnectionProfiles();
                        }
                        else if (CgosLoginRenderer.GetCgosPlayer2InputCheckHit(point, !_session.IsCgosWhiteConnectionRunning))
                        {
                            _session.ToggleCgosPlayer2Input();
                        }
                        else if (CgosLoginRenderer.GetCgosAdminInputCheckHit(point, !_session.IsCgosAdminRunning))
                        {
                            _session.ToggleCgosAdminInput();
                        }
                        else if (CgosLoginRenderer.GetCgosConnectionEngineSelectButtonHit(point, _session) is { } engineStone)
                        {
                            _session.OpenCgosPlayerSelectionDialog(engineStone);
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 _session.CgosConnectionProfiles.Count > 0 &&
                                 CgosLoginPage.Default.AdminConnectButton.IsHit(point))
                        {
                            ToggleCgosAdminProcess();
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 _session.IsCgosAdminRunning &&
                                 CgosLoginPage.Default.AdminWhoButton.IsHit(point))
                        {
                            SendCgosAdminCommand("who");
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 CgosLoginPage.Default.AdminWhitePlayerButton.IsHit(point))
                        {
                            _session.OpenCgosAdminPlayerSelectionDialog(GoStone.White);
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 CgosLoginPage.Default.AdminBlackPlayerButton.IsHit(point))
                        {
                            _session.OpenCgosAdminPlayerSelectionDialog(GoStone.Black);
                        }
                        else if (_session.CanSendCgosAdminMatch && CgosLoginPage.Default.AdminMatchButton.IsHit(point))
                        {
                            SendSelectedCgosAdminMatch();
                        }
                        else if (_session.CanSendCgosAdminMatch && CgosLoginPage.Default.AdminSwapButton.IsHit(point))
                        {
                            _session.SwapCgosAdminPlayers();
                        }
                        else if (!string.IsNullOrWhiteSpace(_session.CgosAdminLogDirectory) && CgosLoginPage.Default.AdminCodeButton.IsHit(point))
                        {
                            OpenCgosAdminLog();
                        }
                        else if (!string.IsNullOrWhiteSpace(_session.CgosAdminLogDirectory) && CgosLoginPage.Default.AdminTailButton.IsHit(point))
                        {
                            TailCgosAdminLog();
                        }
                        else if (_session.IsCgosGameInProgress &&
                                 _session.IsCgosBlackConnectionRunning &&
                                 CgosLoginPage.Default.BlackResignButton.IsHit(point))
                        {
                            SendCgosPlayerResign(GoStone.Black);
                        }
                        else if (_session.IsCgosPlayer2InputEnabled &&
                                     _session.IsCgosGameInProgress &&
                                     _session.IsCgosWhiteConnectionRunning &&
                                     CgosLoginPage.Default.WhiteResignButton.IsHit(point))
                        {
                            SendCgosPlayerResign(GoStone.White);
                        }
                        else if ((_session.IsCgosBlackConnectionRunning || _session.SelectedCgosBlackGtpEngineProfile is not null) &&
                                 CgosLoginPage.Default.BlackConnectButton.IsHit(point))
                        {
                            ToggleCgosPlayerConnectionProcess(GoStone.Black);
                        }
                        else if (_session.IsCgosPlayer2InputEnabled &&
                                 (_session.IsCgosWhiteConnectionRunning || _session.SelectedCgosWhiteGtpEngineProfile is not null) &&
                                 CgosLoginPage.Default.WhiteConnectButton.IsHit(point))
                        {
                            ToggleCgosPlayerConnectionProcess(GoStone.White);
                        }
                        else if (!string.IsNullOrWhiteSpace(_session.CgosBlackConnectionLogDirectory) && CgosLoginPage.Default.BlackCodeButton.IsHit(point))
                        {
                            OpenCgosPlayerConnectionLog(GoStone.Black);
                        }
                        else if (!string.IsNullOrWhiteSpace(_session.CgosBlackConnectionLogDirectory) && CgosLoginPage.Default.BlackTailButton.IsHit(point))
                        {
                            TailCgosPlayerConnectionLog(GoStone.Black);
                        }
                        else if (_session.IsCgosPlayer2InputEnabled &&
                                 !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory) &&
                                 CgosLoginPage.Default.WhiteCodeButton.IsHit(point))
                        {
                            OpenCgosPlayerConnectionLog(GoStone.White);
                        }
                        else if (_session.IsCgosPlayer2InputEnabled &&
                                 !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory) &&
                                 CgosLoginPage.Default.WhiteTailButton.IsHit(point))
                        {
                            TailCgosPlayerConnectionLog(GoStone.White);
                        }
                    }

                    _previousMouse = mouse;
                    return;
                }

                if (CgosSelectConnectionPage.Default.CancelButton.IsHit(point))
                {
                    _session.ReturnToUseSelection();
                }
                else if (_session.CgosConnectionProfiles.Count > 0 && CgosSelectConnectionPage.Default.SelectButton.IsHit(point))
                {
                    _session.OpenCgosConnectionStartScreen();
                }
                else if (CgosSelectConnectionPage.Default.AddButton.IsHit(point))
                {
                    _session.OpenCgosConnectionAddPanel();
                }
                else if (_session.CgosConnectionProfiles.Count > 0 && CgosSelectConnectionPage.Default.EditButton.IsHit(point))
                {
                    _session.OpenCgosConnectionEditPanel();
                }
                else if (_session.CgosConnectionProfiles.Count > 0 && CgosSelectConnectionPage.Default.DuplicateButton.IsHit(point))
                {
                    _session.OpenCgosConnectionDuplicatePanel();
                }
                else if (_session.CanDeleteSelectedCgosConnectionProfile && CgosSelectConnectionPage.Default.DeleteButton.IsHit(point))
                {
                    _session.RemoveSelectedCgosConnectionProfile();
                    _cgosConnectionCatalog.Save(_session.CgosConnectionProfiles);
                }
                else if (_session.CgosConnectionProfiles.Count > 1 &&
                         CgosSelectConnectionPage.Default.OrderButton.IsHit(point))
                {
                    _session.OpenCgosConnectionOrderEditor();
                }
                else if (CgosSelectConnectionPage.Default.PreviousButton.IsHit(point))
                {
                    _session.MoveCgosConnectionSelectionPage(-1);
                }
                else if (CgosSelectConnectionPage.Default.NextButton.IsHit(point))
                {
                    _session.MoveCgosConnectionSelectionPage(1);
                }
                else if (CgosLoginRenderer.GetCgosConnectionProfileHit(point, _session) is { } connectionProfileIndex)
                {
                    _session.SelectCgosConnectionProfile(connectionProfileIndex);
                }

                _previousMouse = mouse;
                return;
            }

            var isIntermissionMode = _session.CurrentMode.Kind == GoAppModeKind.Resting;
            var isSetupMode = isIntermissionMode && _session.UseKind == GoAppUseKind.LocalPlay;
            var isLocalAppsIntermission = isIntermissionMode && _session.UseKind == GoAppUseKind.LocalApps;
            var isPlayerSelectionIntermission = isSetupMode || isLocalAppsIntermission;
            var isBoardEditing = _session.CurrentMode.Kind == GoAppModeKind.BoardEditing;
            if ((_session.IsPlayerSelectionDialogOpen || _session.IsQuickClientIdentitySelectionPanelOpen) &&
                !_session.IsGtpEngineEditPanelOpen &&
                !_session.IsGtpEngineSelectionDialogOpen)
            {
                TryHandlePlayerSelectionDialogClick(point);
                _previousMouse = mouse;
                return;
            }
            var localMatchScreen = LocalMatchScreen.Default;
            var humanPlayerNameHit = isPlayerSelectionIntermission
                ? localMatchScreen.GetHumanPlayerNameHit(point, _session.BlackPlayerKind, _session.WhitePlayerKind, isLocalAppsIntermission)
                : null;
            if (_session.ActiveHumanPlayerNameStone is not null && humanPlayerNameHit is null)
                EndHumanPlayerNameEdit(commit: true);
            var handledByGtpEngineEditPanel = isPlayerSelectionIntermission && !isBoardEditing && TryHandleGtpEngineEditPanelClick(point);
            var handledByGtpEngineSelectionDialog = !handledByGtpEngineEditPanel && isPlayerSelectionIntermission && !isBoardEditing && TryHandleGtpEngineSelectionDialogClick(point);
            Func<Point, string, int>? getDisplayNameCaretIndex = _presentationServices is null
                ? null
                : (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_presentationServices.Stationery, caretPoint, text);
            Func<Point, TournamentRulesNumericField, string, int>? getNumericCaretIndex = _presentationServices is null
                ? null
                : (caretPoint, field, text) => TournamentRuleEditorLayout.GetNumericCaretIndex(caretPoint, field, text, _presentationServices.Stationery.GetTextCaretIndex);
            var handledByTournamentRulesSetting = !handledByGtpEngineEditPanel &&
                !handledByGtpEngineSelectionDialog &&
                isSetupMode &&
                !isBoardEditing &&
                _tournamentRulesSetting.TryHandleMouseClick(point, getDisplayNameCaretIndex, getNumericCaretIndex);
            if (handledByGtpEngineEditPanel || handledByGtpEngineSelectionDialog || handledByTournamentRulesSetting)
            {
                _previousMouse = mouse;
                return;
            }

            if (_playingScene.IsInitialPositionConciergeVisible)
            {
                _playingScene.TryHandleMouseClick(point);
                _previousMouse = mouse;
                return;
            }

            if (isBoardEditing && TryHandleBoardEditingClick(point))
            {
                _previousMouse = mouse;
                return;
            }

            if (_session.CurrentMode.Kind == GoAppModeKind.GameOver && TryHandlePostGameReviewClick(point))
            {
                _previousMouse = mouse;
                return;
            }

            if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing && TryHandleReviewClick(point))
            {
                _previousMouse = mouse;
                return;
            }

            int? localCommentPageStep = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => MoveCommentPanelRenderer.GetLocalCommentPageStepButtonHit(point),
                GoAppModeKind.GameOver => MoveCommentPanelRenderer.GetCompletedLocalGameCommentPageStepButtonHit(point),
                _ => null,
            };
            if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver)
            {
                int? localCommentMoveStep = _session.CurrentMode.Kind switch
                {
                    GoAppModeKind.Playing => MoveCommentPanelRenderer.GetLocalCommentMoveStepButtonHit(point),
                    GoAppModeKind.GameOver => MoveCommentPanelRenderer.GetCompletedLocalGameCommentMoveStepButtonHit(point),
                    _ => null,
                };
                if (localCommentMoveStep is { } selectedLocalCommentMoveStep)
                {
                    TrySeekReadOnlyAdjacentComment(selectedLocalCommentMoveStep);
                    _previousMouse = mouse;
                    return;
                }
            }

            if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                localCommentPageStep is { } selectedLocalCommentPageStep)
            {
                _session.ChangeCommentPage(selectedLocalCommentPageStep);
                _previousMouse = mouse;
                return;
            }

            if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                CanEditCompletedLocalGameComment() &&
                MoveCommentPanelRenderer.GetCompletedLocalGameCommentEditButtonHit(point))
            {
                OpenCommentEditor(_session, _session.LocalDisplayMoveIndex);
                _previousMouse = mouse;
                return;
            }

            if (_session.CurrentMode.Kind == GoAppModeKind.Playing &&
                LocalMatchPlayPage.Default.RightSidePanel.GetBoardLensButtonHit(point, _session.IsRenParseDisplayEnabled) is { } boardLensButton)
            {
                switch (boardLensButton)
                {
                    case BoardLensButton.Toggle:
                        ToggleBoardLens();
                        break;
                    case BoardLensButton.Previous:
                        TryStepBoardLens(-1);
                        break;
                    case BoardLensButton.Next:
                        TryStepBoardLens(1);
                        break;
                    case BoardLensButton.Exit:
                        TryDeactivateBoardLens();
                        break;
                }

                _previousMouse = mouse;
                return;
            }

            MoveInformationDisplayMode? localInformationMode = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => MoveTrendChartRenderer.GetLocalMoveInformationDisplayModeButtonHit(point),
                GoAppModeKind.GameOver => MoveTrendChartRenderer.GetCompletedLocalGameMoveInformationDisplayModeButtonHit(point, _session),
                _ => null,
            };
            if (localInformationMode is { } selectedLocalInformationMode)
            {
                _session.SetMoveInformationDisplayMode(selectedLocalInformationMode);
                GuiOperationLog.User("Changed local move information display", $"mode={selectedLocalInformationMode}");
                _previousMouse = mouse;
                return;
            }

            MoveTrendDisplayMode? localTrendMode = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => MoveTrendChartRenderer.GetLocalTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode),
                GoAppModeKind.GameOver => MoveTrendChartRenderer.GetCompletedLocalGameTrendDisplayModeButtonHit(point, _session, _session.MoveTrendDisplayMode),
                _ => null,
            };
            if (localTrendMode is { } selectedLocalTrendMode)
            {
                _session.SetMoveTrendDisplayMode(selectedLocalTrendMode);
                GuiOperationLog.User("Changed local trend display", $"mode={selectedLocalTrendMode}");
                _previousMouse = mouse;
                return;
            }

            var localChartPopupOpenHit = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => PopupTrendChartRenderer.GetLocalLiveChartPopupOpenHit(point),
                GoAppModeKind.GameOver => PopupTrendChartRenderer.GetCompletedLocalGameChartPopupOpenHit(point),
                _ => false,
            };
            if (_session.CanOpenLocalChartPopup && localChartPopupOpenHit)
            {
                ResetReadOnlyChartPopupDoubleClick();
                _session.OpenLocalChartPopup();
                _previousMouse = mouse;
                return;
            }

            if (isLocalAppsIntermission && _session.IsAppProviderGameSettingsDialogOpen)
            {
                TryHandleAppProviderGameSettingsClick(point);
                _previousMouse = mouse;
                return;
            }
            if (isLocalAppsIntermission &&
                LocalMatchIntermissionPage.Default.GetRandomSeedAutoChangeHit(point) is { } seedRole &&
                (seedRole != PonnukiRandomSeedRole.Player1 || _session.CanAutoChangePonnukiPlayer1Seed) &&
                (seedRole != PonnukiRandomSeedRole.Player2 || _session.CanAutoChangePonnukiPlayer2Seed))
            {
                _session.TogglePonnukiRandomSeedAutoChange(seedRole);
                _previousMouse = mouse;
                return;
            }
            if ((isSetupMode || isLocalAppsIntermission) && localMatchScreen.BackToTitleButton.IsHit(point))
            {
                _session.ReturnToUseSelection();
            }
            else if (isLocalAppsIntermission && LocalMatchIntermissionPage.Default.AppProviderGameSettingsButton.IsHit(point))
            {
                OpenAppProviderGameSettings();
            }
            else if (isLocalAppsIntermission && LocalMatchIntermissionPage.Default.ChangeAppProviderButton.IsHit(point))
            {
                _session.ReturnToUseSelection();
                _titleMenuPage = TitleMenuPage.CaptureGame;
                GuiOperationLog.User("Returned to App Provider selection", "app=ponnuki");
            }
            else if (isLocalAppsIntermission &&
                     _session.CanStartPlaying &&
                     localMatchScreen.StartPlayingButton.IsHit(point))
            {
                StartPonnukiApp();
            }
            else if (isSetupMode && localMatchScreen.ImportSgfButton.IsHit(point))
            {
                if (_session.HasReviewGameRecord)
                {
                    _session.ClearSgfGameRecord();
                }
                else
                {
                    ImportSgf();
                }
            }
            else if (isSetupMode && _session.HasReviewGameRecord && BoardAndReviewScreen.Default.StartReviewingButton.IsHit(point))
            {
                StartReviewingStoredGameRecord();
            }
            else if (isSetupMode && BoardAndReviewScreen.Default.StartBoardEditingButton.IsHit(point))
            {
                StartWhiteboardFromLocalSetup();
            }
            else if (isSetupMode && localMatchScreen.GetRandomSeedHit(point, _session) is { } seedStone)
            {
                EditLocalMatchRandomSeed(seedStone);
            }
            else if (isSetupMode &&
                     _session.CanStartPlaying &&
                     localMatchScreen.StartPlayingButton.IsHit(point))
            {
                StartLocalMatch();
            }
            else if (isPlayerSelectionIntermission &&
                     localMatchScreen.GetHandleHit(point, _session.UseKind == GoAppUseKind.LocalApps) is { } handleStone)
            {
                BeginOrMoveLocalMatchHandleEdit(point, handleStone);
            }
            else if (isPlayerSelectionIntermission &&
                     localMatchScreen.GetPlayerSelectorBounds(GoStone.Black, isLocalAppsIntermission).Contains(point))
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.OpenPlayerSelectionDialog(GoStone.Black);
            }
            else if (isPlayerSelectionIntermission &&
                     localMatchScreen.GetPlayerSelectorBounds(GoStone.White, isLocalAppsIntermission).Contains(point))
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.OpenPlayerSelectionDialog(GoStone.White);
            }
            else if (isPlayerSelectionIntermission &&
                     localMatchScreen.GetPlayerKindRow(GoStone.Black, isLocalAppsIntermission).GetPlayerKindHit(point) is { } blackPlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.Black, blackPlayerKind);
            }
            else if (isPlayerSelectionIntermission && _session.BlackPlayerKind == GoPlayerKind.Computer &&
                     (isLocalAppsIntermission
                         ? GtpEngineRenderer.GetPonnukiBlackGtpEngineBrowseButtonHit(point)
                         : GtpEngineRenderer.GetBlackGtpEngineBrowseButtonHit(point)))
            {
                OpenGtpEngineSelectionDialog(GoStone.Black);
            }
            else if (isPlayerSelectionIntermission &&
                     localMatchScreen.GetPlayerKindRow(GoStone.White, isLocalAppsIntermission).GetPlayerKindHit(point) is { } whitePlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.White, whitePlayerKind);
            }
            else if (isPlayerSelectionIntermission && _session.WhitePlayerKind == GoPlayerKind.Computer &&
                     (isLocalAppsIntermission
                         ? GtpEngineRenderer.GetPonnukiWhiteGtpEngineBrowseButtonHit(point)
                         : GtpEngineRenderer.GetWhiteGtpEngineBrowseButtonHit(point)))
            {
                OpenGtpEngineSelectionDialog(GoStone.White);
            }
            else if (humanPlayerNameHit is { } playerNameStone)
            {
                BeginHumanPlayerNameEdit(point, playerNameStone);
            }
            else if (PlayersComponent.Default.GetEngineErrorLogHit(point, _session))
            {
                OpenEngineLog();
            }
            else
            {
                _playingScene.TryHandleMouseClick(point);
            }
        }

        _previousMouse = mouse;
    }

    /// <summary>
    /// モーダル入力は最前面のアクティブウィンドウだけに渡します。
    /// 未移行の画面は従来の分岐へフォールバックします。
    /// </summary>
    private bool TryHandleActiveWindowClick(Point point)
    {
        switch (_session.ActiveWindowId)
        {
            case ActiveWindowId.GtpEngineEdit:
                return TryHandleGtpEngineEditPanelClick(point);
            case ActiveWindowId.GtpEngineSelection:
                return TryHandleGtpEngineSelectionDialogClick(point);
            case ActiveWindowId.PlayerSelection:
            case ActiveWindowId.PlayerEdit:
            case ActiveWindowId.ClientIdentitySelection:
            case ActiveWindowId.ClientIdentityEdit:
                TryHandlePlayerSelectionDialogClick(point);
                return true;
            case ActiveWindowId.ClientIdentityConnectionSelection:
                return TryHandleClientIdentityConnectionSelectionClick(point);
            case ActiveWindowId.QuickClientIdentitySelection:
                return TryHandleQuickClientIdentitySelectionClick(point);
            case ActiveWindowId.GtpEngineGuiOptions:
            case ActiveWindowId.GtpEngineComboSelection:
                if (_session.IsAppProviderGameSettingsDialogOpen)
                    TryHandleAppProviderGameSettingsClick(point);
                else
                    TryHandleGtpEngineEditPanelClick(point);
                return true;
            case ActiveWindowId.GtpEngineDeleteConfirmation:
                return TryHandleGtpEngineDeleteConfirmationClick(point);
            case ActiveWindowId.TournamentRulesSelection:
            case ActiveWindowId.TournamentRulesEdit:
                return _tournamentRulesSetting.TryHandleMouseClick(
                    point,
                    _presentationServices is null ? null : (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_presentationServices.Stationery, caretPoint, text),
                    _presentationServices is null ? null : (caretPoint, field, text) => TournamentRuleEditorLayout.GetNumericCaretIndex(caretPoint, field, text, _presentationServices.Stationery.GetTextCaretIndex));
            case ActiveWindowId.TournamentRulesDeleteConfirmation:
                return _tournamentRulesSetting.TryHandleMouseClick(point);
            case ActiveWindowId.CgosAdminPlayerSelection:
                return TryHandleCgosAdminPlayerSelectionDialogClick(point);
            case ActiveWindowId.CgosConnectionEdit:
                return TryHandleCgosConnectionEditPanelClick(point);
            case ActiveWindowId.CatalogOrderEditor:
                return TryHandleCatalogOrderEditorClick(point);
            case ActiveWindowId.ReviewChartPopup:
                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                    HandleReviewChartPopupClick(point);
                else
                    HandleReadOnlyChartPopupClick(point);
                return true;
            case ActiveWindowId.InitialPositionConcierge:
                return _playingScene.TryHandleMouseClick(point);
            case ActiveWindowId.ApplicationSettings:
                HandleApplicationSettingsClick(point);
                return true;
            case ActiveWindowId.BoardEditing:
                return TryHandleBoardEditingClick(point);
            case ActiveWindowId.VariationEditing:
                return TryHandleVariationEditingClick(point);
            case ActiveWindowId.CgosMatchNotification:
                return TryHandleCgosMatchNotificationClick(point);
            default:
                return false;
        }
    }

    private bool TryHandleCgosAdminPlayerSelectionDialogClick(Point point)
    {
        if (!_session.IsCgosAdminPlayerSelectionDialogOpen)
            return false;

        if (CgosLoginPage.Default.PlayerDialogCancelButton.IsHit(point))
            _session.CancelCgosAdminPlayerSelectionDialog();
        else if (CgosLoginPage.Default.PlayerDialogSelectButton.IsHit(point))
            _session.CommitCgosAdminPlayerSelectionDialog();
        else if (CgosLoginPage.Default.PlayerDialogPreviousButton.IsHit(point))
            _session.MoveCgosAdminPlayerSelectionPage(-1);
        else if (CgosLoginPage.Default.PlayerDialogNextButton.IsHit(point))
            _session.MoveCgosAdminPlayerSelectionPage(1);
        else if (CgosLoginRenderer.GetCgosAdminPlayerDialogItemHit(point, _session) is { } playerIndex)
            _session.SelectCgosAdminPlayerDialogItem(playerIndex);

        return true;
    }

    private bool TryHandleCatalogOrderEditorClick(Point point)
    {
        if (_session.PlayerOrderEditor.IsOpen)
        {
            TryHandlePlayerSelectionDialogClick(point);
            return true;
        }

        if (_session.GtpEngineOrderEditor.IsOpen)
            return TryHandleGtpEngineSelectionDialogClick(point);

        if (_session.TournamentRulesOrderEditor.IsOpen)
            return _tournamentRulesSetting.TryHandleMouseClick(point);

        if (_session.CgosConnectionOrderEditor.IsOpen)
        {
            TryHandleCgosConnectionOrderEditorClick(point);
            return true;
        }

        return false;
    }

    private bool TryHandleQuickClientIdentitySelectionClick(Point point)
    {
        if (!_session.IsQuickClientIdentitySelectionPanelOpen)
            return false;

        var quickSelection = EntryProfilesScreen.Default.QuickSelection;
        if (quickSelection.CancelButton.IsHit(point))
            _session.CancelQuickClientIdentitySelectionPanel();
        else if (quickSelection.SelectButton.IsHit(point))
            _session.CommitQuickClientIdentitySelection();
        else if (quickSelection.GetItemHit(point, _session.GetQuickClientIdentitySelectionTargets(_session.QuickClientIdentitySelectionStone, _session.QuickClientIdentitySelectionIsCgos).Count) is { } index)
            _session.SelectQuickClientIdentity(index);
        return true;
    }

    private bool TryHandleClientIdentityConnectionSelectionClick(Point point)
    {
        if (!_session.IsClientIdentityProfileConnectionSelectionPanelOpen)
            return false;

        var connectionSelection = EntryProfilesScreen.Default.ConnectionSelection;
        connectionSelection.UpdateState(
            _session.ClientIdentityProfileConnectionSelectionPageIndex,
            _session.ClientIdentityProfileConnectionSelectionPageCount);
        if (connectionSelection.CancelButton.IsHit(point))
            _session.CancelClientIdentityProfileConnectionSelectionPanel();
        else if (connectionSelection.SelectButton.IsHit(point))
            _session.CommitClientIdentityProfileConnectionSelection();
        else if (connectionSelection.PreviousButton.IsHit(point))
            _session.MoveClientIdentityProfileConnectionSelectionPage(-1);
        else if (connectionSelection.NextButton.IsHit(point))
            _session.MoveClientIdentityProfileConnectionSelectionPage(1);
        else if (connectionSelection.GetItemHit(point, _session.ClientIdentityProfileConnectionSelectionPageIndex, GoAppSession.ClientIdentityProfileConnectionSelectionPageSize, _session.CgosConnectionProfiles.Count) is { } index)
            _session.SelectClientIdentityProfileConnection(index);
        return true;
    }

    private void TryHandlePlayerSelectionDialogClick(Point point)
    {
        if (_session.IsQuickClientIdentitySelectionPanelOpen)
        {
            var quickSelection = EntryProfilesScreen.Default.QuickSelection;
            if (quickSelection.CancelButton.IsHit(point)) _session.CancelQuickClientIdentitySelectionPanel();
            else if (quickSelection.SelectButton.IsHit(point)) _session.CommitQuickClientIdentitySelection();
            else if (quickSelection.GetItemHit(point, _session.GetQuickClientIdentitySelectionTargets(_session.QuickClientIdentitySelectionStone, _session.QuickClientIdentitySelectionIsCgos).Count) is { } targetIndex) _session.SelectQuickClientIdentity(targetIndex);
            return;
        }
        if (_session.IsClientIdentityProfileSelectionPanelOpen)
        {
            var profileSelection = EntryProfilesScreen.Default.ProfileSelection;
            var profileEdit = EntryProfilesScreen.Default.ProfileEdit;
            var profileCount = _session.GetPlayerClientIdentityProfiles(_session.PlayerEditDraft.Id).Count;
            profileSelection.UpdateState(
                profileCount,
                _session.IsClientIdentityProfileDefault(_session.ClientIdentityProfileSelectionIndex));
            if (profileSelection.CloseButton.IsHit(point))
                _session.CloseClientIdentityProfileSelectionPanel();
            else if (profileSelection.UseButton.IsHit(point))
                _session.InputSelectedClientIdentityProfileToPlayerEditDraft();
            else if (profileSelection.SetDefaultButton.IsHit(point) && _session.SetSelectedClientIdentityProfileAsDefault())
                SavePlayerAndClientIdentityCatalogs();
            else if (profileSelection.AddButton.IsHit(point) && _session.AddClientIdentityProfileForInput())
                SavePlayerAndClientIdentityCatalogs();
            else if (profileSelection.EditButton.IsHit(point))
                _session.OpenClientIdentityProfileEditPanel();
            else if (profileSelection.DuplicateButton.IsHit(point) && _session.DuplicateSelectedClientIdentityProfile())
                SavePlayerAndClientIdentityCatalogs();
            else if (profileEdit.AddLocalButton.IsHit(point) && _session.AddClientIdentityProfile(false))
                SavePlayerAndClientIdentityCatalogs();
            else if (profileEdit.AddCgosButton.IsHit(point) && _session.AddClientIdentityProfile(true))
                SavePlayerAndClientIdentityCatalogs();
            else if (profileSelection.DeleteButton.IsHit(point) && _session.RemoveSelectedClientIdentityProfile())
                SavePlayerAndClientIdentityCatalogs();
            else if (profileSelection.GetItemHit(point, _session.GetPlayerClientIdentityProfiles(_session.PlayerEditDraft.Id).Count) is { } targetIndex)
                _session.SelectClientIdentityProfile(targetIndex);
            return;
        }
        if (_session.IsClientIdentityProfileEditPanelOpen)
        {
            var profileEdit = EntryProfilesScreen.Default.ProfileEdit;
            profileEdit.UpdateState(_session.IsClientIdentityProfileEditDirty);
            if (profileEdit.DiscardButton.IsHit(point) && _session.IsClientIdentityProfileEditDirty)
            {
                _session.CancelClientIdentityProfileEdit();
                SavePlayerAndClientIdentityCatalogs();
                BeginDiscardTransition();
            }
            else if (profileEdit.SaveButton.IsHit(point))
            {
                if (_session.IsClientIdentityProfileEditDirty)
                {
                    SaveClientIdentityProfileEditDraft();
                    _session.ReturnToClientIdentityProfileSelectionPanel();
                }
                else _session.ReturnToClientIdentityProfileSelectionPanelWithoutSaving();
            }
            else if (profileEdit.GetFieldHit(point) is { } field)
                BeginOrMoveClientIdentityProfileEditField(point, field);
            return;
        }
        if (_session.PlayerOrderEditor.IsOpen)
        {
            var editor = _session.PlayerOrderEditor;
            if (CatalogOrderPresenter.GetCatalogOrderCancelButtonHit(point) && editor.HasChanges)
            {
                _session.CancelPlayerOrderEditor();
                BeginDiscardTransition();
            }
            else if (CatalogOrderPresenter.GetCatalogOrderSaveButtonHit(point))
            {
                if (editor.HasChanges) SavePlayerCatalog(_session.CommitPlayerOrderEditor());
                else _session.CancelPlayerOrderEditor();
            }
            else if (CatalogOrderPresenter.GetCatalogOrderMoveStep(point, editor.PageSize) is var step && step == int.MinValue) editor.MoveSelectedToTop();
            else if (step != 0) editor.MoveSelected(step);
            else if (CatalogOrderPresenter.GetCatalogOrderPageStep(point) is var pageStep && pageStep != 0) editor.MoveVisiblePages(pageStep);
            else if (CatalogOrderPresenter.GetCatalogOrderCardHit(point, editor) is { } orderIndex) editor.BeginDrag(orderIndex);
            return;
        }
        if (_session.IsPlayerEditPanelOpen)
        {
            if (EditEntryProfile.Default.TryToggleClientIdentityPasswordVisibility(point, !_session.IsPlayerEditClientIdentityPasswordDisabled))
                return;
            else if (EditEntryProfile.Default.IsClientIdentityChangeHit(point))
                _session.OpenClientIdentityProfileSelectionPanel();
            else if (EditEntryProfile.Default.DiscardButton.IsHit(point))
            {
                _session.CancelPlayerEditPanel();
                BeginDiscardTransition();
            }
            else if (EditEntryProfile.Default.SaveAndCloseButton.IsHit(point))
            {
                if (_session.HasPlayerEditChanges)
                {
                    if (_session.SavePlayerEditDraft()) SavePlayerAndClientIdentityCatalogs();
                }
                else _session.CancelPlayerEditPanel();
            }
            else if (_session.PlayerEditDraft.Kind == EntryProfileKind.Computer &&
                      EditEntryProfile.Default.IsEngineChangeHit(point))
                _session.OpenPlayerEditGtpEngineSelectionDialog();
            else if (EditEntryProfile.Default.GetFieldHit(point, !_session.IsPlayerEditClientIdentityPasswordDisabled) is { } field)
                BeginOrMovePlayerEditField(point, field);
            return;
        }

        var selectEntryScreen = SelectEntryScreen.Default;
        if (selectEntryScreen.CancelButton.IsHit(point))
        {
            _session.CancelPlayerSelectionDialog();
            return;
        }

        if (selectEntryScreen.SelectButton.IsHit(point))
        {
            _session.CommitPlayerSelectionDialog();
            return;
        }

        if (selectEntryScreen.PreviousButton.IsHit(point))
        {
            _session.MovePlayerSelectionPage(-1);
            return;
        }

        if (selectEntryScreen.NextButton.IsHit(point))
        {
            _session.MovePlayerSelectionPage(1);
            return;
        }

        if (selectEntryScreen.AddHumanButton.IsHit(point))
        {
            if (_session.AddEntryProfile(EntryProfileKind.Human))
                SavePlayerAndClientIdentityCatalogs();
            return;
        }

        if (selectEntryScreen.AddComputerButton.IsHit(point))
        {
            if (_session.AddEntryProfile(EntryProfileKind.Computer))
                SavePlayerAndClientIdentityCatalogs();
            return;
        }

        if (selectEntryScreen.DuplicateButton.IsHit(point))
        {
            if (_session.DuplicateSelectedEntryProfile())
                SavePlayerAndClientIdentityCatalogs();
            return;
        }

        if (selectEntryScreen.DeleteButton.IsHit(point))
        {
            if (_session.DeleteSelectedEntryProfile())
                SavePlayerAndClientIdentityCatalogs();
            return;
        }

        if (selectEntryScreen.EditButton.IsHit(point))
        {
            _session.OpenSelectedPlayerEditPanel();
            return;
        }

        if (selectEntryScreen.OrderButton.IsHit(point))
        {
            _session.OpenPlayerOrderEditor();
            return;
        }

        if (selectEntryScreen.GetClientIdentityItemHit(point, _session.GetPlayerSelectionClientIdentities().Count) is { } clientIdentityIndex)
        {
            _session.SelectPlayerSelectionClientIdentity(clientIdentityIndex);
            return;
        }

        if (selectEntryScreen.GetEntryItemHit(point, _session.PlayerSelectionPageIndex, GoAppSession.PlayerSelectionPageSize, _session.EntryProfiles.Count) is { } index)
            _session.SelectPlayerDialogItem(index);
    }

    private void UpdateTextBoxMouseDrag(MouseState mouse, Point point)
    {
        if (mouse.LeftButton == ButtonState.Released)
        {
            _cgosConnectionEditTextBox.EndMouseSelection();
            _cgosCredentialTextBox.EndMouseSelection();
            _humanPlayerNameTextBox.EndMouseSelection();
            _localMatchHandleTextBox.EndMouseSelection();
            _playerEditTextBox.EndMouseSelection();
            _targetProfileEditTextBox.EndMouseSelection();
            _gtpEngineEditTextBox.EndMouseSelection();
            _gtpEngineIntegerOptionTextBox.EndMouseSelection();
            _gtpEngineStringOptionTextBox.EndMouseSelection();
            _tournamentRulesSetting.EndMouseSelection();
            return;
        }

        if (_presentationServices is null || _previousMouse.LeftButton != ButtonState.Pressed || !CanUpdateTextBoxMouseDrag()) return;

        if (_activeGtpEngineIntegerOption is not null)
        {
            if (_gtpEngineIntegerOptionTextBox.IsMouseSelecting)
            {
                _gtpEngineIntegerOptionTextBox.UpdateMouseSelection(
                    HeadUpDisplayComponent.Default.PopupNumberUnderline.GetCaretIndex(_presentationServices.Stationery, point, _gtpEngineIntegerOptionTextBox.Text));
            }
            return;
        }
        if (_activeGtpEngineStringOption is not null)
        {
            if (_gtpEngineStringOptionTextBox.IsMouseSelecting)
            {
                _gtpEngineStringOptionTextBox.UpdateMouseSelection(
                    HeadUpDisplayComponent.Default.TextInputDialog.GetCaretIndex(_presentationServices.Stationery, point, _gtpEngineStringOptionTextBox.Text));
            }
            return;
        }
        if (_cgosConnectionEditTextBox.IsMouseSelecting &&
            _session.ActiveCgosConnectionEditField is { } connectionField)
        {
            _cgosConnectionEditTextBox.UpdateMouseSelection(
                _presentationServices.Presentation.GetCgosConnectionEditPanelCaretIndex(point, connectionField, _cgosConnectionEditTextBox.Text));
            SyncCgosConnectionEditField(connectionField);
        }
        else if (_cgosCredentialTextBox.IsMouseSelecting &&
                 _session.ActiveCgosCredentialStone is { } credentialStone &&
                 _session.ActiveCgosCredentialField is { } credentialField)
        {
            _cgosCredentialTextBox.UpdateMouseSelection(
                _presentationServices.Presentation.GetCgosCredentialCaretIndex(point, credentialStone, credentialField, _cgosCredentialTextBox.Text));
            _session.SetCgosCredential(credentialStone, credentialField, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
            SyncCgosCredentialSelection();
        }
        else if (_humanPlayerNameTextBox.IsMouseSelecting &&
                 _session.ActiveHumanPlayerNameStone is { } humanStone)
        {
            _humanPlayerNameTextBox.UpdateMouseSelection(
                LocalMatchScreen.Default.GetHumanPlayerNameCaretIndex(_presentationServices.Stationery, point, humanStone, _humanPlayerNameTextBox.Text, _session.UseKind == GoAppUseKind.LocalApps));
            _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
            _session.SetHumanPlayerNameSelection(_humanPlayerNameTextBox.SelectionStart, _humanPlayerNameTextBox.SelectionLength);
        }
        else if (_localMatchHandleTextBox.IsMouseSelecting &&
                 _session.ActiveLocalMatchHandleStone is { } localHandleStone)
        {
            _localMatchHandleTextBox.UpdateMouseSelection(
                EntryProfilesPresenter.Default.GetLocalMatchHandleCaretIndex(_presentationServices.Stationery, point, localHandleStone, _localMatchHandleTextBox.Text, _session.UseKind == GoAppUseKind.LocalApps));
            _session.SetLocalMatchHandleDraft(
                _localMatchHandleTextBox.Text,
                _localMatchHandleTextBox.CaretIndex,
                _localMatchHandleTextBox.SelectionStart,
                _localMatchHandleTextBox.SelectionLength);
        }
        else if (_playerEditTextBox.IsMouseSelecting && _session.ActivePlayerEditField is { } playerField)
        {
            _playerEditTextBox.UpdateMouseSelection(
                EditEntryProfile.Default.GetCaretIndex(_presentationServices.Stationery, point, playerField, _playerEditTextBox.Text));
            SyncPlayerEditField(playerField);
        }
        else if (_targetProfileEditTextBox.IsMouseSelecting && _session.ActiveClientIdentityProfileEditField is { } targetField)
        {
            _targetProfileEditTextBox.UpdateMouseSelection(
                EntryProfilesPresenter.Default.GetClientIdentityProfileEditCaretIndex(_presentationServices.Stationery, point, _session.ClientIdentityProfileEditIndex, targetField, _targetProfileEditTextBox.Text, string.IsNullOrEmpty(_session.ClientIdentityProfileEditDraft.ConnectionProfileId)));
            SyncClientIdentityProfileEditField(targetField);
        }
        else if (_gtpEngineEditTextBox.IsMouseSelecting &&
                 _session.ActiveGtpEngineEditField is { } engineField)
        {
            _gtpEngineEditTextBox.UpdateMouseSelection(
            _presentationServices.Presentation.GetGtpEngineEditPanelCaretIndex(point, engineField, _gtpEngineEditTextBox.Text));
            SyncGtpEngineEditField(engineField);
        }

        _tournamentRulesSetting.UpdateMouseSelection(
            point,
            (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_presentationServices.Stationery, caretPoint, text),
            (caretPoint, field, text) => TournamentRuleEditorLayout.GetNumericCaretIndex(caretPoint, field, text, _presentationServices.Stationery.GetTextCaretIndex));
    }

    private static bool IsShiftDown()
    {
        var keyboard = Keyboard.GetState();
        return keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
    }

    private bool CanUpdateTextBoxMouseDrag() => _session.ActiveWindowId is
        ActiveWindowId.None or
        ActiveWindowId.PlayerEdit or
        ActiveWindowId.ClientIdentityEdit or
        ActiveWindowId.CgosConnectionEdit or
        ActiveWindowId.GtpEngineEdit or
        ActiveWindowId.TournamentRulesEdit or
        ActiveWindowId.TextInput or
        ActiveWindowId.IntegerInput;

    private void UpdateScreenshotKeyboardInput(KeyboardState keyboard)
    {
        var controlDown = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        if (controlDown && keyboard.IsKeyDown(Keys.P) && _previousScreenshotKeyboard.IsKeyUp(Keys.P))
            CaptureWindowScreenshot();

        _previousScreenshotKeyboard = keyboard;
    }

    private void CaptureWindowScreenshot()
    {
        try
        {
            var directory = ApplicationSettings.Current.ScreenshotSaveDirectory;
            Directory.CreateDirectory(directory);
            var filePath = Path.Combine(directory, $"kifuwarabe-go-screenshot-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png");
            var result = _windowScreenshotService.SaveActiveWindow(filePath);
            _screenshotEffectStartedAt = _inputClockSeconds;
            PlayScreenshotShutterSound();
            _applicationSettingsMessage = "SCREENSHOT SAVED: " + Path.GetFileName(filePath);
            GuiOperationLog.User("Captured window screenshot", filePath);
            GuiOperationLog.App("Screenshot diagnostics", result.Diagnostics);
        }
        catch (Exception ex)
        {
            _applicationSettingsMessage = "SCREENSHOT ERROR: " + ex.Message;
            ApplicationErrorLog.Write("SCREENSHOT", "Could not capture the game window.", ex);
        }
    }

    private static bool IsShiftDown(KeyboardState keyboard) =>
        keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

    private void UpdateCatalogOrderDrag(MouseState mouse, Point point)
    {
        if (_session.ActiveWindowId != ActiveWindowId.CatalogOrderEditor)
            return;

        UpdateCatalogOrderDrag(_session.TournamentRulesOrderEditor, mouse, point);
        UpdateCatalogOrderDrag(_session.GtpEngineOrderEditor, mouse, point);
        UpdateCatalogOrderDrag(_session.CgosConnectionOrderEditor, mouse, point);
        UpdateCatalogOrderDrag(_session.PlayerOrderEditor, mouse, point);
    }

    private void UpdateCatalogOrderDrag<T>(CatalogOrderEditor<T> editor, MouseState mouse, Point point)
    {
        if (!editor.IsOpen || editor.DraggedIndex < 0)
        {
            return;
        }

        if (mouse.LeftButton == ButtonState.Released)
        {
            editor.EndDrag();
            return;
        }

        if (CatalogOrderPresenter.GetCatalogOrderCardHit(point, editor) is { } index)
        {
            editor.DragTo(index);
        }
    }

    private void OpenEngineLog()
    {
        var logPath = ApplicationErrorLog.FilePath;
        _desktopLauncher.OpenTextFile(logPath);
    }

    private void OpenGtpLog()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "gtp.log");
        _desktopLauncher.OpenTextFile(logPath);
    }

    private void StartLocalMatch()
    {
        var seeds = _session.ApplyLocalMatchRandomSeedsAtStart();
        _playingScene.StartPlaying();

        var seedComment = $"LOCAL MATCH RANDOM SEEDS\nBlack: {FormatLocalMatchSeed(seeds.Black)}\nWhite: {FormatLocalMatchSeed(seeds.White)}";
        _session.CurrentGameRecord.RootComment = string.IsNullOrWhiteSpace(_session.CurrentGameRecord.RootComment)
            ? seedComment
            : $"{_session.CurrentGameRecord.RootComment}\n\n{seedComment}";
        GuiOperationLog.User("Started Local Match",
            $"blackSeed={FormatLocalMatchSeed(seeds.Black)}; whiteSeed={FormatLocalMatchSeed(seeds.White)}");
    }

    private static string FormatLocalMatchSeed(int? seed) => seed?.ToString() ?? "HUMAN";

    private void StartPonnukiApp()
    {
        _session.ClearLocalAppsError();
        try
        {
            var seeds = _session.ApplyPonnukiRandomSeedsAtStart();
            _gtpEngineCatalog.Save(_session.GtpEngineProfiles);
            var provider = _session.SelectedAppProviderEngine;
            StopPonnukiProviderGame();
            _ponnukiProviderGameSession = new PonnukiProviderGameSession(provider);
            var record = _ponnukiProviderGameSession.StartAsync().GetAwaiter().GetResult();
            record.RootComment = $"PONNUKI RANDOM SEEDS\nProvider: {seeds.Provider}\nBlack Player: {_session.GetPonnukiPlayerSeedLabel(GoStone.Black)} / {seeds.Player1}\nWhite Player: {_session.GetPonnukiPlayerSeedLabel(GoStone.White)} / {seeds.Player2}";
            if (!_session.LoadGameRecordAsInitialPosition(record, out var warning))
                throw new InvalidOperationException(warning);
            _ponnukiProviderObservedMoveCount = 0;

            GuiOperationLog.User(
                "Started Local App",
                $"app=ponnuki; provider={provider.DisplayName}; board={record.BoardSize}; setupStones={record.SetupStones.Count}; seed={_ponnukiProviderGameSession.Seed}");
            _playingScene.StartPlaying();
        }
        catch (Exception ex)
        {
            StopPonnukiProviderGame();
            _session.SetLocalAppsError(ex.Message);
            ApplicationErrorLog.Write("PONNUKI APP", "Could not create the initial position with the App Provider engine.", ex);
            GuiOperationLog.App("Could not start Local App", $"app=ponnuki; error={ex.Message}");
        }
    }

    private void UpdatePonnukiProviderGame()
    {
        if (_ponnukiProviderGameSession is null || _session.UseKind != GoAppUseKind.LocalApps)
            return;

        try
        {
            while (_ponnukiProviderObservedMoveCount < _session.CurrentGameRecord.Moves.Count)
            {
                var move = _session.CurrentGameRecord.Moves[_ponnukiProviderObservedMoveCount];
                var vertex = move.Point is { } point
                    ? KifuwarabeGo2026.Gui.Gtp.GtpCoordinate.FormatVertex(point, _session.BoardSize)
                    : "pass";
                var result = _ponnukiProviderGameSession.ListenMoveAsync(vertex).GetAwaiter().GetResult();
                _ponnukiProviderObservedMoveCount++;
                if (!result.Accepted)
                    throw new InvalidOperationException("The App Provider rejected the move notification.");

                if (result.GameOver)
                {
                    _session.CompleteLocalApp(result.WinnerStone, result.Reason);
                    StopPonnukiProviderGame();
                    return;
                }
            }

            if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
                StopPonnukiProviderGame();
        }
        catch (Exception ex)
        {
            _session.SetLocalAppsError(ex.Message);
            ApplicationErrorLog.Write("PONNUKI APP", "Could not notify the App Provider of a move.", ex);
            StopPonnukiProviderGame();
        }
    }

    private void StopPonnukiProviderGame()
    {
        var providerSession = _ponnukiProviderGameSession;
        _ponnukiProviderGameSession = null;
        if (providerSession is null) return;

        try
        {
            providerSession.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("PONNUKI APP", "Could not stop the App Provider engine.", ex);
        }
    }

    private void RecheckPonnukiProvider()
    {
        try
        {
            var provider = _session.SelectedAppProviderEngine;
            var result = PonnukiPositionProvider.CheckCapabilityAsync(provider).GetAwaiter().GetResult();
            _session.SetAppProviderCapability(result.IsSupported, result.Message);
            GuiOperationLog.User(
                "Rechecked App Provider",
                $"app=ponnuki; engine={provider.DisplayName}; supported={result.IsSupported}");
        }
        catch (Exception ex)
        {
            _session.SetAppProviderCapability(false, $"CHECK FAILED: {ex.Message}");
            ApplicationErrorLog.Write("APP PROVIDER CHECK", "Could not check the Ponnuki App Provider capability.", ex);
        }
    }

    private void BeginGuiReleaseUpdate()
    {
        if (_guiReleaseUpdateTask is not null) return;
        GuiOperationLog.User("Pressed GUI update button");
        _guiReleaseUpdateTask = GuiReleaseUpdater.DownloadLatestAndStartAsync();
    }

    private void CompleteGuiReleaseUpdate()
    {
        var task = _guiReleaseUpdateTask;
        if (task is null || !task.IsCompleted) return;
        _guiReleaseUpdateTask = null;
        try
        {
            var result = task.GetAwaiter().GetResult();
            GuiOperationLog.User("GUI update completed", result.Message);
            if (result.DidStartUpdatedGui) Exit();
            else ShowMessage(result.Message, "GUI UPDATE");
        }
        catch (Exception ex)
        {
            GuiOperationLog.App("GUI update failed", ex.ToString());
            ShowMessage("最新GUIの取得に失敗しました。ネットワーク接続とGitHub Releaseを確認してください。", "GUI UPDATE FAILED");
        }
    }

    private bool TryHandleTitleMenuClick(Point point)
    {
        if (TitleScreen.Default.BackButton.IsHit(point))
        {
            _titleMenuPage = TitleMenuPage.Home;
            GuiOperationLog.User("Pressed title menu Back button", $"page={_titleMenuPage}");
            return true;
        }

        if (_titleMenuPage == TitleMenuPage.Home)
        {
            if (TitleScreen.Default.LocalMatchButton.IsHit(point))
            {
                GuiOperationLog.User("Pressed Local Match button", "Navigate from title to local-match setup");
                _session.SelectUseKind(GoAppUseKind.LocalPlay);
                return true;
            }

            if (TitleScreen.Default.CgosClientButton.IsHit(point))
            {
                GuiOperationLog.User("Pressed CGOS button", "Navigate from title to CGOS connection selection");
                _session.SelectUseKind(GoAppUseKind.CgosClient);
                return true;
            }

            if (TitleScreen.Default.GetAppHit(point) is { } appIndex)
            {
                _titleMenuPage = appIndex switch
                {
                    0 => TitleMenuPage.CaptureGame,
                    1 => TitleMenuPage.Tsumego,
                    _ => TitleMenuPage.NextMove,
                };
                GuiOperationLog.User("Opened Casual Apps entry", $"page={_titleMenuPage}");
                return true;
            }
        }

        if (_titleMenuPage == TitleMenuPage.CaptureGame)
        {
            if (PonnukiProviderSelectionScreen.Default.IsProviderLinkHit(point))
            {
                BeginOpenAppProviderGtpEngineSelectionDialog("ponnuki");
                return true;
            }

            if (_session.CanUseSelectedAppProvider &&
                !_session.IsAppProviderCapabilityCheckRunning &&
                PonnukiProviderSelectionScreen.Default.RecheckButton.IsHit(point))
            {
                RecheckPonnukiProvider();
                return true;
            }

            if (_session.CanStartSelectedAppProvider && PonnukiProviderSelectionScreen.Default.StartButton.IsHit(point))
            {
                _session.SelectUseKind(GoAppUseKind.LocalApps);
                GuiOperationLog.User("Entered Local Apps intermission", "app=ponnuki");
                return true;
            }
        }

        return false;
    }

    private void UpdateAppProviderSelectionKeyboard(KeyboardState keyboard)
    {
        if (!IsActive || !_inputArmed || _titleMenuPage != TitleMenuPage.CaptureGame ||
            _session.IsGtpEngineSelectionDialogOpen || _session.IsGtpEngineEditPanelOpen)
        {
            return;
        }

        var enabled = new[]
        {
            _appProviderSelectionLoadTask is null,
            _session.CanUseSelectedAppProvider && !_session.IsAppProviderCapabilityCheckRunning,
            _session.CanStartSelectedAppProvider,
            true,
        };
        if (!enabled[_appProviderTabIndex])
        {
            _appProviderTabIndex = Array.FindIndex(enabled, value => value);
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousKeyboard.IsKeyUp(Keys.Tab))
        {
            var step = IsShiftDown(keyboard) ? -1 : 1;
            do
            {
                _appProviderTabIndex = (_appProviderTabIndex + step + enabled.Length) % enabled.Length;
            }
            while (!enabled[_appProviderTabIndex]);

            _previousKeyboard = keyboard;
            return;
        }

        var activate = (keyboard.IsKeyDown(Keys.Enter) && _previousKeyboard.IsKeyUp(Keys.Enter)) ||
            (keyboard.IsKeyDown(Keys.Space) && _previousKeyboard.IsKeyUp(Keys.Space));
        if (!activate)
        {
            return;
        }

        switch (_appProviderTabIndex)
        {
            case 0:
                OpenAppProviderGtpEngineSelectionDialog("ponnuki");
                break;
            case 1:
                RecheckPonnukiProvider();
                break;
            case 2:
                _session.SelectUseKind(GoAppUseKind.LocalApps);
                GuiOperationLog.User("Entered Local Apps intermission", "app=ponnuki; input=keyboard");
                break;
            case 3:
                _titleMenuPage = TitleMenuPage.Home;
                GuiOperationLog.User("Pressed title menu Back button", "page=Home; input=keyboard");
                break;
        }

        _previousKeyboard = keyboard;
    }

    private bool TryHandleBoardEditingClick(Point point)
    {
        if (_session.CurrentMode.Kind != GoAppModeKind.BoardEditing)
        {
            return false;
        }

        var boardEditing = BoardAndReviewScreen.Default.BoardEditing;
        if (boardEditing.BlackButton.IsHit(point))
        {
            _session.SetBoardEditingStone(GoStone.Black);
            return true;
        }

        if (boardEditing.WhiteButton.IsHit(point))
        {
            _session.SetBoardEditingStone(GoStone.White);
            return true;
        }

        if (boardEditing.EraseButton.IsHit(point))
        {
            _session.SetBoardEditingStone(GoStone.Empty);
            return true;
        }

        if (boardEditing.UndoButton.IsHit(point))
        {
            _session.UndoBoardEditing();
            return true;
        }

        if (boardEditing.RedoButton.IsHit(point))
        {
            _session.RedoBoardEditing();
            return true;
        }

        if (boardEditing.ClearButton.IsHit(point))
        {
            if (_session.ClearBoardEditing())
                PlayPlaceStoneSound(0.42f, -0.35f, 0f);
            return true;
        }

        if (boardEditing.CancelButton.IsHit(point))
        {
            _session.CancelBoardEditing();
            BeginDiscardTransition();
            return true;
        }

        if (boardEditing.AdoptButton.IsHit(point))
        {
            _session.FinishBoardEditing();
            return true;
        }

        if (BoardRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
        {
            if (_session.TryEditBoardStone(intersection.X, intersection.Y))
            {
                PlayPlaceStoneSound(_session.BoardEditingStone == GoStone.Empty ? 0.42f : 0.78f);
            }

            return true;
        }

        return false;
    }

    private bool TryHandlePostGameReviewClick(Point point)
    {
        if (_session.CurrentMode.Kind != GoAppModeKind.GameOver) return false;
        var controls = BoardAndReviewScreen.Default.Review;
        if (controls.BackToHomeButton.IsHit(point))
        {
            _session.ReturnToSetup();
            return true;
        }
        if (BoardAndReviewScreen.Default.ReviewingRightSidePanel.GetStepButtonHit(point) is { } step)
        {
            ExecuteReviewNavigation(step);
            _reviewMouseRepeatCommand = step is int.MinValue or int.MaxValue ? null : step;
            _reviewMouseNextRepeatAt = _inputClockSeconds + ReviewRepeatInitialDelaySeconds;
            return true;
        }
        if (_session.IsLocalResultPosition && controls.ExportSgfButton.IsHit(point))
        {
            if (_session.IsSgfAutoSaveAvailable)
            {
                ToggleSgfAutoSave();
                if (_session.IsSgfAutoSaveEnabled)
                {
                    _lastAutoSavedLocalGameRecord = null;
                    TryAutoSaveCompletedLocalGame();
                }
            }
            else
            {
                ExportSgf();
            }
            return true;
        }
        return false;
    }

    private bool TryHandleReviewClick(Point point)
    {
        if (_session.CurrentMode.Kind != GoAppModeKind.Reviewing)
        {
            return false;
        }

        if (_session.IsReviewChartPopupOpen)
        {
            HandleReviewChartPopupClick(point);
            return true;
        }

        if (_session.IsReviewResultPosition &&
            BoardAndReviewScreen.Default.Review.ExportSgfButton.IsHit(point))
        {
            ExportSgf();
            return true;
        }

        var reviewControls = BoardAndReviewScreen.Default.Review;
        reviewControls.UpdateBoardLensState(_session.IsRenParseDisplayEnabled, _session.IsMeasureBoardLens);
        if (reviewControls.BoardLensButton.IsHit(point))
        {
            ToggleBoardLens();
            return true;
        }

        if (reviewControls.BoardLensNextButton.IsHit(point))
        {
            TryStepBoardLens(1);
            return true;
        }

        if (reviewControls.BoardLensPreviousButton.IsHit(point))
        {
            TryStepBoardLens(-1);
            return true;
        }

        if (reviewControls.BoardLensExitButton.IsHit(point))
        {
            TryDeactivateBoardLens();
            return true;
        }

        if (BoardAndReviewScreen.Default.ReviewingRightSidePanel.GetStepButtonHit(point) is { } step)
        {
            ExecuteReviewNavigation(step);
            _reviewMouseRepeatCommand = step is int.MinValue or int.MaxValue ? null : step;
            _reviewMouseNextRepeatAt = _inputClockSeconds + ReviewRepeatInitialDelaySeconds;
            return true;
        }

        if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
            MoveCommentPanelRenderer.GetReviewCommentMoveStepButtonHit(point) is { } reviewCommentMoveStep)
        {
            TryMoveReviewAdjacentComment(reviewCommentMoveStep);
            return true;
        }

        if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
            MoveCommentPanelRenderer.GetReviewCommentPageStepButtonHit(point) is { } reviewCommentPageStep)
        {
            _session.ChangeCommentPage(reviewCommentPageStep);
            return true;
        }

        if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
            MoveCommentPanelRenderer.GetReviewCommentEditButtonHit(point))
        {
            OpenCommentEditor(_session, _session.ReviewMoveIndex);
            return true;
        }

        if (MoveTrendChartRenderer.GetReviewMoveInformationDisplayModeButtonHit(point) is { } reviewInformationMode)
        {
            _session.SetMoveInformationDisplayMode(reviewInformationMode);
            return true;
        }

        if (MoveTrendChartRenderer.GetReviewTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode) is { } reviewTrendMode)
        {
            _session.SetMoveTrendDisplayMode(reviewTrendMode);
            return true;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupOpenHit(point))
        {
            _session.OpenReviewChartPopup();
            _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            return true;
        }

        if (_session.UseKind == GoAppUseKind.LocalPlay && reviewControls.UsePositionButton.IsHit(point))
        {
            BeginReviewExit(ReviewExitAction.UsePosition);
            return true;
        }

        if (reviewControls.BackToHomeButton.IsHit(point))
        {
            BeginReviewExit(ReviewExitAction.BackToHome);
            return true;
        }

        return true;
    }

    private void StartVariationEditingFromDisplayedPosition()
    {
        if (_session.UseKind is not { } useKind)
            return;

        GoGameRecord sourceRecord;
        int sourceMoveIndex;
        if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            sourceRecord = _session.CurrentGameRecord.Clone();
            sourceMoveIndex = sourceRecord.Moves.Count;
        }
        else if (_session.UseKind == GoAppUseKind.CgosClient)
        {
            sourceRecord = _cgosGameObservation.CreateGameRecord();
            sourceMoveIndex = _cgosGameObservation.DisplayMoveIndex;
        }
        else
        {
            sourceRecord = _session.CurrentGameRecord.Clone();
            sourceMoveIndex = _session.LocalDisplayMoveIndex;
        }

        var variationSession = new GoAppSession();
        variationSession.SelectUseKind(useKind);
        if (!variationSession.StartVariationEditing(
                sourceRecord,
                sourceMoveIndex,
                GoAppModeKind.Resting,
                out var warning))
        {
            if (!string.IsNullOrWhiteSpace(warning))
                ShowMessage(warning, "Analysis editing");
            return;
        }

        _variationSession = variationSession;
        _session.ActivateModalWindow(ActiveWindowId.VariationEditing);
    }

    private void StartWhiteboardFromLocalSetup()
    {
        var sourceRecord = _session.CurrentGameRecord.Clone();
        var variationSession = new GoAppSession();
        variationSession.SelectUseKind(GoAppUseKind.LocalPlay);
        if (!variationSession.StartVariationEditing(
                sourceRecord,
                sourceRecord.Moves.Count,
                GoAppModeKind.Resting,
                out var warning))
        {
            if (!string.IsNullOrWhiteSpace(warning))
                ShowMessage(warning, "Whiteboard");
            return;
        }

        variationSession.EnableVariationPositionAdoption();
        _variationSession = variationSession;
        _session.ActivateModalWindow(ActiveWindowId.VariationEditing);
    }

    private bool TryHandleVariationEditingClick(Point point)
    {
        var variationSession = _variationSession;
        if (variationSession is null ||
            variationSession.CurrentMode.Kind != GoAppModeKind.VariationEditing)
            return false;

        var variationControls = BoardAndReviewScreen.Default.VariationEditing;
        if (variationControls.DiscardButton.IsHit(point))
        {
            _variationSession = null;
            _session.DeactivateModalWindow(ActiveWindowId.VariationEditing);
            BeginDiscardTransition();
            return true;
        }

        if (variationSession.CanAdoptVariationPosition &&
            variationControls.AdoptButton.IsHit(point))
        {
            var adoptedRecord = variationSession.CreateCurrentPositionAsSetupRecord();
            if (_session.LoadGameRecordAsInitialPosition(adoptedRecord, out var warning))
            {
                _variationSession = null;
                _session.DeactivateModalWindow(ActiveWindowId.VariationEditing);
            }
            else if (!string.IsNullOrWhiteSpace(warning))
            {
                ShowMessage(warning, "Whiteboard");
            }
            return true;
        }

        if (variationControls.ExportSgfButton.IsHit(point))
        {
            ExportSgf(
                variationSession.CurrentGameRecord,
                $"kifuwarabe-analysis-{DateTime.Now:yyyyMMdd-HHmmss}.sgf",
                markCurrentResultSaved: false);
            return true;
        }

        if (variationControls.CommentButton.IsHit(point))
        {
            OpenCommentEditor(variationSession, variationSession.CurrentGameRecord.Moves.Count);
            return true;
        }

        if (variationControls.BoardLensToggleBounds.Contains(point))
        {
            variationSession.ToggleRenParseDisplay();
            _boardLensBannerStartedAt = _inputClockSeconds;
            return true;
        }

        if (variationSession.IsRenParseDisplayEnabled && variationControls.BoardLensPreviousBounds.Contains(point))
        {
            if (variationSession.TryStepBoardLens(-1))
                _boardLensBannerStartedAt = _inputClockSeconds;
            return true;
        }

        if (variationSession.IsRenParseDisplayEnabled && variationControls.BoardLensNextBounds.Contains(point))
        {
            if (variationSession.TryStepBoardLens(1))
                _boardLensBannerStartedAt = _inputClockSeconds;
            return true;
        }

        if (variationSession.IsRenParseDisplayEnabled && variationControls.BoardLensExitBounds.Contains(point))
        {
            if (variationSession.TryDeactivateBoardLens())
                _boardLensBannerStartedAt = _inputClockSeconds;
            return true;
        }

        if (variationControls.PlayButton.IsHit(point))
        {
            variationSession.SetVariationEditingStone(null);
            return true;
        }

        if (variationControls.BlackButton.IsHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.Black);
            return true;
        }

        if (variationControls.WhiteButton.IsHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.White);
            return true;
        }

        if (variationControls.EraseButton.IsHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.Empty);
            return true;
        }

        if (variationControls.ClearButton.IsHit(point))
        {
            if (variationSession.ClearVariationBoard())
                PlayPlaceStoneSound(0.42f, -0.35f, 0f);
            return true;
        }

        if (variationControls.UndoButton.IsHit(point))
        {
            variationSession.UndoVariation();
            return true;
        }

        if (variationControls.PassButton.IsHit(point))
        {
            if (variationSession.VariationEditingStone is null &&
                variationSession.PassVariation())
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
            return true;
        }

        if (BoardRenderer.TryGetBoardIntersection(point, variationSession.BoardSize, out var intersection))
        {
            if (variationSession.VariationEditingStone is null)
            {
                if (variationSession.TryPlaceVariationStone(intersection.X, intersection.Y))
                    PlayPlaceStoneSound();
            }
            else if (variationSession.TryEditVariationStone(intersection.X, intersection.Y))
            {
                PlayPlaceStoneSound(
                    variationSession.VariationEditingStone == GoStone.Empty ? 0.42f : 0.78f);
            }
            return true;
        }

        return true;
    }

    private void MoveReview(int step)
    {
        if (!_session.MoveReview(step, out var warning) && !string.IsNullOrWhiteSpace(warning))
        {
            ShowMessage(warning, "SGF review");
        }
    }

    private void StartReviewingStoredGameRecord()
    {
        if (!_session.StartReviewingStoredGameRecord(out var warning) && !string.IsNullOrWhiteSpace(warning))
        {
            ShowMessage(warning, "SGF review");
        }
    }

    /// <summary>
    /// 指定された棋譜を共通の棋譜レビューフローで開きます。
    /// </summary>
    private void StartReviewingGameRecord(GoGameRecord record, string messageTitle, string? sourceFilePath = null)
    {
        if (!_session.StartReviewingGameRecord(record, out var warning) && !string.IsNullOrWhiteSpace(warning))
        {
            ShowMessage(warning, messageTitle);
        }
        else if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            _reviewSgfFilePath = sourceFilePath;
        }
    }

    private void OpenTournamentRulesSelectionDialog()
    {
        _session.OpenTournamentRulesSelectionDialog();
    }

    private bool TryHandleCgosConnectionEditPanelClick(Point point)
    {
        if (!_session.IsCgosConnectionEditPanelOpen)
        {
            return false;
        }

        if (_session.IsCgosConnectionEditDirty && CgosSelectConnectionPage.Default.EditDiscardButton.IsHit(point))
        {
            EndCgosConnectionEditField();
            _cgosConnectionEditTextBox.Clear();
            _session.CloseCgosConnectionEditPanel();
            BeginDiscardTransition();
            return true;
        }

        if (CgosSelectConnectionPage.Default.EditSaveButton.IsHit(point))
        {
            if (_session.IsCgosConnectionEditDirty)
            {
                if (SaveCgosConnectionEditDraft()) CloseCgosConnectionEditPanel();
            }
            else CloseCgosConnectionEditPanel();
            return true;
        }

        if (CgosLoginRenderer.GetCgosConnectionEditPanelFieldHit(point) is { } field)
        {
            BeginOrMoveCgosConnectionEditField(point, field);
            return true;
        }

        return true;
    }

    private void UpdateCgosConnectionEditPanelByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;

        if (!_session.IsCgosConnectionEditPanelOpen)
        {
            _previousCgosConnectionKeyboard = keyboard;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousCgosConnectionKeyboard.IsKeyUp(Keys.Tab))
        {
            MoveCgosConnectionEditFocus(IsShiftDown(keyboard) ? -1 : 1);
            _previousCgosConnectionKeyboard = keyboard;
            return;
        }

        if (_session.ActiveCgosConnectionEditField is { } field)
        {
            switch (_cgosConnectionEditTextBox.HandleKeyboard(keyboard, _previousCgosConnectionKeyboard, gameTime, _clipboardService))
            {
                case TextBoxKeyboardAction.Commit:
                    EndCgosConnectionEditField();
                    break;
                case TextBoxKeyboardAction.Cancel:
                    CancelCgosConnectionEditField(field);
                    _session.SetCgosConnectionEditWarning("");
                    break;
                default:
                    SyncCgosConnectionEditField(field);
                    break;
            }

            _previousCgosConnectionKeyboard = keyboard;
            return;
        }

        if (keyboard.IsKeyDown(Keys.F5) && _previousCgosConnectionKeyboard.IsKeyUp(Keys.F5))
        {
            if (SaveCgosConnectionEditDraft())
                CloseCgosConnectionEditPanel();
        }

        _previousCgosConnectionKeyboard = keyboard;
    }

    private void ToggleCgosPlayerConnectionProcess(GoStone stone)
    {
        var process = stone == GoStone.Black ? _cgosBlackConnectionProcess : _cgosWhiteConnectionProcess;
        if (process.IsRunning)
        {
            _ = StopCgosPlayerConnectionProcessAsync(stone, process);
            SetCgosPlayerConnectionProcessStatus(stone, "STOPPING", true, process);
            return;
        }

        try
        {
            var status = process.Start(
                _session.SelectedCgosConnectionProfile,
                stone == GoStone.Black ? _session.SelectedCgosBlackGtpEngineProfile : null,
                stone == GoStone.White ? _session.SelectedCgosWhiteGtpEngineProfile : null,
                _session.GetCgosCredential(stone, CgosPlayerCredentialField.LoginName),
                _session.GetCgosCredential(stone, CgosPlayerCredentialField.Password));
            SetCgosPlayerConnectionProcessStatus(stone, status, process.IsRunning, process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            SetCgosPlayerConnectionProcessStatus(stone, "ERROR: " + ex.Message, false, process);
        }
    }

    private void UpdateCgosConnectionProcessStatus()
    {
        if (_session.CgosConnectionFlowKind != CgosConnectionFlowKind.ConnectionStart)
        {
            return;
        }

        var blackStatus = _cgosBlackConnectionProcess.RefreshStatus();
        _session.SetCgosBlackConnectionProcessStatus(blackStatus, _cgosBlackConnectionProcess.IsRunning, _cgosBlackConnectionProcess.LogDirectory, _cgosBlackConnectionProcess.GetRecentOutput(), _cgosBlackConnectionProcess.GtpResponseWaitDisplay);

        var whiteStatus = _cgosWhiteConnectionProcess.RefreshStatus();
        _session.SetCgosWhiteConnectionProcessStatus(whiteStatus, _cgosWhiteConnectionProcess.IsRunning, _cgosWhiteConnectionProcess.LogDirectory, _cgosWhiteConnectionProcess.GetRecentOutput(), _cgosWhiteConnectionProcess.GtpResponseWaitDisplay);
    }

    private void OpenCgosPlayerConnectionLog(GoStone stone)
    {
        var process = stone == GoStone.Black ? _cgosBlackConnectionProcess : _cgosWhiteConnectionProcess;
        try
        {
            var status = process.OpenLog("code", openStandardError: false);
            SetCgosPlayerConnectionProcessStatus(stone, status, process.IsRunning, process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            SetCgosPlayerConnectionProcessStatus(stone, "ERROR: " + ex.Message, process.IsRunning, process);
        }
    }

    private void TailCgosPlayerConnectionLog(GoStone stone)
    {
        var process = stone == GoStone.Black ? _cgosBlackConnectionProcess : _cgosWhiteConnectionProcess;
        try
        {
            var status = process.TailLogWithPowerShell(openStandardError: false);
            SetCgosPlayerConnectionProcessStatus(stone, status, process.IsRunning, process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            SetCgosPlayerConnectionProcessStatus(stone, "ERROR: " + ex.Message, process.IsRunning, process);
        }
    }

    private void SetCgosPlayerConnectionProcessStatus(GoStone stone, string status, bool isRunning, CgosConnectionProcess process)
    {
        if (stone == GoStone.Black)
        {
            _session.SetCgosBlackConnectionProcessStatus(status, isRunning, process.LogDirectory, process.GetRecentOutput());
            return;
        }

        _session.SetCgosWhiteConnectionProcessStatus(status, isRunning, process.LogDirectory, process.GetRecentOutput());
    }

    private void ToggleCgosAdminProcess()
    {
        if (_cgosAdminProcess.IsRunning)
        {
            _ = StopCgosAdminProcessAsync();
            _session.SetCgosAdminProcessStatus("ADMIN STOPPING", true, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
            return;
        }

        try
        {
            var status = _cgosAdminProcess.StartAdmin(_session.SelectedCgosConnectionProfile);
            _session.SetCgosAdminProcessStatus(status, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _session.SetCgosAdminProcessStatus("ERROR: " + ex.Message, false, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
    }

    /// <summary>
    /// CGOS の Admin・プレイヤー1・プレイヤー2をすべて切断します。
    /// </summary>
    private async Task StopCgosPlayerConnectionProcessAsync(GoStone stone, CgosConnectionProcess process)
    {
        try
        {
            await process.StopAsync();
            SetCgosPlayerConnectionProcessStatus(stone, "STOPPED", false, process);
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            SetCgosPlayerConnectionProcessStatus(stone, "ERROR: " + ex.Message, process.IsRunning, process);
        }
    }

    private async Task StopCgosAdminProcessAsync()
    {
        try
        {
            await _cgosAdminProcess.StopAsync();
            _session.SetCgosAdminProcessStatus("ADMIN STOPPED", false, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _session.SetCgosAdminProcessStatus("ERROR: " + ex.Message, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
    }

    private async Task DisconnectAllCgosProcessesAsync()
    {
        await Task.WhenAll(
            _cgosAdminProcess.StopAsync(),
            _cgosBlackConnectionProcess.StopAsync(),
            _cgosWhiteConnectionProcess.StopAsync());
        _session.SetCgosAdminProcessStatus("ADMIN STOPPED", false, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        _session.SetCgosBlackConnectionProcessStatus("STOPPED", false, _cgosBlackConnectionProcess.LogDirectory, _cgosBlackConnectionProcess.GetRecentOutput());
        _session.SetCgosWhiteConnectionProcessStatus("STOPPED", false, _cgosWhiteConnectionProcess.LogDirectory, _cgosWhiteConnectionProcess.GetRecentOutput());
    }

    private void UpdateCgosAdminProcessStatus()
    {
        var status = _cgosAdminProcess.RefreshStatus();
        _session.SetCgosAdminProcessStatus(status, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        _session.SetCgosAdminWaitingPlayers(_cgosAdminProcess.GetAdminWaitingPlayers());
    }

    private void UpdateCgosGameObservation()
    {
        var previousGameId = _cgosGameObservation.GameId;
        var wasFinished = _cgosGameObservation.IsFinished;

        foreach (var line in _cgosBlackConnectionProcess.DrainOutput())
        {
            if (_cgosGameObservation.ProcessLogLine(line)) PlayPlaceStoneSound();
        }

        foreach (var line in _cgosWhiteConnectionProcess.DrainOutput())
        {
            if (_cgosGameObservation.ProcessLogLine(line)) PlayPlaceStoneSound();
        }

        _session.SetCgosGameInProgress(
            _cgosGameObservation.IsStarted &&
            !_cgosGameObservation.IsFinished);

        if (_cgosGameObservation.IsStarted && _cgosGameObservation.GameId != previousGameId)
        {
            _session.ResetLiveChartAutoUpdate();
            _session.SetCgosResultSgfSaved(false);
            BeginCgosMatchNotification();
        }

        if (!wasFinished && _cgosGameObservation.IsFinished)
        {
            TryAutoSaveCgosGame();
            if (_cgosMatchNotificationMode == CgosMatchNotificationMode.None)
                _session.OpenCgosResultScreen();
        }
    }

    private void SendCgosPlayerResign(GoStone stone)
    {
        var process = stone == GoStone.Black ? _cgosBlackConnectionProcess : _cgosWhiteConnectionProcess;
        try
        {
            var status = process.SendCommand("resign");
            SetCgosPlayerConnectionProcessStatus(stone, status, process.IsRunning, process);
            GuiOperationLog.User("Requested CGOS player resignation", $"player={stone}");
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            SetCgosPlayerConnectionProcessStatus(stone, "ERROR: " + ex.Message, process.IsRunning, process);
        }
    }

    private void UpdateReviewMouseRepeat(MouseState mouse, Point point)
    {
        if (_session.CurrentMode.Kind is not (GoAppModeKind.Reviewing or GoAppModeKind.GameOver) ||
            mouse.LeftButton != ButtonState.Pressed)
        {
            _reviewMouseRepeatCommand = null;
            return;
        }

        if (_previousMouse.LeftButton != ButtonState.Pressed ||
            _reviewMouseRepeatCommand is not { } command ||
            BoardAndReviewScreen.Default.ReviewingRightSidePanel.GetStepButtonHit(point) != command)
        {
            return;
        }

        if (_inputClockSeconds < _reviewMouseNextRepeatAt) return;
        _reviewMouseNextRepeatAt = _inputClockSeconds + ReviewRepeatIntervalSeconds;
        ExecuteReviewNavigation(command);
    }

    private void HandleReviewChartPopupClick(Point point)
    {
        if (PopupTrendChartRenderer.GetReviewChartPopupCloseHit(point))
        {
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupScoreToggleHit(point))
        {
            _session.TogglePopupScoreVisibility();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupWinRateToggleHit(point))
        {
            _session.TogglePopupWinRateVisibility();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupCommentToggleHit(point))
        {
            _session.TogglePopupCommentVisibility();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentMoveStepButtonHit(point) is { } commentMoveStep)
        {
            TryMoveReviewAdjacentComment(commentMoveStep);
            return;
        }

        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentPageStepButtonHit(point) is { } commentPageStep)
        {
            _session.ChangeCommentPage(commentPageStep);
            return;
        }

        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentEditButtonHit(point))
        {
            OpenCommentEditor(_session, _session.ReviewMoveIndex);
            return;
        }

        // コメントの半透明パネルは前面要素。余白を押しても背面グラフへ入力を通さない。
        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.IsReviewChartPopupCommentOverlayHit(point))
        {
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupStepButtonHit(point) is { } popupStep)
        {
            ExecuteReviewNavigation(popupStep);
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupSeekMove(point, _session.ReviewMoveCount) is { } moveIndex)
        {
            MoveReview(moveIndex - _session.ReviewMoveIndex);
            var deltaX = point.X - _lastReviewPopupSeekClickPoint.X;
            var deltaY = point.Y - _lastReviewPopupSeekClickPoint.Y;
            var isDoubleClick =
                _inputClockSeconds - _lastReviewPopupSeekClickAt <= ReviewPopupDoubleClickSeconds &&
                deltaX * deltaX + deltaY * deltaY <=
                    ReviewPopupDoubleClickDistance * ReviewPopupDoubleClickDistance;
            _lastReviewPopupSeekClickAt = _inputClockSeconds;
            _lastReviewPopupSeekClickPoint = point;

            if (isDoubleClick)
            {
                _session.CloseReviewChartPopup();
                _reviewPopupSeekDragging = false;
                _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            }
            else
            {
                _reviewPopupSeekDragging = true;
            }
        }
    }

    private void HandleReadOnlyChartPopupClick(Point point)
    {
        if (PopupTrendChartRenderer.GetReviewChartPopupCloseHit(point))
        {
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupBackToLiveHit(point))
        {
            if (IsLocalPlayUseKind() &&
                _session.CurrentMode.Kind == GoAppModeKind.Playing &&
                _session.IsLocalReplayMode)
            {
                _session.ReturnLocalReplayToLive();
                _session.CloseReviewChartPopup();
                _reviewPopupSeekDragging = false;
                ResetReadOnlyChartPopupDoubleClick();
            }
            else if (_session.UseKind == GoAppUseKind.CgosClient &&
                     _session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching &&
                     _cgosGameObservation.IsReplayMode)
            {
                _cgosGameObservation.ReturnToLive();
                _session.CloseReviewChartPopup();
                _reviewPopupSeekDragging = false;
                ResetReadOnlyChartPopupDoubleClick();
            }
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupAutoUpdateHit(point))
        {
            var moveCount = _session.UseKind == GoAppUseKind.CgosClient
                ? _cgosGameObservation.MoveCount
                : _session.CurrentGameRecord.Moves.Count;
            _session.ToggleLiveChartAutoUpdate(moveCount);
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupScoreToggleHit(point))
        {
            _session.TogglePopupScoreVisibility();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupWinRateToggleHit(point))
        {
            _session.TogglePopupWinRateVisibility();
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupCommentToggleHit(point))
        {
            _session.TogglePopupCommentVisibility();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentMoveStepButtonHit(point) is { } commentMoveStep)
        {
            TrySeekReadOnlyAdjacentComment(commentMoveStep);
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentPageStepButtonHit(point) is { } commentPageStep)
        {
            _session.ChangeCommentPage(commentPageStep);
            return;
        }

        if (_session.IsPopupCommentVisible &&
            CanEditCompletedLocalGameComment() &&
            PopupTrendChartRenderer.GetReviewChartPopupCommentEditButtonHit(point))
        {
            OpenCommentEditor(_session, _session.LocalDisplayMoveIndex);
            return;
        }

        if (PopupTrendChartRenderer.GetReviewChartPopupStepButtonHit(point) is { } popupStep &&
            TryGetReadOnlyChartNavigation(out var currentMoveIndex, out var maximumMoveIndex))
        {
            var targetMoveIndex = popupStep switch
            {
                int.MinValue => 0,
                int.MaxValue => maximumMoveIndex,
                _ => Math.Clamp(currentMoveIndex + popupStep, 0, maximumMoveIndex),
            };
            SeekReadOnlyChartPopup(targetMoveIndex);
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
            PopupTrendChartRenderer.GetReviewChartPopupSeekMove(
                point,
                _cgosGameObservation.IsFinished
                    ? _cgosGameObservation.MoveCount
                    : _session.GetLiveChartVisibleMoveCount(_cgosGameObservation.MoveCount)) is { } moveIndex)
        {
            HandleReadOnlyChartPopupSeekClick(
                point,
                moveIndex);
            return;
        }

        if (IsLocalPlayUseKind() &&
            _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver &&
            PopupTrendChartRenderer.GetReviewChartPopupSeekMove(
                point,
                _session.CurrentMode.Kind == GoAppModeKind.GameOver
                    ? _session.CurrentGameRecord.Moves.Count
                    : _session.GetLiveChartVisibleMoveCount(_session.CurrentGameRecord.Moves.Count)) is { } localMoveIndex)
        {
            HandleReadOnlyChartPopupSeekClick(
                point,
                localMoveIndex);
        }
    }

    private void HandleReadOnlyChartPopupSeekClick(Point point, int moveIndex)
    {
        var deltaX = point.X - _lastReviewPopupSeekClickPoint.X;
        var deltaY = point.Y - _lastReviewPopupSeekClickPoint.Y;
        var firstMoveIndex = _lastReadOnlyChartPopupSeekMoveIndex;
        var isDoubleClick =
            firstMoveIndex is { } &&
            firstMoveIndex.Value < GetReadOnlyChartCurrentMoveCount() &&
            _inputClockSeconds - _lastReviewPopupSeekClickAt <= ReviewPopupDoubleClickSeconds &&
            deltaX * deltaX + deltaY * deltaY <=
                ReviewPopupDoubleClickDistance * ReviewPopupDoubleClickDistance;

        if (isDoubleClick)
        {
            SeekReadOnlyChartPopup(firstMoveIndex!.Value);
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            ResetReadOnlyChartPopupDoubleClick();
        }
        else
        {
            SeekReadOnlyChartPopup(moveIndex);
            _reviewPopupSeekDragging = true;
            _lastReviewPopupSeekClickAt = _inputClockSeconds;
            _lastReviewPopupSeekClickPoint = point;
            _lastReadOnlyChartPopupSeekMoveIndex = moveIndex;
        }
    }

    private void SeekReadOnlyChartPopup(int moveIndex)
    {
        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
        {
            _cgosGameObservation.SeekReplay(moveIndex);
        }
        else if (IsLocalPlayUseKind() &&
                 _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver)
        {
            if (_session.CurrentMode.Kind == GoAppModeKind.GameOver)
                _session.SeekLocalReviewTimeline(moveIndex);
            else
                _session.SeekLocalReplay(moveIndex);
        }
    }

    private bool TrySeekReadOnlyAdjacentComment(int direction)
    {
        if (!TryGetReadOnlyChartNavigation(out var currentMoveIndex, out var maximumMoveIndex))
        {
            return false;
        }

        IReadOnlyList<GoGameMove> moves;
        if (_session.UseKind == GoAppUseKind.CgosClient)
        {
            moves = _cgosGameObservation.Moves;
        }
        else
        {
            moves = _session.CurrentGameRecord.Moves;
        }

        var targetMoveNumber = MoveCommentNavigator.FindAdjacent(
            moves,
            currentMoveIndex,
            direction,
            maximumMoveIndex);
        if (targetMoveNumber is not { } target)
        {
            return false;
        }

        SeekReadOnlyChartPopup(target);
        _session.ResetCommentPage();
        return true;
    }

    private bool TryMoveReviewAdjacentComment(int direction)
    {
        var targetMoveNumber = MoveCommentNavigator.FindAdjacent(
            _session.ReviewMoves,
            _session.ReviewMoveIndex,
            direction,
            _session.ReviewMoveCount);
        if (targetMoveNumber is not { } target)
        {
            return false;
        }

        MoveReview(target - _session.ReviewMoveIndex);
        return true;
    }

    private int GetReadOnlyChartCurrentMoveCount() =>
        _session.UseKind == GoAppUseKind.CgosClient
            ? _cgosGameObservation.MoveCount
            : _session.CurrentGameRecord.Moves.Count;

    private bool TryGetReadOnlyChartNavigation(
        out int currentMoveIndex,
        out int maximumMoveIndex)
    {
        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
        {
            currentMoveIndex = _cgosGameObservation.DisplayMoveIndex;
            maximumMoveIndex = _cgosGameObservation.IsFinished
                ? _cgosGameObservation.MoveCount
                : _session.GetLiveChartVisibleMoveCount(_cgosGameObservation.MoveCount);
            return true;
        }

        if (IsLocalPlayUseKind() &&
            _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver)
        {
            currentMoveIndex = _session.CurrentMode.Kind == GoAppModeKind.GameOver
                ? _session.LocalReviewTimelineIndex
                : _session.LocalDisplayMoveIndex;
            maximumMoveIndex = _session.CurrentMode.Kind == GoAppModeKind.GameOver
                ? _session.LocalReviewTimelineMaximum
                : _session.GetLiveChartVisibleMoveCount(_session.CurrentGameRecord.Moves.Count);
            return true;
        }

        currentMoveIndex = 0;
        maximumMoveIndex = 0;
        return false;
    }

    private LiveBoardPreviewModel? CreateLiveBoardPreview()
    {
        if (_variationSession is null)
            return null;

        if (IsLocalPlayUseKind() &&
            _session.CurrentMode.Kind == GoAppModeKind.Playing)
        {
            var moves = _session.CurrentGameRecord.Moves;
            return new LiveBoardPreviewModel(
                _session.BoardSize,
                _session.GetStone,
                moves.Count == 0 ? null : moves[^1],
                moves.Count,
                _session.GetLocalPlayerName(GoStone.Black),
                _session.GetLocalPlayerName(GoStone.White));
        }

        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching &&
            _cgosGameObservation.IsStarted &&
            !_cgosGameObservation.IsFinished)
        {
            return new LiveBoardPreviewModel(
                _cgosGameObservation.BoardSize,
                _cgosGameObservation.GetLiveStone,
                _cgosGameObservation.LatestMove,
                _cgosGameObservation.MoveCount,
                _cgosGameObservation.BlackPlayerName,
                _cgosGameObservation.WhitePlayerName);
        }

        return null;
    }

    private void ResetReadOnlyChartPopupDoubleClick()
    {
        _lastReviewPopupSeekClickAt = double.NegativeInfinity;
        _lastReadOnlyChartPopupSeekMoveIndex = null;
    }

    private void UpdateReviewPopupSeekDrag(MouseState mouse, Point point)
    {
        if (_session.ActiveWindowId != ActiveWindowId.ReviewChartPopup ||
            !_session.IsReviewChartPopupOpen || mouse.LeftButton != ButtonState.Pressed)
        {
            _reviewPopupSeekDragging = false;
            return;
        }

        if (!_reviewPopupSeekDragging)
        {
            return;
        }

        if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            if (PopupTrendChartRenderer.GetReviewChartPopupSeekMove(point, _session.ReviewMoveCount) is { } reviewMoveIndex &&
                reviewMoveIndex != _session.ReviewMoveIndex)
            {
                MoveReview(reviewMoveIndex - _session.ReviewMoveIndex);
            }
            return;
        }

        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
            PopupTrendChartRenderer.GetReviewChartPopupSeekMove(
                point,
                _cgosGameObservation.IsFinished
                    ? _cgosGameObservation.MoveCount
                    : _session.GetLiveChartVisibleMoveCount(_cgosGameObservation.MoveCount)) is { } cgosMoveIndex)
        {
            _cgosGameObservation.SeekReplay(cgosMoveIndex);
            return;
        }

        if (IsLocalPlayUseKind() &&
            _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver &&
            PopupTrendChartRenderer.GetReviewChartPopupSeekMove(
                point,
                _session.CurrentMode.Kind == GoAppModeKind.GameOver
                    ? _session.CurrentGameRecord.Moves.Count
                    : _session.GetLiveChartVisibleMoveCount(_session.CurrentGameRecord.Moves.Count)) is { } localMoveIndex)
        {
            _session.SeekLocalReplay(localMoveIndex);
        }
    }

    private bool IsLocalPlayUseKind() =>
        _session.UseKind is GoAppUseKind.LocalPlay or GoAppUseKind.LocalApps;

    private void BeginCgosMatchNotification()
    {
        if (_cgosMatchNotificationGameId == _cgosGameObservation.GameId)
            return;

        _cgosMatchNotificationGameId = _cgosGameObservation.GameId;
        _cgosMatchNotificationStartedAt = DateTimeOffset.UtcNow;
        _cgosMatchNotificationMode = IsCgosConnectionWaitingScreen()
            ? CgosMatchNotificationMode.Countdown
            : CgosMatchNotificationMode.Deferred;
        _session.ActivateModalWindow(ActiveWindowId.CgosMatchNotification);
        PlayUpcomingMatchChime();
        GuiOperationLog.App(
            "CGOS match notification opened",
            $"gameId={_cgosGameObservation.GameId} mode={_cgosMatchNotificationMode}");
    }

    private void UpdateCgosMatchNotification()
    {
        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.None)
            return;

        if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching &&
            _cgosMatchNotificationGameId == _cgosGameObservation.GameId)
        {
            CloseCgosMatchNotification();
            return;
        }

        if (_cgosGameObservation.IsFinished &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
        {
            CloseCgosMatchNotification();
            return;
        }

        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.Countdown &&
            !IsCgosConnectionWaitingScreen())
        {
            DeferCgosMatchNotification("Left CGOS connection waiting screen");
            return;
        }

        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.Countdown &&
            GetCgosMatchNotificationAge().TotalSeconds >= CgosMatchCountdownSeconds)
        {
            OpenNotifiedCgosMatch("Countdown completed");
        }
    }

    private bool TryHandleCgosMatchNotificationClick(Point point)
    {
        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.None)
            return false;

        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.Deferred)
        {
            if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching)
                return CgosMatchNotification.IsDeferredBannerHit(point);

            if (!CgosMatchNotification.Default.DeferredWatchButton.IsHit(point))
                return CgosMatchNotification.IsDeferredBannerHit(point);

            OpenNotifiedCgosMatch("Pressed deferred match notification");
            return true;
        }

        var buttonsEnabled =
            GetCgosMatchNotificationAge().TotalSeconds >= CgosMatchButtonDelaySeconds;
        CgosMatchNotification.Default.WatchNowButton.IsEnabled = buttonsEnabled;
        CgosMatchNotification.Default.WatchLaterButton.IsEnabled = buttonsEnabled;
        if (CgosMatchNotification.Default.WatchNowButton.IsHit(point))
        {
            OpenNotifiedCgosMatch("Pressed WATCH NOW");
            return true;
        }

        if (CgosMatchNotification.Default.WatchLaterButton.IsHit(point))
        {
            DeferCgosMatchNotification("Pressed WATCH LATER");
            return true;
        }

        // The visible banner consumes clicks so controls behind it cannot be activated.
        return new Rectangle(460, 28, 1000, 116).Contains(point);
    }

    private void OpenNotifiedCgosMatch(string reason)
    {
        GuiOperationLog.User(
            _cgosGameObservation.IsFinished ? "Opened notified CGOS result" : "Opened notified CGOS match",
            $"gameId={_cgosGameObservation.GameId} reason={reason}");
        CloseCgosMatchNotification();
        if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
            _session.ReturnFromReviewingToResting();
        if (_cgosGameObservation.IsFinished)
            _session.OpenCgosResultScreen();
        else
            _session.OpenCgosWatchingScreen();
    }

    private void DeferCgosMatchNotification(string reason)
    {
        if (_cgosMatchNotificationMode == CgosMatchNotificationMode.None)
            return;

        _cgosMatchNotificationMode = CgosMatchNotificationMode.Deferred;
        _cgosMatchNotificationStartedAt = DateTimeOffset.UtcNow;
        GuiOperationLog.User(
            "Deferred CGOS match notification",
            $"gameId={_cgosGameObservation.GameId} reason={reason}");
    }

    private void CloseCgosMatchNotification()
    {
        _cgosMatchNotificationMode = CgosMatchNotificationMode.None;
        _session.DeactivateModalWindow(ActiveWindowId.CgosMatchNotification);
    }

    private void RestoreCgosMatchNotificationAfterLeavingView()
    {
        if (!_cgosGameObservation.IsStarted || _cgosGameObservation.IsFinished)
            return;

        _cgosMatchNotificationGameId = _cgosGameObservation.GameId;
        _cgosMatchNotificationMode = CgosMatchNotificationMode.Deferred;
        _session.ActivateModalWindow(ActiveWindowId.CgosMatchNotification);
        _cgosMatchNotificationStartedAt = DateTimeOffset.UtcNow;
        GuiOperationLog.User(
            "Restored CGOS match notification after leaving view",
            $"gameId={_cgosGameObservation.GameId}");
    }

    private bool IsCgosConnectionWaitingScreen() =>
        !_isApplicationSettingsOpen &&
        _variationSession is null &&
        _session.CurrentMode.Kind != GoAppModeKind.Reviewing &&
        _session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart &&
        !_session.IsGtpEngineSelectionDialogOpen &&
        !_session.IsGtpEngineEditPanelOpen &&
        !_session.IsCgosAdminPlayerSelectionDialogOpen;

    private TimeSpan GetCgosMatchNotificationAge() =>
        DateTimeOffset.UtcNow - _cgosMatchNotificationStartedAt;

    private static int GetCgosMatchSecondsRemaining(TimeSpan age) =>
        Math.Max(0, (int)Math.Ceiling(CgosMatchCountdownSeconds - age.TotalSeconds));

    private void SendSelectedCgosAdminMatch()
    {
        SendCgosAdminCommand($"match {_session.CgosAdminWhitePlayerName} {_session.CgosAdminBlackPlayerName}");
    }

    private void SendCgosAdminCommand(string command)
    {
        try
        {
            var status = _cgosAdminProcess.SendCommand(command);
            _session.SetCgosAdminProcessStatus(status, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _session.SetCgosAdminProcessStatus("ERROR: " + ex.Message, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
    }

    private void OpenCgosAdminLog()
    {
        try
        {
            var status = _cgosAdminProcess.OpenLog("code", openStandardError: false);
            _session.SetCgosAdminProcessStatus(status, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _session.SetCgosAdminProcessStatus("ERROR: " + ex.Message, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
    }

    private void TailCgosAdminLog()
    {
        try
        {
            var status = _cgosAdminProcess.TailLogWithPowerShell(openStandardError: false);
            _session.SetCgosAdminProcessStatus(status, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
        catch (Exception ex) when (ex is InvalidOperationException or IOException or System.ComponentModel.Win32Exception)
        {
            _session.SetCgosAdminProcessStatus("ERROR: " + ex.Message, _cgosAdminProcess.IsRunning, _cgosAdminProcess.LogDirectory, _cgosAdminProcess.GetRecentOutput());
        }
    }

    private bool TryInputCgosConnectionEditCharacter(char character)
    {
        if (!_session.IsCgosConnectionEditPanelOpen || _session.ActiveCgosConnectionEditField is not { } field)
        {
            return false;
        }

        if (!_cgosConnectionEditTextBox.TryInputCharacter(character))
        {
            _session.SetCgosConnectionEditWarning("Text is too long.");
            return true;
        }

        SyncCgosConnectionEditField(field);
        UpdateCgosConnectionEditWarning();
        return true;
    }

    private void BeginOrMoveCgosConnectionEditField(Point point, CgosConnectionProfileEditField field)
    {
        var text = _session.ActiveCgosConnectionEditField == field
            ? _cgosConnectionEditTextBox.Text
            : _session.GetCgosConnectionEditFieldText(field);
        var caretIndex = _presentationServices?.Presentation.GetCgosConnectionEditPanelCaretIndex(point, field, text) ?? text.Length;

        if (_session.ActiveCgosConnectionEditField == field)
        {
            _cgosConnectionEditTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            SyncCgosConnectionEditField(field);
            return;
        }

        _cgosConnectionEditTextBox.Begin(text, caretIndex);
        _cgosConnectionEditTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        SyncCgosConnectionEditField(field);
        _session.BeginCgosConnectionEditField(field, _cgosConnectionEditTextBox.CaretIndex);
        UpdateCgosConnectionEditWarning();
    }

    private void MoveCgosConnectionEditFocus(int step)
    {
        var fields = Enum.GetValues<CgosConnectionProfileEditField>();
        var currentIndex = _session.ActiveCgosConnectionEditField is { } current
            ? Array.IndexOf(fields, current)
            : step > 0 ? -1 : 0;
        EndCgosConnectionEditField();
        BeginCgosConnectionEditField(fields[(currentIndex + step + fields.Length) % fields.Length]);
    }

    private void BeginCgosConnectionEditField(CgosConnectionProfileEditField field)
    {
        var text = _session.GetCgosConnectionEditFieldText(field);
        _cgosConnectionEditTextBox.Begin(text);
        _session.BeginCgosConnectionEditField(field, _cgosConnectionEditTextBox.CaretIndex);
        SyncCgosConnectionEditField(field);
        UpdateCgosConnectionEditWarning();
    }

    private void SyncCgosConnectionEditField(CgosConnectionProfileEditField field)
    {
        _session.SetCgosConnectionEditField(field, _cgosConnectionEditTextBox.Text, _cgosConnectionEditTextBox.CaretIndex);
        _session.SetCgosConnectionEditSelection(_cgosConnectionEditTextBox.SelectionStart, _cgosConnectionEditTextBox.SelectionLength);
    }

    private void EndCgosConnectionEditField()
    {
        if (_session.ActiveCgosConnectionEditField is not { })
        {
            return;
        }

        _session.EndCgosConnectionEditField();
        _cgosConnectionEditTextBox.Clear();
    }

    private void CancelCgosConnectionEditField(CgosConnectionProfileEditField field)
    {
        _cgosConnectionEditTextBox.Begin(_session.GetCgosConnectionEditFieldText(field));
        _session.EndCgosConnectionEditField();
        _cgosConnectionEditTextBox.Clear();
    }

    private bool SaveCgosConnectionEditDraft()
    {
        EndCgosConnectionEditField();
        if (!ValidateCgosConnectionEditDraft(out var profile, out var warning))
        {
            _session.SetCgosConnectionEditWarning(warning);
            return false;
        }

        _session.SaveCgosConnectionEditDraft(profile);
        _cgosConnectionCatalog.Save(_session.CgosConnectionProfiles);
        return true;
    }

    private void CloseCgosConnectionEditPanel()
    {
        _cgosConnectionEditTextBox.Clear();
        _session.CloseCgosConnectionEditPanel();
    }

    private bool ValidateCgosConnectionEditDraft(out CgosConnectionProfile profile, out string warning)
    {
        var draft = _session.CgosConnectionEditDraft;
        profile = draft with
        {
            DisplayName = draft.DisplayName.Trim(),
            Host = draft.Host.Trim(),
            Event = draft.Event.Trim(),
            Round = draft.Round.Trim(),
            Note = draft.Note.Trim(),
        };

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            warning = "Display name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(profile.Host))
        {
            warning = "Host is required.";
            return false;
        }

        if (!int.TryParse(_session.CgosConnectionPortDraft.Trim(), out var port) || port < 1 || port > 65535)
        {
            warning = "Port must be 1-65535.";
            return false;
        }

        profile = profile with { Port = port };
        warning = "";
        return true;
    }

    private void UpdateCgosConnectionEditWarning()
    {
        if (string.IsNullOrWhiteSpace(_session.CgosConnectionEditDraft.DisplayName))
        {
            _session.SetCgosConnectionEditWarning("Display name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_session.CgosConnectionEditDraft.Host))
        {
            _session.SetCgosConnectionEditWarning("Host is required.");
            return;
        }

        if (!int.TryParse(_session.CgosConnectionPortDraft.Trim(), out var port) || port < 1 || port > 65535)
        {
            _session.SetCgosConnectionEditWarning("Port must be 1-65535.");
            return;
        }

        _session.SetCgosConnectionEditWarning("");
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (_isCommentEditorOpen)
        {
            _commentTextArea.TryInputCharacter(e.Character);
            return;
        }
        if (_activeGtpEngineIntegerOption is not null)
        {
            if (char.IsDigit(e.Character) || (e.Character == '-' && _gtpEngineIntegerOptionTextBox.CaretIndex == 0))
                _gtpEngineIntegerOptionTextBox.TryInputCharacter(e.Character);
            return;
        }
        if (_activeGtpEngineStringOption is not null)
        {
            if (!_gtpEngineStringOptionTextBox.TryInputCharacter(e.Character))
                _gtpEngineStringInputMessage = $"TEXT IS TOO LONG (MAX {GtpEngineGuiOptions.MaximumTextLength})";
            return;
        }
        if (!IsActive || !_inputArmed) return;

        // モーダル表示中の入力欄を、背後の画面に残った編集状態より優先します。
        if (_session.IsGtpEngineEditPanelOpen && TryInputGtpEngineEditCharacter(e.Character))
        {
            return;
        }

        if (TryInputPlayerEditCharacter(e.Character)) return;

        if (TryInputClientIdentityProfileEditCharacter(e.Character))
        {
            _receivedClientIdentityTextInput = true;
            return;
        }

        if (TryInputHumanPlayerNameCharacter(e.Character)) return;

        if (TryInputLocalMatchHandleCharacter(e.Character)) return;

        if (TryInputCgosCredentialCharacter(e.Character)) return;

        if (TryInputCgosConnectionEditCharacter(e.Character))
        {
            return;
        }

        if (TryInputGtpEngineEditCharacter(e.Character))
        {
            return;
        }

        _tournamentRulesSetting.TryInputCharacter(e.Character);
    }

    private void OnTextCompositionChanged(TextCompositionState composition)
    {
        if (_isCommentEditorOpen)
        {
            _commentEditorComposition = composition;
            return;
        }
        _gtpEngineStringComposition = _activeGtpEngineStringOption is null
            ? TextCompositionState.Empty
            : composition;
    }

    private void OnTextCompositionDiagnosticsChanged(TextCompositionDiagnostics diagnostics) =>
        _textCompositionDiagnostics = diagnostics;

    private void OpenCommentEditor(GoAppSession session, int moveIndex)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing && !session.HasReviewGameRecord)
            return;
        if (session.CurrentMode.Kind == GoAppModeKind.GameOver && !CanEditCompletedLocalGameComment())
            return;
        if (session.CurrentMode.Kind is not (GoAppModeKind.Reviewing or GoAppModeKind.VariationEditing or GoAppModeKind.GameOver))
            return;

        _commentEditorSession = session;
        _commentEditorMoveIndex = Math.Clamp(moveIndex, 0, session.CurrentGameRecord.Moves.Count);
        // レビューの 0 手目は、盤面復元用の CurrentGameRecord ではなく、SGF 全体を保持する
        // レビュー用レコードから読む。表示と編集ダイアログで参照先を一致させる。
        var initialComment =
            session.CurrentMode.Kind == GoAppModeKind.Reviewing && _commentEditorMoveIndex == 0
                ? session.ReviewRootComment
                : session.CurrentGameRecord.GetComment(_commentEditorMoveIndex);
        _commentTextArea.Begin(initialComment);
        _commentEditorInitialText = initialComment;
        _commentEditorComposition = TextCompositionState.Empty;
        _previousCommentEditorKeyboard = Keyboard.GetState();
        _isCommentEditorOpen = true;
        _session.ActivateModalWindow(ActiveWindowId.CommentEditor);
    }

    private void UpdateCommentEditorKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        var action = _commentTextArea.HandleKeyboard(
            keyboard,
            _previousCommentEditorKeyboard,
            gameTime,
            _clipboardService,
            multiline: true);
        if (action == TextBoxKeyboardAction.Commit)
            CommitCommentEditor(saveToFile: true);
        _previousCommentEditorKeyboard = keyboard;
    }

    private void CommitCommentEditor(bool saveToFile = false)
    {
        var saved = _commentEditorSession?.CurrentMode.Kind switch
        {
            GoAppModeKind.Reviewing => _commentEditorSession.TrySetReviewComment(_commentEditorMoveIndex, _commentTextArea.Text),
            GoAppModeKind.VariationEditing => _commentEditorSession.TrySetVariationComment(_commentEditorMoveIndex, _commentTextArea.Text),
            GoAppModeKind.GameOver => _commentEditorSession.CurrentGameRecord.TrySetComment(_commentEditorMoveIndex, _commentTextArea.Text),
            _ => false,
        };
        if (saved)
        {
            GuiOperationLog.User("Applied SGF comment", $"move={_commentEditorMoveIndex}; characters={_commentTextArea.Text.Length}");
            if (saveToFile && _commentEditorSession is not null)
            {
                if (_commentEditorSession.CurrentMode.Kind == GoAppModeKind.GameOver)
                    SaveCompletedLocalGameCommentSgf();
                else if (SaveReviewSgf() && _commentEditorSession.CurrentMode.Kind == GoAppModeKind.Reviewing)
                    _commentEditorSession.MarkReviewCommentsSaved();
            }
        }
        CancelCommentEditor();
    }

    private void CancelCommentEditor()
    {
        _isCommentEditorOpen = false;
        _session.DeactivateModalWindow(ActiveWindowId.CommentEditor);
        _commentEditorMoveIndex = 0;
        _commentEditorSession = null;
        _commentEditorComposition = TextCompositionState.Empty;
        _commentEditorInitialText = "";
        _commentTextArea.Clear();
    }

    private void BeginReviewExit(ReviewExitAction action)
    {
        if (_session.HasUnsavedReviewCommentChanges)
        {
            _pendingReviewExitAction = action;
            _isReviewUnsavedChangesConfirmationOpen = true;
            _session.ActivateModalWindow(ActiveWindowId.ReviewUnsavedChangesConfirmation);
            return;
        }
        CompleteReviewExit(action);
    }

    private void SavePendingReviewExit()
    {
        if (SaveReviewSgf())
        {
            _session.MarkReviewCommentsSaved();
            CompletePendingReviewExit(discardChanges: false);
        }
    }

    private void CompletePendingReviewExit(bool discardChanges)
    {
        if (discardChanges) _session.MarkReviewCommentsSaved();
        var action = _pendingReviewExitAction;
        CancelPendingReviewExit();
        if (action is { } value) CompleteReviewExit(value);
    }

    private void CancelPendingReviewExit()
    {
        _isReviewUnsavedChangesConfirmationOpen = false;
        _session.DeactivateModalWindow(ActiveWindowId.ReviewUnsavedChangesConfirmation);
        _pendingReviewExitAction = null;
    }

    private void CompleteReviewExit(ReviewExitAction action)
    {
        if (action == ReviewExitAction.UsePosition)
            _session.FinishReviewing();
        else
            _session.ReturnFromReviewingToResting();
    }

    private enum ReviewExitAction { BackToHome, UsePosition }

    private bool TryInputCgosCredentialCharacter(char character)
    {
        if (_session.CgosConnectionFlowKind != CgosConnectionFlowKind.ConnectionStart ||
            _session.IsGtpEngineSelectionDialogOpen ||
            _session.IsGtpEngineEditPanelOpen) return false;
        if (_session.ActiveCgosCredentialStone is not { } stone ||
            _session.ActiveCgosCredentialField is not { } field) return false;
        if (_cgosCredentialTextBox.TryInputCharacter(character))
        {
            _session.SetCgosCredential(stone, field, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
            SyncCgosCredentialSelection();
        }
        return true;
    }

    private void EndCgosCredentialEdit()
    {
        _session.EndCgosCredentialEdit();
        _cgosCredentialTextBox.Clear();
    }

    private void BeginOrMoveCgosCredentialEdit(Point point, GoStone stone, CgosPlayerCredentialField field)
    {
        var text = _session.ActiveCgosCredentialStone == stone && _session.ActiveCgosCredentialField == field
            ? _cgosCredentialTextBox.Text
            : _session.GetCgosCredential(stone, field);
        var caret = _presentationServices?.Presentation.GetCgosCredentialCaretIndex(point, stone, field, text) ?? text.Length;
        if (_session.ActiveCgosCredentialStone != stone || _session.ActiveCgosCredentialField != field)
            _cgosCredentialTextBox.Begin(text, caret);
        _cgosCredentialTextBox.BeginMouseSelection(caret, IsShiftDown());
        _session.BeginCgosCredentialEdit(stone, field, _cgosCredentialTextBox.CaretIndex);
        SyncCgosCredentialSelection();
    }

    private void UpdateCgosCredentialByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;

        if (_session.ActiveCgosCredentialStone is { } activeStone &&
            _session.ActiveCgosCredentialField is { } activeField &&
            keyboard.IsKeyDown(Keys.Tab) && _previousCgosCredentialKeyboard.IsKeyUp(Keys.Tab))
        {
            MoveCgosCredentialFocus(activeStone, activeField, IsShiftDown(keyboard) ? -1 : 1);
            _previousCgosCredentialKeyboard = keyboard;
            return;
        }

        if (_session.ActiveCgosCredentialStone is not { } stone ||
            _session.ActiveCgosCredentialField is not { } field)
        {
            _previousCgosCredentialKeyboard = keyboard;
            return;
        }

        switch (_cgosCredentialTextBox.HandleKeyboard(
                    keyboard,
                    _previousCgosCredentialKeyboard,
                    gameTime,
                    _clipboardService,
                    allowClipboardExport: field != CgosPlayerCredentialField.Password))
        {
            case TextBoxKeyboardAction.Commit:
            case TextBoxKeyboardAction.Cancel:
                EndCgosCredentialEdit();
                break;
            default:
                _session.SetCgosCredential(stone, field, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
                SyncCgosCredentialSelection();
                break;
        }
        _previousCgosCredentialKeyboard = keyboard;
    }

    private void MoveCgosCredentialFocus(GoStone stone, CgosPlayerCredentialField field, int step)
    {
        var stops = new[]
        {
            (GoStone.Black, CgosPlayerCredentialField.LoginName),
            (GoStone.Black, CgosPlayerCredentialField.Password),
            (GoStone.White, CgosPlayerCredentialField.LoginName),
            (GoStone.White, CgosPlayerCredentialField.Password),
        };
        var currentIndex = Array.FindIndex(stops, stop => stop.Item1 == stone && stop.Item2 == field);
        EndCgosCredentialEdit();
        var next = stops[(currentIndex + step + stops.Length) % stops.Length];
        var text = _session.GetCgosCredential(next.Item1, next.Item2);
        _cgosCredentialTextBox.Begin(text);
        _session.BeginCgosCredentialEdit(next.Item1, next.Item2, _cgosCredentialTextBox.CaretIndex);
        SyncCgosCredentialSelection();
    }

    private void BeginHumanPlayerNameEdit(Point point, GoStone stone)
    {
        var text = _session.ActiveHumanPlayerNameStone == stone
            ? _humanPlayerNameTextBox.Text
            : _session.GetHumanPlayerName(stone);
        var caretIndex = _presentationServices is null ? text.Length : LocalMatchScreen.Default.GetHumanPlayerNameCaretIndex(_presentationServices.Stationery, point, stone, text, _session.UseKind == GoAppUseKind.LocalApps);
        if (_session.ActiveHumanPlayerNameStone == stone)
        {
            _humanPlayerNameTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            _session.SetHumanPlayerNameDraft(text, caretIndex);
            return;
        }

        _humanPlayerNameTextBox.Begin(text, caretIndex);
        _humanPlayerNameTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        _session.BeginHumanPlayerNameEdit(stone, caretIndex);
    }

    private void UpdateHumanPlayerNameTextBox(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;

        if (_session.ActiveHumanPlayerNameStone is null)
        {
            _previousHumanPlayerNameKeyboard = keyboard;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousHumanPlayerNameKeyboard.IsKeyUp(Keys.Tab))
        {
            MoveHumanPlayerNameFocus(IsShiftDown(keyboard) ? -1 : 1);
            _previousHumanPlayerNameKeyboard = keyboard;
            return;
        }

        var action = _humanPlayerNameTextBox.HandleKeyboard(
            keyboard,
            _previousHumanPlayerNameKeyboard,
            gameTime,
            _clipboardService);
        _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
        _session.SetHumanPlayerNameSelection(_humanPlayerNameTextBox.SelectionStart, _humanPlayerNameTextBox.SelectionLength);
        if (action == TextBoxKeyboardAction.Commit) EndHumanPlayerNameEdit(commit: true);
        if (action == TextBoxKeyboardAction.Cancel) EndHumanPlayerNameEdit(commit: false);
        _previousHumanPlayerNameKeyboard = keyboard;
    }

    private void MoveHumanPlayerNameFocus(int step)
    {
        var stops = new[] { GoStone.Black, GoStone.White }
            .Where(stone => _session.GetPlayerKind(stone) == GoPlayerKind.Human)
            .ToArray();
        if (stops.Length == 0 || _session.ActiveHumanPlayerNameStone is not { } current)
        {
            return;
        }

        var currentIndex = Array.IndexOf(stops, current);
        EndHumanPlayerNameEdit(commit: true);
        var next = stops[(currentIndex + step + stops.Length) % stops.Length];
        var text = _session.GetHumanPlayerName(next);
        _humanPlayerNameTextBox.Begin(text);
        _session.BeginHumanPlayerNameEdit(next, _humanPlayerNameTextBox.CaretIndex);
    }

    private bool TryInputHumanPlayerNameCharacter(char character)
    {
        if (_session.ActiveHumanPlayerNameStone is null) return false;
        if (!_humanPlayerNameTextBox.TryInputCharacter(character)) return true;
        _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
        _session.SetHumanPlayerNameSelection(_humanPlayerNameTextBox.SelectionStart, _humanPlayerNameTextBox.SelectionLength);
        return true;
    }

    private void BeginOrMoveLocalMatchHandleEdit(Point point, GoStone stone)
    {
        var text = _session.ActiveLocalMatchHandleStone == stone
            ? _localMatchHandleTextBox.Text
            : _session.GetLocalMatchHandleDraft(stone);
        var caretIndex = _presentationServices is null ? text.Length : EntryProfilesPresenter.Default.GetLocalMatchHandleCaretIndex(_presentationServices.Stationery, point, stone, text, _session.UseKind == GoAppUseKind.LocalApps);
        if (_session.ActiveLocalMatchHandleStone != stone)
            _localMatchHandleTextBox.Begin(text, caretIndex);
        _localMatchHandleTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
        _session.BeginLocalMatchHandleEdit(stone, _localMatchHandleTextBox.CaretIndex);
        SyncLocalMatchHandleDraft();
    }

    private void UpdateLocalMatchHandleTextBox(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;
        if (_session.ActiveLocalMatchHandleStone is null)
        {
            _previousLocalMatchHandleKeyboard = keyboard;
            return;
        }

        var action = _localMatchHandleTextBox.HandleKeyboard(
            keyboard,
            _previousLocalMatchHandleKeyboard,
            gameTime,
            _clipboardService);
        SyncLocalMatchHandleDraft();
        if (action is TextBoxKeyboardAction.Commit or TextBoxKeyboardAction.Cancel)
        {
            _session.EndLocalMatchHandleEdit();
            _localMatchHandleTextBox.Clear();
        }
        _previousLocalMatchHandleKeyboard = keyboard;
    }

    private bool TryInputLocalMatchHandleCharacter(char character)
    {
        if (_session.ActiveLocalMatchHandleStone is null) return false;
        if (_localMatchHandleTextBox.TryInputCharacter(character))
            SyncLocalMatchHandleDraft();
        return true;
    }

    private void SyncLocalMatchHandleDraft() =>
        _session.SetLocalMatchHandleDraft(
            _localMatchHandleTextBox.Text,
            _localMatchHandleTextBox.CaretIndex,
            _localMatchHandleTextBox.SelectionStart,
            _localMatchHandleTextBox.SelectionLength);

    private void BeginOrMovePlayerEditField(Point point, EntryProfileEditField field)
    {
        var text = _session.ActivePlayerEditField == field
            ? _playerEditTextBox.Text
            : _session.GetPlayerEditFieldText(field);
        var caretIndex = _presentationServices is null ? text.Length : EditEntryProfile.Default.GetCaretIndex(_presentationServices.Stationery, point, field, text);
        if (_session.ActivePlayerEditField == field)
        {
            _playerEditTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            SyncPlayerEditField(field);
            return;
        }

        _playerEditTextBox.Begin(text, caretIndex);
        _playerEditTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        _session.BeginPlayerEditField(field, _playerEditTextBox.CaretIndex);
    }

    private void UpdatePlayerEditTextBox(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;
        if (!_session.IsPlayerEditPanelOpen || _session.ActivePlayerEditField is not { } field)
        {
            _previousPlayerEditKeyboard = keyboard;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousPlayerEditKeyboard.IsKeyUp(Keys.Tab))
        {
            MovePlayerEditFocus(field, IsShiftDown(keyboard) ? -1 : 1);
            _previousPlayerEditKeyboard = keyboard;
            return;
        }

        switch (_playerEditTextBox.HandleKeyboard(keyboard, _previousPlayerEditKeyboard, gameTime, _clipboardService))
        {
            case TextBoxKeyboardAction.Commit:
                _session.EndPlayerEditField();
                _playerEditTextBox.Clear();
                break;
            case TextBoxKeyboardAction.Cancel:
                _session.CancelPlayerEditField();
                _playerEditTextBox.Clear();
                break;
            default:
                SyncPlayerEditField(field);
                break;
        }
        _previousPlayerEditKeyboard = keyboard;
    }

    private void MovePlayerEditFocus(EntryProfileEditField field, int step)
    {
        var fields = new[] { EntryProfileEditField.DisplayName, EntryProfileEditField.ClientIdentityHandle, EntryProfileEditField.ClientIdentityPassword };
        var index = Array.IndexOf(fields, field);
        var next = fields[(index + step + fields.Length) % fields.Length];
        var text = _session.GetPlayerEditFieldText(next);
        _playerEditTextBox.Begin(text);
        _session.BeginPlayerEditField(next, _playerEditTextBox.CaretIndex);
    }

    private bool TryInputPlayerEditCharacter(char character)
    {
        if (!_session.IsPlayerEditPanelOpen || _session.ActivePlayerEditField is not { } field)
            return false;
        if (_playerEditTextBox.TryInputCharacter(character))
            SyncPlayerEditField(field);
        return true;
    }

    private void SyncPlayerEditField(EntryProfileEditField field) =>
        _session.SetPlayerEditFieldText(
            field,
            _playerEditTextBox.Text,
            _playerEditTextBox.CaretIndex,
            _playerEditTextBox.SelectionStart,
            _playerEditTextBox.SelectionLength);

    private void BeginOrMoveClientIdentityProfileEditField(Point point, ClientIdentityProfileEditField field)
    {
        var text = _session.ActiveClientIdentityProfileEditField == field
            ? _targetProfileEditTextBox.Text
            : _session.GetClientIdentityProfileEditField(field);
        var caretIndex = _presentationServices is null ? text.Length : EntryProfilesPresenter.Default.GetClientIdentityProfileEditCaretIndex(_presentationServices.Stationery, point, _session.ClientIdentityProfileEditIndex, field, text, false);
        if (_session.ActiveClientIdentityProfileEditField == field)
        {
            _targetProfileEditTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            SyncClientIdentityProfileEditField(field);
            return;
        }

        SaveClientIdentityProfileEditDraft();
        _targetProfileEditTextBox.Begin(text, caretIndex);
        _targetProfileEditTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        _session.BeginClientIdentityProfileEditField(field, _targetProfileEditTextBox.CaretIndex);
    }

    private void UpdateClientIdentityProfileEditTextBox(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;
        if (!_session.IsClientIdentityProfileEditPanelOpen || _session.ActiveClientIdentityProfileEditField is not { } field)
        {
            _previousClientIdentityProfileEditKeyboard = keyboard;
            _receivedClientIdentityTextInput = false;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousClientIdentityProfileEditKeyboard.IsKeyUp(Keys.Tab))
        {
            MoveClientIdentityProfileEditFocus(field, IsShiftDown(keyboard) ? -1 : 1);
            _previousClientIdentityProfileEditKeyboard = keyboard;
            return;
        }

        switch (_targetProfileEditTextBox.HandleKeyboard(keyboard, _previousClientIdentityProfileEditKeyboard, gameTime, _clipboardService))
        {
            case TextBoxKeyboardAction.Commit:
                SyncClientIdentityProfileEditField(field);
                SaveClientIdentityProfileEditDraft();
                _session.EndClientIdentityProfileEditField();
                _targetProfileEditTextBox.Clear();
                break;
            case TextBoxKeyboardAction.Cancel:
                _session.CancelClientIdentityProfileEditField();
                _targetProfileEditTextBox.Clear();
                break;
            default:
                SyncClientIdentityProfileEditField(field);
                if (!_receivedClientIdentityTextInput &&
                    TryGetKeyboardTextInputFallback(keyboard, _previousClientIdentityProfileEditKeyboard, out var character))
                {
                    TryInputClientIdentityProfileEditCharacter(character);
                    GuiOperationLog.App("Text input fallback", $"field={field}; character={character}");
                }
                break;
        }
        _receivedClientIdentityTextInput = false;
        _previousClientIdentityProfileEditKeyboard = keyboard;
    }

    /// <summary>
    /// SDL/MonoGame の TextInput イベントが届かない環境でも、英数字のプロフィール入力を継続できるようにします。
    /// 文字イベントを受信したフレームでは呼び出されないため、通常環境での二重入力は防止されます。
    /// </summary>
    private static bool TryGetKeyboardTextInputFallback(KeyboardState keyboard, KeyboardState previousKeyboard, out char character)
    {
        var shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
        for (var key = Keys.A; key <= Keys.Z; key++)
        {
            if (keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key))
            {
                character = (char)((shift ? 'A' : 'a') + (key - Keys.A));
                return true;
            }
        }

        if (keyboard.IsKeyDown(Keys.Space) && previousKeyboard.IsKeyUp(Keys.Space))
        {
            character = ' ';
            return true;
        }

        character = default;
        return false;
    }

    private void MoveClientIdentityProfileEditFocus(ClientIdentityProfileEditField field, int step)
    {
        SyncClientIdentityProfileEditField(field);
        SaveClientIdentityProfileEditDraft();
        var fields = new[] { ClientIdentityProfileEditField.LoginName, ClientIdentityProfileEditField.LoginPass };
        var index = Array.IndexOf(fields, field);
        var next = fields[(index + step + fields.Length) % fields.Length];
        var text = _session.GetClientIdentityProfileEditField(next);
        _targetProfileEditTextBox.Begin(text);
        _session.BeginClientIdentityProfileEditField(next, _targetProfileEditTextBox.CaretIndex);
    }

    private bool TryInputClientIdentityProfileEditCharacter(char character)
    {
        if (!_session.IsClientIdentityProfileEditPanelOpen || _session.ActiveClientIdentityProfileEditField is not { } field)
            return false;
        if (_targetProfileEditTextBox.TryInputCharacter(character))
            SyncClientIdentityProfileEditField(field);
        return true;
    }

    private void SyncClientIdentityProfileEditField(ClientIdentityProfileEditField field) =>
        _session.SetClientIdentityProfileEditFieldText(
            field,
            _targetProfileEditTextBox.Text,
            _targetProfileEditTextBox.CaretIndex,
            _targetProfileEditTextBox.SelectionStart,
            _targetProfileEditTextBox.SelectionLength);

    private void SaveClientIdentityProfileEditDraft()
    {
        _session.SaveClientIdentityProfileEditDraft();
        var profiles = _session.ClientIdentityProfiles.Select(profile => profile.Clone()).ToArray();
        BeginCatalogSave("SAVING CLIENT IDENTITIES...", () => _targetCatalog.Save(profiles));
    }

    /// <summary>
    /// Player の Target 参照と Target 本体を同じ UI 操作で保存する。
    /// Target を先に書くことで、追加時に Player が未保存の Target を参照する状態を避ける。
    /// </summary>
    private void SavePlayerAndClientIdentityCatalogs()
    {
        var clientIdentities = _session.ClientIdentityProfiles.Select(profile => profile.Clone()).ToArray();
        var players = _session.EntryProfiles.Select(profile => profile.Clone()).ToArray();
        BeginCatalogSave("SAVING PLAYER SETTINGS...", () =>
        {
            _targetCatalog.Save(clientIdentities);
            _playerCatalog.Save(players);
        });
    }

    private void SavePlayerCatalog(IEnumerable<EntryProfile> profiles)
    {
        var snapshot = profiles.Select(profile => profile.Clone()).ToArray();
        BeginCatalogSave("SAVING PLAYER SETTINGS...", () => _playerCatalog.Save(snapshot));
    }

    private bool IsCatalogSaveInProgress => _catalogSaveTask is { IsCompleted: false };

    private void BeginCatalogSave(string message, Action save)
    {
        if (IsCatalogSaveInProgress)
            return;

        _catalogSaveMessage = message;
        _catalogSaveTask = Task.Run(save);
    }

    private void CompleteCatalogSave()
    {
        if (_catalogSaveTask is not { IsCompleted: true } saveTask)
            return;

        try
        {
            saveTask.GetAwaiter().GetResult();
            GuiOperationLog.App("Saved catalog", _catalogSaveMessage);
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("CATALOG SAVE", "Could not save catalog settings.", ex);
        }
        finally
        {
            _catalogSaveTask = null;
            _catalogSaveMessage = "";
        }
    }

    private void EndHumanPlayerNameEdit(bool commit)
    {
        if (_session.ActiveHumanPlayerNameStone is null) return;
        if (commit)
            _session.CommitHumanPlayerNameEdit();
        else
            _session.CancelHumanPlayerNameEdit();
        _humanPlayerNameTextBox.Clear();
    }

    private void ImportSgf()
    {
        var fileName = _fileDialogService.OpenFile(new OpenFileDialogOptions
        {
            CheckFileExists = true,
            DefaultExtension = "sgf",
            Filters =
            [
                new FileDialogFilter("SGF files", ["*.sgf"]),
                new FileDialogFilter("All files", ["*.*"]),
            ],
            InitialDirectory = GetInitialSgfDirectory(),
            Title = "Load SGF game record",
        });

        if (fileName is null)
        {
            return;
        }

        try
        {
            var record = SgfGameRecordConverter.FromSgf(File.ReadAllText(fileName, Encoding.UTF8));
            RememberSgfDirectory(fileName);
            StartReviewingGameRecord(record, "SGF input", fileName);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SgfParseException or ArgumentOutOfRangeException)
        {
            ShowMessage(ex.Message, "SGF input");
        }
    }

    private void ExportSgf() =>
        ExportSgf(_session.CurrentGameRecord, _session.LocalMatchSgfFileName);

    /// <summary>
    /// 指定された棋譜を Local と共通の保存フローで SGF 出力します。
    /// </summary>
    private bool ExportSgf(
        GoGameRecord record,
        string fileName,
        bool markCurrentResultSaved = true,
        Action<string>? onSaved = null)
    {
        var selectedFileName = _fileDialogService.SaveFile(new SaveFileDialogOptions
        {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExtension = "sgf",
            Filters =
            [
                new FileDialogFilter("SGF files", ["*.sgf"]),
                new FileDialogFilter("All files", ["*.*"]),
            ],
            InitialDirectory = GetInitialSgfDirectory(),
            InitialFileName = fileName,
            OverwritePrompt = true,
            Title = "Save SGF game record",
        });

        if (selectedFileName is null)
        {
            return false;
        }

        try
        {
            var sgf = SgfGameRecordConverter.ToSgf(record);
            File.WriteAllText(selectedFileName, sgf, Encoding.UTF8);
            RememberSgfDirectory(selectedFileName);
            RefreshSgfAutoSaveState();
            if (markCurrentResultSaved)
                MarkCurrentResultSgfSaved();
            onSaved?.Invoke(selectedFileName);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage(ex.Message, "SGF output");
            return false;
        }
    }

    private void ToggleSgfAutoSave()
    {
        var enabled = !_session.IsSgfAutoSaveEnabled;
        try
        {
            ApplicationSettings.SaveSgfAutoSaveEnabled(enabled);
            _session.SetSgfAutoSaveEnabled(enabled);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _session.SetSgfAutoSaveStatus("SAVE FAILED");
            ApplicationErrorLog.Write("SGF AUTO SAVE", "Could not save the auto-save setting.", ex);
        }
    }

    private void RefreshSgfAutoSaveState()
    {
        var available = Directory.Exists(ApplicationSettings.Current.SgfSaveDirectory);
        _session.SetSgfAutoSaveAvailability(available);
        _session.SetSgfAutoSaveEnabled(available && ApplicationSettings.Current.IsSgfAutoSaveEnabled);
    }

    private void TryAutoSaveCompletedLocalGame()
    {
        if (!_session.IsSgfAutoSaveEnabled ||
            _session.CurrentMode.Kind != GoAppModeKind.GameOver ||
            ReferenceEquals(_lastAutoSavedLocalGameRecord, _session.CurrentGameRecord))
        {
            return;
        }

        _lastAutoSavedLocalGameRecord = _session.CurrentGameRecord;
        if (AutoSaveSgf(
            _session.CurrentGameRecord,
            _session.LocalMatchSgfFileName))
        {
            _session.SetLocalResultSgfSaved(true);
        }
    }

    private void TryAutoSaveCgosGame()
    {
        if (!_session.IsSgfAutoSaveEnabled ||
            !_cgosGameObservation.IsFinished ||
            _lastAutoSavedCgosGameId == _cgosGameObservation.GameId)
        {
            return;
        }

        _lastAutoSavedCgosGameId = _cgosGameObservation.GameId;
        if (AutoSaveSgf(
            _cgosGameObservation.CreateGameRecord(),
            CgosSgfFileNameBuilder.Create(_session.SelectedCgosConnectionProfile, _cgosGameObservation)))
        {
            _session.SetCgosResultSgfSaved(true);
        }
    }

    private bool AutoSaveSgf(GoGameRecord record, string fileName)
    {
        var directory = ApplicationSettings.Current.SgfSaveDirectory;
        if (!Directory.Exists(directory))
        {
            RefreshSgfAutoSaveState();
            return false;
        }

        try
        {
            var sgf = SgfGameRecordConverter.ToSgf(record);
            var path = Path.Combine(directory, Path.GetFileName(fileName));
            File.WriteAllText(path, sgf, Encoding.UTF8);
            _session.SetSgfAutoSaveStatus("AUTO SAVED");
            GuiOperationLog.User("Automatically saved SGF", path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _session.SetSgfAutoSaveStatus("SAVE FAILED");
            ApplicationErrorLog.Write("SGF AUTO SAVE", "Could not automatically save the SGF game record.", ex);
            return false;
        }
    }

    private void MarkCurrentResultSgfSaved()
    {
        if (_session.UseKind == GoAppUseKind.LocalPlay &&
            _session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            _session.SetLocalResultSgfSaved(true);
        }
        else if (_session.UseKind == GoAppUseKind.CgosClient &&
                 _session.CgosConnectionFlowKind == CgosConnectionFlowKind.Result)
        {
            _session.SetCgosResultSgfSaved(true);
        }
    }

    private static string GetInitialSgfDirectory() =>
        Directory.Exists(ApplicationSettings.Current.SgfSaveDirectory)
            ? ApplicationSettings.Current.SgfSaveDirectory
            : AppContext.BaseDirectory;

    private static void RememberSgfDirectory(string fileName)
    {
        var directory = Path.GetDirectoryName(fileName);
        if (string.IsNullOrWhiteSpace(directory) ||
            string.Equals(directory, ApplicationSettings.Current.SgfSaveDirectory, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            ApplicationSettings.SaveSgfDirectory(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            ApplicationErrorLog.Write("SGF SETTINGS", "Could not remember the SGF folder.", ex);
        }
    }

    private void ShowMessage(string message, string caption)
    {
        _messageDialog = new MessageDialog(caption, message);
        _session.ActivateModalWindow(ActiveWindowId.MessageDialog);
    }

    private void OpenGtpEngineSelectionDialog(GoStone stone)
    {
        var appId = _session.UseKind == GoAppUseKind.LocalApps ? "ponnuki" : "play";
        _session.OpenGtpEngineSelectionDialog(stone, appId);
        BeginGtpEngineSelectionLoading(appId, "player");
    }

    private void OpenCgosGtpEngineSelectionDialog(GoStone stone)
    {
        _session.OpenCgosGtpEngineSelectionDialog(stone);
        BeginGtpEngineSelectionLoading("play", "player");
    }

    private void OpenAppProviderGtpEngineSelectionDialog(string appId)
    {
        RefreshGtpEngineAppCompatibilities(appId, "provider");
        _session.OpenAppProviderGtpEngineSelectionDialog(appId);
    }

    private void BeginOpenAppProviderGtpEngineSelectionDialog(string appId)
    {
        if (_appProviderSelectionLoadTask is not null) return;
        _appProviderSelectionLoadAppId = appId;
        var checks = _session.GtpEngineProfiles
            .Select(profile => GtpEngineAppCompatibilityProbe.CheckAsync(profile, appId, "provider"))
            .ToArray();
        _appProviderSelectionLoadTask = Task.WhenAll(checks);
        GuiOperationLog.User("Loading App Provider engines", $"app={appId}; engines={checks.Length}");
    }

    private void CompleteAppProviderSelectionLoading()
    {
        var task = _appProviderSelectionLoadTask;
        if (task is null || !task.IsCompleted) return;
        _appProviderSelectionLoadTask = null;
        try
        {
            _session.SetGtpEngineAppCompatibilities(task.GetAwaiter().GetResult());
            if (_session.UseKind is null && _titleMenuPage == TitleMenuPage.CaptureGame)
                _session.OpenAppProviderGtpEngineSelectionDialog(_appProviderSelectionLoadAppId);
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("APP PROVIDER SELECTION", "Could not inspect App Provider engines.", ex);
            ShowMessage(ex.Message, "App Provider engines");
        }
    }

    private void CompleteRestoredAppProviderCheck()
    {
        var task = _restoredAppProviderCheckTask;
        if (task is null || !task.IsCompleted) return;
        _restoredAppProviderCheckTask = null;
        try
        {
            var result = task.GetAwaiter().GetResult();
            if (!_session.HasSelectedAppProviderEngine ||
                !string.Equals(_session.SelectedAppProviderEngine.ExecutablePath, _restoredAppProviderCheckPath, StringComparison.OrdinalIgnoreCase))
                return;
            _session.SetAppProviderCapability(result.IsSupported, result.Message);
            GuiOperationLog.App(
                "Automatically checked restored App Provider",
                $"app=ponnuki; engine={_session.SelectedAppProviderEngine.DisplayName}; supported={result.IsSupported}");
        }
        catch (Exception ex)
        {
            if (_session.HasSelectedAppProviderEngine &&
                string.Equals(_session.SelectedAppProviderEngine.ExecutablePath, _restoredAppProviderCheckPath, StringComparison.OrdinalIgnoreCase))
                _session.SetAppProviderCapability(false, $"CHECK FAILED: {ex.Message}");
            ApplicationErrorLog.Write("APP PROVIDER CHECK", "Could not automatically check the restored Ponnuki Provider.", ex);
        }
    }

    private void RefreshGtpEngineAppCompatibilities(string appId, string role)
    {
        var checks = _session.GtpEngineProfiles
            .Select(profile => GtpEngineAppCompatibilityProbe.CheckAsync(profile, appId, role))
            .ToArray();
        var results = Task.WhenAll(checks).GetAwaiter().GetResult();
        _session.SetGtpEngineAppCompatibilities(results);
    }

    private void BeginGtpEngineSelectionLoading(string appId, string role)
    {
        if (_gtpEngineSelectionLoadTask is not null)
            return;

        _session.BeginGtpEngineCompatibilityLoading();
        var profiles = _session.GtpEngineProfiles.ToArray();
        // CheckAsync は Process.Start などを最初の await より前で実行し得ます。
        // ここで直接呼ぶとクリック処理が止まり、ダイアログの最初の一枚を描けません。
        // ワーカースレッドへ渡すことで、先にスケルトンとスピナーを表示します。
        _gtpEngineSelectionLoadTask = Task.Run(async () =>
            await Task.WhenAll(profiles.Select(profile => GtpEngineAppCompatibilityProbe.CheckAsync(profile, appId, role))));
        GuiOperationLog.User("Loading selectable GTP engines", $"app={appId}; role={role}; engines={profiles.Length}");
    }

    private void CompleteGtpEngineSelectionLoading()
    {
        var task = _gtpEngineSelectionLoadTask;
        if (task is null || !task.IsCompleted)
            return;

        _gtpEngineSelectionLoadTask = null;
        try
        {
            _session.SetGtpEngineAppCompatibilities(task.GetAwaiter().GetResult());
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("GTP ENGINE SELECTION", "Could not inspect selectable GTP engines.", ex);
            _session.SetGtpEngineAppCompatibilities(
                _session.GtpEngineProfiles.Select(_ => new GtpEngineAppCompatibility(
                    GtpEngineAppCompatibilityKind.CheckFailed,
                    $"CHECK FAILED: {ex.Message}")));
        }
    }

    private bool SaveReviewSgf()
    {
        if (string.IsNullOrWhiteSpace(_reviewSgfFilePath))
        {
            return ExportSgf(
                _session.CurrentGameRecord,
                $"kifuwarabe-go-commented-{DateTime.Now:yyyyMMdd-HHmmss}.sgf",
                onSaved: path => _reviewSgfFilePath = path);
        }

        try
        {
            File.WriteAllText(_reviewSgfFilePath, SgfGameRecordConverter.ToSgf(_session.CurrentGameRecord), Encoding.UTF8);
            RememberSgfDirectory(_reviewSgfFilePath);
            RefreshSgfAutoSaveState();
            GuiOperationLog.User("Overwrote reviewed SGF", _reviewSgfFilePath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage(ex.Message, "SGF output");
            return false;
        }
    }

    private bool CanEditCompletedLocalGameComment() =>
        _session.CurrentMode.Kind == GoAppModeKind.GameOver &&
        _session.UseKind is GoAppUseKind.LocalPlay or GoAppUseKind.LocalApps &&
        Directory.Exists(ApplicationSettings.Current.SgfSaveDirectory) &&
        !string.IsNullOrWhiteSpace(_session.LocalMatchSgfFileName);

    private bool SaveCompletedLocalGameCommentSgf()
    {
        if (!CanEditCompletedLocalGameComment()) return false;

        var path = Path.Combine(
            ApplicationSettings.Current.SgfSaveDirectory,
            Path.GetFileName(_session.LocalMatchSgfFileName));
        try
        {
            File.WriteAllText(path, SgfGameRecordConverter.ToSgf(_session.CurrentGameRecord), Encoding.UTF8);
            _session.SetLocalResultSgfSaved(true);
            _session.SetSgfAutoSaveStatus("SAVED");
            GuiOperationLog.User("Saved edited local SGF comment", path);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _session.SetSgfAutoSaveStatus("SAVE FAILED");
            ApplicationErrorLog.Write("SGF COMMENT SAVE", "Could not save the edited local game comment.", ex);
            ShowMessage(ex.Message, "SGF output");
            return false;
        }
    }

    private void RefreshCurrentGtpEngineAppCompatibilities() =>
        RefreshGtpEngineAppCompatibilities(
            _session.GtpEngineSelectionAppId,
            _session.EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider ? "provider" : "player");

    private bool TryHandleGtpEngineSelectionDialogClick(Point point)
    {
        if (!_session.IsGtpEngineSelectionDialogOpen)
        {
            return false;
        }

        if (_session.GtpEngineOrderEditor.IsOpen)
        {
            return TryHandleGtpEngineOrderEditorClick(point);
        }

        if (_session.IsGtpEngineDeleteConfirmationOpen)
        {
            return TryHandleGtpEngineDeleteConfirmationClick(point);
        }

        if (GtpEngineRenderer.TryGetGtpEngineSelectionDialogPathCopyText(point, _session, out var path))
        {
            _clipboardService.TrySetText(path);
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogCancelButtonHit(point))
        {
            _session.CancelGtpEngineSelectionDialog();
            return true;
        }

        if (_session.IsGtpEngineCompatibilityLoading)
            return true;

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogOkButtonHit(point))
        {
            if (_session.CanCommitGtpEngineSelection)
            {
                var selectionPurpose = _session.EngineSelectionPurpose;
                var selectedEngine = _session.GtpEngineProfiles[_session.GtpEngineDialogSelectionIndex];
                _session.CommitGtpEngineSelectionDialog();
                GuiOperationLog.User("Selected GTP engine",
                    $"purpose={selectionPurpose}; name={selectedEngine.DisplayName}; id={selectedEngine.Id}");
                if (_session.EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider)
                {
                    ApplicationSettings.SaveLastSelectedAppProviderEngine(
                        _session.GtpEngineSelectionAppId,
                        _session.SelectedAppProviderEngine.ExecutablePath);
                    RecheckPonnukiProvider();
                }
            }
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogAddButtonHit(point))
        {
            _session.OpenGtpEngineAddPanel();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogEditButtonHit(point))
        {
            _session.OpenGtpEngineEditPanel();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogDuplicateButtonHit(point))
        {
            _session.OpenGtpEngineDuplicatePanel();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogDeleteButtonHit(point, _session.CanDeleteSelectedGtpEngine))
        {
            _session.OpenGtpEngineDeleteConfirmation();
            return true;
        }

        if (_session.GtpEngineProfiles.Count > 1 &&
            GtpEngineRenderer.GetGtpEngineSelectionDialogOrderButtonHit(point))
        {
            _session.OpenGtpEngineOrderEditor();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogPreviousPageButtonHit(point))
        {
            _session.MoveGtpEngineSelectionPage(-1);
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogNextPageButtonHit(point))
        {
            _session.MoveGtpEngineSelectionPage(1);
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineSelectionDialogListItemHit(point, _session) is { } index)
        {
            _session.SelectGtpEngineDialogItem(index);
            return true;
        }

        return true;
    }

    private bool TryHandleCgosConnectionOrderEditorClick(Point point)
    {
        var editor = _session.CgosConnectionOrderEditor;
        if (CatalogOrderPresenter.GetCatalogOrderCancelButtonHit(point) && editor.HasChanges)
        {
            _session.CancelCgosConnectionOrderEditor();
            BeginDiscardTransition();
            return true;
        }

        if (CatalogOrderPresenter.GetCatalogOrderSaveButtonHit(point))
        {
            if (editor.HasChanges)
            {
                var profiles = _session.CommitCgosConnectionOrderEditor();
                _cgosConnectionCatalog.Save(profiles);
            }
            else _session.CancelCgosConnectionOrderEditor();
            return true;
        }

        var moveStep = CatalogOrderPresenter.GetCatalogOrderMoveStep(point, editor.PageSize);
        if (moveStep == int.MinValue)
            editor.MoveSelectedToTop();
        else if (moveStep != 0)
            editor.MoveSelected(moveStep);
        else if (CatalogOrderPresenter.GetCatalogOrderPageStep(point) is var pageStep && pageStep != 0)
            editor.MoveVisiblePages(pageStep);
        else if (CatalogOrderPresenter.GetCatalogOrderCardHit(point, editor) is { } index)
            editor.BeginDrag(index);

        return true;
    }

    private bool TryHandleGtpEngineOrderEditorClick(Point point)
    {
        var editor = _session.GtpEngineOrderEditor;
        if (CatalogOrderPresenter.GetCatalogOrderCancelButtonHit(point) && editor.HasChanges)
        {
            _session.CancelGtpEngineOrderEditor();
            BeginDiscardTransition();
            return true;
        }

        if (CatalogOrderPresenter.GetCatalogOrderSaveButtonHit(point))
        {
            if (editor.HasChanges)
            {
                var profiles = _session.CommitGtpEngineOrderEditor();
                _gtpEngineCatalog.Save(profiles);
                RefreshCurrentGtpEngineAppCompatibilities();
            }
            else _session.CancelGtpEngineOrderEditor();
            return true;
        }

        var moveStep = CatalogOrderPresenter.GetCatalogOrderMoveStep(point, editor.PageSize);
        if (moveStep == int.MinValue)
            editor.MoveSelectedToTop();
        else if (moveStep != 0)
            editor.MoveSelected(moveStep);
        else if (CatalogOrderPresenter.GetCatalogOrderPageStep(point) is var pageStep && pageStep != 0)
            editor.MoveVisiblePages(pageStep);
        else if (CatalogOrderPresenter.GetCatalogOrderCardHit(point, editor) is { } index)
            editor.BeginDrag(index);

        return true;
    }

    private bool TryHandleGtpEngineDeleteConfirmationClick(Point point)
    {
        if (GtpEngineRenderer.GetGtpEngineDeleteConfirmationCancelButtonHit(point))
        {
            _session.CloseGtpEngineDeleteConfirmation();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineDeleteConfirmationConfirmButtonHit(point))
        {
            _session.RemoveSelectedGtpEngine();
            _gtpEngineCatalog.Save(_session.GtpEngineProfiles);
            RefreshCurrentGtpEngineAppCompatibilities();
            return true;
        }

        return true;
    }

    private bool TryHandleGtpEngineEditPanelClick(Point point)
    {
        if (!_session.IsGtpEngineEditPanelOpen)
        {
            return false;
        }

        if (_session.IsGtpEngineGuiOptionsDialogOpen)
        {
            if (_session.IsGtpEngineRandomMoveSelectionDialogOpen)
            {
                if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogPagerStep(point) is { } comboPageStep)
                    _session.MoveGtpEngineRandomMoveSelectionPage(comboPageStep);
                else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(point))
                    _session.CancelGtpEngineRandomMoveSelectionDialog();
                else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(point))
                    _session.CommitGtpEngineRandomMoveSelectionDialog();
                else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogItemHit(point, _session) is { } itemIndex)
                    _session.SelectGtpEngineRandomMoveItem(itemIndex);

                return true;
            }

            if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogPagerStep(point) is { } optionPageStep)
            {
                _session.MoveGtpEngineGuiOptionsPage(optionPageStep);
                return true;
            }

            if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogCancelButtonHit(point) && _session.IsGtpEngineGuiOptionsDialogDirty)
            {
                _session.CancelGtpEngineGuiOptionsDialog();
                BeginDiscardTransition();
                return true;
            }

            if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogOkButtonHit(point))
            {
                if (_session.IsGtpEngineGuiOptionsDialogDirty) _session.CommitGtpEngineGuiOptionsDialog();
                else _session.CancelGtpEngineGuiOptionsDialog();
                return true;
            }

            if (GtpEngineRenderer.GetGtpEngineGuiOptionControlHit(point, _session) is { } optionHit)
            {
                var option = GtpEngineGuiOptions.Specs[optionHit.Index];
                if (optionHit.Action == 3)
                {
                    _session.SetGtpEngineGuiOptionDraft(option, option.DefaultValue);
                    return true;
                }
                switch (option.Type)
                {
                    case "check":
                        _session.ToggleGtpEngineCheckOption(option);
                        break;
                    case "spin":
                        if (optionHit.Action == 2)
                            EditGtpEngineSpinOption(option);
                        else
                            _session.StepGtpEngineSpinOption(option, optionHit.Action == 0 ? -1 : 1);
                        break;
                    case "combo":
                        _session.OpenGtpEngineRandomMoveSelectionDialog(option);
                        break;
                    case "string":
                        EditGtpEngineStringOption(option);
                        break;
                    case "filename":
                        BrowseGtpEngineFilenameOption(option);
                        break;
                    case "button":
                        _session.ToggleGtpEngineButtonOption(option);
                        break;
                }
                return true;
            }

            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelCloseButtonHit(point) && _session.IsGtpEngineEditDirty)
        {
            EndGtpEngineEditField();
            _gtpEngineEditTextBox.Clear();
            RefreshCurrentGtpEngineAppCompatibilities();
            _session.CancelNewEngineProfileForPlayerEdit();
            _session.CloseGtpEngineEditPanel();
            BeginDiscardTransition();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelFileBrowseButtonHit(point))
        {
            BrowseGtpEngineExecutablePath();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelWorkingDirectoryBrowseButtonHit(point))
        {
            BrowseGtpEngineWorkingDirectory();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelLogButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.ToggleGtpEngineEditLog();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionProfileButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.CycleGtpEngineInitialPositionProfile();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelInitialPositionMethodButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.CycleGtpEngineInitialPositionPreferredMethod();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelGuiOptionsButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.OpenGtpEngineGuiOptionsDialog();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelSaveButtonHit(point))
        {
            if (_session.IsGtpEngineEditDirty)
            {
                if (SaveGtpEngineEditDraft()) CloseGtpEngineEditPanel();
            }
            else CloseGtpEngineEditPanel();
            return true;
        }

        if (GtpEngineRenderer.GetGtpEngineEditPanelFieldHit(point) is { } field)
        {
            BeginOrMoveGtpEngineEditField(point, field);
            return true;
        }

        return true;
    }

    private void UpdateGtpEngineEditPanelByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (!IsActive || !_inputArmed) return;

        if (!_session.IsGtpEngineEditPanelOpen)
        {
            _previousGtpEngineKeyboard = keyboard;
            return;
        }

        if (keyboard.IsKeyDown(Keys.Tab) && _previousGtpEngineKeyboard.IsKeyUp(Keys.Tab))
        {
            MoveGtpEngineEditFocus(IsShiftDown(keyboard) ? -1 : 1);
            _previousGtpEngineKeyboard = keyboard;
            return;
        }

        if (_session.IsGtpEngineGuiOptionsDialogOpen)
        {
            _previousGtpEngineKeyboard = keyboard;
            return;
        }

        if (_session.ActiveGtpEngineEditField is { } field)
        {
            switch (_gtpEngineEditTextBox.HandleKeyboard(
                        keyboard,
                        _previousGtpEngineKeyboard,
                        gameTime,
                        _clipboardService,
                        allowClipboardExport: field != GtpEngineProfileEditField.DefaultCgosPlainTextPassword))
            {
                case TextBoxKeyboardAction.Commit:
                    EndGtpEngineEditField();
                    break;
                case TextBoxKeyboardAction.Cancel:
                    CancelGtpEngineEditField(field);
                    break;
                default:
                    SyncGtpEngineEditField(field);
                    break;
            }

            _previousGtpEngineKeyboard = keyboard;
            return;
        }

        if (IsNewGtpEngineKeyPress(keyboard, Keys.F5))
        {
            if (SaveGtpEngineEditDraft())
                CloseGtpEngineEditPanel();
        }

        _previousGtpEngineKeyboard = keyboard;
    }

    private bool TryInputGtpEngineEditCharacter(char character)
    {
        if (!_session.IsGtpEngineEditPanelOpen || _session.ActiveGtpEngineEditField is not { } field)
        {
            return false;
        }

        if (!_gtpEngineEditTextBox.TryInputCharacter(character))
        {
            _session.SetGtpEngineEditWarning("Text is too long.");
            return true;
        }

        SyncGtpEngineEditField(field);
        UpdateGtpEngineEditWarning();
        return true;
    }

    private void BeginOrMoveGtpEngineEditField(Point point, GtpEngineProfileEditField field)
    {
        var text = _session.ActiveGtpEngineEditField == field
            ? _gtpEngineEditTextBox.Text
            : _session.GetGtpEngineEditFieldText(field);
        var caretIndex = _presentationServices?.Presentation.GetGtpEngineEditPanelCaretIndex(point, field, text) ?? text.Length;

        if (_session.ActiveGtpEngineEditField == field)
        {
            _gtpEngineEditTextBox.BeginMouseSelection(caretIndex, IsShiftDown());
            SyncGtpEngineEditField(field);
            return;
        }

        _gtpEngineEditTextBox.Begin(text, caretIndex);
        _gtpEngineEditTextBox.BeginMouseSelection(caretIndex, extendSelection: false);
        SyncGtpEngineEditField(field);
        _session.BeginGtpEngineEditField(field, _gtpEngineEditTextBox.CaretIndex);
        UpdateGtpEngineEditWarning();
    }

    private void TryHandleAppProviderGameSettingsClick(Point point)
    {
        if (_session.IsGtpEngineRandomMoveSelectionDialogOpen)
        {
            if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogPagerStep(point) is { } comboPageStep)
                _session.MoveGtpEngineRandomMoveSelectionPage(comboPageStep);
            else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(point))
                _session.CancelGtpEngineRandomMoveSelectionDialog();
            else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(point))
            {
                _session.CommitGtpEngineRandomMoveSelectionDialog();
                QueueAppProviderSettingsEvaluation();
            }
            else if (GtpEngineRenderer.GetGtpEngineRandomMoveSelectionDialogItemHit(point, _session) is { } itemIndex)
                _session.SelectGtpEngineRandomMoveItem(itemIndex);
            return;
        }

        if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogPagerStep(point) is { } pageStep)
        {
            _session.MoveGtpEngineGuiOptionsPage(pageStep);
            return;
        }

        if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogCancelButtonHit(point) && _session.IsGtpEngineGuiOptionsDialogDirty)
        {
            _appProviderSettingsEvaluationGeneration++;
            _session.CancelAppProviderGameSettingsDialog();
            BeginDiscardTransition();
            GuiOperationLog.User("Cancelled App Provider game settings", "app=ponnuki; role=provider");
            return;
        }

        if (GtpEngineRenderer.GetGtpEngineGuiOptionsDialogOkButtonHit(point))
        {
            if (_session.IsGtpEngineGuiOptionsDialogDirty)
            {
                var profiles = _session.CommitAppProviderGameSettingsDialog();
                _gtpEngineCatalog.Save(profiles);
                GuiOperationLog.User("Saved App Provider game settings", "app=ponnuki; role=provider");
            }
            else _session.CancelAppProviderGameSettingsDialog();
            return;
        }

        if (GtpEngineRenderer.GetGtpEngineGuiOptionControlHit(point, _session) is not { } optionHit)
            return;

        var option = _session.ActiveGtpEngineGuiOptionSpecs[optionHit.Index];
        if (optionHit.Action == 3)
        {
            _session.SetGtpEngineGuiOptionDraft(option, option.DefaultValue);
            QueueAppProviderSettingsEvaluation();
            return;
        }

        switch (option.Type)
        {
            case "check":
                _session.ToggleGtpEngineCheckOption(option);
                break;
            case "spin":
                if (optionHit.Action == 2)
                    EditGtpEngineSpinOption(option);
                else
                    _session.StepGtpEngineSpinOption(option, optionHit.Action == 0 ? -1 : 1);
                break;
            case "combo":
                _session.OpenGtpEngineRandomMoveSelectionDialog(option);
                return;
            case "string":
                EditGtpEngineStringOption(option);
                break;
            case "filename":
                BrowseGtpEngineFilenameOption(option);
                break;
            case "button":
                _session.ToggleGtpEngineButtonOption(option);
                break;
        }
        QueueAppProviderSettingsEvaluation();
    }

    private void OpenAppProviderGameSettings()
    {
        try
        {
            var specs = PonnukiPositionProvider.GetGameSettingSpecsAsync(_session.SelectedAppProviderEngine)
                .GetAwaiter().GetResult();
            _session.OpenAppProviderGameSettingsDialog(specs);
            QueueAppProviderSettingsEvaluation();
            GuiOperationLog.User("Opened App Provider game settings", $"app=ponnuki; role=provider; options={specs.Count}");
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("APP PROVIDER SETTINGS", "Could not load the Ponnuki Provider option schema.", ex);
            ShowMessage(ex.Message, "Ponnuki game settings");
        }
    }

    private void QueueAppProviderSettingsEvaluation()
    {
        if (!_session.IsAppProviderGameSettingsDialogOpen || !_session.CanUseSelectedAppProvider) return;
        _appProviderSettingsEvaluationGeneration++;
        if (_appProviderSettingsEvaluationTask is not null) return;

        _appProviderSettingsEvaluationTaskGeneration = _appProviderSettingsEvaluationGeneration;
        _appProviderSettingsEvaluationPath = _session.SelectedAppProviderEngine.ExecutablePath;
        var draft = new Dictionary<string, string>(_session.GtpEngineGuiOptionsDialogDraft, StringComparer.Ordinal);
        _appProviderSettingsEvaluationTask = PonnukiPositionProvider.EvaluateGameSettingsAsync(
            _session.SelectedAppProviderEngine,
            draft);
    }

    private void CompleteAppProviderSettingsEvaluation()
    {
        if (_appProviderSettingsEvaluationTask is not { IsCompleted: true } task) return;
        var completedGeneration = _appProviderSettingsEvaluationTaskGeneration;
        var completedPath = _appProviderSettingsEvaluationPath;
        _appProviderSettingsEvaluationTask = null;
        try
        {
            var evaluation = task.GetAwaiter().GetResult();
            if (_session.IsAppProviderGameSettingsDialogOpen &&
                completedGeneration == _appProviderSettingsEvaluationGeneration &&
                completedPath.Equals(_session.SelectedAppProviderEngine.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            {
                _session.ApplyAppProviderGameSettingsEvaluation(evaluation.Specs, evaluation.Values);
                if (evaluation.Adjustments.Count > 0)
                    GuiOperationLog.User("App Provider adjusted tentative game settings", string.Join("; ", evaluation.Adjustments.Select(value => $"{value.Id}: {value.From.GetRawText()} -> {value.To.GetRawText()} ({value.Reason})")));
            }
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("APP PROVIDER SETTINGS", "Could not evaluate tentative Ponnuki Provider options.", ex);
        }

        if (_session.IsAppProviderGameSettingsDialogOpen && completedGeneration != _appProviderSettingsEvaluationGeneration)
            QueueAppProviderSettingsEvaluation();
    }

    private void MoveGtpEngineEditFocus(int step)
    {
        var fields = new[]
        {
            GtpEngineProfileEditField.DisplayName,
            GtpEngineProfileEditField.ExecutablePath,
            GtpEngineProfileEditField.WorkingDirectory,
            GtpEngineProfileEditField.Arguments,
        };
        var currentIndex = _session.ActiveGtpEngineEditField is { } current
            ? Array.IndexOf(fields, current)
            : step > 0 ? -1 : 0;
        EndGtpEngineEditField();
        BeginGtpEngineEditField(fields[(currentIndex + step + fields.Length) % fields.Length]);
    }

    private void BeginGtpEngineEditField(GtpEngineProfileEditField field)
    {
        var text = _session.GetGtpEngineEditFieldText(field);
        _gtpEngineEditTextBox.Begin(text);
        _session.BeginGtpEngineEditField(field, _gtpEngineEditTextBox.CaretIndex);
        SyncGtpEngineEditField(field);
        UpdateGtpEngineEditWarning();
    }

    private void SyncGtpEngineEditField(GtpEngineProfileEditField field)
    {
        _session.SetGtpEngineEditField(field, _gtpEngineEditTextBox.Text, _gtpEngineEditTextBox.CaretIndex);
        _session.SetGtpEngineEditSelection(_gtpEngineEditTextBox.SelectionStart, _gtpEngineEditTextBox.SelectionLength);
    }

    private void SyncCgosCredentialSelection() =>
        _session.SetCgosCredentialSelection(_cgosCredentialTextBox.SelectionStart, _cgosCredentialTextBox.SelectionLength);

    private void EndGtpEngineEditField()
    {
        if (_session.ActiveGtpEngineEditField is not { })
        {
            return;
        }

        _session.EndGtpEngineEditField();
        _gtpEngineEditTextBox.Clear();
    }

    private void CancelGtpEngineEditField(GtpEngineProfileEditField field)
    {
        _gtpEngineEditTextBox.Begin(_session.GetGtpEngineEditFieldText(field));
        _session.EndGtpEngineEditField();
        _gtpEngineEditTextBox.Clear();
        _session.SetGtpEngineEditWarning("");
    }

    private void BrowseGtpEngineExecutablePath()
    {
        EndGtpEngineEditField();
        var source = _session.GtpEngineEditDraft;
        var fileName = _fileDialogService.OpenFile(new OpenFileDialogOptions
        {
            CheckFileExists = true,
            Filters = _platformExecutableService.SelectionFilters,
            InitialFileName = Path.GetFileName(source.ExecutablePath),
            InitialDirectory = GetInitialGtpEngineDirectory(source),
            Title = "Select GTP engine executable",
        });

        if (fileName is null)
        {
            return;
        }

        if (GtpEngineExecutableGuard.IsGuiApplication(fileName))
        {
            _session.SetGtpEngineEditWarning(GtpEngineExecutableGuard.GuiSelectedMessage);
            ShowMessage(GtpEngineExecutableGuard.GuiSelectedMessage, "GTP engine executable");
            return;
        }

        _session.SetGtpEngineExecutablePathDraft(fileName);
    }

    private void EditGtpEngineStringOption(GtpEngineGuiOptionSpec option)
    {
        _activeGtpEngineStringOption = option;
        _session.ActivateModalWindow(ActiveWindowId.TextInput);
        _gtpEngineStringOptionTextBox.Begin(_session.GetGtpEngineGuiOptionDraft(option));
        _gtpEngineStringComposition = TextCompositionState.Empty;
        _previousGtpEngineStringKeyboard = Keyboard.GetState();
        _gtpEngineStringInputMessage = $"TEXT VALUE (MAX {GtpEngineGuiOptions.MaximumTextLength})";
    }

    private void UpdateGtpEngineStringInputKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        var action = _gtpEngineStringOptionTextBox.HandleKeyboard(
            keyboard,
            _previousGtpEngineStringKeyboard,
            gameTime,
            _clipboardService);
        if (action == TextBoxKeyboardAction.Commit)
            CommitGtpEngineStringInput();
        else if (action == TextBoxKeyboardAction.Cancel)
            CancelGtpEngineStringInput();
        _previousGtpEngineStringKeyboard = keyboard;
    }

    private void CommitGtpEngineStringInput()
    {
        if (_activeGtpEngineStringOption is not { } option)
            return;

        _session.SetGtpEngineGuiOptionDraft(option, _gtpEngineStringOptionTextBox.Text);
        if (_session.IsAppProviderGameSettingsDialogOpen)
            QueueAppProviderSettingsEvaluation();
        CancelGtpEngineStringInput();
    }

    private void RestoreGtpEngineStringInputDefault()
    {
        if (_activeGtpEngineStringOption is not { } option)
            return;

        _gtpEngineStringOptionTextBox.Begin(option.DefaultValue);
        _gtpEngineStringInputMessage = "DEFAULT VALUE (PRESS OK TO APPLY)";
    }

    private void CancelGtpEngineStringInput()
    {
        _activeGtpEngineStringOption = null;
        _session.DeactivateModalWindow(ActiveWindowId.TextInput);
        _gtpEngineStringOptionTextBox.Clear();
        _gtpEngineStringComposition = TextCompositionState.Empty;
        _gtpEngineStringInputMessage = "";
        _gtpEngineStringInputMessage = "";
    }

    private void EditGtpEngineSpinOption(GtpEngineGuiOptionSpec option)
    {
        _activeLocalMatchRandomSeedStone = null;
        _activeGtpEngineIntegerOption = option;
        _session.ActivateModalWindow(ActiveWindowId.IntegerInput);
        _gtpEngineIntegerOptionTextBox.Begin(_session.GetGtpEngineGuiOptionDraft(option));
        _previousGtpEngineIntegerKeyboard = Keyboard.GetState();
        _gtpEngineIntegerInputMessage = $"RANGE  {option.Min ?? int.MinValue} .. {option.Max ?? int.MaxValue}";
    }

    private void EditLocalMatchRandomSeed(GoStone stone)
    {
        var option = GtpEngineGuiOptions.Specs.First(spec => spec.Id == GtpEngineGuiOptions.RandomSeedId);
        _activeLocalMatchRandomSeedStone = stone;
        _activeGtpEngineIntegerOption = option;
        _session.ActivateModalWindow(ActiveWindowId.IntegerInput);
        _gtpEngineIntegerOptionTextBox.Begin(_session.GetLocalMatchRandomSeedText(stone));
        _previousGtpEngineIntegerKeyboard = Keyboard.GetState();
        _gtpEngineIntegerInputMessage = $"EMPTY: AUTO   RANGE  {option.Min ?? int.MinValue} .. {option.Max ?? int.MaxValue}";
    }

    private void UpdateGtpEngineIntegerInputKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        var action = _gtpEngineIntegerOptionTextBox.HandleKeyboard(
            keyboard,
            _previousGtpEngineIntegerKeyboard,
            gameTime,
            _clipboardService,
            pasteCharacterFilter: character => char.IsDigit(character) || character == '-');
        if (action == TextBoxKeyboardAction.Commit)
            CommitGtpEngineIntegerInput();
        else if (action == TextBoxKeyboardAction.Cancel)
            CancelGtpEngineIntegerInput();
        _previousGtpEngineIntegerKeyboard = keyboard;
    }

    private void CommitGtpEngineIntegerInput()
    {
        if (_activeGtpEngineIntegerOption is not { } option)
            return;
        if (_activeLocalMatchRandomSeedStone is { } stone && string.IsNullOrWhiteSpace(_gtpEngineIntegerOptionTextBox.Text))
        {
            _session.SetLocalMatchRandomSeedText(stone, "");
            CancelGtpEngineIntegerInput();
            return;
        }
        if (!int.TryParse(_gtpEngineIntegerOptionTextBox.Text, out var value))
        {
            _gtpEngineIntegerInputMessage = "ENTER A VALID INTEGER";
            return;
        }
        var minimum = option.Min ?? int.MinValue;
        var maximum = option.Max ?? int.MaxValue;
        if (value < minimum || value > maximum)
        {
            _gtpEngineIntegerInputMessage = $"OUT OF RANGE  {minimum} .. {maximum}";
            return;
        }
        if (_activeLocalMatchRandomSeedStone is { } localMatchStone)
            _session.SetLocalMatchRandomSeedText(localMatchStone, value.ToString());
        else
        {
            _session.SetGtpEngineGuiOptionDraft(option, value.ToString());
            if (_session.IsAppProviderGameSettingsDialogOpen)
                QueueAppProviderSettingsEvaluation();
        }
        CancelGtpEngineIntegerInput();
    }

    private void CancelGtpEngineIntegerInput()
    {
        _activeGtpEngineIntegerOption = null;
        _activeLocalMatchRandomSeedStone = null;
        _session.DeactivateModalWindow(ActiveWindowId.IntegerInput);
        _gtpEngineIntegerOptionTextBox.Clear();
        _gtpEngineIntegerInputMessage = "";
    }

    private void BrowseGtpEngineFilenameOption(GtpEngineGuiOptionSpec option)
    {
        var fileName = _fileDialogService.OpenFile(new OpenFileDialogOptions
        {
            CheckFileExists = false,
            InitialFileName = Path.GetFileName(_session.GetGtpEngineGuiOptionDraft(option)),
            Filters = [new FileDialogFilter("All files", ["*.*"])],
            Title = $"Select {option.Label}",
        });
        if (fileName is not null)
        {
            if (fileName.Length > GtpEngineGuiOptions.MaximumTextLength)
                ShowMessage($"The file path exceeds {GtpEngineGuiOptions.MaximumTextLength} characters.", "GTP engine option");
            else
                _session.SetGtpEngineGuiOptionDraft(option, fileName);
        }
    }

    private void BrowseGtpEngineWorkingDirectory()
    {
        EndGtpEngineEditField();
        var selectedPath = _fileDialogService.SelectFolder(new FolderDialogOptions
        {
            AllowCreateFolder = true,
            InitialDirectory = GetInitialGtpEngineDirectory(_session.GtpEngineEditDraft),
            Title = "Select GTP engine working directory",
        });

        if (selectedPath is null)
        {
            return;
        }

        _session.SetGtpEngineWorkingDirectoryDraft(WorkingDirectoryModel.FromString(selectedPath));
    }

    private bool SaveGtpEngineEditDraft()
    {
        EndGtpEngineEditField();
        if (!ValidateGtpEngineEditDraft(out var profile, out var warning))
        {
            _session.SetGtpEngineEditWarning(warning);
            return false;
        }

        _session.SaveGtpEngineEditDraft(profile);
        _session.CompleteNewEngineProfileForPlayerEdit(profile.Id);
        _gtpEngineCatalog.Save(_session.GtpEngineProfiles);
        return true;
    }

    private void CloseGtpEngineEditPanel()
    {
        _gtpEngineEditTextBox.Clear();
        RefreshCurrentGtpEngineAppCompatibilities();
        _session.CloseGtpEngineEditPanel();
    }

    private bool ValidateGtpEngineEditDraft(out GtpEngineProfile profile, out string warning)
    {
        profile = _session.GtpEngineEditDraft.Clone();
        profile.DisplayName = profile.DisplayName.Trim();
        profile.ExecutablePath = profile.ExecutablePath.Trim();
        profile.WorkingDirectoryModel = WorkingDirectoryModel.FromString(profile.WorkingDirectoryModel.Value.Trim());
        profile.Arguments = profile.Arguments.Trim();

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            warning = "Display name is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(profile.ExecutablePath))
        {
            warning = "Executable path is required.";
            return false;
        }

        if (GtpEngineExecutableGuard.IsGuiApplication(profile))
        {
            warning = GtpEngineExecutableGuard.GuiSelectedMessage;
            return false;
        }

        if (profile.WorkingDirectoryModel.IsEmpty)
        {
            profile.WorkingDirectoryModel = WorkingDirectoryModel.FromString(Path.GetDirectoryName(profile.ExecutablePath) ?? string.Empty);
        }

        warning = "";
        return true;
    }

    private void UpdateGtpEngineEditWarning()
    {
        if (string.IsNullOrWhiteSpace(_session.GtpEngineEditDraft.DisplayName))
        {
            _session.SetGtpEngineEditWarning("Display name is required.");
            return;
        }

        if (string.IsNullOrWhiteSpace(_session.GtpEngineEditDraft.ExecutablePath))
        {
            _session.SetGtpEngineEditWarning("Executable path is required.");
            return;
        }

        if (GtpEngineExecutableGuard.IsGuiApplication(_session.GtpEngineEditDraft))
        {
            _session.SetGtpEngineEditWarning(GtpEngineExecutableGuard.GuiSelectedMessage);
            return;
        }

        _session.SetGtpEngineEditWarning("");
    }

    private static string GetInitialGtpEngineDirectory(GtpEngineProfile profile)
    {
        // 実行ファイルの親ディレクトリー
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath))
        {
            var directory = Path.GetDirectoryName(profile.ExecutablePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        // 作業ディレクトリー
        if (!profile.WorkingDirectoryModel.IsEmpty && Directory.Exists(profile.WorkingDirectoryModel.Value))
        {
            return profile.WorkingDirectoryModel.Value;
        }

        return AppContext.BaseDirectory;
    }

    private bool IsNewGtpEngineKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousGtpEngineKeyboard.IsKeyUp(key);

    private void HandleApplicationSettingsClick(Point point)
    {
        var settingsScreen = ApplicationSettingsScreen.Default;
        if (settingsScreen.BackButton.IsHit(point))
        {
            GuiOperationLog.User("Pressed settings Back button");
            _isApplicationSettingsOpen = false;
            _session.DeactivateModalWindow(ActiveWindowId.ApplicationSettings);
            return;
        }

        if (settingsScreen.GetTabHit(point) is { } page)
        {
            _applicationSettingsPage = page;
            _applicationSettingsMessage = "";
            GuiOperationLog.User("Changed settings tab", page.ToString());
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && settingsScreen.LogRootLink.IsHit(point))
        {
            GuiOperationLog.User("Pressed log folder Browse button");
            var selectedPath = _fileDialogService.SelectFolder(new FolderDialogOptions
            {
                InitialDirectory = ApplicationSettings.Current.LogRootDirectory,
                Title = "Select the folder which will contain the Gui and Cgos log folders.",
            });
            if (selectedPath is not null)
            {
                try
                {
                    var previous = ApplicationSettings.Current.LogRootDirectory;
                    ApplicationSettings.Save(selectedPath);
                    GuiOperationLog.User("Changed log folder", $"from={previous} to={ApplicationSettings.Current.LogRootDirectory}");
                    _applicationSettingsMessage = "SAVED. New log files will use this folder.";
                    RefreshGuiLogFiles();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    _applicationSettingsMessage = "ERROR: " + ex.Message;
                    ApplicationErrorLog.Write("SETTINGS", "Could not save the log folder.", ex);
                }
            }
            else
            {
                GuiOperationLog.User("Cancelled log folder selection");
            }
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.OtherFolders && settingsScreen.SgfFolderLink.IsHit(point))
        {
            GuiOperationLog.User("Pressed SGF folder Browse button");
            var selectedPath = _fileDialogService.SelectFolder(new FolderDialogOptions
            {
                InitialDirectory = Directory.Exists(ApplicationSettings.Current.SgfSaveDirectory)
                    ? ApplicationSettings.Current.SgfSaveDirectory
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Title = "Select the default folder for SGF game records.",
            });
            if (selectedPath is not null)
            {
                try
                {
                    ApplicationSettings.SaveSgfDirectory(selectedPath);
                    _applicationSettingsMessage = "SAVED. SGF files will start in this folder.";
                    GuiOperationLog.User("Changed SGF save folder", ApplicationSettings.Current.SgfSaveDirectory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    _applicationSettingsMessage = "ERROR: " + ex.Message;
                    ApplicationErrorLog.Write("SETTINGS", "Could not save the SGF folder.", ex);
                }
            }
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.OtherFolders && settingsScreen.ScreenshotFolderLink.IsHit(point))
        {
            GuiOperationLog.User("Pressed screenshot folder Browse button");
            var selectedPath = _fileDialogService.SelectFolder(new FolderDialogOptions
            {
                InitialDirectory = Directory.Exists(ApplicationSettings.Current.ScreenshotSaveDirectory)
                    ? ApplicationSettings.Current.ScreenshotSaveDirectory
                    : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Title = "Select the folder for window screenshots.",
            });
            if (selectedPath is not null)
            {
                try
                {
                    ApplicationSettings.SaveScreenshotDirectory(selectedPath);
                    _applicationSettingsMessage = "SAVED. Ctrl + P screenshots will use this folder.";
                    GuiOperationLog.User("Changed screenshot save folder", ApplicationSettings.Current.ScreenshotSaveDirectory);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
                {
                    _applicationSettingsMessage = "ERROR: " + ex.Message;
                    ApplicationErrorLog.Write("SETTINGS", "Could not save the screenshot folder.", ex);
                }
            }
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Other && settingsScreen.ApplicationSettingsFileLink.IsHit(point))
        {
            OpenSettingsFolder(ApplicationSettings.FilePath, "application settings");
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Other && settingsScreen.EngineSettingsFileLink.IsHit(point))
        {
            OpenSettingsFolder(_gtpEngineCatalog.ListPath, "engine settings");
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && settingsScreen.IsSelectedLogOpenBadgeHit(point, _selectedGuiLogIndex))
        {
            var path = _guiLogFiles[_selectedGuiLogIndex];
            OpenGuiLog(path, "Pressed Open log badge");
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && settingsScreen.GetLogItemHit(point, _guiLogFiles.Count) is { } index)
        {
            _selectedGuiLogIndex = index;
            _applicationSettingsMessage = Path.GetFileName(_guiLogFiles[index]);
            GuiOperationLog.User("Selected GUI log", _applicationSettingsMessage);
            OpenGuiLog(_guiLogFiles[index], "Opened GUI log link");
            return;
        }
    }

    private void OpenGuiLog(string path, string action)
    {
        GuiOperationLog.User(action, Path.GetFileName(path));
        try
        {
            var result = _desktopLauncher.OpenFileWithPreferredApplication(path, "code");
            _applicationSettingsMessage = result == DesktopOpenResult.PreferredApplication
                ? "OPENED IN CODE"
                : "CODE NOT FOUND; OPENED WITH DEFAULT APP";
        }
        catch (Exception ex)
        {
            _applicationSettingsMessage = "ERROR: " + ex.Message;
            ApplicationErrorLog.Write("OPEN GUI LOG", "Could not open the selected GUI log.", ex);
        }
    }

    private void OpenSettingsFolder(string filePath, string description)
    {
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new InvalidOperationException("Settings folder is unavailable.");
            }

            Directory.CreateDirectory(directory);
            _desktopLauncher.RevealFile(filePath);
            _applicationSettingsMessage = $"OPENED {description.ToUpperInvariant()} FOLDER";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _applicationSettingsMessage = "ERROR: " + ex.Message;
            ApplicationErrorLog.Write("OPEN SETTINGS FOLDER", $"Could not open the {description} folder.", ex);
        }
    }

    private void OpenTournamentRulesSettingsFile(string filePath)
    {
        try
        {
            var result = _desktopLauncher.OpenFileWithPreferredApplication(filePath, "code");
            _session.SetTournamentRulesDisplayNameWarning(
                result == DesktopOpenResult.PreferredApplication
                    ? "OPENED SETTINGS IN CODE"
                    : "CODE NOT FOUND; OPENED SETTINGS WITH DEFAULT APP");
            GuiOperationLog.User("Opened tournament rules settings", Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _session.SetTournamentRulesDisplayNameWarning("Could not open settings file.");
            ApplicationErrorLog.Write("TOURNAMENT RULES SETTINGS", "Could not open settings file.", ex);
        }
    }

    private void RefreshGuiLogFiles()
    {
        _guiLogFiles.Clear();
        var directory = Path.Combine(ApplicationSettings.Current.LogRootDirectory, "Gui");
        if (Directory.Exists(directory))
        {
            _guiLogFiles.AddRange(Directory.EnumerateFiles(directory, "*.log", SearchOption.TopDirectoryOnly)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .Take(4));
        }
        _selectedGuiLogIndex = _guiLogFiles.Count > 0 ? 0 : -1;
    }

    private void LogAutomaticScreenTransition()
    {
        var current = GetCurrentScreenState();
        if (current == _lastScreenState)
            return;

        GuiOperationLog.App("Screen changed", $"from={_lastScreenState} to={current}; breadcrumb={GetScreenBreadcrumb()}");
        // Board Lens の案内は操作した画面だけで表示し、画面遷移先へ持ち越さない。
        _boardLensBannerStartedAt = double.NegativeInfinity;
        _lastScreenState = current;
    }

    private void BeginDiscardTransition()
    {
        _screenTransitionStartedAt = _inputClockSeconds;
        PlayScreenTransitionSound();
    }

    private string GetCurrentScreenState() =>
        _isApplicationSettingsOpen
            ? "Application settings"
            : _session.UseKind is null
                ? $"Title/{_titleMenuPage}"
                : _session.UseKind == GoAppUseKind.LocalPlay
                    ? _playingScene.IsInitialPositionConciergeVisible
                        ? "Formal/LocalMatch/InitialPositionConcierge"
                        : $"Formal/LocalMatch/{_session.CurrentMode.Kind}"
                    : $"Formal/OnlineMatch.Cgos/{_session.CgosConnectionFlowKind}/{_session.CurrentMode.Kind}";

    private StickyNoteScreenId GetStickyNoteScreen()
    {
        if (_session.IsGtpEngineSelectionDialogOpen)
            return StickyNoteScreenId.GtpEngineSelection;
        if (_session.IsTournamentRulesSelectionDialogOpen)
            return StickyNoteScreenId.TournamentRulesSelection;
        if (_session.IsQuickClientIdentitySelectionPanelOpen)
            return StickyNoteScreenId.QuickClientIdentitySelection;
        if (_session.IsGtpEngineEditPanelOpen)
            return StickyNoteScreenId.GtpEngineEdit;
        if (_session.IsClientIdentityProfileSelectionPanelOpen)
            return StickyNoteScreenId.ClientIdentitySelection;
        if (_session.IsClientIdentityProfileEditPanelOpen)
            return StickyNoteScreenId.ClientIdentityEdit;
        if (_session.IsPlayerEditPanelOpen)
            return StickyNoteScreenId.EntryProfileEdit;
        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
            return StickyNoteScreenId.CgosConnection;

        if (_session.UseKind is not null || _isApplicationSettingsOpen)
            return StickyNoteScreenId.Unknown;

        return _titleMenuPage switch
        {
            TitleMenuPage.Home => StickyNoteScreenId.TitleHome,
            TitleMenuPage.CaptureGame => StickyNoteScreenId.TitlePonnukiProviderSelection,
            _ => StickyNoteScreenId.Unknown,
        };
    }

    private string GetScreenBreadcrumb()
    {
        if (_isApplicationSettingsOpen)
            return "TITLE  >  SETTINGS";

        if (_session.UseKind is null)
        {
            return _titleMenuPage switch
            {
                TitleMenuPage.Home => "TITLE",
                TitleMenuPage.CaptureGame => "TITLE  >  CASUAL APPS  >  PONNUKI",
                TitleMenuPage.Tsumego => "TITLE  >  CASUAL APPS  >  TSUMEGO",
                TitleMenuPage.NextMove => "TITLE  >  CASUAL APPS  >  NEXT MOVE",
                _ => "TITLE",
            };
        }

        var breadcrumb = _session.UseKind switch
        {
            GoAppUseKind.LocalPlay => GetLocalPlayBreadcrumb(),
            GoAppUseKind.LocalApps => "CASUAL APPS  >  LOCAL APPS",
            GoAppUseKind.CgosClient => GetCgosBreadcrumb(),
            _ => "FORMAL APPS",
        };

        if (_session.IsReviewChartPopupOpen)
            breadcrumb += "  >  POPUP TREND CHART";
        else if (_session.IsGtpEngineSelectionDialogOpen)
            breadcrumb += "  >  COMPUTER SELECT";
        else if (_session.IsGtpEngineEditPanelOpen)
            breadcrumb += "  >  COMPUTER EDIT";
        else if (_session.IsTournamentRulesSelectionDialogOpen)
            breadcrumb += "  >  TOURNAMENT RULE SELECT";
        else if (_session.IsTournamentRulesAddPanelOpen)
            breadcrumb += "  >  TOURNAMENT RULE EDIT";
        else if (_session.IsTournamentRulesDeleteConfirmationOpen)
            breadcrumb += "  >  TOURNAMENT RULE DELETE";

        return breadcrumb;
    }

    private void TryStepBoardLens(int direction)
    {
        if (_session.TryStepBoardLens(direction))
            _boardLensBannerStartedAt = _inputClockSeconds;
    }

    private string GetLocalPlayBreadcrumb()
    {
        if (_playingScene.IsInitialPositionConciergeVisible)
            return "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  INITIAL POSITION";

        return _session.CurrentMode.Kind switch
        {
            GoAppModeKind.Resting => "FORMAL APPS  >  LOCAL MATCH  >  INTERVAL",
            GoAppModeKind.Playing => "FORMAL APPS  >  LOCAL MATCH  >  PLAY",
            GoAppModeKind.GameOver => _session.IsLocalResultPosition
                ? "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  REVIEW  >  RESULT"
                : "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  REVIEW",
            GoAppModeKind.BoardEditing => "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  EDIT BOARD",
            GoAppModeKind.VariationEditing => "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  EDIT BOARD",
            GoAppModeKind.Reviewing => "FORMAL APPS  >  LOCAL MATCH  >  PLAY  >  REVIEW",
            _ => "FORMAL APPS  >  LOCAL MATCH",
        };
    }

    private string GetCgosBreadcrumb() =>
        _session.CgosConnectionFlowKind switch
        {
            CgosConnectionFlowKind.ProfileSelection => "Formal Apps > Online Match (CGOS) > Select Connection",
            CgosConnectionFlowKind.ConnectionStart => "Formal Apps > Online Match (CGOS) > Login",
            CgosConnectionFlowKind.Watching => "Formal Apps > Online Match (CGOS) > Watch",
            CgosConnectionFlowKind.Result => "Formal Apps > Online Match (CGOS) > Watch",
            _ => "FORMAL APPS  >  ONLINE MATCH (CGOS)",
        };

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.TextInput -= OnTextInput;
            _textCompositionService.CompositionChanged -= OnTextCompositionChanged;
            _textCompositionService.DiagnosticsChanged -= OnTextCompositionDiagnosticsChanged;
            Window.ClientSizeChanged -= OnWindowClientSizeChanged;
            Deactivated -= OnGameDeactivated;
            _cgosBlackConnectionProcess.Dispose();
            _cgosWhiteConnectionProcess.Dispose();
            _cgosAdminProcess.Dispose();
            _playingScene.Dispose();
            _upcomingMatchChimeInstance?.Dispose();
            _upcomingMatchChime?.Dispose();
            _screenshotShutterSoundInstance?.Dispose();
            _screenshotShutterSound?.Dispose();
            _screenTransitionSoundInstance?.Dispose();
            _screenTransitionSound?.Dispose();
            _placeStoneSoundInstance?.Dispose();
            _placeStoneSound?.Dispose();
            _presentationServices?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void PlayPlaceStoneSound(float volume = 1f, float pitch = 0f, float pan = 0f)
    {
        if (_placeStoneSoundInstance is null || _placeStoneSoundInstance.State == SoundState.Playing)
        {
            return;
        }

        _placeStoneSoundInstance.Volume = volume;
        _placeStoneSoundInstance.Pitch = pitch;
        _placeStoneSoundInstance.Pan = pan;
        _placeStoneSoundInstance.Play();
    }

    private void PlayUpcomingMatchChime()
    {
        if (_upcomingMatchChimeInstance is null)
        {
            return;
        }

        if (_upcomingMatchChimeInstance.State == SoundState.Playing)
        {
            _upcomingMatchChimeInstance.Stop();
        }

        _upcomingMatchChimeInstance.Volume = 0.72f;
        _upcomingMatchChimeInstance.Play();
    }

    private void PlayScreenshotShutterSound()
    {
        if (_screenshotShutterSoundInstance is null)
            return;

        if (_screenshotShutterSoundInstance.State == SoundState.Playing)
            _screenshotShutterSoundInstance.Stop();

        _screenshotShutterSoundInstance.Volume = 0.72f;
        _screenshotShutterSoundInstance.Play();
    }

    private void PlayScreenTransitionSound()
    {
        if (_screenTransitionSoundInstance is null)
            return;

        if (_screenTransitionSoundInstance.State == SoundState.Playing)
            _screenTransitionSoundInstance.Stop();

        _screenTransitionSoundInstance.Volume = 0.38f;
        _screenTransitionSoundInstance.Play();
    }

    private static SoundEffect CreateScreenTransitionSound()
    {
        const int sampleRate = 44100;
        const float duration = 1.5f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];
        uint noiseState = 0xB16B00B5;

        for (var i = 0; i < sampleCount; i++)
        {
            var time = i / (float)sampleRate;
            noiseState = noiseState * 1664525u + 1013904223u;
            var noise = ((noiseState >> 8) / 8388607.5f) - 1f;
            var crackle = MathF.Sign(noise) * MathF.Pow(MathF.Abs(noise), 4.2f);
            var buzzFrequency = 72f + time * 94f;
            var buzz = MathF.Sin(MathF.Tau * buzzFrequency * time) * 0.15f;
            var pulse = 0.42f + 0.58f * MathF.Pow(MathF.Abs(MathF.Sin(MathF.Tau * (6f + time * 4f) * time)), 6f);
            var envelope = Math.Clamp(time / 0.035f, 0f, 1f) * Math.Clamp((duration - time) / 0.18f, 0f, 1f);
            var wave = (crackle * 0.72f * pulse + buzz) * envelope;
            var sample = (short)(Math.Clamp(wave, -1f, 1f) * short.MaxValue * 0.68f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private static SoundEffect CreateScreenshotShutterSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.19f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];
        uint noiseState = 0x4B1D5EED;

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            noiseState = noiseState * 1664525u + 1013904223u;
            var noise = ((noiseState >> 8) / 8388607.5f) - 1f;
            var firstClick = ShutterPulse(t, 0f, 0.034f, 68f, noise);
            var secondClick = ShutterPulse(t, 0.072f, 0.052f, 52f, -noise);
            var mechanism = t >= 0.02f
                ? MathF.Sin(MathF.Tau * (118f - 240f * (t - 0.02f)) * (t - 0.02f)) * MathF.Exp(-24f * (t - 0.02f)) * 0.22f
                : 0f;
            var wave = Math.Clamp(firstClick + secondClick + mechanism, -1f, 1f);
            var sample = (short)(wave * short.MaxValue * 0.78f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);

        static float ShutterPulse(float time, float start, float pulseDuration, float decay, float noise)
        {
            var localTime = time - start;
            if (localTime < 0f || localTime >= pulseDuration)
                return 0f;

            var attack = Math.Clamp(localTime / 0.0015f, 0f, 1f);
            var envelope = attack * MathF.Exp(-decay * localTime);
            var metal = MathF.Sin(MathF.Tau * 1850f * localTime) * 0.34f +
                        MathF.Sin(MathF.Tau * 2730f * localTime) * 0.16f;
            return (noise * 0.72f + metal) * envelope;
        }
    }

    private static SoundEffect CreateUpcomingMatchChime()
    {
        const int sampleRate = 44100;
        const float duration = 0.82f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var wave = CreateBellTone(t, 0f, 0.34f, 659.25f) +
                       CreateBellTone(t, 0.38f, 0.40f, 783.99f);
            var sample = (short)(Math.Clamp(wave, -1f, 1f) * short.MaxValue * 0.52f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);

        static float CreateBellTone(float time, float start, float toneDuration, float frequency)
        {
            var localTime = time - start;
            if (localTime < 0f || localTime >= toneDuration)
            {
                return 0f;
            }

            var attack = Math.Clamp(localTime / 0.012f, 0f, 1f);
            var tail = Math.Clamp((toneDuration - localTime) / 0.045f, 0f, 1f);
            var envelope = attack * tail * MathF.Exp(-4.2f * localTime);
            var fundamental = MathF.Sin(MathF.Tau * frequency * localTime);
            var secondPartial = MathF.Sin(MathF.Tau * frequency * 2.01f * localTime) * 0.34f;
            var thirdPartial = MathF.Sin(MathF.Tau * frequency * 3.97f * localTime) * 0.16f;
            return (fundamental + secondPartial + thirdPartial) * envelope;
        }
    }

    private static SoundEffect CreatePlaceStoneSound()
    {
        const int sampleRate = 44100;
        const float duration = 0.09f;
        var sampleCount = (int)(sampleRate * duration);
        var buffer = new byte[sampleCount * sizeof(short)];

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var envelope = MathF.Exp(-42f * t);
            var wave = MathF.Sin(MathF.Tau * 520f * t) * 0.55f + MathF.Sin(MathF.Tau * 210f * t) * 0.45f;
            var sample = (short)(wave * envelope * short.MaxValue * 0.55f);
            buffer[i * 2] = (byte)(sample & 0xff);
            buffer[i * 2 + 1] = (byte)((sample >> 8) & 0xff);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }
}

internal enum CgosMatchNotificationMode
{
    None,
    Countdown,
    Deferred,
}
