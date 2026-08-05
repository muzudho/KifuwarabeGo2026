namespace KifuwarabeGo2026.Gui;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Connect;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Apps.Ponnuki;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Infrastructure.FileSystem;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Infrastructure;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.Cgos.Connect;
using KifuwarabeGo2026.Gui.Presentation.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Presentation.Cgos.Watching;
using KifuwarabeGo2026.Gui.Presentation.Local.Intermission;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Title;
using KifuwarabeGo2026.Gui.Sgf;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

public class Game1 : Game
{
    private const string ProductTitle = "Kifuwarabe Go 2026";
    private readonly GraphicsDeviceManager _graphics;
    private readonly IClipboardService _clipboardService;
    private readonly IMessageDialogService _messageDialogService;
    private readonly IFileDialogService _fileDialogService;
    private readonly ITextInputDialogService _textInputDialogService;
    private readonly IDesktopLauncher _desktopLauncher;
    private readonly ITextRasterizer _textRasterizer;
    private readonly IWindowIconService _windowIconService;
    private readonly IPlatformExecutableService _platformExecutableService;
    private readonly IWindowScreenshotService _windowScreenshotService;
    private readonly GoAppSession _session = new();
    private readonly TournamentRulesCatalog _tournamentRulesCatalog;
    private readonly GtpEngineCatalog _gtpEngineCatalog;
    private readonly CgosConnectionCatalog _cgosConnectionCatalog;
    private readonly TournamentRulesSetting _tournamentRulesSetting;
    private readonly PlayingScene _playingScene;
    private readonly CgosConnectionProcess _cgosBlackConnectionProcess;
    private readonly CgosConnectionProcess _cgosWhiteConnectionProcess;
    private readonly CgosConnectionProcess _cgosAdminProcess;
    private readonly CgosGameObservation _cgosGameObservation = new();
    private GoAppSession? _variationSession;
    private GoScreenRenderer? _renderer;
    private SoundEffect? _placeStoneSound;
    private SoundEffectInstance? _placeStoneSoundInstance;
    private SoundEffect? _upcomingMatchChime;
    private SoundEffectInstance? _upcomingMatchChimeInstance;
    private SoundEffect? _screenshotShutterSound;
    private SoundEffectInstance? _screenshotShutterSoundInstance;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private KeyboardState _previousScreenshotKeyboard;
    private KeyboardState _previousGtpEngineKeyboard;
    private readonly TextBoxController _gtpEngineEditTextBox = new(520);
    private readonly TextBoxController _gtpEngineIntegerOptionTextBox = new(11);
    private GtpEngineGuiOptionSpec? _activeGtpEngineIntegerOption;
    private KeyboardState _previousGtpEngineIntegerKeyboard;
    private string _gtpEngineIntegerInputMessage = "";
    private readonly TextBoxController _humanPlayerNameTextBox = new(80);
    private KeyboardState _previousHumanPlayerNameKeyboard;
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
    private string _appProviderSelectionLoadAppId = "";

    private const double CgosMatchCountdownSeconds = 10d;
    private const double CgosMatchFadeSeconds = 1.2d;
    private const double CgosMatchButtonDelaySeconds = 0.30d;
    private const double ReviewRepeatInitialDelaySeconds = 0.42d;
    private const double ReviewRepeatIntervalSeconds = 0.075d;
    private const double ReviewPopupDoubleClickSeconds = 0.36d;
    private const int ReviewPopupDoubleClickDistance = 18;
    private const double ScreenshotEffectDurationSeconds = 0.42d;

