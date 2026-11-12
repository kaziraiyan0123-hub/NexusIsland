using System.IO;
using System.Windows;

namespace NexusIsland;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            try
            {
                File.AppendAllText(@"E:\NexusIsland\crash.log", $"[{System.DateTime.Now}] Dispatcher Unhandled: {e.Exception}\n");
            }
            catch { }
            e.Handled = true;
        };
    }
}
