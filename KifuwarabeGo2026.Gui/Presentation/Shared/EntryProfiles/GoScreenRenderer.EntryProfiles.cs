namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Shared.Domain;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;
using static KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles.EntryProfilesScreenBounds;

/// <summary>Player 選択欄と選択ダイアログの描画・当たり判定。</summary>
public sealed partial class GoScreenRenderer
{

    public int GetLocalMatchHandleCaretIndex(Point point, GoStone stone, string text, bool isPonnuki) =>
        GetTextBoxCaretIndex(point.X, text, LocalMatchScreen.Default.GetHandleTextBounds(stone, isPonnuki), 0.32f);
    public int GetClientIdentityProfileEditCaretIndex(Point point, int index, ClientIdentityProfileEditField field, string text, bool isLocalMatch) =>
        GetTextBoxCaretIndex(point.X, text, ClientIdentityProfileEditFieldTextBounds(index, field, isLocalMatch), 0.34f);

    internal void DrawSetupPlayerRow(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var player = session.GetSelectedEntryProfile(stone);
        var label = stone == GoStone.Black ? "BLACK PLAYER" : "WHITE PLAYER";
        DrawPlayerSelector(
            PlayerSelectorLayout.CreatePlayerSelector(y) with
            {
                Label = label,
                Value = player?.DisplayName ?? "SELECT PLAYER",
                IsComputer = player is null ? null : player.Kind == EntryProfileKind.Computer,
            },
            mousePoint);
        var isPonnuki = y is PonnukiBlackPlayerKindButtonY or PonnukiWhitePlayerKindButtonY;
        var handleBounds = LocalMatchScreen.Default.GetHandleBounds(stone, isPonnuki);
        DrawFittedText("HANDLE", new Rectangle(1156, handleBounds.Y + 4, 118, 32), UiLabel.TextColor, 0.34f);
        var textBounds = LocalMatchScreen.Default.GetHandleTextBounds(stone, isPonnuki);
        var active = session.ActiveLocalMatchHandleStone == stone;
        var hovered = textBounds.Contains(mousePoint);
        var text = session.GetLocalMatchHandleDraft(stone);
        DrawFittedText(text, textBounds, Color.White, 0.32f);
        DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        if (active)
        {
            DrawTextBoxSelection(text, session.LocalMatchHandleSelectionStart, session.LocalMatchHandleSelectionLength, textBounds, 0.32f);
            DrawTextBoxCaret(text, session.LocalMatchHandleCaretIndex, textBounds, 0.32f);
        }
        if (hovered && !active)
        {
            // 入力欄のアンダーライン終端の近くに、読みやすい反転プレートで表示する。
            var hintBounds = new Rectangle(textBounds.Right - 76, textBounds.Bottom - 25, 70, 23);
            DrawRoundedFill(hintBounds, 6, new Color(185, 196, 255));
            DrawSharpCenteredFittedText("EDIT", hintBounds, new Color(15, 20, 31), 0.34f);
        }
    }

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
        if (!session.IsClientIdentityProfileConnectionSelectionPanelOpen) return;
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
        DrawFittedText("HANDLE", new Rectangle(firstRow.X + 18, firstRow.Y - 27, 410, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("PASSWORD", new Rectangle(firstRow.X + 460, firstRow.Y - 27, 250, 24), new Color(180, 210, 215), 0.30f);
        DrawFittedText("IN DEFAULT", new Rectangle(firstRow.X + 730, firstRow.Y - 27, 240, 24), new Color(180, 210, 215), 0.30f);

        for (var index = 0; index < targets.Count; index++)
        {
            var target = targets[index];
            var row = new Rectangle(bounds.X + 36, bounds.Y + 140 + index * 92, bounds.Width - 72, 78);
            var selected = session.IsClientIdentityProfileDefault(index);
            var operated = index == session.ClientIdentityProfileSelectionIndex;
            FillRect(row, selected ? new Color(38, 103, 86) : row.Contains(mousePoint) ? new Color(43, 52, 62) : new Color(24, 31, 37));
            DrawRect(row, operated ? 3 : selected ? 2 : 1, operated ? new Color(125, 225, 255) : selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
            if (operated) DrawSelectionFingerMark(new Vector2(row.X - 55, row.Center.Y - 13), 1.65f);
            DrawFittedText(target.LoginName, new Rectangle(row.X + 18, row.Y + 25, 410, 30), Color.White, 0.42f);
            DrawFittedText(string.IsNullOrEmpty(target.LoginPass) ? "NONE" : "SET", new Rectangle(row.X + 460, row.Y + 25, 250, 30), Color.White, 0.34f);
            if (selected)
                DrawFittedText("IN DEFAULT", new Rectangle(row.X + 730, row.Y + 25, 240, 30), new Color(147, 244, 200), 0.30f);
        }
    }

    private void DrawClientIdentityProfileConnectionSelectionPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsClientIdentityProfileConnectionSelectionPanelOpen) return;

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
        if (!isEditing && isHovered)
            DrawActionBadge("EDIT", textBounds);
    }

    private static Vector2 ClientIdentityUnderlineConnectorStart(Rectangle textBounds) =>
        new(textBounds.Right - 24, textBounds.Bottom + 4);

    private static Vector2 PlayerEditUnderlineConnectorStart(Rectangle textBounds) =>
        ClientIdentityUnderlineConnectorStart(textBounds);

#if false // Superseded by the independent EditEntryProfile component.
    // Moved to Presentation/Shared/EditEntryProfile/EditEntryProfile.cs.
    // Kept temporarily only as an inactive compatibility stub while this file is split further.
    private void DrawPlayerEditStickyNoteLegacy(GoAppSession session, Point mousePoint)
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
            bodyLines = ["コンピューターとして着手するための GTP エンジン。"];
            connectorStart = PlayerEditUnderlineConnectorStart(engineBounds);
        }

        if (heading is null || bodyLines is null) return;
        DrawStickyNote(
            StickyNoteKind.EntryProfileFieldHint,
            connectorStart,
            new Color(185, 196, 255),
            new Color(116, 145, 178),
            heading,
            bodyLines,
            bodyLineSpacing: 26);
    }

    private static Vector2 PlayerEditUnderlineConnectorStartLegacy(Rectangle textBounds) =>
        // アンダーラインの中心線へ、右端から少し内側で接続する。
        new(textBounds.Right - 24, textBounds.Bottom + 4);

#endif
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

    private enum PlayerEditFieldIconKindLegacy
    {
        None,
        PlayerName,
        Engine,
    }
}
