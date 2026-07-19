namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Local.Resting.TournamentRule;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

public sealed partial class GoScreenRenderer
{

    public void DrawCgosClientTop(GoAppSession session, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        DrawCgosClientTopPanel(session, mousePoint);
        DrawGtpEngineSelectionDialog(session, mousePoint);
        DrawGtpEngineEditPanel(session, mousePoint);

        _spriteBatch.End();
    }


    public int GetCgosConnectionEditPanelCaretIndex(Point point, CgosConnectionProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, CgosConnectionEditPanelFieldTextBounds(field), 0.42f);


    public static bool GetCgosUseButtonHit(Point point) => CgosUseButtonBounds.Contains(point);


    public static bool GetCgosBackButtonHit(Point point) => CgosBackButtonBounds.Contains(point);


    public static bool GetCgosUseSelectedProfileButtonHit(Point point, bool enabled) =>
        enabled && CgosUseSelectedProfileButtonBounds.Contains(point);


    public static bool GetCgosAdminButtonHit(Point point, bool enabled) =>
        enabled && CgosAdminButtonBounds.Contains(point);


    public static bool GetCgosAdminWhoButtonHit(Point point, bool enabled) =>
        enabled && CgosAdminWhoButtonBounds.Contains(point);


    public static bool GetCgosAdminWhitePlayerPreviousButtonHit(Point point) => CgosAdminWhitePlayerPreviousButtonBounds.Contains(point);


    public static bool GetCgosAdminWhitePlayerNextButtonHit(Point point) => CgosAdminWhitePlayerNextButtonBounds.Contains(point);


    public static bool GetCgosAdminBlackPlayerPreviousButtonHit(Point point) => CgosAdminBlackPlayerPreviousButtonBounds.Contains(point);


    public static bool GetCgosAdminBlackPlayerNextButtonHit(Point point) => CgosAdminBlackPlayerNextButtonBounds.Contains(point);


    public static bool GetCgosAdminMatchButtonHit(Point point, bool enabled) => enabled && CgosAdminMatchButtonBounds.Contains(point);


    public static bool GetCgosAdminSwapButtonHit(Point point, bool enabled) => enabled && CgosAdminSwapButtonBounds.Contains(point);

    /// <summary>
    /// ［Admin ＞ CODE］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosAdminCodeButtonHit(Point point, bool enabled) =>
        enabled && CgosAdminCodeButtonBounds.Contains(point);

    /// <summary>
    /// ［Admin ＞ TAIL］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosAdminTailButtonHit(Point point, bool enabled) =>
        enabled && CgosAdminTailButtonBounds.Contains(point);


    public static bool GetCgosBlackConnectionButtonHit(Point point, bool enabled) =>
        enabled && CgosBlackConnectionButtonBounds.Contains(point);


    public static bool GetCgosWhiteConnectionButtonHit(Point point, bool enabled) =>
        enabled && CgosWhiteConnectionButtonBounds.Contains(point);

    /// <summary>
    /// ［プレイヤー１　＞　CODE］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosPlayer1CodeButtonHit(Point point, bool enabled) =>
        enabled && CgosPlayer1CodeButtonBounds.Contains(point);

    /// <summary>
    /// ［プレイヤー１　＞　TAIL］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosPlayer1TailButtonHit(Point point, bool enabled) =>
        enabled && CgosBlackTailButtonBounds.Contains(point);

    /// <summary>
    /// ［プレイヤー２　＞　CODE］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosPlayer2CodeButtonHit(Point point, bool enabled) =>
        enabled && CgosWhiteCodeButtonBounds.Contains(point);

    /// <summary>
    /// ［プレイヤー２　＞　TAIL］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>
    public static bool GetCgosPlayer2TailButtonHit(Point point, bool enabled) =>
        enabled && CgosWhiteTailButtonBounds.Contains(point);


    public static bool GetCgosConnectionStartBackButtonHit(Point point) =>
        CgosConnectionStartBackButtonBounds.Contains(point);


    public static bool GetCgosConnectionBeginButtonHit(Point point, bool enabled) =>
        enabled && CgosConnectionBeginButtonBounds.Contains(point);


    public static GoStone? GetCgosConnectionEngineSelectButtonHit(Point point, GoAppSession session)
    {
        if (!session.IsCgosBlackConnectionRunning && CgosBlackEngineSelector.ContainsBrowseButton(point)) return GoStone.Black;
        return !session.IsCgosWhiteConnectionRunning && CgosWhiteEngineSelector.ContainsBrowseButton(point) ? GoStone.White : null;
    }


    public static bool GetCgosConnectionOpenLogCodeButtonHit(Point point) =>
        CgosConnectionOpenLogCodeButtonBounds.Contains(point);


    public static bool GetCgosConnectionOpenLogNotepadButtonHit(Point point) =>
        CgosConnectionOpenLogNotepadButtonBounds.Contains(point);


    public static bool GetCgosConnectionOpenStandardErrorLogCodeButtonHit(Point point) =>
        CgosConnectionOpenStandardErrorLogCodeButtonBounds.Contains(point);


    public static bool GetCgosConnectionOpenStandardErrorLogNotepadButtonHit(Point point) =>
        CgosConnectionOpenStandardErrorLogNotepadButtonBounds.Contains(point);


