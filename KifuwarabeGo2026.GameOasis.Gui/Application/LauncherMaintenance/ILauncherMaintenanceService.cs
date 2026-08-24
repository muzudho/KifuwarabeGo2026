namespace KifuwarabeGo2026.GameOasis.Gui.Application.LauncherMaintenance;

using System;

/// <summary>
/// Opens the platform-specific launcher maintenance experience.
/// The portable GUI does not know how an OS represents shortcuts or app entries.
/// </summary>
public interface ILauncherMaintenanceService
{
    bool IsSupported { get; }

    string UnsupportedReason { get; }

    void ShowInteractiveUpdater();
}

public sealed class UnsupportedLauncherMaintenanceService : ILauncherMaintenanceService
{
    public static UnsupportedLauncherMaintenanceService Instance { get; } = new();

    private UnsupportedLauncherMaintenanceService()
    {
    }

    public bool IsSupported => false;

    public string UnsupportedReason => "Launcher maintenance is not available on this platform yet.";

    public void ShowInteractiveUpdater() => throw new PlatformNotSupportedException(UnsupportedReason);
}
