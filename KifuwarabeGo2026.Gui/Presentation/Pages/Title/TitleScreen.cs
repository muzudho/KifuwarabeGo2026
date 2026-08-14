namespace KifuwarabeGo2026.Gui.Presentation.Pages.Title;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using Microsoft.Xna.Framework;

/// <summary>タイトル画面のレイアウトと操作コントロールを所有します。</summary>
public sealed class TitleScreen
{
    public static TitleScreen Default { get; } = new();

    private TitleScreen()
    {
        Headline = new Headline("KIFUWARABE GO 2026", new Vector2(478, 230), new Color(244, 238, 218), 1.05f);
        FormalAppsLabel = new Headline("FORMAL APPS", new Vector2(500, 338), new Color(99, 223, 185), 0.48f);
        CasualAppsLabel = new Headline("CASUAL APPS", new Vector2(950, 338), new Color(255, 190, 92), 0.48f);
        LocalMatchButton = new Button(new Rectangle(500, 390, 400, 126), "Local Match", 0.52f);
        CgosClientButton = new Button(new Rectangle(500, 536, 400, 126), "Online Match (CGOS)", 0.52f);
        CaptureGameButton = new Button(new Rectangle(950, 390, 440, 84), "ポン抜きゲーム", 0.43f);
        BackButton = new Button(new Rectangle(1260, 316, 152, 54), "BACK", 0.36f);
        UpdateButton = new Button(new Rectangle(1698, 972, 70, 62), string.Empty, 0.1f);
        SettingsButton = new Button(new Rectangle(1780, 972, 70, 62), string.Empty, 0.1f);
    }

    public Rectangle PanelBounds { get; } = new(420, 172, 1080, 736);

    public Headline Headline { get; }

    #region ［FORMAL APPS］
    public Headline FormalAppsLabel { get; }

    public Button LocalMatchButton { get; }

    public Button CgosClientButton { get; }
    #endregion

    #region ［CASUAL APPS］
    public Headline CasualAppsLabel { get; }

    public Button CaptureGameButton { get; }
    #endregion

    public Button BackButton { get; }

    #region ［右下のボタン］
    public Button UpdateButton { get; }

    public Button SettingsButton { get; }
    #endregion
}
