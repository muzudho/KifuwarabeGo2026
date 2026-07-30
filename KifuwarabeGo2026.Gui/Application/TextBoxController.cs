namespace KifuwarabeGo2026.Gui.Application;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

public sealed class TextBoxController
{
    private const double CaretKeyRepeatInitialDelaySeconds = 0.42d;
    private const double CaretKeyRepeatIntervalSeconds = 0.055d;
    private const int MaximumHistoryCount = 100;

    private readonly int _maxLength;
    private double _leftKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _rightKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _backKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private double _deleteKeyRepeatCountdown = CaretKeyRepeatInitialDelaySeconds;
    private readonly Stack<TextEditSnapshot> _undoHistory = new();
    private readonly Stack<TextEditSnapshot> _redoHistory = new();

    public TextBoxController(int maxLength)
    {
        _maxLength = maxLength;
    }

    public string Text { get; private set; } = "";

    public int CaretIndex { get; private set; }

    public int SelectionStart => SelectionAnchor is { } anchor ? Math.Min(anchor, CaretIndex) : CaretIndex;

    public int SelectionLength => SelectionAnchor is { } anchor ? Math.Abs(anchor - CaretIndex) : 0;

    public bool HasSelection => SelectionLength > 0;

    private int? SelectionAnchor { get; set; }

    public bool IsMouseSelecting { get; private set; }

    public bool IsCaretNavigationKeyHeld { get; private set; }

    public void Begin(string text)
    {
        Begin(text, text.Length);
    }

    public void Begin(string text, int caretIndex)
    {
        Text = text;
        SetCaretIndex(caretIndex);
        ClearSelection();
        ClearHistory();
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
    }

    public void SetCaretIndex(int caretIndex, bool extendSelection = false)
    {
        if (extendSelection && SelectionAnchor is null)
            SelectionAnchor = CaretIndex;
        else if (!extendSelection)
            SelectionAnchor = null;
        CaretIndex = Math.Clamp(caretIndex, 0, Text.Length);
    }

    public void BeginMouseSelection(int caretIndex, bool extendSelection)
    {
        SetCaretIndex(caretIndex, extendSelection);
        if (!extendSelection)
            SelectionAnchor = CaretIndex;
        IsMouseSelecting = true;
    }

    public void UpdateMouseSelection(int caretIndex)
    {
        if (IsMouseSelecting)
            SetCaretIndex(caretIndex, extendSelection: true);
    }

    public void EndMouseSelection() => IsMouseSelecting = false;

    public void Clear()
    {
        Text = "";
        CaretIndex = 0;
        ClearSelection();
        IsMouseSelecting = false;
        ClearHistory();
        IsCaretNavigationKeyHeld = false;
        ResetCaretKeyRepeat();
    }

    public bool TryInputCharacter(char character)
    {
        if (char.IsControl(character))
        {
            return true;
        }

        if (!HasSelection && Text.Length >= _maxLength)
        {
            return false;
        }

        PushUndoSnapshot();
        DeleteSelection();
        Text = Text.Insert(CaretIndex, character.ToString());
        CaretIndex++;
        return true;
    }

