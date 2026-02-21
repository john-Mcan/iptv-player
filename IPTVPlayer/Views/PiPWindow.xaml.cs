using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LibVLCSharp.Shared;

namespace IPTVPlayer.Views;

public partial class PiPWindow : Window, IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private bool _isDisposed;

    public event Action<long>? PiPClosedWithTime;

    public PiPWindow(string url, long startTimeMs, int volume)
    {
        InitializeComponent();

        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 20;
        Top = workArea.Bottom - Height - 20;

        _libVLC = new LibVLC(
            "--no-video-title-show",
            "--aout=mmdevice",
            $"--mmdevice-volume={(volume / 100f).ToString(System.Globalization.CultureInfo.InvariantCulture)}"
        );
        _mediaPlayer = new MediaPlayer(_libVLC)
        {
            EnableHardwareDecoding = true,
            Volume = volume
        };

        Loaded += (_, _) =>
        {
            PipVideoView.MediaPlayer = _mediaPlayer;

            using var media = new Media(_libVLC, url, FromType.FromLocation);
            if (startTimeMs > 0)
                media.AddOption($":start-time={startTimeMs / 1000}");
            _mediaPlayer.Play(media);
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!_isDisposed)
        {
            PiPClosedWithTime?.Invoke(_mediaPlayer.Time);
            Dispose();
        }
        base.OnClosed(e);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        PipVideoView.MediaPlayer = null;
        _mediaPlayer.Stop();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
    }

    private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            try { Close(); } catch { }
            return;
        }
        try { DragMove(); }
        catch (InvalidOperationException) { }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        int newVolume = _mediaPlayer.Volume + (e.Delta > 0 ? 5 : -5);
        _mediaPlayer.Volume = Math.Clamp(newVolume, 0, 100);
    }

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
        var anim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
        CloseBtn.BeginAnimation(OpacityProperty, anim);
    }

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
        var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
        CloseBtn.BeginAnimation(OpacityProperty, anim);
    }
}
