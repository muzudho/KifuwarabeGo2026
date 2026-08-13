namespace KifuwarabeGo2026.Gui.Presentation;

using KifuwarabeGo2026.Gui.Application.Local.Playing;
using KifuwarabeGo2026.Gui.Presentation.Pages.InitialPositionConcierge;
using Microsoft.Xna.Framework;

/// <summary>GoScreenRenderer と初期局面コンシェルジュを接続します。</summary>
public sealed partial class GoScreenRenderer
{
    public InitialPositionConcierge InitialPositionConcierge { get; } = new();

    private void DrawInitialPositionConcierge(InitialPositionConciergeView view, Point mousePoint) =>
        InitialPositionConcierge.Draw(view, mousePoint, new InitialPositionConciergeDrawingCallbacks(DrawDynamicOptionText, DrawFittedText, DrawText, FillRect, DrawRect, DrawCommandButton));
}
