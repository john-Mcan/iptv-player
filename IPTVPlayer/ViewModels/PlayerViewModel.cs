using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTVPlayer.Models;
using LibVLCSharp.Shared;

namespace IPTVPlayer.ViewModels;

public partial class PlayerViewModel : ObservableObject, IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private bool _isSeeking;
    private bool _userRequestedStop;
    private bool _isPiPTransition;
    private int _pendingAudioTrackId = -1;
    private int _reconnectAttempts;
    private CancellationTokenSource? _reconnectCts;
    private long _lastKnownTimeMs;

    private const int MaxReconnectAttempts = 10;
    private const int MaxBackoffMs = 30_000;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private bool _isLive;

    [ObservableProperty]
    private double _position;

    [ObservableProperty]
    private int _volume = 80;

    [ObservableProperty]
    private bool _isMuted;

    [ObservableProperty]
    private TimeSpan _currentTime;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private string _currentChannelName = string.Empty;

    [ObservableProperty]
    private string _currentUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _audioTracks = new();

    [ObservableProperty]
    private ObservableCollection<TrackInfo> _subtitleTracks = new();

    [ObservableProperty]
    private TrackInfo? _selectedAudioTrack;

    [ObservableProperty]
    private TrackInfo? _selectedSubtitleTrack;

    [ObservableProperty]
    private string _statusText = "Listo";

    [ObservableProperty]
    private bool _hasMedia;

    [ObservableProperty]
    private bool _isReconnecting;

    [ObservableProperty]
    private string _reconnectStatus = string.Empty;

    public MediaPlayer VlcMediaPlayer => _mediaPlayer;

    public Action<long, long>? OnPositionUpdated { get; set; }

    public PlayerViewModel()
    {
        Core.Initialize();
        _libVLC = new LibVLC(
            "--no-video-title-show",
            "--aout=mmdevice",
            "--mmdevice-volume=1.0"
        );
        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            Volume = _volume,
            EnableHardwareDecoding = true
        };
        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _mediaPlayer.Playing += (_, _) => Dispatch(() =>
        {
            IsPlaying = true;
            HasMedia = true;
            _mediaPlayer.Volume = Volume;
            _mediaPlayer.Mute = IsMuted;

            if (IsReconnecting)
            {
                IsReconnecting = false;
                ReconnectStatus = string.Empty;
                _reconnectAttempts = 0;
            }

            StatusText = $"Reproduciendo: {CurrentChannelName}";
        });

        _mediaPlayer.Paused += (_, _) => Dispatch(() =>
        {
            IsPlaying = false;
            StatusText = "Pausado";
        });

        _mediaPlayer.Stopped += (_, _) => Dispatch(() =>
        {
            IsPlaying = false;
            if (!IsReconnecting && !_isPiPTransition)
            {
                HasMedia = false;
                Position = 0;
                CurrentTime = TimeSpan.Zero;
                Duration = TimeSpan.Zero;
                AudioTracks.Clear();
                SubtitleTracks.Clear();
            }
            if (_userRequestedStop)
                StatusText = "Detenido";
        });

        _mediaPlayer.PositionChanged += (_, e) => Dispatch(() =>
        {
            if (!_isSeeking)
                Position = e.Position * 100.0;
            _lastKnownTimeMs = _mediaPlayer.Time;
            CurrentTime = TimeSpan.FromMilliseconds(_lastKnownTimeMs);
            OnPositionUpdated?.Invoke(_lastKnownTimeMs, (long)Duration.TotalMilliseconds);
        });

        _mediaPlayer.LengthChanged += (_, e) => Dispatch(() =>
        {
            Duration = TimeSpan.FromMilliseconds(e.Length);
            IsLive = e.Length <= 0;
        });

        _mediaPlayer.ESAdded += (_, _) => Dispatch(RefreshTracks);
        _mediaPlayer.ESDeleted += (_, _) => Dispatch(RefreshTracks);

        _mediaPlayer.EncounteredError += (_, _) => Dispatch(() =>
        {
            IsPlaying = false;
            if (!_userRequestedStop && !string.IsNullOrEmpty(CurrentUrl))
                StartReconnect();
            else
            {
                StatusText = "Error al reproducir el stream";
                HasMedia = false;
            }
        });

        _mediaPlayer.EndReached += (_, _) => Dispatch(() =>
        {
            if (IsLive && !_userRequestedStop && !string.IsNullOrEmpty(CurrentUrl))
                StartReconnect();
            else
            {
                IsPlaying = false;
                HasMedia = false;
                StatusText = "Reproducción finalizada";
            }
        });
    }

    private void StartReconnect()
    {
        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            IsReconnecting = false;
            ReconnectStatus = string.Empty;
            HasMedia = false;
            StatusText = $"No se pudo reconectar después de {MaxReconnectAttempts} intentos";
            return;
        }

        _reconnectAttempts++;
        IsReconnecting = true;
        var delayMs = Math.Min(1000 * (int)Math.Pow(2, _reconnectAttempts - 1), MaxBackoffMs);
        ReconnectStatus = $"Reconectando ({_reconnectAttempts}/{MaxReconnectAttempts})...";
        StatusText = ReconnectStatus;

        _reconnectCts?.Cancel();
        _reconnectCts = new CancellationTokenSource();
        var token = _reconnectCts.Token;
        var url = CurrentUrl;
        var isLive = IsLive;
        var resumeTimeMs = _lastKnownTimeMs;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;

                using var media = new Media(_libVLC, url, FromType.FromLocation);
                media.AddOption(":network-caching=1000");
                if (!isLive && resumeTimeMs > 0)
                    media.AddOption($":start-time={resumeTimeMs / 1000}");
                _mediaPlayer.Play(media);
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [RelayCommand]
    private void CancelReconnect()
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _reconnectAttempts = 0;
        IsReconnecting = false;
        ReconnectStatus = string.Empty;
        HasMedia = false;
        StatusText = "Reconexión cancelada";
    }

    private void RefreshTracks()
    {
        var currentAudioId = _mediaPlayer.AudioTrack;
        AudioTracks.Clear();
        foreach (var t in _mediaPlayer.AudioTrackDescription ?? [])
            AudioTracks.Add(new TrackInfo { Id = t.Id, Name = t.Name ?? $"Pista {t.Id}" });

        if (_pendingAudioTrackId > 0 && AudioTracks.Any(t => t.Id == _pendingAudioTrackId))
        {
            var track = AudioTracks.First(t => t.Id == _pendingAudioTrackId);
            SelectedAudioTrack = track;
            _mediaPlayer.SetAudioTrack(track.Id);
            _pendingAudioTrackId = -1;
        }
        else
        {
            SelectedAudioTrack = AudioTracks.FirstOrDefault(t => t.Id == currentAudioId);
            if ((SelectedAudioTrack is null || SelectedAudioTrack.Id == -1) && AudioTracks.Count > 1)
            {
                var firstReal = AudioTracks.FirstOrDefault(t => t.Id > 0);
                if (firstReal is not null)
                {
                    SelectedAudioTrack = firstReal;
                    _mediaPlayer.SetAudioTrack(firstReal.Id);
                }
            }
        }

        var currentSpuId = _mediaPlayer.Spu;
        SubtitleTracks.Clear();
        foreach (var t in _mediaPlayer.SpuDescription ?? [])
            SubtitleTracks.Add(new TrackInfo { Id = t.Id, Name = t.Name ?? $"Subtítulo {t.Id}" });
        SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault(t => t.Id == currentSpuId);
    }

    public void PlayChannel(Channel channel)
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _reconnectAttempts = 0;
        _pendingAudioTrackId = -1;
        IsReconnecting = false;
        ReconnectStatus = string.Empty;
        _userRequestedStop = false;
        _lastKnownTimeMs = 0;

        CurrentChannelName = channel.Name;
        CurrentUrl = channel.Url;
        StatusText = $"Cargando: {channel.Name}...";

        using var media = new Media(_libVLC, channel.Url, FromType.FromLocation);
        media.AddOption(":network-caching=1000");
        _mediaPlayer.Play(media);
    }

    public void PlayChannelFromPosition(Channel channel, long startPositionMs)
    {
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _reconnectAttempts = 0;
        _pendingAudioTrackId = -1;
        IsReconnecting = false;
        ReconnectStatus = string.Empty;
        _userRequestedStop = false;
        _lastKnownTimeMs = startPositionMs;

        CurrentChannelName = channel.Name;
        CurrentUrl = channel.Url;
        StatusText = $"Cargando: {channel.Name}...";

        using var media = new Media(_libVLC, channel.Url, FromType.FromLocation);
        media.AddOption(":network-caching=1000");
        if (startPositionMs > 0)
            media.AddOption($":start-time={startPositionMs / 1000}");
        _mediaPlayer.Play(media);
    }

    public long GetCurrentTimeMs() => _lastKnownTimeMs;
    public long GetDurationMs() => (long)Duration.TotalMilliseconds;

    public void PrepareForPiP()
    {
        _isPiPTransition = true;
    }

    public async Task ResumeFromPiPAsync(long pipTimeMs, int audioTrackId = -1)
    {
        _pendingAudioTrackId = audioTrackId;
        StatusText = $"Cargando: {CurrentChannelName}...";

        await Task.Run(() => { try { _mediaPlayer.Stop(); } catch { } });

        _isPiPTransition = false;
        _userRequestedStop = false;
        _lastKnownTimeMs = 0;

        using var media = new Media(_libVLC, CurrentUrl, FromType.FromLocation);
        media.AddOption(":network-caching=1000");

        if (!IsLive && pipTimeMs > 0)
            media.AddOption($":start-time={pipTimeMs / 1000}");

        _mediaPlayer.Play(media);
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        if (!HasMedia) return;
        if (_mediaPlayer.IsPlaying)
            _mediaPlayer.Pause();
        else
            _mediaPlayer.Play();
    }

    [RelayCommand]
    private void Stop()
    {
        _userRequestedStop = true;
        _reconnectCts?.Cancel();
        _reconnectCts = null;
        _reconnectAttempts = 0;
        IsReconnecting = false;
        ReconnectStatus = string.Empty;
        _mediaPlayer.Stop();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
    }

    partial void OnVolumeChanged(int value)
    {
        _mediaPlayer.Volume = value;
    }

    partial void OnIsMutedChanged(bool value)
    {
        _mediaPlayer.Mute = value;
    }

    partial void OnSelectedAudioTrackChanged(TrackInfo? value)
    {
        if (value is not null)
            _mediaPlayer.SetAudioTrack(value.Id);
    }

    partial void OnSelectedSubtitleTrackChanged(TrackInfo? value)
    {
        if (value is not null)
            _mediaPlayer.SetSpu(value.Id);
    }

    public void BeginSeek() => _isSeeking = true;

    public void EndSeek(double positionPercent)
    {
        _mediaPlayer.Position = (float)(positionPercent / 100.0);
        _isSeeking = false;
    }

    private static void Dispatch(Action action)
    {
        Application.Current?.Dispatcher?.BeginInvoke(action);
    }

    public void Dispose()
    {
        _reconnectCts?.Cancel();
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
        GC.SuppressFinalize(this);
    }
}
