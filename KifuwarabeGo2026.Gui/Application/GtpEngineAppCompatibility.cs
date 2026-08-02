namespace KifuwarabeGo2026.Gui.Application;

public enum GtpEngineAppCompatibilityKind
{
    Supported,
    LegacyPlay,
    Unsupported,
    CheckFailed,
}

public sealed record GtpEngineAppCompatibility(
    GtpEngineAppCompatibilityKind Kind,
    string Message)
{
    public bool CanSelect => Kind is GtpEngineAppCompatibilityKind.Supported or GtpEngineAppCompatibilityKind.LegacyPlay;
}

public enum GtpEngineSelectionPurpose
{
    LocalPlayer,
    CgosPlayer,
    AppProvider,
}
