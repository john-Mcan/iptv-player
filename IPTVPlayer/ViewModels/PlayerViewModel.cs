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

    public MediaPlayer VlcMediaPlayer => _mediaPlayer;

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
            // Re-apply audio settings when playback starts to avoid silent playback
            _mediaPlayer.Volume = Volume;
            _mediaPlayer.Mute = IsMuted;
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
            HasMedia = false;
            Position = 0;
            CurrentTime = TimeSpan.Zero;
            Duration = TimeSpan.Zero;
            AudioTracks.Clear();
            SubtitleTracks.Clear();
            StatusText = "Detenido";
        });

        _mediaPlayer.PositionChanged += (_, e) => Dispatch(() =>
        {
            if (!_isSeeking)
                Position = e.Position * 100.0;
            CurrentTime = TimeSpan.FromMilliseconds(_mediaPlayer.Time);
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
            StatusText = "Error al reproducir el stream";
            IsPlaying = false;
            HasMedia = false;
        });
    }

    private void RefreshTracks()
    {
        // --- Audio Tracks ---
        var currentAudioId = _mediaPlayer.AudioTrack;
        AudioTracks.Clear();
        foreach (var t in _mediaPlayer.AudioTrackDescription ?? [])
            AudioTracks.Add(new TrackInfo { Id = t.Id, Name = t.Name ?? $"Pista {t.Id}" });

        // Select current audio track; if it's "Disable" (-1), pick the first real track
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

        // --- Subtitle Tracks ---
        var currentSpuId = _mediaPlayer.Spu;
        SubtitleTracks.Clear();
        foreach (var t in _mediaPlayer.SpuDescription ?? [])
            SubtitleTracks.Add(new TrackInfo { Id = t.Id, Name = t.Name ?? $"Subtítulo {t.Id}" });
        SelectedSubtitleTrack = SubtitleTracks.FirstOrDefault(t => t.Id == currentSpuId);
    }

    public void PlayChannel(Channel channel)
    {
        CurrentChannelName = channel.Name;
        CurrentUrl = channel.Url;
        StatusText = $"Cargando: {channel.Name}...";

        using var media = new Media(_libVLC, channel.Url, FromType.FromLocation);
        media.AddOption(":network-caching=1000");
        _mediaPlayer.Play(media);
    }

    public void ResumeFromPiP(long pipTimeMs)
    {
        StatusText = $"Cargando: {CurrentChannelName}...";

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
        _mediaPlayer.Stop();
    }

    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        _mediaPlayer.Mute = IsMuted;
    }

    partial void OnVolumeChanged(int value)
    {
        _mediaPlayer.Volume = value;
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
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
        GC.SuppressFinalize(this);
    }
}
