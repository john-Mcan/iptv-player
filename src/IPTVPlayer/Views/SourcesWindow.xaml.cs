using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using IPTVPlayer.Models;
using Microsoft.Win32;

namespace IPTVPlayer.Views;

/// <summary>Gestor de fuentes: agrega, edita y elimina listas M3U y cuentas Xtream, y elige cuál cargar.</summary>
public partial class SourcesWindow : Window
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    // Fuente que se está editando; null mientras se completa una nueva.
    private PlaylistSource? _editing;

    public ObservableCollection<PlaylistSource> Sources { get; }

    public PlaylistSource? SelectedSource => SourceList.SelectedItem as PlaylistSource;

    public SourcesWindow(IEnumerable<PlaylistSource> sources, PlaylistSource? selected)
    {
        InitializeComponent();

        Sources = new ObservableCollection<PlaylistSource>(sources);
        SourceList.ItemsSource = Sources;
        SourceList.SelectedItem = selected ?? Sources.FirstOrDefault();

        if (SourceList.SelectedItem == null)
            StartNew();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var value = 1;
            DwmSetWindowAttribute(hwnd, 20, ref value, sizeof(int));
        }
    }

    private void SourceList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SourceList.SelectedItem is PlaylistSource source)
            Fill(source);
    }

    private void Fill(PlaylistSource source)
    {
        _editing = source;
        NameBox.Text = source.Name;
        M3uOption.IsChecked = !source.IsXtream;
        XtreamOption.IsChecked = source.IsXtream;
        UrlBox.Text = source.IsXtream ? string.Empty : source.Url;
        ServerBox.Text = source.IsXtream ? source.Url : string.Empty;
        UserBox.Text = source.Username;
        PasswordInput.Password = source.Password;
        DeleteButton.IsEnabled = true;
        ShowMessage(string.Empty);
    }

    private void StartNew()
    {
        _editing = null;
        SourceList.SelectedItem = null;
        NameBox.Text = UrlBox.Text = ServerBox.Text = UserBox.Text = string.Empty;
        PasswordInput.Password = string.Empty;
        M3uOption.IsChecked = true;
        DeleteButton.IsEnabled = false;
        ShowMessage(string.Empty);
        UrlBox.Focus();
    }

    private void SourceType_Changed(object sender, RoutedEventArgs e)
    {
        if (M3uFields == null || XtreamFields == null) return;
        var isXtream = XtreamOption.IsChecked == true;
        M3uFields.Visibility = isXtream ? Visibility.Collapsed : Visibility.Visible;
        XtreamFields.Visibility = isXtream ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Valida el formulario y guarda la fuente (nueva o editada). Devuelve false si falta algo.</summary>
    private bool SaveForm()
    {
        var isXtream = XtreamOption.IsChecked == true;
        var url = (isXtream ? ServerBox.Text : UrlBox.Text).Trim();

        if (url.Length == 0)
            return ShowError(isXtream ? "Indica la URL del servidor." : "Indica la URL o el archivo de la lista.");

        if (isXtream)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                return ShowError("La URL del servidor debe empezar con http:// o https://.");
            if (UserBox.Text.Trim().Length == 0 || PasswordInput.Password.Length == 0)
                return ShowError("Indica el usuario y la contraseña de la cuenta.");
        }

        var source = _editing ?? new PlaylistSource();
        source.Name = NameBox.Text.Trim().Length > 0 ? NameBox.Text.Trim() : PlaylistSource.SuggestName(url);
        source.Type = isXtream ? PlaylistSourceType.Xtream : PlaylistSourceType.M3U;
        source.Url = url;
        source.Username = isXtream ? UserBox.Text.Trim() : string.Empty;
        source.Password = isXtream ? PasswordInput.Password : string.Empty;

        if (_editing == null)
            Sources.Add(source);

        SourceList.Items.Refresh();
        SourceList.SelectedItem = source;
        ShowMessage("Guardada.");
        return true;
    }

    private bool ShowError(string message)
    {
        ShowMessage(message, isError: true);
        return false;
    }

    private void ShowMessage(string message, bool isError = false)
    {
        MessageText.Text = message;
        MessageText.Foreground = (System.Windows.Media.Brush)FindResource(isError ? "ErrorBrush" : "TextDimBrush");
    }

    private void New_Click(object sender, RoutedEventArgs e) => StartNew();

    private void Save_Click(object sender, RoutedEventArgs e) => SaveForm();

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (SourceList.SelectedItem is not PlaylistSource source) return;

        var index = SourceList.SelectedIndex;
        Sources.Remove(source);

        if (Sources.Count == 0)
            StartNew();
        else
            SourceList.SelectedIndex = Math.Min(index, Sources.Count - 1);
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Elegir lista M3U",
            Filter = "Listas M3U (*.m3u;*.m3u8)|*.m3u;*.m3u8|Todos los archivos (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == true)
            UrlBox.Text = dialog.FileName;
    }

    private void Load_Click(object sender, RoutedEventArgs e)
    {
        if (SaveForm())
            DialogResult = true;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
