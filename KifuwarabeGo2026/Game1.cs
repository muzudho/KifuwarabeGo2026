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

        if (_session.CurrentMode.Kind != GoAppModeKind.Playing)
        {
            _tournamentRulesSetting.UpdateByKeyboard(keyboard, gameTime);
        }
        UpdateMouseInput();

        base.Update(gameTime);
    }

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
            var handledByGtpEngineSelectionDialog = isSetupMode && TryHandleGtpEngineSelectionDialogClick(point);
            Func<Point, string, int>? getDisplayNameCaretIndex = _renderer is null
                ? null
                : _renderer.GetTournamentRulesAddPanelDisplayNameCaretIndex;
            var handledByTournamentRulesSetting = !handledByGtpEngineSelectionDialog &&
                isSetupMode &&
                _tournamentRulesSetting.TryHandleMouseClick(point, getDisplayNameCaretIndex);
            if (handledByGtpEngineSelectionDialog || handledByTournamentRulesSetting)
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

        if (GoScreenRenderer.GetGtpEngineSelectionDialogEditButtonHit(point))
        {
            EditSelectedGtpEngine();
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

    private void EditSelectedGtpEngine()
    {
        var selectedIndex = _session.SelectedGtpEngineIndex;
        if (selectedIndex < 0 || selectedIndex >= _session.GtpEngineProfiles.Count)
        {
            return;
        }

        var source = _session.GtpEngineProfiles[selectedIndex];
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

        var profile = source.Clone();
        profile.ExecutablePath = dialog.FileName;
        profile.WorkingDirectory = Path.GetDirectoryName(dialog.FileName) ?? profile.WorkingDirectory;
        if (string.IsNullOrWhiteSpace(profile.DisplayName) || profile.DisplayName == "Unnamed GTP Engine")
        {
            profile.DisplayName = Path.GetFileNameWithoutExtension(dialog.FileName);
        }

        _session.ReplaceSelectedGtpEngine(profile);
        _gtpEngineCatalog.Save(_session.GtpEngineProfiles);
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
