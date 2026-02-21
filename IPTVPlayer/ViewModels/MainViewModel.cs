using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PlaylistService _playlistService = new();
    private const int MaxRecentUrls = 5;

    [ObservableProperty]
    private string _playlistUrl = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ContentCategory> _categories = new();

    [ObservableProperty]
    private ContentCategory? _selectedCategory;

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

    [ObservableProperty]
    private ObservableCollection<string> _recentUrls = new();

    public PlayerViewModel Player { get; } = new();

    public void LoadSettings(AppSettings settings)
    {
        PlaylistUrl = settings.LastPlaylistUrl;
        RecentUrls = new ObservableCollection<string>(settings.RecentPlaylistUrls);
        Player.Volume = settings.Volume;
        Player.IsMuted = settings.IsMuted;
    }

    public void SaveToSettings(AppSettings settings)
    {
        settings.LastPlaylistUrl = PlaylistUrl;
        settings.RecentPlaylistUrls = RecentUrls.ToList();
        settings.Volume = Player.Volume;
        settings.IsMuted = Player.IsMuted;
    }

    private void AddToRecentUrls(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        var existing = RecentUrls.FirstOrDefault(u => u.Equals(url, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
            RecentUrls.Remove(existing);

        RecentUrls.Insert(0, url);

        while (RecentUrls.Count > MaxRecentUrls)
            RecentUrls.RemoveAt(RecentUrls.Count - 1);
    }

    [RelayCommand]
    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistUrl)) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var categories = await _playlistService.LoadPlaylistAsync(PlaylistUrl);
            Categories = new ObservableCollection<ContentCategory>(categories);
            TotalChannels = categories.Sum(c => c.TotalChannels);

            SelectedCategory = Categories.FirstOrDefault();

            Player.StatusText = $"Playlist cargada: {TotalChannels} canales en {Categories.Count} categorías";

            AddToRecentUrls(PlaylistUrl);
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

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(ContentCategory? value) => ApplyFilter();

    private void ApplyFilter()
    {
        if (SelectedCategory is null)
        {
            FilteredGroups = new ObservableCollection<ChannelGroup>();
            return;
        }

        var groups = SelectedCategory.Groups;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredGroups = new ObservableCollection<ChannelGroup>(groups);
            return;
        }

        var filtered = groups
            .Select(g => new ChannelGroup
            {
                Name = g.Name,
                Channels = new ObservableCollection<Channel>(
                    g.Channels.Where(c =>
                        c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)))
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
