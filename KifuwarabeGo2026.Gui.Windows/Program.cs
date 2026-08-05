namespace KifuwarabeGo2026.Gui;

using KifuwarabeGo2026.Gui.Infrastructure.Logging;
using KifuwarabeGo2026.Gui.Infrastructure.Windows;
using System;

internal static class Program
{
    [System.STAThread]
    private static void Main()
    {
        var startedAt = DateTimeOffset.Now;
        GuiOperationLog.Initialize(startedAt);
        ApplicationErrorLog.Initialize(startedAt);
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            ApplicationErrorLog.Write("UNHANDLED EXCEPTION", "An unhandled application error occurred.", args.ExceptionObject as Exception);
        System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (_, args) =>
            ApplicationErrorLog.Write("UNOBSERVED TASK EXCEPTION", "An unobserved task error occurred.", args.Exception);

        try
        {
            GuiOperationLog.App("Application session started");
            var platformExecutableService = new WindowsPlatformExecutableService();
            using var game = new Game1(
                new WindowsClipboardService(),
                new WindowsMessageDialogService(),
                new WindowsFileDialogService(),
                new WindowsTextInputDialogService(),
                new WindowsDesktopLauncher(),
                new WindowsTextRasterizer(),
                new WindowsWindowIconService(),
                platformExecutableService,
                new WindowsWindowScreenshotService());
            game.Run();
        }
        catch (Exception ex)
        {
            ApplicationErrorLog.Write("FATAL ERROR", "The application terminated because of an error.", ex);
            throw;
        }
        finally
        {
            GuiOperationLog.Close();
        }
    }
}
