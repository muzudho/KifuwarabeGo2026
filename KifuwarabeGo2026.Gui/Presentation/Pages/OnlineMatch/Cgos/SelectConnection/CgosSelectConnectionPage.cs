namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.SelectConnection;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;
using KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;

/// <summary>CGOS 接続先選択画面の描画と操作ボタンを所有します。</summary>
public sealed class CgosSelectConnectionPage
{
    public static CgosSelectConnectionPage Default { get; } = new();

    private CgosSelectConnectionPage()
    {
        CancelButton = Button(1368, 156, 132, 48, "CANCEL", .34f);
        SelectButton = Button(1518, 156, 132, 48, "SELECT", .34f);
        PreviousButton = Button(730, 816, 90, 44, "PREV", .42f);
        NextButton = Button(830, 816, 90, 44, "NEXT", .42f);
        AddButton = Button(270, 874, 100, 44, "ADD", .38f);
        EditButton = Button(380, 874, 100, 44, "EDIT", .38f);
        DuplicateButton = Button(490, 874, 120, 44, "DUPLICATE", .25f);
        DeleteButton = Button(620, 874, 100, 44, "DELETE", .34f);
        OrderButton = Button(740, 874, 120, 44, "ORDER", .34f);
        EditDiscardButton = Button(1144, 156, 132, 48, "DISCARD", .30f);
        EditSaveButton = Button(1288, 156, 162, 48, "SAVE & CLOSE", .27f);
    }

    public void Draw(CgosLoginRenderer renderer, StationeryDrawingContext drawingContext, GoAppSession session, Point mousePosition) =>
        renderer.Draw(drawingContext, session, mousePosition);

    public Button CancelButton { get; }
    public Button SelectButton { get; }
    public Button PreviousButton { get; }
    public Button NextButton { get; }
    public Button AddButton { get; }
    public Button EditButton { get; }
    public Button DuplicateButton { get; }
    public Button DeleteButton { get; }
    public Button OrderButton { get; }
    public Button EditDiscardButton { get; }
    public Button EditSaveButton { get; }

    private static Button Button(int x, int y, int width, int height, string label, float scale) =>
        new(new Rectangle(x, y, width, height), label, scale);
}