    public static bool GetCgosPreviousPageButtonHit(Point point) => CgosPreviousPageButtonBounds.Contains(point);


    public static bool GetCgosNextPageButtonHit(Point point) => CgosNextPageButtonBounds.Contains(point);


    public static bool GetCgosAddButtonHit(Point point) => CgosAddButtonBounds.Contains(point);


    public static bool GetCgosEditButtonHit(Point point) => CgosEditButtonBounds.Contains(point);


    public static bool GetCgosDuplicateButtonHit(Point point) => CgosDuplicateButtonBounds.Contains(point);


    public static bool GetCgosDeleteButtonHit(Point point, bool enabled) =>
        enabled && CgosDeleteButtonBounds.Contains(point);


    public static bool GetCgosConnectionEditPanelCloseButtonHit(Point point) =>
        CgosConnectionEditPanelCloseButtonBounds.Contains(point);


    public static bool GetCgosConnectionEditPanelSaveButtonHit(Point point) =>
        CgosConnectionEditPanelSaveButtonBounds.Contains(point);


    public static CgosConnectionProfileEditField? GetCgosConnectionEditPanelFieldHit(Point point)
    {
        foreach (var field in CgosConnectionEditFields)
        {
            if (CgosConnectionEditPanelFieldRowBounds(field).Contains(point))
            {
                return field;
            }
        }

        return null;
    }


    public static int? GetCgosConnectionProfileHit(Point point, GoAppSession session)
    {
        var visibleSlot = 0;
        foreach (var index in session.GetVisibleCgosConnectionProfileIndexes())
        {
            if (CgosConnectionProfileBounds(visibleSlot).Contains(point))
            {
                return index;
            }

            visibleSlot++;
        }

        return null;
    }


    private void DrawUseSelectionPanel(Point mousePoint)
    {
        var panel = new Rectangle(420, 172, 1080, 736);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText("KIFUWARABE GO 2026", new Vector2(panel.X + 58, panel.Y + 58), new Color(244, 238, 218), 1.05f);
        DrawText("SELECT USE", new Vector2(panel.X + 62, panel.Y + 142), new Color(180, 195, 195), 0.54f);

        DrawUseChoice(LocalUseButtonBounds, "Local (推奨)", "PLAY / REVIEW", cgosClient: false, mousePoint);
        DrawUseChoice(CgosUseButtonBounds, "Connect To CGOS", "WATCH / CONNECT", cgosClient: true, mousePoint);
    }


