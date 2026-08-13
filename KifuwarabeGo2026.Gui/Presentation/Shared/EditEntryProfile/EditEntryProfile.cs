namespace KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.TableRowLabel;
using Microsoft.Xna.Framework;
using System;

/// <summary>［EDIT ENTRY PROFILE］画面の構成、操作判定、描画を担当します。</summary>
public sealed class EditEntryProfile
{
    #region レイアウト

    /// <summary>表のラベル、アイコン、値をそろえるための基準位置です。</summary>
    private const int FieldLabelX = 536;
    private const int FieldIconX = 742;
    private const int FieldValueX = 785;
    private const int FieldValueWidth = 575;

    private static readonly Rectangle ClientIdentityTextBounds = new(FieldValueX, 439, FieldValueWidth, 42);
    private static readonly Rectangle EngineTextBounds = new(FieldValueX, 503, FieldValueWidth, 42);

    #endregion

    #region ［DISCARD］ボタン

    /// <summary>編集内容を破棄して画面を閉じます。</summary>
    public Button DiscardButton { get; } = new(new Rectangle(1080, 288, 132, 42), "DISCARD", 0.30f);

    #endregion

    #region ［SAVE & CLOSE］ボタン

    /// <summary>編集内容を保存して画面を閉じます。</summary>
    public Button SaveAndCloseButton { get; } = new(new Rectangle(1224, 288, 148, 42), "SAVE & CLOSE", 0.26f);

    #endregion

    #region ［Table　＞　PLAYER NAME］

    /// <summary>［PLAYER NAME］のラベル</summary>
    TableRowLabel PlayerNameLabel { get; init; } = new("PLAYER NAME", new Rectangle(FieldLabelX, 382, 180, 32), new Color(180, 195, 195));

    /// <summary>［PLAYER NAME］の単一行テキスト入力用アンダーラインです。</summary>
    
    SinglelineTextUnderline PlayerNameTextUnderline { get; init; } = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    /// <summary>［PLAYER NAME］の付箋</summary>
    
    StickyNote PlayerNameStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 421), new Color(185, 196, 255), new Color(116, 145, 178), "PLAYER NAME とは？", ["画面に表示され、対局者に識別されるプレイヤーの呼び名です。"], 26);

    #endregion

    #region ［Table　＞　HANDLE］

    /// <summary>［HANDLE］のラベル</summary>
    TableRowLabel HandleLabel { get; init; } = new("HANDLE", new Rectangle(FieldLabelX, 446, 180, 32), new Color(180, 195, 195));

    /// <summary>［HANDLE］の付箋</summary>
    StickyNote HandleStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 485), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局サーバーにログインするときのプレイヤー名です。", "接続する環境ごとにプロフィールへ設定できます。"], 26);

    #endregion

    #region ［Table　＞　ENGINE］

    /// <summary>［ENGINE］のラベル</summary>
    TableRowLabel EngineLabel { get; init; } = new("ENGINE", new Rectangle(FieldLabelX, 510, 180, 32), new Color(180, 195, 195));

    /// <summary>［ENGINE］の付箋</summary>
    StickyNote EngineStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, 549), new Color(185, 196, 255), new Color(116, 145, 178), "ENGINE とは？", ["コンピューター対局に使用する GTP エンジンです。"], 26);

    #endregion

    #region Shared field components

    /// <summary>HANDLE と ENGINE の選択式フィールドで共用するリンクアンダーラインです。</summary>
    LinkUnderline PopupFieldUnderline { get; init; } = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    #endregion

    #region Hit testing and caret

    /// <summary>HANDLE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsClientIdentityChangeHit(Point point) => ClientIdentityTextBounds.Contains(point);

    /// <summary>ENGINE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsEngineChangeHit(Point point) => EngineTextBounds.Contains(point);

    /// <summary>入力編集を開始するフィールドを取得します。</summary>
    public EntryProfileEditField? GetFieldHit(Point point) =>
        PlayerFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point) ? EntryProfileEditField.DisplayName : null;

    /// <summary>外部から渡された文字幅計算を使ってキャレット位置を求めます。</summary>
    public int GetCaretIndex(Point point, EntryProfileEditField field, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, PlayerFieldTextBounds(field), 0.42f);

    #endregion

    #region Drawing

    /// <summary>［EDIT ENTRY PROFILE］画面を描画します。</summary>
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
        DrawPopupField(HandleLabel, session.PlayerEditClientIdentityHandle, ClientIdentityTextBounds, mousePoint, FieldIcon.None, draw);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPopupField(EngineLabel, session.PlayerEditEngineDisplayName, EngineTextBounds, mousePoint, FieldIcon.Engine, draw);

        // 付箋（一番前景に描画するために、最後に描画します）
        var stickyNote = FieldHoverBounds(PlayerFieldTextBounds(EntryProfileEditField.DisplayName)).Contains(mousePoint)
            ? PlayerNameStickyNote
            : FieldHoverBounds(ClientIdentityTextBounds).Contains(mousePoint)
                ? HandleStickyNote
                : session.PlayerEditDraft.Kind == EntryProfileKind.Computer && FieldHoverBounds(EngineTextBounds).Contains(mousePoint)
                    ? EngineStickyNote : null;
        if (stickyNote?.TryPlace(stickyNoteScreen) == true)
            stickyNote.Draw(new StickyNoteDrawingCallbacks(draw.DrawLine, draw.FillRectangle, draw.DrawRectangle, draw.DrawDynamicText));
    }

    /// <summary>［PLAYER NAME］欄を描画します。</summary>
    private void DrawPlayerNameField(GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        // ラベル
        PlayerNameLabel.Draw(draw.DrawText);

        // 下線
        var field = EntryProfileEditField.DisplayName;
        var textBounds = PlayerFieldTextBounds(field);
        var active = session.ActivePlayerEditField == field;
        PlayerNameTextUnderline.Draw(textBounds, active, textBounds.Contains(mousePoint), new UnderlineDrawingSurface(draw));

        // 選択範囲
        var text = session.GetPlayerEditFieldText(field);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);

        // テキスト
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);

        // キャレット
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);

        // アイコン
        DrawIcon(FieldIcon.PlayerName, IconBounds(textBounds), draw);
        draw.DrawEditHint(active, textBounds.Contains(mousePoint), textBounds);
    }

    /// <summary>HANDLE と ENGINE の、選択画面へ接続するフィールドを描画します。</summary>
    private void DrawPopupField(TableRowLabel label, string value, Rectangle textBounds, Point mousePoint, FieldIcon icon, EditEntryProfileDrawingCallbacks draw)
    {
        var hovered = textBounds.Contains(mousePoint);
        label.Draw(draw.DrawText);
        draw.DrawFittedText(value, textBounds, Color.White, 0.42f);
        PopupFieldUnderline.Draw(textBounds, hovered, new UnderlineDrawingSurface(draw));
        if (icon != FieldIcon.None) DrawIcon(icon, IconBounds(textBounds), draw);
        if (hovered) draw.DrawChangeHint(textBounds);
    }

    #endregion

    #region Layout helpers

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

    #endregion

    #region Private types

    private enum FieldIcon { None, PlayerName, Engine }

    private sealed class UnderlineDrawingSurface(EditEntryProfileDrawingCallbacks draw) : IUnderlineDrawingSurface
    {
        public void FillRectangle(Rectangle bounds, Color color) => draw.FillRectangle(bounds, color);
        public void FillRoundedRectangle(Rectangle bounds, int radius, Color color) => draw.FillRoundedRectangle(bounds, radius, color);
        public void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => draw.DrawLine(start, end, thickness, color);
    }

    #endregion
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
