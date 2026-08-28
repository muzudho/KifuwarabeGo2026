namespace KifuwarabeGo2026.GameOasis.Gui.Application.Local.Resting.TournamentRule;

using System.Collections.Generic;

/// <summary>大会ルール画面が使用する既存設定形式の保存境界です。</summary>
public interface ITournamentRulesCatalog
{
    string ListPath { get; }
    IReadOnlyList<TournamentRules> Rules { get; }
    void Save(TournamentRules rules);
    void Delete(TournamentRules rules);
    void SaveOrder(IEnumerable<TournamentRules> rules);
    TournamentRules CreateNew(TournamentRules source);
    TournamentRules Duplicate(TournamentRules source);
}
