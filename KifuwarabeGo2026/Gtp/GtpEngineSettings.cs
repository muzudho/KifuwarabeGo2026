namespace KifuwarabeGo2026.Gtp;

public sealed record GtpEngineSettings(
    string Name,
    string ExecutablePath,
    string WorkingDirectory,
    string Arguments,
    bool EnableGtpLog,
    string LogPrefix = "");
