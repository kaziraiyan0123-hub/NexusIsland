using System;
using System.IO;
using System.Threading;
using System.Windows;

namespace NexusIsland;

public static class Program
{
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] Program.Main started\n");
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try { File.AppendAllText(@"E:\NexusIsland\crash.log", $"[{DateTime.Now}] AppDomain Unhandled: {e.ExceptionObject}\n"); } catch { }
        };
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            try { File.AppendAllText(@"E:\NexusIsland\crash.log", $"[{DateTime.Now}] Task Unobserved: {e.Exception}\n"); } catch { }
            e.SetObserved();
        };

        const string mutexName = @"Local\NexusIsland_SingleInstance_App";
        _mutex = new Mutex(true, mutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] Mutex already acquired, exiting\n");
            // Already running; avoid multiple competing instances
            return;
        }

        try
        {
            var app = new App();
            app.InitializeComponent();

            var window = new MainWindow();
            File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] Starting app.Run(window)\n");
            app.Run(window);
            File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] app.Run finished normally\n");
        }
        catch (Exception ex)
        {
            try
            {
                File.WriteAllText("E:\\NexusIsland\\main_exception.log", ex.ToString());
            }
            catch { }
            System.Windows.MessageBox.Show(
                $"Nexus Island startup error:\n\n{ex.Message}\n\nDetails saved to main_exception.log",
                "Nexus Island Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            try
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
            catch { }
        }
    }
}
