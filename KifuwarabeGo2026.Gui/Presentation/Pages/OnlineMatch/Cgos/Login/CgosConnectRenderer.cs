namespace KifuwarabeGo2026.Gui.Presentation.Pages.OnlineMatch.Cgos.Login;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation;
using Microsoft.Xna.Framework;

public static class CgosConnectRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition) =>
        renderer.DrawCgosClientTop(session, mousePosition);
}
