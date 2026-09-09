namespace KifuwarabeGo2026.LobbyGui.MonoGame;

using KifuwarabeGo2026.LobbyGui.Application;
using Microsoft.Xna.Framework;
using StationeryUI.MonoGame;

/// <summary>ホストが所有するレイアウトと共通コントロールへの描画Port。</summary>
public interface ILobbyPageLayout
{
    Rectangle GetItemBounds(LobbyHomeTarget target);
    Rectangle GetSectionBounds(LobbyHomeTarget target);
    Rectangle GetGameOasisItemBounds(int index);
    void DrawHomeLabels(KfwStationeryDrawingTools drawingContext);
    void DrawBackButton(Point mousePoint, KfwStationeryDrawingTools drawingContext, bool focused);
}
