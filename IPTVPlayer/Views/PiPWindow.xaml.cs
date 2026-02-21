using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using LibVLCSharp.Shared;

namespace IPTVPlayer.Views;

public partial class PiPWindow : Window, IDisposable
{
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private bool _isDisposed;
    private bool _isMuted;

    private Point _resizeStart;
    private double _resizeStartWidth, _resizeStartHeight;

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

        PipVolumeSlider.Value = volume;

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

    #region Drag & Close

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

    #endregion

    #region Resize

    private void ResizeGrip_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var grip = (UIElement)sender;
        _resizeStart = grip.PointToScreen(e.GetPosition(grip));
        _resizeStartWidth = ActualWidth;
        _resizeStartHeight = ActualHeight;
        grip.CaptureMouse();
        e.Handled = true;
    }

    private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
    {
        var grip = (UIElement)sender;
        if (!grip.IsMouseCaptured) return;

        var current = grip.PointToScreen(e.GetPosition(grip));
        Width = Math.Max(MinWidth, _resizeStartWidth + (current.X - _resizeStart.X));
        Height = Math.Max(MinHeight, _resizeStartHeight + (current.Y - _resizeStart.Y));
    }

    private void ResizeGrip_MouseUp(object sender, MouseButtonEventArgs e)
    {
        ((UIElement)sender).ReleaseMouseCapture();
    }

    #endregion

    #region Volume

    private void Overlay_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        int newVolume = Math.Clamp(_mediaPlayer.Volume + (e.Delta > 0 ? 5 : -5), 0, 100);
        PipVolumeSlider.Value = newVolume;
    }

    private void MuteBtn_Click(object sender, RoutedEventArgs e)
    {
        _isMuted = !_isMuted;
        _mediaPlayer.Mute = _isMuted;

        var icon = (TextBlock)MuteBtn.Template.FindName("MuteIcon", MuteBtn);
        if (icon != null)
            icon.Text = _isMuted ? "\uE74F" : "\uE767";
    }

    private void PipVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_mediaPlayer != null && !_isDisposed)
            _mediaPlayer.Volume = (int)e.NewValue;
    }

    #endregion

    #region Hover show/hide controls

    private void Overlay_MouseEnter(object sender, MouseEventArgs e)
    {
        var fadeIn = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(150));
        CloseBtn.BeginAnimation(OpacityProperty, fadeIn);
        ControlsPanel.BeginAnimation(OpacityProperty, fadeIn);
    }

    private void Overlay_MouseLeave(object sender, MouseEventArgs e)
    {
        var fadeOut = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(300));
        CloseBtn.BeginAnimation(OpacityProperty, fadeOut);
        ControlsPanel.BeginAnimation(OpacityProperty, fadeOut);
    }

    #endregion
}
