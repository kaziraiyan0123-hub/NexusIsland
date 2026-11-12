using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using NexusIsland.Core;
using NexusIsland.Services;
using NexusIsland.ViewModels;
using Point = System.Windows.Point;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace NexusIsland;

public partial class MainWindow : Window
{
    private readonly HotkeyService _hotkeyService = new();
    private readonly TrayIconService _trayService = new();
    private Point _dragStartPoint;
    private bool _isDragging;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        InitializeComponent();

        RestoreOrCenterPosition();

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Deactivated += MainWindow_Deactivated;
    }

    private void RestoreOrCenterPosition()
    {
        var workArea = SystemParameters.WorkArea;
        double defaultLeft = workArea.Left + (workArea.Width - Width) / 2;
        double defaultTop = workArea.Top;

        if (ViewModel.Settings.WindowLeft.HasValue && ViewModel.Settings.WindowTop.HasValue)
        {
            double savedLeft = ViewModel.Settings.WindowLeft.Value;
            double savedTop = ViewModel.Settings.WindowTop.Value;

            // If saved coordinates were within 15px of top edge, dock flush to workArea.Top
            if (savedTop <= workArea.Top + 15)
            {
                savedTop = workArea.Top;
            }

            // Verify saved coordinates are reasonably within screen bounds
            if (savedLeft >= -100 && savedLeft <= workArea.Right &&
                savedTop >= workArea.Top && savedTop <= workArea.Bottom - 60)
            {
                Left = savedLeft;
                Top = savedTop;
                return;
            }
        }

        Left = defaultLeft;
        Top = defaultTop;
    }

    private void CenterOnScreen()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - Width) / 2;
        Top = workArea.Top;
        ViewModel.SavePosition(Left, Top);
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try { System.IO.File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] MainWindow_Loaded\n"); } catch { }
        var hwnd = new WindowInteropHelper(this).Handle;

        // Hook WndProc for pass-through hit testing on transparent margins
        try
        {
            _hwndSource = HwndSource.FromHwnd(hwnd);
            _hwndSource?.AddHook(WndProc);
        }
        catch { }

        // Register Global Hotkey (Ctrl + Shift + Space)
        try
        {
            _hotkeyService.Register(hwnd, NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, 0x20);
            _hotkeyService.HotkeyPressed += ToggleIsland;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Hotkey registration failed: {ex.Message}");
        }

        // Initialize System Tray Icon
        try
        {
            _trayService.Initialize();
            _trayService.ToggleRequested += ToggleIsland;
            _trayService.ExitRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _trayService.Dispose();
                    System.Windows.Application.Current.Shutdown();
                });
            };
            _trayService.ShowBalloon("✦ NEXUS ISLAND — Control Center Ready", "Quick controls, CPU/RAM telemetry & notifications are active!");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Tray icon initialization failed: {ex.Message}");
        }

        // Register Native Windows Clipboard Listener
        try
        {
            NativeMethods.AddClipboardFormatListener(hwnd);
        }
        catch { }

        // Wire ViewModel events
        ViewModel.RequestResetPosition += CenterOnScreen;
        ViewModel.RequestModeResize += _ =>
        {
            if (ViewModel.IsExpanded) Dispatcher.Invoke(() => AnimateModeSwitch());
        };
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Apply initial visual sizing
        AnimateState(ViewModel.IsExpanded, isInitial: true);
        if (SatelliteBubble != null)
        {
            SatelliteBubble.Visibility = ViewModel.IsSatelliteVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // Native Windows Clipboard Change
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ViewModel.OnClipboardUpdated();
            handled = true;
            return IntPtr.Zero;
        }

        // WM_NCHITTEST: Pass clicks through to windows underneath if outside IslandContainer or SatelliteBubble
        if (msg == NativeMethods.WM_NCHITTEST)
        {
            int x = (short)(lParam.ToInt32() & 0xFFFF);
            int y = (short)((lParam.ToInt32() >> 16) & 0xFFFF);

            Point ptScreen = new Point(x, y);
            bool insideMain = false;
            try
            {
                Point ptIsland = IslandContainer.PointFromScreen(ptScreen);
                Rect islandBounds = new Rect(0, 0, IslandContainer.ActualWidth, IslandContainer.ActualHeight);
                insideMain = islandBounds.Contains(ptIsland);
            }
            catch { }

            bool insideSatellite = false;
            if (SatelliteBubble != null && SatelliteBubble.Visibility == Visibility.Visible)
            {
                try
                {
                    Point ptSat = SatelliteBubble.PointFromScreen(ptScreen);
                    Rect satBounds = new Rect(0, 0, SatelliteBubble.ActualWidth, SatelliteBubble.ActualHeight);
                    insideSatellite = satBounds.Contains(ptSat);
                }
                catch { }
            }

            if (!insideMain && !insideSatellite)
            {
                handled = true;
                return (IntPtr)(-1); // HTTRANSPARENT - Click goes straight through to apps underneath!
            }
        }
        return IntPtr.Zero;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModel.IsExpanded))
        {
            Dispatcher.Invoke(() => AnimateState(ViewModel.IsExpanded, isInitial: false));
        }
        else if (e.PropertyName == nameof(ViewModel.IsSatelliteVisible))
        {
            Dispatcher.Invoke(() => AnimateSatellite(ViewModel.IsSatelliteVisible));
        }
    }

    private void AnimateState(bool isExpanded, bool isInitial)
    {
        double targetW;
        double targetH;
        double targetRadius;

        if (isExpanded)
        {
            switch (ViewModel.ExpandedMode)
            {
                case IslandExpandedMode.MusicPlayer:
                    targetW = 440;
                    targetH = 232;
                    targetRadius = 24;
                    break;
                case IslandExpandedMode.NotchNook:
                    targetW = 570;
                    targetH = 175;
                    targetRadius = 22;
                    break;
                default:
                    targetW = 590;
                    targetH = 500;
                    targetRadius = 24;
                    break;
            }
        }
        else
        {
            targetW = ViewModel.IsMediaPlaying ? 295 : 210;
            targetH = 34;
            targetRadius = 17;
        }

        double satLeft = (targetW / 2.0) + 12.0;
        bool isDocked = Top <= 2;
        CornerRadius radius = isDocked
            ? new CornerRadius(0, 0, targetRadius, targetRadius)
            : new CornerRadius(targetRadius);

        if (isInitial)
        {
            IslandContainer.Width = targetW;
            IslandContainer.Height = targetH;
            IslandContainer.CornerRadius = radius;
            CompactView.Visibility = isExpanded ? Visibility.Collapsed : Visibility.Visible;
            CompactView.Opacity = isExpanded ? 0.0 : 1.0;
            ExpandedView.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
            ExpandedView.Opacity = isExpanded ? 1.0 : 0.0;
            if (SatelliteBubble != null)
            {
                Canvas.SetLeft(SatelliteBubble, satLeft);
            }
            return;
        }

        var duration = TimeSpan.FromMilliseconds(340);
        // Ultra-smooth Apple fluid curve with gentle organic spring physics
        var easeSpring = new BackEase { Amplitude = 0.05, EasingMode = EasingMode.EaseOut };
        var easeCubic = new CubicEase { EasingMode = EasingMode.EaseOut };

        // Morph container dimensions smoothly
        var animW = new DoubleAnimation(targetW, new Duration(duration)) { EasingFunction = easeSpring };
        var animH = new DoubleAnimation(targetH, new Duration(duration)) { EasingFunction = easeSpring };

        IslandContainer.BeginAnimation(FrameworkElement.WidthProperty, animW);
        IslandContainer.BeginAnimation(FrameworkElement.HeightProperty, animH);
        IslandContainer.CornerRadius = radius;

        // Smoothly glide satellite bubble with island flank
        if (SatelliteBubble != null)
        {
            var animSatLeft = new DoubleAnimation(satLeft, new Duration(duration)) { EasingFunction = easeSpring };
            SatelliteBubble.BeginAnimation(Canvas.LeftProperty, animSatLeft);
        }

        // Apple Jelly Squash & Stretch Effect (Delicate organic elastic deformation)
        if (IslandSpringScale != null)
        {
            var stretchX = new DoubleAnimationUsingKeyFrames();
            stretchX.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            stretchX.KeyFrames.Add(new EasingDoubleKeyFrame(isExpanded ? 1.012 : 0.988, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)), easeCubic));
            stretchX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), easeCubic));
            IslandSpringScale.BeginAnimation(ScaleTransform.ScaleXProperty, stretchX);

            var stretchY = new DoubleAnimationUsingKeyFrames();
            stretchY.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            stretchY.KeyFrames.Add(new EasingDoubleKeyFrame(isExpanded ? 0.988 : 1.012, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90)), easeCubic));
            stretchY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(duration), easeCubic));
            IslandSpringScale.BeginAnimation(ScaleTransform.ScaleYProperty, stretchY);
        }

        // Staggered fluid cross-fade with physical slide
        if (isExpanded)
        {
            // Fade out compact view smoothly
            var fadeOutCompact = new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(90))) { EasingFunction = easeCubic };
            fadeOutCompact.Completed += (s, e) => CompactView.Visibility = Visibility.Collapsed;
            CompactView.BeginAnimation(UIElement.OpacityProperty, fadeOutCompact);

            // Cascade in expanded view with subtle slide down
            ExpandedView.Visibility = Visibility.Visible;
            ExpandedView.Opacity = 0.0;
            if (ExpandedViewTranslate != null)
            {
                var slideDown = new DoubleAnimation(-6.0, 0.0, new Duration(TimeSpan.FromMilliseconds(240)))
                {
                    BeginTime = TimeSpan.FromMilliseconds(50),
                    EasingFunction = easeCubic
                };
                ExpandedViewTranslate.BeginAnimation(TranslateTransform.YProperty, slideDown);
            }

            var fadeInExpanded = new DoubleAnimation(1.0, new Duration(TimeSpan.FromMilliseconds(240)))
            {
                BeginTime = TimeSpan.FromMilliseconds(50),
                EasingFunction = easeCubic
            };
            ExpandedView.BeginAnimation(UIElement.OpacityProperty, fadeInExpanded);
        }
        else
        {
            // Fade out expanded view smoothly over 130ms
            var fadeOutExpanded = new DoubleAnimation(0.0, new Duration(TimeSpan.FromMilliseconds(130))) { EasingFunction = easeCubic };
            fadeOutExpanded.Completed += (s, e) => ExpandedView.Visibility = Visibility.Collapsed;
            ExpandedView.BeginAnimation(UIElement.OpacityProperty, fadeOutExpanded);

            // Fade in compact view seamlessly as container finishes shrinking
            CompactView.Visibility = Visibility.Visible;
            CompactView.Opacity = 0.0;
            var fadeInCompact = new DoubleAnimation(1.0, new Duration(TimeSpan.FromMilliseconds(200)))
            {
                BeginTime = TimeSpan.FromMilliseconds(100),
                EasingFunction = easeCubic
            };
            CompactView.BeginAnimation(UIElement.OpacityProperty, fadeInCompact);
        }
    }

    private void AnimateModeSwitch()
    {
        double targetW, targetH;
        int targetRadius;
        switch (ViewModel.ExpandedMode)
        {
            case IslandExpandedMode.MusicPlayer:
                targetW = 440;
                targetH = 232;
                targetRadius = 24;
                break;
            case IslandExpandedMode.NotchNook:
                targetW = 570;
                targetH = 175;
                targetRadius = 22;
                break;
            default:
                targetW = 590;
                targetH = 500;
                targetRadius = 24;
                break;
        }

        var duration = TimeSpan.FromMilliseconds(300);
        var easeSpring = new BackEase { Amplitude = 0.05, EasingMode = EasingMode.EaseOut };
        var easeCubic = new CubicEase { EasingMode = EasingMode.EaseOut };

        var animW = new DoubleAnimation(targetW, new Duration(duration)) { EasingFunction = easeSpring };
        var animH = new DoubleAnimation(targetH, new Duration(duration)) { EasingFunction = easeSpring };

        IslandContainer.BeginAnimation(FrameworkElement.WidthProperty, animW);
        IslandContainer.BeginAnimation(FrameworkElement.HeightProperty, animH);

        bool isDocked = Top <= 2;
        IslandContainer.CornerRadius = isDocked
            ? new CornerRadius(0, 0, targetRadius, targetRadius)
            : new CornerRadius(targetRadius);

        if (ExpandedViewTranslate != null)
        {
            var modeSlide = new DoubleAnimation(-4.0, 0.0, new Duration(TimeSpan.FromMilliseconds(220)))
            {
                EasingFunction = easeCubic
            };
            ExpandedViewTranslate.BeginAnimation(TranslateTransform.YProperty, modeSlide);
        }

        var modeFade = new DoubleAnimation(0.35, 1.0, new Duration(TimeSpan.FromMilliseconds(220)))
        {
            EasingFunction = easeCubic
        };
        ExpandedView.BeginAnimation(UIElement.OpacityProperty, modeFade);
    }

    private void AnimateSatellite(bool isVisible)
    {
        if (SatelliteBubble == null || SatelliteScale == null || SatelliteTranslate == null) return;

        if (isVisible)
        {
            SatelliteBubble.Visibility = Visibility.Visible;
            double currentW = IslandContainer.ActualWidth > 0 ? IslandContainer.ActualWidth : (ViewModel.IsMediaPlaying ? 295 : 210);
            Canvas.SetLeft(SatelliteBubble, (currentW / 2.0) + 12.0);

            var spring = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
            var scaleAnim = new DoubleAnimation(0.2, 1.0, TimeSpan.FromMilliseconds(260)) { EasingFunction = spring };
            SatelliteScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            SatelliteScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            var slideAnim = new DoubleAnimation(-18.0, 0.0, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            SatelliteTranslate.BeginAnimation(TranslateTransform.XProperty, slideAnim);
        }
        else
        {
            var scaleAnim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(160));
            scaleAnim.Completed += (s, e) =>
            {
                if (!ViewModel.IsSatelliteVisible) SatelliteBubble.Visibility = Visibility.Collapsed;
            };
            SatelliteScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            SatelliteScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);
        }
    }

    private void IslandContainer_MouseEnter(object sender, MouseEventArgs e)
    {
        if (ViewModel.IsCompact)
        {
            double baseW = ViewModel.IsMediaPlaying ? 295 : 210;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var animW = new DoubleAnimation(baseW + 18, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
            var animH = new DoubleAnimation(36, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
            IslandContainer.BeginAnimation(FrameworkElement.WidthProperty, animW);
            IslandContainer.BeginAnimation(FrameworkElement.HeightProperty, animH);

            if (SatelliteBubble != null)
            {
                var animSat = new DoubleAnimation(((baseW + 18) / 2.0) + 12.0, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
                SatelliteBubble.BeginAnimation(Canvas.LeftProperty, animSat);
            }
        }
    }

    private void IslandContainer_MouseLeave(object sender, MouseEventArgs e)
    {
        if (ViewModel.IsCompact)
        {
            double baseW = ViewModel.IsMediaPlaying ? 295 : 210;
            var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
            var animW = new DoubleAnimation(baseW, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
            var animH = new DoubleAnimation(34, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
            IslandContainer.BeginAnimation(FrameworkElement.WidthProperty, animW);
            IslandContainer.BeginAnimation(FrameworkElement.HeightProperty, animH);

            if (SatelliteBubble != null)
            {
                var animSat = new DoubleAnimation((baseW / 2.0) + 12.0, TimeSpan.FromMilliseconds(220)) { EasingFunction = ease };
                SatelliteBubble.BeginAnimation(Canvas.LeftProperty, animSat);
            }
        }
    }

    // Interactive Apple Scrubber Drag & Touch Seeking
    private bool _isScrubbing;

    private void Scrubber_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.ActualWidth > 0)
        {
            _isScrubbing = true;
            elem.CaptureMouse();
            UpdateScrubberPosition(elem, e.GetPosition(elem).X);
            e.Handled = true;
        }
    }

    private void Scrubber_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isScrubbing && sender is FrameworkElement elem && elem.ActualWidth > 0)
        {
            UpdateScrubberPosition(elem, e.GetPosition(elem).X);
        }
    }

    private void Scrubber_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isScrubbing && sender is FrameworkElement elem)
        {
            _isScrubbing = false;
            elem.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Scrubber_TouchDown(object? sender, TouchEventArgs e)
    {
        if (sender is FrameworkElement elem && elem.ActualWidth > 0)
        {
            _isScrubbing = true;
            elem.CaptureTouch(e.TouchDevice);
            UpdateScrubberPosition(elem, e.GetTouchPoint(elem).Position.X);
            e.Handled = true;
        }
    }

    private void Scrubber_TouchMove(object? sender, TouchEventArgs e)
    {
        if (_isScrubbing && sender is FrameworkElement elem && elem.ActualWidth > 0)
        {
            UpdateScrubberPosition(elem, e.GetTouchPoint(elem).Position.X);
            e.Handled = true;
        }
    }

    private void Scrubber_TouchUp(object? sender, TouchEventArgs e)
    {
        if (_isScrubbing && sender is FrameworkElement elem)
        {
            _isScrubbing = false;
            elem.ReleaseTouchCapture(e.TouchDevice);
            e.Handled = true;
        }
    }

    private void UpdateScrubberPosition(FrameworkElement elem, double x)
    {
        double percent = Math.Clamp(x / elem.ActualWidth, 0.0, 1.0);
        ViewModel.SeekCommand.Execute(percent);
    }

    private void SatelliteBubble_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.OpenSatelliteTabCommand.Execute(null);
        e.Handled = true;
    }

    private void SatelliteBubble_MouseEnter(object sender, MouseEventArgs e)
    {
        var ease = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
        var anim = new DoubleAnimation(1.12, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease };
        SatelliteScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        SatelliteScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    private void SatelliteBubble_MouseLeave(object sender, MouseEventArgs e)
    {
        var ease = new BackEase { Amplitude = 0.35, EasingMode = EasingMode.EaseOut };
        var anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(160)) { EasingFunction = ease };
        SatelliteScale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
        SatelliteScale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
    }

    // Touch & Mouse Handling for Compact Pill (Tap to Expand, Drag to Reposition)
    private void CompactView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
        _isDragging = false;
    }

    private void CompactView_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Point current = e.GetPosition(this);
            Vector diff = current - _dragStartPoint;

            // Apple Swipe-Down gesture to expand HUD
            if (diff.Y > 28 && ViewModel.IsCompact && !_isDragging)
            {
                _isDragging = true;
                if (ViewModel.IsMediaPlaying)
                    ViewModel.ExpandedMode = IslandExpandedMode.MusicPlayer;
                else
                    ViewModel.ExpandedMode = IslandExpandedMode.NotchNook;
                ViewModel.IsExpanded = true;
                Activate();
                return;
            }

            if (Math.Abs(diff.X) > 6 || Math.Abs(diff.Y) > 6)
            {
                _isDragging = true;
                DragMove();
                SnapToTopEdgeIfClose();
                UpdateDockedCornerRadius();
                ViewModel.SavePosition(Left, Top);
            }
        }
    }

    private void CompactView_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging && ViewModel.IsCompact)
        {
            if (ViewModel.IsMediaPlaying)
            {
                ViewModel.ExpandedMode = IslandExpandedMode.MusicPlayer;
            }
            else
            {
                ViewModel.ExpandedMode = IslandExpandedMode.NotchNook;
            }
            ViewModel.IsExpanded = true;
            Activate();
        }
        _isDragging = false;
    }

    // Direct Touch Screen / Touch Panel Event Handlers
    private void CompactView_TouchDown(object? sender, TouchEventArgs e)
    {
        _dragStartPoint = e.GetTouchPoint(this).Position;
        _isDragging = false;
    }

    private void CompactView_TouchMove(object? sender, TouchEventArgs e)
    {
        Point current = e.GetTouchPoint(this).Position;
        Vector diff = current - _dragStartPoint;

        // Apple Touch Swipe-Down gesture to expand HUD on touchscreens
        if (diff.Y > 24 && ViewModel.IsCompact && !_isDragging)
        {
            _isDragging = true;
            if (ViewModel.IsMediaPlaying)
                ViewModel.ExpandedMode = IslandExpandedMode.MusicPlayer;
            else
                ViewModel.ExpandedMode = IslandExpandedMode.NotchNook;
            ViewModel.IsExpanded = true;
            Activate();
        }
    }

    private void CompactView_TouchUp(object? sender, TouchEventArgs e)
    {
        if (!_isDragging && ViewModel.IsCompact)
        {
            if (ViewModel.IsMediaPlaying)
            {
                ViewModel.ExpandedMode = IslandExpandedMode.MusicPlayer;
            }
            else
            {
                ViewModel.ExpandedMode = IslandExpandedMode.NotchNook;
            }
            ViewModel.IsExpanded = true;
            Activate();
        }
        _isDragging = false;
    }

    // Touch Handling for Expanded View (Swipe-Up to Collapse)
    private Point _expandedTouchStart;
    private bool _isExpandedSwiping;

    private void ExpandedView_TouchDown(object? sender, TouchEventArgs e)
    {
        _expandedTouchStart = e.GetTouchPoint(this).Position;
        _isExpandedSwiping = false;
    }

    private void ExpandedView_TouchMove(object? sender, TouchEventArgs e)
    {
        Point current = e.GetTouchPoint(this).Position;
        Vector diff = current - _expandedTouchStart;

        // Apple Touch Swipe-Up gesture to collapse HUD back to pill
        if (diff.Y < -24 && ViewModel.IsExpanded && !_isExpandedSwiping)
        {
            _isExpandedSwiping = true;
            ViewModel.IsExpanded = false;
            e.Handled = true;
        }
    }

    private void ExpandedView_TouchUp(object? sender, TouchEventArgs e)
    {
        _isExpandedSwiping = false;
    }

    // Fluid Mouse Wheel Gesture: Scroll down to expand, Scroll up to collapse
    private void IslandContainer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta < -60 && ViewModel.IsCompact)
        {
            if (ViewModel.IsMediaPlaying)
                ViewModel.ExpandedMode = IslandExpandedMode.MusicPlayer;
            else
                ViewModel.ExpandedMode = IslandExpandedMode.NotchNook;
            ViewModel.IsExpanded = true;
            e.Handled = true;
        }
        else if (e.Delta > 60 && ViewModel.IsExpanded)
        {
            ViewModel.IsExpanded = false;
            e.Handled = true;
        }
    }

    // Header Bar drag handle in Expanded View
    private void HeaderBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            SnapToTopEdgeIfClose();
            UpdateDockedCornerRadius();
            ViewModel.SavePosition(Left, Top);
        }
    }

    private void SnapToTopEdgeIfClose()
    {
        var workArea = SystemParameters.WorkArea;
        if (Top <= workArea.Top + 15)
        {
            Top = workArea.Top;
        }
    }

    private void UpdateDockedCornerRadius()
    {
        double currentRadius;
        if (ViewModel.IsExpanded)
        {
            currentRadius = ViewModel.ExpandedMode == IslandExpandedMode.ControlCenter ? 24 : 22;
        }
        else
        {
            currentRadius = 17;
        }

        bool isDocked = Top <= 2;
        IslandContainer.CornerRadius = isDocked
            ? new CornerRadius(0, 0, currentRadius, currentRadius)
            : new CornerRadius(currentRadius);
    }

    public void ToggleIsland()
    {
        Dispatcher.Invoke(() =>
        {
            ViewModel.IsExpanded = !ViewModel.IsExpanded;
            if (ViewModel.IsExpanded)
            {
                Activate();
            }
        });
    }

    private void MainWindow_Deactivated(object? sender, EventArgs e)
    {
        if (ViewModel.IsExpanded && ViewModel.AutoCollapse)
        {
            ViewModel.IsExpanded = false;
        }
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        try { System.IO.File.AppendAllText(@"E:\NexusIsland\app_lifecycle.log", $"[{DateTime.Now}] MainWindow_Closing\n"); } catch { }
        var hwnd = new WindowInteropHelper(this).Handle;
        try { NativeMethods.RemoveClipboardFormatListener(hwnd); } catch { }
        _hwndSource?.RemoveHook(WndProc);
        _hotkeyService.Dispose();
        _trayService.Dispose();
        ViewModel.SavePosition(Left, Top);
    }
}
