namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

public sealed class GoScreenRenderer
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;
    private readonly Texture2D _softCircle;
    private readonly Texture2D _stoneLight;
    private readonly Texture2D _stoneDark;

    public GoScreenRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("Fonts/Ui");
        _pixel = CreateTexture(1, 1, (_, _) => Color.White);
        _softCircle = CreateCircleTexture(128, new Color(255, 255, 255, 255), softEdge: true);
        _stoneLight = CreateStoneTexture(128, lightStone: true);
        _stoneDark = CreateStoneTexture(128, lightStone: false);
    }

    public void Draw(GoAppSession session, Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        DrawBoard(session, mousePoint);
        DrawSidePanel(session, mousePoint);
        DrawTournamentRulesSelectionDialog(session, mousePoint);
        DrawTournamentRulesAddPanel(session, mousePoint);
        DrawGtpEngineSelectionDialog(session, mousePoint);
        DrawGtpEngineEditPanel(session, mousePoint);

        _spriteBatch.End();
    }

    public void DrawUseSelection(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        DrawUseSelectionPanel(mousePoint);

        _spriteBatch.End();
    }

    public void DrawCgosClientTop(Point mousePosition)
    {
        var mousePoint = VirtualScreen.ToVirtualPoint(_graphicsDevice.Viewport, mousePosition);

        _spriteBatch.Begin(
            samplerState: SamplerState.LinearClamp,
            transformMatrix: VirtualScreen.GetTransform(_graphicsDevice.Viewport));

        DrawBackground();
        DrawCgosClientTopPanel(mousePoint);

        _spriteBatch.End();
    }

    public static int? GetBoardSizeButtonHit(Point point, GoAppModeKind modeKind)
    {
        if (modeKind == GoAppModeKind.GameOver)
        {
            return null;
        }

        var y = AddPanelBoardSizeButtonY;
        if (BoardSizeButtonBounds(0, y).Contains(point))
        {
            return 9;
        }

        if (BoardSizeButtonBounds(1, y).Contains(point))
        {
            return 13;
        }

        return BoardSizeButtonBounds(2, y).Contains(point) ? 19 : null;
    }

    public static bool GetTournamentRulesBrowseButtonHit(Point point) =>
        TournamentRulesSelector.ContainsBrowseButton(point);

    public static bool GetTournamentRulesSelectionDialogCloseButtonHit(Point point) =>
        TournamentRulesSelectionDialogCloseButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogAddButtonHit(Point point) =>
        TournamentRulesSelectionDialogAddButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogEditButtonHit(Point point) =>
        TournamentRulesSelectionDialogEditButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogDuplicateButtonHit(Point point) =>
        TournamentRulesSelectionDialogDuplicateButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogDeleteButtonHit(Point point, bool enabled) =>
        enabled && TournamentRulesSelectionDialogDeleteButtonBounds.Contains(point);

    public static bool GetTournamentRulesDeleteConfirmationConfirmButtonHit(Point point) =>
        TournamentRulesDeleteConfirmationConfirmButtonBounds.Contains(point);

    public static bool GetTournamentRulesDeleteConfirmationCancelButtonHit(Point point) =>
        TournamentRulesDeleteConfirmationCancelButtonBounds.Contains(point);

    public static bool GetTournamentRulesAddPanelCloseButtonHit(Point point) =>
        TournamentRulesAddPanelCloseButtonBounds.Contains(point);

    public static bool GetTournamentRulesAddPanelDisplayNameBoxHit(Point point) =>
        TournamentRulesAddPanelDisplayNameRowBounds.Contains(point);

    public int GetTournamentRulesAddPanelDisplayNameCaretIndex(Point point, string text) =>
        GetTextBoxCaretIndex(point.X, text, TournamentRulesAddPanelDisplayNameTextBounds, 0.46f);

    public int GetGtpEngineEditPanelCaretIndex(Point point, GtpEngineProfileEditField field, string text) =>
        GetTextBoxCaretIndex(point.X, text, GtpEngineEditPanelFieldTextBounds(field), 0.42f);

    public static bool GetTournamentRulesAddPanelFileBrowseButtonHit(Point point) =>
        TournamentRulesAddPanelFileBrowseButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogPreviousPageButtonHit(Point point) =>
        TournamentRulesSelectionDialogPreviousPageButtonBounds.Contains(point);

    public static bool GetTournamentRulesSelectionDialogNextPageButtonHit(Point point) =>
        TournamentRulesSelectionDialogNextPageButtonBounds.Contains(point);

    public static int? GetTournamentRulesSelectionDialogListItemHit(Point point, GoAppSession session)
    {
        for (var i = 0; i < GoAppSession.TournamentRulesSelectionPageSize; i++)
        {
            if (!TournamentRulesSelectionDialogListItemBounds(i).Contains(point))
            {
                continue;
            }

            var index = session.TournamentRulesSelectionPageIndex * GoAppSession.TournamentRulesSelectionPageSize + i;
            return index < session.TournamentRulesList.Count ? index : null;
        }

        return null;
    }

    public static bool TryGetTournamentRulesSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
    {
        text = "";
        if (session.SelectedTournamentRulesIndex < 0 || session.SelectedTournamentRulesIndex >= session.TournamentRulesList.Count)
        {
            return false;
        }

        var path = session.TournamentRulesList[session.SelectedTournamentRulesIndex].FilePath;
        if (string.IsNullOrWhiteSpace(path) || !PathTooltipCopyButtonBounds(TournamentRulesSelectionDialogPropertyRowBounds(6)).Contains(point))
        {
            return false;
        }

        text = path;
        return true;
    }

    public static bool GetGtpEngineSelectionDialogCloseButtonHit(Point point) =>
        GtpEngineSelectionDialogCloseButtonBounds.Contains(point);

    public static bool GetGtpEngineSelectionDialogAddButtonHit(Point point) =>
        GtpEngineSelectionDialogAddButtonBounds.Contains(point);

    public static bool GetGtpEngineSelectionDialogEditButtonHit(Point point) =>
        GtpEngineSelectionDialogEditButtonBounds.Contains(point);

    public static bool GetGtpEngineSelectionDialogDuplicateButtonHit(Point point) =>
        GtpEngineSelectionDialogDuplicateButtonBounds.Contains(point);

    public static bool GetGtpEngineSelectionDialogDeleteButtonHit(Point point, bool enabled) =>
        enabled && GtpEngineSelectionDialogDeleteButtonBounds.Contains(point);

    public static bool GetGtpEngineDeleteConfirmationConfirmButtonHit(Point point) =>
        GtpEngineDeleteConfirmationConfirmButtonBounds.Contains(point);

    public static bool GetGtpEngineDeleteConfirmationCancelButtonHit(Point point) =>
        GtpEngineDeleteConfirmationCancelButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelCloseButtonHit(Point point) =>
        GtpEngineEditPanelCloseButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelSaveButtonHit(Point point) =>
        GtpEngineEditPanelSaveButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelFileBrowseButtonHit(Point point) =>
        GtpEngineEditPanelFileBrowseButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelWorkingDirectoryBrowseButtonHit(Point point) =>
        GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds.Contains(point);

    public static bool GetGtpEngineEditPanelLogButtonHit(Point point) =>
        GtpEngineEditPanelLogButtonBounds.Contains(point);

    public static GtpEngineProfileEditField? GetGtpEngineEditPanelFieldHit(Point point)
    {
        foreach (var field in GtpEngineEditFields)
        {
            if (GtpEngineEditPanelFieldRowBounds(field).Contains(point))
            {
                return field;
            }
        }

        return null;
    }

    public static bool GetGtpEngineSelectionDialogPreviousPageButtonHit(Point point) =>
        GtpEngineSelectionDialogPreviousPageButtonBounds.Contains(point);

    public static bool GetGtpEngineSelectionDialogNextPageButtonHit(Point point) =>
        GtpEngineSelectionDialogNextPageButtonBounds.Contains(point);

    public static int? GetGtpEngineSelectionDialogListItemHit(Point point, GoAppSession session)
    {
        for (var i = 0; i < GoAppSession.GtpEngineSelectionPageSize; i++)
        {
            if (!GtpEngineSelectionDialogListItemBounds(i).Contains(point))
            {
                continue;
            }

            var index = session.GtpEngineSelectionPageIndex * GoAppSession.GtpEngineSelectionPageSize + i;
            return index < session.GtpEngineProfiles.Count ? index : null;
        }

        return null;
    }

    public static bool TryGetGtpEngineSelectionDialogPathCopyText(Point point, GoAppSession session, out string text)
    {
        text = "";
        var selectedIndex = session.GtpEngineSelectionTargetStone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        if (selectedIndex < 0 || selectedIndex >= session.GtpEngineProfiles.Count)
        {
            return false;
        }

        var profile = session.GtpEngineProfiles[selectedIndex];
        if (!string.IsNullOrWhiteSpace(profile.ExecutablePath) && PathTooltipCopyButtonBounds(GtpEngineSelectionDialogPropertyRowBounds(1)).Contains(point))
        {
            text = profile.ExecutablePath;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(profile.WorkingDirectory) && PathTooltipCopyButtonBounds(GtpEngineSelectionDialogPropertyRowBounds(2)).Contains(point))
        {
            text = profile.WorkingDirectory;
            return true;
        }

        return false;
    }

    public static GoRuleKind? GetRuleKindButtonHit(Point point)
    {
        if (RuleKindButtonBounds(0).Contains(point))
        {
            return GoRuleKind.Japanese;
        }

        if (RuleKindButtonBounds(1).Contains(point))
        {
            return GoRuleKind.PureGo;
        }

        return RuleKindButtonBounds(2).Contains(point) ? GoRuleKind.Chinese : null;
    }

    public static decimal? GetKomiStepButtonHit(Point point)
    {
        if (KomiStepButtonBounds(0).Contains(point))
        {
            return -0.5m;
        }

        return KomiStepButtonBounds(1).Contains(point) ? 0.5m : null;
    }

    public static TimeSpan? GetMainTimeStepButtonHit(Point point)
    {
        if (MainTimeStepButtonBounds(0).Contains(point))
        {
            return TimeSpan.FromMinutes(-1);
        }

        return MainTimeStepButtonBounds(1).Contains(point) ? TimeSpan.FromMinutes(1) : null;
    }

    public static int? GetMoveLimitStepButtonHit(Point point)
    {
        if (MoveLimitStepButtonBounds(0).Contains(point))
        {
            return -10;
        }

        return MoveLimitStepButtonBounds(1).Contains(point) ? 10 : null;
    }

    public static bool GetSaveTournamentRulesButtonHit(Point point) => SaveTournamentRulesButtonBounds.Contains(point);

    public static bool GetLocalUseButtonHit(Point point) => LocalUseButtonBounds.Contains(point);

    public static bool GetCgosUseButtonHit(Point point) => CgosUseButtonBounds.Contains(point);

    public static bool GetCgosBackButtonHit(Point point) => CgosBackButtonBounds.Contains(point);

    public static bool GetImportSgfButtonHit(Point point) => ImportSgfButtonBounds.Contains(point);

    public static bool GetStartReviewingButtonHit(Point point, bool enabled) =>
        enabled && StartReviewingButtonBounds.Contains(point);

    public static bool GetStartBoardEditingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartBoardEditingButtonBounds.Contains(point);

    public static bool GetStartPlayingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartPlayingButtonBounds.Contains(point);

    public static bool GetReturnToSetupButtonHit(Point point) => ReturnToSetupButtonBounds.Contains(point);

    public static bool GetExportSgfButtonHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static GoPlayerKind? GetBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, BlackPlayerKindButtonY);

    public static GoPlayerKind? GetWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, WhitePlayerKindButtonY);

    public static bool GetBlackGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(BlackEngineButtonY).ContainsBrowseButton(point);

    public static bool GetWhiteGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(WhiteEngineButtonY).ContainsBrowseButton(point);

    public static bool GetPassButtonHit(Point point) => PassButtonBounds.Contains(point);

    public static bool GetResignButtonHit(Point point) => ResignButtonBounds.Contains(point);

    public static bool GetCancelPlayingButtonHit(Point point) => CancelPlayingButtonBounds.Contains(point);

    public static bool GetBoardEditingBlackButtonHit(Point point) => BoardEditingBlackButtonBounds.Contains(point);

    public static bool GetBoardEditingWhiteButtonHit(Point point) => BoardEditingWhiteButtonBounds.Contains(point);

    public static bool GetBoardEditingEraseButtonHit(Point point) => BoardEditingEraseButtonBounds.Contains(point);

    public static bool GetBoardEditingUndoButtonHit(Point point) => BoardEditingUndoButtonBounds.Contains(point);

    public static bool GetBoardEditingRedoButtonHit(Point point) => BoardEditingRedoButtonBounds.Contains(point);

    public static bool GetBoardEditingExportSgfButtonHit(Point point) => BoardEditingExportSgfButtonBounds.Contains(point);

    public static bool GetBoardEditingDoneButtonHit(Point point) => BoardEditingDoneButtonBounds.Contains(point);

    public static int? GetReviewStepButtonHit(Point point)
    {
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            if (ReviewStepButtonBounds(i).Contains(point))
            {
                return ReviewStepButtonValues[i];
            }
        }

        return null;
    }

    public static bool GetReviewDoneButtonHit(Point point) => ReviewDoneButtonBounds.Contains(point);

    public static bool TryGetBoardIntersection(Point point, int boardSize, out Point intersection)
    {
        var layout = GetBoardLayout(boardSize);
        var nearestX = (int)MathF.Round((point.X - layout.Start.X) / layout.Cell);
        var nearestY = (int)MathF.Round((point.Y - layout.Start.Y) / layout.Cell);
        if (nearestX < 0 || nearestX >= boardSize || nearestY < 0 || nearestY >= boardSize)
        {
            intersection = Point.Zero;
            return false;
        }

        var center = BoardPoint(layout.Start, layout.Cell, nearestX, nearestY);
        var closeEnough = Vector2.Distance(new Vector2(point.X, point.Y), center) <= Math.Max(16f, layout.Cell * 0.42f);
        intersection = new Point(nearestX, nearestY);
        return closeEnough;
    }

    private void DrawBackground()
    {
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(11, 13, 18));
        FillRect(new Rectangle(0, 0, VirtualScreen.Width, 150), new Color(24, 30, 40));
        FillRect(new Rectangle(0, 930, VirtualScreen.Width, 150), new Color(9, 28, 31));

        for (var i = 0; i < 18; i++)
        {
            var alpha = (byte)(50 - i * 2);
            DrawLine(new Vector2(-120, 180 + i * 42), new Vector2(2050, -40 + i * 64), 2, new Color((byte)56, (byte)86, (byte)96, alpha));
        }

        DrawGlow(new Vector2(1030, 90), 520, new Color(39, 122, 104, 80));
        DrawGlow(new Vector2(1700, 850), 360, new Color(144, 59, 48, 72));
    }

    private void DrawBoard(GoAppSession session, Point mousePoint)
    {
        var boardSize = session.BoardSize;
        var boardOuter = new Rectangle(54, 50, 980, 980);

        FillRect(new Rectangle(boardOuter.X + 18, boardOuter.Y + 22, boardOuter.Width, boardOuter.Height), new Color(0, 0, 0, 125));
        FillRect(boardOuter, new Color(66, 42, 28));
        FillRect(new Rectangle(boardOuter.X + 8, boardOuter.Y + 8, boardOuter.Width - 16, boardOuter.Height - 16), new Color(180, 126, 62));
        FillRect(BoardBounds, new Color(221, 166, 82));

        for (var i = 0; i < 24; i++)
        {
            var x = BoardBounds.X + i * 38;
            DrawLine(new Vector2(x, BoardBounds.Y), new Vector2(x + 220, BoardBounds.Bottom), 1, new Color(246, 196, 113, 42));
        }

        var layout = GetBoardLayout(boardSize);
        var start = layout.Start;
        var cell = layout.Cell;
        var end = new Vector2(BoardBounds.Right - BoardMargin, BoardBounds.Bottom - BoardMargin);

        for (var i = 0; i < boardSize; i++)
        {
            var p = start.X + cell * i;
            DrawLine(new Vector2(p, start.Y), new Vector2(p, end.Y), i == 0 || i == boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
            p = start.Y + cell * i;
            DrawLine(new Vector2(start.X, p), new Vector2(end.X, p), i == 0 || i == boardSize - 1 ? 4 : 2, new Color(42, 31, 24));
        }

        foreach (var star in GetStarPoints(boardSize))
        {
            var center = BoardPoint(start, cell, star.X, star.Y);
            DrawCircle(center, Math.Max(5, cell * 0.1f), new Color(55, 38, 25));
        }

        if (session.RenParseDisplayMode == RenParseDisplayMode.Graph)
        {
            DrawRenGraphStep1Overlay(session, start, cell);
        }
        else if (session.RenParseDisplayMode is RenParseDisplayMode.GraphStep2 or RenParseDisplayMode.Eye)
        {
            DrawRenGraphOverlay(session, start, cell, session.RenParseDisplayMode == RenParseDisplayMode.Eye);
        }
        else
        {
            DrawPlacedStones(session, start, cell);
            DrawRenParseOverlay(session, start, cell);
        }

        DrawSuperKoMarks(session, start, cell);
        DrawKoMark(session, start, cell);
        DrawHoverStone(session, mousePoint, cell);
        DrawBoardFrameHighlights(boardOuter);
    }

    private void DrawSidePanel(GoAppSession session, Point mousePoint)
    {
        var panel = new Rectangle(1102, 78, 760, 924);
        FillRect(new Rectangle(panel.X + 16, panel.Y + 18, panel.Width, panel.Height), new Color(0, 0, 0, 120));
        FillRect(panel, new Color(21, 25, 32, 236));
        DrawRect(panel, 2, new Color(82, 111, 114));

        if (session.CurrentMode.Kind == GoAppModeKind.Playing)
        {
            DrawPlayingSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.GameOver)
        {
            DrawGameOverSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing)
        {
            DrawBoardEditingSidePanel(session, mousePoint);
            return;
        }

        if (session.CurrentMode.Kind == GoAppModeKind.Reviewing)
        {
            DrawReviewingSidePanel(session, mousePoint);
            return;
        }

        DrawSetupSidePanel(session, mousePoint);
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

    private void DrawLocalClosedBox(Rectangle bounds)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 70));
        FillRect(bounds, new Color(17, 24, 29));
        DrawRect(bounds, 4, new Color(126, 150, 164));
        DrawMiniBoardGrid(new Rectangle(bounds.X + 22, bounds.Y + 20, bounds.Width - 44, bounds.Height - 40), new Color(88, 102, 112, 85));

        var left = new Vector2(bounds.X + 94, bounds.Y + 76);
        var right = new Vector2(bounds.X + 206, bounds.Y + 76);
        DrawLine(left, right, 5, new Color(99, 223, 185));
        DrawIconStone(left, 24, black: true);
        DrawIconStone(right, 24, black: false);
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

    private void DrawIconStone(Vector2 center, float radius, bool black)
    {
        DrawCircle(center, radius + 5, black ? new Color(178, 219, 226) : new Color(72, 80, 84));
        DrawStone(center, radius, black);
        if (black)
        {
            DrawCircle(new Vector2(center.X - radius * 0.28f, center.Y - radius * 0.32f), radius * 0.22f, new Color(255, 255, 255, 42));
        }
    }

    private void DrawMiniBoardGrid(Rectangle bounds, Color color)
    {
        for (var i = 0; i < 7; i++)
        {
            var x = bounds.X + i * bounds.Width / 6f;
            DrawLine(new Vector2(x, bounds.Y), new Vector2(x, bounds.Bottom), 1, color);
            var y = bounds.Y + i * bounds.Height / 6f;
            DrawLine(new Vector2(bounds.X, y), new Vector2(bounds.Right, y), 1, color);
        }
    }

    private void DrawCgosClientTopPanel(Point mousePoint)
    {
        var panel = new Rectangle(420, 172, 1080, 736);
        FillRect(new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height), new Color(0, 0, 0, 130));
        FillRect(panel, new Color(21, 25, 32, 242));
        DrawRect(panel, 2, new Color(82, 111, 114));

        DrawText("CGOS CLIENT", new Vector2(panel.X + 58, panel.Y + 58), new Color(255, 230, 160), 1.0f);
        DrawText("CONNECTION", new Vector2(panel.X + 62, panel.Y + 142), new Color(180, 195, 195), 0.54f);
        DrawInfoStrip(panel.X + 62, panel.Y + 204, "HOST", "uec-go.com:6809");
        DrawInfoStrip(panel.X + 62, panel.Y + 292, "STATUS", "GUI NOT CONNECTED");
        DrawInfoStrip(panel.X + 62, panel.Y + 380, "ROLE", "CGOS client area");
        DrawFittedText(
            "The CGOS communication program is currently separated as a console client. This screen is the new GUI entry point for CGOS-specific controls.",
            new Rectangle(panel.X + 62, panel.Y + 500, panel.Width - 124, 84),
            new Color(204, 211, 206),
            0.42f);
        DrawCommandButton(CgosBackButtonBounds, "BACK", false, mousePoint, scale: 0.56f);
    }

    private void DrawSetupSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("KIFUWARABE GO 2026", new Vector2(1142, 104), new Color(244, 238, 218), 1.0f);
        DrawText("TOURNAMENT", new Vector2(1144, 166), new Color(180, 195, 195), 0.5f);
        DrawLabeledBrowseSelector(TournamentRulesSelector with { Value = session.TournamentDisplayName }, mousePoint);

        DrawText("CURRENT RULES", new Vector2(1144, 294), new Color(180, 195, 195), 0.5f);
        DrawInfoStrip(1144, 334, "RULE", session.RuleKind.ToString());
        DrawInfoStrip(1144, 406, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 478, "KOMI", FormatKomi(session.Komi));
        DrawInfoStrip(1144, 550, "MOVES", FormatMoveLimit(session.MoveLimit));

        DrawInfoStrip(1144, 646, "BLACK", PlayerKindLabel(session.BlackPlayerKind));
        DrawPlayerKindButtons(session.BlackPlayerKind, mousePoint, BlackPlayerKindButtonY);
        DrawSetupEngineButtons(session, GoStone.Black, mousePoint, BlackEngineButtonY);
        DrawInfoStrip(1144, 780, "WHITE", PlayerKindLabel(session.WhitePlayerKind));
        DrawPlayerKindButtons(session.WhitePlayerKind, mousePoint, WhitePlayerKindButtonY);
        DrawSetupEngineButtons(session, GoStone.White, mousePoint, WhiteEngineButtonY);
        DrawCommandButton(ImportSgfButtonBounds, session.HasReviewGameRecord ? "SGF CLEAR" : "SGF INPUT", false, mousePoint);
        DrawCommandButton(StartReviewingButtonBounds, "KIFU REVIEW", false, mousePoint, enabled: session.HasReviewGameRecord, scale: 0.32f);
        DrawCommandButton(StartBoardEditingButtonBounds, "EDIT BOARD", false, mousePoint, scale: 0.36f);
        DrawCommandButton(StartPlayingButtonBounds, "START", false, mousePoint, scale: 0.48f);
    }

    private void DrawBoardEditingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("BOARD EDIT", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawInfoStrip(1144, 204, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 276, "BLACK", session.BlackStoneCount.ToString());
        DrawInfoStrip(1144, 348, "WHITE", session.WhiteStoneCount.ToString());

        DrawText("STONE", new Vector2(1144, 454), new Color(180, 195, 195), 0.56f);
        DrawCommandButton(BoardEditingBlackButtonBounds, "BLACK", session.BoardEditingStone == GoStone.Black, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingWhiteButtonBounds, "WHITE", session.BoardEditingStone == GoStone.White, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingEraseButtonBounds, "ERASE", session.BoardEditingStone == GoStone.Empty, mousePoint, scale: 0.5f);
        DrawCommandButton(BoardEditingUndoButtonBounds, "UNDO", false, mousePoint, enabled: session.CanUndoBoardEditing, scale: 0.5f);
        DrawCommandButton(BoardEditingRedoButtonBounds, "REDO", false, mousePoint, enabled: session.CanRedoBoardEditing, scale: 0.5f);

        DrawText("CURRENT POSITION", new Vector2(1144, 636), new Color(180, 195, 195), 0.52f);
        DrawStoneCountStrip(session, 676);
        DrawCommandButton(BoardEditingExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.52f);
        DrawCommandButton(BoardEditingDoneButtonBounds, "DONE", false, mousePoint);
    }

    private void DrawReviewingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("KIFU REVIEW", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawInfoStrip(1144, 204, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 276, "MOVE", $"{session.ReviewMoveIndex} / {session.ReviewMoveCount}");
        DrawInfoStrip(1144, 348, "TURN", session.CurrentTurn == GoStone.Black ? "BLACK" : "WHITE");

        DrawText("STEP", new Vector2(1144, 454), new Color(180, 195, 195), 0.56f);
        for (var i = 0; i < ReviewStepButtonValues.Length; i++)
        {
            var step = ReviewStepButtonValues[i];
            var enabled = step < 0 ? session.ReviewMoveIndex > 0 : session.ReviewMoveIndex < session.ReviewMoveCount;
            DrawCommandButton(ReviewStepButtonBounds(i), step > 0 ? $"+{step}" : step.ToString(), false, mousePoint, enabled, 0.42f);
        }

        DrawText("Push R key:", new Vector2(1144, 636), new Color(180, 195, 195), 0.46f);
        DrawReviewRenParseModeStrip(session, mousePoint);

        DrawText("CURRENT POSITION", new Vector2(1144, 760), new Color(180, 195, 195), 0.52f);
        DrawStoneCountStrip(session, 800);
        DrawCommandButton(ReviewDoneButtonBounds, "USE POSITION", false, mousePoint, scale: 0.52f);
    }

    private void DrawPlayingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("TURN", new Vector2(1144, 132), new Color(180, 195, 195), 0.62f);

        var turnLabel = session.CurrentTurn == GoStone.Black ? "BLACK" : "WHITE";
        DrawInfoStrip(1144, 180, "TURN", turnLabel);
        DrawInfoStrip(1144, 244, "NEXT", GetMoveThinkingText(session));

        DrawText("PLAYERS", new Vector2(1144, 300), new Color(180, 195, 195), 0.62f);
        DrawInfoStrip(1144, 348, "BLACK", PlayerKindLabel(session.BlackPlayerKind));
        DrawInfoStrip(1144, 446, "WHITE", PlayerKindLabel(session.WhitePlayerKind));

        DrawText("TIME", new Vector2(1144, 544), new Color(180, 195, 195), 0.62f);
        DrawTimeStrip(session, 592);

        DrawText("AGEHAMA", new Vector2(1144, 688), new Color(180, 195, 195), 0.52f);
        DrawAgehamaStrip(session, 728);

        DrawText("PURE GO SCORE", new Vector2(1144, 792), new Color(180, 195, 195), 0.52f);
        DrawStoneCountStrip(session, 826);

        if (HasComputerPlayer(session))
        {
            DrawText("ENGINE", new Vector2(1488, 544), new Color(180, 195, 195), 0.46f);
            DrawText(GetEngineStatusText(session), new Vector2(1632, 544), GetEngineStatusColor(session), 0.46f);
        }

        if (session.CanAcceptHumanMove)
        {
            DrawCommandButton(PassButtonBounds, "PASS", false, mousePoint);
            DrawCommandButton(ResignButtonBounds, "RESIGN", false, mousePoint);
            return;
        }

        DrawCommandButton(CancelPlayingButtonBounds, "CANCEL", false, mousePoint);
    }

    private void DrawGameOverSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("GAME OVER", new Vector2(1144, 132), new Color(255, 230, 160), 0.9f);
        DrawText(FormatGameEndMoveCount(session.PlayedMoveCount), new Vector2(1144, 196), new Color(99, 223, 185), 0.58f);

        var result = string.IsNullOrWhiteSpace(session.GameOverReason) ? "GAME OVER" : session.GameOverReason;
        var winnerLabel = session.Winner is { } winner
            ? winner == GoStone.Black ? "BLACK WINS" : "WHITE WINS"
            : "NO WINNER";

        var resultSection = new Rectangle(1144, 236, 668, 176);
        DrawResultSection(resultSection, "RESULT");
        DrawResultRow(new Rectangle(1164, 290, 628, 44), "END", result, new Color(80, 48, 38), Color.White);
        DrawResultRow(new Rectangle(1164, 346, 628, 44), "WINNER", winnerLabel, new Color(39, 68, 65), new Color(99, 223, 185));

        var scoreSection = new Rectangle(1144, 436, 668, 236);
        DrawResultSection(scoreSection, "SCORE");
        DrawStoneCountStrip(session, 494);
        DrawAgehamaStrip(session, 604);

        var tournamentSection = new Rectangle(1144, 696, 668, 134);
        DrawResultSection(tournamentSection, "TOURNAMENT");
        DrawResultRow(new Rectangle(1164, 750, 628, 56), "RULES", session.TournamentDisplayName, new Color(39, 68, 65), Color.White);

        var actionSection = new Rectangle(1144, 854, 668, 126);
        DrawResultSection(actionSection, "ACTION");
        DrawCommandButton(ExportSgfButtonBounds, "SGF OUTPUT", false, mousePoint, scale: 0.52f);
        DrawCommandButton(ReturnToSetupButtonBounds, "RULE SETUP", false, mousePoint);
    }

    private void DrawTournamentRulesSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesSelectionDialogOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(TournamentRulesSelectionDialogBounds.X + 18, TournamentRulesSelectionDialogBounds.Y + 20, TournamentRulesSelectionDialogBounds.Width, TournamentRulesSelectionDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(TournamentRulesSelectionDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(TournamentRulesSelectionDialogBounds, 2, new Color(116, 145, 146));

        DrawText("TOURNAMENT RULES", new Vector2(TournamentRulesSelectionDialogBounds.X + 30, TournamentRulesSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(TournamentRulesSelectionDialogCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.42f);

        DrawText("LIST", new Vector2(TournamentRulesSelectionDialogListBounds.X, TournamentRulesSelectionDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawText("PROPERTIES", new Vector2(TournamentRulesSelectionDialogPropertyBounds.X, TournamentRulesSelectionDialogPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);

        FillRect(TournamentRulesSelectionDialogListBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesSelectionDialogListBounds, 1, new Color(67, 84, 92));

        var startIndex = session.TournamentRulesSelectionPageIndex * GoAppSession.TournamentRulesSelectionPageSize;
        for (var i = 0; i < GoAppSession.TournamentRulesSelectionPageSize; i++)
        {
            var index = startIndex + i;
            if (index >= session.TournamentRulesList.Count)
            {
                break;
            }

            DrawTournamentRulesSelectionListItem(TournamentRulesSelectionDialogListItemBounds(i), session, index, mousePoint);
        }

        DrawTournamentRulesSelectionProperties(session, mousePoint);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.TournamentRulesList.Count / (double)GoAppSession.TournamentRulesSelectionPageSize));
        DrawCommandButton(TournamentRulesSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.TournamentRulesSelectionPageIndex + 1} / {pageCount}", new Vector2(TournamentRulesSelectionDialogBounds.X + 350, TournamentRulesSelectionDialogBounds.Bottom - 62), new Color(227, 224, 210), 0.48f);
        DrawCommandButton(TournamentRulesSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogAddButtonBounds, "ADD", false, mousePoint, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogEditButtonBounds, "EDIT", false, mousePoint, enabled: session.TournamentRulesList.Count > 0, scale: 0.42f);
        DrawCommandButton(TournamentRulesSelectionDialogDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.TournamentRulesList.Count > 0, scale: 0.34f);
        DrawCommandButton(TournamentRulesSelectionDialogDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedTournamentRules, scale: 0.42f);
        DrawTournamentRulesDeleteConfirmation(session, mousePoint);
    }

    private void DrawTournamentRulesAddPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesAddPanelOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(TournamentRulesAddPanelBounds.X + 18, TournamentRulesAddPanelBounds.Y + 20, TournamentRulesAddPanelBounds.Width, TournamentRulesAddPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(TournamentRulesAddPanelBounds, new Color(19, 24, 31, 248));
        DrawRect(TournamentRulesAddPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsTournamentRulesEditPanelMode ? "EDIT TOURNAMENT RULES" : "ADD TOURNAMENT RULES", new Vector2(TournamentRulesAddPanelBounds.X + 30, TournamentRulesAddPanelBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(TournamentRulesAddPanelCloseButtonBounds, "BACK", false, mousePoint, scale: 0.42f);

        FillRect(TournamentRulesAddPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesAddPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawDisplayNameTextBox(session, mousePoint);
        DrawText("RULE", new Vector2(AddPanelControlX, 324), new Color(180, 195, 195), 0.5f);
        DrawRuleKindButtons(session.RuleKind, mousePoint);
        DrawText($"BOARD {session.BoardSize} x {session.BoardSize}", new Vector2(AddPanelControlX, 414), new Color(99, 223, 185), 0.62f);
        DrawBoardSizeButtons(session.BoardSize, mousePoint, AddPanelBoardSizeButtonY);
        DrawRulesNumberStrip(AddPanelControlX, 508, "KOMI", FormatKomi(session.Komi), KomiStepButtonBounds(0), "-0.5", KomiStepButtonBounds(1), "+0.5", mousePoint);
        DrawRulesNumberStrip(AddPanelControlX, 572, "TIME", FormatMainTime(session.MainTime), MainTimeStepButtonBounds(0), "-1m", MainTimeStepButtonBounds(1), "+1m", mousePoint);
        DrawRulesNumberStrip(AddPanelControlX, 636, "MOVES", FormatMoveLimit(session.MoveLimit), MoveLimitStepButtonBounds(0), "-10", MoveLimitStepButtonBounds(1), "+10", mousePoint);
        DrawFilePathSelector(session, mousePoint);
        DrawCommandButton(SaveTournamentRulesButtonBounds, SaveTournamentRulesLabel(session), false, mousePoint);
    }

    private void DrawDisplayNameTextBox(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesAddPanelDisplayNameRowBounds;
        var hovered = bounds.Contains(mousePoint);
        var active = session.IsTournamentRulesDisplayNameEditing;
        var displayName = active ? session.TournamentRulesDisplayNameDraft : session.TournamentDisplayName;

        DrawDataRowFrame(bounds, active, hovered);
        DrawUiLabel(UiLabel.InCompactRow("DISPLAY", bounds));

        var textBounds = TournamentRulesAddPanelDisplayNameTextBounds;
        DrawFittedText(displayName, textBounds, Color.White, 0.46f);
        if (active)
        {
            DrawTextBoxCaret(displayName, session.TournamentRulesDisplayNameCaretIndex, textBounds, 0.46f);
        }

        if (!string.IsNullOrWhiteSpace(session.TournamentRulesDisplayNameWarning))
        {
            DrawFittedText(session.TournamentRulesDisplayNameWarning, new Rectangle(bounds.X, bounds.Bottom + 8, bounds.Width, 28), new Color(255, 183, 146), 0.34f);
        }
    }

    private void DrawFilePathSelector(GoAppSession session, Point mousePoint)
    {
        var bounds = TournamentRulesAddPanelFileRowBounds;
        var filePath = string.IsNullOrWhiteSpace(session.CurrentTournamentRules.FilePath) ? "-" : session.CurrentTournamentRules.FilePath;
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow("FILE", bounds));
        DrawFittedText(filePath, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 282, 42), Color.White, 0.38f);
        DrawCommandButton(TournamentRulesAddPanelFileBrowseButtonBounds, "REF", false, mousePoint, scale: 0.34f);
    }

    private void DrawTextBoxCaret(string text, int caretIndex, Rectangle textBounds, float textScale)
    {
        var clampedCaretIndex = Math.Clamp(caretIndex, 0, text.Length);
        var prefix = text[..clampedCaretIndex];
        var measuredText = _font.MeasureString(text);
        var fittedScale = MathF.Min(textScale, MathF.Min(textBounds.Width / Math.Max(1f, measuredText.X), textBounds.Height / Math.Max(1f, measuredText.Y)));
        var x = textBounds.X + MathF.Min(textBounds.Width - 2, _font.MeasureString(prefix).X * fittedScale);
        DrawLine(new Vector2(x, textBounds.Y + 5), new Vector2(x, textBounds.Bottom - 5), 2, new Color(147, 244, 200));
    }

    private int GetTextBoxCaretIndex(int pointX, string text, Rectangle textBounds, float textScale)
    {
        if (string.IsNullOrEmpty(text) || pointX <= textBounds.X)
        {
            return 0;
        }

        var measuredText = _font.MeasureString(text);
        var fittedScale = MathF.Min(textScale, MathF.Min(textBounds.Width / Math.Max(1f, measuredText.X), textBounds.Height / Math.Max(1f, measuredText.Y)));
        var previousX = (float)textBounds.X;
        for (var i = 0; i < text.Length; i++)
        {
            var nextX = textBounds.X + MathF.Min(textBounds.Width - 2, _font.MeasureString(text[..(i + 1)]).X * fittedScale);
            if (pointX < (previousX + nextX) * 0.5f)
            {
                return i;
            }

            previousX = nextX;
        }

        return text.Length;
    }

    private void DrawTournamentRulesSelectionListItem(Rectangle bounds, GoAppSession session, int index, Point mousePoint)
    {
        var rules = session.TournamentRulesList[index];
        var selected = index == session.SelectedTournamentRulesIndex;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
        DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 16), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        DrawFittedText(rules.DisplayName, new Rectangle(bounds.X + 62, bounds.Y + 6, bounds.Width - 82, 32), Color.White, 0.5f);
        DrawText($"{rules.Rule}  {rules.BoardSize}x{rules.BoardSize}  KOMI {FormatKomi(rules.Komi)}", new Vector2(bounds.X + 62, bounds.Y + 42), new Color(204, 211, 206), 0.34f);
    }

    private void DrawTournamentRulesSelectionProperties(GoAppSession session, Point mousePoint)
    {
        FillRect(TournamentRulesSelectionDialogPropertyBounds, new Color(15, 20, 26));
        DrawRect(TournamentRulesSelectionDialogPropertyBounds, 1, new Color(67, 84, 92));

        if (session.SelectedTournamentRulesIndex < 0 || session.SelectedTournamentRulesIndex >= session.TournamentRulesList.Count)
        {
            DrawText("NO RULES", new Vector2(TournamentRulesSelectionDialogPropertyBounds.X + 24, TournamentRulesSelectionDialogPropertyBounds.Y + 24), Color.White, 0.5f);
            return;
        }

        var rules = session.TournamentRulesList[session.SelectedTournamentRulesIndex];
        var y = TournamentRulesSelectionDialogPropertyBounds.Y + 22;
        DrawPropertyRow(y, "NAME", rules.DisplayName);
        DrawPropertyRow(y + 70, "RULE", rules.Rule.ToString());
        DrawPropertyRow(y + 140, "BOARD", $"{rules.BoardSize} x {rules.BoardSize}");
        DrawPropertyRow(y + 210, "KOMI", FormatKomi(rules.Komi));
        DrawPropertyRow(y + 280, "TIME", FormatMainTime(rules.MainTime));
        DrawPropertyRow(y + 350, "MOVES", FormatMoveLimit(rules.MoveLimit));
        var filePath = string.IsNullOrWhiteSpace(rules.FilePath) ? "-" : rules.FilePath;
        var fileRowBounds = TournamentRulesSelectionDialogPropertyRowBounds(6);
        DrawPathPropertyRow(fileRowBounds, "FILE", string.IsNullOrWhiteSpace(rules.FilePath) ? "-" : Path.GetFileName(rules.FilePath));
        DrawPathTooltipIfHovered(fileRowBounds, filePath, mousePoint);
    }

    private void DrawTournamentRulesDeleteConfirmation(GoAppSession session, Point mousePoint)
    {
        if (!session.IsTournamentRulesDeleteConfirmationOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(TournamentRulesDeleteConfirmationBounds.X + 12, TournamentRulesDeleteConfirmationBounds.Y + 14, TournamentRulesDeleteConfirmationBounds.Width, TournamentRulesDeleteConfirmationBounds.Height), new Color(0, 0, 0, 150));
        FillRect(TournamentRulesDeleteConfirmationBounds, new Color(24, 29, 36, 252));
        DrawRect(TournamentRulesDeleteConfirmationBounds, 2, new Color(255, 183, 146));

        DrawText("DELETE RULES FILE", new Vector2(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 24), new Color(255, 230, 160), 0.62f);
        DrawFittedText($"{session.TournamentRulesDeleteConfirmationFileName} file will be deleted.", new Rectangle(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 92, TournamentRulesDeleteConfirmationBounds.Width - 56, 42), Color.White, 0.5f);
        DrawText("DELETE?", new Vector2(TournamentRulesDeleteConfirmationBounds.X + 28, TournamentRulesDeleteConfirmationBounds.Y + 150), new Color(180, 195, 195), 0.46f);
        DrawCommandButton(TournamentRulesDeleteConfirmationCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.42f);
        DrawCommandButton(TournamentRulesDeleteConfirmationConfirmButtonBounds, "DELETE", false, mousePoint, scale: 0.42f);
    }

    private void DrawGtpEngineSelectionDialog(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineSelectionDialogOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(GtpEngineSelectionDialogBounds.X + 18, GtpEngineSelectionDialogBounds.Y + 20, GtpEngineSelectionDialogBounds.Width, GtpEngineSelectionDialogBounds.Height), new Color(0, 0, 0, 145));
        FillRect(GtpEngineSelectionDialogBounds, new Color(19, 24, 31, 248));
        DrawRect(GtpEngineSelectionDialogBounds, 2, new Color(116, 145, 146));

        var target = session.GtpEngineSelectionTargetStone == GoStone.Black ? "BLACK" : "WHITE";
        DrawText($"GTP ENGINE SELECT  {target}", new Vector2(GtpEngineSelectionDialogBounds.X + 30, GtpEngineSelectionDialogBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(GtpEngineSelectionDialogCloseButtonBounds, "CLOSE", false, mousePoint, scale: 0.42f);

        DrawText("LIST", new Vector2(GtpEngineSelectionDialogListBounds.X, GtpEngineSelectionDialogListBounds.Y - 34), new Color(180, 195, 195), 0.46f);
        DrawText("PROPERTIES", new Vector2(GtpEngineSelectionDialogPropertyBounds.X, GtpEngineSelectionDialogPropertyBounds.Y - 34), new Color(180, 195, 195), 0.46f);

        FillRect(GtpEngineSelectionDialogListBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogListBounds, 1, new Color(67, 84, 92));

        var startIndex = session.GtpEngineSelectionPageIndex * GoAppSession.GtpEngineSelectionPageSize;
        for (var i = 0; i < GoAppSession.GtpEngineSelectionPageSize; i++)
        {
            var index = startIndex + i;
            if (index >= session.GtpEngineProfiles.Count)
            {
                break;
            }

            DrawGtpEngineSelectionListItem(GtpEngineSelectionDialogListItemBounds(i), session, index, mousePoint);
        }

        DrawGtpEngineSelectionProperties(session, mousePoint);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.GtpEngineProfiles.Count / (double)GoAppSession.GtpEngineSelectionPageSize));
        DrawCommandButton(GtpEngineSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.GtpEngineSelectionPageIndex + 1} / {pageCount}", new Vector2(GtpEngineSelectionDialogBounds.X + 350, GtpEngineSelectionDialogBounds.Bottom - 62), new Color(227, 224, 210), 0.48f);
        DrawCommandButton(GtpEngineSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex < pageCount - 1, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogAddButtonBounds, "ADD", false, mousePoint, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogEditButtonBounds, "EDIT", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.42f);
        DrawCommandButton(GtpEngineSelectionDialogDuplicateButtonBounds, "DUPLICATE", false, mousePoint, enabled: session.GtpEngineProfiles.Count > 0, scale: 0.32f);
        DrawCommandButton(GtpEngineSelectionDialogDeleteButtonBounds, "DELETE", false, mousePoint, enabled: session.CanDeleteSelectedGtpEngine, scale: 0.42f);
        DrawGtpEngineDeleteConfirmation(session, mousePoint);
    }

    private void DrawGtpEngineEditPanel(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineEditPanelOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 105));
        FillRect(new Rectangle(GtpEngineEditPanelBounds.X + 18, GtpEngineEditPanelBounds.Y + 20, GtpEngineEditPanelBounds.Width, GtpEngineEditPanelBounds.Height), new Color(0, 0, 0, 145));
        FillRect(GtpEngineEditPanelBounds, new Color(19, 24, 31, 248));
        DrawRect(GtpEngineEditPanelBounds, 2, new Color(116, 145, 146));

        DrawText(session.IsGtpEngineAddPanelMode ? "ADD GTP ENGINE" : "EDIT GTP ENGINE", new Vector2(GtpEngineEditPanelBounds.X + 30, GtpEngineEditPanelBounds.Y + 24), new Color(244, 238, 218), 0.78f);
        DrawCommandButton(GtpEngineEditPanelCloseButtonBounds, "BACK", false, mousePoint, scale: 0.42f);

        FillRect(GtpEngineEditPanelEditorBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineEditPanelEditorBounds, 1, new Color(67, 84, 92));

        DrawGtpEngineEditField(session, GtpEngineProfileEditField.DisplayName, "DISPLAY", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.ExecutablePath, "EXE", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.WorkingDirectory, "WORKDIR", mousePoint);
        DrawGtpEngineEditField(session, GtpEngineProfileEditField.Arguments, "ARGS", mousePoint);

        var logBounds = GtpEngineEditPanelLogRowBounds;
        DrawDataRowFrame(logBounds);
        DrawUiLabel(UiLabel.InCompactRow("GTP LOG", logBounds));
        DrawCommandButton(GtpEngineEditPanelLogButtonBounds, session.GtpEngineEditDraft.EnableGtpLog ? "ON" : "OFF", session.GtpEngineEditDraft.EnableGtpLog, mousePoint, scale: 0.42f);

        if (!string.IsNullOrWhiteSpace(session.GtpEngineEditWarning))
        {
            DrawFittedText(session.GtpEngineEditWarning, new Rectangle(GtpEngineEditPanelEditorBounds.X + 48, GtpEngineEditPanelEditorBounds.Bottom - 74, GtpEngineEditPanelEditorBounds.Width - 96, 34), new Color(255, 183, 146), 0.38f);
        }

        DrawCommandButton(GtpEngineEditPanelSaveButtonBounds, SaveGtpEngineLabel(session), false, mousePoint);
    }

    private void DrawGtpEngineEditField(GoAppSession session, GtpEngineProfileEditField field, string label, Point mousePoint)
    {
        var bounds = GtpEngineEditPanelFieldRowBounds(field);
        var active = session.ActiveGtpEngineEditField == field;
        var hovered = bounds.Contains(mousePoint);
        var text = session.GetGtpEngineEditFieldText(field);
        DrawDataRowFrame(bounds, active, hovered);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));

        var textBounds = GtpEngineEditPanelFieldTextBounds(field);
        DrawFittedText(string.IsNullOrEmpty(text) ? "-" : text, textBounds, Color.White, 0.42f);
        if (active)
        {
            DrawTextBoxCaret(text, session.GtpEngineEditCaretIndex, textBounds, 0.42f);
        }

        if (field == GtpEngineProfileEditField.ExecutablePath)
        {
            DrawCommandButton(GtpEngineEditPanelFileBrowseButtonBounds, "REF", false, mousePoint, scale: 0.34f);
        }

        if (field == GtpEngineProfileEditField.WorkingDirectory)
        {
            DrawCommandButton(GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds, "REF", false, mousePoint, scale: 0.34f);
        }
    }

    private void DrawGtpEngineDeleteConfirmation(GoAppSession session, Point mousePoint)
    {
        if (!session.IsGtpEngineDeleteConfirmationOpen)
        {
            return;
        }

        FillRect(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 95));
        FillRect(new Rectangle(GtpEngineDeleteConfirmationBounds.X + 12, GtpEngineDeleteConfirmationBounds.Y + 14, GtpEngineDeleteConfirmationBounds.Width, GtpEngineDeleteConfirmationBounds.Height), new Color(0, 0, 0, 150));
        FillRect(GtpEngineDeleteConfirmationBounds, new Color(24, 29, 36, 252));
        DrawRect(GtpEngineDeleteConfirmationBounds, 2, new Color(255, 183, 146));

        DrawText("DELETE GTP ENGINE", new Vector2(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 24), new Color(255, 230, 160), 0.62f);
        DrawFittedText($"{session.GtpEngineDeleteConfirmationName} will be removed from the list.", new Rectangle(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 92, GtpEngineDeleteConfirmationBounds.Width - 56, 42), Color.White, 0.5f);
        DrawText("DELETE?", new Vector2(GtpEngineDeleteConfirmationBounds.X + 28, GtpEngineDeleteConfirmationBounds.Y + 150), new Color(180, 195, 195), 0.46f);
        DrawCommandButton(GtpEngineDeleteConfirmationCancelButtonBounds, "CANCEL", false, mousePoint, scale: 0.42f);
        DrawCommandButton(GtpEngineDeleteConfirmationConfirmButtonBounds, "DELETE", false, mousePoint, scale: 0.42f);
    }

    private void DrawGtpEngineSelectionListItem(Rectangle bounds, GoAppSession session, int index, Point mousePoint)
    {
        var profile = session.GtpEngineProfiles[index];
        var selectedIndex = session.GtpEngineSelectionTargetStone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        var selected = index == selectedIndex;
        var hovered = bounds.Contains(mousePoint);
        FillRect(bounds, selected ? new Color(38, 103, 86) : hovered ? new Color(43, 52, 62) : new Color(24, 31, 37));
        DrawRect(bounds, 1, selected ? new Color(147, 244, 200) : new Color(70, 85, 94));
        DrawText($"{index + 1:00}", new Vector2(bounds.X + 14, bounds.Y + 16), selected ? new Color(177, 255, 215) : new Color(180, 195, 195), 0.4f);
        DrawFittedText(profile.DisplayName, new Rectangle(bounds.X + 62, bounds.Y + 6, bounds.Width - 82, 32), Color.White, 0.5f);
        DrawFittedText(string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "-" : profile.ExecutablePath, new Rectangle(bounds.X + 62, bounds.Y + 40, bounds.Width - 82, 24), new Color(204, 211, 206), 0.34f);
    }

    private void DrawGtpEngineSelectionProperties(GoAppSession session, Point mousePoint)
    {
        FillRect(GtpEngineSelectionDialogPropertyBounds, new Color(15, 20, 26));
        DrawRect(GtpEngineSelectionDialogPropertyBounds, 1, new Color(67, 84, 92));

        var selectedIndex = session.GtpEngineSelectionTargetStone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        if (selectedIndex < 0 || selectedIndex >= session.GtpEngineProfiles.Count)
        {
            DrawText("NO ENGINE", new Vector2(GtpEngineSelectionDialogPropertyBounds.X + 24, GtpEngineSelectionDialogPropertyBounds.Y + 24), Color.White, 0.5f);
            return;
        }

        var profile = session.GtpEngineProfiles[selectedIndex];
        var y = GtpEngineSelectionDialogPropertyBounds.Y + 22;
        DrawGtpEnginePropertyRow(y, "NAME", profile.DisplayName);
        var executablePath = string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "-" : profile.ExecutablePath;
        var workingDirectory = string.IsNullOrWhiteSpace(profile.WorkingDirectory) ? "-" : profile.WorkingDirectory;
        var executablePathRowBounds = GtpEngineSelectionDialogPropertyRowBounds(1);
        var workingDirectoryRowBounds = GtpEngineSelectionDialogPropertyRowBounds(2);

        DrawPathPropertyRow(executablePathRowBounds, "EXE", executablePath);
        DrawPathPropertyRow(workingDirectoryRowBounds, "WORKDIR", workingDirectory);
        DrawGtpEnginePropertyRow(y + 210, "ARGS", string.IsNullOrWhiteSpace(profile.Arguments) ? "-" : profile.Arguments);
        DrawGtpEnginePropertyRow(y + 280, "GTP LOG", profile.EnableGtpLog ? "ON" : "OFF");

        DrawPathTooltipIfHovered(executablePathRowBounds, executablePath, mousePoint);
        DrawPathTooltipIfHovered(workingDirectoryRowBounds, workingDirectory, mousePoint);
    }

    private void DrawGtpEnginePropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(GtpEngineSelectionDialogPropertyBounds.X + 18, y, GtpEngineSelectionDialogPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(TournamentRulesSelectionDialogPropertyBounds.X + 18, y, TournamentRulesSelectionDialogPropertyBounds.Width - 36, 52);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPathPropertyRow(Rectangle bounds, string label, string value)
    {
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InCompactRow(label, bounds));
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPathTooltipIfHovered(Rectangle rowBounds, string fullPath, Point mousePoint)
    {
        if (string.IsNullOrWhiteSpace(fullPath) || fullPath == "-")
        {
            return;
        }

        var popupBounds = PathTooltipBounds(rowBounds);
        if (rowBounds.Contains(mousePoint) || popupBounds.Contains(mousePoint))
        {
            DrawPathTooltip(popupBounds, fullPath, mousePoint);
        }
    }

    private void DrawBoardSizeButtons(int boardSize, Point mousePoint, int y)
    {
        var labels = new[] { "9 x 9", "13 x 13", "19 x 19" };
        var sizes = new[] { 9, 13, 19 };
        for (var i = 0; i < labels.Length; i++)
        {
            var bounds = BoardSizeButtonBounds(i, y);
            var selected = boardSize == sizes[i];
            var hovered = bounds.Contains(mousePoint);
            FillRect(bounds, selected ? new Color(39, 125, 97) : hovered ? new Color(50, 62, 72) : new Color(32, 38, 47));
            DrawRect(bounds, 2, selected ? new Color(147, 244, 200) : new Color(88, 102, 112));

            var size = _font.MeasureString(labels[i]) * 0.7f;
            DrawText(labels[i], new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), Color.White, 0.7f);
        }
    }

    private void DrawRuleKindButtons(GoRuleKind selectedKind, Point mousePoint)
    {
        DrawCommandButton(RuleKindButtonBounds(0), "JAPANESE", selectedKind == GoRuleKind.Japanese, mousePoint, scale: 0.44f);
        DrawCommandButton(RuleKindButtonBounds(1), "PURE GO", selectedKind == GoRuleKind.PureGo, mousePoint, scale: 0.44f);
        DrawCommandButton(RuleKindButtonBounds(2), "CHINESE", selectedKind == GoRuleKind.Chinese, mousePoint, scale: 0.44f);
    }

    private void DrawRulesNumberStrip(int x, int y, string label, string value, Rectangle minusBounds, string minusLabel, Rectangle plusBounds, string plusLabel, Point mousePoint)
    {
        var bounds = new Rectangle(x, y, 668, 56);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InRow(label, bounds));
        DrawText(value, new Vector2(bounds.X + 176, bounds.Y + 13), Color.White, 0.52f);
        DrawCommandButton(minusBounds, minusLabel, false, mousePoint, scale: 0.42f);
        DrawCommandButton(plusBounds, plusLabel, false, mousePoint, scale: 0.42f);
    }

    private void DrawPlayerKindButtons(GoPlayerKind selectedKind, Point mousePoint, int y)
    {
        var humanBounds = PlayerKindButtonBounds(0, y);
        var computerBounds = PlayerKindButtonBounds(1, y);
        var bounds = PlayerKindSegmentBounds(y);

        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, 90));
        FillRect(bounds, new Color(33, 43, 52));
        DrawSegmentedPlayerKindButton(humanBounds, "HUMAN", selectedKind == GoPlayerKind.Human, humanBounds.Contains(mousePoint));
        DrawSegmentedPlayerKindButton(computerBounds, "COMPUTER", selectedKind == GoPlayerKind.Computer, computerBounds.Contains(mousePoint));
        DrawRect(bounds, 2, new Color(126, 150, 164));
    }

    private void DrawSegmentedPlayerKindButton(Rectangle bounds, string label, bool selected, bool hovered)
    {
        var fill = selected ? new Color(31, 151, 112) : hovered ? new Color(44, 59, 70) : new Color(33, 43, 52);
        var textColor = selected ? Color.White : new Color(202, 213, 211);
        FillRect(bounds, fill);

        var measured = _font.MeasureString(label);
        var fittedScale = MathF.Min(0.52f, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
    }

    private void DrawSetupEngineButtons(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var playerKind = stone == GoStone.Black ? session.BlackPlayerKind : session.WhitePlayerKind;
        if (playerKind != GoPlayerKind.Computer)
        {
            return;
        }

        var selectedIndex = stone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        var engineName = selectedIndex >= 0 && selectedIndex < session.GtpEngineProfiles.Count
            ? session.GtpEngineProfiles[selectedIndex].DisplayName
            : "No engine";
        DrawLabeledBrowseSelector(GtpEngineSelectorBounds(y) with { Value = engineName }, mousePoint);
    }

    private const int AddPanelControlX = 626;

    private const int AddPanelBoardSizeButtonY = 452;

    private const int BlackPlayerKindButtonY = 660;

    private const int WhitePlayerKindButtonY = 794;

    private const int BlackEngineButtonY = 724;

    private const int WhiteEngineButtonY = 856;

    private static Rectangle BoardSizeButtonBounds(int index, int y) => new(AddPanelControlX + index * 224, y, 188, 62);

    private static LabeledBrowseSelector TournamentRulesSelector => new(new Rectangle(1144, 198, 668, 56), "RULES", "");

    private static Rectangle TournamentRulesSelectionDialogBounds => new(230, 126, 1460, 820);

    private static Rectangle TournamentRulesSelectionDialogListBounds => new(270, 242, 650, 560);

    private static Rectangle TournamentRulesSelectionDialogPropertyBounds => new(950, 242, 700, 560);

    private static Rectangle TournamentRulesSelectionDialogCloseButtonBounds => new(1518, 156, 132, 48);

    private static Rectangle TournamentRulesSelectionDialogAddButtonBounds => new(958, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogEditButtonBounds => new(1128, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogDuplicateButtonBounds => new(1298, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogDeleteButtonBounds => new(1468, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogPreviousPageButtonBounds => new(270, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogNextPageButtonBounds => new(770, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogListItemBounds(int index) =>
        new(TournamentRulesSelectionDialogListBounds.X + 16, TournamentRulesSelectionDialogListBounds.Y + 16 + index * 88, TournamentRulesSelectionDialogListBounds.Width - 32, 72);

    private static Rectangle TournamentRulesSelectionDialogPropertyRowBounds(int index) =>
        new(TournamentRulesSelectionDialogPropertyBounds.X + 18, TournamentRulesSelectionDialogPropertyBounds.Y + 22 + index * 70, TournamentRulesSelectionDialogPropertyBounds.Width - 36, 52);

    private static Rectangle TournamentRulesDeleteConfirmationBounds => new(640, 390, 640, 260);

    private static Rectangle TournamentRulesDeleteConfirmationCancelButtonBounds =>
        new(TournamentRulesDeleteConfirmationBounds.X + 300, TournamentRulesDeleteConfirmationBounds.Bottom - 76, 140, 48);

    private static Rectangle TournamentRulesDeleteConfirmationConfirmButtonBounds =>
        new(TournamentRulesDeleteConfirmationBounds.X + 464, TournamentRulesDeleteConfirmationBounds.Bottom - 76, 140, 48);

    private static Rectangle TournamentRulesAddPanelBounds => new(430, 126, 1060, 820);

    private static Rectangle TournamentRulesAddPanelEditorBounds => new(520, 228, 880, 590);

    private static Rectangle TournamentRulesAddPanelCloseButtonBounds => new(1318, 156, 132, 48);

    private static Rectangle TournamentRulesAddPanelDisplayNameRowBounds => new(AddPanelControlX, 244, 668, 56);

    private static Rectangle TournamentRulesAddPanelDisplayNameTextBounds =>
        new(TournamentRulesAddPanelDisplayNameRowBounds.X + 152, TournamentRulesAddPanelDisplayNameRowBounds.Y + 7, TournamentRulesAddPanelDisplayNameRowBounds.Width - 168, 42);

    private static Rectangle TournamentRulesAddPanelFileRowBounds => new(AddPanelControlX, 710, 668, 56);

    private static Rectangle TournamentRulesAddPanelFileBrowseButtonBounds => new(TournamentRulesAddPanelFileRowBounds.Right - 112, TournamentRulesAddPanelFileRowBounds.Y + 8, 96, 40);

    private static Rectangle GtpEngineSelectionDialogBounds => new(230, 126, 1460, 820);

    private static Rectangle GtpEngineSelectionDialogListBounds => new(270, 242, 650, 560);

    private static Rectangle GtpEngineSelectionDialogPropertyBounds => new(950, 242, 700, 560);

    private static Rectangle GtpEngineSelectionDialogCloseButtonBounds => new(1518, 156, 132, 48);

    private static Rectangle GtpEngineSelectionDialogPreviousPageButtonBounds => new(270, 854, 150, 52);

    private static Rectangle GtpEngineSelectionDialogNextPageButtonBounds => new(770, 854, 150, 52);

    private static Rectangle GtpEngineSelectionDialogAddButtonBounds => new(898, 854, 140, 52);

    private static Rectangle GtpEngineSelectionDialogEditButtonBounds => new(1058, 854, 140, 52);

    private static Rectangle GtpEngineSelectionDialogDuplicateButtonBounds => new(1218, 854, 140, 52);

    private static Rectangle GtpEngineSelectionDialogDeleteButtonBounds => new(1378, 854, 140, 52);

    private static Rectangle GtpEngineDeleteConfirmationBounds => new(654, 358, 612, 260);

    private static Rectangle GtpEngineDeleteConfirmationCancelButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 298, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);

    private static Rectangle GtpEngineDeleteConfirmationConfirmButtonBounds => new(GtpEngineDeleteConfirmationBounds.X + 448, GtpEngineDeleteConfirmationBounds.Bottom - 76, 130, 48);

    private static Rectangle GtpEngineEditPanelBounds => new(430, 126, 1060, 820);

    private static Rectangle GtpEngineEditPanelEditorBounds => new(520, 228, 880, 590);

    private static Rectangle GtpEngineEditPanelCloseButtonBounds => new(1318, 156, 132, 48);

    private static Rectangle GtpEngineEditPanelSaveButtonBounds => new(1080, 840, 320, 58);

    private static Rectangle GtpEngineEditPanelFileBrowseButtonBounds => new(
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.ExecutablePath).Right - 112,
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.ExecutablePath).Y + 8,
        96,
        40);

    private static Rectangle GtpEngineEditPanelWorkingDirectoryBrowseButtonBounds => new(
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.WorkingDirectory).Right - 112,
        GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField.WorkingDirectory).Y + 8,
        96,
        40);

    private static Rectangle GtpEngineEditPanelLogRowBounds => new(AddPanelControlX, 596, 668, 56);

    private static Rectangle GtpEngineEditPanelLogButtonBounds => new(GtpEngineEditPanelLogRowBounds.X + 152, GtpEngineEditPanelLogRowBounds.Y + 8, 120, 40);

    private static readonly GtpEngineProfileEditField[] GtpEngineEditFields =
    {
        GtpEngineProfileEditField.DisplayName,
        GtpEngineProfileEditField.ExecutablePath,
        GtpEngineProfileEditField.WorkingDirectory,
        GtpEngineProfileEditField.Arguments,
    };

    private static Rectangle GtpEngineEditPanelFieldRowBounds(GtpEngineProfileEditField field) => field switch
    {
        GtpEngineProfileEditField.DisplayName => new Rectangle(AddPanelControlX, 264, 668, 56),
        GtpEngineProfileEditField.ExecutablePath => new Rectangle(AddPanelControlX, 348, 668, 56),
        GtpEngineProfileEditField.WorkingDirectory => new Rectangle(AddPanelControlX, 432, 668, 56),
        GtpEngineProfileEditField.Arguments => new Rectangle(AddPanelControlX, 516, 668, 56),
        _ => Rectangle.Empty,
    };

    private static Rectangle GtpEngineEditPanelFieldTextBounds(GtpEngineProfileEditField field)
    {
        var bounds = GtpEngineEditPanelFieldRowBounds(field);
        var rightPadding = field is GtpEngineProfileEditField.ExecutablePath or GtpEngineProfileEditField.WorkingDirectory ? 282 : 168;
        return new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - rightPadding, 42);
    }

    private static Rectangle GtpEngineSelectionDialogListItemBounds(int index) =>
        new(GtpEngineSelectionDialogListBounds.X + 16, GtpEngineSelectionDialogListBounds.Y + 16 + index * 88, GtpEngineSelectionDialogListBounds.Width - 32, 72);

    private static Rectangle GtpEngineSelectionDialogPropertyRowBounds(int index) =>
        new(GtpEngineSelectionDialogPropertyBounds.X + 18, GtpEngineSelectionDialogPropertyBounds.Y + 22 + index * 70, GtpEngineSelectionDialogPropertyBounds.Width - 36, 52);

    private static Rectangle PathTooltipBounds(Rectangle rowBounds)
    {
        var y = rowBounds.Y - 102;
        if (y < 140)
        {
            y = rowBounds.Bottom - 2;
        }

        return new Rectangle(rowBounds.X, y, rowBounds.Width, 104);
    }

    private static Rectangle PathTooltipCopyButtonBounds(Rectangle rowBounds)
    {
        return PathTooltipCopyButtonBoundsFromPopup(PathTooltipBounds(rowBounds));
    }

    private static Rectangle PathTooltipCopyButtonBoundsFromPopup(Rectangle popupBounds) =>
        new(popupBounds.Right - 124, popupBounds.Y + 56, 100, 34);

    private static Rectangle RuleKindButtonBounds(int index) => new(AddPanelControlX + index * 224, 358, 188, 50);

    private static Rectangle KomiStepButtonBounds(int index) => new(AddPanelControlX + 444 + index * 112, 516, 92, 40);

    private static Rectangle MainTimeStepButtonBounds(int index) => new(AddPanelControlX + 444 + index * 112, 580, 92, 40);

    private static Rectangle MoveLimitStepButtonBounds(int index) => new(AddPanelControlX + 444 + index * 112, 644, 92, 40);

    private static Rectangle PlayerKindButtonBounds(int index, int y) => new(1536 + index * 132, y, 132, 52);

    private static Rectangle PlayerKindSegmentBounds(int y) => new(1536, y, 264, 52);

    private static LabeledBrowseSelector GtpEngineSelectorBounds(int y) => new(new Rectangle(1144, y - 4, 668, 44), "ENGINE", "");

    private static Rectangle StartPlayingButtonBounds => new(1658, 920, 154, 56);

    private static Rectangle ImportSgfButtonBounds => new(1144, 920, 154, 56);

    private static Rectangle StartReviewingButtonBounds => new(1315, 920, 154, 56);

    private static Rectangle StartBoardEditingButtonBounds => new(1486, 920, 154, 56);

    private static Rectangle SaveTournamentRulesButtonBounds => new(974, 798, 320, 56);

    private static Rectangle LocalUseButtonBounds => new(508, 404, 438, 300);

    private static Rectangle CgosUseButtonBounds => new(974, 404, 438, 300);

    private static Rectangle CgosBackButtonBounds => new(1146, 800, 292, 64);

    private static Rectangle ReturnToSetupButtonBounds => new(1318, 910, 320, 56);

    private static Rectangle ExportSgfButtonBounds => new(1164, 910, 140, 56);

    private static Rectangle PassButtonBounds => new(1144, 920, 320, 72);

    private static Rectangle ResignButtonBounds => new(1492, 920, 320, 72);

    private static Rectangle CancelPlayingButtonBounds => new(1144, 920, 668, 72);

    private static Rectangle BoardEditingBlackButtonBounds => new(1144, 506, 204, 62);

    private static Rectangle BoardEditingWhiteButtonBounds => new(1376, 506, 204, 62);

    private static Rectangle BoardEditingEraseButtonBounds => new(1608, 506, 204, 62);

    private static Rectangle BoardEditingUndoButtonBounds => new(1144, 588, 320, 56);

    private static Rectangle BoardEditingRedoButtonBounds => new(1492, 588, 320, 56);

    private static Rectangle BoardEditingExportSgfButtonBounds => new(1144, 920, 320, 56);

    private static Rectangle BoardEditingDoneButtonBounds => new(1492, 920, 320, 56);

    private static readonly int[] ReviewStepButtonValues = [-50, -10, -1, 1, 10, 50];

    private static Rectangle ReviewStepButtonBounds(int index) => new(1144 + index % 3 * 232, 504 + index / 3 * 64, 160, 46);

    private static readonly RenParseDisplayMode[] ReviewRenParseDisplayModes =
    [
        RenParseDisplayMode.Overlay,
        RenParseDisplayMode.Graph,
        RenParseDisplayMode.GraphStep2,
        RenParseDisplayMode.Eye,
    ];

    private static readonly string[] ReviewRenParseDisplayModeLabels =
    [
        "Ren Number",
        "Ren Rect",
        "Ren Graph",
        "Eye",
    ];

    private static Rectangle ReviewRenParseDisplayModeBounds(int index) => new(1144 + index * 166, 684, 150, 46);

    private static Rectangle ReviewDoneButtonBounds => new(1492, 920, 320, 56);

    private static GoPlayerKind? GetPlayerKindButtonHit(Point point, int y)
    {
        if (PlayerKindButtonBounds(0, y).Contains(point))
        {
            return GoPlayerKind.Human;
        }

        return PlayerKindButtonBounds(1, y).Contains(point) ? GoPlayerKind.Computer : null;
    }

    private static string PlayerKindLabel(GoPlayerKind playerKind) => playerKind == GoPlayerKind.Human ? "Human" : "Computer";

    private static bool HasComputerPlayer(GoAppSession session) =>
        session.BlackPlayerKind == GoPlayerKind.Computer || session.WhitePlayerKind == GoPlayerKind.Computer;

    private static string GetEngineStatusText(GoAppSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.EngineErrorMessage))
        {
            return "ERROR";
        }

        if (!session.IsEngineReady)
        {
            return "STARTING";
        }

        return session.IsEngineThinking ? "THINKING" : "READY";
    }

    private static Color GetEngineStatusColor(GoAppSession session)
    {
        if (!string.IsNullOrWhiteSpace(session.EngineErrorMessage))
        {
            return new Color(255, 183, 146);
        }

        return session.IsEngineThinking || !session.IsEngineReady
            ? new Color(255, 230, 160)
            : new Color(99, 223, 185);
    }

    private static string FormatElapsedTime(TimeSpan elapsed)
    {
        var totalHours = (int)elapsed.TotalHours;
        return totalHours > 0
            ? $"{totalHours}:{elapsed.Minutes:00}:{elapsed.Seconds:00}"
            : $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    private static string FormatMainTime(TimeSpan mainTime) =>
        mainTime == TimeSpan.Zero ? "NO LIMIT" : FormatElapsedTime(mainTime);

    private static string FormatMoveLimit(int moveLimit) =>
        moveLimit <= 0 ? "NO LIMIT" : moveLimit.ToString();

    private static string GetMoveThinkingText(GoAppSession session)
    {
        var text = $"{session.NextMoveNumber}手目を思考中";
        return session.MoveLimit <= 0 ? text : $"{text} / {session.MoveLimit}";
    }

    private static string FormatGameEndMoveCount(int playedMoveCount) => $"{playedMoveCount}手で終局";

    private static string FormatKomi(decimal komi) => komi.ToString("0.0");

    private static string SaveTournamentRulesLabel(GoAppSession session) =>
        string.IsNullOrWhiteSpace(session.TournamentRulesSaveMessage)
            ? "SAVE RULES"
            : $"SAVE RULES {session.TournamentRulesSaveMessage}";

    private static string SaveGtpEngineLabel(GoAppSession session) =>
        string.IsNullOrWhiteSpace(session.GtpEngineEditSaveMessage)
            ? "SAVE ENGINE"
            : $"SAVE ENGINE {session.GtpEngineEditSaveMessage}";

    private void DrawCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled = true, float scale = 0.62f)
    {
        var hovered = enabled && bounds.Contains(mousePoint);
        var fill = !enabled ? new Color(24, 27, 31) : selected ? new Color(31, 151, 112) : hovered ? new Color(58, 82, 94) : new Color(36, 48, 58);
        var border = !enabled ? new Color(43, 50, 56) : selected ? new Color(151, 255, 215) : hovered ? new Color(178, 219, 226) : new Color(126, 150, 164);
        FillRect(new Rectangle(bounds.X + 4, bounds.Y + 5, bounds.Width, bounds.Height), new Color(0, 0, 0, enabled ? 95 : 28));
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);
        if (enabled)
        {
            DrawRect(new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4), 1, selected ? new Color(215, 255, 238, 95) : new Color(255, 255, 255, hovered ? 70 : 36));
        }

        var textColor = enabled ? Color.White : new Color(91, 100, 106);
        var measured = _font.MeasureString(label);
        var fittedScale = MathF.Min(scale, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
    }

    private void DrawReviewRenParseModeStrip(GoAppSession session, Point mousePoint)
    {
        for (var i = 0; i < ReviewRenParseDisplayModes.Length; i++)
        {
            DrawCommandButton(
                ReviewRenParseDisplayModeBounds(i),
                ReviewRenParseDisplayModeLabels[i],
                session.RenParseDisplayMode == ReviewRenParseDisplayModes[i],
                mousePoint,
                enabled: true,
                scale: 0.28f);
        }
    }

    private void DrawPathTooltip(Rectangle bounds, string fullPath, Point mousePoint)
    {
        FillRect(new Rectangle(bounds.X + 8, bounds.Y + 10, bounds.Width, bounds.Height), new Color(0, 0, 0, 150));
        FillRect(bounds, new Color(30, 36, 43, 252));
        DrawRect(bounds, 2, new Color(147, 244, 200));
        DrawText("FULL PATH", new Vector2(bounds.X + 18, bounds.Y + 12), new Color(180, 195, 195), 0.34f);
        DrawFittedText(fullPath, new Rectangle(bounds.X + 18, bounds.Y + 38, bounds.Width - 150, 44), Color.White, 0.42f);
        DrawCommandButton(PathTooltipCopyButtonBoundsFromPopup(bounds), "COPY", false, mousePoint, scale: 0.34f);
    }

    private void DrawLabeledBrowseSelector(LabeledBrowseSelector selector, Point mousePoint)
    {
        DrawDataRowFrame(selector.Bounds);

        DrawFittedText(selector.Label, selector.LabelBounds, new Color(158, 178, 178), 0.36f);
        DrawFittedText(selector.Value, selector.ValueBounds, Color.White, 0.52f);
        DrawCommandButton(selector.BrowseButtonBounds, "REF", false, mousePoint, scale: 0.44f);
    }

    private void DrawDataRowFrame(Rectangle bounds, bool active = false, bool hovered = false)
    {
        var fill = active ? new Color(28, 41, 45) : hovered ? new Color(28, 36, 43) : new Color(21, 28, 34);
        var line = active ? new Color(104, 191, 165) : hovered ? new Color(58, 77, 85) : new Color(43, 56, 63);
        FillRect(bounds, fill);
        FillRect(new Rectangle(bounds.X, bounds.Y, bounds.Width, 1), line);
        FillRect(new Rectangle(bounds.X, bounds.Bottom - 1, bounds.Width, 1), line);
        if (active)
        {
            FillRect(new Rectangle(bounds.X, bounds.Y, 3, bounds.Height), new Color(99, 223, 185));
        }
    }

    private void DrawInfoStrip(int x, int y, string label, string value)
    {
        var bounds = new Rectangle(x, y, 668, 72);
        DrawDataRowFrame(bounds);
        DrawUiLabel(UiLabel.InRow(label, bounds));
        DrawText(value, new Vector2(x + 184, y + 20), Color.White, 0.62f);
    }

    private void DrawResultSection(Rectangle bounds, string title)
    {
        FillRect(bounds, new Color(17, 22, 29, 215));
        DrawRect(bounds, 1, new Color(58, 78, 86));
        DrawText(title, new Vector2(bounds.X + 18, bounds.Y + 14), new Color(180, 195, 195), 0.5f);
        DrawLine(new Vector2(bounds.X + 18, bounds.Y + 48), new Vector2(bounds.Right - 18, bounds.Y + 48), 1, new Color(64, 82, 90));
    }

    private void DrawResultRow(Rectangle bounds, string label, string value, Color chipColor, Color valueColor)
    {
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        FillRect(new Rectangle(bounds.X + 14, bounds.Y + 10, 126, bounds.Height - 20), chipColor);
        DrawRect(new Rectangle(bounds.X + 14, bounds.Y + 10, 126, bounds.Height - 20), 1, new Color(120, 130, 126));
        DrawText(label, new Vector2(bounds.X + 32, bounds.Y + 14), Color.White, 0.38f);
        DrawFittedText(value, new Rectangle(bounds.X + 164, bounds.Y + 6, bounds.Width - 182, bounds.Height - 12), valueColor, 0.58f);
    }

    private void DrawAgehamaStrip(GoAppSession session, int y = 540)
    {
        var bounds = new Rectangle(1144, y, 668, 56);
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        DrawText("AGEHAMA", new Vector2(bounds.X + 20, bounds.Y + 16), new Color(180, 195, 195), 0.46f);
        DrawText($"BLACK {session.BlackAgehama}", new Vector2(bounds.X + 220, bounds.Y + 14), Color.White, 0.5f);
        DrawText($"WHITE {session.WhiteAgehama}", new Vector2(bounds.X + 430, bounds.Y + 14), Color.White, 0.5f);
    }

    private void DrawTimeStrip(GoAppSession session, int y)
    {
        var bounds = new Rectangle(1144, y, 668, 74);
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));

        var blackActive = session.CurrentMode.Kind == GoAppModeKind.Playing && session.CurrentTurn == GoStone.Black;
        var whiteActive = session.CurrentMode.Kind == GoAppModeKind.Playing && session.CurrentTurn == GoStone.White;
        DrawPlayerTime(new Rectangle(bounds.X + 18, bounds.Y + 14, 300, 46), "BLACK", session.BlackElapsedTime, blackActive, blackStone: true);
        DrawPlayerTime(new Rectangle(bounds.X + 350, bounds.Y + 14, 300, 46), "WHITE", session.WhiteElapsedTime, whiteActive, blackStone: false);
    }

    private void DrawPlayerTime(Rectangle bounds, string label, TimeSpan elapsed, bool active, bool blackStone)
    {
        FillRect(bounds, active ? new Color(39, 68, 65) : new Color(30, 36, 43));
        DrawRect(bounds, 1, active ? new Color(99, 223, 185) : new Color(70, 85, 94));
        DrawCircle(new Vector2(bounds.X + 24, bounds.Center.Y), 9, blackStone ? new Color(10, 12, 16) : new Color(230, 224, 207));
        DrawText(label, new Vector2(bounds.X + 46, bounds.Y + 14), new Color(180, 195, 195), 0.38f);
        DrawText(FormatElapsedTime(elapsed), new Vector2(bounds.X + 154, bounds.Y + 10), Color.White, 0.56f);
    }

    private void DrawStoneCountStrip(GoAppSession session, int y)
    {
        var bounds = new Rectangle(1144, y, 668, 82);
        var blackStones = session.BlackStoneCount;
        var whiteStones = session.WhiteStoneCount;
        var total = blackStones + whiteStones;
        var leader = blackStones == whiteStones ? "EVEN" : blackStones > whiteStones ? $"BLACK +{blackStones - whiteStones}" : $"WHITE +{whiteStones - blackStones}";

        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        DrawText("STONES", new Vector2(bounds.X + 20, bounds.Y + 15), new Color(180, 195, 195), 0.46f);
        DrawText($"BLACK {blackStones}", new Vector2(bounds.X + 150, bounds.Y + 13), Color.White, 0.5f);
        DrawText($"WHITE {whiteStones}", new Vector2(bounds.X + 334, bounds.Y + 13), Color.White, 0.5f);
        DrawText(leader, new Vector2(bounds.X + 518, bounds.Y + 13), new Color(99, 223, 185), 0.5f);

        var bar = new Rectangle(bounds.X + 20, bounds.Y + 52, bounds.Width - 40, 14);
        FillRect(bar, new Color(14, 18, 23));
        if (total > 0)
        {
            var blackWidth = (int)MathF.Round(bar.Width * (blackStones / (float)total));
            if (blackWidth > 0)
            {
                FillRect(new Rectangle(bar.X, bar.Y, blackWidth, bar.Height), new Color(9, 10, 13));
            }

            var whiteWidth = bar.Width - blackWidth;
            if (whiteWidth > 0)
            {
                FillRect(new Rectangle(bar.X + blackWidth, bar.Y, whiteWidth, bar.Height), new Color(230, 224, 207));
            }
        }

        DrawRect(bar, 1, new Color(95, 108, 116));
    }

    private void DrawMiniBoard(Rectangle rect)
    {
        FillRect(rect, new Color(202, 145, 68));
        var margin = 14f;
        var cell = (rect.Width - margin * 2) / 8f;
        for (var i = 0; i < 9; i++)
        {
            var x = rect.X + margin + cell * i;
            DrawLine(new Vector2(x, rect.Y + margin), new Vector2(x, rect.Bottom - margin), 1, new Color(48, 34, 24));
            var y = rect.Y + margin + cell * i;
            DrawLine(new Vector2(rect.X + margin, y), new Vector2(rect.Right - margin, y), 1, new Color(48, 34, 24));
        }

        DrawStone(new Vector2(rect.X + margin + cell * 2, rect.Y + margin + cell * 2), 9, black: true);
        DrawStone(new Vector2(rect.X + margin + cell * 5, rect.Y + margin + cell * 4), 9, black: false);
    }

    private void DrawPlacedStones(GoAppSession session, Vector2 start, float cell)
    {
        for (var y = 0; y < session.BoardSize; y++)
        {
            for (var x = 0; x < session.BoardSize; x++)
            {
                var stone = session.GetStone(x, y);
                if (stone != GoStone.Empty)
                {
                    DrawStone(BoardPoint(start, cell, x, y), cell * 0.44f, stone == GoStone.Black);
                }
            }
        }
    }

    private void DrawRenParseOverlay(GoAppSession session, Vector2 start, float cell)
    {
        if (session.RenParseDisplayMode != RenParseDisplayMode.Overlay)
        {
            return;
        }

        var renParse = session.ParseRens();
        DrawRenBoundaries(renParse, start, cell);
        DrawRenNumbers(renParse, start, cell);
    }

    private void DrawRenGraphStep1Overlay(GoAppSession session, Vector2 start, float cell)
    {
        var renParse = session.ParseRens();
        DrawRenGraphCells(session, start, cell);
        DrawRenBoundaries(renParse, start, cell);
        DrawRenRepresentativeNumbers(renParse, start, cell);
    }

    private void DrawRenGraphOverlay(GoAppSession session, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var renParse = session.ParseRens();
        var nodes = CreateRenGraphNodes(renParse, start, cell, applyEyeJudgement);

        FillRect(BoardBounds, new Color(56, 145, 129));
        DrawRenGraphEdges(nodes, renParse.Edges, cell);
        DrawRenGraphNodes(nodes, cell);
    }

    private void DrawRenGraphCells(GoAppSession session, Vector2 start, float cell)
    {
        var halfCell = cell * 0.5f;
        for (var y = 0; y < session.BoardSize; y++)
        {
            for (var x = 0; x < session.BoardSize; x++)
            {
                var center = BoardPoint(start, cell, x, y);
                var rect = new Rectangle(
                    (int)MathF.Round(center.X - halfCell),
                    (int)MathF.Round(center.Y - halfCell),
                    (int)MathF.Ceiling(cell),
                    (int)MathF.Ceiling(cell));
                FillRect(rect, RenGraphCellColor(session.GetStone(x, y)));
            }
        }
    }

    private RenGraphNode[] CreateRenGraphNodes(GoRenParseResult renParse, Vector2 start, float cell, bool applyEyeJudgement)
    {
        var sumX = new float[renParse.Count + 1];
        var sumY = new float[renParse.Count + 1];

        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                sumX[renNumber] += center.X;
                sumY[renNumber] += center.Y;
            }
        }

        var nodes = new RenGraphNode[renParse.Count + 1];
        for (var renNumber = 1; renNumber <= renParse.Count; renNumber++)
        {
            var ren = renParse.GetRen(renNumber);
            nodes[renNumber] = new RenGraphNode(
                renNumber,
                ren.Stone,
                new Vector2(sumX[renNumber] / ren.Points.Count, sumY[renNumber] / ren.Points.Count),
                !applyEyeJudgement || !ren.IsEye,
                new List<int>(ren.EyeRenNumbers));
        }

        return nodes;
    }

    private void DrawRenGraphEdges(RenGraphNode[] nodes, IReadOnlyList<GoRenGraphEdge> edges, float cell)
    {
        var thickness = MathHelper.Clamp(cell * 0.08f, 4f, 8f);
        var color = new Color(70, 70, 220, 230);
        foreach (var edge in edges)
        {
            if (!nodes[edge.From].IsVisible || !nodes[edge.To].IsVisible)
            {
                continue;
            }

            DrawLine(nodes[edge.From].Center, nodes[edge.To].Center, thickness, color);
        }
    }

    private void DrawRenGraphNodes(RenGraphNode[] nodes, float cell)
    {
        var radius = MathHelper.Clamp(cell * 0.45f, 22f, 46f);
        var scale = MathHelper.Clamp(cell / 72f, 0.34f, 0.84f);
        for (var renNumber = 1; renNumber < nodes.Length; renNumber++)
        {
            var node = nodes[renNumber];
            if (!node.IsVisible)
            {
                continue;
            }

            DrawCircle(node.Center, radius, RenGraphNodeColor(node.Stone));
            DrawCenteredText(node.Number.ToString(), node.Center, new Color(0, 177, 238), scale);
            DrawRenGraphEyeMarkers(node, radius, scale);
        }
    }

    private void DrawRenGraphEyeMarkers(RenGraphNode node, float radius, float scale)
    {
        if (node.EyeNumbers.Count == 0)
        {
            return;
        }

        var markerScale = Math.Max(0.22f, scale * 0.52f);
        var markerSize = Math.Max(16f, radius * 0.56f);
        var spacing = markerSize + 6f;
        var startX = node.Center.X + radius * 0.34f;
        var startY = node.Center.Y + radius * 0.62f;

        for (var i = 0; i < node.EyeNumbers.Count; i++)
        {
            var markerBounds = new Rectangle(
                (int)MathF.Round(startX + (i * spacing) - (markerSize * 0.5f)),
                (int)MathF.Round(startY - (markerSize * 0.5f)),
                (int)MathF.Round(markerSize),
                (int)MathF.Round(markerSize));
            FillRect(markerBounds, new Color(255, 238, 0, 245));
            DrawRect(markerBounds, 2, new Color(255, 250, 220));
            DrawCenteredText(node.EyeNumbers[i].ToString(), new Vector2(markerBounds.Center.X, markerBounds.Center.Y), new Color(56, 94, 120), markerScale);
        }
    }

    private void DrawRenBoundaries(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var size = renParse.Size;
        var halfCell = cell * 0.5f;
        var thickness = Math.Max(5, (int)MathF.Round(cell * 0.08f));
        var color = new Color(255, 238, 0, 238);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                var center = BoardPoint(start, cell, x, y);
                var left = center.X - halfCell;
                var top = center.Y - halfCell;
                var right = center.X + halfCell;
                var bottom = center.Y + halfCell;

                if (x == 0 || renParse.GetRenNumber(x - 1, y) != renNumber)
                {
                    FillRect(CreateVerticalLineRect(left, top, bottom, thickness), color);
                }

                if (y == 0 || renParse.GetRenNumber(x, y - 1) != renNumber)
                {
                    FillRect(CreateHorizontalLineRect(left, right, top, thickness), color);
                }

                if (x == size - 1)
                {
                    FillRect(CreateVerticalLineRect(right, top, bottom, thickness), color);
                }

                if (y == size - 1)
                {
                    FillRect(CreateHorizontalLineRect(left, right, bottom, thickness), color);
                }
            }
        }
    }

    private void DrawRenNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var label = renParse.GetRenNumber(x, y).ToString();
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(label, center, new Color(0, 177, 238), scale);
            }
        }
    }

    private void DrawRenRepresentativeNumbers(GoRenParseResult renParse, Vector2 start, float cell)
    {
        var scale = MathHelper.Clamp(cell / 72f, 0.28f, 0.88f);
        var drawn = new bool[renParse.Count + 1];
        for (var y = 0; y < renParse.Size; y++)
        {
            for (var x = 0; x < renParse.Size; x++)
            {
                var renNumber = renParse.GetRenNumber(x, y);
                if (drawn[renNumber])
                {
                    continue;
                }

                drawn[renNumber] = true;
                var center = BoardPoint(start, cell, x, y);
                DrawCenteredText(renNumber.ToString(), center, new Color(0, 177, 238), scale);
            }
        }
    }

    private static Color RenGraphNodeColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };

    private static Color RenGraphCellColor(GoStone stone) => stone switch
    {
        GoStone.Black => Color.Black,
        GoStone.White => new Color(248, 248, 244),
        _ => new Color(255, 197, 18),
    };

    private sealed class RenGraphNode
    {
        public RenGraphNode(int number, GoStone stone, Vector2 center, bool isVisible, List<int> eyeNumbers)
        {
            Number = number;
            Stone = stone;
            Center = center;
            IsVisible = isVisible;
            EyeNumbers = eyeNumbers;
        }

        public int Number { get; }

        public GoStone Stone { get; }

        public Vector2 Center { get; }

        public bool IsVisible { get; set; }

        public List<int> EyeNumbers { get; }
    }

    private void DrawHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
        if (session.CurrentMode.Kind == GoAppModeKind.BoardEditing)
        {
            DrawBoardEditingHoverStone(session, mousePoint, cell);
            return;
        }

        if (session.CurrentMode.Kind != GoAppModeKind.Playing ||
            !session.CanAcceptHumanMove ||
            !TryGetBoardIntersection(mousePoint, session.BoardSize, out var intersection) ||
            session.GetStone(intersection.X, intersection.Y) != GoStone.Empty ||
            (session.KoPoint is { } ko && ko.X == intersection.X && ko.Y == intersection.Y) ||
            session.IsSuperKoPoint(intersection.X, intersection.Y))
        {
            return;
        }

        var layout = GetBoardLayout(session.BoardSize);
        var center = BoardPoint(layout.Start, layout.Cell, intersection.X, intersection.Y);
        var black = session.CurrentTurn == GoStone.Black;
        DrawCircle(center, cell * 0.55f, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 110));
        DrawCircle(center, cell * 0.36f, black ? new Color(8, 10, 14, 90) : new Color(255, 250, 232, 95));
    }

    private void DrawBoardEditingHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
        if (!TryGetBoardIntersection(mousePoint, session.BoardSize, out var intersection))
        {
            return;
        }

        var layout = GetBoardLayout(session.BoardSize);
        var center = BoardPoint(layout.Start, layout.Cell, intersection.X, intersection.Y);
        if (session.BoardEditingStone == GoStone.Empty)
        {
            var radius = cell * 0.32f;
            DrawLine(new Vector2(center.X - radius, center.Y - radius), new Vector2(center.X + radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            DrawLine(new Vector2(center.X + radius, center.Y - radius), new Vector2(center.X - radius, center.Y + radius), 6, new Color(180, 42, 42, 205));
            return;
        }

        var black = session.BoardEditingStone == GoStone.Black;
        DrawCircle(center, cell * 0.55f, black ? new Color(8, 10, 14, 105) : new Color(255, 250, 232, 120));
        DrawCircle(center, cell * 0.36f, black ? new Color(8, 10, 14, 95) : new Color(255, 250, 232, 105));
    }

    private void DrawSuperKoMarks(GoAppSession session, Vector2 start, float cell)
    {
        foreach (var point in session.EnumerateSuperKoPoints())
        {
            var center = BoardPoint(start, cell, point.X, point.Y);
            var radius = Math.Max(15f, cell * 0.32f);
            var bounds = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
            FillRect(bounds, new Color(82, 39, 138, 198));
            DrawRect(bounds, 2, new Color(235, 206, 255));

            const string label = "S-KO";
            var scale = cell < 55 ? 0.24f : 0.3f;
            var size = _font.MeasureString(label) * scale;
            DrawText(label, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), Color.White, scale);
        }
    }

    private void DrawKoMark(GoAppSession session, Vector2 start, float cell)
    {
        if (session.KoPoint is not { } ko)
        {
            return;
        }

        var center = BoardPoint(start, cell, ko.X, ko.Y);
        var radius = Math.Max(12f, cell * 0.26f);
        var bounds = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        FillRect(bounds, new Color(143, 38, 38, 210));
        DrawRect(bounds, 2, new Color(255, 230, 160));

        const string label = "KO";
        var size = _font.MeasureString(label) * 0.34f;
        DrawText(label, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), Color.White, 0.34f);
    }

    private static Vector2 BoardPoint(Vector2 start, float cell, int x, int y) => new(start.X + cell * x, start.Y + cell * y);

    private const float BoardMargin = 38f;

    private static readonly Rectangle BoardBounds = new(88, 84, 912, 912);

    private static (Vector2 Start, float Cell) GetBoardLayout(int boardSize)
    {
        var playable = BoardBounds.Width - BoardMargin * 2;
        var cell = playable / (boardSize - 1);
        var start = new Vector2(BoardBounds.X + BoardMargin, BoardBounds.Y + BoardMargin);
        return (start, cell);
    }

    private static Point[] GetStarPoints(int boardSize)
    {
        return boardSize switch
        {
            9 => new[] { new Point(2, 2), new Point(6, 2), new Point(4, 4), new Point(2, 6), new Point(6, 6) },
            13 => new[] { new Point(3, 3), new Point(9, 3), new Point(6, 6), new Point(3, 9), new Point(9, 9) },
            _ => new[] { new Point(3, 3), new Point(9, 3), new Point(15, 3), new Point(3, 9), new Point(9, 9), new Point(15, 9), new Point(3, 15), new Point(9, 15), new Point(15, 15) },
        };
    }

    private void DrawBoardFrameHighlights(Rectangle boardOuter)
    {
        FillRect(new Rectangle(boardOuter.X, boardOuter.Y, boardOuter.Width, 5), new Color(255, 220, 128, 90));
        FillRect(new Rectangle(boardOuter.X, boardOuter.Y, 5, boardOuter.Height), new Color(255, 220, 128, 70));
        FillRect(new Rectangle(boardOuter.Right - 7, boardOuter.Y, 7, boardOuter.Height), new Color(31, 20, 15, 120));
        FillRect(new Rectangle(boardOuter.X, boardOuter.Bottom - 7, boardOuter.Width, 7), new Color(31, 20, 15, 120));
    }

    private void DrawGlow(Vector2 center, float radius, Color color)
    {
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        _spriteBatch.Draw(_softCircle, destination, color);
    }

    private void DrawStone(Vector2 center, float radius, bool black)
    {
        var size = (int)(radius * 2);
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size);
        _spriteBatch.Draw(_softCircle, new Rectangle(destination.X + 7, destination.Y + 10, destination.Width, destination.Height), new Color(0, 0, 0, 110));
        _spriteBatch.Draw(black ? _stoneDark : _stoneLight, destination, Color.White);
    }

    private void DrawCircle(Vector2 center, float radius, Color color)
    {
        var size = (int)(radius * 2);
        _spriteBatch.Draw(_softCircle, new Rectangle((int)(center.X - radius), (int)(center.Y - radius), size, size), color);
    }

    private void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
    {
        var direction = end - start;
        var length = direction.Length();
        var angle = MathF.Atan2(direction.Y, direction.X);
        _spriteBatch.Draw(_pixel, start, null, color, angle, Vector2.Zero, new Vector2(length, thickness), SpriteEffects.None, 0f);
    }

    private void FillRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

    private void DrawRect(Rectangle rect, int thickness, Color color)
    {
        FillRect(new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        FillRect(new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    private static Rectangle CreateVerticalLineRect(float x, float top, float bottom, int thickness) =>
        new((int)MathF.Round(x - thickness / 2f), (int)MathF.Round(top), thickness, (int)MathF.Round(bottom - top));

    private static Rectangle CreateHorizontalLineRect(float left, float right, float y, int thickness) =>
        new((int)MathF.Round(left), (int)MathF.Round(y - thickness / 2f), (int)MathF.Round(right - left), thickness);

    private void DrawText(string text, Vector2 position, Color color, float scale)
    {
        _spriteBatch.DrawString(_font, text, position + new Vector2(2, 2), new Color(0, 0, 0, 125), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawCenteredText(string text, Vector2 center, Color color, float scale)
    {
        var size = _font.MeasureString(text) * scale;
        DrawText(text, new Vector2(center.X - size.X / 2, center.Y - size.Y / 2), color, scale);
    }

    private void DrawUiLabel(UiLabel label) => DrawFittedText(label.Text, label.Bounds, UiLabel.TextColor, label.Scale);

    private void DrawFittedText(string text, Rectangle bounds, Color color, float scale)
    {
        var measured = _font.MeasureString(text);
        var fittedScale = MathF.Min(scale, MathF.Min(bounds.Width / Math.Max(1f, measured.X), bounds.Height / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(text, new Vector2(bounds.X, bounds.Center.Y - size.Y / 2), color, fittedScale);
    }

    private Texture2D CreateTexture(int width, int height, Func<int, int, Color> colorFactory)
    {
        var texture = new Texture2D(_graphicsDevice, width, height);
        var data = new Color[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                data[y * width + x] = colorFactory(x, y);
            }
        }

        texture.SetData(data);
        return texture;
    }

    private Texture2D CreateCircleTexture(int size, Color color, bool softEdge)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var dx = x - center;
            var dy = y - center;
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            var radius = size * 0.48f;
            if (distance > radius)
            {
                return Color.Transparent;
            }

            var alpha = softEdge ? MathHelper.Clamp((radius - distance) / (radius * 0.45f), 0f, 1f) : 1f;
            return color * alpha;
        });
    }

    private Texture2D CreateStoneTexture(int size, bool lightStone)
    {
        return CreateTexture(size, size, (x, y) =>
        {
            var center = (size - 1) * 0.5f;
            var nx = (x - center) / center;
            var ny = (y - center) / center;
            var distance = MathF.Sqrt(nx * nx + ny * ny);
            if (distance > 0.96f)
            {
                return Color.Transparent;
            }

            var highlight = MathF.Max(0f, 1f - MathF.Sqrt((nx + 0.32f) * (nx + 0.32f) + (ny + 0.38f) * (ny + 0.38f)) * 2.2f);
            var shade = 1f - MathHelper.Clamp(distance * 0.55f, 0f, 0.55f);
            if (lightStone)
            {
                var value = (byte)MathHelper.Clamp(232 + highlight * 22 - distance * 22, 205, 255);
                var blue = (byte)MathHelper.Clamp(value - 10, 195, 245);
                return new Color(value, value, blue, (byte)255);
            }

            var baseValue = 18 + highlight * 72 - distance * 12;
            return new Color((byte)MathHelper.Clamp(baseValue, 8, 92), (byte)MathHelper.Clamp(baseValue + 2, 9, 96), (byte)MathHelper.Clamp(baseValue + 7, 14, 105));
        });
    }
}
