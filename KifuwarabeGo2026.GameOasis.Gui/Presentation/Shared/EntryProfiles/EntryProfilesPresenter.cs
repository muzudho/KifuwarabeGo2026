namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.EntryProfiles;

using KifuwarabeGo2026.GameOasis.Gui.Application;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Reference.PlayDomain.Go;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.LocalMatch;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.RightSidePanel;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.GameOasis.Gui.Presentation;
using static KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.EntryProfiles.EntryProfilesScreenBounds;

/// <summary>Player 選択欄と選択ダイアログの描画・当たり判定。</summary>
public sealed class EntryProfilesPresenter
{
    public static EntryProfilesPresenter Default { get; } = new();
    private readonly ActionBadgeComponent _editActionBadge = ActionBadgeComponent.Create("EDIT", Rectangle.Empty);
    private KfwStationeryDrawingTools _drawingContext = null!;

    private EntryProfilesPresenter()
    {
    }

    public void DrawPanels(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePoint)
    {
        _drawingContext = drawingContext;
        DrawClientIdentityProfileEditPanel(session, mousePoint);
        DrawQuickClientIdentitySelectionPanel(session, mousePoint);
    }

    public int GetLocalMatchHandleCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, GoStone stone, string text, bool isPonnuki) =>
        drawingContext.GetTextCaretIndex(point.X, text, LocalMatchScreen.Default.GetHandleTextBounds(stone, isPonnuki), 0.32f);
    public int GetClientIdentityProfileEditCaretIndex(KfwStationeryDrawingTools drawingContext, Point point, int index, ClientIdentityProfileEditField field, string text, bool isLocalMatch) =>
        drawingContext.GetTextCaretIndex(point.X, text, ClientIdentityProfileEditFieldTextBounds(index, field, isLocalMatch), 0.34f);

    private void DrawClientIdentityProfileEditPanel(GoAppSession session, Point mousePoint)
    {
        if (session.IsClientIdentityProfileSelectionPanelOpen)
        {
            DrawClientIdentityProfileSelectionPanel(session, mousePoint);
            return;
        }
        if (!session.IsClientIdentityProfileEditPanelOpen) return;
        var profileEdit = EntryProfilesScreen.Default.ProfileEdit;
        profileEdit.UpdateState(session.IsClientIdentityProfileEditDirty);
        var bounds = new Rectangle(430, 150, 1080, 760);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("EDIT CLIENT IDENTITY", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.68f);
        DrawDynamicOptionText("機械で扱えるフォーマットのプレイヤー情報を設定できます。", new Rectangle(bounds.X + 36, bounds.Y + 82, 860, 32), new Color(180, 195, 195), 0.34f);
        var targets = session.GetPlayerClientIdentityProfiles(session.PlayerEditDraft.Id);
        profileEdit.DiscardButton.Draw(mousePoint, _stationeryDrawingContext);
        profileEdit.SaveButton.Draw(mousePoint, _stationeryDrawingContext);
        DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.LoginName, "HANDLE", mousePoint, false);
        DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.LoginPass, "PASSWORD", mousePoint, false);
        DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.Comment, "COMMENT", mousePoint, false);
    }
#if false
        for (var index = 0; index < targets.Count; index++)
        {
            // 編集画面には、操作対象の一件だけを表示する。選択リストは別画面へ分離する。
            if (index != session.ClientIdentityProfileEditIndex) continue;
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var isSelectedClientIdentity = index == session.ClientIdentityProfileEditIndex;
            if (isSelectedClientIdentity)
            {
                DrawText("▶", new Vector2(row.X + 4, row.Y + 25), new Color(147, 244, 200), 0.34f);
                DrawRect(row, 2, new Color(147, 244, 200));
            }
            if (isSelectedClientIdentity)
            {
                DrawClientIdentityProfileEditField(session, index, ClientIdentityProfileEditField.LoginName, "HANDLE", mousePoint, false);
                DrawClientIdentityProfileEditField(session, index, ClientIdentityProfileEditField.LoginPass, "PASSWORD", mousePoint, false);
                DrawFittedText($"CONNECTION: {session.ClientIdentityProfileEditConnectionDisplayName}", new Rectangle(row.X + 18, row.Y + 47, 920, 22), new Color(147, 244, 200), 0.28f);
                if (session.IsClientIdentityProfileDefault(index))
                    DrawFittedText("IN DEFAULT", new Rectangle(row.Right - 130, row.Y + 45, 110, 22), new Color(147, 244, 200), 0.28f);
            }
            else
            {
                DrawFittedText(target.DisplayName, new Rectangle(row.X + 18, row.Y + 10, 240, 28), Color.White, 0.46f);
                DrawFittedText($"HANDLE: {target.LoginName}", new Rectangle(row.X + 280, row.Y + 10, 390, 28), new Color(180, 195, 195), 0.34f);
                DrawFittedText(session.GetClientIdentityProfileConnectionDisplayName(target), new Rectangle(row.X + 280, row.Y + 42, 520, 24), new Color(147, 244, 200), 0.30f);
            }
        }
    }

