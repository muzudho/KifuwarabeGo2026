namespace KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.TableRowLabel;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>［EDIT ENTRY PROFILE］画面の構成、操作判定、描画を担当します。</summary>
public sealed class EditEntryProfile
{
    private const int FieldLabelX = 536;
    private const int FieldIconX = 742;
    private const int FieldValueX = 785;
    private const int FieldValueWidth = 575;

    private static readonly Rectangle ClientIdentityTextBounds = new(FieldValueX, 439, FieldValueWidth, 42);
    private static readonly Rectangle EngineTextBounds = new(FieldValueX, 503, FieldValueWidth, 42);

    private readonly TableRowLabel _playerNameLabel = new("PLAYER NAME", new Rectangle(FieldLabelX, 382, 180, 32), new Color(180, 195, 195));
    private readonly TableRowLabel _handleLabel = new("HANDLE", new Rectangle(FieldLabelX, 446, 180, 32), new Color(180, 195, 195));
    private readonly TableRowLabel _engineLabel = new("ENGINE", new Rectangle(FieldLabelX, 510, 180, 32), new Color(180, 195, 195));
    private readonly SinglelineTextUnderline _playerNameTextUnderline = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly LinkUnderline _popupFieldUnderline = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private readonly StickyNote _playerNameStickyNote = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 421), new Color(185, 196, 255), new Color(116, 145, 178), "PLAYER NAME とは？", ["画面に表示され、対局者に識別されるプレイヤーの呼び名です。"], 26);
    private readonly StickyNote _handleStickyNote = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 485), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局サーバーにログインするときのプレイヤー名です。", "接続する環境ごとにプロフィールへ設定できます。"], 26);
    private readonly StickyNote _engineStickyNote = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 549), new Color(185, 196, 255), new Color(116, 145, 178), "ENGINE とは？", ["コンピューター対局に使用する GTP エンジンです。"], 26);

    public Button DiscardButton { get; } = new(new Rectangle(1080, 288, 132, 42), "DISCARD", 0.30f);
    public Button SaveAndCloseButton { get; } = new(new Rectangle(1224, 288, 148, 42), "SAVE & CLOSE", 0.26f);

    public bool IsClientIdentityChangeHit(Point point) => ClientIdentityTextBounds.Contains(point);
    public bool IsEngineChangeHit(Point point) => EngineTextBounds.Contains(point);
    public EntryProfileEditField? GetFieldHit(Point point) =>
        PlayerFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point) ? EntryProfileEditField.DisplayName : null;

    public int GetCaretIndex(Point point, EntryProfileEditField field, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, PlayerFieldTextBounds(field), 0.42f);

    public void Draw(GoAppSession session, Point mousePoint, StickyNoteScreenId stickyNoteScreen, EditEntryProfileDrawingCallbacks draw)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(draw);
        if (!session.IsPlayerEditPanelOpen) return;

        var bounds = new Rectangle(510, 270, 900, 480);
        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 140));
        draw.FillRectangle(bounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(bounds, 2, new Color(116, 145, 146));
        draw.DrawText("EDIT ENTRY PROFILE", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DiscardButton.Draw(mousePoint, draw.DrawButton);
        SaveAndCloseButton.Draw(mousePoint, draw.DrawButton);
        DrawPlayerNameField(session, mousePoint, draw);
        DrawPopupField(_handleLabel, session.PlayerEditClientIdentityHandle, ClientIdentityTextBounds, mousePoint, FieldIcon.None, draw);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPopupField(_engineLabel, session.PlayerEditEngineDisplayName, EngineTextBounds, mousePoint, FieldIcon.Engine, draw);

        var stickyNote = FieldHoverBounds(PlayerFieldTextBounds(EntryProfileEditField.DisplayName)).Contains(mousePoint)
            ? _playerNameStickyNote
            : FieldHoverBounds(ClientIdentityTextBounds).Contains(mousePoint)
                ? _handleStickyNote
                : session.PlayerEditDraft.Kind == EntryProfileKind.Computer && FieldHoverBounds(EngineTextBounds).Contains(mousePoint)
                    ? _engineStickyNote : null;
        if (stickyNote?.TryPlace(stickyNoteScreen) == true)
            stickyNote.Draw(new StickyNoteDrawingCallbacks(draw.DrawLine, draw.FillRectangle, draw.DrawRectangle, draw.DrawDynamicText));
    }

    private void DrawPlayerNameField(GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var field = EntryProfileEditField.DisplayName;
        var textBounds = PlayerFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        _playerNameLabel.Draw(draw.DrawText);
        _playerNameTextUnderline.Draw(textBounds, active, textBounds.Contains(mousePoint), new UnderlineDrawingSurface(draw));
        var text = session.GetPlayerEditFieldText(field);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);
        DrawIcon(FieldIcon.PlayerName, IconBounds(textBounds), draw);
        draw.DrawEditHint(active, textBounds.Contains(mousePoint), textBounds);
    }

    private void DrawPopupField(TableRowLabel label, string value, Rectangle textBounds, Point mousePoint, FieldIcon icon, EditEntryProfileDrawingCallbacks draw)
    {
        var hovered = textBounds.Contains(mousePoint);
        label.Draw(draw.DrawText);
        draw.DrawFittedText(value, textBounds, Color.White, 0.42f);
        _popupFieldUnderline.Draw(textBounds, hovered, new UnderlineDrawingSurface(draw));
        if (icon != FieldIcon.None) DrawIcon(icon, IconBounds(textBounds), draw);
        if (hovered) draw.DrawChangeHint(textBounds);
    }

    private static Rectangle PlayerFieldTextBounds(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => new(FieldValueX, 375, FieldValueWidth, 42),
        EntryProfileEditField.Identifier => new(FieldValueX, 439, FieldValueWidth, 42),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    private static Rectangle IconBounds(Rectangle textBounds) => new(FieldIconX, textBounds.Y + 4, 34, 34);
    private static Rectangle FieldHoverBounds(Rectangle textBounds) => new(FieldLabelX, textBounds.Y, textBounds.Right - FieldLabelX, textBounds.Height);

    private static void DrawIcon(FieldIcon icon, Rectangle bounds, EditEntryProfileDrawingCallbacks draw)
    {
        if (icon == FieldIcon.PlayerName)
        {
            draw.DrawStone(new Vector2(bounds.Center.X - 7, bounds.Center.Y), 7, true);
            draw.DrawStone(new Vector2(bounds.Center.X + 7, bounds.Center.Y), 7, false);
            return;
        }
        if (icon == FieldIcon.Engine) draw.DrawPlayerRoleFace(new Vector2(bounds.Center.X, bounds.Center.Y), true);
    }

    private enum FieldIcon { None, PlayerName, Engine }

    private sealed class UnderlineDrawingSurface(EditEntryProfileDrawingCallbacks draw) : IUnderlineDrawingSurface
    {
        public void FillRectangle(Rectangle bounds, Color color) => draw.FillRectangle(bounds, color);
        public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => draw.FillRoundedRectangle(bounds, radius, color);
        public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => draw.DrawLine(start, end, thickness, color);
    }
}

/// <summary>画面コンポーネントに渡す描画機能です。</summary>
public sealed record EditEntryProfileDrawingCallbacks(
    int VirtualScreenWidth,
    int VirtualScreenHeight,
    Action<Rectangle, Color> FillRectangle,
    Action<Rectangle, int, Color> FillRoundedRectangle,
    Action<Rectangle, int, Color> DrawRectangle,
    Action<string, Vector2, Color, float> DrawText,
    Action<string, Rectangle, Color, float> DrawFittedText,
    Action<Rectangle, string, bool, Point, bool, float> DrawButton,
    Action<Vector2, float, bool> DrawStone,
    Action<Vector2, bool> DrawPlayerRoleFace,
    Action<string, int, int, Rectangle, float> DrawTextSelection,
    Action<string, int, Rectangle, float> DrawTextCaret,
    Action<bool, bool, Rectangle> DrawEditHint,
    Action<Rectangle> DrawChangeHint,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<string, Rectangle, Color, float> DrawDynamicText);
