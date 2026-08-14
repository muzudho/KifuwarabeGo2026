namespace KifuwarabeGo2026.Gui.Presentation.Pages.PonnukiProviderSelection;

using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Headline;
using Microsoft.Xna.Framework;

/// <summary>ポン抜きゲームで使用するアプリプロバイダーを選択する画面です。</summary>
public sealed class PonnukiProviderSelectionScreen
{
    public static PonnukiProviderSelectionScreen Default { get; } = new();

    private PonnukiProviderSelectionScreen()
    {
        Headline = new Headline("ポン抜きゲーム", new Vector2(500, 350), Color.White, 0.62f);
        ProviderLabel = new Headline("APP PROVIDER ENGINE", new Vector2(530, 416), new Color(255, 190, 92), 0.42f);
        BackButton = new Button(new Rectangle(1260, 316, 152, 54), "BACK", 0.36f);
        RecheckButton = new Button(new Rectangle(828, 826, 340, 54), "RECHECK PROVIDER", 0.30f);
        StartButton = new Button(new Rectangle(1198, 826, 152, 54), "NEXT", 0.40f);
    }

    public Headline Headline { get; }
    public Headline ProviderLabel { get; }
    public Rectangle ProviderDisplayBounds { get; } = new(570, 466, 780, 56);
    public Rectangle ProviderTextBounds { get; } = new(712, 473, 638, 42);
    public Rectangle CapabilityStatusBounds { get; } = new(570, 794, 780, 26);
    public Button BackButton { get; }
    public Button RecheckButton { get; }
    public Button StartButton { get; }
}
