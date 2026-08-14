namespace KifuwarabeGo2026.Gui.Presentation.Shared.TextAreaDialog;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>コメント入力ダイアログの領域と操作 UI を所有します。</summary>
public sealed class TextAreaDialog
{
    public static TextAreaDialog Default { get; } = new();

    private TextAreaDialog()
    {
        DiscardButton = new Button(new Rectangle(1230, 172, 150, 54), "DISCARD", 0.30f);
        ApplyButton = new Button(new Rectangle(1410, 172, 150, 54), "CLOSE", 0.34f);
    }

    public Rectangle Bounds { get; } = new(320, 150, 1280, 780);
    public Rectangle TextBounds { get; } = new(390, 330, 1140, 400);
    public Button DiscardButton { get; }
    public Button ApplyButton { get; }

    public void SetHasChanges(bool hasChanges)
    {
        DiscardButton.IsEnabled = hasChanges;
        ApplyButton.Label = hasChanges ? "SAVE & CLOSE" : "CLOSE";
        ApplyButton.LabelScale = hasChanges ? 0.25f : 0.34f;
    }
}
