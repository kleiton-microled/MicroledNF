using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace Microled.Nfe.DesktopLauncher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogCrash("UnhandledException", args.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogCrash("UnobservedTaskException", args.Exception);
            args.SetObserved();
        };
        base.OnStartup(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogCrash("DispatcherUnhandledException", e.Exception);
        MessageBox.Show(
            "O painel encontrou um erro e vai continuar aberto." + Environment.NewLine + Environment.NewLine + e.Exception.Message,
            "Microled NFe",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void LogCrash(string kind, Exception? exception)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microled",
                "Nfe",
                "launcher");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "crash.log"),
                DateTime.Now.ToString("s") + " [" + kind + "] " + exception + Environment.NewLine);
        }
        catch
        {
            // ignore logging failures
        }
    }
}
