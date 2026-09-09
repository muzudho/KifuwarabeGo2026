namespace KifuwarabeGo2026.LobbyGui.MonoGame;

using Microsoft.Xna.Framework;
using StationeryUI.MonoGame;

public static class LobbyPortalFrame
{
    public static Rectangle Draw(KfwStationeryDrawingTools drawing, string version)
    {
        var bounds = LobbyPortalLayout.Panel;
        drawing.FillRectangle(bounds, new Color(21, 25, 32, 248));
        drawing.DrawRectangle(bounds, 2, new Color(82, 111, 114));
        drawing.DrawText("KIFUWARABE GO 2026", new Vector2(132, 101), new Color(244, 238, 218), 0.64f);
        drawing.DrawText(version, new Vector2(1630, 112), new Color(99, 223, 185), 0.34f);
        return bounds;
    }
}
