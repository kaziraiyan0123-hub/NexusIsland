namespace NexusIsland.Core;

public enum IslandMode
{
    Compact,            // Minimal pill: time, battery, mini status
    Music,              // Album art, media controls, audio wave
    Battery,            // Battery gauge & charging telemetry
    Volume,             // Volume slider HUD (auto dismiss)
    Brightness,         // Brightness slider HUD (auto dismiss)
    Notification,       // Live notification preview
    Weather,            // Weather details
    Focus,              // Pomodoro / Study timer
    SystemActivity,     // Live CPU, RAM, Network, Disk
    ControlCenter       // Full expanded control center & mini-apps
}

public enum MiniAppTab
{
    QuickControls,
    SystemActivity,
    Clipboard,
    Notes,
    Calculator,
    Calendar,
    AppLauncher,
    Focus,
    Settings
}

public enum ThemePreset
{
    MidnightOLED,
    CyberpunkCyan,
    AppleMinimal,
    NeonBlue,
    Crimson,
    Aurora,
    MatrixEmerald,
    AmberGold,
    SynthwaveMagenta,
    Custom
}
