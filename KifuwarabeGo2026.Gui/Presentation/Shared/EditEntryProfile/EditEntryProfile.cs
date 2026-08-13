namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;

/// <summary>EDIT ENTRY PROFILE モーダルのレイアウト、入力、描画を担当します。</summary>
public sealed partial class GoScreenRenderer
{
    private readonly SinglelineTextUnderline _playerNameTextUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });
    private static readonly Rectangle PlayerEditPanelCancelButtonBounds = new(1080, 288, 132, 42);
    private static readonly Rectangle PlayerEditPanelSaveButtonBounds = new(1224, 288, 148, 42);

    private const int PlayerEditFieldLabelX = 536;
    private const int PlayerEditFieldIconX = 742;
    private const int PlayerEditFieldValueX = 785;
    private const int PlayerEditFieldValueWidth = 575;

    private static readonly Rectangle PlayerEditPanelClientIdentityTextBounds = new(PlayerEditFieldValueX, 439, PlayerEditFieldValueWidth, 42);
    private static readonly Rectangle PlayerEditPanelEngineTextBounds = new(PlayerEditFieldValueX, 503, PlayerEditFieldValueWidth, 42);

    public static bool GetPlayerEditPanelCancelButtonHit(Point point) => PlayerEditPanelCancelButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelSaveButtonHit(Point point) => PlayerEditPanelSaveButtonBounds.Contains(point);
    public static bool GetPlayerEditPanelClientIdentityChangeHit(Point point) => PlayerEditPanelClientIdentityTextBounds.Contains(point);
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
        DrawPlayerEditField(session, EntryProfileEditField.DisplayName, "PLAYER NAME", mousePoint);
        DrawPlayerEditPopupField("HANDLE", session.PlayerEditClientIdentityHandle, PlayerEditPanelClientIdentityTextBounds, mousePoint);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPlayerEditPopupField("ENGINE", session.PlayerEditEngineDisplayName, PlayerEditPanelEngineTextBounds, mousePoint, PlayerEditFieldIconKind.Engine);
        DrawPlayerEditStickyNote(session, mousePoint);
    }

    private static Rectangle PlayerEditPanelFieldTextBounds(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => new(PlayerEditFieldValueX, 375, PlayerEditFieldValueWidth, 42),
        EntryProfileEditField.Identifier => new(PlayerEditFieldValueX, 439, PlayerEditFieldValueWidth, 42),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown player edit field."),
    };

    private static Rectangle PlayerEditFieldIconBounds(Rectangle textBounds) => new(PlayerEditFieldIconX, textBounds.Y + 4, 34, 34);
    private static Rectangle PlayerEditFieldHoverBounds(Rectangle textBounds) => new(PlayerEditFieldLabelX, textBounds.Y, textBounds.Right - PlayerEditFieldLabelX, textBounds.Height);

    private void DrawPlayerEditField(GoAppSession session, EntryProfileEditField field, string label, Point mousePoint)
    {
        var textBounds = PlayerEditPanelFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        DrawText(label, new Vector2(PlayerEditFieldLabelX, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        var hovered = textBounds.Contains(mousePoint);
        _playerNameTextUnderline.Draw(textBounds, active, hovered, this);
        var text = session.GetPlayerEditFieldText(field);
        if (active) DrawTextBoxSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active) DrawTextBoxCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);
        if (field == EntryProfileEditField.DisplayName) DrawPlayerEditFieldIcon(PlayerEditFieldIconKind.PlayerName, PlayerEditFieldIconBounds(textBounds));
        DrawEditableTextEditHint(active, hovered, textBounds);
    }

    private void DrawPlayerEditPopupField(string label, string value, Rectangle textBounds, Point mousePoint, PlayerEditFieldIconKind iconKind = PlayerEditFieldIconKind.None)
    {
        var hovered = textBounds.Contains(mousePoint);
        DrawText(label, new Vector2(PlayerEditFieldLabelX, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
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

    private void DrawPlayerEditStickyNote(GoAppSession session, Point mousePoint)
    {
        var displayNameBounds = PlayerEditPanelFieldTextBounds(EntryProfileEditField.DisplayName);
        var handleBounds = PlayerEditPanelClientIdentityTextBounds;
        var engineBounds = PlayerEditPanelEngineTextBounds;
        string? heading = null;
        string[]? bodyLines = null;
        Vector2 connectorStart = default;
        if (PlayerEditFieldHoverBounds(displayNameBounds).Contains(mousePoint))
        {
            heading = "PLAYER NAME とは？";
            bodyLines = ["画面に表示され、棋譜に書き込まれる、プレイヤーの呼び名。"];
            connectorStart = PlayerEditUnderlineConnectorStart(displayNameBounds);
        }
        else if (PlayerEditFieldHoverBounds(handleBounds).Contains(mousePoint))
        {
            heading = "HANDLE とは？";
            bodyLines = ["対局サービスにログインするときの、プレイヤー固有の名前。", "接続する相手の機械に入力できるフォーマットに合わせます。"];
            connectorStart = PlayerEditUnderlineConnectorStart(handleBounds);
        }
        else if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer && PlayerEditFieldHoverBounds(engineBounds).Contains(mousePoint))
        {
            heading = "ENGINE とは？";
            bodyLines = ["コンピューターとして対局するための GTP エンジン。"];
            connectorStart = PlayerEditUnderlineConnectorStart(engineBounds);
        }
        if (heading is null || bodyLines is null) return;
        DrawStickyNote(StickyNoteKind.EntryProfileFieldHint, connectorStart, new Color(185, 196, 255), new Color(116, 145, 178), heading, bodyLines, bodyLineSpacing: 26);
    }

    private static Vector2 PlayerEditUnderlineConnectorStart(Rectangle textBounds) => new(textBounds.Right - 24, textBounds.Bottom + 4);

    private enum PlayerEditFieldIconKind { None, PlayerName, Engine }
}
