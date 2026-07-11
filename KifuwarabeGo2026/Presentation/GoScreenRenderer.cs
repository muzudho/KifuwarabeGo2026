namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Domain;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
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
        DrawGtpEngineSelectionDialog(session, mousePoint);

        _spriteBatch.End();
    }

    public static int? GetBoardSizeButtonHit(Point point, GoAppModeKind modeKind)
    {
        if (modeKind == GoAppModeKind.GameOver)
        {
            return null;
        }

        var y = SetupBoardSizeButtonY;
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

    public static bool GetGtpEngineSelectionDialogCloseButtonHit(Point point) =>
        GtpEngineSelectionDialogCloseButtonBounds.Contains(point);

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

    public static bool GetSaveTournamentRulesButtonHit(Point point) => SaveTournamentRulesButtonBounds.Contains(point);

    public static bool GetStartPlayingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartPlayingButtonBounds.Contains(point);

    public static bool GetReturnToSetupButtonHit(Point point) => ReturnToSetupButtonBounds.Contains(point);

    public static GoPlayerKind? GetBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, BlackPlayerKindButtonY);

    public static GoPlayerKind? GetWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, WhitePlayerKindButtonY);

    public static bool GetBlackGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(BlackEngineButtonY).ContainsBrowseButton(point);

    public static bool GetWhiteGtpEngineBrowseButtonHit(Point point) =>
        GtpEngineSelectorBounds(WhiteEngineButtonY).ContainsBrowseButton(point);

    public static bool GetPassButtonHit(Point point) => PassButtonBounds.Contains(point);

    public static bool GetResignButtonHit(Point point) => ResignButtonBounds.Contains(point);

    public static bool GetCancelPlayingButtonHit(Point point) => CancelPlayingButtonBounds.Contains(point);

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

        DrawPlacedStones(session, start, cell);
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

        DrawSetupSidePanel(session, mousePoint);
    }

    private void DrawSetupSidePanel(GoAppSession session, Point mousePoint)
    {
        var boardSize = session.BoardSize;

        DrawText("KIFUWARABE GO 2026", new Vector2(1142, 104), new Color(244, 238, 218), 1.0f);
        DrawText($"MODE {session.CurrentMode.DisplayName}", new Vector2(1540, 112), new Color(227, 224, 210), 0.5f);
        DrawText("TOURNAMENT", new Vector2(1144, 166), new Color(180, 195, 195), 0.5f);
        DrawLabeledBrowseSelector(TournamentRulesSelector with { Value = session.TournamentDisplayName }, mousePoint);

        DrawText("RULE", new Vector2(1144, 348), new Color(180, 195, 195), 0.5f);
        DrawRuleKindButtons(session.RuleKind, mousePoint);
        DrawText($"BOARD {boardSize} x {boardSize}", new Vector2(1144, 438), new Color(99, 223, 185), 0.62f);
        DrawBoardSizeButtons(boardSize, mousePoint, SetupBoardSizeButtonY);

        DrawRulesNumberStrip(1144, 552, "KOMI", FormatKomi(session.Komi), KomiStepButtonBounds(0), "-0.5", KomiStepButtonBounds(1), "+0.5", mousePoint);
        DrawRulesNumberStrip(1144, 624, "TIME", FormatMainTime(session.MainTime), MainTimeStepButtonBounds(0), "-1m", MainTimeStepButtonBounds(1), "+1m", mousePoint);

        DrawInfoStrip(1144, 700, "BLACK", PlayerKindLabel(session.BlackPlayerKind), new Color(26, 27, 30), Color.White);
        DrawPlayerKindButtons(session.BlackPlayerKind, mousePoint, BlackPlayerKindButtonY);
        DrawSetupEngineButtons(session, GoStone.Black, mousePoint, BlackEngineButtonY);
        DrawInfoStrip(1144, 798, "WHITE", PlayerKindLabel(session.WhitePlayerKind), new Color(236, 229, 211), new Color(24, 24, 24));
        DrawPlayerKindButtons(session.WhitePlayerKind, mousePoint, WhitePlayerKindButtonY);
        DrawSetupEngineButtons(session, GoStone.White, mousePoint, WhiteEngineButtonY);
        DrawCommandButton(SaveTournamentRulesButtonBounds, SaveTournamentRulesLabel(session), false, mousePoint);
        DrawCommandButton(StartPlayingButtonBounds, "START", false, mousePoint);
    }

    private void DrawPlayingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("TURN", new Vector2(1144, 132), new Color(180, 195, 195), 0.62f);

        var turnLabel = session.CurrentTurn == GoStone.Black ? "BLACK" : "WHITE";
        var turnChip = session.CurrentTurn == GoStone.Black ? new Color(26, 27, 30) : new Color(236, 229, 211);
        var turnText = session.CurrentTurn == GoStone.Black ? Color.White : new Color(24, 24, 24);
        DrawInfoStrip(1144, 180, "TURN", turnLabel, turnChip, turnText);

        DrawText("PLAYERS", new Vector2(1144, 300), new Color(180, 195, 195), 0.62f);
        DrawInfoStrip(1144, 348, "BLACK", PlayerKindLabel(session.BlackPlayerKind), new Color(26, 27, 30), Color.White);
        DrawInfoStrip(1144, 446, "WHITE", PlayerKindLabel(session.WhitePlayerKind), new Color(236, 229, 211), new Color(24, 24, 24));

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

        DrawTournamentRulesSelectionProperties(session);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.TournamentRulesList.Count / (double)GoAppSession.TournamentRulesSelectionPageSize));
        DrawCommandButton(TournamentRulesSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.TournamentRulesSelectionPageIndex + 1} / {pageCount}", new Vector2(TournamentRulesSelectionDialogBounds.X + 350, TournamentRulesSelectionDialogBounds.Bottom - 62), new Color(227, 224, 210), 0.48f);
        DrawCommandButton(TournamentRulesSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.TournamentRulesSelectionPageIndex < pageCount - 1, scale: 0.42f);
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

    private void DrawTournamentRulesSelectionProperties(GoAppSession session)
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
        DrawPropertyRow(y + 350, "FILE", string.IsNullOrWhiteSpace(rules.FilePath) ? "-" : Path.GetFileName(rules.FilePath));
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

        DrawGtpEngineSelectionProperties(session);

        var pageCount = Math.Max(1, (int)Math.Ceiling(session.GtpEngineProfiles.Count / (double)GoAppSession.GtpEngineSelectionPageSize));
        DrawCommandButton(GtpEngineSelectionDialogPreviousPageButtonBounds, "PREV", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex > 0, scale: 0.42f);
        DrawText($"PAGE {session.GtpEngineSelectionPageIndex + 1} / {pageCount}", new Vector2(GtpEngineSelectionDialogBounds.X + 350, GtpEngineSelectionDialogBounds.Bottom - 62), new Color(227, 224, 210), 0.48f);
        DrawCommandButton(GtpEngineSelectionDialogNextPageButtonBounds, "NEXT", false, mousePoint, enabled: session.GtpEngineSelectionPageIndex < pageCount - 1, scale: 0.42f);
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

    private void DrawGtpEngineSelectionProperties(GoAppSession session)
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
        DrawGtpEnginePropertyRow(y + 70, "EXE", string.IsNullOrWhiteSpace(profile.ExecutablePath) ? "-" : profile.ExecutablePath);
        DrawGtpEnginePropertyRow(y + 140, "WORKDIR", string.IsNullOrWhiteSpace(profile.WorkingDirectory) ? "-" : profile.WorkingDirectory);
        DrawGtpEnginePropertyRow(y + 210, "ARGS", string.IsNullOrWhiteSpace(profile.Arguments) ? "-" : profile.Arguments);
        DrawGtpEnginePropertyRow(y + 280, "GTP LOG", profile.EnableGtpLog ? "ON" : "OFF");
    }

    private void DrawGtpEnginePropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(GtpEngineSelectionDialogPropertyBounds.X + 18, y, GtpEngineSelectionDialogPropertyBounds.Width - 36, 52);
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        FillRect(new Rectangle(bounds.X + 12, bounds.Y + 10, 118, 32), new Color(39, 68, 65));
        DrawFittedText(label, new Rectangle(bounds.X + 24, bounds.Y + 10, 94, 32), Color.White, 0.34f);
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
    }

    private void DrawPropertyRow(int y, string label, string value)
    {
        var bounds = new Rectangle(TournamentRulesSelectionDialogPropertyBounds.X + 18, y, TournamentRulesSelectionDialogPropertyBounds.Width - 36, 52);
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        FillRect(new Rectangle(bounds.X + 12, bounds.Y + 10, 118, 32), new Color(39, 68, 65));
        DrawFittedText(label, new Rectangle(bounds.X + 24, bounds.Y + 10, 94, 32), Color.White, 0.34f);
        DrawFittedText(value, new Rectangle(bounds.X + 152, bounds.Y + 7, bounds.Width - 168, 38), Color.White, 0.46f);
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
        FillRect(bounds, new Color(24, 31, 37));
        DrawRect(bounds, 1, new Color(70, 85, 94));
        DrawText(label, new Vector2(bounds.X + 20, bounds.Y + 16), new Color(180, 195, 195), 0.42f);
        DrawText(value, new Vector2(bounds.X + 176, bounds.Y + 13), Color.White, 0.52f);
        DrawCommandButton(minusBounds, minusLabel, false, mousePoint, scale: 0.42f);
        DrawCommandButton(plusBounds, plusLabel, false, mousePoint, scale: 0.42f);
    }

    private void DrawPlayerKindButtons(GoPlayerKind selectedKind, Point mousePoint, int y)
    {
        DrawCommandButton(PlayerKindButtonBounds(0, y), "HUMAN", selectedKind == GoPlayerKind.Human, mousePoint);
        DrawCommandButton(PlayerKindButtonBounds(1, y), "COMPUTER", selectedKind == GoPlayerKind.Computer, mousePoint);
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

    private const int SetupBoardSizeButtonY = 476;

    private const int BlackPlayerKindButtonY = 710;

    private const int WhitePlayerKindButtonY = 808;

    private const int BlackEngineButtonY = 774;

    private const int WhiteEngineButtonY = 872;

    private static Rectangle BoardSizeButtonBounds(int index, int y) => new(1144 + index * 224, y, 188, 62);

    private static LabeledBrowseSelector TournamentRulesSelector => new(new Rectangle(1144, 198, 668, 56), "RULES", "");

    private static Rectangle TournamentRulesSelectionDialogBounds => new(230, 126, 1460, 820);

    private static Rectangle TournamentRulesSelectionDialogListBounds => new(270, 242, 650, 560);

    private static Rectangle TournamentRulesSelectionDialogPropertyBounds => new(950, 242, 700, 560);

    private static Rectangle TournamentRulesSelectionDialogCloseButtonBounds => new(1518, 156, 132, 48);

    private static Rectangle TournamentRulesSelectionDialogPreviousPageButtonBounds => new(270, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogNextPageButtonBounds => new(770, 854, 150, 52);

    private static Rectangle TournamentRulesSelectionDialogListItemBounds(int index) =>
        new(TournamentRulesSelectionDialogListBounds.X + 16, TournamentRulesSelectionDialogListBounds.Y + 16 + index * 88, TournamentRulesSelectionDialogListBounds.Width - 32, 72);

    private static Rectangle GtpEngineSelectionDialogBounds => new(230, 126, 1460, 820);

    private static Rectangle GtpEngineSelectionDialogListBounds => new(270, 242, 650, 560);

    private static Rectangle GtpEngineSelectionDialogPropertyBounds => new(950, 242, 700, 560);

    private static Rectangle GtpEngineSelectionDialogCloseButtonBounds => new(1518, 156, 132, 48);

    private static Rectangle GtpEngineSelectionDialogPreviousPageButtonBounds => new(270, 854, 150, 52);

    private static Rectangle GtpEngineSelectionDialogNextPageButtonBounds => new(770, 854, 150, 52);

    private static Rectangle GtpEngineSelectionDialogListItemBounds(int index) =>
        new(GtpEngineSelectionDialogListBounds.X + 16, GtpEngineSelectionDialogListBounds.Y + 16 + index * 88, GtpEngineSelectionDialogListBounds.Width - 32, 72);

    private static Rectangle RuleKindButtonBounds(int index) => new(1144 + index * 224, 382, 188, 50);

    private static Rectangle KomiStepButtonBounds(int index) => new(1588 + index * 112, 560, 92, 40);

    private static Rectangle MainTimeStepButtonBounds(int index) => new(1588 + index * 112, 632, 92, 40);

    private static Rectangle PlayerKindButtonBounds(int index, int y) => new(1536 + index * 140, y, 132, 52);

    private static LabeledBrowseSelector GtpEngineSelectorBounds(int y) => new(new Rectangle(1144, y - 4, 668, 44), "ENGINE", "");

    private static Rectangle StartPlayingButtonBounds => new(1492, 920, 320, 56);

    private static Rectangle SaveTournamentRulesButtonBounds => new(1144, 920, 320, 56);

    private static Rectangle ReturnToSetupButtonBounds => new(1318, 910, 320, 56);

    private static Rectangle PassButtonBounds => new(1144, 920, 320, 72);

    private static Rectangle ResignButtonBounds => new(1492, 920, 320, 72);

    private static Rectangle CancelPlayingButtonBounds => new(1144, 920, 668, 72);

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

    private static string FormatKomi(decimal komi) => komi.ToString("0.0");

    private static string SaveTournamentRulesLabel(GoAppSession session) =>
        string.IsNullOrWhiteSpace(session.TournamentRulesSaveMessage)
            ? "SAVE RULES"
            : $"SAVE RULES {session.TournamentRulesSaveMessage}";

    private void DrawCommandButton(Rectangle bounds, string label, bool selected, Point mousePoint, bool enabled = true, float scale = 0.62f)
    {
        var hovered = enabled && bounds.Contains(mousePoint);
        var fill = !enabled ? new Color(28, 31, 36) : selected ? new Color(39, 125, 97) : hovered ? new Color(56, 67, 77) : new Color(32, 38, 47);
        var border = !enabled ? new Color(58, 65, 70) : selected ? new Color(147, 244, 200) : new Color(103, 119, 130);
        FillRect(bounds, fill);
        DrawRect(bounds, 2, border);

        var textColor = enabled ? Color.White : new Color(130, 138, 142);
        var measured = _font.MeasureString(label);
        var fittedScale = MathF.Min(scale, MathF.Min((bounds.Width - 20) / Math.Max(1f, measured.X), (bounds.Height - 10) / Math.Max(1f, measured.Y)));
        var size = measured * fittedScale;
        DrawText(label, new Vector2(bounds.Center.X - size.X / 2, bounds.Center.Y - size.Y / 2), textColor, fittedScale);
    }

    private void DrawLabeledBrowseSelector(LabeledBrowseSelector selector, Point mousePoint)
    {
        FillRect(selector.Bounds, new Color(24, 31, 37));
        DrawRect(selector.Bounds, 1, new Color(70, 85, 94));

        FillRect(selector.LabelBounds, new Color(39, 68, 65));
        DrawRect(selector.LabelBounds, 1, new Color(120, 130, 126));
        DrawFittedText(selector.Label, new Rectangle(selector.LabelBounds.X + 12, selector.LabelBounds.Y + 4, selector.LabelBounds.Width - 24, selector.LabelBounds.Height - 8), Color.White, 0.42f);
        DrawFittedText(selector.Value, selector.ValueBounds, Color.White, 0.52f);
        DrawCommandButton(selector.BrowseButtonBounds, "REF", false, mousePoint, scale: 0.44f);
    }

    private void DrawInfoStrip(int x, int y, string label, string value, Color chipColor, Color chipTextColor)
    {
        FillRect(new Rectangle(x, y, 668, 72), new Color(30, 36, 43));
        DrawRect(new Rectangle(x, y, 668, 72), 1, new Color(70, 85, 94));
        FillRect(new Rectangle(x + 18, y + 16, 132, 40), chipColor);
        DrawRect(new Rectangle(x + 18, y + 16, 132, 40), 1, new Color(120, 130, 126));
        DrawText(label, new Vector2(x + 38, y + 25), chipTextColor, 0.46f);
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

    private void DrawHoverStone(GoAppSession session, Point mousePoint, float cell)
    {
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

    private void DrawText(string text, Vector2 position, Color color, float scale)
    {
        _spriteBatch.DrawString(_font, text, position + new Vector2(2, 2), new Color(0, 0, 0, 125), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

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
