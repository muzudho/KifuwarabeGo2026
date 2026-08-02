namespace KifuwarabeGo2026.Gui.Gtp;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// `kfw-options` 応答のJSON文書です。
/// </summary>
public sealed class GtpGuiOptionsDocument
{
    public int Version { get; set; }

    public List<GtpGuiOptionDefinition> Options { get; set; } = [];

    public static GtpGuiOptionsDocument Parse(string json) =>
        JsonSerializer.Deserialize<GtpGuiOptionsDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new FormatException("kfw-options returned an empty JSON document.");
}

/// <summary>
/// GUIが描画・検証するGTPエンジンオプションの定義です。
/// </summary>
public sealed class GtpGuiOptionDefinition
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public string Type { get; set; } = "";

    public string Default { get; set; } = "";

    public string Value { get; set; } = "";

    public int? Min { get; set; }

    public int? Max { get; set; }

    public List<string> Vars { get; set; } = [];
}

/// <summary>
/// `kfw-describe-options` 応答のJSON文書です。
/// </summary>
public sealed class GtpOptionSchemaDocument
{
    public int Version { get; set; }

    public string App { get; set; } = "";

    public string Role { get; set; } = "";

    public List<GtpOptionSchemaDefinition> Options { get; set; } = [];

    public static GtpOptionSchemaDocument Parse(string json) =>
        JsonSerializer.Deserialize<GtpOptionSchemaDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new FormatException("kfw-describe-options returned an empty JSON document.");
}

public sealed class GtpOptionSchemaDefinition
{
    public string Id { get; set; } = "";

    public string Label { get; set; } = "";

    public string Type { get; set; } = "";

    public JsonElement Default { get; set; }

    public int? Minimum { get; set; }

    public int? Maximum { get; set; }

    public int? MaximumLength { get; set; }

    public List<string> Values { get; set; } = [];

    public string Apply { get; set; } = "";
}
