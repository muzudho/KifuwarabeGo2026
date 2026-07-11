namespace KifuwarabeGo2026.Application;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

public sealed class TextBoxController
{
    private const double CaretKeyRepeatInitialDelaySeconds = 0.42d;
    private const double CaretKeyRepeatIntervalSeconds = 0.055d;

    private readonly int _maxLength;
    private double _leftKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _rightKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _backKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _deleteKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;

    public TextBoxController(int maxLength)
    {
        _maxLength = maxLength;
    }

    public string Text { get; private set; } = "";

    public int CaretIndex { get; private set; }

    public bool IsCaretNavigationKeyHeld { get; private set; }

    public void Begin(string text)
    {
        Text = text;
        CaretIndex = Text.Length;
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
    }

    public void Clear()
    {
        Text = "";
        CaretIndex = 0;
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
    }

    public bool TryInputCharacter(char character)
    {
        if (char.IsControl(character))
        {
            return true;
        }

        if (Text.Length >= _maxLength)
        {
            return false;
        }

        Text = Text.Insert(CaretIndex, character.ToString());
        CaretIndex++;
        return true;
    }

    public TextBoxKeyboardAction HandleKeyboard(KeyboardState keyboard, KeyboardState previousKeyboard, GameTime gameTime)
    {
        IsCaretNavigationKeyHeld = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right);

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Enter))
        {
            return TextBoxKeyboardAction.Commit;
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Escape))
        {
            return TextBoxKeyboardAction.Cancel;
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Left, ref _leftKeyRepeatCountdown, gameTime) && CaretIndex > 0)
        {
            CaretIndex--;
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Right, ref _rightKeyRepeatCountdown, gameTime) && CaretIndex < Text.Length)
        {
            CaretIndex++;
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Home))
        {
            CaretIndex = 0;
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.End))
        {
            CaretIndex = Text.Length;
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Back, ref _backKeyRepeatCountdown, gameTime) && CaretIndex > 0)
        {
            Text = Text.Remove(CaretIndex - 1, 1);
            CaretIndex--;
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Delete, ref _deleteKeyRepeatCountdown, gameTime) && CaretIndex < Text.Length)
        {
            Text = Text.Remove(CaretIndex, 1);
        }

        return TextBoxKeyboardAction.None;
    }

    private static bool IsNewKeyPress(KeyboardState keyboard, KeyboardState previousKeyboard, Keys key) =>
        keyboard.IsKeyDown(key) && previousKeyboard.IsKeyUp(key);

    private static bool ShouldHandleRepeatedKey(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        Keys key,
        ref double repeatCountdown,
        GameTime gameTime)
    {
        if (keyboard.IsKeyUp(key))
        {
            repeatCountdown = CaretKeyRepeatInitialDelaySeconds;
            return false;
        }

        if (previousKeyboard.IsKeyUp(key))
        {
            repeatCountdown = CaretKeyRepeatInitialDelaySeconds;
            return true;
        }

        repeatCountdown -= gameTime.ElapsedGameTime.TotalSeconds;
        if (repeatCountdown > 0d)
        {
            return false;
        }

        repeatCountdown += CaretKeyRepeatIntervalSeconds;
        if (repeatCountdown <= 0d)
        {
            repeatCountdown = CaretKeyRepeatIntervalSeconds;
        }

        return true;
    }

    private void ResetCaretKeyRepeat()
    {
        _leftKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _rightKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _backKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
        _deleteKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    }
}

public enum TextBoxKeyboardAction
{
    None,
    Commit,
    Cancel,
}
