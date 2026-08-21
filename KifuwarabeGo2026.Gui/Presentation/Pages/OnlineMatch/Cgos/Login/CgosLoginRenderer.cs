namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Shared.CatalogOrder;
using KifuwarabeGo2026.Gui.Presentation.Shared.EntryProfiles;
using KifuwarabeGo2026.Gui.Presentation;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.LinkUnderline;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Shared.Underline;
using KifuwarabeGo2026.Gui.Presentation.Pages.GtpEngine;

using KifuwarabeGo2026.Gui.Presentation.Title;
using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.GoApps.Formal.OnlineMatch.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Shared.Domain;
using KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.StickyNote;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.ActionBadge;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.SelectConnection;
using KifuwarabeGo2026.Gui.Presentation.Shared.RandomSeedRow;

/// <summary>CGOS の接続選択・ログイン画面を描画します。</summary>
public sealed class CgosLoginRenderer
{
    private const int AddPanelControlX = 626;
    private readonly GtpEngineRenderer _gtpEngineRenderer;
    private readonly Action<GoAppSession, Point> _drawPlayerEditPanel;
    private readonly LinkUnderline _compactLinkUnderline = new(
        new RoundUnderline { TopOffset = 1, Thickness = 3, Radius = 1 });
    private readonly LinkUnderline _selectorLinkUnderline = new(
        new RoundUnderline { TopOffset = 2, Thickness = 4, Radius = 2 });
    private readonly ActionBadgeComponent _editActionBadge = ActionBadgeComponent.Create("EDIT", Rectangle.Empty);
    private readonly HashSet<GoStone> _visibleCgosPasswords = [];
    private KfwStationeryDrawingTools _drawingContext = null!;

    public CgosLoginRenderer(GtpEngineRenderer gtpEngineRenderer, Action<GoAppSession, Point> drawPlayerEditPanel)
    {
        _gtpEngineRenderer = gtpEngineRenderer;
        _drawPlayerEditPanel = drawPlayerEditPanel;
    }

    public void Draw(KfwStationeryDrawingTools drawingContext, GoAppSession session, Point mousePosition)
    {
        _drawingContext = drawingContext;
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);

        drawingContext.Begin();

        drawingContext.DrawBackground();
        var modalOpen = session.IsPlayerSelectionDialogOpen || session.IsPlayerEditPanelOpen || session.IsClientIdentityProfileSelectionPanelOpen || session.IsClientIdentityProfileEditPanelOpen || session.IsQuickClientIdentitySelectionPanelOpen ||
                        session.IsGtpEngineSelectionDialogOpen || session.IsGtpEngineEditPanelOpen ||
                        session.IsCgosConnectionEditPanelOpen || session.IsCgosAdminPlayerSelectionDialogOpen || session.IsCgosPracticeResignConfirmationPending;
        DrawCgosClientTopPanel(session, modalOpen ? new Point(-1, -1) : mousePoint);
        SelectEntryPresenter.Default.Draw(_drawingContext, session, mousePoint);
        _drawPlayerEditPanel(session, mousePoint);
        EntryProfilesPresenter.Default.DrawPanels(_drawingContext, session, mousePoint);
        _gtpEngineRenderer.Draw(_drawingContext, session, mousePoint);
        DrawCgosConnectionEditPanel(session, mousePoint);
        DrawCgosAdminPlayerSelectionDialog(session, mousePoint);
        DrawCgosPracticeResignConfirmation(session, mousePoint);

