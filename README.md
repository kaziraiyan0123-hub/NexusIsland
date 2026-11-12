# ✦ NEXUS ISLAND — Windows 11 Desktop Companion & System HUD

> **NEXUS ISLAND** is an advanced, futuristic Windows 11 desktop companion and dynamic system HUD built in C# and .NET 8 WPF. It merges the elegance of Apple's Dynamic Island with the functionality of a Windows Control Center and the aesthetics of a sci-fi HUD.

---

## 🌟 Master Features Architecture

```
                  ✦ NEXUS ISLAND CONTROL CENTER ✦
   [ ⚡ Ctrl ] [ 📊 HUD ] [ 📋 Clips ] [ 📝 Notes ] [ 🧮 Calc ] [ ⏱️ Focus ] [ 🚀 Apps ] [ 🎨 Theme ]
```

### 1. Dynamic Island Morphing Modes
- **Compact Pill**: Sleek floating OLED pill at top-center showing live clock, pulse dot, music track title with reactive mini-equalizer (when audio plays), live battery gauge with charging bolt, and notification alerts.
- **Music Mode**: Active Windows Media Session (GSMTC) integration for Spotify, YouTube, Chrome, and local media players. Displays song title, artist, album art, 6-bar reactive audio equalizer reacting to live system audio peaks, and playback controls (Prev, Play/Pause, Next).
- **Volume & Brightness HUD**: Interactive master volume slider (CoreAudio) with mute toggle, and hardware screen brightness slider via WMI.
- **Battery HUD**: Live percentage, charging animation, line status (AC / Battery), and estimated remaining battery time.
- **Notification Mode**: Pushes animated preview alerts directly into the top island with custom icons, titles, and messages, auto-dismissing after 4.5 seconds.
- **System Activity Telemetry**: Live CPU load % (Ryzen 3 7320U via `GetSystemTimes`), RAM usage (GB used / total via `GlobalMemoryStatusEx`), Network IPv4 download/upload bandwidth (KB/s), and Storage C: % free space.

---

### 2. Quick Controls (Real Windows 11 Integration)
- **📶 Wi-Fi**: Directly launches Windows available networks flyout (`ms-availablenetworks:`).
- **🎧 Bluetooth**: Opens Windows Bluetooth & devices settings (`ms-settings:bluetooth`).
- **🌙 Theme**: Live toggles Windows system and app theme between Dark and Light mode in the registry.
- **💡 Night Light**: Opens Windows display color temperature settings (`ms-settings:nightlight`).
- **📸 Screenshot**: Triggers Windows Snipping Tool screen selection (`ms-screenclip:`).
- **🎥 Screen Record**: Launches Windows Game Bar screen recording (`ms-gamebar:`).
- **🔋 Battery Saver**: Opens Windows Battery Saver power settings (`ms-settings:batterysaver`).
- **🛡️ Lock PC**: Native `LockWorkStation()` lock.

---

### 3. Integrated Mini-Applications Suite
1. **📋 Clipboard Manager**:
   - Live hook into Windows `WM_CLIPBOARDUPDATE` messages via `AddClipboardFormatListener`.
   - Automatically records copied text with timestamps and live search filtering.
   - 1-click copy back to clipboard with a dynamic notification alert: *"Copied! 📋"*.
   - Pin favorites (📌) so they stay permanently pinned and persist in `%APPDATA%\NexusIsland\settings.json`.
   - Clear unpinned clips button.
2. **📝 Quick Notes**:
   - Multi-note workspace with search filter, timestamps, pin status, and "+ New Note" button.
   - Scratchpad editor with instant autosave directly to disk on every keystroke.
3. **🧮 Scientific Calculator**:
   - High-precision recursive-descent expression parser with standard order of operations, parenthesis handling, decimals, and powers (`^`).
   - Functions: `sin`, `cos`, `tan` (degrees), `√`, `log`, `ln`, `x²`, `1/x`, `π`, and `e`.
   - Dual-screen HUD display with expression history and large glowing result.
4. **⏱️ Focus Mode & Pomodoro Timers**:
   - 4 modes: Pomodoro Focus (25m), Short Break (5m), Long Break (15m), and Stopwatch (milliseconds).
   - Custom countdown timer duration presets (5m, 10m, 15m, 30m, 45m, 60m).
   - Digital HUD timer with animated progress bar, session alerts, and daily statistics.
5. **🚀 App Launcher & Universal Search**:
   - Universal Bar: type a URL to open your browser, enter system commands (`wt`, `taskmgr`, `calc`, `cmd`, `explorer`, `settings`), or enter query to perform an instant Google search.
   - Quick launch tiles with hover glow for native Windows tools.

---

### 4. Personalization & Settings
- **9 Built-in Themes**:
  1. **Cyberpunk Cyan** (Neon Cyan `#00F0FF` with glass border)
  2. **Midnight OLED** (Deep pitch black `#0A0B0E` with crisp white accents)
  3. **Apple Minimal** (Subtle matte grey `#E2E8F0` glass)
  4. **Neon Blue** (Electric blue `#3B82F6`)
  5. **Crimson Red** (Aggressive red `#EF4444`)
  6. **Aurora Purple** (Ethereal purple `#A855F7`)
  7. **Matrix Emerald** (Terminal green `#10B981`)
  8. **Amber Gold** (Warm amber gold `#F59E0B`)
  9. **Synthwave Magenta** (Retro magenta `#EC4899`)
- **Custom Hex Accent Generator**: Type any hex code (e.g. `#8B5CF6`) and click **Apply Accent ✦** to generate a bespoke theme.
- **Island Geometry & Appearance**:
  - Live **Island Scale** slider (0.80x to 1.25x).
  - Live **Opacity / Transparency** slider (70% to 100%).
- **System Integration**:
  - **Windows Startup**: One-click toggle to launch automatically on boot via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.
  - **Audio Cues**: Subtle sci-fi sound effects on click, notification, and timer completion.
  - **Always on Top** toggle and screen centering button.

---

## 🚀 How to Launch

1. **Desktop Shortcut**: Double-click the **NEXUS ISLAND** shortcut on your Desktop.
2. **One-Click Launcher**: Double-click `E:\NexusIsland\Run_NexusIsland.bat` or `E:\NexusIsland\Launch.vbs`.
3. **PowerShell / Terminal**:
```powershell
& "E:\NexusIsland\bin\Release\net8.0-windows10.0.19041.0\NexusIsland.exe"
```

### Shortcuts & Controls
- **Global Toggle**: Press `Ctrl + Shift + Space` anywhere in Windows to expand/collapse.
- **Click to Expand**: Click the compact pill to open the Control Center HUD.
- **Click Outside to Collapse**: Automatically collapses back to the minimal pill when you click outside or blur.
- **Drag Position**: Click and hold the island pill to drag it anywhere across your monitor.
- **System Tray**: Right-click the tray icon in the Windows taskbar notification area to toggle or exit.
