namespace KifuwarabeGo2026.Gui.Presentation.Cgos.ConnectionTarget;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class CgosConnectionTargetRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition) =>
        renderer.DrawCgosClientTop(session, mousePosition);
}
