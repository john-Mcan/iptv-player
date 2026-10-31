using System.Windows;
using System.Windows.Controls;
using IPTVPlayer.Models;
using IPTVPlayer.ViewModels;

namespace IPTVPlayer.Views;

public partial class LiveTvSidebar : UserControl
{
    public LiveTvSidebar()
    {
        InitializeComponent();
    }

    // TreeView.SelectedItem no admite binding, así que la selección se pasa al ViewModel desde aquí.
    private void ChannelTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is Channel channel && DataContext is MainViewModel vm)
            vm.PlayChannel(channel);
    }
}
