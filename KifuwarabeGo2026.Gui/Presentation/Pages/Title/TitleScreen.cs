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
        EntrySettingsLabel = new Headline("ENTRY SETTINGS", new Vector2(460, 338), new Color(125, 225, 255), 0.43f);
        FormalAppsLabel = new Headline("FORMAL APPS", new Vector2(800, 338), new Color(99, 223, 185), 0.43f);
        CasualAppsLabel = new Headline("CASUAL APPS", new Vector2(1140, 338), new Color(255, 190, 92), 0.43f);
        EngineProfilesButton = new Button(new Rectangle(460, 390, 300, 126), "エンジン登録", 0.38f);
        EntryProfilesButton = new Button(new Rectangle(460, 536, 300, 126), "エントリー登録", 0.38f);
        LocalMatchButton = new Button(new Rectangle(800, 390, 300, 126), "Local Match", 0.46f);
        CgosClientButton = new Button(new Rectangle(800, 536, 300, 126), "Online Match (CGOS)", 0.42f);
        CaptureGameButton = new Button(new Rectangle(1140, 390, 300, 126), "ポン抜きゲーム", 0.40f);
        BackButton = new Button(new Rectangle(1260, 316, 152, 54), "BACK", 0.36f);
        UpdateButton = new Button(new Rectangle(1548, 972, 220, 62), "ランチャーを開く", 0.20f);
        SettingsButton = new Button(new Rectangle(1780, 972, 70, 62), string.Empty, 0.1f);
    }

    public Rectangle PanelBounds { get; } = new(420, 172, 1080, 736);
    public Rectangle EntrySettingsLabelBounds { get; } = new(450, 322, 300, 62);
    public Rectangle FormalAppsLabelBounds { get; } = new(790, 322, 300, 62);
    public Rectangle CasualAppsLabelBounds { get; } = new(1130, 322, 300, 62);

    public Headline Headline { get; }

    #region ［FORMAL APPS］
    public Headline EntrySettingsLabel { get; }
    public Headline FormalAppsLabel { get; }

    public Button LocalMatchButton { get; }

    public Button CgosClientButton { get; }
    #endregion

    #region ［CASUAL APPS］
    public Headline CasualAppsLabel { get; }

    public Button CaptureGameButton { get; }
    public Button GameOasisButton { get; } = new(new Rectangle(1140, 536, 300, 126), "Game Oasis", 0.44f);
    public Button GameOasisGoButton { get; } = new(new Rectangle(560, 430, 380, 180), "GO", 0.62f);
    public Button GameOasisPonnukiButton { get; } = new(new Rectangle(980, 430, 380, 180), "PONNUKI", 0.52f);
    public Button EngineProfilesButton { get; }
    public Button EntryProfilesButton { get; }

    public int? GetAppHit(Point point) => CaptureGameButton.IsHit(point) ? 0 : null;
    #endregion

    public Button BackButton { get; }

    #region ［右下のボタン］
    public Button UpdateButton { get; }

    public Button SettingsButton { get; }
    #endregion
}
