namespace KifuwarabeGo2026.LobbyGui.MonoGame;

using Microsoft.Xna.Framework;
using KifuwarabeGo2026.LobbyGui.Application;

/// <summary>1920x1080の論理座標。ポータルの描画と入力が共用します。</summary>
public static class LobbyPortalLayout
{
    public static Rectangle Panel => new(100, 76, 1720, 932);
    public static Rectangle Engine => new(132, 166, 260, 66);
    public static Rectangle Entry => new(404, 166, 260, 66);
    public static Rectangle Settings => new(884, 166, 180, 66);
    public static Rectangle UpdateInstaller => new(1076, 166, 350, 66);
    public static Rectangle OpenInstaller => new(1438, 166, 350, 66);
    public static Rectangle Previous => new(132, 918, 180, 58);
    public static Rectangle Next => new(1608, 918, 180, 58);
    public static Rectangle Card(int slot) => slot is >= 0 and < LobbyHomePresenter.PageSize
        ? new(132 + slot % 2 * 840, 320 + slot / 2 * 284, 816, 260)
        : throw new ArgumentOutOfRangeException(nameof(slot));
    public static int? HitCard(Point point, int visibleCount)
    {
        for (var slot = 0; slot < Math.Min(visibleCount, LobbyHomePresenter.PageSize); slot++)
            if (Card(slot).Contains(point)) return slot;
        return null;
    }
}
