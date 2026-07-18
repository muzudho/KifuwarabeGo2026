namespace KifuwarabeGo2026.Presentation.Cgos.Watching;

using KifuwarabeGo2026.Application.Cgos.Watching;
using KifuwarabeGo2026.Presentation;
using Microsoft.Xna.Framework;

/// <summary>
/// CGOS 対局の観戦・結果画面を描画します。
/// </summary>
public static class CgosWatchingRenderer
{
    public static void Draw(GoScreenRenderer renderer, CgosGameObservation observation, Point mousePosition) =>
        renderer.DrawCgosWatching(observation, mousePosition);
}