    public TextBoxKeyboardAction HandleKeyboard(
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        GameTime gameTime,
        IClipboardService clipboardService,
        bool allowClipboardExport = true,
        Func<char, bool>? pasteCharacterFilter = null)
    {
        IsCaretNavigationKeyHeld = keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.Right);
        var control = keyboard.IsKeyDown(Keys.LeftControl) || keyboard.IsKeyDown(Keys.RightControl);
        var shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.Z))
        {
            Undo();
            return TextBoxKeyboardAction.None;
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.Y))
        {
            Redo();
            return TextBoxKeyboardAction.None;
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.A))
        {
            SelectionAnchor = 0;
            CaretIndex = Text.Length;
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.C) &&
            allowClipboardExport && HasSelection)
        {
            clipboardService.TrySetText(Text.Substring(SelectionStart, SelectionLength));
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.X) &&
            allowClipboardExport && HasSelection)
        {
            if (clipboardService.TrySetText(Text.Substring(SelectionStart, SelectionLength)))
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
        }

        if (control && IsNewKeyPress(keyboard, previousKeyboard, Keys.V) &&
            clipboardService.TryGetText(out var clipboardText))
        {
            InsertText(clipboardText, pasteCharacterFilter);
        }

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
            SetCaretIndex(CaretIndex - 1, shift);
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Right, ref _rightKeyRepeatCountdown, gameTime) && CaretIndex < Text.Length)
        {
            SetCaretIndex(CaretIndex + 1, shift);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.Home))
        {
            SetCaretIndex(0, shift);
        }

        if (IsNewKeyPress(keyboard, previousKeyboard, Keys.End))
        {
            SetCaretIndex(Text.Length, shift);
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Back, ref _backKeyRepeatCountdown, gameTime))
        {
            if (HasSelection)
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
            else if (CaretIndex > 0)
            {
                PushUndoSnapshot();
                Text = Text.Remove(CaretIndex - 1, 1);
                CaretIndex--;
            }
        }

        if (ShouldHandleRepeatedKey(keyboard, previousKeyboard, Keys.Delete, ref _deleteKeyRepeatCountdown, gameTime))
        {
            if (HasSelection)
            {
                PushUndoSnapshot();
                DeleteSelection();
            }
            else if (CaretIndex < Text.Length)
            {
                PushUndoSnapshot();
                Text = Text.Remove(CaretIndex, 1);
            }
        }

        return TextBoxKeyboardAction.None;
    }

    private void InsertText(string value, Func<char, bool>? characterFilter)
    {
        value = value.Replace("\r", "").Replace("\n", "");
        if (characterFilter is not null)
            value = new string(value.Where(characterFilter).ToArray());
        var available = _maxLength - (Text.Length - SelectionLength);
        if (available <= 0 || value.Length == 0) return;
        var inserted = value[..Math.Min(available, value.Length)];
        PushUndoSnapshot();
        DeleteSelection();
        Text = Text.Insert(CaretIndex, inserted);
        CaretIndex += inserted.Length;
    }

    private bool DeleteSelection()
    {
        if (!HasSelection)
        {
            ClearSelection();
            return false;
        }
        var start = SelectionStart;
        Text = Text.Remove(start, SelectionLength);
        CaretIndex = start;
        ClearSelection();
        return true;
    }

    private void ClearSelection() => SelectionAnchor = null;

    private void PushUndoSnapshot()
    {
        _undoHistory.Push(CaptureSnapshot());
        while (_undoHistory.Count > MaximumHistoryCount)
        {
            var snapshots = _undoHistory.Take(MaximumHistoryCount).Reverse().ToArray();
            _undoHistory.Clear();
            foreach (var snapshot in snapshots)
                _undoHistory.Push(snapshot);
        }
        _redoHistory.Clear();
    }

    private void Undo()
    {
        if (_undoHistory.Count == 0) return;
        _redoHistory.Push(CaptureSnapshot());
        RestoreSnapshot(_undoHistory.Pop());
    }

    private void Redo()
    {
        if (_redoHistory.Count == 0) return;
        _undoHistory.Push(CaptureSnapshot());
        RestoreSnapshot(_redoHistory.Pop());
    }

    private TextEditSnapshot CaptureSnapshot() => new(Text, CaretIndex, SelectionAnchor);

    private void RestoreSnapshot(TextEditSnapshot snapshot)
    {
        Text = snapshot.Text;
        CaretIndex = snapshot.CaretIndex;
        SelectionAnchor = snapshot.SelectionAnchor;
        IsMouseSelecting = false;
    }

    private void ClearHistory()
    {
        _undoHistory.Clear();
        _redoHistory.Clear();
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

    private readonly record struct TextEditSnapshot(string Text, int CaretIndex, int? SelectionAnchor);
}

public enum TextBoxKeyboardAction
{
    None,
    Commit,
    Cancel,
}
