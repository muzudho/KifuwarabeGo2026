namespace KifuwarabeGo2026.Presentation.CgosConnect;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;

public static class CgosConnectRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition) =>
        renderer.DrawCgosClientTop(session, mousePosition);
}
