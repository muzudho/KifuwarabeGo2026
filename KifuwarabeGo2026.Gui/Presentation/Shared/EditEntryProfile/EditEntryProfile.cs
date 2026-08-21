namespace KifuwarabeGo2026.Gui.Presentation.Shared.EditEntryProfile;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.SinglelineTextUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.TableRowLabel;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

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
    private const int FieldRowTop = 295;
    private const int FieldRowPitch = 80;

    private const int ClientIdentityLabelX = FieldLabelX;
    private const int ClientIdentityValueX = 785;
    private const int ClientIdentityValueWidth = 470;
    private static readonly Rectangle ClientIdentityHandleTextBounds = new(ClientIdentityValueX, 568, ClientIdentityValueWidth, 36);
    private static readonly Rectangle ClientIdentityPasswordTextBounds = new(ClientIdentityValueX, 630, ClientIdentityValueWidth, 36);
    private static readonly Rectangle ClientIdentityPasswordVisibilityButtonBounds = new(ClientIdentityPasswordTextBounds.Right - 4, ClientIdentityPasswordTextBounds.Y, 42, 36);
    private static readonly Rectangle ClientIdentityListButtonBounds = new(1312, FieldRowTop + FieldRowPitch + 29, 58, 58);
    private static readonly Rectangle EntryTypeHumanButtonBounds = new(FieldValueX, 365, 170, 46);
    private static readonly Rectangle EntryTypeEngineButtonBounds = new(FieldValueX + 182, 365, 170, 46);
    private static readonly Rectangle EngineTextBounds = new(FieldValueX, 430, FieldValueWidth, 42);
    private static readonly Rectangle AddClientIdentityButtonBounds = new(869, 876, 170, 42);
    private static readonly Rectangle HandleHeadingBounds = new(600, 526, 280, 32);
    private static readonly Rectangle PasswordHeadingBounds = new(890, 526, 280, 32);

    #endregion

    #region ［DISCARD］ボタン

    /// <summary>編集内容を破棄して画面を閉じます。</summary>
    public Button DiscardButton { get; } = new(new Rectangle(1080, 188, 132, 42), "DISCARD", 0.30f);

    #endregion

    #region ［SAVE & CLOSE］ボタン

    /// <summary>編集内容を保存して画面を閉じます。</summary>
    public Button SaveAndCloseButton { get; } = new(new Rectangle(1224, 188, 148, 42), "SAVE & CLOSE", 0.26f);

    /// <summary>ポップアップ選択フィールドへ表示する操作バッジです。</summary>
    public ActionBadgeComponent ChangeActionBadge { get; } = ActionBadgeComponent.Create("CHANGE", EngineTextBounds);

    #endregion

    #region ［Table　＞　ENTRY NAME］

    /// <summary>［ENTRY NAME］のラベル</summary>
    TableRowLabel PlayerNameLabel { get; init; } = new("ENTRY NAME", new Rectangle(FieldLabelX, FieldRowTop + 7, 180, 32), new Color(180, 195, 195));

    /// <summary>［PLAYER NAME］の単一行テキスト入力用アンダーラインです。</summary>
    
    SinglelineTextUnderline PlayerNameTextUnderline { get; init; } = new(new RoundUnderline { TopOffset = 2, Thickness = 5, Radius = 2 }, "EDIT");

    /// <summary>［PLAYER NAME］の付箋</summary>
    
    StickyNote PlayerNameStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(FieldValueX + FieldValueWidth - 24, FieldRowTop + 46), new Color(185, 196, 255), new Color(116, 145, 178), "ENTRY NAME とは？", ["画面に表示されたり、棋譜に保存されたり", "する対局者の名前です。", "『もし記事に掲載されたり、配信で", "呼び出されることになったら？』", "ということも考慮して、長すぎず、", "発音もしやすい名前にすると良いでしょう。"], 26);

    #endregion

    #region ［Table　＞　ENTRY TYPE］

    TableRowLabel EntryTypeLabel { get; init; } = new("ENTRY TYPE", new Rectangle(FieldLabelX, EntryTypeHumanButtonBounds.Y + 7, 180, 32), new Color(180, 195, 195));
    public Button HumanTypeButton { get; } = new(EntryTypeHumanButtonBounds, "HUMAN", 0.34f);
    public Button EngineTypeButton { get; } = new(EntryTypeEngineButtonBounds, "ENGINE", 0.34f);

    #endregion

    #region ［Table　＞　HANDLE］

    /// <summary>［HANDLE］のラベル</summary>
    TableRowLabel HandleLabel { get; init; } = new("HANDLE", new Rectangle(ClientIdentityLabelX, ClientIdentityHandleTextBounds.Y + 7, 180, 32), new Color(180, 195, 195));
    TableRowLabel PasswordLabel { get; init; } = new("PASSWORD", new Rectangle(ClientIdentityLabelX, ClientIdentityPasswordTextBounds.Y + 7, 180, 32), new Color(180, 195, 195));

    /// <summary>［HANDLE］の付箋</summary>
    StickyNote HandleStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(HandleHeadingBounds.Right, HandleHeadingBounds.Bottom), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局者の名前です。", "接続先の機械に入力できるフォーマットに", "合わせたものを言います。", "使える文字や、文字数の上限は", "確認しておいてください。"], 26);

    StickyNote PasswordStickyNote { get; init; } = new(StickyNoteKind.EntryProfileFieldHint, new Vector2(PasswordHeadingBounds.Right, PasswordHeadingBounds.Bottom), new Color(255, 218, 168), new Color(178, 116, 82), "PASSWORD とは？", ["他の人に知られてはいけない秘密の文字列です。", "パスワードの内容は接続先から", "見える可能性があります。", "他のサービスで使用しているパスワードは", "絶対に入力しないでください。"], 26);

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
    HashSet<string> VisibleClientIdentityPasswords { get; } = new(StringComparer.Ordinal);
    public Button AddClientIdentityButton { get; } = new(AddClientIdentityButtonBounds, "ADD", 0.32f);

    #endregion

    #region Hit testing and caret

    /// <summary>HANDLE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsClientIdentityChangeHit(Point point) => ClientIdentityListButton.IsHit(point);

    public (int Index, EntryProfileEditField Field)? GetClientIdentityFieldHit(Point point, int count) =>
        ClientIdentityCredentialPair.GetFieldHit(point, count);

    public int GetClientIdentityRemoveHit(Point point, int count) => ClientIdentityCredentialPair.GetRemoveHit(point, count);

    public int GetClientIdentityCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, int index,
        EntryProfileEditField field, string text) => drawingContext.GetTextCaretIndex(point.X, text,
            field switch
            {
                EntryProfileEditField.ClientIdentityHandle => ClientIdentityCredentialPair.HandleBounds(index),
                EntryProfileEditField.ClientIdentityPassword => ClientIdentityCredentialPair.PasswordBounds(index),
                EntryProfileEditField.ClientIdentityComment => ClientIdentityCredentialPair.CommentBounds(index),
                _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown client identity field."),
            }, 0.36f);

    public bool TryToggleClientIdentityPasswordVisibility(Point point, IReadOnlyList<ClientIdentityProfile> identities)
    {
        var index = ClientIdentityCredentialPair.GetVisibilityHit(point, identities.Count);
        if (index < 0) return false;
        var id = identities[index].Id;
        if (!VisibleClientIdentityPasswords.Add(id)) VisibleClientIdentityPasswords.Remove(id);
        return true;
    }

    /// <summary>目アイコンのクリックで PASSWORD の表示方法を切り替えます。</summary>
    public bool TryToggleClientIdentityPasswordVisibility(Point point)
    {
        if (!ClientIdentityPasswordVisibilityButton.IsHit(point)) return false;
        IsClientIdentityPasswordVisible = !IsClientIdentityPasswordVisible;
        return true;
    }

    /// <summary>ENGINE の変更リンクがクリックされたかを判定します。</summary>
    public bool IsEngineChangeHit(Point point) => EngineTextBounds.Contains(point);

    /// <summary>入力編集を開始するフィールドを取得します。</summary>
    public EntryProfileEditField? GetFieldHit(Point point) =>
        PlayerFieldTextBounds(EntryProfileEditField.DisplayName).Contains(point) ? EntryProfileEditField.DisplayName :
        ClientIdentityHandleTextBounds.Contains(point) ? EntryProfileEditField.ClientIdentityHandle :
        ClientIdentityPasswordTextBounds.Contains(point) ? EntryProfileEditField.ClientIdentityPassword : null;

    /// <summary>外部から渡された文字幅計算を使ってキャレット位置を求めます。</summary>
    public int GetCaretIndex(Point point, EntryProfileEditField field, string text, Func<int, string, Rectangle, float, int> getCaretIndex) =>
        (getCaretIndex ?? throw new ArgumentNullException(nameof(getCaretIndex)))(point.X, text, PlayerFieldTextBounds(field), 0.42f);

    public int GetCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, EntryProfileEditField field, string text) =>
        drawingContext.GetTextCaretIndex(point.X, text, PlayerFieldTextBounds(field), 0.42f);

    #endregion

    #region Drawing

    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint,
        StickyNoteScreenId stickyNoteScreen) =>
        Draw(session, mousePoint, stickyNoteScreen,
            new EditEntryProfileDrawingCallbacks(
                drawingContext.ScreenWidth,
                drawingContext.ScreenHeight,
                drawingContext.FillRectangle,
                drawingContext.FillRoundedRectangle,
                drawingContext.DrawRectangle,
                drawingContext.DrawText,
                drawingContext.DrawFittedText,
                drawingContext,
                drawingContext.DrawIconStone,
                drawingContext.DrawPlayerRoleFaceIcon,
                drawingContext.DrawTextSelection,
                drawingContext.DrawTextCaret,
                drawingContext.DrawLine,
                drawingContext.DrawDynamicText,
                drawingContext.DrawRotatedCenteredText));

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

        var bounds = new Rectangle(510, 145, 900, 900);
        draw.FillRectangle(new Rectangle(0, 0, draw.VirtualScreenWidth, draw.VirtualScreenHeight), new Color(0, 0, 0, 140));
        draw.FillRectangle(bounds, new Color(24, 29, 36, 252));
        draw.DrawRectangle(bounds, 2, new Color(116, 145, 146));
        draw.DrawText("EDIT ENTRY PROFILE", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DiscardButton.IsEnabled = session.HasPlayerEditChanges;
        SaveAndCloseButton.Label = session.HasPlayerEditChanges ? "SAVE & CLOSE" : "CLOSE";
        SaveAndCloseButton.LabelScale = session.HasPlayerEditChanges ? 0.26f : 0.34f;
        DiscardButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        SaveAndCloseButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        DrawPlayerNameField(session, mousePoint, draw);
        DrawEntryTypeField(session, mousePoint, draw);
        if (session.PlayerEditDraft.Kind == EntryProfileKind.Computer)
            DrawPopupField(EngineLabel, session.PlayerEditEngineDisplayName, EngineTextBounds, mousePoint, FieldIcon.Engine, draw);
        DrawClientIdentitySection(session, mousePoint, draw);

        // 付箋（一番前景に描画するために、最後に描画します）
        var stickyNote = FieldHoverBounds(PlayerFieldTextBounds(EntryProfileEditField.DisplayName)).Contains(mousePoint)
            ? PlayerNameStickyNote
            : HandleHeadingBounds.Contains(mousePoint)
                ? HandleStickyNote
                : PasswordHeadingBounds.Contains(mousePoint)
                    ? PasswordStickyNote
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
        PlayerNameTextUnderline.Bounds = textBounds;
        PlayerNameTextUnderline.SetEditing(active);
        PlayerNameTextUnderline.UpdatePointer(mousePoint);
        PlayerNameTextUnderline.Draw(draw.KfwStationeryDrawingTools);

        // 選択範囲
        var text = session.GetPlayerEditFieldText(field);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, textBounds, 0.42f);

        // テキスト
        draw.DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active && session.PlayerEditComposition.IsActive && !string.IsNullOrEmpty(session.PlayerEditComposition.Text))
        {
            var caret = Math.Clamp(session.PlayerEditCaretIndex, 0, text.Length);
            var prefixWidth = draw.KfwStationeryDrawingTools.MeasureText(text[..caret]).X * 0.42f;
            var compositionBounds = new Rectangle(
                textBounds.X + (int)prefixWidth,
                textBounds.Y,
                Math.Max(1, textBounds.Width - (int)prefixWidth),
                textBounds.Height);
            draw.DrawDynamicText(session.PlayerEditComposition.Text, compositionBounds, new Color(255, 225, 128), 0.42f);
            var compositionWidth = draw.KfwStationeryDrawingTools.MeasureText(session.PlayerEditComposition.Text).X * 0.42f;
            draw.DrawLine(
                new Vector2(compositionBounds.X, textBounds.Bottom - 1),
                new Vector2(Math.Min(textBounds.Right, compositionBounds.X + compositionWidth), textBounds.Bottom - 1),
                2,
                new Color(255, 225, 128));
        }

        // キャレット
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, textBounds, 0.42f);

        // アイコン
        DrawIcon(FieldIcon.EntryName, IconBounds(textBounds), draw);
    }

    private void DrawEntryTypeField(GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        EntryTypeLabel.Draw(draw.DrawText);
        HumanTypeButton.IsSelected = session.PlayerEditDraft.Kind == EntryProfileKind.Human;
        EngineTypeButton.IsSelected = session.PlayerEditDraft.Kind == EntryProfileKind.Computer;
        EngineTypeButton.IsEnabled = session.GtpEngineProfiles.Count > 0;
        HumanTypeButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        EngineTypeButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
    }

    private void DrawClientIdentitySection(GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var identities = session.PlayerEditClientIdentities;
        var sectionBounds = new Rectangle(ClientIdentityLabelX - 24, 552, 836, Math.Max(70, identities.Count * ClientIdentityCredentialPair.Pitch + 18));
        draw.DrawText($"CLIENT IDENTITIES  {identities.Count} / 5", new Vector2(ClientIdentityLabelX, 500), new Color(99, 223, 185), 0.34f);
        draw.DrawFittedText("HANDLE", HandleHeadingBounds, new Color(180, 195, 195), 0.50f);
        draw.DrawFittedText("PASSWORD", PasswordHeadingBounds, new Color(180, 195, 195), 0.50f);
        var listBottom = identities.Count == 0
            ? ClientIdentityCredentialPair.Top
            : ClientIdentityCredentialPair.RowBounds(identities.Count - 1).Bottom;
        AddClientIdentityButton.Bounds = new Rectangle(ClientIdentityCredentialPair.RowBounds(0).Center.X - 85, listBottom + 14, 170, 42);
        AddClientIdentityButton.IsEnabled = identities.Count < 5;
        AddClientIdentityButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        for (var index = 0; index < identities.Count; index++) DrawClientIdentityPair(index, identities[index], session, mousePoint, draw);
        var warningTop = AddClientIdentityButton.Bounds.Bottom + 8;
        draw.DrawDynamicText("パスワードの内容は接続先から見える可能性があります。",
            new Rectangle(ClientIdentityLabelX, warningTop, 812, 26), new Color(255, 190, 132), 0.28f);
        draw.DrawDynamicText("他のサービスで使用しているパスワードは絶対に入力しないでください。",
            new Rectangle(ClientIdentityLabelX, warningTop + 26, 812, 26), new Color(255, 190, 132), 0.28f);
    }

    private void DrawClientIdentityPair(int index, ClientIdentityProfile identity, GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var row = ClientIdentityCredentialPair.RowBounds(index);
        var selected = index == session.ClientIdentityProfileEditIndex;
        draw.FillRectangle(row, selected ? new Color(35, 48, 57, 235) : new Color(27, 34, 40, 220));
        draw.DrawRectangle(row, 1, selected ? new Color(99, 223, 185) : new Color(66, 86, 94));
        draw.DrawFittedText($"{index + 1}", new Rectangle(row.X + 16, row.Y + 8, 28, 36), new Color(178, 219, 226), 0.34f);
        DrawIdentityValue(index, EntryProfileEditField.ClientIdentityHandle, identity.LoginName, false, selected, session, mousePoint, draw);
        DrawIdentityValue(index, EntryProfileEditField.ClientIdentityPassword, identity.LoginPass,
            !VisibleClientIdentityPasswords.Contains(identity.Id), selected, session, mousePoint, draw);
        draw.DrawFittedText("COMMENT", new Rectangle(row.X + 62, row.Y + 37, 62, 24), new Color(180, 195, 195), 0.24f);
        DrawIdentityValue(index, EntryProfileEditField.ClientIdentityComment, identity.Comment, false, selected, session, mousePoint, draw);
        DrawEyeButton(ClientIdentityCredentialPair.VisibilityBounds(index), VisibleClientIdentityPasswords.Contains(identity.Id), mousePoint, draw);
        var remove = new Button(ClientIdentityCredentialPair.RemoveBounds(index), "REMOVE", 0.25f) { IsEnabled = session.PlayerEditClientIdentities.Count > 1 };
        remove.Draw(mousePoint, draw.KfwStationeryDrawingTools);
    }

    private void DrawIdentityValue(int index, EntryProfileEditField field, string text, bool mask, bool selected,
        GoAppSession session, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var bounds = field switch
        {
            EntryProfileEditField.ClientIdentityHandle => ClientIdentityCredentialPair.HandleBounds(index),
            EntryProfileEditField.ClientIdentityPassword => ClientIdentityCredentialPair.PasswordBounds(index),
            EntryProfileEditField.ClientIdentityComment => ClientIdentityCredentialPair.CommentBounds(index),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown client identity field."),
        };
        var active = selected && session.ActivePlayerEditField == field;
        PlayerNameTextUnderline.Bounds = bounds;
        PlayerNameTextUnderline.SetEditing(active);
        PlayerNameTextUnderline.UpdatePointer(mousePoint);
        PlayerNameTextUnderline.Draw(draw.KfwStationeryDrawingTools);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, bounds, 0.36f);
        draw.DrawFittedText(mask ? new string('●', text.Length) : string.IsNullOrEmpty(text) ? "-" : text, bounds, Color.White, 0.36f);
        if (field == EntryProfileEditField.ClientIdentityComment)
        {
            draw.FillRectangle(bounds, selected ? new Color(35, 48, 57, 235) : new Color(27, 34, 40, 220));
            if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, bounds, 0.36f);
            draw.DrawDynamicText(string.IsNullOrEmpty(text) ? "-" : text, bounds, Color.White, 0.36f);
            PlayerNameTextUnderline.Bounds = bounds;
            PlayerNameTextUnderline.SetEditing(active);
            PlayerNameTextUnderline.UpdatePointer(mousePoint);
            PlayerNameTextUnderline.Draw(draw.KfwStationeryDrawingTools);
        }
        if (active && session.PlayerEditComposition.IsActive && !string.IsNullOrEmpty(session.PlayerEditComposition.Text))
        {
            var caret = Math.Clamp(session.PlayerEditCaretIndex, 0, text.Length);
            var prefixWidth = draw.KfwStationeryDrawingTools.MeasureText(text[..caret]).X * 0.36f;
            var compositionText = mask
                ? new string('●', session.PlayerEditComposition.Text.Length)
                : session.PlayerEditComposition.Text;
            var compositionBounds = new Rectangle(
                bounds.X + (int)prefixWidth,
                bounds.Y,
                Math.Max(1, bounds.Width - (int)prefixWidth),
                bounds.Height);
            draw.DrawDynamicText(compositionText, compositionBounds, new Color(255, 225, 128), 0.36f);
            var compositionWidth = draw.KfwStationeryDrawingTools.MeasureText(compositionText).X * 0.36f;
            draw.DrawLine(
                new Vector2(compositionBounds.X, bounds.Bottom - 1),
                new Vector2(Math.Min(bounds.Right, compositionBounds.X + compositionWidth), bounds.Bottom - 1),
                2,
                new Color(255, 225, 128));
        }
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, bounds, 0.36f);
    }

    private static void DrawEyeButton(Rectangle bounds, bool visible, Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        new Button(bounds, string.Empty, 0.1f).Draw(mousePoint, draw.KfwStationeryDrawingTools);
        var color = bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);
        draw.DrawLine(new Vector2(bounds.X + 7, center.Y), new Vector2(center.X, visible ? bounds.Y + 8 : center.Y + 2), 2, color);
        draw.DrawLine(new Vector2(center.X, visible ? bounds.Y + 8 : center.Y + 2), new Vector2(bounds.Right - 7, center.Y), 2, color);
        if (visible) draw.FillRectangle(new Rectangle(bounds.Center.X - 3, bounds.Center.Y - 3, 6, 6), color);
    }

    private static void DrawVerticalSectionLabel(Rectangle sectionBounds, string title, Color accent, EditEntryProfileDrawingCallbacks draw)
    {
        var labelBounds = new Rectangle(sectionBounds.X - 48, sectionBounds.Y, 40, sectionBounds.Height);
        draw.FillRectangle(labelBounds, new Color(accent, 150));
        draw.DrawRectangle(labelBounds, 1, new Color(accent, 230));
        draw.DrawVerticalText(title, new Vector2(labelBounds.Center.X, labelBounds.Center.Y), new Color(205, 218, 218), 0.30f);
    }

    private void DrawEditableIdentityField(TableRowLabel label, EntryProfileEditField field, GoAppSession session,
        Rectangle bounds, Point mousePoint, EditEntryProfileDrawingCallbacks draw, bool mask)
    {
        label.Draw(draw.DrawText);
        var active = session.ActivePlayerEditField == field;
        PlayerNameTextUnderline.Bounds = bounds;
        PlayerNameTextUnderline.SetEditing(active);
        PlayerNameTextUnderline.UpdatePointer(mousePoint);
        PlayerNameTextUnderline.Draw(draw.KfwStationeryDrawingTools);
        var text = session.GetPlayerEditFieldText(field);
        if (active) draw.DrawTextSelection(text, session.PlayerEditSelectionStart, session.PlayerEditSelectionLength, bounds, 0.42f);
        draw.DrawFittedText(mask ? new string('●', text.Length) : text, bounds, Color.White, 0.42f);
        if (active) draw.DrawTextCaret(text, session.PlayerEditCaretIndex, bounds, 0.42f);
    }

    private void DrawClientIdentityListButton(Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        ClientIdentityListButton.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        var bounds = ClientIdentityListButton.Bounds;
        var color = bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);

        // 箇条書きの紙。既存のワイヤーフレーム調に合わせ、塗りを最小限にします。
        var paper = new Rectangle(bounds.X + 18, bounds.Y + 16, 22, 26);
        draw.DrawRectangle(paper, 2, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y), new Vector2(paper.Right, paper.Y + 8), 1, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y), new Vector2(paper.Right - 8, paper.Y + 8), 1, color);
        draw.DrawLine(new Vector2(paper.Right - 8, paper.Y + 8), new Vector2(paper.Right, paper.Y + 8), 1, color);
        for (var row = 0; row < 3; row++)
        {
            var y = paper.Y + 8 + row * 6;
            draw.FillRectangle(new Rectangle(paper.X + 4, y, 3, 3), color);
            draw.DrawLine(new Vector2(paper.X + 10, y + 1), new Vector2(paper.Right - 4, y + 1), 1, color);
        }
    }

    private void DrawClientIdentityPasswordVisibilityButton(Point mousePoint, EditEntryProfileDrawingCallbacks draw)
    {
        var button = ClientIdentityPasswordVisibilityButton;
        button.IsEnabled = true;
        button.Draw(mousePoint, draw.KfwStationeryDrawingTools);
        var bounds = button.Bounds;
        var color = bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);
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
        PopupFieldUnderline.Draw(draw.KfwStationeryDrawingTools);
        if (icon != FieldIcon.None) DrawIcon(icon, IconBounds(textBounds), draw);
        if (hovered)
        {
            ChangeActionBadge.SetAnchorBounds(textBounds);
            ChangeActionBadge.Show();
            ChangeActionBadge.Draw(draw.KfwStationeryDrawingTools);
        }
        else
        {
            ChangeActionBadge.Hide();
        }
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
        if (icon == FieldIcon.EntryName)
        {
            draw.KfwStationeryDrawingTools.DrawEntryIcon(new Vector2(bounds.Center.X, bounds.Center.Y));
            return;
        }
        if (icon == FieldIcon.Engine) draw.DrawPlayerRoleFace(new Vector2(bounds.Center.X, bounds.Center.Y), true);
    }

    #endregion

    #region Private types

    private enum FieldIcon { None, EntryName, Engine }

    #endregion

    // Static layout rectangles must be initialized before controls capture them.
    public static EditEntryProfile Default { get; } = new();
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
    KfwStationeryDrawingTools KfwStationeryDrawingTools,
    Action<Vector2, float, bool> DrawStone,
    Action<Vector2, bool> DrawPlayerRoleFace,
    Action<string, int, int, Rectangle, float> DrawTextSelection,
    Action<string, int, Rectangle, float> DrawTextCaret,
    Action<Vector2, Vector2, float, Color> DrawLine,
    Action<string, Rectangle, Color, float> DrawDynamicText,
    Action<string, Vector2, Color, float> DrawVerticalText);
