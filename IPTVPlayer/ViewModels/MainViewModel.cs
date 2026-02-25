using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PlaylistService _playlistService = new();
    private readonly EpgService _epgService = new();
    private const int MaxRecentUrls = 5;
    private const int MaxHistoryEntries = 20;

    private HashSet<string> _favoriteUrlSet = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _favoriteSeriesSet = new(StringComparer.OrdinalIgnoreCase);
    private List<WatchHistoryEntry> _watchHistory = [];
    private WatchHistoryEntry? _currentWatchEntry;
    private string? _epgUrl;

    private List<(string SeriesName, int Season, int Episode, Channel Channel)>? _parsedSeriesData;
    private string? _currentSeriesShowName;
    private int _currentSeriesSeason;

    [ObservableProperty]
    private ContentTab _activeTab = ContentTab.LiveTV;

    [ObservableProperty]
    private string _playlistUrl = string.Empty;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _recentUrls = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private Channel? _selectedChannel;

    // Live TV
    private ContentCategory? _liveTvCategory;

    [ObservableProperty]
    private ObservableCollection<ChannelGroup> _filteredLiveTvGroups = new();

    // Movies
    private ContentCategory? _moviesCategory;

    [ObservableProperty]
    private ObservableCollection<ChannelGroup> _movieCategoryList = new();

    [ObservableProperty]
    private ChannelGroup? _selectedMovieCategory;

    [ObservableProperty]
    private ObservableCollection<Channel> _filteredMovies = new();

    // Series
    private ContentCategory? _seriesCategory;

    [ObservableProperty]
    private ObservableCollection<ChannelGroup> _seriesCategoryList = new();

    [ObservableProperty]
    private ChannelGroup? _selectedSeriesCategory;

    [ObservableProperty]
    private ObservableCollection<Channel> _filteredSeries = new();

    [ObservableProperty]
    private SeriesNavLevel _seriesNavLevel = SeriesNavLevel.Shows;

    [ObservableProperty]
    private string _seriesBreadcrumb = string.Empty;

    // Favorites
    [ObservableProperty]
    private ObservableCollection<Channel> _liveTvFavorites = new();

    [ObservableProperty]
    private ObservableCollection<Channel> _movieFavorites = new();

    [ObservableProperty]
    private ObservableCollection<Channel> _seriesFavorites = new();

    // Watch History
    [ObservableProperty]
    private ObservableCollection<WatchHistoryEntry> _recentLiveTv = new();

    [ObservableProperty]
    private ObservableCollection<WatchHistoryEntry> _continueWatchingMovies = new();

    [ObservableProperty]
    private ObservableCollection<WatchHistoryEntry> _continueWatchingSeries = new();

    // EPG
    [ObservableProperty]
    private ObservableCollection<EpgProgram> _currentEpgProgrammes = new();

    [ObservableProperty]
    private EpgProgram? _currentProgram;

    [ObservableProperty]
    private EpgProgram? _nextProgram;

    [ObservableProperty]
    private bool _isEpgLoading;

    public bool IsLiveTvActive => ActiveTab == ContentTab.LiveTV;
    public bool IsMoviesActive => ActiveTab == ContentTab.Movies;
    public bool IsSeriesActive => ActiveTab == ContentTab.Series;

    public bool CanNavigateSeriesBack => SeriesNavLevel != SeriesNavLevel.Shows;

    public bool HasLiveTvFavorites => LiveTvFavorites.Count > 0;
    public bool HasMovieFavorites => MovieFavorites.Count > 0;
    public bool HasSeriesFavorites => SeriesFavorites.Count > 0;
    public bool HasRecentLiveTv => RecentLiveTv.Count > 0;
    public bool HasContinueWatchingMovies => ContinueWatchingMovies.Count > 0;
    public bool HasContinueWatchingSeries => ContinueWatchingSeries.Count > 0;
    public bool HasEpgData => CurrentEpgProgrammes.Count > 0;

    public int LiveTvChannelCount => _liveTvCategory?.TotalChannels ?? 0;
    public int MovieCount => _moviesCategory?.TotalChannels ?? 0;
    public int SeriesCount => _seriesCategory?.TotalChannels ?? 0;
    public int TotalChannels => LiveTvChannelCount + MovieCount + SeriesCount;

    public PlayerViewModel Player { get; } = new();

    // Full List View state
    [ObservableProperty]
    private bool _isViewingFullList;

    [ObservableProperty]
    private string _fullListTitle = string.Empty;

    [ObservableProperty]
    private IEnumerable<object> _activeFullList = [];

    // Previews for sidebars
    public IEnumerable<Channel> LiveTvFavoritesPreview => LiveTvFavorites;
    public IEnumerable<Channel> MovieFavoritesPreview => MovieFavorites;
    public IEnumerable<Channel> SeriesFavoritesPreview => SeriesFavorites;
    public IEnumerable<WatchHistoryEntry> RecentLiveTvPreview => RecentLiveTv;
    public IEnumerable<WatchHistoryEntry> ContinueWatchingMoviesPreview => ContinueWatchingMovies;
    public IEnumerable<WatchHistoryEntry> ContinueWatchingSeriesPreview => ContinueWatchingSeries;

    [RelayCommand]
    private void OpenFullList(string listType)
    {
        IsViewingFullList = true;
        switch (listType)
        {
            case "LiveTvFavorites":
                FullListTitle = "Favoritos TV en Vivo";
                ActiveFullList = LiveTvFavorites;
                break;
            case "MovieFavorites":
                FullListTitle = "Películas Favoritas";
                ActiveFullList = MovieFavorites;
                break;
            case "SeriesFavorites":
                FullListTitle = "Series Favoritas";
                ActiveFullList = SeriesFavorites;
                break;
            case "RecentLiveTv":
                FullListTitle = "Vistos Recientemente";
                ActiveFullList = RecentLiveTv;
                break;
            case "ContinueWatchingMovies":
                FullListTitle = "Continuar Viendo (Películas)";
                ActiveFullList = ContinueWatchingMovies;
                break;
            case "ContinueWatchingSeries":
                FullListTitle = "Continuar Viendo (Series)";
                ActiveFullList = ContinueWatchingSeries;
                break;
        }
    }

    [RelayCommand]
    private void CloseFullList()
    {
        IsViewingFullList = false;
        ActiveFullList = [];
    }

    public MainViewModel()
    {
        Player.OnPositionUpdated = UpdateWatchPosition;
    }

    partial void OnActiveTabChanged(ContentTab value)
    {
        OnPropertyChanged(nameof(IsLiveTvActive));
        OnPropertyChanged(nameof(IsMoviesActive));
        OnPropertyChanged(nameof(IsSeriesActive));
        SearchText = string.Empty;
        ApplyFilter();

        if (IsViewingFullList)
        {
            var isFavorites = FullListTitle.Contains("Favorit");
            var isHistory = FullListTitle.Contains("Reciente") || FullListTitle.Contains("Continuar");

            if (isFavorites)
            {
                switch (value)
                {
                    case ContentTab.LiveTV: OpenFullList("LiveTvFavorites"); break;
                    case ContentTab.Movies: OpenFullList("MovieFavorites"); break;
                    case ContentTab.Series: OpenFullList("SeriesFavorites"); break;
                }
            }
            else if (isHistory)
            {
                switch (value)
                {
                    case ContentTab.LiveTV: OpenFullList("RecentLiveTv"); break;
                    case ContentTab.Movies: OpenFullList("ContinueWatchingMovies"); break;
                    case ContentTab.Series: OpenFullList("ContinueWatchingSeries"); break;
                }
            }
        }
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedMovieCategoryChanged(ChannelGroup? value) => ApplyMovieFilter();
    partial void OnSelectedSeriesCategoryChanged(ChannelGroup? value) => RebuildSeriesNavigation();

    partial void OnSeriesNavLevelChanged(SeriesNavLevel value)
    {
        OnPropertyChanged(nameof(CanNavigateSeriesBack));
    }

    [RelayCommand]
    private async Task LoadPlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(PlaylistUrl)) return;

        // Backup existing valid URL
        var previousUrl = RecentUrls.FirstOrDefault(); 
        
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var categories = await _playlistService.LoadPlaylistAsync(PlaylistUrl);

            _liveTvCategory = categories.FirstOrDefault(c => c.Type == CategoryType.LiveTV);
            _moviesCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Movies);
            _seriesCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Series);

            MarkFavoriteChannels();

            var movieGroups = (_moviesCategory?.Groups ?? []).ToList();
            if (movieGroups.Count > 1)
            {
                movieGroups.Insert(0, new ChannelGroup
                {
                    Name = "Todas",
                    Channels = new ObservableCollection<Channel>(movieGroups.SelectMany(g => g.Channels))
                });
            }
            MovieCategoryList = new ObservableCollection<ChannelGroup>(movieGroups);

            var seriesGroups = (_seriesCategory?.Groups ?? []).ToList();
            if (seriesGroups.Count > 1)
            {
                seriesGroups.Insert(0, new ChannelGroup
                {
                    Name = "Todas",
                    Channels = new ObservableCollection<Channel>(seriesGroups.SelectMany(g => g.Channels))
                });
            }
            SeriesCategoryList = new ObservableCollection<ChannelGroup>(seriesGroups);

            SelectedMovieCategory = MovieCategoryList.FirstOrDefault();
            SelectedSeriesCategory = SeriesCategoryList.FirstOrDefault();

            OnPropertyChanged(nameof(LiveTvChannelCount));
            OnPropertyChanged(nameof(MovieCount));
            OnPropertyChanged(nameof(SeriesCount));
            OnPropertyChanged(nameof(TotalChannels));

            ApplyFilter();
            RefreshFavoriteLists();
            RefreshHistoryLists();

            Player.StatusText = $"Playlist cargada — {TotalChannels} canales";
            AddToRecentUrls(PlaylistUrl);

            _epgUrl = _playlistService.EpgUrl;
            if (!string.IsNullOrEmpty(_epgUrl))
                _ = LoadEpgAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            Player.StatusText = "Error al cargar playlist";
            // Restore previous functional URL if loading failed
            if (!string.IsNullOrEmpty(previousUrl) && previousUrl != PlaylistUrl)
            {
                PlaylistUrl = previousUrl;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    #region Filtering

    private void ApplyFilter()
    {
        switch (ActiveTab)
        {
            case ContentTab.LiveTV: ApplyLiveTvFilter(); break;
            case ContentTab.Movies: ApplyMovieFilter(); break;
            case ContentTab.Series: RefreshSeriesDisplay(); break;
        }
    }

    private void ApplyLiveTvFilter()
    {
        var groups = _liveTvCategory?.Groups;
        if (groups is null || groups.Count == 0)
        {
            FilteredLiveTvGroups = new();
            return;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredLiveTvGroups = new ObservableCollection<ChannelGroup>(groups);
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

        FilteredLiveTvGroups = new ObservableCollection<ChannelGroup>(filtered);
    }

    private void ApplyMovieFilter()
    {
        IEnumerable<Channel> source;

        if (SelectedMovieCategory is not null)
            source = SelectedMovieCategory.Channels;
        else if (_moviesCategory is not null)
            source = _moviesCategory.Groups.SelectMany(g => g.Channels);
        else
        {
            FilteredMovies = new();
            return;
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
            source = source.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredMovies = new ObservableCollection<Channel>(source);
    }

    [GeneratedRegex(@"[Ss](\d{1,2})\s*[Ee](\d{1,3})|[Tt](\d{1,2})\s*[Ee](\d{1,3})|(\d{1,2})[xX](\d{1,3})")]
    private static partial Regex EpisodePattern();

    private static (string SeriesName, int Season, int Episode) ParseEpisodeInfo(string name)
    {
        var match = EpisodePattern().Match(name);
        if (!match.Success)
            return (name.Trim(), 0, 0);

        var seriesName = name[..match.Index].Trim(' ', '-', '\u2013', '\u2014', ':', '.');
        if (string.IsNullOrWhiteSpace(seriesName))
            seriesName = name.Trim();

        int season = 0, episode = 0;
        for (int i = 1; i <= 5; i += 2)
        {
            if (match.Groups[i].Success)
            {
                season = int.Parse(match.Groups[i].Value);
                episode = int.Parse(match.Groups[i + 1].Value);
                break;
            }
        }
        return (seriesName, season, episode);
    }

    private void RebuildSeriesNavigation()
    {
        IEnumerable<Channel> source;

        if (SelectedSeriesCategory is not null)
            source = SelectedSeriesCategory.Channels;
        else if (_seriesCategory is not null)
            source = _seriesCategory.Groups.SelectMany(g => g.Channels);
        else
        {
            _parsedSeriesData = null;
            FilteredSeries = new();
            return;
        }

        _parsedSeriesData = source.Select(c =>
        {
            var (seriesName, season, episode) = ParseEpisodeInfo(c.Name);
            return (SeriesName: seriesName, Season: season, Episode: episode, Channel: c);
        }).ToList();

        _currentSeriesShowName = null;
        _currentSeriesSeason = 0;

        var hasPatternMatches = _parsedSeriesData.Any(e => e.Season > 0);
        var seriesNames = _parsedSeriesData
            .Where(e => e.Season > 0)
            .Select(e => e.SeriesName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!hasPatternMatches)
        {
            SeriesNavLevel = SeriesNavLevel.Episodes;
            SeriesBreadcrumb = string.Empty;
        }
        else if (seriesNames.Count > 1)
        {
            SeriesNavLevel = SeriesNavLevel.Shows;
            SeriesBreadcrumb = string.Empty;
        }
        else
        {
            _currentSeriesShowName = seriesNames.FirstOrDefault();
            var seasons = _parsedSeriesData
                .Select(e => e.Season).Where(s => s > 0).Distinct().ToList();
            if (seasons.Count > 1)
            {
                SeriesNavLevel = SeriesNavLevel.Seasons;
                SeriesBreadcrumb = _currentSeriesShowName ?? string.Empty;
            }
            else
            {
                SeriesNavLevel = SeriesNavLevel.Episodes;
                SeriesBreadcrumb = string.Empty;
            }
        }

        RefreshSeriesDisplay();
    }

    private void RefreshSeriesDisplay()
    {
        if (_parsedSeriesData == null)
        {
            FilteredSeries = new();
            return;
        }

        switch (SeriesNavLevel)
        {
            case SeriesNavLevel.Shows: DisplaySeriesShows(); break;
            case SeriesNavLevel.Seasons: DisplaySeriesSeasons(); break;
            case SeriesNavLevel.Episodes: DisplaySeriesEpisodes(); break;
        }
    }

    private void DisplaySeriesShows()
    {
        var shows = _parsedSeriesData!
            .GroupBy(e => e.SeriesName, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First().Channel;
                var seasonCount = g.Select(e => e.Season).Where(s => s > 0).Distinct().Count();
                return new Channel
                {
                    Name = g.Key,
                    LogoUrl = first.LogoUrl,
                    GroupTitle = seasonCount > 0
                        ? $"{seasonCount} temp \u00B7 {g.Count()} ep"
                        : $"{g.Count()} episodios",
                    Url = string.Empty,
                    Category = CategoryType.Series,
                    IsFavorite = _favoriteSeriesSet.Contains(g.Key)
                };
            })
            .OrderBy(c => c.Name)
            .ToList();

        if (!string.IsNullOrWhiteSpace(SearchText))
            shows = shows.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        FilteredSeries = new ObservableCollection<Channel>(shows);
    }

    private void DisplaySeriesSeasons()
    {
        if (_currentSeriesShowName == null) return;

        var episodes = _parsedSeriesData!
            .Where(e => e.SeriesName.Equals(_currentSeriesShowName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var seasonItems = episodes
            .Select(e => e.Season).Where(s => s > 0).Distinct().OrderBy(s => s)
            .Select(s =>
            {
                var seasonEps = episodes.Where(e => e.Season == s).ToList();
                return new Channel
                {
                    Name = $"Temporada {s}",
                    LogoUrl = seasonEps.First().Channel.LogoUrl,
                    GroupTitle = $"{seasonEps.Count} episodios",
                    Url = string.Empty,
                    Category = CategoryType.Series,
                    IsFavorite = _favoriteSeriesSet.Contains(_currentSeriesShowName ?? "")
                };
            })
            .ToList();

        FilteredSeries = new ObservableCollection<Channel>(seasonItems);
    }

    private void DisplaySeriesEpisodes()
    {
        IEnumerable<(string SeriesName, int Season, int Episode, Channel Channel)> source = _parsedSeriesData!;

        if (_currentSeriesShowName != null)
            source = source.Where(e => e.SeriesName.Equals(_currentSeriesShowName, StringComparison.OrdinalIgnoreCase));

        if (_currentSeriesSeason > 0)
            source = source.Where(e => e.Season == _currentSeriesSeason);

        var channels = source.OrderBy(e => e.Season).ThenBy(e => e.Episode).Select(e => e.Channel);

        if (!string.IsNullOrWhiteSpace(SearchText))
            channels = channels.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredSeries = new ObservableCollection<Channel>(channels);
    }

    public void HandleSeriesItemClick(Channel item)
    {
        switch (SeriesNavLevel)
        {
            case SeriesNavLevel.Shows:
                _currentSeriesShowName = item.Name;
                var episodes = _parsedSeriesData!
                    .Where(e => e.SeriesName.Equals(item.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var seasons = episodes.Select(e => e.Season).Where(s => s > 0).Distinct().ToList();

                if (seasons.Count <= 1)
                {
                    _currentSeriesSeason = seasons.FirstOrDefault();
                    SeriesNavLevel = SeriesNavLevel.Episodes;
                    SeriesBreadcrumb = item.Name;
                }
                else
                {
                    SeriesNavLevel = SeriesNavLevel.Seasons;
                    SeriesBreadcrumb = item.Name;
                }
                break;

            case SeriesNavLevel.Seasons:
                var seasonStr = item.Name.Replace("Temporada ", "");
                if (int.TryParse(seasonStr, out var season))
                {
                    _currentSeriesSeason = season;
                    SeriesNavLevel = SeriesNavLevel.Episodes;
                    SeriesBreadcrumb = $"{_currentSeriesShowName} \u203A Temporada {season}";
                }
                break;

            case SeriesNavLevel.Episodes:
                PlayChannel(item);
                return;
        }

        SearchText = string.Empty;
        RefreshSeriesDisplay();
    }

    [RelayCommand]
    private void NavigateSeriesBack()
    {
        switch (SeriesNavLevel)
        {
            case SeriesNavLevel.Episodes when _currentSeriesShowName != null:
                var episodes = _parsedSeriesData!
                    .Where(e => e.SeriesName.Equals(_currentSeriesShowName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var seasons = episodes.Select(e => e.Season).Where(s => s > 0).Distinct().ToList();

                if (seasons.Count > 1 && _currentSeriesSeason > 0)
                {
                    _currentSeriesSeason = 0;
                    SeriesNavLevel = SeriesNavLevel.Seasons;
                    SeriesBreadcrumb = _currentSeriesShowName;
                }
                else
                {
                    _currentSeriesShowName = null;
                    _currentSeriesSeason = 0;
                    SeriesNavLevel = SeriesNavLevel.Shows;
                    SeriesBreadcrumb = string.Empty;
                }
                break;

            case SeriesNavLevel.Seasons:
                _currentSeriesShowName = null;
                _currentSeriesSeason = 0;
                SeriesNavLevel = SeriesNavLevel.Shows;
                SeriesBreadcrumb = string.Empty;
                break;
        }

        SearchText = string.Empty;
        RefreshSeriesDisplay();
    }

    public void NavigateToSeriesShow(string showName)
    {
        if (_parsedSeriesData == null) return;

        _currentSeriesShowName = showName;
        var episodes = _parsedSeriesData
            .Where(e => e.SeriesName.Equals(showName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var seasons = episodes.Select(e => e.Season).Where(s => s > 0).Distinct().ToList();

        if (seasons.Count <= 1)
        {
            _currentSeriesSeason = seasons.FirstOrDefault();
            SeriesNavLevel = SeriesNavLevel.Episodes;
            SeriesBreadcrumb = showName;
        }
        else
        {
            SeriesNavLevel = SeriesNavLevel.Seasons;
            SeriesBreadcrumb = showName;
        }

        SearchText = string.Empty;
        RefreshSeriesDisplay();
    }

    #endregion

    #region Playback

    [RelayCommand]
    private void PlayFromFullList(object? item)
    {
        if (item is Channel channel)
            PlayChannel(channel);
        else if (item is WatchHistoryEntry history)
            PlayFromHistory(history);
    }

    public void PlayChannel(Channel channel)
    {
        SaveCurrentWatchPosition();
        SelectedChannel = channel;
        AddToWatchHistory(channel);
        Player.PlayChannel(channel);
        UpdateEpgForChannel(channel);
        IsViewingFullList = false;
    }

    public void PlayFromHistory(WatchHistoryEntry entry)
    {
        SaveCurrentWatchPosition();
        var channel = new Channel
        {
            Name = entry.Name,
            Url = entry.Url,
            LogoUrl = entry.LogoUrl,
            Category = entry.Category,
            IsFavorite = _favoriteUrlSet.Contains(entry.Url)
        };
        SelectedChannel = channel;
        _currentWatchEntry = entry;
        entry.Timestamp = DateTime.UtcNow;

        if (entry.Category != CategoryType.LiveTV && entry.PositionMs > 0)
            Player.PlayChannelFromPosition(channel, entry.PositionMs);
        else
            Player.PlayChannel(channel);

        UpdateEpgForChannel(channel);
        RefreshHistoryLists();
        IsViewingFullList = false;
    }

    #endregion

    #region Favorites

    [RelayCommand]
    private void ToggleFavorite(Channel channel)
    {
        if (string.IsNullOrEmpty(channel.Url) && channel.Category == CategoryType.Series)
        {
            var showName = SeriesNavLevel == SeriesNavLevel.Seasons
                ? (_currentSeriesShowName ?? channel.Name)
                : channel.Name;

            if (_favoriteSeriesSet.Remove(showName))
                channel.IsFavorite = false;
            else
            {
                _favoriteSeriesSet.Add(showName);
                channel.IsFavorite = true;
            }
        }
        else
        {
            channel.IsFavorite = !channel.IsFavorite;
            if (channel.IsFavorite)
                _favoriteUrlSet.Add(channel.Url);
            else
                _favoriteUrlSet.Remove(channel.Url);
        }

        RefreshFavoriteLists();
    }

    private void MarkFavoriteChannels()
    {
        foreach (var channel in GetAllChannels())
            channel.IsFavorite = _favoriteUrlSet.Contains(channel.Url);
    }

    private void RefreshFavoriteLists()
    {
        var favChannels = GetAllChannels().Where(c => c.IsFavorite).ToList();
        LiveTvFavorites = new(favChannels.Where(c => c.Category == CategoryType.LiveTV));
        MovieFavorites = new(favChannels.Where(c => c.Category == CategoryType.Movies));

        if (_parsedSeriesData != null && _favoriteSeriesSet.Count > 0)
        {
            SeriesFavorites = new(_parsedSeriesData
                .GroupBy(e => e.SeriesName, StringComparer.OrdinalIgnoreCase)
                .Where(g => _favoriteSeriesSet.Contains(g.Key))
                .Select(g => new Channel
                {
                    Name = g.Key,
                    LogoUrl = g.First().Channel.LogoUrl,
                    Url = string.Empty,
                    Category = CategoryType.Series
                }));
        }
        else
            SeriesFavorites = new();

        OnPropertyChanged(nameof(HasLiveTvFavorites));
        OnPropertyChanged(nameof(HasMovieFavorites));
        OnPropertyChanged(nameof(HasSeriesFavorites));
        OnPropertyChanged(nameof(LiveTvFavoritesPreview));
        OnPropertyChanged(nameof(MovieFavoritesPreview));
        OnPropertyChanged(nameof(SeriesFavoritesPreview));
    }

    #endregion

    #region Watch History

    private void AddToWatchHistory(Channel channel)
    {
        var existing = _watchHistory.FirstOrDefault(h =>
            h.Url.Equals(channel.Url, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Timestamp = DateTime.UtcNow;
            existing.Name = channel.Name;
            existing.LogoUrl = channel.LogoUrl;
            _currentWatchEntry = existing;
        }
        else
        {
            var entry = new WatchHistoryEntry
            {
                Url = channel.Url,
                Name = channel.Name,
                LogoUrl = channel.LogoUrl,
                Category = channel.Category,
                Timestamp = DateTime.UtcNow
            };
            _watchHistory.Insert(0, entry);
            _currentWatchEntry = entry;
        }

        while (_watchHistory.Count > MaxHistoryEntries)
            _watchHistory.RemoveAt(_watchHistory.Count - 1);

        RefreshHistoryLists();
    }

    private void SaveCurrentWatchPosition()
    {
        if (_currentWatchEntry == null) return;
        var posMs = Player.GetCurrentTimeMs();
        var durMs = Player.GetDurationMs();
        if (posMs > 0)
        {
            _currentWatchEntry.PositionMs = posMs;
            if (durMs > 0) _currentWatchEntry.DurationMs = durMs;
        }
    }

    private void UpdateWatchPosition(long positionMs, long durationMs)
    {
        if (_currentWatchEntry == null || durationMs <= 0) return;
        _currentWatchEntry.PositionMs = positionMs;
        _currentWatchEntry.DurationMs = durationMs;
    }

    [RelayCommand]
    private void RemoveHistoryEntry(WatchHistoryEntry entry)
    {
        if (entry == null) return;
        
        var existing = _watchHistory.FirstOrDefault(h => h.Url.Equals(entry.Url, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _watchHistory.Remove(existing);
            if (_currentWatchEntry?.Url.Equals(entry.Url, StringComparison.OrdinalIgnoreCase) == true)
            {
                _currentWatchEntry = null;
            }
            RefreshHistoryLists();
        }
    }

    private void RefreshHistoryLists()
    {
        var ordered = _watchHistory.OrderByDescending(h => h.Timestamp).ToList();

        RecentLiveTv = new(ordered.Where(h => h.Category == CategoryType.LiveTV).Take(10));
        ContinueWatchingMovies = new(ordered.Where(h => h.Category == CategoryType.Movies && h.HasProgress).Take(10));
        ContinueWatchingSeries = new(ordered.Where(h => h.Category == CategoryType.Series && h.HasProgress).Take(10));

        OnPropertyChanged(nameof(HasRecentLiveTv));
        OnPropertyChanged(nameof(HasContinueWatchingMovies));
        OnPropertyChanged(nameof(HasContinueWatchingSeries));
        OnPropertyChanged(nameof(RecentLiveTvPreview));
        OnPropertyChanged(nameof(ContinueWatchingMoviesPreview));
        OnPropertyChanged(nameof(ContinueWatchingSeriesPreview));
    }

    #endregion

    #region EPG

    private async Task LoadEpgAsync()
    {
        if (string.IsNullOrEmpty(_epgUrl)) return;

        try
        {
            IsEpgLoading = true;
            Player.StatusText = "Cargando EPG...";
            await _epgService.LoadAsync(_epgUrl);
            Player.StatusText = "EPG cargada";
            if (SelectedChannel != null)
                UpdateEpgForChannel(SelectedChannel);
        }
        catch
        {
            Player.StatusText = "No se pudo cargar EPG";
        }
        finally
        {
            IsEpgLoading = false;
        }
    }

    public void UpdateEpgForChannel(Channel? channel)
    {
        if (channel == null || !_epgService.IsLoaded)
        {
            CurrentEpgProgrammes = new();
            CurrentProgram = null;
            NextProgram = null;
            OnPropertyChanged(nameof(HasEpgData));
            return;
        }

        var tvgId = !string.IsNullOrEmpty(channel.TvgId) ? channel.TvgId : channel.TvgName;
        CurrentEpgProgrammes = new(_epgService.GetProgramsForChannel(tvgId));
        CurrentProgram = _epgService.GetCurrentProgram(tvgId);
        NextProgram = _epgService.GetNextProgram(tvgId);
        OnPropertyChanged(nameof(HasEpgData));
    }

    public void RefreshEpgDisplay()
    {
        if (SelectedChannel != null && _epgService.IsLoaded)
            UpdateEpgForChannel(SelectedChannel);
    }

    #endregion

    #region Settings

    public void LoadSettings(AppSettings settings)
    {
        PlaylistUrl = settings.LastPlaylistUrl;
        RecentUrls = new ObservableCollection<string>(settings.RecentPlaylistUrls);
        Player.Volume = settings.Volume;
        Player.IsMuted = settings.IsMuted;

        if (Enum.TryParse<ContentTab>(settings.ActiveTab, out var tab))
            ActiveTab = tab;

        _favoriteUrlSet = new HashSet<string>(settings.FavoriteUrls, StringComparer.OrdinalIgnoreCase);
        _favoriteSeriesSet = new HashSet<string>(settings.FavoriteSeriesNames ?? [], StringComparer.OrdinalIgnoreCase);
        _watchHistory = settings.WatchHistory?.ToList() ?? [];
        Player.MaxReconnectAttempts = settings.MaxReconnectAttempts;
        RefreshHistoryLists();
    }

    public void SaveToSettings(AppSettings settings)
    {
        SaveCurrentWatchPosition();
        settings.LastPlaylistUrl = PlaylistUrl;
        settings.RecentPlaylistUrls = RecentUrls.ToList();
        settings.Volume = Player.Volume;
        settings.IsMuted = Player.IsMuted;
        settings.ActiveTab = ActiveTab.ToString();
        settings.FavoriteUrls = _favoriteUrlSet.ToList();
        settings.FavoriteSeriesNames = _favoriteSeriesSet.ToList();
        settings.WatchHistory = _watchHistory;
    }

    public void ClearWatchHistory()
    {
        _watchHistory.Clear();
        _currentWatchEntry = null;
        RefreshHistoryLists();
    }

    public void ClearFavorites()
    {
        _favoriteUrlSet.Clear();
        _favoriteSeriesSet.Clear();
        MarkFavoriteChannels();
        RefreshFavoriteLists();
        if (SeriesNavLevel == SeriesNavLevel.Shows)
            RefreshSeriesDisplay();
    }

    #endregion

    #region Helpers

    private IEnumerable<Channel> GetAllChannels()
    {
        var cats = new[] { _liveTvCategory, _moviesCategory, _seriesCategory };
        return cats.Where(c => c != null)
                   .SelectMany(c => c!.Groups)
                   .SelectMany(g => g.Channels);
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

        // WPF editable ComboBox clears Text when ItemsSource mutates,
        // which wipes PlaylistUrl via the two-way binding. Restore it.
        PlaylistUrl = url;
    }

    #endregion
}
