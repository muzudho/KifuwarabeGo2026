namespace KifuwarabeGo2026.Gui;

using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using System;

internal static class Program
{
    [System.STAThread]
    private static void Main()
    {
        ApplicationErrorLog.Initialize(DateTimeOffset.Now);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ApplicationErrorLog.Write("UNHANDLED EXCEPTION", "An unhandled application error occurred.", args.ExceptionObject as Exception);
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
            ApplicationErrorLog.Write("UNOBSERVED TASK EXCEPTION", "An unobserved task error occurred.", args.Exception);

        try
        {
            using var game = new Game1();
            game.Run();
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("FATAL ERROR", "The application terminated because of an error.", ex);
            throw;
        }
    }
}
