namespace KifuwarabeGo2026.Presentation.Cgos.Watching;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Application.Cgos.Watching;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;

/// <summary>
/// CGOS 対局の観戦・結果画面を描画します。
/// </summary>
public static class CgosWatchingRenderer
{
    /// <summary>
    /// CGOS 対局の観戦・結果画面を描画します。
    /// </summary>
    /// <param name="renderer"></param>
    /// <param name="session"></param>
    /// <param name="observation"></param>
    /// <param name="mousePosition"></param>
    public static void Draw(GoScreenRenderer renderer, GoAppSession session, CgosGameObservation observation, Point mousePosition) =>
        renderer.DrawCgosWatching(session, observation, mousePosition);
}
