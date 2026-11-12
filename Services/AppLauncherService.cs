using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NexusIsland.Core;

namespace NexusIsland.Services;

public class AppLauncherService
{
    public void LaunchShortcut(AppShortcut shortcut)
    {
        if (string.IsNullOrWhiteSpace(shortcut.Path)) return;
        LaunchTarget(shortcut.Path, shortcut.Arguments);
    }

    public bool LaunchTarget(string target, string arguments = "")
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = target,
                Arguments = arguments,
                UseShellExecute = true
            };
            Process.Start(psi);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to launch '{target}': {ex.Message}");
            return false;
        }
    }

    public void ExecuteSearchOrCommand(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return;
        query = query.Trim();

        // 1. Web URL
        if (query.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            query.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            (query.Contains('.') && !query.Contains(' ') && (query.EndsWith(".com") || query.EndsWith(".org") || query.EndsWith(".net") || query.EndsWith(".io") || query.EndsWith(".dev"))))
        {
            string url = query.StartsWith("http") ? query : $"https://{query}";
            LaunchTarget(url);
            return;
        }

        // 2. Direct URI schemes (ms-settings:, spotify:, etc.)
        if (query.Contains(':') && !query.Contains(' ') && !Path.IsPathRooted(query))
        {
            LaunchTarget(query);
            return;
        }

        // 3. Known quick system commands
        string lower = query.ToLowerInvariant();
        switch (lower)
        {
            case "cmd":
            case "terminal":
            case "wt":
                LaunchTarget("wt.exe");
                return;
            case "powershell":
            case "ps":
                LaunchTarget("powershell.exe");
                return;
            case "calc":
            case "calculator":
                LaunchTarget("calc.exe");
                return;
            case "notepad":
            case "notes":
                LaunchTarget("notepad.exe");
                return;
            case "taskmgr":
            case "task manager":
                LaunchTarget("taskmgr.exe");
                return;
            case "explorer":
            case "files":
                LaunchTarget("explorer.exe");
                return;
            case "settings":
                LaunchTarget("ms-settings:");
                return;
        }

        // 4. Default: Web search query in default browser
        string encoded = Uri.EscapeDataString(query);
        LaunchTarget($"https://www.google.com/search?q={encoded}");
    }
}
