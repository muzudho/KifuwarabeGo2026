namespace KifuwarabeGo2026.Gui;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Connect;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Infrastructure.FileSystem;
using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.Cgos.Connect;
using KifuwarabeGo2026.Gui.Presentation.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Gui.Presentation.Cgos.Watching;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using KifuwarabeGo2026.Gui.Presentation.Title;
using KifuwarabeGo2026.Gui.Sgf;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GoAppSession _session = new();
    private readonly TournamentRulesCatalog _tournamentRulesCatalog;
    private readonly GtpEngineCatalog _gtpEngineCatalog;
    private readonly CgosConnectionCatalog _cgosConnectionCatalog;
    private readonly TournamentRulesSetting _tournamentRulesSetting;
    private readonly PlayingScene _playingScene;
    private readonly CgosConnectionProcess _cgosBlackConnectionProcess = new("BlackPlayer");
    private readonly CgosConnectionProcess _cgosWhiteConnectionProcess = new("WhitePlayer");
    private readonly CgosConnectionProcess _cgosAdminProcess = new("Admin");
    private readonly CgosGameObservation _cgosGameObservation = new();
    private GoScreenRenderer? _renderer;
    private SoundEffect? _placeStoneSound;
    private SoundEffectInstance? _placeStoneSoundInstance;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private KeyboardState _previousGtpEngineKeyboard;
    private readonly TextBoxController _gtpEngineEditTextBox = new(520);
    private readonly TextBoxController _humanPlayerNameTextBox = new(80);
    private KeyboardState _previousHumanPlayerNameKeyboard;
    private KeyboardState _previousCgosConnectionKeyboard;
    private readonly TextBoxController _cgosConnectionEditTextBox = new(240);
    private KeyboardState _previousCgosCredentialKeyboard;
    private readonly TextBoxController _cgosCredentialTextBox = new(240);

    public Game1()
    {
        _tournamentRulesCatalog = TournamentRulesCatalog.LoadFromDefaultLocation();
        _gtpEngineCatalog = GtpEngineCatalog.LoadFromDefaultLocation();
        _cgosConnectionCatalog = CgosConnectionCatalog.LoadFromDefaultLocation();
        _session.SetTournamentRules(_tournamentRulesCatalog.Rules);
        _session.SetGtpEngineProfiles(_gtpEngineCatalog.Profiles);
        _session.SetCgosConnectionProfiles(_cgosConnectionCatalog.Profiles);
        _tournamentRulesSetting = new TournamentRulesSetting(_session, _tournamentRulesCatalog, OpenTournamentRulesSelectionDialog, BrowseTournamentRulesFilePath);
        _playingScene = new PlayingScene(
            _session,
            PlayPlaceStoneSound,
            () => _gtpEngineCatalog.Save(_session.GtpEngineProfiles));

        _graphics = new GraphicsDeviceManager(this);
        _graphics.PreferredBackBufferWidth = VirtualScreen.Width;
        _graphics.PreferredBackBufferHeight = VirtualScreen.Height;
        _graphics.SynchronizeWithVerticalRetrace = true;

        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Kifuwarabe Go 2026";
        Window.AllowUserResizing = true;
        Window.TextInput += OnTextInput;
    }

    protected override void LoadContent()
    {
        _renderer = new GoScreenRenderer(GraphicsDevice, Content);
        _placeStoneSound = CreatePlaceStoneSound();
        _placeStoneSoundInstance = _placeStoneSound.CreateInstance();
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboard = Keyboard.GetState();
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            Exit();
        }

        if (_session.UseKind is not GoAppUseKind.LocalGame)
        {
            if (_session.UseKind == GoAppUseKind.CgosClient)
            {
                UpdateCgosConnectionProcessStatus();
                UpdateCgosAdminProcessStatus();
                UpdateCgosGameObservation();

                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                    UpdateGlobalKeyboardInput(keyboard);
                else
                    // ［CGOS　＞　観戦画面］キーボード入力
                    UpdateCgosWatchingKeyboardInput(keyboard);

                UpdateCgosConnectionEditPanelByKeyboard(keyboard, gameTime);
                UpdateCgosCredentialByKeyboard(keyboard, gameTime);
            }

            UpdateMouseInput();
            base.Update(gameTime);
            return;
        }

        _playingScene.Update();
        _session.AddCurrentTurnElapsedTime(gameTime.ElapsedGameTime);
        UpdateGlobalKeyboardInput(keyboard);
        UpdateHumanPlayerNameTextBox(keyboard, gameTime);

        if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            UpdateGtpEngineEditPanelByKeyboard(keyboard, gameTime);
            _tournamentRulesSetting.UpdateByKeyboard(keyboard, gameTime);
        }
        UpdateMouseInput();

        base.Update(gameTime);
    }

    private void UpdateGlobalKeyboardInput(KeyboardState keyboard)
    {
        if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing && TryHandleReviewKeyboardInput(keyboard))
        {
            _previousKeyboard = keyboard;
            return;
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
        var canToggle = _session.CgosConnectionFlowKind is CgosConnectionFlowKind.Watching or CgosConnectionFlowKind.Result;
        if (canToggle && keyboard.IsKeyDown(Keys.R) && _previousKeyboard.IsKeyUp(Keys.R))
            _session.ToggleRenParseDisplay();

        _previousKeyboard = keyboard;
    }

    private bool IsNewGlobalKeyPress(KeyboardState keyboard, Keys key) =>
        keyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);

    private bool TryHandleReviewKeyboardInput(KeyboardState keyboard)
    {
        if (IsNewGlobalKeyPress(keyboard, Keys.Left))
        {
            MoveReview(-1);
            return true;
        }

        if (IsNewGlobalKeyPress(keyboard, Keys.Right))
        {
            MoveReview(1);
            return true;
        }

        if (IsNewGlobalKeyPress(keyboard, Keys.Down))
        {
            MoveReview(-10);
            return true;
        }

        if (IsNewGlobalKeyPress(keyboard, Keys.Up))
        {
            MoveReview(10);
            return true;
        }

        if (IsNewGlobalKeyPress(keyboard, Keys.PageDown))
        {
            MoveReview(-50);
            return true;
        }

        if (IsNewGlobalKeyPress(keyboard, Keys.PageUp))
        {
            MoveReview(50);
            return true;
        }

        return false;
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
                TitleRenderer.Draw(_renderer, Mouse.GetState().Position);
            }
        }
        else if (_session.UseKind == GoAppUseKind.CgosClient)
        {
            if (_renderer is not null)
            {
                if (_session.CurrentMode.Kind == GoAppModeKind.Reviewing)
                {
                    LocalRestingRenderer.Draw(_renderer, _session, Mouse.GetState().Position);
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
                LocalRestingRenderer.Draw(_renderer, _session, Mouse.GetState().Position);
            }
        }

        base.Draw(gameTime);
    }

    private void UpdateMouseInput()
    {
        var mouse = Mouse.GetState();
        var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
        var engineErrorLogHovered = _session.UseKind == GoAppUseKind.LocalGame &&
            GoScreenRenderer.GetEngineErrorLogHit(point, _session);
        Mouse.SetCursor(engineErrorLogHovered ? MouseCursor.Hand : MouseCursor.Arrow);

        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            if (_session.UseKind is null)
            {
                if (TitleRenderer.IsLocalGameButtonHit(point))
                {
                    _session.SelectUseKind(GoAppUseKind.LocalGame);
                }
                else if (TitleRenderer.IsCgosClientButtonHit(point))
                {
                    _session.SelectUseKind(GoAppUseKind.CgosClient);
                }

                _previousMouse = mouse;
                return;
            }

            // ［CGOS　＞　観戦画面］マウス入力
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

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.Result)
                {
                    if (GoScreenRenderer.GetCgosWatchingReviewButtonHit(point))
                    {
                        StartReviewingGameRecord(_cgosGameObservation.CreateGameRecord(), "CGOS review");
                    }
                    else if (GoScreenRenderer.GetCgosWatchingExportSgfButtonHit(point))
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

                if (TryHandleCgosConnectionEditPanelClick(point))
                {
                    _previousMouse = mouse;
                    return;
                }

                if (_session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
                {
                    if (GoScreenRenderer.GetCgosCredentialFieldHit(point) is { } credential)
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
                        else if (GoScreenRenderer.GetCgosConnectionEngineSelectButtonHit(point, _session) is { } engineStone)
                        {
                            _session.OpenCgosGtpEngineSelectionDialog(engineStone);
                        }
                        else if (GoScreenRenderer.GetCgosAdminButtonHit(point, _session.CgosConnectionProfiles.Count > 0))
                        {
                            ToggleCgosAdminProcess();
                        }
                        else if (GoScreenRenderer.GetCgosAdminWhoButtonHit(point, _session.IsCgosAdminRunning))
                        {
                            SendCgosAdminCommand("who");
                        }
                        else if (GoScreenRenderer.GetCgosAdminWhitePlayerSelectButtonHit(point))
                        {
                            _session.OpenCgosAdminPlayerSelectionDialog(GoStone.White);
                        }
                        else if (GoScreenRenderer.GetCgosAdminBlackPlayerSelectButtonHit(point))
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
                        else if (GoScreenRenderer.GetCgosBlackConnectionButtonHit(point, _session.IsCgosBlackConnectionRunning || _session.SelectedCgosBlackGtpEngineProfile is not null))
                        {
                            ToggleCgosPlayerConnectionProcess(GoStone.Black);
                        }
                        else if (GoScreenRenderer.GetCgosWhiteConnectionButtonHit(point, _session.IsCgosWhiteConnectionRunning || _session.SelectedCgosWhiteGtpEngineProfile is not null))
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
                        else if (GoScreenRenderer.GetCgosPlayer2CodeButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory)))
                        {
                            OpenCgosPlayerConnectionLog(GoStone.White);
                        }
                        else if (GoScreenRenderer.GetCgosPlayer2TailButtonHit(point, !string.IsNullOrWhiteSpace(_session.CgosWhiteConnectionLogDirectory)))
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

            var isSetupMode = _session.CurrentMode.Kind == GoAppModeKind.Resting;
            var isBoardEditing = _session.CurrentMode.Kind == GoAppModeKind.BoardEditing;
            var humanPlayerNameHit = isSetupMode ? GoScreenRenderer.GetHumanPlayerNameTextBoxHit(point, _session) : null;
            if (_session.ActiveHumanPlayerNameStone is not null && humanPlayerNameHit is null)
                EndHumanPlayerNameEdit(commit: true);
            var handledByGtpEngineEditPanel = isSetupMode && !isBoardEditing && TryHandleGtpEngineEditPanelClick(point);
            var handledByGtpEngineSelectionDialog = !handledByGtpEngineEditPanel && isSetupMode && !isBoardEditing && TryHandleGtpEngineSelectionDialogClick(point);
            Func<Point, string, int>? getDisplayNameCaretIndex = _renderer is null
                ? null
                : (caretPoint, text) => TournamentRuleRenderer.GetDisplayNameCaretIndex(_renderer, caretPoint, text);
            var handledByTournamentRulesSetting = !handledByGtpEngineEditPanel &&
                !handledByGtpEngineSelectionDialog &&
                isSetupMode &&
                !isBoardEditing &&
                _tournamentRulesSetting.TryHandleMouseClick(point, getDisplayNameCaretIndex);
            if (handledByGtpEngineEditPanel || handledByGtpEngineSelectionDialog || handledByTournamentRulesSetting)
            {
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

            if (isSetupMode && GoScreenRenderer.GetSetupBackToTitleButtonHit(point))
            {
                _session.ReturnToUseSelection();
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver && GoScreenRenderer.GetReturnToSetupButtonHit(point))
            {
                _session.ReturnToSetup();
            }
            else if (_session.CurrentMode.Kind == GoAppModeKind.GameOver && GoScreenRenderer.GetExportSgfButtonHit(point))
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
                _session.StartBoardEditing();
            }
            else if (isSetupMode && GoScreenRenderer.GetStartPlayingButtonHit(point, _session.CurrentMode.Kind))
            {
                _playingScene.StartPlaying();
            }
            else if (isSetupMode && GoScreenRenderer.GetBlackPlayerKindButtonHit(point) is { } blackPlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.Black, blackPlayerKind);
            }
            else if (isSetupMode && _session.BlackPlayerKind == GoPlayerKind.Computer && GoScreenRenderer.GetBlackGtpEngineBrowseButtonHit(point))
            {
                OpenGtpEngineSelectionDialog(GoStone.Black);
            }
            else if (isSetupMode && GoScreenRenderer.GetWhitePlayerKindButtonHit(point) is { } whitePlayerKind)
            {
                EndHumanPlayerNameEdit(commit: true);
                _session.SetPlayerKind(GoStone.White, whitePlayerKind);
            }
            else if (isSetupMode && _session.WhitePlayerKind == GoPlayerKind.Computer && GoScreenRenderer.GetWhiteGtpEngineBrowseButtonHit(point))
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

    private void OpenEngineLog()
    {
        var logPath = ApplicationErrorLog.FilePath;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = logPath,
                UseShellExecute = true,
            });
        }
        catch
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "notepad",
                UseShellExecute = true,
            };
            startInfo.ArgumentList.Add(logPath);
            Process.Start(startInfo);
        }
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

        if (GoScreenRenderer.GetBoardEditingExportSgfButtonHit(point))
        {
            ExportSgf();
            return true;
        }

        if (GoScreenRenderer.GetBoardEditingDoneButtonHit(point))
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

        if (GoScreenRenderer.GetReviewStepButtonHit(point) is { } step)
        {
            MoveReview(step);
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
        if (!_session.IsCgosConnectionEditPanelOpen)
        {
            _previousCgosConnectionKeyboard = keyboard;
            return;
        }

        if (_session.ActiveCgosConnectionEditField is { } field)
        {
            switch (_cgosConnectionEditTextBox.HandleKeyboard(keyboard, _previousCgosConnectionKeyboard, gameTime))
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

        if (_cgosGameObservation.IsStarted && _cgosGameObservation.GameId != previousGameId)
        {
            _session.OpenCgosWatchingScreen();
        }

        if (!wasFinished && _cgosGameObservation.IsFinished)
        {
            _session.OpenCgosResultScreen();
        }
    }

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
            _cgosConnectionEditTextBox.SetCaretIndex(caretIndex);
            SyncCgosConnectionEditField(field);
            return;
        }

        _cgosConnectionEditTextBox.Begin(text, caretIndex);
        SyncCgosConnectionEditField(field);
        _session.BeginCgosConnectionEditField(field, _cgosConnectionEditTextBox.CaretIndex);
        UpdateCgosConnectionEditWarning();
    }

    private void SyncCgosConnectionEditField(CgosConnectionProfileEditField field)
    {
        _session.SetCgosConnectionEditField(field, _cgosConnectionEditTextBox.Text, _cgosConnectionEditTextBox.CaretIndex);
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
            Role = draft.Role.Trim(),
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
            _session.SetCgosCredential(stone, field, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
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
        else
            _cgosCredentialTextBox.SetCaretIndex(caret);
        _session.BeginCgosCredentialEdit(stone, field, _cgosCredentialTextBox.CaretIndex);
    }

    private void UpdateCgosCredentialByKeyboard(KeyboardState keyboard, GameTime gameTime)
    {
        if (_session.ActiveCgosCredentialStone is not { } stone ||
            _session.ActiveCgosCredentialField is not { } field)
        {
            _previousCgosCredentialKeyboard = keyboard;
            return;
        }

        switch (_cgosCredentialTextBox.HandleKeyboard(keyboard, _previousCgosCredentialKeyboard, gameTime))
        {
            case TextBoxKeyboardAction.Commit:
            case TextBoxKeyboardAction.Cancel:
                EndCgosCredentialEdit();
                break;
            default:
                _session.SetCgosCredential(stone, field, _cgosCredentialTextBox.Text, _cgosCredentialTextBox.CaretIndex);
                break;
        }
        _previousCgosCredentialKeyboard = keyboard;
    }

    private void BeginHumanPlayerNameEdit(Point point, GoStone stone)
    {
        var text = _session.ActiveHumanPlayerNameStone == stone
            ? _humanPlayerNameTextBox.Text
            : _session.GetHumanPlayerName(stone);
        var caretIndex = _renderer?.GetHumanPlayerNameCaretIndex(point, stone, text) ?? text.Length;
        if (_session.ActiveHumanPlayerNameStone == stone)
        {
            _humanPlayerNameTextBox.SetCaretIndex(caretIndex);
            _session.SetHumanPlayerNameDraft(text, caretIndex);
            return;
        }

        _humanPlayerNameTextBox.Begin(text, caretIndex);
        _session.BeginHumanPlayerNameEdit(stone, caretIndex);
    }

    private void UpdateHumanPlayerNameTextBox(KeyboardState keyboard, GameTime gameTime)
    {
        if (_session.ActiveHumanPlayerNameStone is null)
        {
            _previousHumanPlayerNameKeyboard = keyboard;
            return;
        }

        var action = _humanPlayerNameTextBox.HandleKeyboard(keyboard, _previousHumanPlayerNameKeyboard, gameTime);
        _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
        if (action == TextBoxKeyboardAction.Commit) EndHumanPlayerNameEdit(commit: true);
        if (action == TextBoxKeyboardAction.Cancel) EndHumanPlayerNameEdit(commit: false);
        _previousHumanPlayerNameKeyboard = keyboard;
    }

    private bool TryInputHumanPlayerNameCharacter(char character)
    {
        if (_session.ActiveHumanPlayerNameStone is null) return false;
        if (!_humanPlayerNameTextBox.TryInputCharacter(character)) return true;
        _session.SetHumanPlayerNameDraft(_humanPlayerNameTextBox.Text, _humanPlayerNameTextBox.CaretIndex);
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

    private string? BrowseTournamentRulesFilePath(TournamentRules rules)
    {
        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExt = "json",
            Filter = "Tournament rules (*.json)|*.json|All files (*.*)|*.*",
            FileName = string.IsNullOrWhiteSpace(rules.FilePath) ? "tournament-rules-custom.json" : Path.GetFileName(rules.FilePath),
            InitialDirectory = GetInitialTournamentRulesDirectory(rules),
            OverwritePrompt = true,
            Title = "Save tournament rules",
        };

        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.FileName : null;
    }

    private void ImportSgf()
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            CheckFileExists = true,
            DefaultExt = "sgf",
            Filter = "SGF files (*.sgf)|*.sgf|All files (*.*)|*.*",
            InitialDirectory = AppContext.BaseDirectory,
            Title = "Load SGF game record",
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        try
        {
            var record = SgfGameRecordConverter.FromSgf(File.ReadAllText(dialog.FileName, Encoding.UTF8));
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
    private void ExportSgf(GoGameRecord record, string fileName)
    {
        using var dialog = new System.Windows.Forms.SaveFileDialog
        {
            AddExtension = true,
            CheckPathExists = true,
            DefaultExt = "sgf",
            Filter = "SGF files (*.sgf)|*.sgf|All files (*.*)|*.*",
            FileName = fileName,
            InitialDirectory = AppContext.BaseDirectory,
            OverwritePrompt = true,
            Title = "Save SGF game record",
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        try
        {
            var sgf = SgfGameRecordConverter.ToSgf(record);
            File.WriteAllText(dialog.FileName, sgf, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowMessage(ex.Message, "SGF output");
        }
    }

    private static void ShowMessage(string message, string caption)
    {
        System.Windows.Forms.MessageBox.Show(
            message,
            caption,
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Warning);
    }

    private static string GetInitialTournamentRulesDirectory(TournamentRules rules)
    {
        if (!string.IsNullOrWhiteSpace(rules.FilePath))
        {
            var directory = Path.GetDirectoryName(rules.FilePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        return AppContext.BaseDirectory;
    }

    private void OpenGtpEngineSelectionDialog(GoStone stone)
    {
        _session.OpenGtpEngineSelectionDialog(stone);
    }

    private bool TryHandleGtpEngineSelectionDialogClick(Point point)
    {
        if (!_session.IsGtpEngineSelectionDialogOpen)
        {
            return false;
        }

        if (_session.IsGtpEngineDeleteConfirmationOpen)
        {
            return TryHandleGtpEngineDeleteConfirmationClick(point);
        }

        if (GoScreenRenderer.TryGetGtpEngineSelectionDialogPathCopyText(point, _session, out var path))
        {
            SystemClipboard.SetText(path);
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogCancelButtonHit(point))
        {
            _session.CancelGtpEngineSelectionDialog();
            return true;
        }

        if (GoScreenRenderer.GetGtpEngineSelectionDialogOkButtonHit(point))
        {
            _session.CommitGtpEngineSelectionDialog();
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
            _session.SelectGtpEngineDialogItem(index);
            return true;
        }

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
                        _session.OpenGtpEngineRandomMoveSelectionDialog();
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
        if (!_session.IsGtpEngineEditPanelOpen)
        {
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
            switch (_gtpEngineEditTextBox.HandleKeyboard(keyboard, _previousGtpEngineKeyboard, gameTime))
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
            _gtpEngineEditTextBox.SetCaretIndex(caretIndex);
            SyncGtpEngineEditField(field);
            return;
        }

        _gtpEngineEditTextBox.Begin(text, caretIndex);
        SyncGtpEngineEditField(field);
        _session.BeginGtpEngineEditField(field, _gtpEngineEditTextBox.CaretIndex);
        UpdateGtpEngineEditWarning();
    }

    private void SyncGtpEngineEditField(GtpEngineProfileEditField field)
    {
        _session.SetGtpEngineEditField(field, _gtpEngineEditTextBox.Text, _gtpEngineEditTextBox.CaretIndex);
    }

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
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            CheckFileExists = true,
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = Path.GetFileName(source.ExecutablePath),
            InitialDirectory = GetInitialGtpEngineDirectory(source),
            Title = "Select GTP engine executable",
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _session.SetGtpEngineExecutablePathDraft(dialog.FileName);
    }

    private void EditGtpEngineStringOption(GtpEngineGuiOptionSpec option)
    {
        var current = _session.GetGtpEngineGuiOptionDraft(option);
        using var dialog = new System.Windows.Forms.Form
        {
            AcceptButton = null,
            CancelButton = null,
            ClientSize = new System.Drawing.Size(620, 150),
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
            Text = option.Label,
        };
        using var textBox = new System.Windows.Forms.TextBox { Left = 20, Top = 20, Width = 580, Text = current, MaxLength = GtpEngineGuiOptions.MaximumTextLength };
        using var cancelButton = new System.Windows.Forms.Button { Left = 20, Top = 78, Width = 110, Height = 42, Text = "CANCEL", DialogResult = System.Windows.Forms.DialogResult.Cancel };
        using var okButton = new System.Windows.Forms.Button { Left = 150, Top = 78, Width = 110, Height = 42, Text = "OK", DialogResult = System.Windows.Forms.DialogResult.OK };
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.AddRange([textBox, cancelButton, okButton]);
        textBox.SelectAll();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            _session.SetGtpEngineGuiOptionDraft(option, textBox.Text);
    }

    private void EditGtpEngineSpinOption(GtpEngineGuiOptionSpec option)
    {
        _ = int.TryParse(_session.GetGtpEngineGuiOptionDraft(option), out var current);
        using var dialog = new System.Windows.Forms.Form
        {
            ClientSize = new System.Drawing.Size(620, 150),
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent,
            Text = option.Label,
        };
        using var numberBox = new System.Windows.Forms.NumericUpDown
        {
            Left = 20,
            Top = 20,
            Width = 580,
            DecimalPlaces = 0,
            Minimum = option.Min ?? int.MinValue,
            Maximum = option.Max ?? int.MaxValue,
            Value = Math.Clamp(current, option.Min ?? int.MinValue, option.Max ?? int.MaxValue),
            ThousandsSeparator = false,
        };
        using var cancelButton = new System.Windows.Forms.Button { Left = 20, Top = 78, Width = 110, Height = 42, Text = "CANCEL", DialogResult = System.Windows.Forms.DialogResult.Cancel };
        using var okButton = new System.Windows.Forms.Button { Left = 150, Top = 78, Width = 110, Height = 42, Text = "OK", DialogResult = System.Windows.Forms.DialogResult.OK };
        dialog.AcceptButton = okButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.AddRange([numberBox, cancelButton, okButton]);
        numberBox.Select(0, numberBox.Text.Length);
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            _session.SetGtpEngineGuiOptionDraft(option, decimal.ToInt32(numberBox.Value).ToString());
    }

    private void BrowseGtpEngineFilenameOption(GtpEngineGuiOptionSpec option)
    {
        using var dialog = new System.Windows.Forms.OpenFileDialog
        {
            CheckFileExists = false,
            FileName = Path.GetFileName(_session.GetGtpEngineGuiOptionDraft(option)),
            Filter = "All files (*.*)|*.*",
            Title = $"Select {option.Label}",
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            if (dialog.FileName.Length > GtpEngineGuiOptions.MaximumTextLength)
                ShowMessage($"The file path exceeds {GtpEngineGuiOptions.MaximumTextLength} characters.", "GTP engine option");
            else
                _session.SetGtpEngineGuiOptionDraft(option, dialog.FileName);
        }
    }

    private void BrowseGtpEngineWorkingDirectory()
    {
        EndGtpEngineEditField();
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select GTP engine working directory",
            SelectedPath = GetInitialGtpEngineDirectory(_session.GtpEngineEditDraft),
            ShowNewFolderButton = true,
        };

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        _session.SetGtpEngineWorkingDirectoryDraft(WorkingDirectoryModel.FromString(dialog.SelectedPath));
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Window.TextInput -= OnTextInput;
            _cgosBlackConnectionProcess.Dispose();
            _cgosWhiteConnectionProcess.Dispose();
            _cgosAdminProcess.Dispose();
            _playingScene.Dispose();
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
