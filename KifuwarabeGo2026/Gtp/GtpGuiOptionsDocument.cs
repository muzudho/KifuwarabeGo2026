namespace KifuwarabeGo2026.Gtp;

using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// `gui_options` 応答のJSON文書です。
/// </summary>
public sealed class GtpGuiOptionsDocument
{
    public int Version { get; set; }

    public List<GtpGuiOptionDefinition> Options { get; set; } = [];

    public static GtpGuiOptionsDocument Parse(string json) =>
        JsonSerializer.Deserialize<GtpGuiOptionsDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        }) ?? throw new FormatException("gui_options returned an empty JSON document.");
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

    public List<string> Vars { get; set; } = [];
}
