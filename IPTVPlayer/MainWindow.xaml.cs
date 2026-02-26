using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using IPTVPlayer.Models;
using IPTVPlayer.Services;
using IPTVPlayer.ViewModels;
using IPTVPlayer.Views;

namespace IPTVPlayer;

public partial class MainWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private readonly MainViewModel _vm;
    private readonly AppSettings _settings;
    private PiPWindow? _pipWindow;
    private bool _isFullscreen;
    private WindowState _prevWindowState;
    private ResizeMode _prevResizeMode;

    private readonly DispatcherTimer _hideControlsTimer;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _epgRefreshTimer;
    private bool _fsControlsVisible;

    public MainWindow()
    {
        InitializeComponent();

        _settings = SettingsService.Load();
        _vm = new MainViewModel();
        _vm.LoadSettings(_settings);
        DataContext = _vm;

        if (!double.IsNaN(_settings.WindowLeft) && !double.IsNaN(_settings.WindowTop))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = _settings.WindowLeft;
            Top = _settings.WindowTop;
        }
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        SidebarColumn.Width = new GridLength(_settings.SidebarWidth);

        _hideControlsTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _hideControlsTimer.Tick += (_, _) =>
        {
            _hideControlsTimer.Stop();
            HideFsControls();
        };

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        _epgRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _epgRefreshTimer.Tick += (_, _) => _vm.RefreshEpgDisplay();
        _epgRefreshTimer.Start();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        EnableDarkTitleBar();

        VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;

        SeekSlider.AddHandler(Thumb.DragStartedEvent,
            new DragStartedEventHandler((_, _) => _vm.Player.BeginSeek()));
        SeekSlider.AddHandler(Thumb.DragCompletedEvent,
            new DragCompletedEventHandler((_, _) => _vm.Player.EndSeek(SeekSlider.Value)));

        FsSeekSlider.AddHandler(Thumb.DragStartedEvent,
            new DragStartedEventHandler((_, _) => _vm.Player.BeginSeek()));
        FsSeekSlider.AddHandler(Thumb.DragCompletedEvent,
            new DragCompletedEventHandler((_, _) => _vm.Player.EndSeek(FsSeekSlider.Value)));

        SyncTabUI();

        if (_settings.AutoLoadPlaylist && !string.IsNullOrWhiteSpace(_vm.PlaylistUrl))
            _vm.LoadPlaylistCommand.Execute(null);
    }

    private void EnableDarkTitleBar()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            int value = 1;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        }
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm  —  ddd dd MMM");
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        _vm.SaveToSettings(_settings);
        _settings.WindowWidth = Width;
        _settings.WindowHeight = Height;
        _settings.WindowLeft = Left;
        _settings.WindowTop = Top;
        _settings.SidebarWidth = SidebarColumn.ActualWidth;
        SettingsService.Save(_settings);

        _hideControlsTimer.Stop();
        _clockTimer.Stop();
        _epgRefreshTimer.Stop();
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
        {
            UrlBox.IsDropDownOpen = false;
            _vm.LoadPlaylistCommand.Execute(null);
        }
    }

    #region Settings

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsWindow(
            _settings.AutoLoadPlaylist,
            _settings.MaxReconnectAttempts,
            _settings.HideAdultContent)
        { Owner = this };

        var saved = dlg.ShowDialog() == true;
        var changed = false;

        if (saved)
        {
            _settings.AutoLoadPlaylist = dlg.SettingsAutoLoadPlaylist;
            _settings.MaxReconnectAttempts = dlg.SettingsMaxReconnectAttempts;
            _vm.Player.MaxReconnectAttempts = dlg.SettingsMaxReconnectAttempts;

            if (_settings.HideAdultContent != dlg.SettingsHideAdultContent)
            {
                _settings.HideAdultContent = dlg.SettingsHideAdultContent;
                _vm.HideAdultContent = dlg.SettingsHideAdultContent;
            }
            
            changed = true;
        }

        if (dlg.HistoryCleared)
        {
            _vm.ClearWatchHistory();
            changed = true;
        }

        if (dlg.FavoritesCleared)
        {
            _vm.ClearFavorites();
            changed = true;
        }

        if (changed)
        {
            _vm.SaveToSettings(_settings);
            SettingsService.Save(_settings);
        }
    }

    #endregion

    #region Content Tabs

    private void ContentTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tabStr
            && Enum.TryParse<ContentTab>(tabStr, out var tab))
        {
            _vm.ActiveTab = tab;
        }
    }

    private void SyncTabUI()
    {
        LiveTvTab.IsChecked = _vm.ActiveTab == ContentTab.LiveTV;
        MoviesTab.IsChecked = _vm.ActiveTab == ContentTab.Movies;
        SeriesTab.IsChecked = _vm.ActiveTab == ContentTab.Series;
    }

    #endregion

    #region Channel / Content Selection

    private void ChannelTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Channel channel)
            _vm.PlayChannel(channel);
    }

    private void ContentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is Channel channel)
        {
            _vm.PlayChannel(channel);
            lb.SelectedItem = null; // allow re-selecting same item
        }
    }

    private void SeriesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox lb && lb.SelectedItem is Channel channel)
        {
            _vm.HandleSeriesItemClick(channel);
            lb.SelectedItem = null; // allow re-selecting same item
        }
    }

    private void FavoriteItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Channel channel)
            _vm.PlayChannel(channel);
    }

    private void SeriesFavoriteItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is Channel channel)
            _vm.NavigateToSeriesShow(channel.Name);
    }

    private void HistoryItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WatchHistoryEntry entry)
            _vm.PlayFromHistory(entry);
    }

    private void ContinueWatchingItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is WatchHistoryEntry entry)
            _vm.PlayFromHistory(entry);
    }

    #endregion

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
        var audioTrackId = _vm.Player.VlcMediaPlayer.AudioTrack;

        _vm.Player.VlcMediaPlayer.Pause();
        _vm.Player.PrepareForPiP();
        VideoView.MediaPlayer = null;

        _pipWindow = new PiPWindow(currentUrl, currentTimeMs, currentVolume, audioTrackId);

        _pipWindow.PiPClosedWithTime += async (pipTimeMs, pipAudioTrackId) =>
        {
            _pipWindow = null;
            VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;
            await _vm.Player.ResumeFromPiPAsync(pipTimeMs, pipAudioTrackId);
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
        _prevResizeMode = ResizeMode;

        HeaderBar.Visibility = Visibility.Collapsed;
        LeftSidebar.Visibility = Visibility.Collapsed;
        SidebarSplitter.Visibility = Visibility.Collapsed;
        SidebarColumn.Width = new GridLength(0);
        HorizontalSplitter.Visibility = Visibility.Collapsed;
        BelowVideoRow.Height = new GridLength(0);
        BelowVideoRow.MinHeight = 0;
        ControlsBar.Visibility = Visibility.Collapsed;
        StatusBar.Visibility = Visibility.Collapsed;

        if (WindowState == WindowState.Maximized)
            WindowState = WindowState.Normal;

        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
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

        ResizeMode = _prevResizeMode;
        WindowStyle = WindowStyle.SingleBorderWindow;
        WindowState = _prevWindowState;

        HeaderBar.Visibility = Visibility.Visible;
        LeftSidebar.Visibility = Visibility.Visible;
        SidebarSplitter.Visibility = Visibility.Visible;
        SidebarColumn.Width = new GridLength(_settings.SidebarWidth);
        HorizontalSplitter.Visibility = Visibility.Visible;
        BelowVideoRow.Height = new GridLength(2, GridUnitType.Star);
        BelowVideoRow.MinHeight = 100;
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
