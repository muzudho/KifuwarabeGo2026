namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.TableRowLabel;
using Microsoft.Xna.Framework;
using System;

/// <summary>EDIT ENTRY PROFILE モーダルのレイアウト、入力、描画を担当します。</summary>
public sealed partial class GoScreenRenderer
{
    #region ［CANCEL］

    private static readonly Rectangle PlayerEditPanelCancelButtonBounds = new(1080, 288, 132, 42);

    public static bool GetPlayerEditPanelCancelButtonHit(Point point) => PlayerEditPanelCancelButtonBounds.Contains(point);

    #endregion

    #region ［SAVE & CLOSE］

    private static readonly Rectangle PlayerEditPanelSaveButtonBounds = new(1224, 288, 148, 42);

    public static bool GetPlayerEditPanelSaveButtonHit(Point point) => PlayerEditPanelSaveButtonBounds.Contains(point);

    #endregion

    #region ［TABLE］

    private const int PlayerEditFieldLabelX = 536;
    private const int PlayerEditFieldIconX = 742;
    private const int PlayerEditFieldValueX = 785;
    private const int PlayerEditFieldValueWidth = 575;

    private static readonly Rectangle PlayerEditPanelClientIdentityTextBounds = new(PlayerEditFieldValueX, 439, PlayerEditFieldValueWidth, 42);
    private static readonly Rectangle PlayerEditPanelEngineTextBounds = new(PlayerEditFieldValueX, 503, PlayerEditFieldValueWidth, 42);

    #endregion

    #region ［TABLE　＞　PLAYER NAME］

    /// <summary>
    /// ［PLAYER NAME］のラベル列
    /// </summary>
    private readonly TableRowLabel _playerNameLabel = new(
        "PLAYER NAME", new Rectangle(PlayerEditFieldLabelX, 382, 180, 32), new Color(180, 195, 195));

    /// <summary>
    /// ［PLAYER NAME］の値列
    /// </summary>
    private readonly SinglelineTextUnderline _playerNameTextUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    /// <summary>
    /// ［PLAYER NAME］の付箋
    /// </summary>
    private readonly StickyNote _playerNameStickyNote = new(
        StickyNoteKind.EntryProfileFieldHint,
        new Vector2(PlayerEditFieldValueX + PlayerEditFieldValueWidth - 24, 421),
        new Color(185, 196, 255), new Color(116, 145, 178),
        "PLAYER NAME とは？",
        ["画面に表示され、棋譜に書き込まれる、プレイヤーの呼び名。"],
        bodyLineSpacing: 26);

    #endregion

    #region ［TABLE　＞　HANDLE］

    /// <summary>
    /// ［HANDLE］のラベル列
    /// </summary>
    private readonly TableRowLabel _handleLabel = new(
        "HANDLE", new Rectangle(PlayerEditFieldLabelX, 446, 180, 32), new Color(180, 195, 195));

    /// <summary>
    /// ［HANDLE］の付箋
    /// </summary>
    private readonly StickyNote _handleStickyNote = new(
        StickyNoteKind.EntryProfileFieldHint,
        new Vector2(PlayerEditFieldValueX + PlayerEditFieldValueWidth - 24, 485),
        new Color(185, 196, 255), new Color(116, 145, 178),
        "HANDLE とは？",
        ["対局サービスにログインするときの、プレイヤー固有の名前。", "接続する相手の機械に入力できるフォーマットに合わせます。"],
        bodyLineSpacing: 26);

    #endregion

    #region ［TABLE　＞　ENGINE］

    /// <summary>
    /// ［ENGINE］のラベル列
    /// </summary>
    private readonly TableRowLabel _engineLabel = new(
        "ENGINE", new Rectangle(PlayerEditFieldLabelX, 510, 180, 32), new Color(180, 195, 195));

    /// <summary>
    /// ［ENGINE］の付箋
    /// </summary>
    private readonly StickyNote _engineStickyNote = new(
        StickyNoteKind.EntryProfileFieldHint,
        new Vector2(PlayerEditFieldValueX + PlayerEditFieldValueWidth - 24, 549),
        new Color(185, 196, 255), new Color(116, 145, 178),
        "ENGINE とは？",
        ["コンピューターとして対局するための GTP エンジン。"],
        bodyLineSpacing: 26);

    #endregion

    /// <summary>
    /// XXX: 何これ？
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool GetPlayerEditPanelClientIdentityChangeHit(Point point) => PlayerEditPanelClientIdentityTextBounds.Contains(point);

    /// <summary>
    /// XXX: 何これ？
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public static bool GetPlayerEditPanelEngineChangeHit(Point point) => PlayerEditPanelEngineTextBounds.Contains(point);

