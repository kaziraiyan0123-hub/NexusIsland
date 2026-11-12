using System;
using System.Collections.Generic;

namespace NexusIsland.Core;

public class AppShortcut
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string IconGlyph { get; set; } = "⚡";
    public string Arguments { get; set; } = string.Empty;
}

public class QuickNote
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = "New Note";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsPinned { get; set; }
}

public class ClipboardEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Text { get; set; } = string.Empty;
    public DateTime CopiedAt { get; set; } = DateTime.Now;
    public bool IsPinned { get; set; }
}

public class AppSettings
{
    public ThemePreset Theme { get; set; } = ThemePreset.CyberpunkCyan;
    public double IslandScale { get; set; } = 1.0;
    public double IslandOpacity { get; set; } = 0.98;
    public string CustomAccentHex { get; set; } = "#00F0FF";
    public int AnimationSpeedMs { get; set; } = 260;
    public double TopOffset { get; set; } = 12.0;
    public double? WindowLeft { get; set; }
    public double? WindowTop { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool AutoCollapse { get; set; } = true;
    public int AutoCollapseSeconds { get; set; } = 3;
    public bool LaunchOnStartup { get; set; } = false;
    public string HotkeyModifier { get; set; } = "Win";
    public string HotkeyKey { get; set; } = "Space";
    public string WeatherCity { get; set; } = "Auto";
    public double WeatherLatitude { get; set; } = 0.0;
    public double WeatherLongitude { get; set; } = 0.0;
    public bool SoundEffectsEnabled { get; set; } = false;
    public bool AmbientGlowEnabled { get; set; } = true;

    // Mini Apps Data
    public List<QuickNote> Notes { get; set; } = new();
    public List<AppShortcut> FavoriteApps { get; set; } = new();
    public List<ClipboardEntry> PinnedClipboard { get; set; } = new();

    public AppSettings()
    {
        FavoriteApps.Add(new AppShortcut { Name = "Terminal", Path = "wt.exe", IconGlyph = "💻" });
        FavoriteApps.Add(new AppShortcut { Name = "Task Manager", Path = "taskmgr.exe", IconGlyph = "📊" });
        FavoriteApps.Add(new AppShortcut { Name = "Explorer", Path = "explorer.exe", IconGlyph = "📁" });
        FavoriteApps.Add(new AppShortcut { Name = "Notepad", Path = "notepad.exe", IconGlyph = "📝" });
        FavoriteApps.Add(new AppShortcut { Name = "Calculator", Path = "calc.exe", IconGlyph = "🔢" });
        FavoriteApps.Add(new AppShortcut { Name = "Settings", Path = "ms-settings:", IconGlyph = "⚙️" });
    }
}
