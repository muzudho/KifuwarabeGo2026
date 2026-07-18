namespace KifuwarabeGo2026.Presentation;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Application.Cgos.ConnectionTarget;
using KifuwarabeGo2026.Domain;
using KifuwarabeGo2026.Presentation.Local.Resting.TournamentRule;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// ［画面描画］の共通処理
/// </summary>
public sealed partial class GoScreenRenderer
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
    public static bool GetLocalUseButtonHit(Point point) => LocalUseButtonBounds.Contains(point);
    public static bool GetImportSgfButtonHit(Point point) => ImportSgfButtonBounds.Contains(point);
    public static bool GetStartPlayingButtonHit(Point point, GoAppModeKind modeKind) =>
        modeKind != GoAppModeKind.GameOver && StartPlayingButtonBounds.Contains(point);

    public static bool GetReturnToSetupButtonHit(Point point) => ReturnToSetupButtonBounds.Contains(point);

    public static bool GetExportSgfButtonHit(Point point) => ExportSgfButtonBounds.Contains(point);

    public static bool GetSetupBackToTitleButtonHit(Point point) => SetupBackToTitleButtonBounds.Contains(point);

    public static GoPlayerKind? GetBlackPlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, BlackPlayerKindButtonY);

    public static GoPlayerKind? GetWhitePlayerKindButtonHit(Point point) => GetPlayerKindButtonHit(point, WhitePlayerKindButtonY);

    public static GoStone? GetHumanPlayerNameTextBoxHit(Point point, GoAppSession session)
    {
        if (session.BlackPlayerKind == GoPlayerKind.Human && HumanPlayerNameRowBounds(BlackEngineButtonY).Contains(point)) return GoStone.Black;
        return session.WhitePlayerKind == GoPlayerKind.Human && HumanPlayerNameRowBounds(WhiteEngineButtonY).Contains(point) ? GoStone.White : null;
    }

    public int GetHumanPlayerNameCaretIndex(Point point, GoStone stone, string text) =>
        GetTextBoxCaretIndex(point.X, text, HumanPlayerNameTextBounds(stone == GoStone.Black ? BlackEngineButtonY : WhiteEngineButtonY), 0.42f);
    public static bool GetPassButtonHit(Point point) => PassButtonBounds.Contains(point);

    public static bool GetResignButtonHit(Point point) => ResignButtonBounds.Contains(point);

    public static bool GetCancelPlayingButtonHit(Point point) => CancelPlayingButtonBounds.Contains(point);
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
    private void DrawSetupSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("KIFUWARABE GO 2026", new Vector2(1142, 104), new Color(244, 238, 218), 1.0f);
        DrawCommandButton(SetupBackToTitleButtonBounds, "BACK TO TITLE", false, mousePoint, scale: 0.32f);
        DrawText("TOURNAMENT", new Vector2(1144, 166), new Color(180, 195, 195), 0.5f);
        DrawLabeledBrowseSelector(TournamentRulesSelector with { Value = session.TournamentDisplayName }, mousePoint);

        DrawText("CURRENT RULES", new Vector2(1144, 294), new Color(180, 195, 195), 0.5f);
        DrawInfoStrip(1144, 334, "RULE", session.RuleKind.ToString());
        DrawInfoStrip(1144, 406, "BOARD", $"{session.BoardSize} x {session.BoardSize}");
        DrawInfoStrip(1144, 478, "KOMI", FormatKomi(session.Komi));
        DrawInfoStrip(1144, 550, "MOVES", FormatMoveLimit(session.MoveLimit));

        DrawInfoStrip(1144, 646, "BLACK", PlayerKindLabel(session.BlackPlayerKind));
        DrawPlayerKindButtons(session.BlackPlayerKind, mousePoint, BlackPlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.Black, mousePoint, BlackEngineButtonY);
        DrawInfoStrip(1144, 780, "WHITE", PlayerKindLabel(session.WhitePlayerKind));
        DrawPlayerKindButtons(session.WhitePlayerKind, mousePoint, WhitePlayerKindButtonY);
        DrawSetupPlayerSelector(session, GoStone.White, mousePoint, WhiteEngineButtonY);
        DrawCommandButton(ImportSgfButtonBounds, session.HasReviewGameRecord ? "SGF CLEAR" : "SGF INPUT", false, mousePoint);
        DrawCommandButton(StartReviewingButtonBounds, "KIFU REVIEW", false, mousePoint, enabled: session.HasReviewGameRecord, scale: 0.32f);
        DrawCommandButton(StartBoardEditingButtonBounds, "EDIT BOARD", false, mousePoint, scale: 0.36f);
        DrawCommandButton(StartPlayingButtonBounds, "START", false, mousePoint, scale: 0.48f);
    }
    private void DrawPlayingSidePanel(GoAppSession session, Point mousePoint)
    {
        DrawText("TURN", new Vector2(1144, 132), new Color(180, 195, 195), 0.62f);

        DrawInfoStrip(1144, 180, "TURN", session.GetLocalPlayerName(session.CurrentTurn));
        DrawInfoStrip(1144, 244, "NEXT", GetMoveThinkingText(session));

        DrawText("PLAYERS", new Vector2(1144, 300), new Color(180, 195, 195), 0.62f);
        DrawPlayerIdentityStrip(1144, 348, session.GetLocalPlayerName(GoStone.Black), black: true, session.CurrentTurn == GoStone.Black);
        DrawPlayerIdentityStrip(1144, 446, session.GetLocalPlayerName(GoStone.White), black: false, session.CurrentTurn == GoStone.White);

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

    private void DrawSetupPlayerSelector(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var playerKind = stone == GoStone.Black ? session.BlackPlayerKind : session.WhitePlayerKind;
        if (playerKind == GoPlayerKind.Human)
        {
            DrawHumanPlayerNameTextBox(session, stone, mousePoint, y);
            return;
        }

        var selectedIndex = stone == GoStone.Black ? session.SelectedBlackGtpEngineIndex : session.SelectedWhiteGtpEngineIndex;
        var engineName = selectedIndex >= 0 && selectedIndex < session.GtpEngineProfiles.Count
            ? session.GtpEngineProfiles[selectedIndex].DisplayName
            : "No engine";
        DrawLabeledBrowseSelector(GtpEngineSelectorBounds(y) with { Value = engineName }, mousePoint);
    }

    private void DrawHumanPlayerNameTextBox(GoAppSession session, GoStone stone, Point mousePoint, int y)
    {
        var bounds = HumanPlayerNameRowBounds(y);
        var active = session.ActiveHumanPlayerNameStone == stone;
        var text = active ? session.HumanPlayerNameDraft : session.GetHumanPlayerName(stone);
        DrawDataRowFrame(bounds, active, bounds.Contains(mousePoint));
        DrawFittedText("PLAYER NAME", new Rectangle(bounds.X + 14, bounds.Y + 10, 136, bounds.Height - 20), new Color(158, 178, 178), 0.32f);
        DrawFittedText(text, HumanPlayerNameTextBounds(y), Color.White, 0.42f);
        if (active) DrawTextBoxCaret(text, session.HumanPlayerNameCaretIndex, HumanPlayerNameTextBounds(y), 0.42f);
    }

    private void DrawPlayerIdentityStrip(int x, int y, string playerName, bool black, bool active)
    {
        var bounds = new Rectangle(x, y, 668, 56);
        DrawDataRowFrame(bounds, active);
        DrawStone(new Vector2(bounds.X + 32, bounds.Center.Y), 17, black);
        DrawFittedText(playerName, new Rectangle(bounds.X + 64, bounds.Y + 7, bounds.Width - 82, 40), Color.White, 0.5f);
        if (active) FillRect(new Rectangle(bounds.X + 62, bounds.Bottom - 5, bounds.Width - 80, 2), new Color(99, 223, 185));
    }

    private const int AddPanelControlX = 626;

    private const int AddPanelBoardSizeButtonY = 452;

    private const int BlackPlayerKindButtonY = 660;

    private const int WhitePlayerKindButtonY = 794;

    private const int BlackEngineButtonY = 724;

    private const int WhiteEngineButtonY = 856;

    private static Rectangle BoardSizeButtonBounds(int index, int y) => new(AddPanelControlX + index * 224, y, 188, 62);
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

    private static Rectangle HumanPlayerNameRowBounds(int y) => new(1144, y - 4, 668, 44);

    private static Rectangle HumanPlayerNameTextBounds(int y) => new(1308, y + 2, 488, 32);
    private static Rectangle StartPlayingButtonBounds => new(1658, 920, 154, 56);

    private static Rectangle ImportSgfButtonBounds => new(1144, 920, 154, 56);

    private static Rectangle SetupBackToTitleButtonBounds => new(1642, 104, 170, 52);
    private static Rectangle LocalUseButtonBounds => new(508, 404, 438, 300);
    private static Rectangle ReturnToSetupButtonBounds => new(1318, 910, 320, 56);

    private static Rectangle ExportSgfButtonBounds => new(1164, 910, 140, 56);

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

    private static string FormatMoveLimit(int moveLimit) =>
        moveLimit <= 0 ? "NO LIMIT" : moveLimit.ToString();

    private static string GetMoveThinkingText(GoAppSession session)
    {
        var text = $"{session.NextMoveNumber}手目を思考中";
        return session.MoveLimit <= 0 ? text : $"{text} / {session.MoveLimit}";
    }

    private static string FormatGameEndMoveCount(int playedMoveCount) => $"{playedMoveCount}手で終局";

    private static string FormatKomi(decimal komi) => komi.ToString("0.0");
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
        DrawCommandButton(selector.BrowseButtonBounds, selector.ButtonLabel, false, mousePoint, enabled: selector.Enabled, scale: 0.34f);
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
    private void DrawGlow(Vector2 center, float radius, Color color)
    {
        var destination = new Rectangle((int)(center.X - radius), (int)(center.Y - radius), (int)(radius * 2), (int)(radius * 2));
        _spriteBatch.Draw(_softCircle, destination, color);
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
}