    private void DrawUseChoice(Rectangle bounds, string title, string caption, bool cgosClient, Point mousePoint)
    {
        var hovered = bounds.Contains(mousePoint);
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 95));
        FillRect(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        DrawRect(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(88, 102, 112));
        FillRect(new Rectangle(bounds.X, bounds.Y, 6, bounds.Height), hovered ? new Color(99, 223, 185) : new Color(58, 78, 86));
        DrawText(title, new Vector2(bounds.X + 42, bounds.Y + 34), Color.White, 0.66f);

        var iconBounds = new Rectangle(bounds.X + 50, bounds.Y + 106, 300, 150);
        if (cgosClient)
        {
            DrawCgosConnectedBox(iconBounds);
        }
        else
        {
            DrawLocalClosedBox(iconBounds);
        }

        DrawFittedText(caption, new Rectangle(bounds.X + 42, bounds.Y + 254, bounds.Width - 84, 44), new Color(204, 241, 226), 0.52f);
    }


    private void DrawCgosConnectedBox(Rectangle bounds)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 70));
        FillRect(bounds, new Color(17, 24, 29));
        DrawCgosBoxFrame(bounds);
        DrawMiniBoardGrid(new Rectangle(bounds.X + 22, bounds.Y + 44, bounds.Width - 44, bounds.Height - 64), new Color(88, 102, 112, 85));

        var localStone = new Vector2(bounds.X + 150, bounds.Y + 92);
        var exit = new Vector2(bounds.X + 150, bounds.Y);
        var server = new Vector2(bounds.X + 252, bounds.Y - 18);

        DrawLine(localStone, exit, 5, new Color(99, 223, 185));
        DrawLine(exit, server, 5, new Color(99, 223, 185));
        DrawIconStone(localStone, 24, black: true);
        DrawIconStone(server, 18, black: false);
    }


    private void DrawCgosBoxFrame(Rectangle bounds)
    {
        var color = new Color(126, 150, 164);
        var gapLeft = bounds.X + 136;
        var gapRight = bounds.X + 164;
        FillRect(new Rectangle(bounds.X, bounds.Y, gapLeft - bounds.X, 4), color);
        FillRect(new Rectangle(gapRight, bounds.Y, bounds.Right - gapRight, 4), color);
        FillRect(new Rectangle(bounds.X, bounds.Bottom - 4, bounds.Width, 4), color);
        FillRect(new Rectangle(bounds.X, bounds.Y, 4, bounds.Height), color);
        FillRect(new Rectangle(bounds.Right - 4, bounds.Y, 4, bounds.Height), color);
    }


    private void DrawCgosClientTopPanel(GoAppSession session, Point mousePoint)
    {
        var panel = new Rectangle(420, 172, 1080, 736);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText("CGOS CLIENT", new Vector2(panel.X + 58, panel.Y + 58), new Color(255, 230, 160), 1.0f);

        if (session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
        {
            DrawCgosConnectionStartPanel(session, mousePoint);
            return;
        }

        DrawText("CONNECTION PROFILE", new Vector2(panel.X + 62, panel.Y + 112), new Color(180, 195, 195), 0.54f);

        DrawText("LIST", new Vector2(CgosConnectionListBounds.X, CgosConnectionListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        FillRect(CgosConnectionListBounds, new Color(15, 20, 26));
        DrawRect(CgosConnectionListBounds, 1, new Color(67, 84, 92));
        var visibleSlot = 0;
        foreach (var index in session.GetVisibleCgosConnectionProfileIndexes())
        {
            DrawCgosConnectionProfileItem(CgosConnectionProfileBounds(visibleSlot), session, index, mousePoint);
            visibleSlot++;
        }

        DrawText("PROPERTIES", new Vector2(CgosConnectionPropertyBounds.X, CgosConnectionPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawCgosConnectionProperties(session);
        DrawCommandButton(CgosPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.CanMoveCgosConnectionSelectionPage(-1), scale: 0.42f);
        DrawCommandButton(CgosNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.CanMoveCgosConnectionSelectionPage(1), scale: 0.42f);
        DrawCommandButton(CgosAddButtonBounds, "ADD", false, mousePoint, scale: 0.38f);
        DrawCommandButton(CgosEditButtonBounds, "EDIT", false, mousePoint, enabled: session.CgosConnectionProfiles.Count > 0, scale: 0.38f);
        DrawCommandButton(CgosDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.CgosConnectionProfiles.Count > 0, scale: 0.25f);
        DrawCommandButton(CgosDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedCgosConnectionProfile, scale: 0.34f);
        DrawCommandButton(CgosUseSelectedProfileButtonBounds, "USE", false, mousePoint, enabled: session.CgosConnectionProfiles.Count > 0, scale: 0.42f);
        DrawCommandButton(CgosBackButtonBounds, "BACK", false, mousePoint, scale: 0.42f);
        DrawCgosConnectionEditPanel(session, mousePoint);
    }


    private void DrawCgosConnectionStartPanel(GoAppSession session, Point mousePoint)
    {
        var profile = session.SelectedCgosConnectionProfile;
        DrawText("USE CONNECTION", new Vector2(482, 300), new Color(180, 195, 195), 0.54f);
        DrawCommandButton(
            CgosConnectionStartBackButtonBounds,
            session.IsAnyCgosProcessRunning ? "DISCONNECT ALL & BACK" : "BACK",
            false,
            mousePoint,
            scale: session.IsAnyCgosProcessRunning ? 0.25f : 0.42f);

        DrawCgosSelectedProfileBar(profile);
        DrawCgosProcessPanel(
            CgosAdminProcessPanelBounds,
            "ADMIN CONNECTION",
            session.CgosAdminStatusMessage,
            session.CgosAdminRecentOutput,
            null,
            null,
            null,
            CgosAdminButtonBounds,
            session.IsCgosAdminRunning ? "STOP" : "START",
            true,
            CgosAdminTailButtonBounds,
            CgosAdminCodeButtonBounds,
            !string.IsNullOrWhiteSpace(session.CgosAdminLogDirectory),
            mousePoint);
        DrawCommandButton(CgosAdminWhoButtonBounds, "WHO", false, mousePoint, enabled: session.IsCgosAdminRunning, scale: 0.28f);
        DrawCgosAdminPlayerSelector(CgosAdminWhitePlayerRowBounds, "WHITE", session.CgosAdminWhitePlayerName, CgosAdminWhitePlayerPreviousButtonBounds, CgosAdminWhitePlayerNextButtonBounds, mousePoint);
        DrawCgosAdminPlayerSelector(CgosAdminBlackPlayerRowBounds, "BLACK", session.CgosAdminBlackPlayerName, CgosAdminBlackPlayerPreviousButtonBounds, CgosAdminBlackPlayerNextButtonBounds, mousePoint);
        DrawCommandButton(CgosAdminMatchButtonBounds, "MATCH", false, mousePoint, enabled: session.CanSendCgosAdminMatch, scale: 0.22f);
        DrawCommandButton(CgosAdminSwapButtonBounds, "SWAP", false, mousePoint, enabled: session.CanSendCgosAdminMatch, scale: 0.22f);

        DrawCgosProcessPanel(
            CgosBlackProcessPanelBounds,
            "PLAYER 1",
            session.CgosBlackConnectionStatusMessage,
            session.CgosBlackConnectionRecentOutput,
            session.SelectedCgosBlackGtpEngineProfile?.DisplayName,
            CgosBlackEngineSelector with { Enabled = !session.IsCgosBlackConnectionRunning },
            session.CgosBlackConnectionElapsedDisplay,
            CgosBlackConnectionButtonBounds,
            session.IsCgosBlackConnectionRunning ? "STOP" : "START",
            session.IsCgosBlackConnectionRunning || session.SelectedCgosBlackGtpEngineProfile is not null,
            CgosBlackTailButtonBounds,
            CgosPlayer1CodeButtonBounds,
            !string.IsNullOrWhiteSpace(session.CgosBlackConnectionLogDirectory),
            mousePoint);

        DrawCgosProcessPanel(
            CgosWhiteProcessPanelBounds,
            "PLAYER 2",
            session.CgosWhiteConnectionStatusMessage,
            session.CgosWhiteConnectionRecentOutput,
            session.SelectedCgosWhiteGtpEngineProfile?.DisplayName,
            CgosWhiteEngineSelector with { Enabled = !session.IsCgosWhiteConnectionRunning },
            session.CgosWhiteConnectionElapsedDisplay,
            CgosWhiteConnectionButtonBounds,
            session.IsCgosWhiteConnectionRunning ? "STOP" : "START",
            session.IsCgosWhiteConnectionRunning || session.SelectedCgosWhiteGtpEngineProfile is not null,
            CgosWhiteTailButtonBounds,
            CgosWhiteCodeButtonBounds,
            !string.IsNullOrWhiteSpace(session.CgosWhiteConnectionLogDirectory),
            mousePoint);

        DrawCgosConnectionTooltips(session, mousePoint);
    }


    private void DrawCgosSelectedProfileBar(CgosConnectionProfile profile)
    {
        FillRect(CgosSelectedProfileBarBounds, new Color(15, 20, 26));
        DrawRect(CgosSelectedProfileBarBounds, 1, new Color(67, 84, 92));
        DrawUiLabel(UiLabel.InCompactRow("TARGET", CgosSelectedProfileBarBounds));
        var text = $"{profile.DisplayName} / {profile.Host}:{profile.Port} / {profile.Event} / {profile.Role}";
        DrawFittedText(text, new Rectangle(CgosSelectedProfileBarBounds.X + 152, CgosSelectedProfileBarBounds.Y + 7, CgosSelectedProfileBarBounds.Width - 168, 38), Color.White, 0.42f);
    }


    private void DrawCgosAdminPlayerSelector(Rectangle bounds, string label, string playerName, Rectangle previousBounds, Rectangle nextBounds, Point mousePoint)
    {
        FillRect(bounds, new Color(11, 15, 20));
        DrawRect(bounds, 1, new Color(67, 84, 92));
        DrawText(label, new Vector2(bounds.X + 6, bounds.Y + 9), new Color(180, 195, 195), 0.22f);
        DrawFittedText(playerName, new Rectangle(bounds.X + 62, bounds.Y + 5, bounds.Width - 126, bounds.Height - 10), Color.White, 0.25f);
        DrawCommandButton(previousBounds, "<", false, mousePoint, enabled: true, scale: 0.22f);
        DrawCommandButton(nextBounds, ">", false, mousePoint, enabled: true, scale: 0.22f);
    }

    /// <summary>
    /// ［CGOSプロセス・パネル］の描画
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="title"></param>
    /// <param name="status"></param>
    /// <param name="output"></param>
    /// <param name="engineName"></param>
    /// <param name="elapsedDisplay"></param>
    /// <param name="startButtonBounds"></param>
    /// <param name="startLabel"></param>
    /// <param name="startEnabled"></param>
    /// <param name="tailButtonBounds"></param>
    /// <param name="codeButtonBounds"></param>
    /// <param name="logToolsEnabled"></param>
    /// <param name="mousePoint"></param>
    private void DrawCgosProcessPanel(
        Rectangle bounds,
        string title,
        string status,
        IReadOnlyList<string> output,
        string? engineName,
        LabeledBrowseSelector? engineSelector,
        string? elapsedDisplay,
        Rectangle startButtonBounds,
        string startLabel,
        bool startEnabled,
        Rectangle tailButtonBounds,
        Rectangle codeButtonBounds,
        bool logToolsEnabled,
        Point mousePoint)
    {
        FillRect(bounds, new Color(15, 20, 26));
        DrawRect(bounds, 1, new Color(67, 84, 92));
        DrawText(title, new Vector2(bounds.X + 18, bounds.Y + 18), new Color(255, 230, 160), 0.42f);
        if (!string.IsNullOrEmpty(elapsedDisplay))
        {
            DrawFittedText(elapsedDisplay, new Rectangle(bounds.X + 158, bounds.Y + 14, bounds.Width - 174, 32), new Color(146, 220, 255), 0.27f);
        }

        var stateRow = new Rectangle(bounds.X + 16, bounds.Y + 62, bounds.Width - 32, 48);
        DrawDataRowFrame(stateRow);
        DrawUiLabel(UiLabel.InCompactRow("STATE", stateRow));
        DrawFittedText(status, new Rectangle(stateRow.X + 132, stateRow.Y + 7, stateRow.Width - 148, 34), Color.White, 0.34f);

        if (engineSelector is { } selector)
        {
            DrawLabeledBrowseSelector(selector with { Value = engineName ?? "-" }, mousePoint);
        }

        var outputBounds = new Rectangle(bounds.X + 16, bounds.Y + 210, bounds.Width - 32, 118);
        FillRect(outputBounds, new Color(11, 15, 20));
        DrawRect(outputBounds, 1, new Color(67, 84, 92));
        DrawFittedText(output.Count == 0 ? "-" : ShortenForCgosMessageRow(output[^1]), new Rectangle(outputBounds.X + 12, outputBounds.Y + 10, outputBounds.Width - 24, 28), new Color(204, 211, 206), 0.28f);
        if (output.Count > 1)
        {
            DrawFittedText(ShortenForCgosMessageRow(output[^2]), new Rectangle(outputBounds.X + 12, outputBounds.Y + 44, outputBounds.Width - 24, 28), new Color(204, 211, 206), 0.25f);
        }

        DrawCommandButton(startButtonBounds, startLabel, false, mousePoint, enabled: startEnabled, scale: 0.36f);
        DrawCommandButton(tailButtonBounds, "TAIL", false, mousePoint, enabled: logToolsEnabled, scale: 0.26f);
        DrawCommandButton(codeButtonBounds, "CODE", false, mousePoint, enabled: logToolsEnabled, scale: 0.3f);
    }


    private void DrawCgosConnectionLogRows(GoAppSession session, Point mousePoint)
    {
        var logPath = string.IsNullOrWhiteSpace(session.CgosConnectionLogDirectory) ? "Logs/Cgos" : session.CgosConnectionLogDirectory;

        var stdioBounds = new Rectangle(CgosConnectionStartStatusBounds.X + 22, CgosConnectionStartStatusBounds.Y + 256, CgosConnectionStartStatusBounds.Width - 44, 56);
        DrawDataRowFrame(stdioBounds);
        DrawUiLabel(UiLabel.InCompactRow("STDIO", stdioBounds));
        DrawFittedText(logPath, CgosConnectionLogPathBounds, Color.White, 0.32f);
        DrawCommandButton(CgosConnectionOpenLogCodeButtonBounds, "CODE", false, mousePoint, scale: 0.24f);
        DrawCommandButton(CgosConnectionOpenLogNotepadButtonBounds, "NOTEPAD", false, mousePoint, scale: 0.2f);

        var stderrBounds = new Rectangle(CgosConnectionStartStatusBounds.X + 22, CgosConnectionStartStatusBounds.Y + 332, CgosConnectionStartStatusBounds.Width - 44, 56);
        DrawDataRowFrame(stderrBounds);
        DrawUiLabel(UiLabel.InCompactRow("STDERR", stderrBounds));
        DrawFittedText(logPath, CgosConnectionStandardErrorLogPathBounds, Color.White, 0.32f);
        DrawCommandButton(CgosConnectionOpenStandardErrorLogCodeButtonBounds, "CODE", false, mousePoint, scale: 0.24f);
        DrawCommandButton(CgosConnectionOpenStandardErrorLogNotepadButtonBounds, "NOTEPAD", false, mousePoint, scale: 0.2f);
    }


    private void DrawCgosConnectionOutput(GoAppSession session)
    {
        var bounds = CgosConnectionOutputBounds;
        FillRect(bounds, new Color(11, 15, 20));
        DrawRect(bounds, 1, new Color(67, 84, 92));
        DrawText("MESSAGE", new Vector2(bounds.X, bounds.Y - 28), new Color(180, 195, 195), 0.38f);

        var lines = session.CgosConnectionRecentOutput;
        if (lines.Count == 0)
        {
            DrawFittedText("-", new Rectangle(bounds.X + 16, bounds.Y + 10, bounds.Width - 32, 26), new Color(204, 211, 206), 0.34f);
            return;
        }

        var maxVisibleLines = Math.Max(1, (bounds.Height - 20) / 23);
        var firstVisibleLine = Math.Max(0, lines.Count - maxVisibleLines);
        for (var index = firstVisibleLine; index < lines.Count; index++)
        {
            var visibleIndex = index - firstVisibleLine;
            DrawFittedText(ShortenForCgosMessageRow(lines[index]), CgosConnectionOutputLineBounds(visibleIndex), new Color(204, 211, 206), 0.31f);
        }
    }


    private void DrawCgosConnectionTooltips(GoAppSession session, Point mousePoint)
    {
        if (CgosBlackEngineSelector.ValueBounds.Contains(mousePoint) && session.SelectedCgosBlackGtpEngineProfile is { } blackProfile)
        {
            DrawCgosTextTooltip(
                CgosConnectionEngineTooltipBounds,
                "BLACK ENGINE COMMAND",
                FormatCgosEngineCommand(blackProfile));
            return;
        }

        if (CgosWhiteEngineSelector.ValueBounds.Contains(mousePoint) && session.SelectedCgosWhiteGtpEngineProfile is { } whiteProfile)
        {
            DrawCgosTextTooltip(
                CgosConnectionEngineTooltipBounds,
                "WHITE ENGINE COMMAND",
                FormatCgosEngineCommand(whiteProfile));
            return;
        }

        if (TryGetHoveredCgosMessageLine(session, mousePoint, out var message))
        {
            DrawCgosTextTooltip(CgosConnectionMessageTooltipBounds, "MESSAGE", message);
        }
    }


    private void DrawCgosConnectionStartRow(Rectangle panelBounds, int y, string label, string value)
    {
        var bounds = new Rectangle(panelBounds.X + 22, y, panelBounds.Width - 44, 56);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }


    private static bool TryGetHoveredCgosMessageLine(GoAppSession session, Point mousePoint, out string message)
    {
        var lines = session.CgosConnectionRecentOutput;
        var maxVisibleLines = Math.Max(1, (CgosConnectionOutputBounds.Height - 20) / 23);
        var firstVisibleLine = Math.Max(0, lines.Count - maxVisibleLines);
        for (var index = firstVisibleLine; index < lines.Count; index++)
        {
            var visibleIndex = index - firstVisibleLine;
            if (CgosConnectionOutputLineBounds(visibleIndex).Contains(mousePoint))
            {
                message = lines[index];
                return true;
            }
        }

        message = "";
        return false;
    }


    private void DrawCgosTextTooltip(Rectangle bounds, string title, string text)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(30, 36, 43, 252));
        DrawRect(bounds, 2, new Color(147, 244, 200));
        DrawText(title, new Vector2(bounds.X + 18, bounds.Y + 12), new Color(180, 195, 195), 0.34f);
        DrawFittedText(text, new Rectangle(bounds.X + 18, bounds.Y + 42, bounds.Width - 36, bounds.Height - 54), Color.White, 0.42f);
    }


    private static string ShortenForCgosMessageRow(string text)
    {
        const int maxLength = 92;
        var trimmed = text.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..(maxLength - 3)] + "...";
    }


    private static string FormatCgosEngineCommand(GtpEngineProfile profile)
    {
        var executable = string.IsNullOrWhiteSpace(profile.ExecutablePath)
            ? "-"
            : profile.ExecutablePath.Trim();
        if (string.IsNullOrWhiteSpace(profile.Arguments))
        {
            return executable;
        }

        return $"{executable} {profile.Arguments.Trim()}";
    }


    private void DrawCgosConnectionEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsCgosConnectionEditPanelOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(CgosConnectionEditPanelBounds.X + 14, CgosConnectionEditPanelBounds.Y + 16, CgosConnectionEditPanelBounds.Width, CgosConnectionEditPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(CgosConnectionEditPanelBounds, new Color(19, 24, 31, 250));
        DrawRect(CgosConnectionEditPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsCgosConnectionAddPanelMode ? "ADD CGOS PROFILE" : "EDIT CGOS PROFILE", new Vector2(CgosConnectionEditPanelBounds.X + 28, CgosConnectionEditPanelBounds.Y + 24), new Color(244, 238, 218), 0.68f);
        DrawCommandButton(CgosConnectionEditPanelCloseButtonBounds, "BACK", false, mousePoint, scale: 0.42f);

        FillRect(CgosConnectionEditPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(CgosConnectionEditPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.DisplayName, "DISPLAY", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Host, "HOST", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Port, "PORT", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Event, "EVENT", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Role, "ROLE", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Note, "NOTE", mousePoint);

        if (!string.IsNullOrWhiteSpace(session.CgosConnectionEditWarning))
        {
            DrawFittedText(session.CgosConnectionEditWarning, new Rectangle(CgosConnectionEditPanelEditorBounds.X + 40, CgosConnectionEditPanelEditorBounds.Bottom - 70, CgosConnectionEditPanelEditorBounds.Width - 80, 34), new Color(255, 183, 146), 0.38f);
        }

        DrawCommandButton(CgosConnectionEditPanelSaveButtonBounds, SaveCgosConnectionLabel(session), false, mousePoint, scale: 0.46f);
    }


    private void DrawCgosConnectionEditField(GoAppSession session, CgosConnectionProfileEditField field, string label, Point mousePoint)
    {
        var bounds = CgosConnectionEditPanelFieldRowBounds(field);
        var active = session.ActiveCgosConnectionEditField == field;
        var text = session.GetCgosConnectionEditFieldText(field);
        DrawDataRowFrame(bounds, active, bounds.Contains(mousePoint));
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));

        var textBounds = CgosConnectionEditPanelFieldTextBounds(field);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
        {
            DrawTextBoxCaret(text, session.CgosConnectionEditCaretIndex, textBounds, 0.42f);
        }
    }


    private void DrawCgosConnectionProfileItem(Rectangle bounds, GoAppSession session, int index, Point mousePoint)
    {
        var profile = session.CgosConnectionProfiles[index];
        var selected = index == session.SelectedCgosConnectionProfileIndex;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
        DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 18), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        DrawFittedText(profile.DisplayName, new Rectangle(bounds.X + 62, bounds.Y + 8, bounds.Width - 82, 34), Color.White, 0.52f);
        DrawText($"{profile.Host}:{profile.Port}", new Vector2(bounds.X + 62, bounds.Y + 48), new Color(204, 211, 206), 0.34f);
    }


    private void DrawCgosConnectionProperties(GoAppSession session)
    {
        FillRect(CgosConnectionPropertyBounds, new Color(15, 20, 26));
        DrawRect(CgosConnectionPropertyBounds, 1, new Color(67, 84, 92));

        var profile = session.SelectedCgosConnectionProfile;
        var y = CgosConnectionPropertyBounds.Y + 22;
        DrawCgosConnectionPropertyRow(y, "NAME", profile.DisplayName);
        DrawCgosConnectionPropertyRow(y + 70, "HOST", profile.Host);
        DrawCgosConnectionPropertyRow(y + 140, "PORT", profile.Port.ToString());
        DrawCgosConnectionPropertyRow(y + 210, "EVENT", profile.Event);
        DrawCgosConnectionPropertyRow(y + 280, "ROLE", profile.Role);
        DrawCgosConnectionPropertyRow(y + 350, "NOTE", profile.Note);
    }


    private void DrawCgosConnectionPropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(CgosConnectionPropertyBounds.X + 18, y, CgosConnectionPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }


    private static Rectangle CgosUseButtonBounds => new(974, 404, 438, 300);


    private static Rectangle CgosBackButtonBounds => new(1348, 204, 110, 48);


    private static Rectangle CgosUseSelectedProfileButtonBounds => new(1224, 278, 212, 54);


    private static Rectangle CgosAdminButtonBounds => new(CgosAdminProcessPanelBounds.X + 18, CgosAdminProcessPanelBounds.Bottom - 70, 108, 48);


    private static Rectangle CgosAdminWhoButtonBounds => new(CgosAdminProcessPanelBounds.X + 18, CgosAdminProcessPanelBounds.Bottom - 126, 68, 40);


    private static Rectangle CgosAdminMatchButtonBounds => new(CgosAdminProcessPanelBounds.X + 96, CgosAdminProcessPanelBounds.Bottom - 126, 82, 40);


    private static Rectangle CgosAdminSwapButtonBounds => new(CgosAdminProcessPanelBounds.X + 188, CgosAdminProcessPanelBounds.Bottom - 126, 82, 40);


    private static Rectangle CgosAdminWhitePlayerRowBounds => new(CgosAdminProcessPanelBounds.X + 16, CgosAdminProcessPanelBounds.Y + 116, CgosAdminProcessPanelBounds.Width - 32, 38);


    private static Rectangle CgosAdminBlackPlayerRowBounds => new(CgosAdminProcessPanelBounds.X + 16, CgosAdminProcessPanelBounds.Y + 160, CgosAdminProcessPanelBounds.Width - 32, 38);


    private static Rectangle CgosAdminWhitePlayerPreviousButtonBounds => new(CgosAdminWhitePlayerRowBounds.Right - 62, CgosAdminWhitePlayerRowBounds.Y + 3, 27, 32);


    private static Rectangle CgosAdminWhitePlayerNextButtonBounds => new(CgosAdminWhitePlayerRowBounds.Right - 31, CgosAdminWhitePlayerRowBounds.Y + 3, 27, 32);


    private static Rectangle CgosAdminBlackPlayerPreviousButtonBounds => new(CgosAdminBlackPlayerRowBounds.Right - 62, CgosAdminBlackPlayerRowBounds.Y + 3, 27, 32);


    private static Rectangle CgosAdminBlackPlayerNextButtonBounds => new(CgosAdminBlackPlayerRowBounds.Right - 31, CgosAdminBlackPlayerRowBounds.Y + 3, 27, 32);


    private static Rectangle CgosAdminTailButtonBounds => new(CgosAdminProcessPanelBounds.X + 136, CgosAdminProcessPanelBounds.Bottom - 70, 68, 48);


    private static Rectangle CgosAdminCodeButtonBounds => new(CgosAdminProcessPanelBounds.Right - 90, CgosAdminProcessPanelBounds.Bottom - 70, 72, 48);


    private static Rectangle CgosConnectionStartBackButtonBounds => new(1134, 244, 324, 48);


    private static Rectangle CgosBlackConnectionButtonBounds => new(CgosBlackProcessPanelBounds.X + 18, CgosBlackProcessPanelBounds.Bottom - 70, 120, 48);


    private static Rectangle CgosBlackTailButtonBounds => new(CgosBlackProcessPanelBounds.X + 146, CgosBlackProcessPanelBounds.Bottom - 70, 58, 48);


    private static Rectangle CgosPlayer1CodeButtonBounds => new(CgosBlackProcessPanelBounds.Right - 90, CgosBlackProcessPanelBounds.Bottom - 70, 72, 48);


    private static Rectangle CgosWhiteConnectionButtonBounds => new(CgosWhiteProcessPanelBounds.X + 18, CgosWhiteProcessPanelBounds.Bottom - 70, 120, 48);


    private static Rectangle CgosWhiteTailButtonBounds => new(CgosWhiteProcessPanelBounds.X + 146, CgosWhiteProcessPanelBounds.Bottom - 70, 58, 48);


    private static Rectangle CgosWhiteCodeButtonBounds => new(CgosWhiteProcessPanelBounds.Right - 90, CgosWhiteProcessPanelBounds.Bottom - 70, 72, 48);


    private static Rectangle CgosConnectionBeginButtonBounds => new(1134, 800, 302, 58);


    private static Rectangle CgosConnectionLogPathBounds => new(1102, 608, 178, 38);


    private static Rectangle CgosConnectionOpenLogCodeButtonBounds => new(1286, 610, 60, 32);


    private static Rectangle CgosConnectionOpenLogNotepadButtonBounds => new(1352, 610, 72, 32);


    private static Rectangle CgosConnectionStandardErrorLogPathBounds => new(1102, 684, 178, 38);


    private static Rectangle CgosConnectionOpenStandardErrorLogCodeButtonBounds => new(1286, 686, 60, 32);


    private static Rectangle CgosConnectionOpenStandardErrorLogNotepadButtonBounds => new(1352, 686, 72, 32);


    private static Rectangle CgosPreviousPageButtonBounds => new(482, 800, 130, 52);


    private static Rectangle CgosNextPageButtonBounds => new(772, 800, 130, 52);


    private static Rectangle CgosAddButtonBounds => new(936, 800, 120, 52);


    private static Rectangle CgosEditButtonBounds => new(1062, 800, 120, 52);


    private static Rectangle CgosDuplicateButtonBounds => new(1188, 800, 120, 52);


    private static Rectangle CgosDeleteButtonBounds => new(1316, 800, 120, 52);


    private static Rectangle CgosConnectionListBounds => new(482, 350, 420, 332);


    private static Rectangle CgosConnectionPropertyBounds => new(936, 350, 500, 426);


    private static Rectangle CgosSelectedProfileBarBounds => new(482, 358, 954, 56);


    private static Rectangle CgosAdminProcessPanelBounds => new(482, 452, 302, 356);


    private static Rectangle CgosBlackProcessPanelBounds => new(808, 452, 302, 356);


    private static Rectangle CgosWhiteProcessPanelBounds => new(1134, 452, 302, 356);


    private static LabeledBrowseSelector CgosBlackEngineSelector => new(
        new Rectangle(CgosBlackProcessPanelBounds.X + 16, CgosBlackProcessPanelBounds.Y + 128, CgosBlackProcessPanelBounds.Width - 32, 48),
        "ENGINE",
        "",
        "SELECT",
        58,
        88);


    private static LabeledBrowseSelector CgosWhiteEngineSelector => new(
        new Rectangle(CgosWhiteProcessPanelBounds.X + 16, CgosWhiteProcessPanelBounds.Y + 128, CgosWhiteProcessPanelBounds.Width - 32, 48),
        "ENGINE",
        "",
        "SELECT",
        58,
        88);


    private static Rectangle CgosConnectionStartTargetBounds => new(482, 350, 420, 426);


    private static Rectangle CgosConnectionStartStatusBounds => new(936, 350, 500, 426);


    private static Rectangle CgosConnectionOutputBounds => new(482, 800, 620, 108);


    private static Rectangle CgosConnectionOutputLineBounds(int index) =>
        new(CgosConnectionOutputBounds.X + 16, CgosConnectionOutputBounds.Y + 10 + index * 23, CgosConnectionOutputBounds.Width - 32, 21);


    private static Rectangle CgosConnectionEngineTooltipBounds => new(500, 594, 1040, 100);


    private static Rectangle CgosConnectionMessageTooltipBounds => new(500, 674, 1040, 106);


    private static Rectangle CgosConnectionEditPanelBounds => new(430, 126, 1060, 820);


    private static Rectangle CgosConnectionEditPanelEditorBounds => new(520, 228, 880, 590);


    private static Rectangle CgosConnectionEditPanelCloseButtonBounds => new(1318, 156, 132, 48);


    private static Rectangle CgosConnectionEditPanelSaveButtonBounds => new(1080, 840, 320, 58);


    private static readonly CgosConnectionProfileEditField[] CgosConnectionEditFields =
    {
        CgosConnectionProfileEditField.DisplayName,
        CgosConnectionProfileEditField.Host,
        CgosConnectionProfileEditField.Port,
        CgosConnectionProfileEditField.Event,
        CgosConnectionProfileEditField.Role,
        CgosConnectionProfileEditField.Note,
    };


    private static Rectangle CgosConnectionEditPanelFieldRowBounds(CgosConnectionProfileEditField field) => field switch
    {
        CgosConnectionProfileEditField.DisplayName => new Rectangle(AddPanelControlX, 250, 668, 56),
        CgosConnectionProfileEditField.Host => new Rectangle(AddPanelControlX, 320, 668, 56),
        CgosConnectionProfileEditField.Port => new Rectangle(AddPanelControlX, 390, 668, 56),
        CgosConnectionProfileEditField.Event => new Rectangle(AddPanelControlX, 460, 668, 56),
        CgosConnectionProfileEditField.Role => new Rectangle(AddPanelControlX, 530, 668, 56),
        CgosConnectionProfileEditField.Note => new Rectangle(AddPanelControlX, 600, 668, 56),
        _ => Rectangle.Empty,
    };


    private static Rectangle CgosConnectionEditPanelFieldTextBounds(CgosConnectionProfileEditField field)
    {
        var bounds = CgosConnectionEditPanelFieldRowBounds(field);
        return new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 42);
    }


    private static Rectangle CgosConnectionProfileBounds(int index) =>
        new(CgosConnectionListBounds.X + 16, CgosConnectionListBounds.Y + 16 + index * 104, CgosConnectionListBounds.Width - 32, 86);


    private static string SaveCgosConnectionLabel(GoAppSession session) =>
        string.IsNullOrWhiteSpace(session.CgosConnectionEditSaveMessage)
            ? "SAVE PROFILE"
            : $"SAVE PROFILE {session.CgosConnectionEditSaveMessage}";
}
