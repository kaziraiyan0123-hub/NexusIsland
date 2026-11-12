using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace NexusIsland.Services;

public class MediaInfo
{
    public bool IsPlaying { get; set; }
    public string Title { get; set; } = "No Media Playing";
    public string Artist { get; set; } = "Windows Audio Session";
    public string AlbumTitle { get; set; } = string.Empty;
    public BitmapImage? Thumbnail { get; set; }
    public TimeSpan Position { get; set; }
    public TimeSpan Duration { get; set; }
}

public class MediaService
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;

    public event Action<MediaInfo>? MediaInfoChanged;
    public MediaInfo CurrentMedia { get; private set; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                UpdateCurrentSession();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MediaService init error: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession();
    }

    private void UpdateCurrentSession()
    {
        try
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
            }

            _currentSession = _manager?.GetCurrentSession();

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }

            _ = RefreshMediaInfoAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateCurrentSession error: {ex.Message}");
        }
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        _ = RefreshMediaInfoAsync();
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshMediaInfoAsync();
    }

    public async Task RefreshMediaInfoAsync()
    {
        try
        {
            if (_currentSession == null)
            {
                CurrentMedia = new MediaInfo();
                MediaInfoChanged?.Invoke(CurrentMedia);
                return;
            }

            var playbackInfo = _currentSession.GetPlaybackInfo();
            var isPlaying = playbackInfo?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            var properties = await _currentSession.TryGetMediaPropertiesAsync();
            var timeline = _currentSession.GetTimelineProperties();

            var newInfo = new MediaInfo
            {
                IsPlaying = isPlaying,
                Title = string.IsNullOrWhiteSpace(properties?.Title) ? "Active Audio" : properties.Title,
                Artist = string.IsNullOrWhiteSpace(properties?.Artist) ? "Windows Media" : properties.Artist,
                AlbumTitle = properties?.AlbumTitle ?? string.Empty,
                Position = timeline?.Position ?? TimeSpan.Zero,
                Duration = timeline?.EndTime ?? TimeSpan.FromMinutes(3)
            };

            if (properties?.Thumbnail != null)
            {
                try
                {
                    using var stream = await properties.Thumbnail.OpenReadAsync();
                    using var netStream = stream.AsStreamForRead();
                    var mem = new MemoryStream();
                    await netStream.CopyToAsync(mem);
                    mem.Position = 0;

                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = mem;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    newInfo.Thumbnail = bitmap;
                }
                catch
                {
                    newInfo.Thumbnail = null;
                }
            }

            CurrentMedia = newInfo;
            MediaInfoChanged?.Invoke(CurrentMedia);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshMediaInfoAsync error: {ex.Message}");
        }
    }

    public async Task TogglePlayPauseAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
                await Task.Delay(150);
                await RefreshMediaInfoAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"TogglePlayPauseAsync error: {ex.Message}");
            }
        }
    }

    public async Task SkipNextAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
                await Task.Delay(200);
                await RefreshMediaInfoAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SkipNextAsync error: {ex.Message}");
            }
        }
    }

    public async Task SkipPreviousAsync()
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
                await Task.Delay(200);
                await RefreshMediaInfoAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SkipPreviousAsync error: {ex.Message}");
            }
        }
    }

    public async Task SeekToPositionAsync(TimeSpan position)
    {
        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryChangePlaybackPositionAsync(position.Ticks);
                CurrentMedia.Position = position;
                MediaInfoChanged?.Invoke(CurrentMedia);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SeekToPositionAsync error: {ex.Message}");
            }
        }
    }
}
