namespace KifuwarabeGo2026.Presentation.Local.Resting;

using KifuwarabeGo2026.Application;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;

public static class LocalRestingRenderer
{
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, Point mousePosition) =>
        renderer.Draw(session, mousePosition);
}
