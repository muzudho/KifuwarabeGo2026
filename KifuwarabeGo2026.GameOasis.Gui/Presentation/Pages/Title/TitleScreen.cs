namespace KifuwarabeGo2026.GameOasis.Gui.Presentation.Pages.Title;

using StationeryUI.MonoGame.Controls.Button;
using StationeryUI.MonoGame.Controls.Headline;
using KifuwarabeGo2026.LobbyGui.Application;
using Microsoft.Xna.Framework;
using System;
using KifuwarabeGo2026.LobbyGui.MonoGame;

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
        EngineProfilesButton = new Button(LobbyPortalLayout.Engine, "エンジン登録", 0.38f);
        EntryProfilesButton = new Button(LobbyPortalLayout.Entry, "エントリー登録", 0.38f);
        LocalMatchButton = new Button(LobbyPortalLayout.Card(0), "囲碁ローカルマッチ", 0.46f);
        CgosClientButton = new Button(LobbyPortalLayout.Card(1), "囲碁オンラインマッチ（CGOS）", 0.42f);
        CaptureGameButton = new Button(LobbyPortalLayout.Card(2), "ポン抜き", 0.40f);
        BackButton = new Button(new Rectangle(1260, 316, 152, 54), "BACK", 0.36f);
        UpdateButton = new Button(new Rectangle(1548, 972, 220, 62), "インストーラーを起動", 0.20f);
        SettingsButton = new Button(new Rectangle(1780, 972, 70, 62), string.Empty, 0.1f);
    }

    public Rectangle PanelBounds { get; } = new(420, 172, 1080, 736);
    public Rectangle EntrySettingsLabelBounds { get; } = new(450, 322, 300, 62);
    public Rectangle FormalAppsLabelBounds { get; } = new(790, 322, 300, 62);
    public Rectangle CasualAppsLabelBounds { get; } = new(1130, 322, 300, 62);
    public Rectangle GamePlatformLabelBounds { get; } = new(450, 704, 980, 46);

    public Headline Headline { get; }

    #region ［FORMAL APPS］
    public Headline EntrySettingsLabel { get; }
    public Headline FormalAppsLabel { get; }

    public Button LocalMatchButton { get; }

    public Button CgosClientButton { get; }
    #endregion

    #region ［CASUAL APPS］
    public Headline CasualAppsLabel { get; }
    public Headline GamePlatformLabel { get; } = new("GAME PLATFORM", new Vector2(460, 716), new Color(178, 145, 255), 0.43f);

    public Button CaptureGameButton { get; }
    public Button GameOasisButton { get; } = new(LobbyPortalLayout.Card(3), "コンピューター囲碁サンプル", 0.44f);
    public Button GameOasisGoButton { get; } = new(new Rectangle(560, 430, 380, 180), "GO", 0.62f);
    public Button GameOasisPonnukiButton { get; } = new(new Rectangle(980, 430, 380, 180), "PONNUKI", 0.52f);
    public Button EngineProfilesButton { get; }
    public Button EntryProfilesButton { get; }

    public LobbyHomeTarget? GetHomeTargetHit(Point point, LobbyHomePresentation? home = null)
    {
        if (EngineProfilesButton.IsHit(point)) return LobbyHomeTarget.EngineProfiles;
        if (EntryProfilesButton.IsHit(point)) return LobbyHomeTarget.EntryProfiles;
        var presentation = home ?? LobbyHomePresenter.Create();
        return LobbyPortalLayout.HitCard(point, presentation.VisibleItems.Count) is { } slot
            ? presentation.Select(slot) : null;
    }

    public static Rectangle GetGameOasisPlaySpaceBounds(int index)
    {
        var column = index % 2;
        var row = index / 2;
        return new Rectangle(520 + column * 450, 420 + row * 190, 410, 160);
    }

    public static int? GetGameOasisPlaySpaceHit(Point point, int count)
    {
        for (var index = 0; index < Math.Min(count, 4); index++)
            if (GetGameOasisPlaySpaceBounds(index).Contains(point)) return index;
        return null;
    }
    #endregion

    public Button BackButton { get; }

    #region ［右下のボタン］
    public Button UpdateButton { get; }

    public Button SettingsButton { get; }
    #endregion
}