        drawingContext.End();
    }


    public int GetCgosConnectionEditPanelCaretIndex(Point point, CgosConnectionProfileEditField field, string text) =>
        _drawingContext.GetTextCaretIndex(point.X, text, CgosConnectionEditPanelFieldTextBounds(field), 0.42f);

    public int GetCgosCredentialCaretIndex(Point point, GoStone stone, CgosPlayerCredentialField field, string text) =>
        _drawingContext.GetTextCaretIndex(point.X, text, CgosCredentialTextBounds(stone, field), 0.32f);

    public static (GoStone Stone, CgosPlayerCredentialField Field)? GetCgosCredentialFieldHit(Point point)
    {
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
        foreach (var field in new[] { CgosPlayerCredentialField.LoginName, CgosPlayerCredentialField.Password })
            if (CgosCredentialTextBounds(stone, field).Contains(point)) return (stone, field);
        return null;
    }

    public bool TryToggleCgosPasswordVisibility(Point point, bool player2Enabled)
    {
        foreach (var stone in new[] { GoStone.Black, GoStone.White })
        {
            if (stone == GoStone.White && !player2Enabled) continue;
            if (!CgosPasswordVisibilityBounds(stone).Contains(point)) continue;
            if (!_visibleCgosPasswords.Add(stone)) _visibleCgosPasswords.Remove(stone);
            return true;
        }

        return false;
    }


    public static bool GetCgosPlayer2InputCheckHit(Point point, bool enabled) =>
        enabled && CgosPlayer2InputCheckBounds.Contains(point);

    public static bool GetCgosAdminInputCheckHit(Point point, bool enabled) =>
        enabled && CgosAdminInputCheckBounds.Contains(point);


    public static int? GetCgosAdminPlayerDialogItemHit(Point point, GoAppSession session)
    {
        for (var slot = 0; slot < GoAppSession.CgosAdminPlayerSelectionPageSize; slot++)
        {
            if (!CgosAdminPlayerDialogItemBounds(slot).Contains(point)) continue;
            var index = session.CgosAdminPlayerSelectionPageIndex * GoAppSession.CgosAdminPlayerSelectionPageSize + slot;
            return index < session.CgosAdminWaitingPlayers.Count ? index : null;
        }

        return null;
    }


    /// <summary>
    /// ［Admin ＞ LOG: EDIT］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>

    /// <summary>
    /// ［Admin ＞ LOG: VIEW］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>



    /// <summary>
    /// ［プレイヤー１　＞　LOG: EDIT］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>

    /// <summary>
    /// ［プレイヤー１　＞　LOG: VIEW］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>

    /// <summary>
    /// ［プレイヤー２　＞　LOG: EDIT］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>

    /// <summary>
    /// ［プレイヤー２　＞　LOG: VIEW］ボタンの活性化状態
    /// </summary>
    /// <param name="point"></param>
    /// <param name="enabled"></param>
    /// <returns></returns>


    public static GoStone? GetCgosConnectionEngineSelectButtonHit(Point point, GoAppSession session)
    {
        if (!session.IsCgosBlackConnectionRunning && CgosPlayerSelectorValueBounds(CgosBlackEngineSelector).Contains(point)) return GoStone.Black;
        return session.IsCgosPlayer2InputEnabled &&
               !session.IsCgosWhiteConnectionRunning &&
               CgosPlayerSelectorValueBounds(CgosWhiteEngineSelector).Contains(point)
            ? GoStone.White
            : null;
    }


    public static CgosConnectionProfileEditField? GetCgosConnectionEditPanelFieldHit(Point point)
    {
        foreach (var field in CgosConnectionEditFields)
        {
            if (CgosConnectionEditPanelFieldTextBounds(field).Contains(point))
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
        _drawingContext.DrawIconStone(localStone, 24, black: true);
        _drawingContext.DrawIconStone(server, 18, black: false);
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
        var panel = new Rectangle(230, 126, 1460, 820);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        if (session.CgosConnectionFlowKind == CgosConnectionFlowKind.ConnectionStart)
        {
            new Headline("CGOS CLIENT", new Vector2(panel.X + 58, panel.Y + 58), new Color(255, 230, 160), 1.0f).Draw(_drawingContext);
            DrawCgosConnectionStartPanel(session, mousePoint);
            return;
        }

        DrawText("CGOS CONNECTION SELECT", new Vector2(panel.X + 30, panel.Y + 24), new Color(244, 238, 218), 0.78f);

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
        DrawText($"PAGE {session.CgosConnectionSelectionPageIndex + 1} / {session.GetCgosConnectionSelectionPageCount()}", new Vector2(600, 817), new Color(227, 224, 210), 0.42f);
        var page = CgosSelectConnectionPage.Default;
        page.PreviousButton.IsEnabled = session.CanMoveCgosConnectionSelectionPage(-1);
        page.NextButton.IsEnabled = session.CanMoveCgosConnectionSelectionPage(1);
        page.EditButton.IsEnabled = session.CgosConnectionProfiles.Count > 0;
        page.DuplicateButton.IsEnabled = session.CgosConnectionProfiles.Count > 0;
        page.DeleteButton.IsEnabled = session.CanDeleteSelectedCgosConnectionProfile;
        page.OrderButton.IsEnabled = session.CgosConnectionProfiles.Count > 1;
        page.SelectButton.IsEnabled = session.CgosConnectionProfiles.Count > 0;
        page.PreviousButton.Draw(mousePoint, _drawingContext);
        page.NextButton.Draw(mousePoint, _drawingContext);
        page.AddButton.Draw(mousePoint, _drawingContext);
        page.EditButton.Draw(mousePoint, _drawingContext);
        page.DuplicateButton.Draw(mousePoint, _drawingContext);
        page.DeleteButton.Draw(mousePoint, _drawingContext);
        page.OrderButton.Draw(mousePoint, _drawingContext);
        page.SelectButton.Draw(mousePoint, _drawingContext);
        page.CancelButton.Draw(mousePoint, _drawingContext);
        CatalogOrderPresenter.Default.Draw(_drawingContext,
            session.CgosConnectionOrderEditor,
            "CGOS CONNECTIONS",
            mousePoint,
            profile => profile.DisplayName,
            profile => $"{profile.Host}:{profile.Port}  {profile.Event} {profile.Round}".Trim());
    }


    private void DrawCgosConnectionStartPanel(GoAppSession session, Point mousePoint)
    {
        var page = CgosLoginPage.Default;
        page.UpdateGameInProgressButtons(session.IsCgosGameInProgress, session.IsCgosPracticeUnexpectedGameInProgress);
        var profile = session.SelectedCgosConnectionProfile;
        DrawText("USE CONNECTION", new Vector2(288, 300), new Color(180, 195, 195), 0.54f);
        page.BackButton.Label = session.IsAnyCgosProcessRunning ? "DISCONNECT ALL & BACK" : "BACK";
        page.BackButton.LabelScale = session.IsAnyCgosProcessRunning ? 0.25f : 0.42f;
        page.BackButton.Draw(mousePoint, _drawingContext);

        DrawCgosSelectedProfileBar(profile);
        DrawCgosProcessPanel(
            CgosAdminProcessPanelBounds,
            "ADMIN CONNECTION",
            session.CgosAdminStatusMessage,
            null,
            null,
            null,
            page.AdminConnectButton,
            session.IsCgosAdminRunning ? "DISCONNECT" : "CONNECT",
            session.IsCgosAdminInputEnabled,
            page.AdminTailButton,
            page.AdminCodeButton,
            session.IsCgosAdminInputEnabled && !string.IsNullOrWhiteSpace(session.CgosAdminLogDirectory),
            mousePoint);
        page.AdminWhoButton.IsEnabled = session.IsCgosAdminInputEnabled && session.IsCgosAdminRunning;
        page.AdminWhoButton.Draw(mousePoint, _drawingContext);
        DrawCgosAdminPlayerSelector(CgosAdminWhitePlayerRowBounds, "WHITE", session.CgosAdminWhitePlayerName, mousePoint);
        DrawCgosAdminPlayerSelector(CgosAdminBlackPlayerRowBounds, "BLACK", session.CgosAdminBlackPlayerName, mousePoint);
        page.AdminMatchButton.IsEnabled = session.IsCgosAdminInputEnabled && session.CanSendCgosAdminMatch;
        page.AdminSwapButton.IsEnabled = page.AdminMatchButton.IsEnabled;
        page.AdminMatchButton.Draw(mousePoint, _drawingContext);
        page.AdminSwapButton.Draw(mousePoint, _drawingContext);
        if (!session.IsCgosAdminInputEnabled)
            FillRect(CgosAdminProcessPanelBounds, new Color(8, 11, 15, 176));
        DrawCgosOptionalInputCheck(
            CgosAdminInputCheckBounds,
            "Adminを入力する",
            session.IsCgosAdminInputEnabled,
            !session.IsCgosAdminRunning,
            mousePoint);

        DrawCgosProcessPanel(
            CgosBlackProcessPanelBounds,
            "PLAYER 1",
            session.CgosBlackConnectionStatusMessage,
            session.SelectedCgosBlackEntryProfile?.DisplayName ?? session.SelectedCgosBlackGtpEngineProfile?.DisplayName,
            CgosBlackEngineSelector with { Enabled = !session.IsCgosBlackConnectionRunning },
            string.IsNullOrWhiteSpace(session.CgosBlackGtpResponseWaitDisplay)
                ? session.CgosBlackConnectionElapsedDisplay
                : session.CgosBlackGtpResponseWaitDisplay,
            page.BlackConnectButton,
            session.IsCgosBlackConnectionRunning
                ? session.IsCgosGameInProgress ? "ABORT" : "DISCONNECT"
                : "CONNECT",
            session.IsCgosBlackConnectionRunning || session.SelectedCgosBlackEntryProfile is not null,
            page.BlackTailButton,
            page.BlackCodeButton,
            !string.IsNullOrWhiteSpace(session.CgosBlackConnectionLogDirectory),
            mousePoint);
        if (session.IsCgosGameInProgress && session.IsCgosBlackConnectionRunning)
        {
            page.BlackResignButton.Draw(mousePoint, _drawingContext);
        }
        DrawCgosCredentialFields(session, GoStone.Black, mousePoint);

        DrawCgosProcessPanel(
            CgosWhiteProcessPanelBounds,
            "PRACTICE PLAYER",
            session.CgosWhiteConnectionStatusMessage,
            session.SelectedCgosWhiteEntryProfile?.DisplayName ?? session.SelectedCgosWhiteGtpEngineProfile?.DisplayName,
            CgosWhiteEngineSelector with { Enabled = session.IsCgosPlayer2InputEnabled && !session.IsCgosWhiteConnectionRunning },
            string.IsNullOrWhiteSpace(session.CgosWhiteGtpResponseWaitDisplay)
                ? session.CgosWhiteConnectionElapsedDisplay
                : session.CgosWhiteGtpResponseWaitDisplay,
            page.WhiteConnectButton,
            session.IsCgosWhiteConnectionRunning
                ? "DISCONNECT"
                : "CONNECT",
            session.IsCgosPlayer2InputEnabled &&
            (session.IsCgosWhiteConnectionRunning || session.SelectedCgosWhiteGtpEngineProfile is not null),
            page.WhiteTailButton,
            page.WhiteCodeButton,
            session.IsCgosPlayer2InputEnabled && !string.IsNullOrWhiteSpace(session.CgosWhiteConnectionLogDirectory),
            mousePoint);
        if (session.IsCgosPracticeUnexpectedGameInProgress && session.IsCgosWhiteConnectionRunning)
        {
            page.WhiteResignButton.Label = session.IsCgosPracticeResignRequested ? "REQUESTED" : "RESIGN";
            page.WhiteResignButton.IsEnabled = !session.IsCgosPracticeResignRequested;
            page.WhiteResignButton.Draw(mousePoint, _drawingContext);
        }
        if (session.IsCgosPracticeUnexpectedGameInProgress)
        {
            var unexpected = $"UNEXPECTED #{session.CgosPracticeUnexpectedGameId}  {session.CgosPracticeUnexpectedColor}  MOVE {session.CgosPracticeUnexpectedMoveCount}";
            DrawFittedText(unexpected, new Rectangle(CgosWhiteProcessPanelBounds.X + 18, CgosWhiteProcessPanelBounds.Y + 250, CgosWhiteProcessPanelBounds.Width - 36, 28), new Color(255, 183, 146), 0.24f);
            DrawFittedText($"VS {session.CgosPracticeUnexpectedOpponent}  {session.CgosPracticeUnexpectedTimeDisplay}", new Rectangle(CgosWhiteProcessPanelBounds.X + 18, CgosWhiteProcessPanelBounds.Y + 278, CgosWhiteProcessPanelBounds.Width - 36, 28), Color.White, 0.24f);
        }
        DrawCgosCredentialFields(session, GoStone.White, mousePoint);
        RandomSeedRowComponent.Cgos.DrawCgos(_drawingContext, mousePoint,
            session.SupportsCgosRandomSeed(GoStone.Black), session.GetCgosRandomSeedText(GoStone.Black), !session.IsCgosBlackConnectionRunning,
            session.IsCgosPlayer2InputEnabled && session.SupportsCgosRandomSeed(GoStone.White), session.GetCgosRandomSeedText(GoStone.White), !session.IsCgosWhiteConnectionRunning);
        if (!session.IsCgosPlayer2InputEnabled)
            FillRect(CgosWhiteProcessPanelBounds, new Color(8, 11, 15, 176));
        DrawCgosOptionalInputCheck(
            CgosPlayer2InputCheckBounds,
            "プラクティスプレイヤーを入力する",
            session.IsCgosPlayer2InputEnabled,
            !session.IsCgosWhiteConnectionRunning,
            mousePoint);

        DrawCgosConnectionTooltips(session, mousePoint);
    }

    private void DrawCgosPracticeResignConfirmation(GoAppSession session, Point mousePoint)
    {
        if (!session.IsCgosPracticeResignConfirmationPending) return;

        var bounds = new Rectangle(560, 300, 800, 360);
        FillRect(new Rectangle(0, 0, 1920, 1080), new Color(0, 0, 0, 165));
        FillRect(bounds, new Color(21, 25, 32));
        DrawRect(bounds, 2, new Color(255, 183, 146));
        DrawText("PRACTICE PLAYER RESIGN?", new Vector2(bounds.X + 42, bounds.Y + 42), new Color(255, 230, 160), 0.58f);
        DrawFittedText($"GAME #{session.CgosPracticeUnexpectedGameId}  VS {session.CgosPracticeUnexpectedOpponent}", new Rectangle(bounds.X + 42, bounds.Y + 120, bounds.Width - 84, 44), Color.White, 0.42f);
        DrawFittedText("Only the unexpected practice match will resign.", new Rectangle(bounds.X + 42, bounds.Y + 174, bounds.Width - 84, 40), new Color(180, 195, 195), 0.34f);
        var page = CgosLoginPage.Default;
        page.PracticeResignCancelButton.Bounds = new Rectangle(bounds.Right - 264, bounds.Bottom - 80, 92, 40);
        page.PracticeResignConfirmButton.Bounds = new Rectangle(bounds.Right - 156, bounds.Bottom - 80, 92, 40);
        page.PracticeResignCancelButton.Draw(mousePoint, _drawingContext);
        page.PracticeResignConfirmButton.Draw(mousePoint, _drawingContext);
    }


    private void DrawCgosSelectedProfileBar(CgosConnectionProfile profile)
    {
        FillRect(CgosSelectedProfileBarBounds, new Color(15, 20, 26));
        DrawRect(CgosSelectedProfileBarBounds, 1, new Color(67, 84, 92));
        DrawUiLabel(UiLabel.InCompactRow("CLIENT IDENTITY", CgosSelectedProfileBarBounds));
        var text = $"{profile.DisplayName} / {profile.Host}:{profile.Port} / {profile.Event} / {profile.Round}";
        DrawFittedText(text, new Rectangle(CgosSelectedProfileBarBounds.X + 152, CgosSelectedProfileBarBounds.Y + 7, CgosSelectedProfileBarBounds.Width - 168, 38), Color.White, 0.42f);
    }

    private void DrawCgosAdminPlayerSelector(Rectangle bounds, string label, string playerName, Point mousePoint)
    {
        DrawText(label, new Vector2(bounds.X + 6, bounds.Y + 9), new Color(180, 195, 195), 0.22f);
        var valueBounds = CgosAdminPlayerValueBounds(bounds);
        var hovered = valueBounds.Contains(mousePoint);
        DrawFittedText(string.IsNullOrEmpty(playerName) ? "-" : playerName, valueBounds, Color.White, 0.25f);
        _compactLinkUnderline.Bounds = valueBounds;
        _compactLinkUnderline.SetActionBadge(ActionBadgeComponent.Create("CHANGE", valueBounds));
        _compactLinkUnderline.UpdatePointer(mousePoint);
        _compactLinkUnderline.Draw(_drawingContext);
    }

    private void DrawCgosCredentialFields(GoAppSession session, GoStone stone, Point mousePoint)
    {
        foreach (var field in new[] { CgosPlayerCredentialField.LoginName, CgosPlayerCredentialField.Password })
        {
            var bounds = CgosCredentialRowBounds(stone, field);
            var active = session.ActiveCgosCredentialStone == stone && session.ActiveCgosCredentialField == field;
            var textBounds = CgosCredentialTextBounds(stone, field);
            var hovered = textBounds.Contains(mousePoint);
            DrawText(field == CgosPlayerCredentialField.LoginName ? "HANDLE" : "PASSWORD", new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.28f);
            DrawRoundedFill(new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 4), 2, active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
            var tabIndex = (stone == GoStone.Black ? 0 : 2) + (field == CgosPlayerCredentialField.LoginName ? 0 : 1);
            var activeTabIndex = session.ActiveCgosCredentialStone is { } activeStone &&
                session.ActiveCgosCredentialField is { } activeField
                ? (activeStone == GoStone.Black ? 0 : 2) + (activeField == CgosPlayerCredentialField.LoginName ? 0 : 1)
                : -1;
            DrawTabNavigationHint(bounds, tabIndex, activeTabIndex, 4);
            var text = session.GetCgosCredential(stone, field);
            var passwordVisible = field != CgosPlayerCredentialField.Password || _visibleCgosPasswords.Contains(stone);
            var displayText = field == CgosPlayerCredentialField.Password && !passwordVisible
                ? new string('●', text.Length)
                : text;
            if (active)
                DrawTextBoxSelection(displayText, session.CgosCredentialSelectionStart, session.CgosCredentialSelectionLength, textBounds, 0.32f);
            DrawFittedText(string.IsNullOrEmpty(displayText) ? "-" : displayText, textBounds, Color.White, 0.32f);
            if (active) DrawTextBoxCaret(displayText, session.CgosCredentialCaretIndex, textBounds, 0.32f);
            DrawEditableTextEditHint(active, hovered, textBounds);
            if (field == CgosPlayerCredentialField.Password)
                DrawCgosPasswordEyeButton(CgosPasswordVisibilityBounds(stone), passwordVisible, mousePoint);
        }
    }

    private void DrawCgosPasswordEyeButton(Rectangle bounds, bool visible, Point mousePoint)
    {
        new Button(bounds, string.Empty, 0.1f).Draw(mousePoint, _drawingContext);
        var color = bounds.Contains(mousePoint) ? new Color(222, 243, 246) : new Color(178, 219, 226);
        var center = new Vector2(bounds.Center.X, bounds.Center.Y);
        DrawLine(new Vector2(bounds.X + 6, center.Y), new Vector2(center.X, visible ? bounds.Y + 6 : center.Y + 2), 2, color);
        DrawLine(new Vector2(center.X, visible ? bounds.Y + 6 : center.Y + 2), new Vector2(bounds.Right - 6, center.Y), 2, color);
        if (visible) FillRect(new Rectangle(bounds.Center.X - 3, bounds.Center.Y - 3, 6, 6), color);
    }

    private void DrawCgosOptionalInputCheck(
        Rectangle bounds,
        string label,
        bool isChecked,
        bool enabled,
        Point mousePoint)
    {
        var hovered = enabled && bounds.Contains(mousePoint);
        var checkBounds = new Rectangle(bounds.X, bounds.Y + 3, 22, 22);
        DrawRect(checkBounds, 2, isChecked ? new Color(99, 223, 185) : new Color(91, 117, 128));
        if (isChecked)
        {
            FillRect(new Rectangle(checkBounds.X + 5, checkBounds.Y + 5, 12, 12), new Color(99, 223, 185));
        }
        DrawFittedText(
            label,
            new Rectangle(bounds.X + 32, bounds.Y, bounds.Width - 32, bounds.Height),
            enabled ? hovered ? new Color(220, 255, 242) : Color.White : new Color(115, 125, 130),
            0.32f);
    }


    private void DrawCgosAdminPlayerSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsCgosAdminPlayerSelectionDialogOpen) return;

        FillRect(new Rectangle(0, 0, _drawingContext.ScreenWidth, _drawingContext.ScreenHeight), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(CgosAdminPlayerDialogBounds.X + 18, CgosAdminPlayerDialogBounds.Y + 20, CgosAdminPlayerDialogBounds.Width, CgosAdminPlayerDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(CgosAdminPlayerDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(CgosAdminPlayerDialogBounds, 2, new Color(116, 145, 146));

        var target = session.CgosAdminPlayerSelectionTarget == GoStone.White ? "WHITE" : "BLACK";
        DrawText($"PARTICIPANT SELECT  {target}", new Vector2(CgosAdminPlayerDialogBounds.X + 30, CgosAdminPlayerDialogBounds.Y + 24), new Color(244, 238, 218), 0.72f);
        var page = CgosLoginPage.Default;
        page.PlayerDialogSelectButton.IsEnabled = session.CgosAdminWaitingPlayers.Count > 0;
        page.PlayerDialogCancelButton.Draw(mousePoint, _drawingContext);
        page.PlayerDialogSelectButton.Draw(mousePoint, _drawingContext);

        DrawText("PARTICIPANTS", new Vector2(CgosAdminPlayerDialogListBounds.X, CgosAdminPlayerDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        FillRect(CgosAdminPlayerDialogListBounds, new Color(15, 20, 26));
        DrawRect(CgosAdminPlayerDialogListBounds, 1, new Color(67, 84, 92));

        if (session.CgosAdminWaitingPlayers.Count == 0)
        {
            DrawText("NO PARTICIPANTS - RUN WHO", new Vector2(CgosAdminPlayerDialogListBounds.X + 24, CgosAdminPlayerDialogListBounds.Y + 24), new Color(180, 195, 195), 0.46f);
        }

        var startIndex = session.CgosAdminPlayerSelectionPageIndex * GoAppSession.CgosAdminPlayerSelectionPageSize;
        for (var slot = 0; slot < GoAppSession.CgosAdminPlayerSelectionPageSize; slot++)
        {
            var index = startIndex + slot;
            if (index >= session.CgosAdminWaitingPlayers.Count) break;
            var bounds = CgosAdminPlayerDialogItemBounds(slot);
            var selected = index == session.CgosAdminPlayerDialogSelectionIndex;
            var hovered = bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
            DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
            DrawText($"{index + 1:00}", new Vector2(bounds.X + 16, bounds.Y + 14), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.38f);
            DrawFittedText(session.CgosAdminWaitingPlayers[index], new Rectangle(bounds.X + 70, bounds.Y + 8, bounds.Width - 90, 38), Color.White, 0.48f);
        }

        var pageCount = session.GetCgosAdminPlayerSelectionPageCount();
        DrawText($"PAGE {session.CgosAdminPlayerSelectionPageIndex + 1} / {pageCount}", new Vector2(910, 825), new Color(227, 224, 210), 0.42f);
        page.PlayerDialogPreviousButton.IsEnabled = session.CgosAdminPlayerSelectionPageIndex > 0;
        page.PlayerDialogNextButton.IsEnabled = session.CgosAdminPlayerSelectionPageIndex < pageCount - 1;
        page.PlayerDialogPreviousButton.Draw(mousePoint, _drawingContext);
        page.PlayerDialogNextButton.Draw(mousePoint, _drawingContext);
    }

    /// <summary>
    /// ［CGOSプロセス・パネル］の描画
    /// </summary>
    /// <param name="bounds"></param>
    /// <param name="title"></param>
    /// <param name="status"></param>
    /// <param name="engineName"></param>
    /// <param name="elapsedDisplay"></param>
    /// <param name="startButton"></param>
    /// <param name="startLabel"></param>
    /// <param name="startEnabled"></param>
    /// <param name="tailButton"></param>
    /// <param name="codeButton"></param>
    /// <param name="logToolsEnabled"></param>
    /// <param name="mousePoint"></param>
    private void DrawCgosProcessPanel(
        Rectangle bounds,
        string title,
        string status,
        string? engineName,
        PlayerSelector? engineSelector,
        string? elapsedDisplay,
        Button startButton,
        string startLabel,
        bool startEnabled,
        Button tailButton,
        Button codeButton,
        bool logToolsEnabled,
        Point mousePoint)
    {
        FillRect(bounds, new Color(15, 20, 26));
        DrawRect(bounds, 1, new Color(67, 84, 92));
        DrawText(title, new Vector2(bounds.X + 18, bounds.Y + 18), new Color(255, 230, 160), 0.42f);

        var stateRow = new Rectangle(bounds.X + 16, bounds.Y + 62, bounds.Width - 32, 48);
        DrawDataRowFrame(stateRow);
        DrawUiLabel(UiLabel.InCompactRow("STATE", stateRow));
        var statusBounds = string.IsNullOrEmpty(elapsedDisplay)
            ? new Rectangle(stateRow.X + 132, stateRow.Y + 7, stateRow.Width - 148, 34)
            : new Rectangle(stateRow.X + 132, stateRow.Y + 7, 116, 34);
        DrawFittedText(status, statusBounds, Color.White, 0.34f);
        if (!string.IsNullOrEmpty(elapsedDisplay))
        {
            DrawFittedText(elapsedDisplay, new Rectangle(stateRow.X + 258, stateRow.Y + 3, stateRow.Width - 274, 42), new Color(146, 220, 255), 0.54f);
        }

        if (engineSelector is { } selector)
        {
            DrawCgosPlayerSelector(selector with { Value = engineName ?? "-" }, mousePoint);
        }

        startButton.Label = startLabel;
        startButton.IsEnabled = startEnabled;
        startButton.Draw(mousePoint, _drawingContext);
        DrawText("LOG:", new Vector2(bounds.X + 18, tailButton.Bounds.Y + 15), new Color(180, 195, 195), 0.22f);
        tailButton.IsEnabled = logToolsEnabled;
        tailButton.Draw(mousePoint, _drawingContext);
        codeButton.IsEnabled = logToolsEnabled;
        codeButton.Draw(mousePoint, _drawingContext);
    }

    private void DrawCgosPlayerSelector(PlayerSelector selector, Point mousePoint)
    {
        var fieldBounds = CgosPlayerSelectorValueBounds(selector);
        var hovered = selector.Enabled && fieldBounds.Contains(mousePoint);
        var textBounds = hovered
            ? new Rectangle(fieldBounds.X, fieldBounds.Y, fieldBounds.Width - 122, fieldBounds.Height)
            : fieldBounds;
        DrawText(selector.Label, new Vector2(selector.Bounds.X + 12, fieldBounds.Y + 7), new Color(180, 195, 195), 0.28f);
        DrawFittedText(selector.Value, textBounds, selector.Enabled ? Color.White : new Color(91, 100, 106), 0.46f);
        _selectorLinkUnderline.Bounds = fieldBounds;
        _selectorLinkUnderline.SetActionBadge(ActionBadgeComponent.Create("CHANGE", fieldBounds));
        _selectorLinkUnderline.UpdatePointer(mousePoint);
        _selectorLinkUnderline.Draw(_drawingContext);
    }


    private void DrawCgosConnectionLogRows(GoAppSession session, Point mousePoint)
    {
        var logPath = string.IsNullOrWhiteSpace(session.CgosConnectionLogDirectory) ? "Logs/Cgos" : session.CgosConnectionLogDirectory;

        var stdioBounds = new Rectangle(CgosConnectionStartStatusBounds.X + 22, CgosConnectionStartStatusBounds.Y + 256, CgosConnectionStartStatusBounds.Width - 44, 56);
        DrawDataRowFrame(stdioBounds);
        DrawUiLabel(UiLabel.InCompactRow("STDIO", stdioBounds));
        DrawFittedText(logPath, CgosConnectionLogPathBounds, Color.White, 0.32f);
        var page = CgosLoginPage.Default;
        page.LogCodeButton.Draw(mousePoint, _drawingContext);
        page.LogNotepadButton.Draw(mousePoint, _drawingContext);

        var stderrBounds = new Rectangle(CgosConnectionStartStatusBounds.X + 22, CgosConnectionStartStatusBounds.Y + 332, CgosConnectionStartStatusBounds.Width - 44, 56);
        DrawDataRowFrame(stderrBounds);
        DrawUiLabel(UiLabel.InCompactRow("STDERR", stderrBounds));
        DrawFittedText(logPath, CgosConnectionStandardErrorLogPathBounds, Color.White, 0.32f);
        page.ErrorLogCodeButton.Draw(mousePoint, _drawingContext);
        page.ErrorLogNotepadButton.Draw(mousePoint, _drawingContext);
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
        if (CgosPlayerSelectorValueBounds(CgosBlackEngineSelector).Contains(mousePoint) && session.SelectedCgosBlackGtpEngineProfile is { } blackProfile)
        {
            DrawCgosEngineCommandStickyNote(
                CgosPlayerSelectorValueBounds(CgosBlackEngineSelector),
                "BLACK ENGINE COMMAND",
                FormatCgosEngineCommand(blackProfile));
            return;
        }

        if (CgosPlayerSelectorValueBounds(CgosWhiteEngineSelector).Contains(mousePoint) && session.SelectedCgosWhiteGtpEngineProfile is { } whiteProfile)
        {
            DrawCgosEngineCommandStickyNote(
                CgosPlayerSelectorValueBounds(CgosWhiteEngineSelector),
                "WHITE ENGINE COMMAND",
                FormatCgosEngineCommand(whiteProfile));
            return;
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


    private void DrawCgosEngineCommandStickyNote(Rectangle targetBounds, string title, string command)
    {
        DrawStickyNote(
            StickyNoteKind.CgosEngineCommandHint,
            new Vector2(targetBounds.Right, targetBounds.Center.Y),
            new Color(147, 244, 200),
            new Color(87, 157, 128),
            title,
            WrapCgosCommandForTooltip(command, 28).Take(4).ToArray(),
            bodyLineSpacing: 28,
            anchorBounds: targetBounds);
    }

    private static IEnumerable<string> WrapCgosCommandForTooltip(string command, int maximumLength)
    {
        while (command.Length > maximumLength)
        {
            var split = command.LastIndexOfAny([' ', '\\', '/'], Math.Min(maximumLength, command.Length - 1));
            if (split <= 0) split = maximumLength;
            var includesSeparator = command[split] is ' ' or '\\' or '/';
            yield return command[..(split + (includesSeparator ? 1 : 0))];
            command = command[(split + (includesSeparator ? 1 : 0))..];
        }

        yield return command;
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

        FillRect(new Rectangle(0, 0, _drawingContext.ScreenWidth, _drawingContext.ScreenHeight), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(CgosConnectionEditPanelBounds.X + 14, CgosConnectionEditPanelBounds.Y + 16, CgosConnectionEditPanelBounds.Width, CgosConnectionEditPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(CgosConnectionEditPanelBounds, new Color(19, 24, 31, 250));
        DrawRect(CgosConnectionEditPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsCgosConnectionAddPanelMode ? "ADD SERVICE PROFILE" : "EDIT SERVICE PROFILE", new Vector2(CgosConnectionEditPanelBounds.X + 28, CgosConnectionEditPanelBounds.Y + 24), new Color(244, 238, 218), 0.68f);
        var page = CgosSelectConnectionPage.Default;
        page.EditDiscardButton.IsEnabled = session.IsCgosConnectionEditDirty;
        page.EditSaveButton.Label = session.IsCgosConnectionEditDirty ? "SAVE & CLOSE" : "CLOSE";
        page.EditSaveButton.LabelScale = session.IsCgosConnectionEditDirty ? 0.27f : 0.34f;
        page.EditDiscardButton.Draw(mousePoint, _drawingContext);
        page.EditSaveButton.Draw(mousePoint, _drawingContext);

        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.DisplayName, "DISPLAY", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Host, "HOST", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Port, "PORT", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Event, "EVENT", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Round, "ROUND", mousePoint);
        DrawCgosConnectionEditField(session, CgosConnectionProfileEditField.Note, "NOTE", mousePoint);

        if (!string.IsNullOrWhiteSpace(session.CgosConnectionEditWarning))
        {
            DrawFittedText(session.CgosConnectionEditWarning, new Rectangle(CgosConnectionEditPanelEditorBounds.X + 40, CgosConnectionEditPanelEditorBounds.Bottom - 70, CgosConnectionEditPanelEditorBounds.Width - 80, 34), new Color(255, 183, 146), 0.38f);
        }

    }


    private void DrawCgosConnectionEditField(GoAppSession session, CgosConnectionProfileEditField field, string label, Point mousePoint)
    {
        var bounds = CgosConnectionEditPanelFieldRowBounds(field);
        var active = session.ActiveCgosConnectionEditField == field;
        var text = session.GetCgosConnectionEditFieldText(field);
        var textBounds = CgosConnectionEditPanelFieldTextBounds(field);
        var hovered = textBounds.Contains(mousePoint);
        DrawText(label, new Vector2(bounds.X + 16, textBounds.Y + 7), new Color(180, 195, 195), 0.36f);
        DrawRoundedFill(
            new Rectangle(textBounds.X, textBounds.Bottom + 2, textBounds.Width, 5),
            2,
            active ? new Color(147, 244, 200) : hovered ? new Color(185, 196, 255) : new Color(100, 110, 145));
        DrawTabNavigationHint(
            bounds,
            Array.IndexOf(CgosConnectionEditFields, field),
            session.ActiveCgosConnectionEditField is { } activeField ? Array.IndexOf(CgosConnectionEditFields, activeField) : -1,
            CgosConnectionEditFields.Length);
        if (active)
            DrawTextBoxSelection(text, session.CgosConnectionEditSelectionStart, session.CgosConnectionEditSelectionLength, textBounds, 0.42f);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
        {
            DrawTextBoxCaret(text, session.CgosConnectionEditCaretIndex, textBounds, 0.42f);
        }
        DrawEditableTextEditHint(active, hovered, textBounds);
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
        DrawCgosConnectionPropertyRow(y + 280, "ROUND", profile.Round);
        DrawCgosConnectionPropertyRow(y + 350, "NOTE", profile.Note);
    }


    private void DrawCgosConnectionPropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(CgosConnectionPropertyBounds.X + 18, y, CgosConnectionPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }


    private static Rectangle CgosAdminWhitePlayerRowBounds => new(CgosAdminProcessPanelBounds.X + 16, CgosAdminProcessPanelBounds.Y + 334, CgosAdminProcessPanelBounds.Width - 32, 36);


    private static Rectangle CgosAdminBlackPlayerRowBounds => new(CgosAdminProcessPanelBounds.X + 16, CgosAdminProcessPanelBounds.Y + 376, CgosAdminProcessPanelBounds.Width - 32, 36);


    private static Rectangle CgosAdminPlayerValueBounds(Rectangle bounds) =>
        new(bounds.X + 62, bounds.Y + 3, bounds.Width - 70, bounds.Height - 8);


    private static Rectangle CgosAdminPlayerDialogBounds => new(510, 170, 900, 740);


    private static Rectangle CgosAdminPlayerDialogListBounds => new(550, 280, 820, 480);


    private static Rectangle CgosAdminPlayerDialogItemBounds(int slot) =>
        new(CgosAdminPlayerDialogListBounds.X + 16, CgosAdminPlayerDialogListBounds.Y + 16 + slot * 72, CgosAdminPlayerDialogListBounds.Width - 32, 56);


    private static Rectangle CgosConnectionLogPathBounds => new(1102, 608, 178, 38);


    private static Rectangle CgosConnectionStandardErrorLogPathBounds => new(1102, 684, 178, 38);




    private static Rectangle CgosConnectionListBounds => new(270, 242, 650, 560);


    private static Rectangle CgosConnectionPropertyBounds => new(950, 270, 700, 532);


    private static Rectangle CgosSelectedProfileBarBounds => new(288, 358, 1344, 56);


    private static Rectangle CgosAdminProcessPanelBounds => new(1204, 464, 428, 448);


    private static Rectangle CgosBlackProcessPanelBounds => new(288, 464, 428, 448);


    private static Rectangle CgosWhiteProcessPanelBounds => new(746, 464, 428, 448);

    private static Rectangle CgosPlayer2InputCheckBounds =>
        new(CgosWhiteProcessPanelBounds.X + 16, CgosWhiteProcessPanelBounds.Y - 34, CgosWhiteProcessPanelBounds.Width - 32, 28);

    private static Rectangle CgosAdminInputCheckBounds =>
        new(CgosAdminProcessPanelBounds.X + 16, CgosAdminProcessPanelBounds.Y - 34, CgosAdminProcessPanelBounds.Width - 32, 28);


    private static PlayerSelector CgosBlackEngineSelector => new(
        new Rectangle(CgosBlackProcessPanelBounds.X + 16, CgosBlackProcessPanelBounds.Y + 120, CgosBlackProcessPanelBounds.Width - 32, 48),
        "PLAYER",
        "",
        "SELECT",
        58,
        88);


    private static PlayerSelector CgosWhiteEngineSelector => new(
        new Rectangle(CgosWhiteProcessPanelBounds.X + 16, CgosWhiteProcessPanelBounds.Y + 120, CgosWhiteProcessPanelBounds.Width - 32, 48),
        "PLAYER",
        "",
        "SELECT",
        58,
        88);

    private static Rectangle CgosPlayerSelectorValueBounds(PlayerSelector selector) =>
        new(selector.Bounds.X + 84, selector.Bounds.Y + 4, selector.Bounds.Width - 96, selector.Bounds.Height - 8);

    private static Rectangle CgosCredentialRowBounds(GoStone stone, CgosPlayerCredentialField field)
    {
        var panel = stone == GoStone.Black ? CgosBlackProcessPanelBounds : CgosWhiteProcessPanelBounds;
        return new Rectangle(panel.X + 16, panel.Y + (field == CgosPlayerCredentialField.LoginName ? 170 : 206), panel.Width - 32, 34);
    }

    private static Rectangle CgosCredentialTextBounds(GoStone stone, CgosPlayerCredentialField field)
    {
        var row = CgosCredentialRowBounds(stone, field);
        return new Rectangle(row.X + 132, row.Y + 1, row.Width - (field == CgosPlayerCredentialField.Password ? 188 : 148), 28);
    }

    private static Rectangle CgosPasswordVisibilityBounds(GoStone stone)
    {
        var row = CgosCredentialRowBounds(stone, CgosPlayerCredentialField.Password);
        return new Rectangle(row.Right - 48, row.Y, 32, 30);
    }


    private static Rectangle CgosConnectionStartTargetBounds => new(482, 350, 420, 426);


    private static Rectangle CgosConnectionStartStatusBounds => new(936, 350, 500, 426);


    private static Rectangle CgosConnectionOutputBounds => new(482, 800, 620, 108);


    private static Rectangle CgosConnectionOutputLineBounds(int index) =>
        new(CgosConnectionOutputBounds.X + 16, CgosConnectionOutputBounds.Y + 10 + index * 23, CgosConnectionOutputBounds.Width - 32, 21);

    private static Rectangle CgosConnectionMessageTooltipBounds => new(500, 674, 1040, 106);


    private static Rectangle CgosConnectionEditPanelBounds => new(430, 126, 1060, 820);


    private static Rectangle CgosConnectionEditPanelEditorBounds => new(520, 228, 880, 590);




    private static readonly CgosConnectionProfileEditField[] CgosConnectionEditFields =
    {
        CgosConnectionProfileEditField.DisplayName,
        CgosConnectionProfileEditField.Host,
        CgosConnectionProfileEditField.Port,
        CgosConnectionProfileEditField.Event,
        CgosConnectionProfileEditField.Round,
        CgosConnectionProfileEditField.Note,
    };


    private static Rectangle CgosConnectionEditPanelFieldRowBounds(CgosConnectionProfileEditField field) => field switch
    {
        CgosConnectionProfileEditField.DisplayName => new Rectangle(AddPanelControlX, 250, 668, 56),
        CgosConnectionProfileEditField.Host => new Rectangle(AddPanelControlX, 320, 668, 56),
        CgosConnectionProfileEditField.Port => new Rectangle(AddPanelControlX, 390, 668, 56),
        CgosConnectionProfileEditField.Event => new Rectangle(AddPanelControlX, 460, 668, 56),
        CgosConnectionProfileEditField.Round => new Rectangle(AddPanelControlX, 530, 668, 56),
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

    private void FillRect(Rectangle bounds, Color color) => _drawingContext.FillRectangle(bounds, color);
    private void DrawRect(Rectangle bounds, int thickness, Color color) => _drawingContext.DrawRectangle(bounds, thickness, color);
    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color) => _drawingContext.DrawLine(start, end, thickness, color);
    private void DrawText(string text, Vector2 position, Color color, float scale) => _drawingContext.DrawText(text, position, color, scale);
    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale) => _drawingContext.DrawFittedText(text, bounds, color, scale);
    private void DrawDataRowFrame(Rectangle bounds) => _drawingContext.DrawDataRowFrame(bounds);
    private void DrawUiLabel(UiLabel label) => DrawFittedText(label.Text, label.Bounds, UiLabel.TextColor, label.Scale);
    private void DrawTextBoxSelection(string text, int start, int length, Rectangle bounds, float scale) => _drawingContext.DrawTextSelection(text, start, length, bounds, scale);
    private void DrawTextBoxCaret(string text, int caret, Rectangle bounds, float scale) => _drawingContext.DrawTextCaret(text, caret, bounds, scale);
    private void DrawRoundedFill(Rectangle bounds, int radius, Color color) => _drawingContext.FillRoundedRectangle(bounds, radius, color);
    private void DrawIconStone(Vector2 center, float radius, bool black) => _drawingContext.DrawIconStone(center, radius, black);
    private void DrawStickyNote(StickyNoteKind kind, Vector2 connectorStart, Color accent, Color border, string heading,
        IReadOnlyList<string> lines, int bodyLineSpacing = 40, Rectangle? anchorBounds = null) =>
        _drawingContext.DrawStickyNote(kind, connectorStart, accent, border, heading, lines, bodyLineSpacing, anchorBounds);
    private void DrawEditableTextEditHint(bool editing, bool hovered, Rectangle bounds)
    {
        if (editing || !hovered) { _editActionBadge.Hide(); return; }
        _editActionBadge.SetAnchorBounds(bounds); _editActionBadge.Show(); _editActionBadge.Draw(_drawingContext);
    }
    private void DrawTabNavigationHint(Rectangle bounds, int tabIndex, int activeIndex, int stopCount)
    {
        if (activeIndex < 0 || tabIndex == activeIndex || stopCount < 2) return;
        var previous = tabIndex == (activeIndex + stopCount - 1) % stopCount;
        var next = tabIndex == (activeIndex + 1) % stopCount;
        if (!previous && !next) return;
        var text = previous ? "SHIFT + TAB" : "TAB"; var width = previous ? 132 : 56;
        var hint = new Rectangle(bounds.X - width - 6, bounds.Y - 34, width, 28);
        DrawRoundedFill(hint, 6, new Color(4, 6, 8, 235));
        DrawFittedText(text, new Rectangle(hint.X + 4, hint.Y + 2, hint.Width - 8, hint.Height - 4), Color.White, 0.32f);
    }
    private void DrawLocalClosedBox(Rectangle bounds)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 70));
        FillRect(bounds, new Color(17, 24, 29)); DrawRect(bounds, 4, new Color(126, 150, 164));
        DrawMiniBoardGrid(new Rectangle(bounds.X + 22, bounds.Y + 20, bounds.Width - 44, bounds.Height - 40), new Color(88, 102, 112, 85));
        var left = new Vector2(bounds.X + 94, bounds.Y + 76); var right = new Vector2(bounds.X + 206, bounds.Y + 76);
        DrawLine(left, right, 5, new Color(99, 223, 185)); DrawIconStone(left, 24, true); DrawIconStone(right, 24, false);
    }
    private void DrawMiniBoardGrid(Rectangle bounds, Color color)
    {
        for (var index = 0; index < 7; index++)
        {
            var x = bounds.X + index * bounds.Width / 6f; var y = bounds.Y + index * bounds.Height / 6f;
            DrawLine(new Vector2(x, bounds.Y), new Vector2(x, bounds.Bottom), 1, color);
            DrawLine(new Vector2(bounds.X, y), new Vector2(bounds.Right, y), 1, color);
        }
    }

}
