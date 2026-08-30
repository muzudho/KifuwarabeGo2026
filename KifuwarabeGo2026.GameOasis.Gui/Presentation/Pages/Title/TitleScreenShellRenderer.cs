namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;

using KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.ApplicationSettings;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.Shared.TitleBackground;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.GameOasis.Gui.Presentation.StationeryUI.Controls.StickyNote;
using Microsoft.Xna.Framework;
using System;

/// <summary>Lobbyページ内容の外側にある互換タイトル画面の装飾と共通操作を描画します。</summary>
public sealed class TitleScreenShellRenderer
{
    private readonly TitleGoEquipment _titleGoEquipment = new();
    private readonly Action<Vector2, float, float, Color, int, float> _drawEllipseWire;
    private readonly Action<Vector2, float, float, Color, int, float, float, float> _drawCircumscribedCircleArc;

    public TitleScreenShellRenderer(
        Action<Vector2, float, float, Color, int, float> drawEllipseWire,
        Action<Vector2, float, float, Color, int, float, float, float> drawCircumscribedCircleArc)
    {
        _drawEllipseWire = drawEllipseWire;
        _drawCircumscribedCircleArc = drawCircumscribedCircleArc;
    }

    public Rectangle DrawFrame(KfwStationeryDrawingTools drawingContext)
    {
        _titleGoEquipment.Draw(new TitleGoEquipmentDrawingCallbacks(
            _drawEllipseWire, _drawCircumscribedCircleArc));

        var titleScreen = TitleScreen.Default;
        var panel = titleScreen.PanelBounds;
        drawingContext.FillRectangle(
            new Rectangle(panel.X + 18, panel.Y + 20, panel.Width, panel.Height),
            new Color(0, 0, 0, 130));
        drawingContext.FillRectangle(panel, new Color(21, 25, 32, 242));
        drawingContext.DrawRectangle(panel, 2, new Color(82, 111, 114));

        titleScreen.Headline.Position = new Vector2(panel.X + 58, panel.Y + 58);
        titleScreen.Headline.Draw(drawingContext);
        drawingContext.DrawText(GetDisplayVersion(),
            new Vector2(panel.X + 790, panel.Y + 91), new Color(99, 223, 185), 0.38f);
        drawingContext.DrawLine(new Vector2(panel.X + 790, panel.Y + 126),
            new Vector2(panel.X + 958, panel.Y + 126), 2, new Color(99, 223, 185, 120));
        return panel;
    }

    public Vector2 SettingsHintConnectorTarget
    {
        get
        {
            var bounds = ApplicationSettingsScreen.Default.SettingsButton.Bounds;
            return new Vector2(bounds.Left - 14, bounds.Center.Y);
        }
    }

    public void DrawControls(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        bool showHoverHints, Action<Vector2> drawSettingsHint)
    {
        if (showHoverHints)
            DrawControlHint(drawingContext, mousePoint, drawSettingsHint);
        DrawLauncherButton(drawingContext, ApplicationSettingsScreen.Default.UpdateButton.Bounds,
            "ランチャーを更新", mousePoint, drawBoardIcon: false);
        DrawLauncherButton(drawingContext, ApplicationSettingsScreen.Default.OpenLauncherButton.Bounds,
            "ランチャーを開く", mousePoint, drawBoardIcon: true);
        ApplicationSettingsScreen.Default.DrawSettingsButton(drawingContext, mousePoint);
    }

    private void DrawControlHint(KfwStationeryDrawingTools drawingContext, Point mousePoint,
        Action<Vector2> drawSettingsHint)
    {
        var openLauncherBounds = ApplicationSettingsScreen.Default.OpenLauncherButton.Bounds;
        var updateBounds = ApplicationSettingsScreen.Default.UpdateButton.Bounds;
        var settingsBounds = ApplicationSettingsScreen.Default.SettingsButton.Bounds;
        if (openLauncherBounds.Contains(mousePoint))
            drawingContext.DrawStickyNote(StickyNoteKind.TitleUpdateHint,
                new Vector2(openLauncherBounds.Left, openLauncherBounds.Center.Y),
                new Color(99, 223, 185), new Color(82, 111, 114),
                "ランチャーを開くとは？",
                ["共通ランチャーを前面に開き、", "このGUIを閉じます。", "GUIとEngineの更新は", "ランチャーから行います！"]);
        else if (updateBounds.Contains(mousePoint))
            drawingContext.DrawStickyNote(StickyNoteKind.TitleUpdateHint,
                new Vector2(updateBounds.Left, updateBounds.Center.Y),
                new Color(125, 225, 255), new Color(82, 111, 114),
                "ランチャーを更新するとは？",
                ["ランチャーを最新版にします。", "更新後、デスクトップへ", "ショートカットを作れます。"]);
        else if (settingsBounds.Contains(mousePoint))
            drawSettingsHint(SettingsHintConnectorTarget);
    }

    private static void DrawLauncherButton(KfwStationeryDrawingTools drawingContext,
        Rectangle bounds, string label, Point mousePoint, bool drawBoardIcon)
    {
        var hovered = bounds.Contains(mousePoint);
        var color = hovered ? new Color(99, 223, 185) : new Color(180, 195, 195);
        drawingContext.FillRectangle(bounds, hovered ? new Color(36, 50, 58) : new Color(24, 31, 37));
        drawingContext.DrawRectangle(bounds, 2, hovered ? new Color(178, 219, 226) : new Color(82, 111, 114));
        var board = new Rectangle(bounds.X + 12, bounds.Y + 11, 40, 40);
        drawingContext.DrawRectangle(board, 2, color);
        for (var index = 1; index < 5; index++)
        {
            var offset = index * 8;
            drawingContext.DrawLine(new Vector2(board.X + offset, board.Y),
                new Vector2(board.X + offset, board.Bottom), 1, color);
            drawingContext.DrawLine(new Vector2(board.X, board.Y + offset),
                new Vector2(board.Right, board.Y + offset), 1, color);
        }
        if (drawBoardIcon)
        {
            drawingContext.DrawStone(new Vector2(board.X + 16, board.Y + 24), 5, true);
            drawingContext.DrawStone(new Vector2(board.X + 31, board.Y + 16), 5, false);
        }
        else
        {
            drawingContext.DrawLine(new Vector2(board.X + 11, board.Center.Y),
                new Vector2(board.Right - 10, board.Center.Y), 3, color);
            drawingContext.DrawLine(new Vector2(board.Right - 17, board.Center.Y - 7),
                new Vector2(board.Right - 10, board.Center.Y), 3, color);
            drawingContext.DrawLine(new Vector2(board.Right - 17, board.Center.Y + 7),
                new Vector2(board.Right - 10, board.Center.Y), 3, color);
        }
        drawingContext.DrawDynamicText(label,
            new Rectangle(bounds.X + 56, bounds.Y + 11, bounds.Width - 62, 42), color, 0.38f);
    }

    private static string GetDisplayVersion()
    {
        var version = typeof(TitleScreenShellRenderer).Assembly.GetName().Version;
        return version is null ? "VERSION" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }
}
