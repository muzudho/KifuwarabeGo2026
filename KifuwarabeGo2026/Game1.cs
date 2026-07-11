namespace KifuwarabeGo2026;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Application.Game;
using KifuwarabeGo2026.Application.TournamentRulesSetting;
using KifuwarabeGo2026.Domain;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly GoAppSession _session = new();
    private readonly TournamentRulesCatalog _tournamentRulesCatalog;
    private readonly GtpEngineCatalog _gtpEngineCatalog;
    private readonly TournamentRulesSetting _tournamentRulesSetting;
    private readonly PlayingScene _playingScene;
    private GoScreenRenderer? _renderer;
    private SoundEffect? _placeStoneSound;
    private SoundEffectInstance? _placeStoneSoundInstance;
    private MouseState _previousMouse;
    private KeyboardState _previousKeyboard;
    private KeyboardState _previousGtpEngineKeyboard;
    private readonly TextBoxController _gtpEngineEditTextBox = new(520);

    public Game1()
    {
        _tournamentRulesCatalog = TournamentRulesCatalog.LoadFromDefaultLocation();
        _gtpEngineCatalog = GtpEngineCatalog.LoadFromDefaultLocation();
        _session.SetTournamentRules(_tournamentRulesCatalog.Rules);
        _session.SetGtpEngineProfiles(_gtpEngineCatalog.Profiles);
        _tournamentRulesSetting = new TournamentRulesSetting(_session, _tournamentRulesCatalog, OpenTournamentRulesSelectionDialog, BrowseTournamentRulesFilePath);
        _playingScene = new PlayingScene(_session, PlayPlaceStoneSound);

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

        _playingScene.Update();
        _session.AddCurrentTurnElapsedTime(gameTime.ElapsedGameTime);
        UpdateGlobalKeyboardInput(keyboard);

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
        if (CanHandleGlobalRenParseToggle() && keyboard.IsKeyDown(Keys.R) && _previousKeyboard.IsKeyUp(Keys.R))
        {
            _session.ToggleRenParseDisplay();
        }

        _previousKeyboard = keyboard;
    }

    private bool CanHandleGlobalRenParseToggle() =>
        _session.ActiveGtpEngineEditField is null &&
        !_session.IsTournamentRulesDisplayNameEditing;

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(11, 13, 18));
        _renderer?.Draw(_session, Mouse.GetState().Position);

        base.Draw(gameTime);
    }

    private void UpdateMouseInput()
    {
        var mouse = Mouse.GetState();
        if (_previousMouse.LeftButton == ButtonState.Released && mouse.LeftButton == ButtonState.Pressed)
        {
            var point = VirtualScreen.ToVirtualPoint(GraphicsDevice.Viewport, mouse.Position);
            var isSetupMode = _session.CurrentMode.Kind != GoAppModeKind.Playing && _session.CurrentMode.Kind != GoAppModeKind.GameOver;
            var handledByGtpEngineEditPanel = isSetupMode && TryHandleGtpEngineEditPanelClick(point);
            var handledByGtpEngineSelectionDialog = !handledByGtpEngineEditPanel && isSetupMode && TryHandleGtpEngineSelectionDialogClick(point);
            Func<Point, string, int>? getDisplayNameCaretIndex = _renderer is null
                ? null
                : _renderer.GetTournamentRulesAddPanelDisplayNameCaretIndex;
            var handledByTournamentRulesSetting = !handledByGtpEngineEditPanel &&
                !handledByGtpEngineSelectionDialog &&
                isSetupMode &&
                _tournamentRulesSetting.TryHandleMouseClick(point, getDisplayNameCaretIndex);
            if (handledByGtpEngineEditPanel || handledByGtpEngineSelectionDialog || handledByTournamentRulesSetting)
            {
                _previousMouse = mouse;
                return;
            }

            if (_session.CurrentMode.Kind == GoAppModeKind.GameOver && GoScreenRenderer.GetReturnToSetupButtonHit(point))
            {
                _session.ReturnToSetup();
            }
            else if (isSetupMode && GoScreenRenderer.GetStartPlayingButtonHit(point, _session.CurrentMode.Kind))
            {
                _playingScene.StartPlaying();
            }
            else if (isSetupMode && GoScreenRenderer.GetBlackPlayerKindButtonHit(point) is { } blackPlayerKind)
            {
                _session.SetPlayerKind(GoStone.Black, blackPlayerKind);
            }
            else if (isSetupMode && GoScreenRenderer.GetBlackGtpEngineBrowseButtonHit(point))
            {
                OpenGtpEngineSelectionDialog(GoStone.Black);
            }
            else if (isSetupMode && GoScreenRenderer.GetWhitePlayerKindButtonHit(point) is { } whitePlayerKind)
            {
                _session.SetPlayerKind(GoStone.White, whitePlayerKind);
            }
            else if (isSetupMode && GoScreenRenderer.GetWhiteGtpEngineBrowseButtonHit(point))
            {
                OpenGtpEngineSelectionDialog(GoStone.White);
            }
            else
            {
                _playingScene.TryHandleMouseClick(point);
            }
        }

        _previousMouse = mouse;
    }

    private void OpenTournamentRulesSelectionDialog()
    {
        _session.OpenTournamentRulesSelectionDialog();
    }

    private void OnTextInput(object? sender, TextInputEventArgs e)
    {
        if (TryInputGtpEngineEditCharacter(e.Character))
        {
            return;
        }

        _tournamentRulesSetting.TryInputCharacter(e.Character);
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

        if (GoScreenRenderer.GetGtpEngineSelectionDialogCloseButtonHit(point))
        {
            _session.CloseGtpEngineSelectionDialog();
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
            _session.SelectGtpEngine(_session.GtpEngineSelectionTargetStone, index);
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

        _session.SetGtpEngineWorkingDirectoryDraft(dialog.SelectedPath);
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
        profile.WorkingDirectory = profile.WorkingDirectory.Trim();
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

        if (string.IsNullOrWhiteSpace(profile.WorkingDirectory))
        {
            profile.WorkingDirectory = Path.GetDirectoryName(profile.ExecutablePath) ?? "";
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
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath))
        {
            var directory = Path.GetDirectoryName(profile.ExecutablePath);
            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                return directory;
            }
        }

        if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory) && Directory.Exists(profile.WorkingDirectory))
        {
            return profile.WorkingDirectory;
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
