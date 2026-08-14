namespace KifuwarabeGo2026.Gui.Presentation.Pages.EditTournamentRule;

using KifuwarabeGo2026.Gui.Application;
using KifuwarabeGo2026.Gui.Presentation.StationeryUI.Controls.Button;
using Microsoft.Xna.Framework;

/// <summary>大会ルール設定画面の操作ボタンと、その選択・有効状態を所有します。</summary>
public sealed class EditTournamentRulePage
{
    // ========================================
    // 生成
    // ========================================

    public static EditTournamentRulePage Default { get; } = new();

    private EditTournamentRulePage()
    {
        DiscardButton = new Button(new Rectangle(1144, 156, 132, 48), "DISCARD", 0.30f);
        SaveButton = new Button(new Rectangle(1288, 156, 162, 48), "CLOSE", 0.34f);
        JapaneseRuleButton = new Button(new Rectangle(758, 319, 164, 50), "JAPANESE", 0.44f);
        PureGoRuleButton = new Button(new Rectangle(938, 319, 164, 50), "PURE GO", 0.44f);
        ChineseRuleButton = new Button(new Rectangle(1118, 319, 164, 50), "CHINESE", 0.44f);
        BoardSize9Button = new Button(new Rectangle(758, 391, 164, 50), "9 x 9", 0.56f);
        BoardSize13Button = new Button(new Rectangle(938, 391, 164, 50), "13 x 13", 0.56f);
        BoardSize19Button = new Button(new Rectangle(1118, 391, 164, 50), "19 x 19", 0.56f);
    }

    public Button DiscardButton { get; }
    public Button SaveButton { get; }
    public Button JapaneseRuleButton { get; }
    public Button PureGoRuleButton { get; }
    public Button ChineseRuleButton { get; }
    public Button BoardSize9Button { get; }
    public Button BoardSize13Button { get; }
    public Button BoardSize19Button { get; }

    public void UpdateState(bool isDirty, GoRuleKind ruleKind, int boardSize, GoAppModeKind modeKind)
    {
        DiscardButton.IsEnabled = isDirty;
        SaveButton.Label = isDirty ? "SAVE & CLOSE" : "CLOSE";
        SaveButton.LabelScale = isDirty ? 0.27f : 0.34f;
        JapaneseRuleButton.IsSelected = ruleKind == GoRuleKind.Japanese;
        PureGoRuleButton.IsSelected = ruleKind == GoRuleKind.PureGo;
        ChineseRuleButton.IsSelected = ruleKind == GoRuleKind.Chinese;
        var canChangeBoardSize = modeKind != GoAppModeKind.GameOver;
        BoardSize9Button.IsEnabled = canChangeBoardSize;
        BoardSize13Button.IsEnabled = canChangeBoardSize;
        BoardSize19Button.IsEnabled = canChangeBoardSize;
        BoardSize9Button.IsSelected = boardSize == 9;
        BoardSize13Button.IsSelected = boardSize == 13;
        BoardSize19Button.IsSelected = boardSize == 19;
    }

    public GoRuleKind? GetRuleKindHit(Point point) =>
        JapaneseRuleButton.IsHit(point) ? GoRuleKind.Japanese :
        PureGoRuleButton.IsHit(point) ? GoRuleKind.PureGo :
        ChineseRuleButton.IsHit(point) ? GoRuleKind.Chinese : null;

    public int? GetBoardSizeHit(Point point) =>
        BoardSize9Button.IsHit(point) ? 9 :
        BoardSize13Button.IsHit(point) ? 13 :
        BoardSize19Button.IsHit(point) ? 19 : null;
}