#endif
    private void DrawClientIdentityProfileSelectionPanel(GoAppSession session, Point mousePoint)
    {
        var bounds = new Rectangle(430, 150, 1080, 760);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(24, 29, 36, 252));
        DrawRect(bounds, 2, new Color(116, 145, 146));
        DrawText("FAVORITE CLIENT IDENTITIES", new Vector2(bounds.X + 34, bounds.Y + 28), new Color(244, 238, 218), 0.58f);
        DrawFittedText("UP TO FIVE IDENTITIES CAN BE SAVED.", new Rectangle(bounds.X + 36, bounds.Y + 65, 500, 22), new Color(180, 195, 195), 0.28f);
        DrawFittedText("GREEN: default input     BLUE: input source", new Rectangle(bounds.X + 36, bounds.Y + 87, 500, 22), new Color(180, 210, 215), 0.29f);
        var targets = session.GetPlayerClientIdentityProfiles(session.PlayerEditDraft.Id);
        var selection = EntryProfilesScreen.Default.ProfileSelection;
        selection.UpdateState(targets.Count, session.IsClientIdentityProfileDefault(session.ClientIdentityProfileSelectionIndex));
        selection.UseButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.CloseButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.AddButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.DuplicateButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.EditButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.SetDefaultButton.Draw(mousePoint, _stationeryDrawingContext);
        selection.DeleteButton.Draw(mousePoint, _stationeryDrawingContext);

        var firstRow = new Rectangle(bounds.X + 36, bounds.Y + 140, bounds.Width - 72, 78);
        DrawFittedText("HANDLE", new Rectangle(firstRow.X + 18, firstRow.Y - 27, 300, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("PASSWORD", new Rectangle(firstRow.X + 335, firstRow.Y - 27, 150, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("COMMENT", new Rectangle(firstRow.X + 500, firstRow.Y - 27, 310, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("IN DEFAULT", new Rectangle(firstRow.X + 825, firstRow.Y - 27, 145, 24), new Color(180, 210, 215), 0.30f);

        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var selected = session.IsClientIdentityProfileDefault(index);
            var operated = index == session.ClientIdentityProfileSelectionIndex;
            FillRect(row, selected ? new Color(38, 103, 86) : row.Contains(mousePoint) ? new Color(43, 52, 62) : new Color(24, 31, 37));
            DrawRect(row, operated ? 3 : selected ? 2 : 1, operated ? new Color(125, 225, 255) : selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
            if (operated) DrawSelectionFingerMark(new Vector2(row.X - 55, row.Center.Y - 13), 1.65f);
            DrawFittedText(target.LoginName, new Rectangle(row.X + 18, row.Y + 25, 300, 30), Color.White, 0.42f);
            DrawFittedText(string.IsNullOrEmpty(target.LoginPass) ? "NONE" : "SET", new Rectangle(row.X + 335, row.Y + 25, 150, 30), Color.White, 0.34f);
            DrawDynamicOptionText(target.Comment, new Rectangle(row.X + 500, row.Y + 25, 310, 30), new Color(180, 195, 195), 0.32f);
            if (selected)
                DrawFittedText("IN DEFAULT", new Rectangle(row.X + 825, row.Y + 25, 145, 30), new Color(147, 244, 200), 0.28f);
        }
    }

#if false
    private void DrawClientIdentityProfileConnectionSelectionPanel(GoAppSession session, Point mousePoint)
    {
        DrawClientIdentityProfileEditField(session, 0, ClientIdentityProfileEditField.Comment, "COMMENT", mousePoint, false);

        var connectionControls = EntryProfilesScreen.Default.ConnectionSelection;
        connectionControls.UpdateState(session.ClientIdentityProfileConnectionSelectionPageIndex, session.ClientIdentityProfileConnectionSelectionPageCount);

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(ClientIdentityProfileConnectionSelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(ClientIdentityProfileConnectionSelectionPanelBounds, 2, new Color(116, 145, 146));
        DrawText("SELECT ONLINE MATCH SERVER", new Vector2(542, 240), new Color(244, 238, 218), 0.50f);
        DrawText("Choose the OnlineMatch (CGOS) server for this Client Identity.", new Vector2(544, 294), new Color(180, 195, 195), 0.28f);
        connectionControls.CancelButton.Draw(mousePoint, _stationeryDrawingContext);
        connectionControls.SelectButton.Draw(mousePoint, _stationeryDrawingContext);

        for (var slot = 0; slot < GoAppSession.ClientIdentityProfileConnectionSelectionPageSize; slot++)
        {
            var index = session.ClientIdentityProfileConnectionSelectionPageIndex * GoAppSession.ClientIdentityProfileConnectionSelectionPageSize + slot;
            if (index >= session.CgosConnectionProfiles.Count) break;
            var connection = session.CgosConnectionProfiles[index];
            var bounds = ClientIdentityProfileConnectionSelectionItemBounds(slot);
            var selected = index == session.ClientIdentityProfileConnectionSelectionIndex;
            DrawDataRowFrame(bounds, active: selected, hovered: bounds.Contains(mousePoint));
            DrawFittedText(connection.DisplayName, new Rectangle(bounds.X + 20, bounds.Y + 10, bounds.Width - 40, 28), Color.White, 0.40f);
            DrawFittedText($"{connection.Host}:{connection.Port}  /  {connection.Event}  /  {connection.Round}".Trim(), new Rectangle(bounds.X + 20, bounds.Y + 43, bounds.Width - 40, 22), new Color(180, 195, 195), 0.26f);
        }

        DrawFittedText($"PAGE {session.ClientIdentityProfileConnectionSelectionPageIndex + 1} / {session.ClientIdentityProfileConnectionSelectionPageCount}", new Rectangle(872, 805, 168, 30), new Color(227, 224, 210), 0.34f);
        connectionControls.PreviousButton.Draw(mousePoint, _stationeryDrawingContext);
        connectionControls.NextButton.Draw(mousePoint, _stationeryDrawingContext);
    }

#endif
    private void DrawQuickClientIdentitySelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsQuickClientIdentitySelectionPanelOpen) return;
        var quick = EntryProfilesScreen.Default.QuickSelection;
        var targets = session.GetQuickClientIdentitySelectionTargets(session.QuickClientIdentitySelectionStone, session.QuickClientIdentitySelectionIsCgos);
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 125));
        FillRect(QuickClientIdentitySelectionPanelBounds, new Color(19, 24, 31, 252));
        DrawRect(QuickClientIdentitySelectionPanelBounds, 2, new Color(116, 145, 146));
        var service = session.QuickClientIdentitySelectionIsCgos ? "ONLINE MATCH (CGOS)" : "LOCAL MATCH";
        DrawText("SELECT TEMPORARY HANDLE", new Vector2(590, 277), new Color(244, 238, 218), 0.56f);
        DrawStickyNote(
            StickyNoteKind.QuickClientIdentityHandleHint,
            new Vector2(960, 805),
            new Color(147, 244, 200),
            new Color(116, 145, 146),
            "HANDLE とは？",
            ["機械に入力できる書式に従った、Player の Entry 名です。", $"この一覧は {service} 用です。選択は今回だけに適用されます。"],
            bodyLineSpacing: 30);
        quick.CancelButton.Draw(mousePoint, _stationeryDrawingContext);
        quick.SelectButton.Draw(mousePoint, _stationeryDrawingContext);
        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var bounds = QuickClientIdentitySelectionItemBounds(index);
            DrawDataRowFrame(bounds, active: index == session.QuickClientIdentitySelectionIndex, hovered: bounds.Contains(mousePoint));
            DrawFittedText(target.LoginName, new Rectangle(bounds.X + 18, bounds.Y + 9, 420, 28), Color.White, 0.42f);
            DrawFittedText(target.DisplayName, new Rectangle(bounds.X + 18, bounds.Y + 43, 420, 20), new Color(180, 195, 195), 0.27f);
        }
    }


    private void DrawClientIdentityProfileEditField(GoAppSession session, int index, ClientIdentityProfileEditField field, string label, Point mousePoint, bool isLocalMatch)
    {
        var textBounds = ClientIdentityProfileEditFieldTextBounds(index, field, isLocalMatch);
        var active = session.ActiveClientIdentityProfileEditField == field;
        DrawText(label, new Vector2(552, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        var hovered = textBounds.Contains(mousePoint);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        var text = session.GetClientIdentityProfileEditField(field);
        var displayText = field == ClientIdentityProfileEditField.LoginPass && !active ? new string('*', text.Length) : text;
        if (active)
            DrawTextBoxSelection(text, session.ClientIdentityProfileEditSelectionStart, session.ClientIdentityProfileEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(displayText) ? "-" : displayText, textBounds, Color.White, 0.42f);
        if (active)
            DrawTextBoxCaret(text, session.ClientIdentityProfileEditCaretIndex, textBounds, 0.42f);
        DrawEditableTextEditHint(active, hovered, textBounds);
        if (field == ClientIdentityProfileEditField.LoginName && ClientIdentityFieldHoverBounds(textBounds).Contains(mousePoint))
            DrawClientIdentityHandleStickyNote(textBounds);
    }

    private void DrawClientIdentityHandleStickyNote(Rectangle textBounds)
    {
        DrawStickyNote(StickyNoteKind.ClientIdentityHandleHint, PlayerEditUnderlineConnectorStart(textBounds), new Color(185, 196, 255), new Color(116, 145, 178), "HANDLE とは？", ["対局サービスにログインするときの、プレイヤー固有の名前。", "接続する相手の機械に入力できるフォーマットに合わせます。"], bodyLineSpacing: 26);
    }

    /// <summary>未編集のテキスト項目へホバーしたときだけ、共通の EDIT バッジを表示します。</summary>
    private void DrawEditableTextEditHint(bool isEditing, bool isHovered, Rectangle textBounds)
    {
        if (isEditing || !isHovered)
        {
            _editActionBadge.Hide();
            return;
        }

        _editActionBadge.SetAnchorBounds(textBounds);
        _editActionBadge.Show();
        _editActionBadge.Draw(_stationeryDrawingContext);
    }

    private static Vector2 ClientIdentityUnderlineConnectorStart(Rectangle textBounds) =>
        new(textBounds.Right - 24, textBounds.Bottom + 4);

    private static Vector2 PlayerEditUnderlineConnectorStart(Rectangle textBounds) =>
        ClientIdentityUnderlineConnectorStart(textBounds);

    private void DrawPlayerEngineCycleButton(Rectangle bounds, bool pointsRight, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, hovered ? new Color(53, 66, 75) : new Color(31, 40, 47));
        DrawRect(bounds, 2, new Color(105, 127, 134));

        var centerX = bounds.Center.X;
        var centerY = bounds.Center.Y;
        var size = 14;
        for (var offset = -size; offset <= size; offset++)
        {
            var distanceFromTip = pointsRight ? size - offset : size + offset;
            var halfHeight = distanceFromTip * size / (size * 2);
            var x = centerX + offset;
            FillRect(
                new Rectangle(x, centerY - halfHeight, 1, halfHeight * 2 + 1),
                new Color(220, 234, 230));
        }
    }

    private KfwStationeryDrawingTools _stationeryDrawingContext => _drawingContext;
    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);
    private void DrawRoundedFill(Rectangle bounds, int radius, Color color) => _drawingContext.FillRoundedRectangle(bounds, radius, color);
    private void DrawTextBoxSelection(string text, int start, int length, Rectangle bounds, float scale) => _drawingContext.DrawTextSelection(text, start, length, bounds, scale);
    private void DrawTextBoxCaret(string text, int caret, Rectangle bounds, float scale) => _drawingContext.DrawTextCaret(text, caret, bounds, scale);
    private void DrawDataRowFrame(Rectangle bounds, bool active = false, bool hovered = false) => _drawingContext.DrawDataRowFrame(bounds, active, hovered);
    private void DrawDynamicOptionText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawDynamicText(text, bounds, color, scale);
    private void DrawSelectionFingerMark(Vector2 origin, float scale) => _drawingContext.DrawSelectionFinger(origin, scale);
    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color borderColor,
        string heading, System.Collections.Generic.IReadOnlyList<string> bodyLines, int bodyLineSpacing = 40,
        Rectangle? anchorBounds = null) =>
        _drawingContext.DrawStickyNote(kind, connectorStart, accent, borderColor, heading, bodyLines, bodyLineSpacing, anchorBounds);
}
