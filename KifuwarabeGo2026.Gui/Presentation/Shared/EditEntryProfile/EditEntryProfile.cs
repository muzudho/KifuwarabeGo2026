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
    // 4 項目を 80px ピッチの見えない行グリッドへそろえます。
    private const int FieldLabelX = 548;
    private const int FieldIconX = 742;
    private const int FieldValueX = 785;
    private const int FieldValueWidth = 575;
    private const int FieldRowTop = 375;
    private const int FieldRowPitch = 80;

    private const int ClientIdentityLabelX = FieldLabelX;
    private const int ClientIdentityValueX = 785;
    private const int ClientIdentityValueWidth = 470;
    private static readonly Rectangle ClientIdentityHandleTextBounds = new(ClientIdentityValueX, FieldRowTop + FieldRowPitch, ClientIdentityValueWidth, 36);
    private static readonly Rectangle ClientIdentityPasswordTextBounds = new(ClientIdentityValueX, FieldRowTop + FieldRowPitch * 2, ClientIdentityValueWidth, 36);
    private static readonly Rectangle ClientIdentityPasswordVisibilityButtonBounds = new(ClientIdentityPasswordTextBounds.Right - 46, ClientIdentityPasswordTextBounds.Y, 42, 36);
    private static readonly Rectangle ClientIdentityListButtonBounds = new(1270, FieldRowTop + FieldRowPitch + 29, 58, 58);
    private static readonly Rectangle EngineTextBounds = new(FieldValueX, FieldRowTop + FieldRowPitch * 3, FieldValueWidth, 42);

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
    TableRowLabel PlayerNameLabel { get; init; } = new("PLAYER NAME", new Rectangle(FieldLabelX, FieldRowTop + 7, 180, 32), new Color(180, 195, 195));

    /// <summary>［PLAYER NAME］の単一行テキスト入力用アンダーラインです。</summary>
    
    SinglelineTextUnderline PlayerNameTextUnderline { get; init; } = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    /// <summary>［PLAYER NAME］の付箋</summary>
    
    StickyNote PlayerNameStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, FieldRowTop + 46), new Color(185, 196, 255), new Color(116, 145, 178), "PLAYER NAME とは？", ["画面に表示され、対局者に識別されるプレイヤーの呼び名です。"], 26);

    #endregion

    #region ［Table　＞　HANDLE］

    /// <summary>［HANDLE］のラベル</summary>
    TableRowLabel HandleLabel { get; init; } = new("HANDLE", new Rectangle(ClientIdentityLabelX, ClientIdentityHandleTextBounds.Y + 7, 180, 32), new Color(180, 195, 195));
    TableRowLabel PasswordLabel { get; init; } = new("PASSWORD", new Rectangle(ClientIdentityLabelX, ClientIdentityPasswordTextBounds.Y + 7, 180, 32), new Color(180, 195, 195));

    /// <summary>［HANDLE］の付箋</summary>
    StickyNote HandleStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(ClientIdentityHandleTextBounds.Right, ClientIdentityHandleTextBounds.Bottom + 2), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局サーバーにログインするときのプレイヤー名です。", "接続する環境ごとにプロフィールへ設定できます。"], 26);

    #endregion

    #region ［Table　＞　ENGINE］

    /// <summary>［ENGINE］のラベル</summary>
    TableRowLabel EngineLabel { get; init; } = new("ENGINE", new Rectangle(FieldLabelX, EngineTextBounds.Y + 7, 180, 32), new Color(180, 195, 195));

    /// <summary>［ENGINE］の付箋</summary>
    StickyNote EngineStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, EngineTextBounds.Y + 46), new Color(185, 196, 255), new Color(116, 145, 178), "ENGINE とは？", ["コンピューター対局に使用する GTP エンジンです。"], 26);

    #endregion

    #region Shared field components

    /// <summary>HANDLE と ENGINE の選択式フィールドで共用するリンクアンダーラインです。</summary>
    LinkUnderline PopupFieldUnderline { get; init; } = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 });

    /// <summary>登録済みクライアント ID を選ぶリストボタンです。</summary>
    Button ClientIdentityListButton { get; } = new(ClientIdentityListButtonBounds, string.Empty, 0.1f);

    /// <summary>PASSWORD の平文表示を切り替える目アイコンです。</summary>
    Button ClientIdentityPasswordVisibilityButton { get; } = new(ClientIdentityPasswordVisibilityButtonBounds, string.Empty, 0.1f);
    bool IsClientIdentityPasswordVisible { get; set; }

    #endregion

    #region Hit testing and caret

    /// <summary>HANDLE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsClientIdentityChangeHit(Point point) => ClientIdentityListButton.IsHit(point);

    /// <summary>目アイコンのクリックで PASSWORD の表示方法を切り替えます。</summary>
    public bool TryToggleClientIdentityPasswordVisibility(Point point, bool passwordEnabled)
    {
        if (!passwordEnabled || !ClientIdentityPasswordVisibilityButton.IsHit(point)) return false;
        IsClientIdentityPasswordVisible = !IsClientIdentityPasswordVisible;
        return true;
    }

    /// <summary>ENGINE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsEngineChangeHit(Point point) => EngineTextBounds.Contains(point);

    /// <summary>入力編集を開始するフィールドを取得します。</summary>
    public EntryProfileEditField? GetFieldHit(Point point, bool passwordEnabled) =>
        PlayerFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point) ? EntryProfileEditField.DisplayName :
        ClientIdentityHandleTextBounds.Contains(point) ? EntryProfileEditField.ClientIdentityHandle :
        passwordEnabled && ClientIdentityPasswordTextBounds.Contains(point) ? EntryProfileEditField.ClientIdentityPassword : null;

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
        if (!session.IsPlayerEditPanelOpen)
        {
            IsClientIdentityPasswordVisible = false;
            return;
        }

        var bounds = new Rectangle(510, 270, 900, 528);
        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 140));
        draw.FillRectangle(bounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(bounds, 2, new Color(116, 145, 146));
        draw.DrawText("EDIT ENTRY PROFILE", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DiscardButton.IsEnabled = session.HasPlayerEditChanges;
        SaveAndCloseButton.Label = session.HasPlayerEditChanges ? "SAVE & CLOSE" : "CLOSE";
        SaveAndCloseButton.LabelScale = session.HasPlayerEditChanges ? 0.26f : 0.34f;
        DiscardButton.Draw(mousePoint, draw.DrawButton);
        SaveAndCloseButton.Draw(mousePoint, draw.DrawButton);
        DrawPlayerNameField(session, mousePoint, draw);
        DrawClientIdentitySection(session, mousePoint, draw);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPopupField(EngineLabel, session.PlayerEditEngineDisplayName, EngineTextBounds, mousePoint, FieldIcon.Engine, draw);

        // 付箋（一番前景に描画するために、最後に描画します）
        var stickyNote = FieldHoverBounds(PlayerFieldTextBounds(EntryProfileEditField.DisplayName)).Contains(mousePoint)
            ? PlayerNameStickyNote
            : FieldHoverBounds(ClientIdentityHandleTextBounds).Contains(mousePoint)
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

    private void DrawClientIdentitySection(GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var sectionBounds = new Rectangle(ClientIdentityLabelX - 24, ClientIdentityHandleTextBounds.Y - 9, 804, 138);
        draw.DrawLine(new Vector2(sectionBounds.X, sectionBounds.Y), new Vector2(sectionBounds.Right, sectionBounds.Y), 1, new Color(58, 78, 86));
        DrawVerticalSectionLabel(sectionBounds, "CLIENT IDENTITY", new Color(66, 104, 116), draw);
        DrawEditableIdentityField(HandleLabel, EntryProfileEditField.ClientIdentityHandle, session, ClientIdentityHandleTextBounds, mousePoint, draw, mask: false);
        DrawEditableIdentityField(PasswordLabel, EntryProfileEditField.ClientIdentityPassword, session, ClientIdentityPasswordTextBounds, mousePoint, draw,
            mask: !IsClientIdentityPasswordVisible, enabled: !session.IsPlayerEditClientIdentityPasswordDisabled);
        DrawClientIdentityPasswordVisibilityButton(mousePoint, !session.IsPlayerEditClientIdentityPasswordDisabled, draw);
        DrawClientIdentityListButton(mousePoint, draw);
    }

    private static void DrawVerticalSectionLabel(Rectangle sectionBounds, string title, Color accent, EditEntryProfileDrawingCallbacks draw)
    {
        var labelBounds = new Rectangle(sectionBounds.X - 48, sectionBounds.Y, 40, sectionBounds.Height);
        draw.FillRectangle(labelBounds, new Color(accent, 150)); draw.DrawRectangle(labelBounds, 1, new Color(accent, 230));
        draw.DrawVerticalText(title, new Vector2(labelBounds.Center.X, labelBounds.Center.Y), new Color(205, 218, 218), 0.30f);
    }

    private void DrawEditableIdentityField(TableRowLabel label, EntryProfileEditField field, GoAppSession session,
        Rectangle bounds, Point mousePoint, EditEntryProfileDrawingCallbacks draw, bool mask, bool enabled = true)
    {
        label.Draw(draw.DrawText);
        if (!enabled)
        {
            draw.FillRectangle(bounds, new Color(28, 34, 40, 235));
            draw.DrawLine(new Vector2(bounds.X, bounds.Bottom - 2), new Vector2(bounds.Right, bounds.Bottom - 2), 2, new Color(53, 65, 70));
            draw.DrawFittedText("NOT USED FOR LOCAL MATCH", bounds, new Color(90, 104, 108), 0.30f);
            return;
        }
        var active = session.ActivePlayerEditField == field;
        PlayerNameTextUnderline.Draw(bounds, active, bounds.Contains(mousePoint), new UnderlineDrawingSurface(draw));
        var text = session.GetPlayerEditFieldText(field);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, bounds, 0.42f);
        draw.DrawFittedText(mask ? new string('●', text.Length) : text, bounds, Color.White, 0.42f);
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, bounds, 0.42f);
    }

    private void DrawClientIdentityListButton(Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        ClientIdentityListButton.Draw(mousePoint, draw.DrawButton);
        var bounds = ClientIdentityListButton.Bounds;
        var color = bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);

        // 箇条書きの紙。既存のワイヤーフレーム調に合わせ、塗りを最小限にします。
        var paper = new Rectangle(bounds.X + 16, bounds.Y + 12, 26, 32);
        draw.DrawRectangle(paper, 2, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y), new Vector2(paper.Right, paper.Y + 8), 1, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y), new Vector2(paper.Right - 8, paper.Y + 8), 1, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y + 8), new Vector2(paper.Right, paper.Y + 8), 1, color);
        for (var row = 0; row < 3; row++)
        {
            var y = paper.Y + 10 + row * 7;
            draw.FillRectangle(new Rectangle(paper.X + 4, y, 3, 3), color);
            draw.DrawLine(new Vector2(paper.X + 10, y + 1), new Vector2(paper.Right - 4, y + 1), 1, color);
        }
    }

    private void DrawClientIdentityPasswordVisibilityButton(Point mousePoint, bool enabled, EditEntryProfileDrawingCallbacks draw)
    {
        var button = ClientIdentityPasswordVisibilityButton;
        button.IsEnabled = enabled;
        button.Draw(mousePoint, draw.DrawButton);
        var bounds = button.Bounds;
        var color = !enabled ? new Color(76, 88, 92) : bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);

        if (IsClientIdentityPasswordVisible)
        {
            // 開いた目: 菱形のまぶたと瞳。
            draw.DrawLine(new Vector2(bounds.X + 7, center.Y), new Vector2(center.X, bounds.Y + 8), 2, color);
            draw.DrawLine(new Vector2(center.X, bounds.Y + 8), new Vector2(bounds.Right - 7, center.Y), 2, color);
            draw.DrawLine(new Vector2(bounds.Right - 7, center.Y), new Vector2(center.X, bounds.Bottom - 8), 2, color);
            draw.DrawLine(new Vector2(center.X, bounds.Bottom - 8), new Vector2(bounds.X + 7, center.Y), 2, color);
            draw.FillRectangle(new Rectangle(bounds.Center.X - 3, bounds.Center.Y - 3, 6, 6), color);
        }
        else
        {
            // 閉じた目: 下へ伸びるまつ毛で、まぶたを閉じた姿にします。
            draw.DrawLine(new Vector2(bounds.X + 7, center.Y - 3), new Vector2(center.X, center.Y + 2), 2, color);
            draw.DrawLine(new Vector2(center.X, center.Y + 2), new Vector2(bounds.Right - 7, center.Y - 3), 2, color);
            draw.DrawLine(new Vector2(bounds.X + 11, center.Y - 1), new Vector2(bounds.X + 9, center.Y + 4), 1, color);
            draw.DrawLine(new Vector2(center.X, center.Y + 3), new Vector2(center.X, center.Y + 8), 1, color);
            draw.DrawLine(new Vector2(bounds.Right - 11, center.Y - 1), new Vector2(bounds.Right - 9, center.Y + 4), 1, color);
        }
    }

    /// <summary>HANDLE と ENGINE の、選択画面へ接続するフィールドを描画します。</summary>
    private void DrawPopupField(TableRowLabel label, string value, Rectangle textBounds, Point mousePoint, FieldIcon icon, EditEntryProfileDrawingCallbacks draw)
    {
        var hovered = textBounds.Contains(mousePoint);
        label.Draw(draw.DrawText);
        draw.DrawFittedText(value, textBounds, Color.White, 0.42f);
        PopupFieldUnderline.Bounds = textBounds;
        PopupFieldUnderline.UpdatePointer(mousePoint);
        PopupFieldUnderline.Draw(new UnderlineDrawingSurface(draw));
        if (icon != FieldIcon.None) DrawIcon(icon, IconBounds(textBounds), draw);
        if (hovered) draw.DrawChangeHint(textBounds);
    }

    #endregion

    #region Layout helpers

    private static Rectangle PlayerFieldTextBounds(EntryProfileEditField field) => field switch
    {
        EntryProfileEditField.DisplayName => new(FieldValueX, FieldRowTop, FieldValueWidth, 42),
        EntryProfileEditField.Identifier => new(FieldValueX, 439, FieldValueWidth, 42),
        EntryProfileEditField.ClientIdentityHandle => ClientIdentityHandleTextBounds,
        EntryProfileEditField.ClientIdentityPassword => ClientIdentityPasswordTextBounds,
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
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Vector2, Color, float> DrawVerticalText);
