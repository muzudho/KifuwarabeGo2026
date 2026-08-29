namespace KifuwarabeGo2026.FormalAdapter.Gtp.Client;

using System.Collections.Generic;

public sealed record GtpEngineSettings(
    string Name,
    string ExecutablePath,
    string WorkingDirectory,
    string Arguments,
    bool EnableGtpLog,
    string LogPrefix = "",
    IReadOnlyDictionary<string, string>? GuiOptions = null,
    string AppId = "play",
    string Role = "player");
