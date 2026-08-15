namespace KifuwarabeGo2026.Gui.Presentation.StationeryUI.MessageDialog;

using KifuwarabeGo2026.Gui.Application.Updates;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI;
using Microsoft.Xna.Framework;
using System;

/// <summary>GUI 自動更新の現在位置と完了した工程を表示するモーダルダイアログ。</summary>
public sealed class GuiUpdateProgressDialog
{
    private static readonly (GuiReleaseUpdateStep Step, string Label)[] Steps =
    [
        (GuiReleaseUpdateStep.CheckingRelease, "最新リリースを確認"),
        (GuiReleaseUpdateStep.DownloadingPackage, "GUI パッケージをダウンロード"),
        (GuiReleaseUpdateStep.ExtractingPackage, "パッケージを展開"),
        (GuiReleaseUpdateStep.VerifyingPackage, "更新版を検証"),
        (GuiReleaseUpdateStep.StartingUpdatedGui, "更新版 GUI を起動"),
    ];

    public static readonly Rectangle Bounds = new(510, 260, 900, 560);
    public static readonly Rectangle CloseButtonBounds = new(1218, 742, 154, 48);
    private volatile GuiReleaseUpdateProgress _progress = new(GuiReleaseUpdateStep.CheckingRelease, "更新を開始しています。");

    public bool HasFailed { get; private set; }
    public string FailureMessage { get; private set; } = "";
    public bool CanClose => HasFailed;

    public void Report(GuiReleaseUpdateProgress progress) => _progress = progress ?? throw new ArgumentNullException(nameof(progress));

    public void Fail(string logPath)
    {
        HasFailed = true;
        FailureMessage = $"最新バージョンへの更新は失敗しました。\nログファイル: {logPath}\nGUI を再起動してやり直してください。";
    }

    public bool IsCloseHit(Point point) => CanClose && CloseButtonBounds.Contains(point);

    public void Draw(KfwStationeryDrawingTools drawingContext, Point mousePosition)
    {
        var mousePoint = drawingContext.ToVirtualPoint(mousePosition);
        drawingContext.Begin();
        drawingContext.FillRectangle(new Rectangle(0, 0, VirtualScreen.Width, VirtualScreen.Height), new Color(0, 0, 0, 170));
        drawingContext.FillRectangle(new Rectangle(Bounds.X + 14, Bounds.Y + 16, Bounds.Width, Bounds.Height), new Color(0, 0, 0, 120));
        drawingContext.FillRectangle(Bounds, new Color(21, 25, 32, 252));
        drawingContext.DrawRectangle(Bounds, 2, HasFailed ? new Color(255, 145, 151) : new Color(99, 223, 185));
        drawingContext.DrawDynamicText(HasFailed ? "GUI UPDATE FAILED" : "GUI UPDATE IN PROGRESS",
            new Rectangle(Bounds.X + 42, Bounds.Y + 30, 700, 48), new Color(244, 238, 218), 0.58f);
        drawingContext.DrawLine(new Vector2(Bounds.X + 42, Bounds.Y + 94), new Vector2(Bounds.Right - 42, Bounds.Y + 94), 1, new Color(82, 111, 114));

        var currentIndex = Array.FindIndex(Steps, item => item.Step == _progress.Step);
        if (_progress.Step == GuiReleaseUpdateStep.Completed) currentIndex = Steps.Length;
        for (var index = 0; index < Steps.Length; index++)
        {
            var status = index < currentIndex ? "[DONE]" : index == currentIndex && HasFailed ? "[FAIL]" : index == currentIndex ? "[....]" : "[    ]";
            var color = index < currentIndex ? new Color(151, 255, 215) : index == currentIndex && HasFailed ? new Color(255, 145, 151) : index == currentIndex ? Color.White : new Color(112, 126, 132);
            drawingContext.DrawDynamicText($"{status}  {Steps[index].Label}",
                new Rectangle(Bounds.X + 62, Bounds.Y + 122 + index * 48, Bounds.Width - 124, 38), color, 0.38f);
        }

        var detail = HasFailed ? FailureMessage : _progress.Message;
        drawingContext.DrawDynamicText(detail,
            new Rectangle(Bounds.X + 62, Bounds.Y + 380, Bounds.Width - 124, 92),
            HasFailed ? new Color(255, 190, 180) : new Color(180, 195, 195), HasFailed ? 0.29f : 0.34f);
        if (CanClose)
            drawingContext.DrawButton(CloseButtonBounds, "CLOSE", false, mousePoint, true, 0.32f);
        drawingContext.End();
    }
}
