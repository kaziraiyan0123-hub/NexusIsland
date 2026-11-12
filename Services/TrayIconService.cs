using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    public event Action? ToggleRequested;
    public event Action? ExitRequested;

    public void Initialize()
    {
        Icon appIcon = SystemIcons.Application;
        try
        {
            string icoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app_logo.ico");
            if (File.Exists(icoPath))
            {
                appIcon = new Icon(icoPath);
            }
        }
        catch { }

        _notifyIcon = new NotifyIcon
        {
            Text = "NEXUS ISLAND — Apple Dynamic Island HUD",
            Visible = true,
            Icon = appIcon
        };

        var menu = new ContextMenuStrip();

        var itemTitle = new ToolStripMenuItem("✦ NEXUS ISLAND");
        itemTitle.Font = new Font(itemTitle.Font, System.Drawing.FontStyle.Bold);
        itemTitle.Click += (s, e) => ToggleRequested?.Invoke();
        menu.Items.Add(itemTitle);

        menu.Items.Add(new ToolStripSeparator());

        var itemToggle = new ToolStripMenuItem("Toggle Island (Ctrl+Shift+Space)");
        itemToggle.Click += (s, e) => ToggleRequested?.Invoke();
        menu.Items.Add(itemToggle);

        menu.Items.Add(new ToolStripSeparator());

        var itemExit = new ToolStripMenuItem("Exit Nexus Island");
        itemExit.Click += (s, e) => ExitRequested?.Invoke();
        menu.Items.Add(itemExit);

        _notifyIcon.ContextMenuStrip = menu;
        _notifyIcon.DoubleClick += (s, e) => ToggleRequested?.Invoke();
    }

    public void ShowBalloon(string title, string text)
    {
        try
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }
    }
}