    public Game1(
        IClipboardService clipboardService,
        IMessageDialogService messageDialogService,
        IFileDialogService fileDialogService,
        ITextInputDialogService textInputDialogService,
        IDesktopLauncher desktopLauncher,
        ITextRasterizer textRasterizer,
        IWindowIconService windowIconService,
        IPlatformExecutableService platformExecutableService,
        IWindowScreenshotService windowScreenshotService)
    {
        _clipboardService = clipboardService;
        _messageDialogService = messageDialogService;
        _fileDialogService = fileDialogService;
        _textInputDialogService = textInputDialogService;
        _desktopLauncher = desktopLauncher;
        _textRasterizer = textRasterizer;
        _windowIconService = windowIconService;
        _platformExecutableService = platformExecutableService;
        _windowScreenshotService = windowScreenshotService;
        _cgosBlackConnectionProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "BlackPlayer");
        _cgosWhiteConnectionProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "WhitePlayer");
        _cgosAdminProcess = new CgosConnectionProcess(_desktopLauncher, _platformExecutableService, "Admin");
        _tournamentRulesCatalog = TournamentRulesCatalog.LoadFromDefaultLocation();
        _gtpEngineCatalog = GtpEngineCatalog.LoadFromDefaultLocation();
        _cgosConnectionCatalog = CgosConnectionCatalog.LoadFromDefaultLocation();
        _session.SetTournamentRules(_tournamentRulesCatalog.Rules);
        _session.SetGtpEngineProfiles(_gtpEngineCatalog.Profiles);
        _session.SetCgosConnectionProfiles(_cgosConnectionCatalog.Profiles);
        RefreshSgfAutoSaveState();
        _tournamentRulesSetting = new TournamentRulesSetting(
            _session,
            _tournamentRulesCatalog,
            OpenTournamentRulesSelectionDialog,
            _clipboardService);
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
        _renderer = new GoScreenRenderer(GraphicsDevice, Content, _textRasterizer);
        _placeStoneSound = CreatePlaceStoneSound();
        _placeStoneSoundInstance = _placeStoneSound.CreateInstance();
        _upcomingMatchChime = CreateUpcomingMatchChime();
        _upcomingMatchChimeInstance = _upcomingMatchChime.CreateInstance();
        _screenshotShutterSound = CreateScreenshotShutterSound();
        _screenshotShutterSoundInstance = _screenshotShutterSound.CreateInstance();
    }

    protected override void Update(GameTime gameTime)
    {
        _inputClockSeconds = gameTime.TotalGameTime.TotalSeconds;
        CompleteAppProviderSelectionLoading();
        var keyboard = Keyboard.GetState();
        var mouse = Mouse.GetState();
        SynchronizeOrArmWindowInput(keyboard, mouse);
        var acceptsInput = IsActive && _inputArmed;
        LogAutomaticScreenTransition();
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

        if (_session.UseKind is null)
        {
            UpdateAppProviderSelectionKeyboard(keyboard);
            UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
        }

        if (_session.UseKind is not (GoAppUseKind.LocalGame or GoAppUseKind.LocalApps))
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
        UpdateHumanPlayerNameTextBox(keyboard, gameTime);

        if (acceptsInput && _session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
            _tournamentRulesSetting.UpdateByKeyboard(keyboard, gameTime);
        }
        UpdateMouseInput();
        TryAutoSaveCompletedLocalGame();

        base.Update(gameTime);
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
            IsNewGlobalKeyPress(keyboard, Keys.Escape))
        {
            _session.CloseReviewChartPopup();
            ResetReadOnlyChartPopupDoubleClick();
            _previousKeyboard = keyboard;
            return;
        }

        if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing && TryHandleReviewKeyboardInput(keyboard))
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

        if (CanHandleGlobalRenParseToggle() && keyboard.IsKeyDown(Keys.R) && _previousKeyboard.IsKeyUp(Keys.R))
        {
            _session.ToggleRenParseDisplay();
        }

        _previousKeyboard = keyboard;
    }

    /// <summary>
    /// CGOS 観戦・結果画面で、通信を伴わないローカル表示操作を処理します。
    /// </summary>
    private void UpdateCgosWatchingKeyboardInput(KeyboardState keyboard)
    {
        if (!IsActive || !_inputArmed) return;

        if (_session.IsReviewChartPopupOpen && IsNewGlobalKeyPress(keyboard, Keys.Escape))
        {
            _session.CloseReviewChartPopup();
            ResetReadOnlyChartPopupDoubleClick();
            _previousKeyboard = keyboard;
            return;
        }

        var canToggle = _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result;
        if (canToggle && keyboard.IsKeyDown(Keys.R) && _previousKeyboard.IsKeyUp(Keys.R))
            _session.ToggleRenParseDisplay();

        _previousKeyboard = keyboard;
    }

    private bool IsNewGlobalKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    private bool TryHandleReviewKeyboardInput(KeyboardState keyboard)
    {
        if (_session.IsReviewChartPopupOpen && IsNewGlobalKeyPress(keyboard, Keys.Escape))
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
        if (navigation == int.MinValue)
        {
            MoveReview(-_session.ReviewMoveIndex);
        }
        else if (navigation == int.MaxValue)
        {
            MoveReview(_session.ReviewMoveCount - _session.ReviewMoveIndex);
        }
        else
        {
            MoveReview(navigation);
        }
    }

    private bool CanHandleGlobalRenParseToggle() =>
        _session.ActiveGtpEngineEditField is null &&
        !_session.IsTournamentRulesDisplayNameEditing;

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));
        if (_session.UseKind is null)
        {
            if (_renderer is not null)
            {
                if (_isApplicationSettingsOpen)
                    _renderer.DrawApplicationSettings(Mouse.GetState().Position, _applicationSettingsPage, ApplicationSettings.Current.LogRootDirectory, ApplicationSettings.Current.SgfSaveDirectory, ApplicationSettings.Current.ScreenshotSaveDirectory, ApplicationSettings.FilePath, _gtpEngineCatalog.ListPath, _guiLogFiles, _selectedGuiLogIndex, _applicationSettingsMessage);
                else
                    TitleRenderer.Draw(_renderer, _session, Mouse.GetState().Position, _titleMenuPage, _appProviderTabIndex, _appProviderSelectionLoadTask is not null);
            }
        }
        else if (_variationSession is not null)
        {
            if (_renderer is not null)
            {
                LocalIntermissionRenderer.Draw(
                    _renderer,
                    _variationSession,
                    Mouse.GetState().Position,
                    CreateLiveBoardPreview());
            }
        }
        else if (_session.UseKind == GoAppUseKind.CgosClient)
        {
            if (_renderer is not null)
            {
                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                {
                    LocalIntermissionRenderer.Draw(_renderer, _session, Mouse.GetState().Position);
                }
                else if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
                {
                    CgosWatchingRenderer.Draw(_renderer, _session, _cgosGameObservation, Mouse.GetState().Position);
                }
                else if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
                {
                    CgosConnectRenderer.Draw(_renderer, _session, Mouse.GetState().Position);
                }
                else
                {
                    CgosConnectionTargetRenderer.Draw(_renderer, _session, Mouse.GetState().Position);
                }
            }
        }
        else
        {
            if (_renderer is not null)
            {
                LocalIntermissionRenderer.Draw(
                    _renderer,
                    _session,
                    Mouse.GetState().Position,
                    initialPositionConcierge: _playingScene.InitialPositionConciergeView);
            }
        }

        if (_renderer is not null &&
            _session.UseKind == GoAppUseKind.CgosClient &&
            _cgosMatchNotificationMode != CgosMatchNotificationMode.None)
        {
            var notificationAge = GetCgosMatchNotificationAge();
            var notificationOpacity =
                (float)Math.Clamp(notificationAge.TotalSeconds / CgosMatchFadeSeconds, 0d, 1d);
            var buttonsEnabled =
                notificationAge.TotalSeconds >= CgosMatchButtonDelaySeconds;
            _renderer.DrawCgosMatchNotification(
                Mouse.GetState().Position,
                _cgosMatchNotificationMode == CgosMatchNotificationMode.Deferred,
                _cgosGameObservation.IsFinished,
                GetCgosMatchSecondsRemaining(notificationAge),
                notificationOpacity,
                notificationOpacity,
                buttonsEnabled,
                _session.CgosConnectionFlowKind != CgosConnectionFlowKind.Watching);
        }

        if (_renderer is not null)
        {
            var screenshotEffectAge = _inputClockSeconds - _screenshotEffectStartedAt;
            if (screenshotEffectAge >= 0d && screenshotEffectAge < ScreenshotEffectDurationSeconds)
                _renderer.DrawScreenshotCaptureEffect((float)(screenshotEffectAge / ScreenshotEffectDurationSeconds));
        }

        if (_renderer is not null && _activeGtpEngineIntegerOption is { } integerOption)
            _renderer.DrawIntegerInputDialog(Mouse.GetState().Position, integerOption.Label, _gtpEngineIntegerOptionTextBox.Text, _gtpEngineIntegerOptionTextBox.CaretIndex, _gtpEngineIntegerInputMessage);

        base.Draw(gameTime);
    }

    private void UpdateMouseInput()
    {
        if (!IsActive || !_inputArmed) return;

        var mouse = Mouse.GetState();
        var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
        UpdateTextBoxMouseDrag(mouse, point);
        UpdateCatalogOrderDrag(mouse, point);
        var engineErrorLogHovered = _session.UseKind == GoAppUseKind.LocalGame &&
            GoScreenRenderer.GetEngineErrorLogHit(point, _session);
        Mouse.SetCursor(engineErrorLogHovered ? MouseCursor.Hand : MouseCursor.Arrow);
        if (_variationSession is null)
        {
            UpdateReviewMouseRepeat(mouse, point);
            UpdateReviewPopupSeekDrag(mouse, point);
        }

        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            GuiOperationLog.User("Mouse click", $"screen={GetCurrentScreenState()} x={point.X} y={point.Y}");
            if (_activeGtpEngineIntegerOption is not null)
            {
                if (GoScreenRenderer.GetIntegerInputDialogOkButtonHit(point))
                    CommitGtpEngineIntegerInput();
                else if (GoScreenRenderer.GetIntegerInputDialogCancelButtonHit(point))
                    CancelGtpEngineIntegerInput();
                _previousMouse = mouse;
                return;
            }
            if (TryHandleCgosMatchNotificationClick(point))
            {
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
                else if (TitleRenderer.IsSettingsButtonHit(point))
                {
                    GuiOperationLog.User("Pressed Settings button");
                    _isApplicationSettingsOpen = true;
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
                (_session.UseKind == GoAppUseKind.LocalGame && _session.IsLocalReplayMode) ||
                (_session.UseKind == GoAppUseKind.CgosClient && _cgosGameObservation.IsReplayMode);
            var isVariationEditVisible =
                isReplayNavigationVisible ||
                _session.CurrentMode.Kind == GoAppModeKind.Reviewing ||
                (_session.UseKind == GoAppUseKind.LocalGame &&
                 _session.CanOpenLocalChartPopup) ||
                (_session.UseKind == GoAppUseKind.CgosClient &&
                 (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result) &&
                 _cgosGameObservation.IsStarted);
            var canReturnReplayToLive =
                (_session.UseKind == GoAppUseKind.LocalGame &&
                 _session.CurrentMode.Kind == GoAppModeKind.Playing &&
                 _session.IsLocalReplayMode) ||
                (_session.UseKind == GoAppUseKind.CgosClient &&
                 !_cgosGameObservation.IsFinished &&
                 _cgosGameObservation.IsReplayMode);
            if (canReturnReplayToLive && GoScreenRenderer.GetReplayBackToLiveButtonHit(point))
            {
                if (_session.UseKind == GoAppUseKind.LocalGame &&
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
            if (isVariationEditVisible && GoScreenRenderer.GetReplayEditButtonHit(point))
            {
                StartVariationEditingFromDisplayedPosition();
                _previousMouse = mouse;
                return;
            }
            if (isReplayNavigationVisible &&
                GoScreenRenderer.GetReplayStepButtonHit(point) is { } replayStep &&
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

                if (_session.IsCgosAdminPlayerSelectionDialogOpen)
                {
                    if (GoScreenRenderer.GetCgosAdminPlayerDialogCancelButtonHit(point))
                    {
                        _session.CancelCgosAdminPlayerSelectionDialog();
                    }
                    else if (GoScreenRenderer.GetCgosAdminPlayerDialogSelectButtonHit(point))
                    {
                        _session.CommitCgosAdminPlayerSelectionDialog();
                    }
                    else if (GoScreenRenderer.GetCgosAdminPlayerDialogPreviousPageButtonHit(point))
                    {
                        _session.MoveCgosAdminPlayerSelectionPage(-1);
                    }
                    else if (GoScreenRenderer.GetCgosAdminPlayerDialogNextPageButtonHit(point))
                    {
                        _session.MoveCgosAdminPlayerSelectionPage(1);
                    }
                    else if (GoScreenRenderer.GetCgosAdminPlayerDialogItemHit(point, _session) is { } playerIndex)
                    {
                        _session.SelectCgosAdminPlayerDialogItem(playerIndex);
                    }

                    _previousMouse = mouse;
                    return;
                }

                if (TryHandleGtpEngineEditPanelClick(point) || TryHandleGtpEngineSelectionDialogClick(point))
                {
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    _session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                    GoScreenRenderer.GetCgosCommentMoveStepButtonHit(point) is { } cgosCommentMoveStep)
                {
                    TrySeekReadOnlyAdjacentComment(cgosCommentMoveStep);
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    _session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                    GoScreenRenderer.GetCgosCommentPageStepButtonHit(point) is { } cgosCommentPageStep)
                {
                    _session.ChangeCommentPage(cgosCommentPageStep);
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    GoScreenRenderer.GetCgosMoveInformationDisplayModeButtonHit(point) is { } cgosInformationMode)
                {
                    _session.SetMoveInformationDisplayMode(cgosInformationMode);
                    GuiOperationLog.User("Changed CGOS move information display", $"mode={cgosInformationMode}");
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    GoScreenRenderer.GetCgosTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode) is { } trendMode)
                {
                    _session.SetMoveTrendDisplayMode(trendMode);
                    GuiOperationLog.User("Changed CGOS trend display", $"mode={trendMode}");
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
                    GoScreenRenderer.GetCgosLiveChartPopupOpenHit(point))
                {
                    ResetReadOnlyChartPopupDoubleClick();
                    _session.OpenCgosLiveChartPopup();
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Watching &&
                    GoScreenRenderer.GetCgosWatchingBackButtonHit(point))
                {
                    RestoreCgosMatchNotificationAfterLeavingView();
                    _session.ReturnToCgosConnectionScreen();
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Result)
                {
                    if (GoScreenRenderer.GetCgosWatchingReviewButtonHit(point))
                    {
                        StartReviewingGameRecord(_cgosGameObservation.CreateGameRecord(), "CGOS review");
                    }
                    else if (_session.IsSgfAutoSaveAvailable &&
                             GoScreenRenderer.GetCgosWatchingSgfAutoSaveCheckHit(point))
                    {
                        ToggleSgfAutoSave();
                        if (_session.IsSgfAutoSaveEnabled)
                        {
                            _lastAutoSavedCgosGameId = null;
                            TryAutoSaveCgosGame();
                        }
                    }
                    else if (!_session.IsSgfAutoSaveAvailable &&
                             GoScreenRenderer.GetCgosWatchingExportSgfButtonHit(point))
                    {
                        ExportSgf(
                            _cgosGameObservation.CreateGameRecord(),
                            CgosSgfFileNameBuilder.Create(_session.SelectedCgosConnectionProfile, _cgosGameObservation));
                    }
                    else if (GoScreenRenderer.GetCgosWatchingBackButtonHit(point))
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
                    if (GoScreenRenderer.GetCgosCredentialFieldHit(point) is { } credential &&
                        (credential.Stone == GoStone.Black || _session.IsCgosPlayer2InputEnabled))
                    {
                        BeginOrMoveCgosCredentialEdit(point, credential.Stone, credential.Field);
                    }
                    else
                    {
                        EndCgosCredentialEdit();
                        if (GoScreenRenderer.GetCgosConnectionStartBackButtonHit(point))
                        {
                            if (_session.IsAnyCgosProcessRunning) _ = DisconnectAllCgosProcessesAsync();
                            _session.ReturnToCgosConnectionProfiles();
                        }
                        else if (GoScreenRenderer.GetCgosPlayer2InputCheckHit(point, !_session.IsCgosWhiteConnectionRunning))
                        {
                            _session.ToggleCgosPlayer2Input();
                        }
                        else if (GoScreenRenderer.GetCgosAdminInputCheckHit(point, !_session.IsCgosAdminRunning))
                        {
                            _session.ToggleCgosAdminInput();
                        }
                        else if (GoScreenRenderer.GetCgosConnectionEngineSelectButtonHit(point, _session) is { } engineStone)
                        {
                            OpenCgosGtpEngineSelectionDialog(engineStone);
                        }
                        else if (GoScreenRenderer.GetCgosAdminButtonHit(
                                     point,
                                     _session.IsCgosAdminInputEnabled && _session.CgosConnectionProfiles.Count > 0))
                        {
                            ToggleCgosAdminProcess();
                        }
                        else if (GoScreenRenderer.GetCgosAdminWhoButtonHit(
                                     point,
                                     _session.IsCgosAdminInputEnabled && _session.IsCgosAdminRunning))
                        {
                            SendCgosAdminCommand("who");
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 GoScreenRenderer.GetCgosAdminWhitePlayerSelectButtonHit(point))
                        {
                            _session.OpenCgosAdminPlayerSelectionDialog(GoStone.White);
                        }
                        else if (_session.IsCgosAdminInputEnabled &&
                                 GoScreenRenderer.GetCgosAdminBlackPlayerSelectButtonHit(point))
                        {
                            _session.OpenCgosAdminPlayerSelectionDialog(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosAdminMatchButtonHit(point, _session.CanSendCgosAdminMatch))
                        {
                            SendSelectedCgosAdminMatch();
                        }
                        else if (GoScreenRenderer.GetCgosAdminSwapButtonHit(point, _session.CanSendCgosAdminMatch))
                        {
                            _session.SwapCgosAdminPlayers();
                        }
                        else if (GoScreenRenderer.GetCgosAdminCodeButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosAdminLogDirectory)))
                        {
                            OpenCgosAdminLog();
                        }
                        else if (GoScreenRenderer.GetCgosAdminTailButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosAdminLogDirectory)))
                        {
                            TailCgosAdminLog();
                        }
                        else if (GoScreenRenderer.GetCgosBlackResignButtonHit(
                                     point,
                                     _session.IsCgosGameInProgress && _session.IsCgosBlackConnectionRunning))
                        {
                            SendCgosPlayerResign(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosWhiteResignButtonHit(
                                     point,
                                     _session.IsCgosPlayer2InputEnabled &&
                                     _session.IsCgosGameInProgress &&
                                     _session.IsCgosWhiteConnectionRunning))
                        {
                            SendCgosPlayerResign(GoStone.White);
                        }
                        else if (GoScreenRenderer.GetCgosBlackConnectionButtonHit(
                                     point,
                                     _session.IsCgosBlackConnectionRunning || _session.SelectedCgosBlackGtpEngineProfile is not null,
                                     _session.IsCgosGameInProgress))
                        {
                            ToggleCgosPlayerConnectionProcess(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosWhiteConnectionButtonHit(
                                     point,
                                     _session.IsCgosPlayer2InputEnabled &&
                                     (_session.IsCgosWhiteConnectionRunning || _session.SelectedCgosWhiteGtpEngineProfile is not null),
                                     _session.IsCgosGameInProgress))
                        {
                            ToggleCgosPlayerConnectionProcess(GoStone.White);
                        }
                        else if (GoScreenRenderer.GetCgosPlayer1CodeButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosBlackConnectionLogDirectory)))
                        {
                            OpenCgosPlayerConnectionLog(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosPlayer1TailButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosBlackConnectionLogDirectory)))
                        {
                            TailCgosPlayerConnectionLog(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosPlayer2CodeButtonHit(
                                     point,
                                     _session.IsCgosPlayer2InputEnabled &&
                                     !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory)))
                        {
                            OpenCgosPlayerConnectionLog(GoStone.White);
                        }
                        else if (GoScreenRenderer.GetCgosPlayer2TailButtonHit(
                                     point,
                                     _session.IsCgosPlayer2InputEnabled &&
                                     !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory)))
                        {
                            TailCgosPlayerConnectionLog(GoStone.White);
                        }
                    }

                    _previousMouse = mouse;
                    return;
                }

                if (GoScreenRenderer.GetCgosBackButtonHit(point))
                {
                    _session.ReturnToUseSelection();
                }
                else if (GoScreenRenderer.GetCgosUseSelectedProfileButtonHit(point, _session.CgosConnectionProfiles.Count > 0))
                {
                    _session.OpenCgosConnectionStartScreen();
                }
                else if (GoScreenRenderer.GetCgosAddButtonHit(point))
                {
                    _session.OpenCgosConnectionAddPanel();
                }
                else if (GoScreenRenderer.GetCgosEditButtonHit(point) && _session.CgosConnectionProfiles.Count > 0)
                {
                    _session.OpenCgosConnectionEditPanel();
                }
                else if (GoScreenRenderer.GetCgosDuplicateButtonHit(point) && _session.CgosConnectionProfiles.Count > 0)
                {
                    _session.OpenCgosConnectionDuplicatePanel();
                }
                else if (GoScreenRenderer.GetCgosDeleteButtonHit(point, _session.CanDeleteSelectedCgosConnectionProfile))
                {
                    _session.RemoveSelectedCgosConnectionProfile();
                    _cgosConnectionCatalog.Save(_session.CgosConnectionProfiles);
                }
                else if (_session.CgosConnectionProfiles.Count > 1 &&
                         GoScreenRenderer.GetCgosOrderButtonHit(point))
                {
                    _session.OpenCgosConnectionOrderEditor();
                }
                else if (GoScreenRenderer.GetCgosPreviousPageButtonHit(point))
                {
                    _session.MoveCgosConnectionSelectionPage(-1);
                }
                else if (GoScreenRenderer.GetCgosNextPageButtonHit(point))
                {
                    _session.MoveCgosConnectionSelectionPage(1);
                }
                else if (GoScreenRenderer.GetCgosConnectionProfileHit(point, _session) is { } connectionProfileIndex)
                {
                    _session.SelectCgosConnectionProfile(connectionProfileIndex);
                }

                _previousMouse = mouse;
                return;
            }

            var isIntermissionMode = _session.CurrentMode.Kind == GoAppModeKind.Resting;
            var isSetupMode = isIntermissionMode && _session.UseKind == GoAppUseKind.LocalGame;
            var isLocalAppsIntermission = isIntermissionMode && _session.UseKind == GoAppUseKind.LocalApps;
            var isPlayerSelectionIntermission = isSetupMode || isLocalAppsIntermission;
            var isBoardEditing = _session.CurrentMode.Kind == GoAppModeKind.BoardEditing;
            var humanPlayerNameHit = isPlayerSelectionIntermission ? GoScreenRenderer.GetHumanPlayerNameTextBoxHit(point, _session) : null;
            if (_session.ActiveHumanPlayerNameStone is not null && humanPlayerNameHit is null)
                EndHumanPlayerNameEdit(commit: true);
            var handledByGtpEngineEditPanel = isPlayerSelectionIntermission && !isBoardEditing && TryHandleGtpEngineEditPanelClick(point);
            var handledByGtpEngineSelectionDialog = !handledByGtpEngineEditPanel && isPlayerSelectionIntermission && !isBoardEditing && TryHandleGtpEngineSelectionDialogClick(point);
            Func<Point, string, int>? getDisplayNameCaretIndex = _renderer is null
                ? null
                : (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_renderer, caretPoint, text);
            Func<Point, TournamentRulesNumericField, string, int>? getNumericCaretIndex = _renderer is null
                ? null
                : (caretPoint, field, text) => _renderer.GetTournamentRulesNumericCaretIndex(caretPoint, field, text);
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

            if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing && TryHandleReviewClick(point))
            {
                _previousMouse = mouse;
                return;
            }

            int? localCommentPageStep = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => GoScreenRenderer.GetLocalCommentPageStepButtonHit(point),
                GoAppModeKind.GameOver => GoScreenRenderer.GetLocalGameOverCommentPageStepButtonHit(point),
                _ => null,
            };
            if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
                _session.CurrentMode.Kind is GoAppModeKind.Playing or GoAppModeKind.GameOver)
            {
                int? localCommentMoveStep = _session.CurrentMode.Kind switch
                {
                    GoAppModeKind.Playing => GoScreenRenderer.GetLocalCommentMoveStepButtonHit(point),
                    GoAppModeKind.GameOver => GoScreenRenderer.GetLocalGameOverCommentMoveStepButtonHit(point),
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

            MoveInformationDisplayMode? localInformationMode = _session.CurrentMode.Kind switch
            {
                GoAppModeKind.Playing => GoScreenRenderer.GetLocalMoveInformationDisplayModeButtonHit(point),
                GoAppModeKind.GameOver => GoScreenRenderer.GetLocalGameOverMoveInformationDisplayModeButtonHit(point),
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
                GoAppModeKind.Playing => GoScreenRenderer.GetLocalTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode),
                GoAppModeKind.GameOver => GoScreenRenderer.GetLocalGameOverTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode),
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
                GoAppModeKind.Playing => GoScreenRenderer.GetLocalLiveChartPopupOpenHit(point),
                GoAppModeKind.GameOver => GoScreenRenderer.GetLocalGameOverChartPopupOpenHit(point),
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
            if ((isSetupMode || isLocalAppsIntermission) && GoScreenRenderer.GetSetupBackToTitleButtonHit(point))
            {
                _session.ReturnToUseSelection();
            }
            else if (isLocalAppsIntermission && GoScreenRenderer.GetAppProviderGameSettingsButtonHit(point))
            {
                OpenAppProviderGameSettings();
            }
            else if (isLocalAppsIntermission && GoScreenRenderer.GetChangeAppProviderButtonHit(point))
            {
                _session.ReturnToUseSelection();
                _titleMenuPage = TitleMenuPage.CaptureGame;
                GuiOperationLog.User("Returned to App Provider selection", "app=ponnuki");
            }
            else if (isLocalAppsIntermission &&
                     _session.CanStartPlaying &&
                     GoScreenRenderer.GetStartPlayingButtonHit(point, _session.CurrentMode.Kind))
            {
                StartPonnukiApp();
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver && GoScreenRenderer.GetReturnToSetupButtonHit(point))
            {
                _session.ReturnToSetup();
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver &&
                     GoScreenRenderer.GetLocalGameOverReviewButtonHit(point))
            {
                StartReviewingGameRecord(_session.CurrentGameRecord.Clone(), "Local review");
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver &&
                     _session.IsSgfAutoSaveAvailable &&
                     GoScreenRenderer.GetSgfAutoSaveCheckHit(point))
            {
                ToggleSgfAutoSave();
                if (_session.IsSgfAutoSaveEnabled)
                {
                    _lastAutoSavedLocalGameRecord = null;
                    TryAutoSaveCompletedLocalGame();
                }
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver &&
                     !_session.IsSgfAutoSaveAvailable &&
                     GoScreenRenderer.GetExportSgfButtonHit(point))
            {
                ExportSgf();
            }
            else if (isSetupMode && GoScreenRenderer.GetImportSgfButtonHit(point))
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
            else if (isSetupMode && GoScreenRenderer.GetStartReviewingButtonHit(point, _session.HasReviewGameRecord))
            {
                StartReviewingStoredGameRecord();
            }
            else if (isSetupMode && GoScreenRenderer.GetStartBoardEditingButtonHit(point, _session.CurrentMode.Kind))
            {
                StartWhiteboardFromLocalSetup();
            }
            else if (isSetupMode &&
                     _session.CanStartPlaying &&
                     GoScreenRenderer.GetStartPlayingButtonHit(point, _session.CurrentMode.Kind))
            {
                _playingScene.StartPlaying();
            }
            else if (isPlayerSelectionIntermission && GoScreenRenderer.GetBlackPlayerKindButtonHit(point) is { } blackPlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.Black, blackPlayerKind);
            }
            else if (isPlayerSelectionIntermission && _session.BlackPlayerKind == GoPlayerKind.Computer && GoScreenRenderer.GetBlackGtpEngineBrowseButtonHit(point))
            {
                OpenGtpEngineSelectionDialog(GoStone.Black);
            }
            else if (isPlayerSelectionIntermission && GoScreenRenderer.GetWhitePlayerKindButtonHit(point) is { } whitePlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.White, whitePlayerKind);
            }
            else if (isPlayerSelectionIntermission && _session.WhitePlayerKind == GoPlayerKind.Computer && GoScreenRenderer.GetWhiteGtpEngineBrowseButtonHit(point))
            {
                OpenGtpEngineSelectionDialog(GoStone.White);
            }
            else if (humanPlayerNameHit is { } playerNameStone)
            {
                BeginHumanPlayerNameEdit(point, playerNameStone);
            }
            else if (GoScreenRenderer.GetEngineErrorLogHit(point, _session))
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

    private void UpdateTextBoxMouseDrag(MouseState mouse, Point point)
    {
        if (mouse.LeftButton == ButtonState.Released)
        {
            _cgosConnectionEditTextBox.EndMouseSelection();
            _cgosCredentialTextBox.EndMouseSelection();
            _humanPlayerNameTextBox.EndMouseSelection();
            _gtpEngineEditTextBox.EndMouseSelection();
            _tournamentRulesSetting.EndMouseSelection();
            return;
        }

        if (_renderer is null || _previousMouse.LeftButton != ButtonState.Pressed) return;

        if (_cgosConnectionEditTextBox.IsMouseSelecting &&
            _session.ActiveCgosConnectionEditField is { } connectionField)
        {
            _cgosConnectionEditTextBox.UpdateMouseSelection(
                _renderer.GetCgosConnectionEditPanelCaretIndex(point, connectionField, _cgosConnectionEditTextBox.Text));
            SyncCgosConnectionEditField(connectionField);
        }
        else if (_cgosCredentialTextBox.IsMouseSelecting &&
                 _session.ActiveCgosCredentialStone is { } credentialStone &&
                 _session.ActiveCgosCredentialField is { } credentialField)
        {
            _cgosCredentialTextBox.UpdateMouseSelection(
                _renderer.GetCgosCredentialCaretIndex(point, credentialStone, credentialField, _cgosCredentialTextBox.Text));
            _session.SetCgosCredential(credentialStone, credentialField, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
            SyncCgosCredentialSelection();
        }
        else if (_humanPlayerNameTextBox.IsMouseSelecting &&
                 _session.ActiveHumanPlayerNameStone is { } humanStone)
        {
            _humanPlayerNameTextBox.UpdateMouseSelection(
                _renderer.GetHumanPlayerNameCaretIndex(point, humanStone, _humanPlayerNameTextBox.Text));
            _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
            _session.SetHumanPlayerNameSelection(_humanPlayerNameTextBox.SelectionStart, _humanPlayerNameTextBox.SelectionLength);
        }
        else if (_gtpEngineEditTextBox.IsMouseSelecting &&
                 _session.ActiveGtpEngineEditField is { } engineField)
        {
            _gtpEngineEditTextBox.UpdateMouseSelection(
                _renderer.GetGtpEngineEditPanelCaretIndex(point, engineField, _gtpEngineEditTextBox.Text));
            SyncGtpEngineEditField(engineField);
        }

        _tournamentRulesSetting.UpdateMouseSelection(
            point,
            (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_renderer, caretPoint, text),
            (caretPoint, field, text) => _renderer.GetTournamentRulesNumericCaretIndex(caretPoint, field, text));
    }

    private static bool IsShiftDown()
    {
        var keyboard = Keyboard.GetState();
        return keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
    }

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
        UpdateCatalogOrderDrag(_session.TournamentRulesOrderEditor, mouse, point);
        UpdateCatalogOrderDrag(_session.GtpEngineOrderEditor, mouse, point);
        UpdateCatalogOrderDrag(_session.CgosConnectionOrderEditor, mouse, point);
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

        if (GoScreenRenderer.GetCatalogOrderCardHit(point, editor) is { } index)
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

    private void StartPonnukiApp()
    {
        _session.ClearLocalAppsError();
        try
        {
            var provider = _session.SelectedAppProviderEngine;
            StopPonnukiProviderGame();
            _ponnukiProviderGameSession = new PonnukiProviderGameSession(provider);
            var record = _ponnukiProviderGameSession.StartAsync().GetAwaiter().GetResult();
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

    private bool TryHandleTitleMenuClick(Point point)
    {
        if (TitleRenderer.IsBackButtonHit(point))
        {
            _titleMenuPage = TitleMenuPage.Home;
            GuiOperationLog.User("Pressed title menu Back button", $"page={_titleMenuPage}");
            return true;
        }

        if (_titleMenuPage == TitleMenuPage.Home)
        {
            if (TitleRenderer.IsLocalGameButtonHit(point))
            {
                GuiOperationLog.User("Pressed Local button", "Navigate from title to local setup");
                _session.SelectUseKind(GoAppUseKind.LocalGame);
                return true;
            }

            if (TitleRenderer.IsCgosClientButtonHit(point))
            {
                GuiOperationLog.User("Pressed CGOS button", "Navigate from title to CGOS connection selection");
                _session.SelectUseKind(GoAppUseKind.CgosClient);
                return true;
            }

            if (TitleRenderer.GetAppHit(point) is { } appIndex)
            {
                _titleMenuPage = appIndex switch
                {
                    0 => TitleMenuPage.CaptureGame,
                    1 => TitleMenuPage.Tsumego,
                    _ => TitleMenuPage.NextMove,
                };
                GuiOperationLog.User("Opened Go Apps entry", $"page={_titleMenuPage}");
                return true;
            }
        }

        if (_titleMenuPage == TitleMenuPage.CaptureGame)
        {
            if (TitleRenderer.IsAppProviderEngineSelectButtonHit(point))
            {
                BeginOpenAppProviderGtpEngineSelectionDialog("ponnuki");
                return true;
            }

            if (_session.CanUseSelectedAppProvider && TitleRenderer.IsAppProviderRecheckButtonHit(point))
            {
                RecheckPonnukiProvider();
                return true;
            }

            if (_session.CanStartSelectedAppProvider && TitleRenderer.IsAppProviderStartButtonHit(point))
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
            _session.CanUseSelectedAppProvider,
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

        if (GoScreenRenderer.GetBoardEditingBlackButtonHit(point))
        {
            _session.SetBoardEditingStone(GoStone.Black);
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingWhiteButtonHit(point))
        {
            _session.SetBoardEditingStone(GoStone.White);
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingEraseButtonHit(point))
        {
            _session.SetBoardEditingStone(GoStone.Empty);
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingUndoButtonHit(point))
        {
            _session.UndoBoardEditing();
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingRedoButtonHit(point))
        {
            _session.RedoBoardEditing();
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingClearButtonHit(point))
        {
            if (_session.ClearBoardEditing())
                PlayPlaceStoneSound(0.42f, -0.35f, 0f);
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingCancelButtonHit(point))
        {
            _session.CancelBoardEditing();
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingAdoptButtonHit(point))
        {
            _session.FinishBoardEditing();
            return true;
        }

        if (GoScreenRenderer.TryGetBoardIntersection(point, _session.BoardSize, out var intersection))
        {
            if (_session.TryEditBoardStone(intersection.X, intersection.Y))
            {
                PlayPlaceStoneSound(_session.BoardEditingStone == GoStone.Empty ? 0.42f : 0.78f);
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

        if (GoScreenRenderer.GetReviewStepButtonHit(point) is { } step)
        {
            ExecuteReviewNavigation(step);
            _reviewMouseRepeatCommand = step is int.MinValue or int.MaxValue ? null : step;
            _reviewMouseNextRepeatAt = _inputClockSeconds + ReviewRepeatInitialDelaySeconds;
            return true;
        }

        if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
            GoScreenRenderer.GetReviewCommentMoveStepButtonHit(point) is { } reviewCommentMoveStep)
        {
            TryMoveReviewAdjacentComment(reviewCommentMoveStep);
            return true;
        }

        if (_session.MoveInformationDisplayMode == MoveInformationDisplayMode.Comment &&
            GoScreenRenderer.GetReviewCommentPageStepButtonHit(point) is { } reviewCommentPageStep)
        {
            _session.ChangeCommentPage(reviewCommentPageStep);
            return true;
        }

        if (GoScreenRenderer.GetReviewMoveInformationDisplayModeButtonHit(point) is { } reviewInformationMode)
        {
            _session.SetMoveInformationDisplayMode(reviewInformationMode);
            return true;
        }

        if (GoScreenRenderer.GetReviewTrendDisplayModeButtonHit(point, _session.MoveTrendDisplayMode) is { } reviewTrendMode)
        {
            _session.SetMoveTrendDisplayMode(reviewTrendMode);
            return true;
        }

        if (GoScreenRenderer.GetReviewChartPopupOpenHit(point))
        {
            _session.OpenReviewChartPopup();
            _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            return true;
        }

        if (_session.UseKind == GoAppUseKind.LocalGame && GoScreenRenderer.GetReviewDoneButtonHit(point))
        {
            _session.FinishReviewing();
            return true;
        }

        if (GoScreenRenderer.GetReviewBackToRestButtonHit(point))
        {
            _session.ReturnFromReviewingToResting();
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
    }

    private void StartWhiteboardFromLocalSetup()
    {
        var sourceRecord = _session.CurrentGameRecord.Clone();
        var variationSession = new GoAppSession();
        variationSession.SelectUseKind(GoAppUseKind.LocalGame);
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
    }

    private bool TryHandleVariationEditingClick(Point point)
    {
        var variationSession = _variationSession;
        if (variationSession is null ||
            variationSession.CurrentMode.Kind != GoAppModeKind.VariationEditing)
            return false;

        if (GoScreenRenderer.GetVariationEditingDiscardButtonHit(point))
        {
            _variationSession = null;
            return true;
        }

        if (variationSession.CanAdoptVariationPosition &&
            GoScreenRenderer.GetVariationEditingAdoptButtonHit(point))
        {
            var adoptedRecord = variationSession.CreateCurrentPositionAsSetupRecord();
            if (_session.LoadGameRecordAsInitialPosition(adoptedRecord, out var warning))
            {
                _variationSession = null;
            }
            else if (!string.IsNullOrWhiteSpace(warning))
            {
                ShowMessage(warning, "Whiteboard");
            }
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingExportSgfButtonHit(point))
        {
            ExportSgf(
                variationSession.CurrentGameRecord,
                $"kifuwarabe-analysis-{DateTime.Now:yyyyMMdd-HHmmss}.sgf",
                markCurrentResultSaved: false);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingPlayButtonHit(point))
        {
            variationSession.SetVariationEditingStone(null);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingBlackButtonHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.Black);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingWhiteButtonHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.White);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingEraseButtonHit(point))
        {
            variationSession.SetVariationEditingStone(GoStone.Empty);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingClearButtonHit(point))
        {
            if (variationSession.ClearVariationBoard())
                PlayPlaceStoneSound(0.42f, -0.35f, 0f);
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingUndoButtonHit(point))
        {
            variationSession.UndoVariation();
            return true;
        }

        if (GoScreenRenderer.GetVariationEditingPassButtonHit(point))
        {
            if (variationSession.VariationEditingStone is null &&
                variationSession.PassVariation())
                PlayPlaceStoneSound(0.45f, 0.25f, 0f);
            return true;
        }

        if (GoScreenRenderer.TryGetBoardIntersection(point, variationSession.BoardSize, out var intersection))
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
    private void StartReviewingGameRecord(GoGameRecord record, string messageTitle)
    {
        if (!_session.StartReviewingGameRecord(record, out var warning) && !string.IsNullOrWhiteSpace(warning))
        {
            ShowMessage(warning, messageTitle);
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

        if (GoScreenRenderer.GetCgosConnectionEditPanelCloseButtonHit(point))
        {
            EndCgosConnectionEditField();
            _cgosConnectionEditTextBox.Clear();
            _session.CloseCgosConnectionEditPanel();
            return true;
        }

        if (GoScreenRenderer.GetCgosConnectionEditPanelSaveButtonHit(point))
        {
            SaveCgosConnectionEditDraft();
            return true;
        }

        if (GoScreenRenderer.GetCgosConnectionEditPanelFieldHit(point) is { } field)
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
            SaveCgosConnectionEditDraft();
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
        if (_session.CurrentMode.Kind != GoAppModeKind.Reviewing ||
            mouse.LeftButton != ButtonState.Pressed)
        {
            _reviewMouseRepeatCommand = null;
            return;
        }

        if (_previousMouse.LeftButton != ButtonState.Pressed ||
            _reviewMouseRepeatCommand is not { } command ||
            GoScreenRenderer.GetReviewStepButtonHit(point) != command)
        {
            return;
        }

        if (_inputClockSeconds < _reviewMouseNextRepeatAt) return;
        _reviewMouseNextRepeatAt = _inputClockSeconds + ReviewRepeatIntervalSeconds;
        ExecuteReviewNavigation(command);
    }

    private void HandleReviewChartPopupClick(Point point)
    {
        if (GoScreenRenderer.GetReviewChartPopupCloseHit(point))
        {
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            _lastReviewPopupSeekClickAt = double.NegativeInfinity;
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupScoreToggleHit(point))
        {
            _session.TogglePopupScoreVisibility();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupWinRateToggleHit(point))
        {
            _session.TogglePopupWinRateVisibility();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupCommentToggleHit(point))
        {
            _session.TogglePopupCommentVisibility();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            GoScreenRenderer.GetReviewChartPopupCommentMoveStepButtonHit(point) is { } commentMoveStep)
        {
            TryMoveReviewAdjacentComment(commentMoveStep);
            return;
        }

        if (_session.IsPopupCommentVisible &&
            GoScreenRenderer.GetReviewChartPopupCommentPageStepButtonHit(point) is { } commentPageStep)
        {
            _session.ChangeCommentPage(commentPageStep);
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupStepButtonHit(point) is { } popupStep)
        {
            ExecuteReviewNavigation(popupStep);
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupSeekMove(point, _session.ReviewMoveCount) is { } moveIndex)
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
        if (GoScreenRenderer.GetReviewChartPopupCloseHit(point))
        {
            _session.CloseReviewChartPopup();
            _reviewPopupSeekDragging = false;
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupBackToLiveHit(point))
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

        if (GoScreenRenderer.GetReviewChartPopupAutoUpdateHit(point))
        {
            var moveCount = _session.UseKind == GoAppUseKind.CgosClient
                ? _cgosGameObservation.MoveCount
                : _session.CurrentGameRecord.Moves.Count;
            _session.ToggleLiveChartAutoUpdate(moveCount);
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupScoreToggleHit(point))
        {
            _session.TogglePopupScoreVisibility();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupWinRateToggleHit(point))
        {
            _session.TogglePopupWinRateVisibility();
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupCommentToggleHit(point))
        {
            _session.TogglePopupCommentVisibility();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            GoScreenRenderer.GetReviewChartPopupCommentMoveStepButtonHit(point) is { } commentMoveStep)
        {
            TrySeekReadOnlyAdjacentComment(commentMoveStep);
            ResetReadOnlyChartPopupDoubleClick();
            return;
        }

        if (_session.IsPopupCommentVisible &&
            GoScreenRenderer.GetReviewChartPopupCommentPageStepButtonHit(point) is { } commentPageStep)
        {
            _session.ChangeCommentPage(commentPageStep);
            return;
        }

        if (GoScreenRenderer.GetReviewChartPopupStepButtonHit(point) is { } popupStep &&
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
            GoScreenRenderer.GetReviewChartPopupSeekMove(
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
            GoScreenRenderer.GetReviewChartPopupSeekMove(
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
            currentMoveIndex = _session.LocalDisplayMoveIndex;
            maximumMoveIndex = _session.CurrentMode.Kind == GoAppModeKind.GameOver
                ? _session.CurrentGameRecord.Moves.Count
                : _session.GetLiveChartVisibleMoveCount(_session.CurrentGameRecord.Moves.Count);
            return true;
        }

        currentMoveIndex = 0;
        maximumMoveIndex = 0;
        return false;
    }

    private LiveBoardPreview? CreateLiveBoardPreview()
    {
        if (_variationSession is null)
            return null;

        if (IsLocalPlayUseKind() &&
            _session.CurrentMode.Kind == GoAppModeKind.Playing)
        {
            var moves = _session.CurrentGameRecord.Moves;
            return new LiveBoardPreview(
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
            return new LiveBoardPreview(
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
        if (!_session.IsReviewChartPopupOpen || mouse.LeftButton != ButtonState.Pressed)
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
            if (GoScreenRenderer.GetReviewChartPopupSeekMove(point, _session.ReviewMoveCount) is { } reviewMoveIndex &&
                reviewMoveIndex != _session.ReviewMoveIndex)
            {
                MoveReview(reviewMoveIndex - _session.ReviewMoveIndex);
            }
            return;
        }

        if (_session.UseKind == GoAppUseKind.CgosClient &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result &&
            GoScreenRenderer.GetReviewChartPopupSeekMove(
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
            GoScreenRenderer.GetReviewChartPopupSeekMove(
                point,
                _session.CurrentMode.Kind == GoAppModeKind.GameOver
                    ? _session.CurrentGameRecord.Moves.Count
                    : _session.GetLiveChartVisibleMoveCount(_session.CurrentGameRecord.Moves.Count)) is { } localMoveIndex)
        {
            _session.SeekLocalReplay(localMoveIndex);
        }
    }

    private bool IsLocalPlayUseKind() =>
        _session.UseKind is GoAppUseKind.LocalGame or GoAppUseKind.LocalApps;

    private void BeginCgosMatchNotification()
    {
        if (_cgosMatchNotificationGameId == _cgosGameObservation.GameId)
            return;

        _cgosMatchNotificationGameId = _cgosGameObservation.GameId;
        _cgosMatchNotificationStartedAt = DateTimeOffset.UtcNow;
        _cgosMatchNotificationMode = IsCgosConnectionWaitingScreen()
            ? CgosMatchNotificationMode.Countdown
            : CgosMatchNotificationMode.Deferred;
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
            _cgosMatchNotificationMode = CgosMatchNotificationMode.None;
            return;
        }

        if (_cgosGameObservation.IsFinished &&
            _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result)
        {
            _cgosMatchNotificationMode = CgosMatchNotificationMode.None;
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
                return GoScreenRenderer.GetCgosMatchDeferredBannerHit(point);

            if (!GoScreenRenderer.GetCgosMatchDeferredHit(point))
                return GoScreenRenderer.GetCgosMatchDeferredBannerHit(point);

            OpenNotifiedCgosMatch("Pressed deferred match notification");
            return true;
        }

        var buttonsEnabled =
            GetCgosMatchNotificationAge().TotalSeconds >= CgosMatchButtonDelaySeconds;
        if (GoScreenRenderer.GetCgosMatchWatchNowHit(point, buttonsEnabled))
        {
            OpenNotifiedCgosMatch("Pressed WATCH NOW");
            return true;
        }

        if (GoScreenRenderer.GetCgosMatchWatchLaterHit(point, buttonsEnabled))
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
        _cgosMatchNotificationMode = CgosMatchNotificationMode.None;
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

    private void RestoreCgosMatchNotificationAfterLeavingView()
    {
        if (!_cgosGameObservation.IsStarted || _cgosGameObservation.IsFinished)
            return;

        _cgosMatchNotificationGameId = _cgosGameObservation.GameId;
        _cgosMatchNotificationMode = CgosMatchNotificationMode.Deferred;
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
        var caretIndex = _renderer?.GetCgosConnectionEditPanelCaretIndex(point, field, text) ?? text.Length;

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

    private void SaveCgosConnectionEditDraft()
    {
        EndCgosConnectionEditField();
        if (!ValidateCgosConnectionEditDraft(out var profile, out var warning))
        {
            _session.SetCgosConnectionEditWarning(warning);
            return;
        }

        _session.SaveCgosConnectionEditDraft(profile);
        _cgosConnectionCatalog.Save(_session.CgosConnectionProfiles);
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
        if (_activeGtpEngineIntegerOption is not null)
        {
            if (char.IsDigit(e.Character) || (e.Character == '-' && _gtpEngineIntegerOptionTextBox.CaretIndex == 0))
                _gtpEngineIntegerOptionTextBox.TryInputCharacter(e.Character);
            return;
        }
        if (!IsActive || !_inputArmed) return;

        // モーダル表示中の入力欄を、背後の画面に残った編集状態より優先します。
        if (_session.IsGtpEngineEditPanelOpen && TryInputGtpEngineEditCharacter(e.Character))
        {
            return;
        }

        if (TryInputHumanPlayerNameCharacter(e.Character)) return;

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
        var caret = _renderer?.GetCgosCredentialCaretIndex(point, stone, field, text) ?? text.Length;
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
        var caretIndex = _renderer?.GetHumanPlayerNameCaretIndex(point, stone, text) ?? text.Length;
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
            StartReviewingGameRecord(record, "SGF input");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SgfParseException or ArgumentOutOfRangeException)
        {
            ShowMessage(ex.Message, "SGF input");
        }
    }

    private void ExportSgf() =>
        ExportSgf(_session.CurrentGameRecord, $"kifuwarabe-go-{DateTime.Now:yyyyMMdd-HHmmss}.sgf");

    /// <summary>
    /// 指定された棋譜を Local と共通の保存フローで SGF 出力します。
    /// </summary>
    private void ExportSgf(
        GoGameRecord record,
        string fileName,
        bool markCurrentResultSaved = true)
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
            return;
        }

        try
        {
            var sgf = SgfGameRecordConverter.ToSgf(record);
            File.WriteAllText(selectedFileName, sgf, Encoding.UTF8);
            RememberSgfDirectory(selectedFileName);
            RefreshSgfAutoSaveState();
            if (markCurrentResultSaved)
                MarkCurrentResultSgfSaved();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage(ex.Message, "SGF output");
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
            $"kifuwarabe-go-{DateTime.Now:yyyyMMdd-HHmmss}.sgf"))
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
        if (_session.UseKind == GoAppUseKind.LocalGame &&
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

    private void ShowMessage(string message, string caption) =>
        _messageDialogService.ShowWarning(caption, message);

    private void OpenGtpEngineSelectionDialog(GoStone stone)
    {
        var appId = _session.UseKind == GoAppUseKind.LocalApps ? "ponnuki" : "play";
        RefreshGtpEngineAppCompatibilities(appId, "player");
        _session.OpenGtpEngineSelectionDialog(stone, appId);
    }

    private void OpenCgosGtpEngineSelectionDialog(GoStone stone)
    {
        RefreshGtpEngineAppCompatibilities("play", "player");
        _session.OpenCgosGtpEngineSelectionDialog(stone);
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

    private void RefreshGtpEngineAppCompatibilities(string appId, string role)
    {
        var checks = _session.GtpEngineProfiles
            .Select(profile => GtpEngineAppCompatibilityProbe.CheckAsync(profile, appId, role))
            .ToArray();
        var results = Task.WhenAll(checks).GetAwaiter().GetResult();
        _session.SetGtpEngineAppCompatibilities(results);
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

        if (GoScreenRenderer.TryGetGtpEngineSelectionDialogPathCopyText(point, _session, out var path))
        {
            _clipboardService.TrySetText(path);
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogCancelButtonHit(point))
        {
            _session.CancelGtpEngineSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogOkButtonHit(point))
        {
            if (_session.CanCommitGtpEngineSelection)
            {
                _session.CommitGtpEngineSelectionDialog();
                if (_session.EngineSelectionPurpose == GtpEngineSelectionPurpose.AppProvider)
                    RecheckPonnukiProvider();
            }
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogAddButtonHit(point))
        {
            _session.OpenGtpEngineAddPanel();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogEditButtonHit(point))
        {
            _session.OpenGtpEngineEditPanel();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogDuplicateButtonHit(point))
        {
            _session.OpenGtpEngineDuplicatePanel();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogDeleteButtonHit(point, _session.CanDeleteSelectedGtpEngine))
        {
            _session.OpenGtpEngineDeleteConfirmation();
            return true;
        }

        if (_session.GtpEngineProfiles.Count > 1 &&
            GoScreenRenderer.GetGtpEngineSelectionDialogOrderButtonHit(point))
        {
            _session.OpenGtpEngineOrderEditor();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogPreviousPageButtonHit(point))
        {
            _session.MoveGtpEngineSelectionPage(-1);
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogNextPageButtonHit(point))
        {
            _session.MoveGtpEngineSelectionPage(1);
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogListItemHit(point, _session) is { } index)
        {
            if (_session.CanSelectGtpEngineForCurrentApp(index))
                _session.SelectGtpEngineDialogItem(index);
            return true;
        }

        return true;
    }

    private bool TryHandleCgosConnectionOrderEditorClick(Point point)
    {
        var editor = _session.CgosConnectionOrderEditor;
        if (GoScreenRenderer.GetCatalogOrderCancelButtonHit(point))
        {
            _session.CancelCgosConnectionOrderEditor();
            return true;
        }

        if (GoScreenRenderer.GetCatalogOrderSaveButtonHit(point))
        {
            var profiles = _session.CommitCgosConnectionOrderEditor();
            _cgosConnectionCatalog.Save(profiles);
            return true;
        }

        var moveStep = GoScreenRenderer.GetCatalogOrderMoveStep(point, editor.PageSize);
        if (moveStep == int.MinValue)
            editor.MoveSelectedToTop();
        else if (moveStep != 0)
            editor.MoveSelected(moveStep);
        else if (GoScreenRenderer.GetCatalogOrderPagePairStep(point) is var pageStep && pageStep != 0)
            editor.MovePagePair(pageStep);
        else if (GoScreenRenderer.GetCatalogOrderCardHit(point, editor) is { } index)
            editor.BeginDrag(index);

        return true;
    }

    private bool TryHandleGtpEngineOrderEditorClick(Point point)
    {
        var editor = _session.GtpEngineOrderEditor;
        if (GoScreenRenderer.GetCatalogOrderCancelButtonHit(point))
        {
            _session.CancelGtpEngineOrderEditor();
            return true;
        }

        if (GoScreenRenderer.GetCatalogOrderSaveButtonHit(point))
        {
            var profiles = _session.CommitGtpEngineOrderEditor();
            _gtpEngineCatalog.Save(profiles);
            RefreshCurrentGtpEngineAppCompatibilities();
            return true;
        }

        var moveStep = GoScreenRenderer.GetCatalogOrderMoveStep(point, editor.PageSize);
        if (moveStep == int.MinValue)
            editor.MoveSelectedToTop();
        else if (moveStep != 0)
            editor.MoveSelected(moveStep);
        else if (GoScreenRenderer.GetCatalogOrderPagePairStep(point) is var pageStep && pageStep != 0)
            editor.MovePagePair(pageStep);
        else if (GoScreenRenderer.GetCatalogOrderCardHit(point, editor) is { } index)
            editor.BeginDrag(index);

        return true;
    }

    private bool TryHandleGtpEngineDeleteConfirmationClick(Point point)
    {
        if (GoScreenRenderer.GetGtpEngineDeleteConfirmationCancelButtonHit(point))
        {
            _session.CloseGtpEngineDeleteConfirmation();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineDeleteConfirmationConfirmButtonHit(point))
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
                if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogPagerStep(point) is { } comboPageStep)
                    _session.MoveGtpEngineRandomMoveSelectionPage(comboPageStep);
                else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(point))
                    _session.CancelGtpEngineRandomMoveSelectionDialog();
                else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(point))
                    _session.CommitGtpEngineRandomMoveSelectionDialog();
                else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogItemHit(point, _session) is { } itemIndex)
                    _session.SelectGtpEngineRandomMoveItem(itemIndex);

                return true;
            }

            if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogPagerStep(point) is { } optionPageStep)
            {
                _session.MoveGtpEngineGuiOptionsPage(optionPageStep);
                return true;
            }

            if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogCancelButtonHit(point))
            {
                _session.CancelGtpEngineGuiOptionsDialog();
                return true;
            }

            if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogOkButtonHit(point))
            {
                _session.CommitGtpEngineGuiOptionsDialog();
                return true;
            }

            if (GoScreenRenderer.GetGtpEngineGuiOptionControlHit(point, _session) is { } optionHit)
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

        if (GoScreenRenderer.GetGtpEngineEditPanelCloseButtonHit(point))
        {
            EndGtpEngineEditField();
            _gtpEngineEditTextBox.Clear();
            RefreshCurrentGtpEngineAppCompatibilities();
            _session.CloseGtpEngineEditPanel();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelFileBrowseButtonHit(point))
        {
            BrowseGtpEngineExecutablePath();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelWorkingDirectoryBrowseButtonHit(point))
        {
            BrowseGtpEngineWorkingDirectory();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelLogButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.ToggleGtpEngineEditLog();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelInitialPositionProfileButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.CycleGtpEngineInitialPositionProfile();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelInitialPositionMethodButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.CycleGtpEngineInitialPositionPreferredMethod();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelGuiOptionsButtonHit(point))
        {
            EndGtpEngineEditField();
            _session.OpenGtpEngineGuiOptionsDialog();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelSaveButtonHit(point))
        {
            SaveGtpEngineEditDraft();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineEditPanelFieldHit(point) is { } field)
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
            SaveGtpEngineEditDraft();
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
        var caretIndex = _renderer?.GetGtpEngineEditPanelCaretIndex(point, field, text) ?? text.Length;

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
            if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogPagerStep(point) is { } comboPageStep)
                _session.MoveGtpEngineRandomMoveSelectionPage(comboPageStep);
            else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogCancelButtonHit(point))
                _session.CancelGtpEngineRandomMoveSelectionDialog();
            else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogSelectButtonHit(point))
                _session.CommitGtpEngineRandomMoveSelectionDialog();
            else if (GoScreenRenderer.GetGtpEngineRandomMoveSelectionDialogItemHit(point, _session) is { } itemIndex)
                _session.SelectGtpEngineRandomMoveItem(itemIndex);
            return;
        }

        if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogPagerStep(point) is { } pageStep)
        {
            _session.MoveGtpEngineGuiOptionsPage(pageStep);
            return;
        }

        if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogCancelButtonHit(point))
        {
            _session.CancelAppProviderGameSettingsDialog();
            GuiOperationLog.User("Cancelled App Provider game settings", "app=ponnuki; role=provider");
            return;
        }

        if (GoScreenRenderer.GetGtpEngineGuiOptionsDialogOkButtonHit(point))
        {
            var profiles = _session.CommitAppProviderGameSettingsDialog();
            _gtpEngineCatalog.Save(profiles);
            GuiOperationLog.User("Saved App Provider game settings", "app=ponnuki; role=provider");
            return;
        }

        if (GoScreenRenderer.GetGtpEngineGuiOptionControlHit(point, _session) is not { } optionHit)
            return;

        var option = _session.ActiveGtpEngineGuiOptionSpecs[optionHit.Index];
        if (optionHit.Action == 3)
        {
            _session.SetGtpEngineGuiOptionDraft(option, option.DefaultValue);
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
    }

    private void OpenAppProviderGameSettings()
    {
        try
        {
            var specs = PonnukiPositionProvider.GetGameSettingSpecsAsync(_session.SelectedAppProviderEngine)
                .GetAwaiter().GetResult();
            _session.OpenAppProviderGameSettingsDialog(specs);
            GuiOperationLog.User("Opened App Provider game settings", $"app=ponnuki; role=provider; options={specs.Count}");
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("APP PROVIDER SETTINGS", "Could not load the Ponnuki Provider option schema.", ex);
            ShowMessage(ex.Message, "Ponnuki game settings");
        }
    }

    private void MoveGtpEngineEditFocus(int step)
    {
        var fields = Enum.GetValues<GtpEngineProfileEditField>();
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

        _session.SetGtpEngineExecutablePathDraft(fileName);
    }

    private void EditGtpEngineStringOption(GtpEngineGuiOptionSpec option)
    {
        var value = _textInputDialogService.PromptText(new TextInputDialogOptions
        {
            InitialValue = _session.GetGtpEngineGuiOptionDraft(option),
            MaximumLength = GtpEngineGuiOptions.MaximumTextLength,
            Title = option.Label,
        });
        if (value is not null)
            _session.SetGtpEngineGuiOptionDraft(option, value);
    }

    private void EditGtpEngineSpinOption(GtpEngineGuiOptionSpec option)
    {
        _activeGtpEngineIntegerOption = option;
        _gtpEngineIntegerOptionTextBox.Begin(_session.GetGtpEngineGuiOptionDraft(option));
        _previousGtpEngineIntegerKeyboard = Keyboard.GetState();
        _gtpEngineIntegerInputMessage = $"RANGE  {option.Min ?? int.MinValue} .. {option.Max ?? int.MaxValue}";
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
        _session.SetGtpEngineGuiOptionDraft(option, value.ToString());
        CancelGtpEngineIntegerInput();
    }

    private void CancelGtpEngineIntegerInput()
    {
        _activeGtpEngineIntegerOption = null;
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

    private void SaveGtpEngineEditDraft()
    {
        EndGtpEngineEditField();
        if (!ValidateGtpEngineEditDraft(out var profile, out var warning))
        {
            _session.SetGtpEngineEditWarning(warning);
            return;
        }

        _session.SaveGtpEngineEditDraft(profile);
        _gtpEngineCatalog.Save(_session.GtpEngineProfiles);
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
        if (GoScreenRenderer.GetSettingsBackButtonHit(point))
        {
            GuiOperationLog.User("Pressed settings Back button");
            _isApplicationSettingsOpen = false;
            return;
        }

        if (GoScreenRenderer.GetSettingsTabHit(point) is { } page)
        {
            _applicationSettingsPage = page;
            _applicationSettingsMessage = "";
            GuiOperationLog.User("Changed settings tab", page.ToString());
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && GoScreenRenderer.GetSettingsBrowseButtonHit(point))
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

        if (_applicationSettingsPage == ApplicationSettingsPage.OtherFolders && GoScreenRenderer.GetSettingsSgfBrowseButtonHit(point))
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

        if (_applicationSettingsPage == ApplicationSettingsPage.OtherFolders && GoScreenRenderer.GetSettingsScreenshotBrowseButtonHit(point))
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

        if (_applicationSettingsPage == ApplicationSettingsPage.Other && GoScreenRenderer.GetSettingsOpenApplicationSettingsFolderButtonHit(point))
        {
            OpenSettingsFolder(ApplicationSettings.FilePath, "application settings");
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Other && GoScreenRenderer.GetSettingsOpenEngineSettingsFolderButtonHit(point))
        {
            OpenSettingsFolder(_gtpEngineCatalog.ListPath, "engine settings");
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && GoScreenRenderer.GetSettingsLogItemHit(point, _guiLogFiles.Count) is { } index)
        {
            _selectedGuiLogIndex = index;
            _applicationSettingsMessage = Path.GetFileName(_guiLogFiles[index]);
            GuiOperationLog.User("Selected GUI log", _applicationSettingsMessage);
            return;
        }

        if (_applicationSettingsPage == ApplicationSettingsPage.Log && GoScreenRenderer.GetSettingsEditButtonHit(point, _selectedGuiLogIndex >= 0))
        {
            var path = _guiLogFiles[_selectedGuiLogIndex];
            GuiOperationLog.User("Pressed Edit in Code button", Path.GetFileName(path));
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

        GuiOperationLog.App("Screen changed", $"from={_lastScreenState} to={current}");
        _lastScreenState = current;
    }

    private string GetCurrentScreenState() =>
        _isApplicationSettingsOpen
            ? "Application settings"
            : _session.UseKind is null
                ? $"Title/{_titleMenuPage}"
                : _session.UseKind == GoAppUseKind.LocalGame
                    ? _playingScene.IsInitialPositionConciergeVisible
                        ? "Local/Playing/InitialPositionConcierge"
                        : $"Local/{_session.CurrentMode.Kind}"
                    : $"CGOS/{_session.CgosConnectionFlowKind}/{_session.CurrentMode.Kind}";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.TextInput -= OnTextInput;
            Deactivated -= OnGameDeactivated;
            _cgosBlackConnectionProcess.Dispose();
            _cgosWhiteConnectionProcess.Dispose();
            _cgosAdminProcess.Dispose();
            _playingScene.Dispose();
            _upcomingMatchChimeInstance?.Dispose();
            _upcomingMatchChime?.Dispose();
            _screenshotShutterSoundInstance?.Dispose();
            _screenshotShutterSound?.Dispose();
            _placeStoneSoundInstance?.Dispose();
            _placeStoneSound?.Dispose();
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
