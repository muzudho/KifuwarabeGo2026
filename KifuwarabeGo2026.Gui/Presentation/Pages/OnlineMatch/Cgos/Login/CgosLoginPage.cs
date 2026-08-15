namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>CGOS ログイン画面の描画と操作ボタンを所有します。</summary>
public sealed class CgosLoginPage
{
    public static CgosLoginPage Default { get; } = new();

    private CgosLoginPage()
    {
        BackButton = Button(1308, 244, 324, 48, "BACK", .42f);
        AdminConnectButton = Button(1220, 648, 396, 48, "CONNECT", .34f);
        AdminWhoButton = Button(1220, 756, 396, 36, "WHO", .28f);
        AdminWhitePlayerButton = Button(1282, 801, 326, 28, "", .22f);
        AdminBlackPlayerButton = Button(1282, 843, 326, 28, "", .22f);
        AdminMatchButton = Button(1428, 882, 198, 28, "MATCH", .22f);
        AdminSwapButton = Button(1220, 882, 198, 28, "SWAP", .22f);
        AdminTailButton = Button(1364, 706, 120, 44, "TAIL", .28f);
        AdminCodeButton = Button(1498, 706, 120, 44, "CODE", .24f);
        BlackConnectButton = Button(304, 748, 396, 48, "CONNECT", .34f);
        BlackResignButton = Button(508, 748, 192, 48, "RESIGN", .34f);
        BlackTailButton = Button(448, 806, 120, 44, "TAIL", .28f);
        BlackCodeButton = Button(582, 806, 120, 44, "CODE", .24f);
        WhiteConnectButton = Button(762, 748, 396, 48, "CONNECT", .34f);
        WhiteResignButton = Button(966, 748, 192, 48, "RESIGN", .34f);
        WhiteTailButton = Button(906, 806, 120, 44, "TAIL", .28f);
        WhiteCodeButton = Button(1040, 806, 120, 44, "CODE", .24f);
        BeginButton = Button(1134, 800, 302, 58, "BEGIN", .34f);
        LogCodeButton = Button(1286, 610, 60, 32, "CODE", .24f);
        LogNotepadButton = Button(1352, 610, 72, 32, "NOTEPAD", .20f);
        ErrorLogCodeButton = Button(1286, 686, 60, 32, "CODE", .24f);
        ErrorLogNotepadButton = Button(1352, 686, 72, 32, "NOTEPAD", .20f);
        PlayerDialogCancelButton = Button(1108, 200, 120, 48, "CANCEL", .34f);
        PlayerDialogSelectButton = Button(1240, 200, 130, 48, "SELECT", .34f);
        PlayerDialogPreviousButton = Button(1050, 810, 100, 44, "PREV", .42f);
        PlayerDialogNextButton = Button(1160, 810, 100, 44, "NEXT", .42f);
    }

    public void Draw(CgosLoginRenderer renderer, StationeryDrawingContext drawingContext, GoAppSession session, Point mousePosition) =>
        renderer.Draw(drawingContext, session, mousePosition);

    public void UpdateGameInProgressButtons(bool gameInProgress)
    {
        BlackConnectButton.Bounds = new Rectangle(304, 748, gameInProgress ? 192 : 396, 48);
        WhiteConnectButton.Bounds = new Rectangle(762, 748, gameInProgress ? 192 : 396, 48);
    }

    public Button BackButton { get; }
    public Button AdminConnectButton { get; }
    public Button AdminWhoButton { get; }
    public Button AdminWhitePlayerButton { get; }
    public Button AdminBlackPlayerButton { get; }
    public Button AdminMatchButton { get; }
    public Button AdminSwapButton { get; }
    public Button AdminTailButton { get; }
    public Button AdminCodeButton { get; }
    public Button BlackConnectButton { get; }
    public Button BlackResignButton { get; }
    public Button BlackTailButton { get; }
    public Button BlackCodeButton { get; }
    public Button WhiteConnectButton { get; }
    public Button WhiteResignButton { get; }
    public Button WhiteTailButton { get; }
    public Button WhiteCodeButton { get; }
    public Button BeginButton { get; }
    public Button LogCodeButton { get; }
    public Button LogNotepadButton { get; }
    public Button ErrorLogCodeButton { get; }
    public Button ErrorLogNotepadButton { get; }
    public Button PlayerDialogCancelButton { get; }
    public Button PlayerDialogSelectButton { get; }
    public Button PlayerDialogPreviousButton { get; }
    public Button PlayerDialogNextButton { get; }

    private static Button Button(int x, int y, int width, int height, string label, float scale) =>
        new(new Rectangle(x, y, width, height), label, scale);
}
