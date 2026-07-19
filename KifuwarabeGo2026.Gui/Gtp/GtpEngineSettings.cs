namespace KifuwarabeGo2026.Gtp;

using KifuwarabeGo2026.Gui.Domain;
using System.Collections.Generic;

public sealed record GtpEngineSettings(
    string Name,
    string ExecutablePath,
    WorkingDirectoryModel WorkingDirectory,
    string Arguments,
    bool EnableGtpLog,
    string LogPrefix = "",
    IReadOnlyDictionary<string, string>? GuiOptions = null);
