namespace KifuwarabeGo2026.Gui.Application.Local.Resting.TournamentRule;

using KifuwarabeGo2026.Gui.Application;

using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

[JsonConverter(typeof(TournamentRulesJsonConverter))]
public sealed class TournamentRules
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

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
        Id = Id,
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

public sealed class TournamentRulesJsonConverter : JsonConverter<TournamentRules>
{
    public override TournamentRules Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var rules = new TournamentRules();
        var legacyMinutes = 0;
        var legacySeconds = 0;
        string? mainTime = null;

        foreach (var property in root.EnumerateObject())
        {
            switch (property.Name.ToUpperInvariant())
            {
                case "ID":
                    if (property.Value.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(property.Value.GetString()))
                        rules.Id = property.Value.GetString()!;
                    break;
                case "DISPLAYNAME":
                    rules.DisplayName = property.Value.GetString() ?? "";
                    break;
                case "RULE":
                    if (property.Value.ValueKind == JsonValueKind.String &&
                        Enum.TryParse<GoRuleKind>(property.Value.GetString(), ignoreCase: true, out var rule))
                        rules.Rule = rule;
                    break;
                case "BOARDSIZE":
                    if (property.Value.TryGetInt32(out var boardSize))
                        rules.BoardSize = boardSize;
                    break;
                case "KOMI":
                    if (property.Value.TryGetDecimal(out var komi))
                        rules.Komi = komi;
                    break;
                case "MAINTIME":
                    mainTime = property.Value.GetString();
                    break;
                case "MAINTIMEMINUTES":
                    property.Value.TryGetInt32(out legacyMinutes);
                    break;
                case "MAINTIMESECONDS":
                    property.Value.TryGetInt32(out legacySeconds);
                    break;
                case "MOVELIMIT":
                    if (property.Value.TryGetInt32(out var moveLimit))
                        rules.MoveLimit = moveLimit;
                    break;
            }
        }

        var totalSeconds = TryParseMainTime(mainTime, out var parsedSeconds)
            ? parsedSeconds
            : Math.Max(0, legacyMinutes * 60 + legacySeconds);
        rules.MainTimeMinutes = totalSeconds / 60;
        rules.MainTimeSeconds = totalSeconds % 60;
        return rules;
    }

    public override void Write(Utf8JsonWriter writer, TournamentRules rules, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (!string.IsNullOrWhiteSpace(rules.Id))
            writer.WriteString("Id", rules.Id);
        writer.WriteString("DisplayName", rules.DisplayName);
        writer.WriteString("Rule", rules.Rule.ToString());
        writer.WriteNumber("BoardSize", rules.BoardSize);
        writer.WriteNumber("Komi", rules.Komi);
        writer.WriteString("MainTime", FormatMainTime(rules.MainTime));
        writer.WriteNumber("MoveLimit", rules.MoveLimit);
        writer.WriteEndObject();
    }

    public static bool TryParseMainTime(string? text, out int totalSeconds)
    {
        totalSeconds = 0;
        var parts = text?.Trim().Split(':');
        if (parts is not { Length: 3 } ||
            !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds) ||
            hours is < 0 or > 999 ||
            minutes is < 0 or > 59 ||
            seconds is < 0 or > 59)
        {
            return false;
        }

        totalSeconds = hours * 3600 + minutes * 60 + seconds;
        return true;
    }

    public static string FormatMainTime(TimeSpan time) =>
        $"{Math.Clamp((int)time.TotalHours, 0, 999):00}:{time.Minutes:00}:{time.Seconds:00}";
}

public enum TournamentRulesNumericField
{
    MainTime,
    MoveLimit,
}