    public static EntryProfileEditField? GetPlayerEditPanelFieldHit(Point point) =>
        PlayerEditPanelFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point)
            ? EntryProfileEditField.DisplayName
            : null;

    public int GetPlayerEditPanelCaretIndex(Point point, EntryProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, PlayerEditPanelFieldTextBounds(field), 0.42f);

    private void DrawPlayerEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsPlayerEditPanelOpen) return;
        var bounds = new Rectangle(510, 270, 900, 480);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 140));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT ENTRY PROFILE", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawCommandButton(PlayerEditPanelCancelButtonBounds, "DISCARD", false, mousePoint, scale: 0.30f);
        DrawCommandButton(PlayerEditPanelSaveButtonBounds, "SAVE & CLOSE", false, mousePoint, scale: 0.26f);
        DrawPlayerEditField(session, EntryProfileEditField.DisplayName, _playerNameLabel, mousePoint);
        DrawPlayerEditPopupField(_handleLabel, session.PlayerEditClientIdentityHandle, PlayerEditPanelClientIdentityTextBounds, mousePoint);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPlayerEditPopupField(_engineLabel, session.PlayerEditEngineDisplayName, PlayerEditPanelEngineTextBounds, mousePoint, PlayerEditFieldIconKind.Engine);
        StickyNote? stickyNote = PlayerEditFieldHoverBounds(PlayerEditPanelFieldTextBounds(EntryProfileEditField.DisplayName)).Contains(mousePoint)
            ? _playerNameStickyNote
            : PlayerEditFieldHoverBounds(PlayerEditPanelClientIdentityTextBounds).Contains(mousePoint)
                ? _handleStickyNote
                : session.PlayerEditDraft.Kind == EntryProfileKind.Computer && PlayerEditFieldHoverBounds(PlayerEditPanelEngineTextBounds).Contains(mousePoint)
                    ? _engineStickyNote
                    : null;
        if (stickyNote?.TryPlace(_stickyNoteScreen) == true)
            stickyNote.Draw(new StickyNoteDrawingCallbacks(DrawLine, FillRect, DrawRect, DrawDynamicOptionText));
    }

    private static Rectangle PlayerEditPanelFieldTextBounds(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => new(PlayerEditFieldValueX, 375, PlayerEditFieldValueWidth, 42),
        EntryProfileEditField.Identifier => new(PlayerEditFieldValueX, 439, PlayerEditFieldValueWidth, 42),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    private static Rectangle PlayerEditFieldIconBounds(Rectangle textBounds) => new(PlayerEditFieldIconX, textBounds.Y + 4, 34, 34);
    private static Rectangle PlayerEditFieldHoverBounds(Rectangle textBounds) => new(PlayerEditFieldLabelX, textBounds.Y, textBounds.Right - PlayerEditFieldLabelX, textBounds.Height);
    private static Vector2 PlayerEditUnderlineConnectorStart(Rectangle textBounds) => new(textBounds.Right - 24, textBounds.Bottom + 4);

    private void DrawPlayerEditField(GoAppSession session, EntryProfileEditField field, TableRowLabel label, Point mousePoint)
    {
        var textBounds = PlayerEditPanelFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        label.Draw(DrawText);
        var hovered = textBounds.Contains(mousePoint);
        _playerNameTextUnderline.Draw(textBounds, active, hovered, this);
        var text = session.GetPlayerEditFieldText(field);
        if (active) DrawTextBoxSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active) DrawTextBoxCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);
        if (field == EntryProfileEditField.DisplayName) DrawPlayerEditFieldIcon(PlayerEditFieldIconKind.PlayerName, PlayerEditFieldIconBounds(textBounds));
        DrawEditableTextEditHint(active, hovered, textBounds);
    }

    private void DrawPlayerEditPopupField(TableRowLabel label, string value, Rectangle textBounds, Point mousePoint, PlayerEditFieldIconKind iconKind = PlayerEditFieldIconKind.None)
    {
        var hovered = textBounds.Contains(mousePoint);
        label.Draw(DrawText);
        DrawFittedText(value, textBounds, Color.White, 0.42f);
        _wideLinkUnderline.Draw(textBounds, hovered, this);
        if (iconKind != PlayerEditFieldIconKind.None) DrawPlayerEditFieldIcon(iconKind, PlayerEditFieldIconBounds(textBounds));
        if (hovered) DrawPlayerEditHint("CHANGE", textBounds);
    }

    private void DrawPlayerEditFieldIcon(PlayerEditFieldIconKind kind, Rectangle bounds)
    {
        if (kind == PlayerEditFieldIconKind.PlayerName)
        {
            DrawIconStone(new Vector2(bounds.Center.X - 7, bounds.Center.Y), 7, black: true);
            DrawIconStone(new Vector2(bounds.Center.X + 7, bounds.Center.Y), 7, black: false);
            return;
        }
        DrawPlayerRoleFaceIcon(new Vector2(bounds.Center.X, bounds.Center.Y), isComputer: true);
    }

    private enum PlayerEditFieldIconKind { None, PlayerName, Engine }
}
