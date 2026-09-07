namespace KifuwarabeGo2026.GameOasis.Gui.Application.InstallerMaintenance;

using System;

/// <summary>
/// Opens the platform-specific launcher maintenance experience.
/// The portable GUI does not know how an OS represents shortcuts or app entries.
/// </summary>
public interface IInstallerMaintenanceService
{
    bool IsSupported { get; }

    string UnsupportedReason { get; }

    void ShowInteractiveUpdater();
}

public sealed class UnsupportedInstallerMaintenanceService : IInstallerMaintenanceService
{
    public static UnsupportedInstallerMaintenanceService Instance { get; } = new();

    private UnsupportedInstallerMaintenanceService()
    {
    }

    public bool IsSupported => false;

    public string UnsupportedReason => "Installer maintenance is not available on this platform yet.";

    public void ShowInteractiveUpdater() => throw new PlatformNotSupportedException(UnsupportedReason);
}
