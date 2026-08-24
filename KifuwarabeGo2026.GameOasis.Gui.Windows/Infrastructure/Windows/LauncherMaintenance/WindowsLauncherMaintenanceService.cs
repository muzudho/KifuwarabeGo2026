namespace KifuwarabeGo2026.GameOasis.Gui.Infrastructure.Windows;

using KifuwarabeGo2026.GameOasis.Gui.Application.LauncherMaintenance;

public sealed class WindowsLauncherMaintenanceService : ILauncherMaintenanceService
{
    public bool IsSupported => true;

    public string UnsupportedReason => string.Empty;

    public void ShowInteractiveUpdater()
    {
        using var form = new WindowsLauncherMaintenanceForm(
            new WindowsLauncherPackageInstaller(),
            new WindowsLauncherShortcutStore(),
            new WindowsShellLinkService());
        form.ShowDialog();
    }
}
