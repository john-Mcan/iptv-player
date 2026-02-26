using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace IPTVPlayer.Views;

public partial class SettingsWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public bool SettingsAutoLoadPlaylist { get; private set; }
    public int SettingsMaxReconnectAttempts { get; private set; }
    public bool HistoryCleared { get; private set; }
    public bool FavoritesCleared { get; private set; }
    public bool SettingsHideAdultContent { get; private set; }

    public SettingsWindow(bool autoLoad, int maxReconnect, bool hideAdultContent)
    {
        InitializeComponent();
        SettingsAutoLoadPlaylist = autoLoad;
        SettingsMaxReconnectAttempts = maxReconnect;
        SettingsHideAdultContent = hideAdultContent;

        AutoLoadCheckBox.IsChecked = autoLoad;
        ReconnectSlider.Value = maxReconnect;
        ReconnectValueText.Text = maxReconnect.ToString();
        HideAdultCheckBox.IsChecked = hideAdultContent;

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"v{version?.Major ?? 1}.{version?.Minor ?? 0}.{version?.Build ?? 0}";
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            int value = 1;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        }
    }

    private void ReconnectSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ReconnectValueText != null)
            ReconnectValueText.Text = ((int)e.NewValue).ToString();
    }

    private void ClearHistory_Click(object sender, RoutedEventArgs e)
    {
        HistoryCleared = true;
        ClearHistoryIcon.Text = "\uE73E";
        ClearHistoryLabel.Text = "Se limpiará al guardar";
        ClearHistoryBtn.IsEnabled = false;
    }

    private void ClearFavorites_Click(object sender, RoutedEventArgs e)
    {
        FavoritesCleared = true;
        ClearFavoritesIcon.Text = "\uE73E";
        ClearFavoritesLabel.Text = "Se limpiarán al guardar";
        ClearFavoritesBtn.IsEnabled = false;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        SettingsAutoLoadPlaylist = AutoLoadCheckBox.IsChecked == true;
        SettingsMaxReconnectAttempts = (int)ReconnectSlider.Value;
        SettingsHideAdultContent = HideAdultCheckBox.IsChecked == true;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        HistoryCleared = false;
        FavoritesCleared = false;
        DialogResult = false;
    }
}
