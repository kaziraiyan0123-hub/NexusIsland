using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Brush = System.Windows.Media.Brush;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;
using Color = System.Windows.Media.Color;
using NexusIsland.Core;
using NexusIsland.Services;
using NexusIsland.Themes;

namespace NexusIsland.ViewModels;

public enum IslandExpandedMode
{
    MusicPlayer,
    NotchNook,
    ControlCenter
}

public class CalendarDayItem : INotifyPropertyChanged
{
    public string DayName { get; init; } = "";
    public int DayNumber { get; init; }
    public bool IsToday { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public RelayCommand(Action execute) : this(_ => execute()) { }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class MainViewModel : INotifyPropertyChanged
{
    // Services
    public AudioService AudioService { get; } = new();
    public BrightnessService BrightnessService { get; } = new();
    public BatteryService BatteryService { get; } = new();
    public MediaService MediaService { get; } = new();
    public SystemMonitorService SystemMonitor { get; } = new();
    public QuickControlsService QuickControls { get; } = new();
    public ClipboardService ClipboardService { get; } = new();
    public CalculatorService CalculatorService { get; } = new();
    public FocusService FocusService { get; } = new();
    public AppLauncherService AppLauncherService { get; } = new();
    public SoundService SoundService { get; } = new();

    public AppSettings Settings { get; private set; }

    private IslandTheme _currentTheme;
    public IslandTheme CurrentTheme
    {
        get => _currentTheme;
        private set
        {
            if (_currentTheme != value)
            {
                _currentTheme = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ActiveThemeName));
                OnPropertyChanged(nameof(IsMidnightActive));
                OnPropertyChanged(nameof(IsCyberpunkActive));
                OnPropertyChanged(nameof(IsAppleActive));
                OnPropertyChanged(nameof(IsNeonBlueActive));
                OnPropertyChanged(nameof(IsCrimsonActive));
                OnPropertyChanged(nameof(IsAuroraActive));
                OnPropertyChanged(nameof(IsMatrixActive));
                OnPropertyChanged(nameof(IsAmberActive));
                OnPropertyChanged(nameof(IsSynthwaveActive));
                OnPropertyChanged(nameof(IsCustomActive));
                OnPropertyChanged(nameof(BatteryBrush));
            }
        }
    }

    public string ActiveThemeName => CurrentTheme.Name;
    public bool IsMidnightActive => Settings.Theme == ThemePreset.MidnightOLED;
    public bool IsCyberpunkActive => Settings.Theme == ThemePreset.CyberpunkCyan;
    public bool IsAppleActive => Settings.Theme == ThemePreset.AppleMinimal;
    public bool IsNeonBlueActive => Settings.Theme == ThemePreset.NeonBlue;
    public bool IsCrimsonActive => Settings.Theme == ThemePreset.Crimson;
    public bool IsAuroraActive => Settings.Theme == ThemePreset.Aurora;
    public bool IsMatrixActive => Settings.Theme == ThemePreset.MatrixEmerald;
    public bool IsAmberActive => Settings.Theme == ThemePreset.AmberGold;
    public bool IsSynthwaveActive => Settings.Theme == ThemePreset.SynthwaveMagenta;
    public bool IsCustomActive => Settings.Theme == ThemePreset.Custom;

    // Phase 5: Personalization & Startup Properties
    private bool _launchOnStartup;
    public bool LaunchOnStartup
    {
        get => _launchOnStartup;
        set
        {
            if (_launchOnStartup != value)
            {
                _launchOnStartup = value;
                Settings.LaunchOnStartup = value;
                StartupService.SetLaunchOnStartup(value);
                SettingsManager.Save(Settings);
                OnPropertyChanged();
                OnPropertyChanged(nameof(StartupStatusText));
                ShowNotification("Windows Startup", value ? "Nexus Island will start with Windows" : "Removed from Windows startup", "⚙️");
            }
        }
    }
    public string StartupStatusText => LaunchOnStartup ? "Startup: ON" : "Startup: OFF";

    private bool _soundEffectsEnabled = false;
    public bool SoundEffectsEnabled
    {
        get => _soundEffectsEnabled;
        set
        {
            if (_soundEffectsEnabled != value)
            {
                _soundEffectsEnabled = value;
                Settings.SoundEffectsEnabled = value;
                SoundService.Enabled = value;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
                OnPropertyChanged(nameof(SoundStatusText));
                if (value) SoundService.PlayClick();
            }
        }
    }
    public string SoundStatusText => SoundEffectsEnabled ? "Audio: ON" : "Audio: OFF";

    private bool _ambientGlowEnabled = true;
    public bool AmbientGlowEnabled
    {
        get => _ambientGlowEnabled;
        set
        {
            if (_ambientGlowEnabled != value)
            {
                _ambientGlowEnabled = value;
                Settings.AmbientGlowEnabled = value;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
                OnPropertyChanged(nameof(AmbientGlowVisibility));
                OnPropertyChanged(nameof(AmbientGlowStatusText));
            }
        }
    }
    public Visibility AmbientGlowVisibility => AmbientGlowEnabled ? Visibility.Visible : Visibility.Collapsed;
    public string AmbientGlowStatusText => AmbientGlowEnabled ? "Ambient Glow: ON" : "Ambient Glow: OFF";

