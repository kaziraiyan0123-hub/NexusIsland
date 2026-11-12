using System;
using System.Windows.Threading;

namespace NexusIsland.Services;

public enum FocusType
{
    PomodoroFocus,
    PomodoroShortBreak,
    PomodoroLongBreak,
    CustomTimer,
    Stopwatch
}

public class FocusService
{
    private readonly DispatcherTimer _timer;
    private TimeSpan _remainingTime;
    private TimeSpan _initialDuration;
    private DateTime _stopwatchStartTime;
    private TimeSpan _stopwatchElapsed = TimeSpan.Zero;

    public FocusType CurrentType { get; private set; } = FocusType.PomodoroFocus;
    public bool IsRunning { get; private set; }

    public int SessionsCompletedToday { get; private set; }
    public int TotalMinutesToday { get; private set; }

    public event Action? Tick;
    public event Action<string, string>? SessionCompleted;

    public FocusService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _timer.Tick += Timer_Tick;
        SetDuration(25);
    }

    public string DisplayText
    {
        get
        {
            if (CurrentType == FocusType.Stopwatch)
            {
                var elapsed = IsRunning ? _stopwatchElapsed + (DateTime.Now - _stopwatchStartTime) : _stopwatchElapsed;
                return $"{(int)elapsed.TotalMinutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds / 100:0}";
            }
            return $"{(int)_remainingTime.TotalMinutes:00}:{_remainingTime.Seconds:00}";
        }
    }

    public double ProgressFraction
    {
        get
        {
            if (CurrentType == FocusType.Stopwatch) return 1.0;
            if (_initialDuration.TotalSeconds <= 0) return 0.0;
            double fraction = 1.0 - (_remainingTime.TotalSeconds / _initialDuration.TotalSeconds);
            return Math.Clamp(fraction, 0.0, 1.0);
        }
    }

    public void SetDuration(int minutes)
    {
        Pause();
        _initialDuration = TimeSpan.FromMinutes(minutes);
        _remainingTime = _initialDuration;
        Tick?.Invoke();
    }

    public void SelectMode(FocusType type)
    {
        Pause();
        CurrentType = type;

        switch (type)
        {
            case FocusType.PomodoroFocus:
                SetDuration(25);
                break;
            case FocusType.PomodoroShortBreak:
                SetDuration(5);
                break;
            case FocusType.PomodoroLongBreak:
                SetDuration(15);
                break;
            case FocusType.CustomTimer:
                SetDuration(10);
                break;
            case FocusType.Stopwatch:
                _stopwatchElapsed = TimeSpan.Zero;
                break;
        }
        Tick?.Invoke();
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        if (CurrentType == FocusType.Stopwatch)
        {
            _stopwatchStartTime = DateTime.Now;
        }
        _timer.Start();
        Tick?.Invoke();
    }

    public void Pause()
    {
        if (!IsRunning) return;
        IsRunning = false;
        _timer.Stop();
        if (CurrentType == FocusType.Stopwatch)
        {
            _stopwatchElapsed += (DateTime.Now - _stopwatchStartTime);
        }
        Tick?.Invoke();
    }

    public void ToggleStartPause()
    {
        if (IsRunning) Pause();
        else Start();
    }

    public void Reset()
    {
        Pause();
        if (CurrentType == FocusType.Stopwatch)
        {
            _stopwatchElapsed = TimeSpan.Zero;
        }
        else
        {
            _remainingTime = _initialDuration;
        }
        Tick?.Invoke();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (CurrentType == FocusType.Stopwatch)
        {
            Tick?.Invoke();
            return;
        }

        if (_remainingTime.TotalSeconds > 1)
        {
            _remainingTime = _remainingTime.Subtract(TimeSpan.FromSeconds(0.25));
            Tick?.Invoke();
        }
        else
        {
            // Completed
            _remainingTime = TimeSpan.Zero;
            Pause();
            Tick?.Invoke();

            string title;
            string message;

            if (CurrentType == FocusType.PomodoroFocus)
            {
                SessionsCompletedToday++;
                TotalMinutesToday += (int)_initialDuration.TotalMinutes;
                title = "Focus Session Complete! 🎯";
                message = "Outstanding work! Take a 5-minute breather.";
            }
            else if (CurrentType == FocusType.PomodoroShortBreak || CurrentType == FocusType.PomodoroLongBreak)
            {
                title = "Break Ended ⚡";
                message = "Ready to start the next session!";
            }
            else
            {
                title = "Timer Complete! ⏱️";
                message = "Your countdown timer has finished.";
            }

            SessionCompleted?.Invoke(title, message);
        }
    }
}
