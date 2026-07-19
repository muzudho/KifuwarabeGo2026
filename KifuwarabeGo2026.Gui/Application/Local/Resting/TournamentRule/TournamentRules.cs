namespace KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.Gui.Application;

using System;
using System.Text.Json.Serialization;

public sealed class TournamentRules
{
    public string DisplayName { get; set; } = "Default 19-ro";

    public GoRuleKind Rule { get; set; } = GoRuleKind.PureGo;

    public int BoardSize { get; set; } = 19;

    public decimal Komi { get; set; } = 6.5m;

    public int MainTimeMinutes { get; set; } = 0;

    public int MainTimeSeconds { get; set; } = 0;

    public int MoveLimit { get; set; } = 400;

    [JsonIgnore]
    public string FilePath { get; set; } = "";

    [JsonIgnore]
    public TimeSpan MainTime => TimeSpan.FromSeconds(Math.Max(0, MainTimeMinutes * 60 + MainTimeSeconds));

    public TournamentRules Clone() => new()
    {
        DisplayName = DisplayName,
        Rule = Rule,
        BoardSize = BoardSize,
        Komi = Komi,
        MainTimeMinutes = MainTimeMinutes,
        MainTimeSeconds = MainTimeSeconds,
        MoveLimit = MoveLimit,
        FilePath = FilePath,
    };
}
