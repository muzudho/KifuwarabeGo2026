namespace KifuwarabeGo2026.Application;

using System.Text.Json.Serialization;

/// <summary>
/// GTPプロトコル対応の思考エンジンのプロファイル
/// </summary>
public sealed class GtpEngineProfile
{
    public string DisplayName { get; set; } = "Kifuwarabe Random GTP";

    public string ExecutablePath { get; set; } = "";

    public string WorkingDirectory { get; set; } = "";

    public string Arguments { get; set; } = "";

    public bool EnableGtpLog { get; set; } = true;

    [JsonIgnore]
    public string LogPrefix { get; set; } = "";

    public GtpEngineProfile Clone() => new()
    {
        DisplayName = DisplayName,
        ExecutablePath = ExecutablePath,
        WorkingDirectory = WorkingDirectory,
        Arguments = Arguments,
        EnableGtpLog = EnableGtpLog,
        LogPrefix = LogPrefix,
    };
}

public enum GtpEngineProfileEditField
{
    DisplayName,
    ExecutablePath,
    WorkingDirectory,
    Arguments,
}
