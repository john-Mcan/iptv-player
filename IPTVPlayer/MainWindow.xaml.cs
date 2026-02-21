using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using IPTVPlayer.Models;
using IPTVPlayer.ViewModels;
using IPTVPlayer.Views;

namespace IPTVPlayer;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private PiPWindow? _pipWindow;
    private bool _isFullscreen;
    private WindowState _prevWindowState;

    private readonly DispatcherTimer _hideControlsTimer;
    private bool _fsControlsVisible;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideControlsTimer.Tick += (_, _) =>
        {
            _hideControlsTimer.Stop();
            HideFsControls();
        };
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;

        SeekSlider.AddHandler(Thumb.DragStartedEvent,
            new DragStartedEventHandler((_, _) => _vm.Player.BeginSeek()));
        SeekSlider.AddHandler(Thumb.DragCompletedEvent,
            new DragCompletedEventHandler((_, _) => _vm.Player.EndSeek(SeekSlider.Value)));

        FsSeekSlider.AddHandler(Thumb.DragStartedEvent,
            new DragStartedEventHandler((_, _) => _vm.Player.BeginSeek()));
        FsSeekSlider.AddHandler(Thumb.DragCompletedEvent,
            new DragCompletedEventHandler((_, _) => _vm.Player.EndSeek(FsSeekSlider.Value)));
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _hideControlsTimer.Stop();
        _pipWindow?.Close();
        VideoView.MediaPlayer = null;
        _vm.Player.Dispose();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.F11:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Escape when _isFullscreen:
                ToggleFullscreen();
                e.Handled = true;
                break;
            case Key.Space:
                _vm.Player.TogglePlayPauseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }

    private void UrlBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            _vm.LoadPlaylistCommand.Execute(null);
    }

    private void ChannelTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ChannelTree.SelectedItem is Channel channel)
            _vm.PlayChannel(channel);
    }

    private void CategoryTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton btn && btn.Tag is ContentCategory category)
        {
            _vm.SelectedCategory = category;
        }
    }

    #region PiP

    private void PiP_Click(object sender, RoutedEventArgs e)
    {
        if (_pipWindow is not null)
        {
            ClosePiP();
            return;
        }

        if (!_vm.Player.HasMedia || string.IsNullOrEmpty(_vm.Player.CurrentUrl))
            return;

        OpenPiP();
    }

    private void OpenPiP()
    {
        var currentUrl = _vm.Player.CurrentUrl;
        var currentTimeMs = _vm.Player.VlcMediaPlayer.Time;
        var currentVolume = _vm.Player.Volume;

        _vm.Player.VlcMediaPlayer.Pause();
        VideoView.MediaPlayer = null;

        _pipWindow = new PiPWindow(currentUrl, currentTimeMs, currentVolume);

        _pipWindow.PiPClosedWithTime += (pipTimeMs) =>
        {
            VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;

            if (pipTimeMs > 0 && _vm.Player.VlcMediaPlayer.Media != null)
                _vm.Player.VlcMediaPlayer.Time = pipTimeMs;

            _vm.Player.VlcMediaPlayer.Play();
            _pipWindow = null;
        };

        _pipWindow.Show();
    }

    private void ClosePiP()
    {
        if (_pipWindow is not null && _pipWindow.IsLoaded)
        {
            try { _pipWindow.Close(); } catch { }
        }
    }

    #endregion

    #region Fullscreen

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
            ExitFullscreen();
        else
            EnterFullscreen();
    }

    private void EnterFullscreen()
    {
        _prevWindowState = WindowState;

        TopBar.Visibility = Visibility.Collapsed;
        Sidebar.Visibility = Visibility.Collapsed;
        SidebarSplitter.Visibility = Visibility.Collapsed;
        SidebarColumn.Width = new GridLength(0);
        ControlsBar.Visibility = Visibility.Collapsed;
        StatusBar.Visibility = Visibility.Collapsed;

        WindowStyle = WindowStyle.None;
        WindowState = WindowState.Maximized;

        FsIconExpand.Visibility = Visibility.Collapsed;
        FsIconRestore.Visibility = Visibility.Visible;
        _isFullscreen = true;

        FullscreenOverlay.Visibility = Visibility.Visible;
        ShowFsControls();
    }

    private void ExitFullscreen()
    {
        if (!_isFullscreen) return;

        _hideControlsTimer.Stop();
        FullscreenOverlay.BeginAnimation(OpacityProperty, null);
        FullscreenOverlay.Opacity = 0;
        FullscreenOverlay.Visibility = Visibility.Collapsed;
        VideoOverlay.Cursor = null;
        _fsControlsVisible = false;

        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = _prevWindowState;

        TopBar.Visibility = Visibility.Visible;
        Sidebar.Visibility = Visibility.Visible;
        SidebarSplitter.Visibility = Visibility.Visible;
        SidebarColumn.Width = new GridLength(280);
        ControlsBar.Visibility = Visibility.Visible;
        StatusBar.Visibility = Visibility.Visible;

        FsIconExpand.Visibility = Visibility.Visible;
        FsIconRestore.Visibility = Visibility.Collapsed;
        _isFullscreen = false;
    }

    #endregion

    #region Fullscreen auto-hide controls

    private void VideoOverlay_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isFullscreen)
            ShowFsControls();
    }

    private void VideoOverlay_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isFullscreen) return;

        if (e.ClickCount == 2)
        {
            ToggleFullscreen();
            return;
        }
        ShowFsControls();
    }

    private void ShowFsControls()
    {
        _hideControlsTimer.Stop();
        _hideControlsTimer.Start();

        if (_fsControlsVisible) return;
        _fsControlsVisible = true;

        FullscreenOverlay.BeginAnimation(OpacityProperty, null);
        FullscreenOverlay.Opacity = 1.0;
        VideoOverlay.Cursor = null;
    }

    private void HideFsControls()
    {
        if (!_fsControlsVisible) return;
        _fsControlsVisible = false;

        var anim = new DoubleAnimation(0.0, TimeSpan.FromMilliseconds(400));
        anim.FillBehavior = FillBehavior.Stop;
        anim.Completed += (_, _) =>
        {
            if (!_fsControlsVisible)
                FullscreenOverlay.Opacity = 0.0;
        };
        FullscreenOverlay.BeginAnimation(OpacityProperty, anim);

        VideoOverlay.Cursor = Cursors.None;
    }

    #endregion
}
