namespace KifuwarabeGo2026.Gui.Application;

using KifuwarabeGo2026.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;
using System.Text.Json.Serialization;

/// <summary>
/// GTPプロトコル対応の思考エンジンのプロファイル
/// </summary>
public sealed class GtpEngineProfile
{
    public string DisplayName { get; set; } = "Kifuwarabe Star Random GTP";

    /// <summary>CGOS 接続画面へ初期表示するログイン名です。</summary>
    public string DefaultCgosLoginName { get; set; } = "";

    /// <summary>CGOS 接続画面へ初期表示する平文パスワードです。</summary>
    public string DefaultCgosPlainTextPassword { get; set; } = "";

    public string ExecutablePath { get; set; } = "";

    /// <summary>
    /// 作業ディレクトリー
    /// </summary>
    [JsonIgnore]
    public WorkingDirectoryModel WorkingDirectoryModel { get; set; } = WorkingDirectoryModel.Empty;
    [JsonPropertyName("workingDirectory")]
    public string WorkingDirectoryStr
    {
        get => WorkingDirectoryModel.Value;
        set => WorkingDirectoryModel = WorkingDirectoryModel.FromString(value);
    }

    public string Arguments { get; set; } = "";

    public bool EnableGtpLog { get; set; } = true;

    public Dictionary<string, string> GuiOptions { get; set; } = new()
    {
        [GtpEngineGuiOptions.RandomMoveId] = GtpEngineGuiOptions.ChebyshevDistanceFromStarRandomMove,
    };

    [JsonIgnore]
    public string LogPrefix { get; set; } = "";

    public GtpEngineProfile Clone() => new()
    {
        DisplayName = DisplayName,
        DefaultCgosLoginName = DefaultCgosLoginName,
        DefaultCgosPlainTextPassword = DefaultCgosPlainTextPassword,
        ExecutablePath = ExecutablePath,
        WorkingDirectoryModel = WorkingDirectoryModel,
        Arguments = Arguments,
        EnableGtpLog = EnableGtpLog,
        GuiOptions = new Dictionary<string, string>(GuiOptions ?? []),
        LogPrefix = LogPrefix,
    };

    public string GetGuiOption(string id, string fallback) =>
        GuiOptions.TryGetValue(id, out var value) ? value : fallback;
}

public enum GtpEngineProfileEditField
{
    DisplayName,
    DefaultCgosLoginName,
    DefaultCgosPlainTextPassword,
    ExecutablePath,
    WorkingDirectory,
    Arguments,
}
