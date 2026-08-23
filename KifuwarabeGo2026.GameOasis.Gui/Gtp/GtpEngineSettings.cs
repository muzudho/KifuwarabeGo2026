namespace KifuwarabeGo2026.GameOasis.Gui.Gtp;

using KifuwarabeGo2026.GameOasis.Gui.Domain;
using KifuwarabeGo2026.Shared.Domain;
using System.Collections.Generic;

public sealed record GtpEngineSettings(
    string Name,
    string ExecutablePath,
    WorkingDirectoryModel WorkingDirectory,
    string Arguments,
    bool EnableGtpLog,
    string LogPrefix = "",
    IReadOnlyDictionary<string, string>? GuiOptions = null,
    string AppId = "play",
    string Role = "player");
