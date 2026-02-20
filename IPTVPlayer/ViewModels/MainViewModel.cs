using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PlaylistService _playlistService = new();

    [ObservableProperty]
    private string _playlistUrl = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ChannelGroup> _channelGroups = new();

    [ObservableProperty]
    private ObservableCollection<ChannelGroup> _filteredGroups = new();

    [ObservableProperty]
    private Channel? _selectedChannel;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private int _totalChannels;

    public PlayerViewModel Player { get; } = new();

    [RelayCommand]
    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistUrl)) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var groups = await _playlistService.LoadPlaylistAsync(PlaylistUrl);
            ChannelGroups = new ObservableCollection<ChannelGroup>(groups);
            TotalChannels = groups.Sum(g => g.Channels.Count);
            ApplyFilter(SearchText);

            Player.StatusText = $"Playlist cargada: {TotalChannels} canales";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            Player.StatusText = "Error al cargar playlist";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter(value);

    private void ApplyFilter(string search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            FilteredGroups = new ObservableCollection<ChannelGroup>(ChannelGroups);
            return;
        }

        var filtered = ChannelGroups
            .Select(g => new ChannelGroup
            {
                Name = g.Name,
                Channels = new ObservableCollection<Channel>(
                    g.Channels.Where(c =>
                        c.Name.Contains(search, StringComparison.OrdinalIgnoreCase)))
            })
            .Where(g => g.Channels.Count > 0)
            .ToList();

        FilteredGroups = new ObservableCollection<ChannelGroup>(filtered);
    }

    public void PlayChannel(Channel channel)
    {
        SelectedChannel = channel;
        Player.PlayChannel(channel);
    }
}
