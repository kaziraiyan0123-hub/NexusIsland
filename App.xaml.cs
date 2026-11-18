using System.IO;
using System.Windows;

namespace NexusIsland;

public partial class App : System.Windows.Application
{
    static App()
    {
        // Lock smooth 60 FPS for all WPF fluid animations without jitter or frame drops
        System.Windows.Media.Animation.Timeline.DesiredFrameRateProperty.OverrideMetadata(
            typeof(System.Windows.Media.Animation.Timeline),
            new FrameworkPropertyMetadata(60));
    }

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
