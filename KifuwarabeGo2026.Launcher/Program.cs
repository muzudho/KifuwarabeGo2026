namespace KifuwarabeGo2026.Launcher;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        using var singleInstance = new Mutex(initiallyOwned: true, "Local\\KifuwarabeGo2026.Launcher", out var createdNew);
        if (!createdNew)
        {
            MessageBox.Show("KifuwarabeGo2026 Launcher は既に起動しています。", "KifuwarabeGo2026 Launcher");
            return;
        }
        Application.Run(new LauncherForm());
    }
}
