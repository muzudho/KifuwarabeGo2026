namespace KifuwarabeGo2026.Gui.Presentation.Shared.SelectEntry;

using Microsoft.Xna.Framework;

/// <summary>
/// ローカル対局設定で使用するプレイヤー選択欄の配置です。
/// </summary>
public static class PlayerSelectorLayout
{
    /// <summary>SELECT ボタンの高さ（仮想画面上のピクセル）です。</summary>
    public const int SelectButtonHeight = 36; //28;

    /// <summary>SELECT ボタンのラベルに使う文字倍率です。</summary>
    public const float SelectButtonLabelScale = 1.0f; //0.50f;

    public static PlayerSelector CreateComputerEngineSelector(int y) =>
        new(new Rectangle(1144, y - 4, 668, 44), "NAME", "", "SELECT");

    public static PlayerSelector CreatePlayerSelector(int y) =>
        new(new Rectangle(1144, y - 4, 668, 56), "PLAYER", "", "SELECT", LabelWidth: 168);
}
