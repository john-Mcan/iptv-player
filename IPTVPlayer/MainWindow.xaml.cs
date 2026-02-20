using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;

        SeekSlider.AddHandler(Thumb.DragStartedEvent,
            new DragStartedEventHandler((_, _) => _vm.Player.BeginSeek()));
        SeekSlider.AddHandler(Thumb.DragCompletedEvent,
            new DragCompletedEventHandler((_, _) => _vm.Player.EndSeek(SeekSlider.Value)));
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
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
                break;
            case Key.Escape when _isFullscreen:
                ToggleFullscreen();
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
        if (sender is System.Windows.Controls.Primitives.ToggleButton btn &&
            btn.Tag is Models.ContentCategory category)
        {
            _vm.SelectedCategory = category;
        }
    }

    #region PiP

    private bool _isClosingPiP;

    private async void PiP_Click(object sender, RoutedEventArgs e)
    {
        if (_pipWindow is not null)
        {
            ClosePiP();
            return;
        }
        await OpenPiPAsync();
    }

    private async Task OpenPiPAsync()
    {
        var mediaPlayer = _vm.Player.VlcMediaPlayer;

        // 1. Create PiP window and show it (do NOT detach from main yet)
        _pipWindow = new PiPWindow();
        _pipWindow.Closed += (_, _) => ClosePiP();
        _pipWindow.Show();

        // 2. Wait for the PiP's VideoView.Loaded event specifically
        //    This is when LibVLCSharp creates its internal ForegroundWindow HWND
        var tcs = new TaskCompletionSource();
        if (_pipWindow.VideoViewControl.IsLoaded)
        {
            tcs.TrySetResult();
        }
        else
        {
            _pipWindow.VideoViewControl.Loaded += (_, _) => tcs.TrySetResult();
        }
        await tcs.Task;

        // 3. Yield to ensure HWND is fully initialized
        await Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.Background);

        // 4. Atomic swap: detach from main → attach to PiP (no delay between!)
        //    Delays let VLC lose the rendering context
        if (_pipWindow is not null)
        {
            VideoView.MediaPlayer = null;
            _pipWindow.VideoViewControl.MediaPlayer = mediaPlayer;
        }
    }

    private void ClosePiP()
    {
        if (_pipWindow is null || _isClosingPiP) return;
        _isClosingPiP = true;

        try
        {
            var pip = _pipWindow;
            _pipWindow = null;

            // Atomic swap: detach from PiP → attach to main (no delay!)
            pip.VideoViewControl.MediaPlayer = null;
            VideoView.MediaPlayer = _vm.Player.VlcMediaPlayer;

            // Close the window
            if (pip.IsLoaded)
            {
                try { pip.Close(); } catch { /* already closed */ }
            }
        }
        finally
        {
            _isClosingPiP = false;
        }
    }

    #endregion

    #region Fullscreen

    private void Fullscreen_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

    private void ToggleFullscreen()
    {
        if (_isFullscreen)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = _prevWindowState;
            TopBar.Visibility = Visibility.Visible;
            Sidebar.Visibility = Visibility.Visible;
            SidebarSplitter.Visibility = Visibility.Visible;
            SidebarColumn.Width = new GridLength(280);
            FsIconExpand.Visibility = Visibility.Visible;
            FsIconRestore.Visibility = Visibility.Collapsed;
            _isFullscreen = false;
        }
        else
        {
            _prevWindowState = WindowState;
            TopBar.Visibility = Visibility.Collapsed;
            Sidebar.Visibility = Visibility.Collapsed;
            SidebarSplitter.Visibility = Visibility.Collapsed;
            SidebarColumn.Width = new GridLength(0);
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            FsIconExpand.Visibility = Visibility.Collapsed;
            FsIconRestore.Visibility = Visibility.Visible;
            _isFullscreen = true;
        }
    }

    #endregion
}