    private double _islandScale = 1.0;
    public double IslandScale
    {
        get => _islandScale;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 0.8, 1.25);
            if (Math.Abs(_islandScale - clamped) > 0.001)
            {
                _islandScale = clamped;
                Settings.IslandScale = clamped;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
            }
        }
    }

    private double _islandOpacity = 0.98;
    public double IslandOpacity
    {
        get => _islandOpacity;
        set
        {
            double clamped = Math.Clamp(Math.Round(value, 2), 0.70, 1.0);
            if (Math.Abs(_islandOpacity - clamped) > 0.001)
            {
                _islandOpacity = clamped;
                Settings.IslandOpacity = clamped;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
            }
        }
    }

    private string _customAccentHex = "#00F0FF";
    public string CustomAccentHex
    {
        get => _customAccentHex;
        set
        {
            if (_customAccentHex != value)
            {
                _customAccentHex = value;
                Settings.CustomAccentHex = value;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
            }
        }
    }

    public ICommand ToggleLaunchOnStartupCommand { get; private set; } = null!;
    public ICommand ToggleSoundEffectsCommand { get; private set; } = null!;
    public ICommand ToggleAmbientGlowCommand { get; private set; } = null!;
    public ICommand ApplyCustomAccentCommand { get; private set; } = null!;

    // Island State
    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded != value)
            {
                _isExpanded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCompact));
                OnPropertyChanged(nameof(IsSatelliteVisible));
            }
        }
    }
    public bool IsCompact => !IsExpanded;

    // Apple-Style Detached Satellite Bubble Multitasking State
    private bool _testSatelliteActive;
    public bool TestSatelliteActive
    {
        get => _testSatelliteActive;
        set
        {
            if (_testSatelliteActive != value)
            {
                _testSatelliteActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSatelliteVisible));
                OnPropertyChanged(nameof(TestSatelliteStatusText));
            }
        }
    }
    public string TestSatelliteStatusText => TestSatelliteActive ? "Satellite: ON" : "Satellite: OFF";

    public bool IsSatelliteVisible => IsCompact && (FocusIsRunning || (IsMediaPlaying && HasActiveNotification) || TestSatelliteActive);

    public string SatelliteIcon
    {
        get
        {
            if (FocusIsRunning)
            {
                return FocusService.CurrentType switch
                {
                    FocusType.PomodoroFocus => "🎯",
                    FocusType.PomodoroShortBreak => "☕",
                    FocusType.PomodoroLongBreak => "🛋️",
                    _ => "⏱️"
                };
            }
            if (IsMediaPlaying) return "🎵";
            return "✦";
        }
    }

    public string SatelliteText
    {
        get
        {
            if (FocusIsRunning)
            {
                var text = FocusService.DisplayText;
                if (text.Contains(':'))
                {
                    var parts = text.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int mins))
                    {
                        if (mins > 0) return $"{mins}m";
                        return $"{parts[1]}s";
                    }
                }
                return text;
            }
            if (IsMediaPlaying) return "PLAY";
            return "HUD";
        }
    }

    public double SatelliteProgressPercent => FocusIsRunning ? FocusProgressPercent : 100.0;

    public ICommand OpenSatelliteTabCommand { get; private set; } = null!;
    public ICommand ToggleTestSatelliteCommand { get; private set; } = null!;

    // Apple-Style HUD Mode (Image 3 Music Player, Image 2 Notch Nook, Control Center)
    private IslandExpandedMode _expandedMode = IslandExpandedMode.MusicPlayer;
    public IslandExpandedMode ExpandedMode
    {
        get => _expandedMode;
        set
        {
            if (_expandedMode != value)
            {
                _expandedMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMusicPlayerMode));
                OnPropertyChanged(nameof(IsNotchNookMode));
                OnPropertyChanged(nameof(IsControlCenterMode));
                RequestModeResize?.Invoke(value);
            }
        }
    }
    public bool IsMusicPlayerMode => ExpandedMode == IslandExpandedMode.MusicPlayer;
    public bool IsNotchNookMode => ExpandedMode == IslandExpandedMode.NotchNook;
    public bool IsControlCenterMode => ExpandedMode == IslandExpandedMode.ControlCenter;

    public event Action<IslandExpandedMode>? RequestModeResize;
    public ICommand SelectExpandedModeCommand { get; private set; } = null!;

    // Phase 3 & 4: Control Center & Mini-App Tab Navigation
    private MiniAppTab _currentTab = MiniAppTab.QuickControls;
    public MiniAppTab CurrentTab
    {
        get => _currentTab;
        set
        {
            if (_currentTab != value)
            {
                _currentTab = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsQuickControlsTab));
                OnPropertyChanged(nameof(IsSystemActivityTab));
                OnPropertyChanged(nameof(IsClipboardTab));
                OnPropertyChanged(nameof(IsNotesTab));
                OnPropertyChanged(nameof(IsCalculatorTab));
                OnPropertyChanged(nameof(IsFocusTab));
                OnPropertyChanged(nameof(IsAppLauncherTab));
                OnPropertyChanged(nameof(IsSettingsTab));
            }
        }
    }
    public bool IsQuickControlsTab => CurrentTab == MiniAppTab.QuickControls;
    public bool IsSystemActivityTab => CurrentTab == MiniAppTab.SystemActivity;
    public bool IsClipboardTab => CurrentTab == MiniAppTab.Clipboard;
    public bool IsNotesTab => CurrentTab == MiniAppTab.Notes;
    public bool IsCalculatorTab => CurrentTab == MiniAppTab.Calculator;
    public bool IsFocusTab => CurrentTab == MiniAppTab.Focus;
    public bool IsAppLauncherTab => CurrentTab == MiniAppTab.AppLauncher;
    public bool IsSettingsTab => CurrentTab == MiniAppTab.Settings;

    // Phase 4: Clipboard Manager State
    private string _clipboardSearchQuery = string.Empty;
    public string ClipboardSearchQuery
    {
        get => _clipboardSearchQuery;
        set
        {
            if (_clipboardSearchQuery != value)
            {
                _clipboardSearchQuery = value;
                OnPropertyChanged();
                ClipboardView?.Refresh();
            }
        }
    }
    public ICollectionView ClipboardView { get; private set; } = null!;
    public ICommand CopyClipboardItemCommand { get; private set; } = null!;
    public ICommand TogglePinClipboardCommand { get; private set; } = null!;
    public ICommand DeleteClipboardItemCommand { get; private set; } = null!;
    public ICommand ClearClipboardHistoryCommand { get; private set; } = null!;

    // Phase 4: Quick Notes State
    public ObservableCollection<QuickNote> Notes { get; } = new();
    private QuickNote? _selectedNote;
    public QuickNote? SelectedNote
    {
        get => _selectedNote;
        set
        {
            if (_selectedNote != value)
            {
                _selectedNote = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasSelectedNote));
                OnPropertyChanged(nameof(SelectedNoteTitle));
                OnPropertyChanged(nameof(SelectedNoteContent));
            }
        }
    }
    public bool HasSelectedNote => SelectedNote != null;

    public string SelectedNoteTitle
    {
        get => SelectedNote?.Title ?? string.Empty;
        set
        {
            if (SelectedNote != null && SelectedNote.Title != value)
            {
                SelectedNote.Title = value;
                OnPropertyChanged();
                NotifyNoteEdited();
            }
        }
    }

    public string SelectedNoteContent
    {
        get => SelectedNote?.Content ?? string.Empty;
        set
        {
            if (SelectedNote != null && SelectedNote.Content != value)
            {
                SelectedNote.Content = value;
                OnPropertyChanged();
                NotifyNoteEdited();
            }
        }
    }

    private string _notesSearchQuery = string.Empty;
    public string NotesSearchQuery
    {
        get => _notesSearchQuery;
        set
        {
            if (_notesSearchQuery != value)
            {
                _notesSearchQuery = value;
                OnPropertyChanged();
                NotesView?.Refresh();
            }
        }
    }
    public ICollectionView NotesView { get; private set; } = null!;
    public ICommand NewNoteCommand { get; private set; } = null!;
    public ICommand DeleteNoteCommand { get; private set; } = null!;
    public ICommand TogglePinNoteCommand { get; private set; } = null!;
    public ICommand SaveNotesCommand { get; private set; } = null!;

    // Phase 4: Calculator State
    private string _calcExpression = string.Empty;
    public string CalcExpression
    {
        get => _calcExpression;
        set { _calcExpression = value; OnPropertyChanged(); }
    }

    private string _calcResult = "0";
    public string CalcResult
    {
        get => _calcResult;
        set { _calcResult = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> CalcHistory { get; } = new();
    public ICommand CalcInputCharCommand { get; private set; } = null!;
    public ICommand CalcClearCommand { get; private set; } = null!;
    public ICommand CalcBackspaceCommand { get; private set; } = null!;
    public ICommand CalcEqualsCommand { get; private set; } = null!;
    public ICommand CalcFunctionCommand { get; private set; } = null!;

    // Phase 4: Focus Mode & Timers State
    public string FocusDisplayText => FocusService.DisplayText;
    public double FocusProgressPercent => FocusService.ProgressFraction * 100.0;
    public bool FocusIsRunning => FocusService.IsRunning;
    public string FocusButtonGlyph => FocusIsRunning ? "⏸" : "▶";
    public string FocusButtonText => FocusIsRunning ? "Pause" : "Start";
    public string FocusModeTitle => FocusService.CurrentType switch
    {
        FocusType.PomodoroFocus => "🎯 FOCUS SESSION",
        FocusType.PomodoroShortBreak => "☕ SHORT BREAK",
        FocusType.PomodoroLongBreak => "🛋️ LONG BREAK",
        FocusType.CustomTimer => "⏱️ COUNTDOWN TIMER",
        FocusType.Stopwatch => "⏱️ STOPWATCH",
        _ => "FOCUS HUD"
    };
    public int FocusSessionsCompleted => FocusService.SessionsCompletedToday;
    public int FocusTotalMinutes => FocusService.TotalMinutesToday;

    public ICommand FocusToggleCommand { get; private set; } = null!;
    public ICommand FocusResetCommand { get; private set; } = null!;
    public ICommand FocusSelectTypeCommand { get; private set; } = null!;
    public ICommand FocusSetDurationCommand { get; private set; } = null!;

    // Phase 4: App Launcher & Universal Search State
    public ObservableCollection<AppShortcut> FavoriteApps { get; } = new();
    private string _appSearchQuery = string.Empty;
    public string AppSearchQuery
    {
        get => _appSearchQuery;
        set
        {
            if (_appSearchQuery != value)
            {
                _appSearchQuery = value;
                OnPropertyChanged();
                AppsView?.Refresh();
            }
        }
    }
    public ICollectionView AppsView { get; private set; } = null!;
    public ICommand LaunchAppCommand { get; private set; } = null!;
    public ICommand ExecuteSearchCommand { get; private set; } = null!;
    public ICommand RemoveAppCommand { get; private set; } = null!;

    // Phase 3: Notifications System
    private string _notificationTitle = string.Empty;
    public string NotificationTitle
    {
        get => _notificationTitle;
        set { _notificationTitle = value; OnPropertyChanged(); }
    }

    private string _notificationMessage = string.Empty;
    public string NotificationMessage
    {
        get => _notificationMessage;
        set { _notificationMessage = value; OnPropertyChanged(); }
    }

    private string _notificationIcon = "🔔";
    public string NotificationIcon
    {
        get => _notificationIcon;
        set { _notificationIcon = value; OnPropertyChanged(); }
    }

    private bool _hasActiveNotification;
    public bool HasActiveNotification
    {
        get => _hasActiveNotification;
        set
        {
            if (_hasActiveNotification != value)
            {
                _hasActiveNotification = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSatelliteVisible));
                OnPropertyChanged(nameof(SatelliteIcon));
                OnPropertyChanged(nameof(SatelliteText));
            }
        }
    }

    private DispatcherTimer? _notificationDismissTimer;

    // Telemetry: Time & Date
    public string CurrentTime => DateTime.Now.ToString("HH:mm");
    public string CurrentDate => DateTime.Now.ToString("ddd, MMM dd");

    // Telemetry: Battery
    private BatteryInfo _battery = new();
    public BatteryInfo Battery
    {
        get => _battery;
        private set
        {
            _battery = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(BatteryPercent));
            OnPropertyChanged(nameof(BatteryWidth));
            OnPropertyChanged(nameof(BatteryBrush));
        }
    }
    public int BatteryPercent => Battery.Percent;
    public double BatteryWidth => Math.Clamp((Battery.Percent / 100.0) * 20.0, 2.0, 20.0);
    public Brush BatteryBrush
    {
        get
        {
            if (Battery.IsCharging)
                return new SolidColorBrush(Color.FromRgb(48, 209, 88)); // Apple Vibrant Emerald Green
            if (Battery.Percent <= 20)
                return new SolidColorBrush(Color.FromRgb(255, 69, 58)); // Apple Coral Red
            if (Battery.Percent <= 35)
                return new SolidColorBrush(Color.FromRgb(255, 159, 10)); // Apple Amber
            return CurrentTheme.AccentBrush;
        }
    }

    // Telemetry: Master Volume
    private float _masterVolume = 0.5f;
    public float MasterVolume
    {
        get => _masterVolume;
        set
        {
            float clamped = Math.Clamp(value, 0.0f, 1.0f);
            if (Math.Abs(_masterVolume - clamped) > 0.005f)
            {
                _masterVolume = clamped;
                AudioService.SetMasterVolume(clamped);
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumePercent));
            }
        }
    }
    public int VolumePercent => (int)Math.Round(MasterVolume * 100);

    private bool _isMuted;
    public bool IsMuted
    {
        get => _isMuted;
        set
        {
            if (_isMuted != value)
            {
                _isMuted = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumeIcon));
            }
        }
    }
    public string VolumeIcon => IsMuted ? "🔇" : (VolumePercent > 50 ? "🔊" : (VolumePercent > 0 ? "🔉" : "🔈"));

    // Telemetry: Screen Brightness
    private int _brightnessPercent = 70;
    public int BrightnessPercent
    {
        get => _brightnessPercent;
        set
        {
            int clamped = Math.Clamp(value, 0, 100);
            if (_brightnessPercent != clamped)
            {
                _brightnessPercent = clamped;
                OnPropertyChanged();
                Task.Run(() => BrightnessService.SetBrightness(clamped));
            }
        }
    }
    public bool IsBrightnessHardwareSupported => BrightnessService.IsSupported;

    // Telemetry: Windows Media Session
    private MediaInfo _media = new();
    public MediaInfo Media
    {
        get => _media;
        private set
        {
            _media = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsMediaPlaying));
            OnPropertyChanged(nameof(MediaDisplayTitle));
            OnPropertyChanged(nameof(MediaTitle));
            OnPropertyChanged(nameof(MediaArtist));
            OnPropertyChanged(nameof(MediaThumbnail));
            OnPropertyChanged(nameof(HasMediaThumbnail));
            OnPropertyChanged(nameof(MediaPositionText));
            OnPropertyChanged(nameof(MediaRemainingText));
            OnPropertyChanged(nameof(MediaProgressPercent));
            OnPropertyChanged(nameof(MediaProgressFraction));
            OnPropertyChanged(nameof(MediaProgressWidth));
            OnPropertyChanged(nameof(PlayPauseIcon));
        }
    }
    public bool IsMediaPlaying => Media.IsPlaying;

    private static string CleanDots(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var cleaned = input.Replace("..", "").Replace("•", " ").Replace("…", "").Trim();
        return cleaned;
    }

    public string MediaTitle
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Media.Title) ||
                Media.Title.Equals("No Media Playing", StringComparison.OrdinalIgnoreCase) ||
                Media.Title.Equals("Apple Music", StringComparison.OrdinalIgnoreCase))
                return "Nexus Music";
            return CleanDots(Media.Title);
        }
    }

    public string MediaArtist
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Media.Artist) || Media.Artist.Equals("Windows Audio Session", StringComparison.OrdinalIgnoreCase))
                return "Ready to Play";
            return CleanDots(Media.Artist);
        }
    }

    public BitmapImage? MediaThumbnail => Media.Thumbnail;
    public bool HasMediaThumbnail => Media.Thumbnail != null;

    public string MediaPositionText => FormatDuration(Media.Position);
    public string MediaRemainingText
    {
        get
        {
            if (Media.Duration > Media.Position)
            {
                var rem = Media.Duration - Media.Position;
                return $"-{FormatDuration(rem)}";
            }
            return "-0:00";
        }
    }
    public double MediaProgressPercent => Media.Duration.TotalSeconds > 0
        ? Math.Clamp((Media.Position.TotalSeconds / Media.Duration.TotalSeconds) * 100.0, 0.0, 100.0)
        : 0.0;
    public double MediaProgressFraction => Media.Duration.TotalSeconds > 0
        ? Math.Clamp(Media.Position.TotalSeconds / Media.Duration.TotalSeconds, 0.0, 1.0)
        : 0.0;
    public double MediaProgressWidth => Math.Max(2.0, (MediaProgressPercent / 100.0) * 260.0);

    private static string FormatDuration(TimeSpan ts)
    {
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    public string MediaDisplayTitle
    {
        get
        {
            var title = MediaTitle;
            var artist = MediaArtist;
            if (string.IsNullOrWhiteSpace(artist) || artist == "Ready to Play")
                return title;
            return $"{title} — {artist}";
        }
    }

    public string PlayPauseIcon => Media.IsPlaying ? "⏸" : "▶";

    // Apple Notch Nook Calendar Day Strip (Image 2)
    public ObservableCollection<CalendarDayItem> CalendarDays { get; } = new();
    public string CurrentMonthName => DateTime.Now.ToString("MMM");
    private string _calendarStatus = "Nothing for today — Focus Mode Ready";
    public string CalendarStatus
    {
        get => _calendarStatus;
        set
        {
            if (_calendarStatus != value)
            {
                _calendarStatus = value;
                OnPropertyChanged();
            }
        }
    }

    public void RefreshCalendarDays()
    {
        CalendarDays.Clear();
        var today = DateTime.Today;
        for (int i = -3; i <= 3; i++)
        {
            var d = today.AddDays(i);
            CalendarDays.Add(new CalendarDayItem
            {
                DayName = d.ToString("ddd").ToUpper()[0..2],
                DayNumber = d.Day,
                IsToday = d == today,
                IsSelected = d == today
            });
        }
    }

    // Audio Equalizer Waveform Bars (Reactive to audio peaks with organic wave physics)
    private double _wavePhase;
    private float _audioPeak;
    public float AudioPeak
    {
        get => _audioPeak;
        private set
        {
            _audioPeak = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(EqBar1));
            OnPropertyChanged(nameof(EqBar2));
            OnPropertyChanged(nameof(EqBar3));
            OnPropertyChanged(nameof(EqBar4));
            OnPropertyChanged(nameof(EqBar5));
            OnPropertyChanged(nameof(EqBar6));
        }
    }

    public double EqBar1 => Media.IsPlaying ? Math.Clamp(AudioPeak * 14.0 + 3.5 + 3.0 * Math.Sin(_wavePhase * 1.2), 3.0, 18.0) : 0.0;
    public double EqBar2 => Media.IsPlaying ? Math.Clamp(AudioPeak * 18.0 + 5.0 + 5.0 * Math.Cos(_wavePhase * 1.5), 4.0, 20.0) : 0.0;
    public double EqBar3 => Media.IsPlaying ? Math.Clamp(AudioPeak * 16.0 + 4.5 + 4.5 * Math.Sin(_wavePhase * 2.1 + 0.8), 3.5, 19.0) : 0.0;
    public double EqBar4 => Media.IsPlaying ? Math.Clamp(AudioPeak * 20.0 + 6.0 + 5.5 * Math.Cos(_wavePhase * 1.8), 5.0, 22.0) : 0.0;
    public double EqBar5 => Media.IsPlaying ? Math.Clamp(AudioPeak * 15.0 + 4.0 + 4.0 * Math.Sin(_wavePhase * 1.4 + 1.5), 3.5, 18.0) : 0.0;
    public double EqBar6 => Media.IsPlaying ? Math.Clamp(AudioPeak * 14.0 + 3.5 + 3.5 * Math.Cos(_wavePhase * 1.1 + 0.4), 3.0, 17.0) : 0.0;

    // Phase 3 Telemetry: System Activity Metrics
    private SystemMetrics _metrics = new();
    public SystemMetrics Metrics
    {
        get => _metrics;
        private set
        {
            _metrics = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CpuPercent));
            OnPropertyChanged(nameof(RamPercent));
            OnPropertyChanged(nameof(RamDetails));
            OnPropertyChanged(nameof(DiskPercent));
            OnPropertyChanged(nameof(DiskDetails));
            OnPropertyChanged(nameof(NetworkSpeedText));
        }
    }
    public double CpuPercent { get => Math.Round(Metrics.CpuPercent, 1); set { } }
    public double RamPercent { get => Metrics.RamPercent; set { } }
    public string RamDetails => $"{Metrics.RamUsedGb} GB / {Metrics.RamTotalGb} GB ({Metrics.RamPercent}%)";
    public double DiskPercent { get => Metrics.DiskPercent; set { } }
    public string DiskDetails => $"{Metrics.DiskPercent}% Used ({Metrics.DiskFreeGb})";
    public string NetworkSpeedText => $"↓ {Metrics.NetworkDownloadKbps:0.0} KB/s  ↑ {Metrics.NetworkUploadKbps:0.0} KB/s";

    // Behavior & Settings
    private bool _alwaysOnTop = true;
    public bool AlwaysOnTop
    {
        get => _alwaysOnTop;
        set
        {
            if (_alwaysOnTop != value)
            {
                _alwaysOnTop = value;
                Settings.AlwaysOnTop = value;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
            }
        }
    }

    private bool _autoCollapse = true;
    public bool AutoCollapse
    {
        get => _autoCollapse;
        set
        {
            if (_autoCollapse != value)
            {
                _autoCollapse = value;
                Settings.AutoCollapse = value;
                SettingsManager.Save(Settings);
                OnPropertyChanged();
            }
        }
    }

    // Commands
    public ICommand ToggleExpandCommand { get; }
    public ICommand ExpandCommand { get; }
    public ICommand CollapseCommand { get; }
    public ICommand SwitchTabCommand { get; }
    public ICommand ChangeThemeCommand { get; }
    public ICommand ResetPositionCommand { get; }
    public ICommand ToggleAlwaysOnTopCommand { get; }
    public ICommand ToggleAutoCollapseCommand { get; }
    public ICommand ExitAppCommand { get; }

    // Media & Volume Commands
    public ICommand ToggleMuteCommand { get; }
    public ICommand MediaPlayPauseCommand { get; }
    public ICommand MediaNextCommand { get; }
    public ICommand MediaPrevCommand { get; }
    public ICommand SeekCommand { get; }
    public ICommand SelectCalendarDayCommand { get; }

    // Phase 3: Quick Controls Commands
    public ICommand WifiCommand { get; }
    public ICommand BluetoothCommand { get; }
    public ICommand AirplaneCommand { get; }
    public ICommand BatterySaverCommand { get; }
    public ICommand NightLightCommand { get; }
    public ICommand ToggleWindowsThemeCommand { get; }
    public ICommand ScreenshotCommand { get; }
    public ICommand ScreenRecordCommand { get; }
    public ICommand LockScreenCommand { get; }
    public ICommand SleepCommand { get; }
    public ICommand TestNotificationCommand { get; }
    public ICommand DismissNotificationCommand { get; }

    // Events for View reactions
    public event Action? RequestResetPosition;

    // Timers
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _batteryTimer;
    private readonly DispatcherTimer _audioWaveTimer;
    private readonly DispatcherTimer _audioSyncTimer;

    public MainViewModel()
    {
        Settings = SettingsManager.Load();
        _currentTheme = Settings.Theme == ThemePreset.Custom
            ? ThemePalette.CreateCustomTheme(Settings.CustomAccentHex)
            : ThemePalette.GetTheme(Settings.Theme);
        _alwaysOnTop = Settings.AlwaysOnTop;
        _autoCollapse = Settings.AutoCollapse;
        _launchOnStartup = StartupService.IsLaunchOnStartupEnabled();
        _soundEffectsEnabled = Settings.SoundEffectsEnabled;
        SoundService.Enabled = _soundEffectsEnabled;
        _ambientGlowEnabled = Settings.AmbientGlowEnabled;
        _islandScale = Settings.IslandScale;
        _islandOpacity = Settings.IslandOpacity;
        _customAccentHex = Settings.CustomAccentHex;

        ToggleLaunchOnStartupCommand = new RelayCommand(() => LaunchOnStartup = !LaunchOnStartup);
        ToggleSoundEffectsCommand = new RelayCommand(() => SoundEffectsEnabled = !SoundEffectsEnabled);
        ToggleAmbientGlowCommand = new RelayCommand(() => AmbientGlowEnabled = !AmbientGlowEnabled);
        ApplyCustomAccentCommand = new RelayCommand(() => ApplyTheme(ThemePreset.Custom));

        // Base Commands
        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
        ExpandCommand = new RelayCommand(() => IsExpanded = true);
        CollapseCommand = new RelayCommand(() => IsExpanded = false);

        SwitchTabCommand = new RelayCommand(p =>
        {
            if (p is MiniAppTab tab) CurrentTab = tab;
            else if (p is string s && Enum.TryParse<MiniAppTab>(s, out var t)) CurrentTab = t;
        });

        ChangeThemeCommand = new RelayCommand(p =>
        {
            if (p is ThemePreset preset) ApplyTheme(preset);
            else if (p is string s && Enum.TryParse<ThemePreset>(s, out var tp)) ApplyTheme(tp);
        });

        ResetPositionCommand = new RelayCommand(ClearSavedPosition);
        ToggleAlwaysOnTopCommand = new RelayCommand(() => AlwaysOnTop = !AlwaysOnTop);
        ToggleAutoCollapseCommand = new RelayCommand(() => AutoCollapse = !AutoCollapse);

        ExitAppCommand = new RelayCommand(() =>
        {
            SettingsManager.Save(Settings);
            System.Windows.Application.Current.Shutdown();
        });

        // Media & Audio Commands
        ToggleMuteCommand = new RelayCommand(() =>
        {
            AudioService.ToggleMute();
            IsMuted = AudioService.IsMuted();
        });

        MediaPlayPauseCommand = new RelayCommand(async () => await MediaService.TogglePlayPauseAsync());
        MediaNextCommand = new RelayCommand(async () => await MediaService.SkipNextAsync());
        MediaPrevCommand = new RelayCommand(async () => await MediaService.SkipPreviousAsync());

        SeekCommand = new RelayCommand(p =>
        {
            if (p is double percent && Media.Duration.TotalSeconds > 0)
            {
                var newPos = TimeSpan.FromSeconds(Media.Duration.TotalSeconds * Math.Clamp(percent, 0.0, 1.0));
                Media.Position = newPos;
                OnPropertyChanged(nameof(MediaProgressPercent));
                OnPropertyChanged(nameof(MediaProgressFraction));
                OnPropertyChanged(nameof(MediaProgressWidth));
                OnPropertyChanged(nameof(MediaPositionText));
                OnPropertyChanged(nameof(MediaRemainingText));
                _ = MediaService.SeekToPositionAsync(newPos);
            }
        });

        SelectCalendarDayCommand = new RelayCommand(p =>
        {
            if (p is CalendarDayItem item)
            {
                foreach (var d in CalendarDays) d.IsSelected = (d == item);
                CalendarStatus = item.IsToday
                    ? "Nothing for today — Focus Mode Ready"
                    : $"{item.DayName} {item.DayNumber:D2} — No Events Scheduled";
                SoundService.PlayClick();
            }
        });

        // Phase 3 Quick Controls Commands
        WifiCommand = new RelayCommand(() =>
        {
            QuickControls.OpenWifiFlyout();
            ShowNotification("Wi-Fi Networks", "Available network flyout opened", "📶");
        });

        BluetoothCommand = new RelayCommand(() =>
        {
            QuickControls.OpenBluetoothSettings();
            ShowNotification("Bluetooth Devices", "Windows Bluetooth settings opened", "🎧");
        });

        AirplaneCommand = new RelayCommand(() =>
        {
            QuickControls.OpenAirplaneMode();
            ShowNotification("Airplane Mode", "Network flight mode settings opened", "✈️");
        });

        BatterySaverCommand = new RelayCommand(() =>
        {
            QuickControls.OpenBatterySaver();
            ShowNotification("Battery Saver", "Power efficiency settings opened", "🔋");
        });

        NightLightCommand = new RelayCommand(() =>
        {
            QuickControls.OpenNightLight();
            ShowNotification("Night Light", "Warm display color settings opened", "💡");
        });

        ToggleWindowsThemeCommand = new RelayCommand(() =>
        {
            QuickControls.ToggleWindowsTheme();
            bool isDark = QuickControls.IsWindowsDarkMode();
            ShowNotification("Windows Theme", isDark ? "Dark theme activated" : "Light theme activated", "🌙");
        });

        ScreenshotCommand = new RelayCommand(() =>
        {
            QuickControls.TriggerScreenCapture();
            ShowNotification("Snipping Tool", "Drag to select screenshot region", "📸");
        });

        ScreenRecordCommand = new RelayCommand(() =>
        {
            QuickControls.TriggerScreenRecording();
            ShowNotification("Screen Recording", "Windows Game Bar capture opened", "🎥");
        });

        LockScreenCommand = new RelayCommand(() => QuickControls.LockScreen());
        SleepCommand = new RelayCommand(() => QuickControls.SleepSystem());

        TestNotificationCommand = new RelayCommand(() =>
        {
            ShowNotification("Nexus Alert", "Dynamic Island live notification test active!", "✦");
        });

        DismissNotificationCommand = new RelayCommand(() => HasActiveNotification = false);

        // Initial battery poll
        Battery = BatteryService.GetBatteryInfo();

        SelectExpandedModeCommand = new RelayCommand(p =>
        {
            if (p is IslandExpandedMode mode) ExpandedMode = mode;
            else if (p is string s && Enum.TryParse<IslandExpandedMode>(s, out var parsed)) ExpandedMode = parsed;
            SoundService.PlayClick();
        });

        RefreshCalendarDays();

        // 1. Clock Timer (every 1 second)
        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (s, e) =>
        {
            OnPropertyChanged(nameof(CurrentTime));
            OnPropertyChanged(nameof(CurrentDate));
            if (Media.IsPlaying)
            {
                if (Media.Position < Media.Duration)
                {
                    Media.Position += TimeSpan.FromSeconds(1);
                    OnPropertyChanged(nameof(MediaPositionText));
                    OnPropertyChanged(nameof(MediaRemainingText));
                    OnPropertyChanged(nameof(MediaProgressPercent));
                    OnPropertyChanged(nameof(MediaProgressWidth));
                }
            }
        };
        _clockTimer.Start();

        // 2. Battery Timer (every 2.5 seconds)
        _batteryTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
        _batteryTimer.Tick += (s, e) =>
        {
            Battery = BatteryService.GetBatteryInfo();
        };
        _batteryTimer.Start();

        // 3. Audio Volume Sync Timer (every 600ms)
        _audioSyncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _audioSyncTimer.Tick += (s, e) =>
        {
            try
            {
                float vol = AudioService.GetMasterVolume();
                if (Math.Abs(_masterVolume - vol) > 0.015f)
                {
                    _masterVolume = vol;
                    OnPropertyChanged(nameof(MasterVolume));
                    OnPropertyChanged(nameof(VolumePercent));
                }

                bool muted = AudioService.IsMuted();
                if (_isMuted != muted)
                {
                    _isMuted = muted;
                    OnPropertyChanged(nameof(IsMuted));
                    OnPropertyChanged(nameof(VolumeIcon));
                }
            }
            catch { }
        };
        _audioSyncTimer.Start();

        // 4. Equalizer Waveform Timer (every 20ms for buttery 50fps Apple fluid waves)
        _audioWaveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(20) };
        _audioWaveTimer.Tick += (s, e) =>
        {
            try
            {
                _wavePhase += 0.14;
                float peak = AudioService.GetPeakSample();
                if (Media.IsPlaying && peak < 0.05f)
                {
                    peak = (float)(0.20 + 0.12 * Math.Sin(_wavePhase));
                }
                AudioPeak = peak;
            }
            catch { }
        };
        _audioWaveTimer.Start();

        // 5. System Monitor Hook (Phase 3)
        SystemMonitor.MetricsUpdated += metrics =>
        {
            App.Current.Dispatcher.Invoke(() => Metrics = metrics);
        };
        SystemMonitor.Start();

        // 6. Phase 4: Mini-Apps Initialization
        InitializePhase4MiniApps();

        // Initialize background hardware & media services asynchronously
        _ = Task.Run(InitializePhase3ServicesAsync);
    }

    private void InitializePhase4MiniApps()
    {
        // --- 1. Clipboard Manager ---
        ClipboardService.Initialize(Settings.PinnedClipboard);
        ClipboardView = CollectionViewSource.GetDefaultView(ClipboardService.History);
        ClipboardView.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(ClipboardSearchQuery)) return true;
            if (item is ClipboardEntry entry)
            {
                return entry.Text.Contains(ClipboardSearchQuery, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        };

        CopyClipboardItemCommand = new RelayCommand(p =>
        {
            if (p is ClipboardEntry entry)
            {
                ClipboardService.CopyToClipboard(entry.Text);
                ShowNotification("Copied! 📋", "Text copied to Windows clipboard", "📋");
            }
            else if (p is string s)
            {
                ClipboardService.CopyToClipboard(s);
                ShowNotification("Copied! 📋", "Text copied to Windows clipboard", "📋");
            }
        });

        TogglePinClipboardCommand = new RelayCommand(p =>
        {
            if (p is ClipboardEntry entry)
            {
                ClipboardService.TogglePin(entry);
                Settings.PinnedClipboard = ClipboardService.History.Where(x => x.IsPinned).ToList();
                SettingsManager.Save(Settings);
                ClipboardView.Refresh();
            }
        });

        DeleteClipboardItemCommand = new RelayCommand(p =>
        {
            if (p is ClipboardEntry entry)
            {
                ClipboardService.RemoveEntry(entry);
                if (entry.IsPinned)
                {
                    Settings.PinnedClipboard = ClipboardService.History.Where(x => x.IsPinned).ToList();
                    SettingsManager.Save(Settings);
                }
                ClipboardView.Refresh();
            }
        });

        ClearClipboardHistoryCommand = new RelayCommand(() =>
        {
            ClipboardService.ClearUnpinned();
            ShowNotification("Clipboard Cleared 🧹", "Unpinned history cleared", "🧹");
            ClipboardView.Refresh();
        });

        // --- 2. Quick Notes ---
        if (Settings.Notes != null && Settings.Notes.Count > 0)
        {
            foreach (var n in Settings.Notes) Notes.Add(n);
        }
        else
        {
            var welcome = new QuickNote
            {
                Title = "Welcome to Nexus Island",
                Content = "This is your fast sci-fi scratchpad.\n\n• Notes autosave instantly.\n• Pin important items to stay at top.\n• Search through notes anytime.",
                CreatedAt = DateTime.Now,
                IsPinned = true
            };
            Notes.Add(welcome);
            Settings.Notes = Notes.ToList();
            SettingsManager.Save(Settings);
        }
        SelectedNote = Notes.FirstOrDefault();

        NotesView = CollectionViewSource.GetDefaultView(Notes);
        NotesView.SortDescriptions.Add(new SortDescription(nameof(QuickNote.IsPinned), ListSortDirection.Descending));
        NotesView.SortDescriptions.Add(new SortDescription(nameof(QuickNote.CreatedAt), ListSortDirection.Descending));
        NotesView.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(NotesSearchQuery)) return true;
            if (item is QuickNote note)
            {
                return note.Title.Contains(NotesSearchQuery, StringComparison.OrdinalIgnoreCase) ||
                       note.Content.Contains(NotesSearchQuery, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        };

        NewNoteCommand = new RelayCommand(() =>
        {
            var newNote = new QuickNote
            {
                Title = $"Note {DateTime.Now:MMM dd, HH:mm}",
                Content = string.Empty,
                CreatedAt = DateTime.Now,
                IsPinned = false
            };
            Notes.Insert(0, newNote);
            SelectedNote = newNote;
            NotifyNoteEdited();
            ShowNotification("Note Created 📝", newNote.Title, "📝");
        });

        DeleteNoteCommand = new RelayCommand(p =>
        {
            var target = p as QuickNote ?? SelectedNote;
            if (target != null)
            {
                Notes.Remove(target);
                SelectedNote = Notes.FirstOrDefault();
                NotifyNoteEdited();
                ShowNotification("Note Deleted 🗑️", target.Title, "🗑️");
            }
        });

        TogglePinNoteCommand = new RelayCommand(p =>
        {
            var target = p as QuickNote ?? SelectedNote;
            if (target != null)
            {
                target.IsPinned = !target.IsPinned;
                NotifyNoteEdited();
            }
        });

        SaveNotesCommand = new RelayCommand(NotifyNoteEdited);

        // --- 3. Calculator ---
        CalcInputCharCommand = new RelayCommand(p =>
        {
            if (p is string s)
            {
                if (CalcExpression == "0" && s != ".") CalcExpression = s;
                else CalcExpression += s;
            }
        });

        CalcClearCommand = new RelayCommand(() =>
        {
            CalcExpression = string.Empty;
            CalcResult = "0";
        });

        CalcBackspaceCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(CalcExpression))
            {
                CalcExpression = CalcExpression.Substring(0, CalcExpression.Length - 1);
            }
        });

        CalcEqualsCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(CalcExpression))
            {
                string res = CalculatorService.Evaluate(CalcExpression);
                CalcResult = res;
                CalcHistory.Clear();
                foreach (var h in CalculatorService.History) CalcHistory.Add(h);
            }
        });

        CalcFunctionCommand = new RelayCommand(p =>
        {
            if (p is string fn)
            {
                CalcExpression = fn switch
                {
                    "sqrt" => $"sqrt({CalcExpression})",
                    "sin" => $"sin({CalcExpression})",
                    "cos" => $"cos({CalcExpression})",
                    "tan" => $"tan({CalcExpression})",
                    "log" => $"log({CalcExpression})",
                    "ln" => $"ln({CalcExpression})",
                    "sq" => $"({CalcExpression})^2",
                    "inv" => $"1/({CalcExpression})",
                    _ => $"{fn}({CalcExpression})"
                };
            }
        });

        // --- 4. Focus Mode & Timers ---
        FocusService.Tick += () =>
        {
            OnPropertyChanged(nameof(FocusDisplayText));
            OnPropertyChanged(nameof(FocusProgressPercent));
            OnPropertyChanged(nameof(FocusIsRunning));
            OnPropertyChanged(nameof(FocusModeTitle));
            OnPropertyChanged(nameof(FocusButtonGlyph));
            OnPropertyChanged(nameof(FocusButtonText));
            OnPropertyChanged(nameof(IsSatelliteVisible));
            OnPropertyChanged(nameof(SatelliteIcon));
            OnPropertyChanged(nameof(SatelliteText));
            OnPropertyChanged(nameof(SatelliteProgressPercent));
        };

        FocusService.SessionCompleted += (title, msg) =>
        {
            ShowNotification(title, msg, "🎯");
            OnPropertyChanged(nameof(FocusSessionsCompleted));
            OnPropertyChanged(nameof(FocusTotalMinutes));
            OnPropertyChanged(nameof(IsSatelliteVisible));
        };

        FocusToggleCommand = new RelayCommand(() =>
        {
            FocusService.ToggleStartPause();
            OnPropertyChanged(nameof(IsSatelliteVisible));
            OnPropertyChanged(nameof(SatelliteIcon));
            OnPropertyChanged(nameof(SatelliteText));
        });
        FocusResetCommand = new RelayCommand(() =>
        {
            FocusService.Reset();
            OnPropertyChanged(nameof(IsSatelliteVisible));
        });
        FocusSelectTypeCommand = new RelayCommand(p =>
        {
            if (p is FocusType ft) FocusService.SelectMode(ft);
            else if (p is string s && Enum.TryParse<FocusType>(s, out var parsed)) FocusService.SelectMode(parsed);
            OnPropertyChanged(nameof(FocusModeTitle));
            OnPropertyChanged(nameof(SatelliteIcon));
        });
        FocusSetDurationCommand = new RelayCommand(p =>
        {
            if (p != null && int.TryParse(p.ToString(), out int mins))
            {
                FocusService.SetDuration(mins);
            }
        });

        // Apple-Style Satellite Bubble Commands
        OpenSatelliteTabCommand = new RelayCommand(() =>
        {
            SoundService.PlayClick();
            if (FocusIsRunning || TestSatelliteActive)
            {
                CurrentTab = MiniAppTab.Focus;
            }
            else if (IsMediaPlaying)
            {
                CurrentTab = MiniAppTab.QuickControls;
            }
            IsExpanded = true;
        });

        ToggleTestSatelliteCommand = new RelayCommand(() =>
        {
            TestSatelliteActive = !TestSatelliteActive;
            SoundService.PlayClick();
            if (TestSatelliteActive)
            {
                ShowNotification("Satellite Detached", "Apple secondary multitasking bubble is now live!", "✦");
            }
        });

        // --- 5. App Launcher & Universal Search ---
        if (Settings.FavoriteApps != null)
        {
            foreach (var app in Settings.FavoriteApps) FavoriteApps.Add(app);
        }
        AppsView = CollectionViewSource.GetDefaultView(FavoriteApps);
        AppsView.Filter = item =>
        {
            if (string.IsNullOrWhiteSpace(AppSearchQuery)) return true;
            if (item is AppShortcut app)
            {
                return app.Name.Contains(AppSearchQuery, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        };

        LaunchAppCommand = new RelayCommand(p =>
        {
            if (p is AppShortcut app)
            {
                AppLauncherService.LaunchShortcut(app);
                ShowNotification("Launched 🚀", $"Started {app.Name}", app.IconGlyph);
            }
        });

        ExecuteSearchCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrWhiteSpace(AppSearchQuery))
            {
                string q = AppSearchQuery;
                AppSearchQuery = string.Empty;
                AppLauncherService.ExecuteSearchOrCommand(q);
                ShowNotification("Nexus Search ⚡", $"Executing '{q}'", "🔍");
            }
        });

        RemoveAppCommand = new RelayCommand(p =>
        {
            if (p is AppShortcut app)
            {
                FavoriteApps.Remove(app);
                Settings.FavoriteApps = FavoriteApps.ToList();
                SettingsManager.Save(Settings);
                AppsView.Refresh();
            }
        });
    }

    public void NotifyNoteEdited()
    {
        Settings.Notes = Notes.ToList();
        SettingsManager.Save(Settings);
        NotesView?.Refresh();
        OnPropertyChanged(nameof(SelectedNoteTitle));
        OnPropertyChanged(nameof(SelectedNoteContent));
    }

    public void OnClipboardUpdated()
    {
        App.Current.Dispatcher.Invoke(() =>
        {
            ClipboardService.OnClipboardUpdated();
            ClipboardView?.Refresh();
        });
    }

    public void ShowNotification(string title, string message, string icon = "🔔")
    {
        NotificationTitle = title;
        NotificationMessage = message;
        NotificationIcon = icon;
        HasActiveNotification = true;
        SoundService.PlayNotification();

        _notificationDismissTimer?.Stop();
        _notificationDismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4.5) };
        _notificationDismissTimer.Tick += (s, e) =>
        {
            HasActiveNotification = false;
            _notificationDismissTimer.Stop();
        };
        _notificationDismissTimer.Start();
    }

    private async Task InitializePhase3ServicesAsync()
    {
        try
        {
            // Initial Volume & Mute
            float initVol = AudioService.GetMasterVolume();
            bool initMuted = AudioService.IsMuted();
            int initBright = BrightnessService.GetBrightness();

            App.Current.Dispatcher.Invoke(() =>
            {
                _masterVolume = initVol;
                _isMuted = initMuted;
                _brightnessPercent = initBright;
                OnPropertyChanged(nameof(MasterVolume));
                OnPropertyChanged(nameof(VolumePercent));
                OnPropertyChanged(nameof(IsMuted));
                OnPropertyChanged(nameof(VolumeIcon));
                OnPropertyChanged(nameof(BrightnessPercent));
            });

            // Media Session Listener
            MediaService.MediaInfoChanged += newInfo =>
            {
                App.Current.Dispatcher.Invoke(() => Media = newInfo);
            };
            await MediaService.InitializeAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InitializePhase3Services error: {ex.Message}");
        }
    }

    public void ApplyTheme(ThemePreset preset)
    {
        Settings.Theme = preset;
        CurrentTheme = preset == ThemePreset.Custom
            ? ThemePalette.CreateCustomTheme(CustomAccentHex)
            : ThemePalette.GetTheme(preset);
        SettingsManager.Save(Settings);
        SoundService.PlayClick();
    }

    public void SavePosition(double left, double top)
    {
        Settings.WindowLeft = left;
        Settings.WindowTop = top;
        SettingsManager.Save(Settings);
    }

    public void ClearSavedPosition()
    {
        Settings.WindowLeft = null;
        Settings.WindowTop = null;
        SettingsManager.Save(Settings);
        RequestResetPosition?.Invoke();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
