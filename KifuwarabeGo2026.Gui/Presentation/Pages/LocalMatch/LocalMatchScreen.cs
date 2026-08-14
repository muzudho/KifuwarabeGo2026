namespace KifuwarabeGo2026.Gui.Presentation.Pages.LocalMatch;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>ローカル対局のセットアップ、対局中、終局後に共通する操作 UI を所有します。</summary>
public sealed class LocalMatchScreen
{
    public static LocalMatchScreen Default { get; } = new();

    private LocalMatchScreen()
    {
        StartPlayingButton = new Button(new Rectangle(1658, 920, 154, 56), "START", 0.48f);
        ChangeAppProviderButton = new Button(new Rectangle(1658, 556, 154, 52), "CHANGE", 0.28f);
        AppProviderGameSettingsButton = new Button(new Rectangle(1328, 556, 320, 52), "GAME SETTINGS", 0.32f);
        ProviderSeedAutoChangeButton = new Button(new Rectangle(1164, 870, 200, 32), "PROVIDER", 0.22f);
        Player1SeedAutoChangeButton = new Button(new Rectangle(1378, 870, 200, 32), "BLACK", 0.22f);
        Player2SeedAutoChangeButton = new Button(new Rectangle(1592, 870, 200, 32), "WHITE", 0.22f);
        ImportSgfButton = new Button(new Rectangle(1492, 184, 320, 56), "KIFU INPUT (SGF)", 0.34f);
        BackToTitleButton = new Button(new Rectangle(1642, 104, 170, 52), "BACK TO TITLE", 0.32f);
        ReturnToSetupButton = new Button(new Rectangle(1492, 132, 320, 56), "BACK TO SETUP", 0.34f);
        ExportSgfButton = new Button(new Rectangle(1164, 910, 306, 56), "SGF OUTPUT", 0.52f);
        GameOverReviewButton = new Button(new Rectangle(1486, 910, 306, 56), "KIFU REVIEW", 0.36f);
        PassButton = new Button(new Rectangle(1144, 920, 320, 72), "PASS", 0.62f);
        ResignButton = new Button(new Rectangle(1492, 920, 320, 72), "RESIGN", 0.62f);
        CancelPlayingButton = new Button(new Rectangle(1144, 920, 668, 72), "CANCEL", 0.62f);
    }

    public Rectangle LocalUseCardBounds { get; } = new(508, 404, 438, 300);
    public Button StartPlayingButton { get; }
    public Button ChangeAppProviderButton { get; }
    public Button AppProviderGameSettingsButton { get; }
    public Button ProviderSeedAutoChangeButton { get; }
    public Button Player1SeedAutoChangeButton { get; }
    public Button Player2SeedAutoChangeButton { get; }
    public Button ImportSgfButton { get; }
    public Button BackToTitleButton { get; }
    public Button ReturnToSetupButton { get; }
    public Button ExportSgfButton { get; }
    public Button GameOverReviewButton { get; }
    public Button PassButton { get; }
    public Button ResignButton { get; }
    public Button CancelPlayingButton { get; }
}
