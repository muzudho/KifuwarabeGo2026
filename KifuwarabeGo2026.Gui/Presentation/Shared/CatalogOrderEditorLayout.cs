namespace KifuwarabeGo2026.Gui.Presentation;

using Microsoft.Xna.Framework;

/// <summary>
/// 大会ルールやエンジン設定などで共用する順序編集ポップアップの配置です。
/// </summary>
public static class CatalogOrderEditorLayout
{
    public static Rectangle Bounds => new(250, 142, 1420, 790);
    public static Rectangle BoardBounds => new(286, 246, 1040, 566);
    public static Rectangle PropertyBounds => new(1350, 246, 284, 566);
    public static Rectangle CancelButtonBounds => new(1350, 166, 132, 48);
    public static Rectangle SaveButtonBounds => new(1500, 166, 134, 48);
    public static Rectangle PreviousPairButtonBounds => new(1086, 830, 110, 44);
    public static Rectangle NextPairButtonBounds => new(1216, 830, 110, 44);
    public static Rectangle TopButtonBounds => new(1370, 424, 244, 44);
    public static Rectangle PageUpButtonBounds => new(1370, 482, 244, 44);
    public static Rectangle UpButtonBounds => new(1370, 540, 244, 44);
    public static Rectangle DownButtonBounds => new(1370, 598, 244, 44);
    public static Rectangle PageDownButtonBounds => new(1370, 656, 244, 44);

    public static Rectangle CardBounds(int visibleIndex, int pageSize)
    {
        var column = visibleIndex / pageSize;
        var row = visibleIndex % pageSize;
        return new Rectangle(BoardBounds.X + 16 + column * 512, BoardBounds.Y + 50 + row * 82, 496, 68);
    }
}
