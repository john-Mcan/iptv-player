using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly PlaylistService _playlistService = new();

    // Fuente cargada y, si es Xtream, su cliente y los episodios ya pedidos por serie.
    private PlaylistSource? _loadedSource;
    private XtreamApiService? _xtream;
    private readonly Dictionary<string, List<Channel>> _xtreamEpisodesCache = new();

    private Favorites _favorites = new();
    private WatchHistory _history = new();
    private WatchHistoryEntry? _currentWatchEntry;

    private SeriesCatalog? _seriesCatalog;
    private string? _currentSeriesShowName;
    private int _currentSeriesSeason;

    [ObservableProperty]
    private ContentTab _activeTab = ContentTab.LiveTV;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSources))]
    private ObservableCollection<PlaylistSource> _sources = new();

    [ObservableProperty]
    private PlaylistSource? _selectedSource;

    [ObservableProperty]
    private string _searchText = string.Empty;


    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private Channel? _selectedChannel;

    [ObservableProperty]
    private bool _hideAdultContent;

    partial void OnHideAdultContentChanged(bool value)
    {
        if (_fullLiveTvCategory != null || _fullMoviesCategory != null || _fullSeriesCategory != null)
        {
            ApplyCategoryFilters();
        }
    }

    // Original complete categories
    private ContentCategory? _fullLiveTvCategory;
    private ContentCategory? _fullMoviesCategory;
    private ContentCategory? _fullSeriesCategory;

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
    private int _selectedSortOption;

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

    public bool IsLiveTvActive => ActiveTab == ContentTab.LiveTV;
    public bool IsMoviesActive => ActiveTab == ContentTab.Movies;
    public bool IsSeriesActive => ActiveTab == ContentTab.Series;

    public bool CanNavigateSeriesBack => SeriesNavLevel != SeriesNavLevel.Shows;

    public bool HasSources => Sources.Count > 0;

    public bool HasLiveTvFavorites => LiveTvFavorites.Count > 0;
    public bool HasMovieFavorites => MovieFavorites.Count > 0;
    public bool HasSeriesFavorites => SeriesFavorites.Count > 0;
    public bool HasRecentLiveTv => RecentLiveTv.Count > 0;
    public bool HasContinueWatchingMovies => ContinueWatchingMovies.Count > 0;
    public bool HasContinueWatchingSeries => ContinueWatchingSeries.Count > 0;

    public int LiveTvChannelCount => _liveTvCategory?.TotalChannels ?? 0;
    public int MovieCount => _moviesCategory?.TotalChannels ?? 0;
    public int SeriesCount => _seriesCategory?.TotalChannels ?? 0;
    public int TotalChannels => LiveTvChannelCount + MovieCount + SeriesCount;

    public PlayerViewModel Player { get; } = new();

    public EpgViewModel Epg { get; }

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
        Epg = new EpgViewModel(status => Player.StatusText = status);
        Player.OnPositionUpdated = UpdateWatchPosition;
        Player.OnEndReached = HandleEndReached;
        Player.HasNextEpisode = () => GetNextEpisode() != null;
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
    partial void OnSelectedSortOptionChanged(int value) => ApplyFilter();

    partial void OnSeriesNavLevelChanged(SeriesNavLevel value)
    {
        OnPropertyChanged(nameof(CanNavigateSeriesBack));
    }

    /// <summary>Carga la fuente seleccionada: una lista M3U o el catálogo de una cuenta Xtream.</summary>
    [RelayCommand]
    private async Task LoadSourceAsync()
    {
        var source = SelectedSource;
        if (source == null) return;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            List<ContentCategory> categories;
            string? epgUrl;
            XtreamApiService? xtream = null;

            if (source.IsXtream)
            {
                xtream = new XtreamApiService(source);
                categories = await xtream.LoadCatalogAsync();
                epgUrl = xtream.EpgUrl;
            }
            else
            {
                categories = await _playlistService.LoadPlaylistAsync(source.Url);
                epgUrl = _playlistService.EpgUrl;
            }

            _loadedSource = source;
            _xtream = xtream;
            _xtreamEpisodesCache.Clear();

            _fullLiveTvCategory = categories.FirstOrDefault(c => c.Type == CategoryType.LiveTV);
            _fullMoviesCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Movies);
            _fullSeriesCategory = categories.FirstOrDefault(c => c.Type == CategoryType.Series);

            ApplyCategoryFilters();

            Player.StatusText = $"{source.Name} cargada — {TotalChannels} canales";

            if (!string.IsNullOrEmpty(epgUrl))
                _ = LoadEpgAsync(epgUrl);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            Player.StatusText = "Error al cargar la fuente";
            // Vuelve a mostrar la fuente cuyo contenido sigue en pantalla.
            if (_loadedSource != null && _loadedSource != source)
                SelectedSource = _loadedSource;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Reemplaza la lista de fuentes (después de editarlas) conservando la selección si sigue existiendo.</summary>
    public void UpdateSources(IEnumerable<PlaylistSource> sources, PlaylistSource? selected)
    {
        Sources = new ObservableCollection<PlaylistSource>(sources);
        SelectedSource = selected != null && Sources.Contains(selected)
            ? selected
            : Sources.FirstOrDefault(s => s == SelectedSource) ?? Sources.FirstOrDefault();
    }

    private static readonly string[] AdultKeywords = ["xxx", "adult", "adultos", "+18", "18+", "porn", "erotic", "erótic", "hentai"];

    private ContentCategory? FilterCategoryAdultContent(ContentCategory? source)
    {
        if (source == null) return null;
        if (!HideAdultContent) return source;

        var cleanGroups = source.Groups.Select(g =>
        {
            var isAdultGroup = AdultKeywords.Any(kw => g.Name.Contains(kw, StringComparison.OrdinalIgnoreCase));
            if (isAdultGroup) return null;

            var cleanChannels = g.Channels.Where(c =>
                !AdultKeywords.Any(kw => c.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                                         c.GroupTitle.Contains(kw, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            if (cleanChannels.Count == 0) return null;

            return new ChannelGroup
            {
                Name = g.Name,
                Channels = new ObservableCollection<Channel>(cleanChannels)
            };
        }).Where(g => g != null).Cast<ChannelGroup>().ToList();

        return new ContentCategory
        {
            Name = source.Name,
            Type = source.Type,
            Icon = source.Icon,
            Groups = new ObservableCollection<ChannelGroup>(cleanGroups)
        };
    }

    private void ApplyCategoryFilters()
    {
        _liveTvCategory = FilterCategoryAdultContent(_fullLiveTvCategory);
        _moviesCategory = FilterCategoryAdultContent(_fullMoviesCategory);
        _seriesCategory = FilterCategoryAdultContent(_fullSeriesCategory);

        MarkFavoriteChannels();

        var movieGroups = (_moviesCategory?.Groups ?? []).ToList();
        movieGroups = OrderGroupsWithAdultAtEnd(movieGroups);
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
        seriesGroups = OrderGroupsWithAdultAtEnd(seriesGroups);
        if (seriesGroups.Count > 1)
        {
            seriesGroups.Insert(0, new ChannelGroup
            {
                Name = "Todas",
                Channels = new ObservableCollection<Channel>(seriesGroups.SelectMany(g => g.Channels))
            });
        }
        SeriesCategoryList = new ObservableCollection<ChannelGroup>(seriesGroups);

        SelectedMovieCategory = MovieCategoryList.FirstOrDefault(g => g.Name == SelectedMovieCategory?.Name) ?? MovieCategoryList.FirstOrDefault();
        SelectedSeriesCategory = SeriesCategoryList.FirstOrDefault(g => g.Name == SelectedSeriesCategory?.Name) ?? SeriesCategoryList.FirstOrDefault();

        OnPropertyChanged(nameof(LiveTvChannelCount));
        OnPropertyChanged(nameof(MovieCount));
        OnPropertyChanged(nameof(SeriesCount));
        OnPropertyChanged(nameof(TotalChannels));

        ApplyFilter();
        RefreshFavoriteLists();
        RefreshHistoryLists();
    }

    private List<ChannelGroup> OrderGroupsWithAdultAtEnd(List<ChannelGroup> groups)
    {
        return groups.OrderBy(g =>
        {
            return AdultKeywords.Any(kw => g.Name.Contains(kw, StringComparison.OrdinalIgnoreCase)) ? 1 : 0;
        }).ToList();
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

        // Sorting
        var channels = source.ToList();
        if (SelectedSortOption == 1) // A-Z
            channels = channels.OrderBy(c => c.Name).ToList();
        else if (SelectedSortOption == 2) // Z-A
            channels = channels.OrderByDescending(c => c.Name).ToList();
        // else: option 0 = Más recientes = original M3U order (newest first)

        FilteredMovies = new ObservableCollection<Channel>(channels);
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
            _seriesCatalog = null;
            FilteredSeries = new();
            return;
        }

        _seriesCatalog = new SeriesCatalog(source);

        _currentSeriesShowName = null;
        _currentSeriesSeason = 0;

        var seriesNames = _seriesCatalog.ShowNames;

        if (!_seriesCatalog.HasEpisodes)
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
            if (_seriesCatalog.Seasons.Count > 1)
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
        if (_seriesCatalog == null)
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
        var shows = _seriesCatalog!.GroupByShow()
            .Select(g =>
            {
                var seasonCount = g.Select(e => e.Season).Where(s => s > 0).Distinct().Count();
                var pending = g.Select(e => e.Channel).FirstOrDefault(c => c.IsPendingSeries);
                return new Channel
                {
                    Name = g.Key,
                    LogoUrl = SeriesCatalog.FirstLogo(g),
                    GroupTitle = pending != null
                        ? pending.GroupTitle
                        : seasonCount > 0
                            ? $"{seasonCount} temp \u00B7 {g.Count()} ep"
                            : $"{g.Count()} episodios",
                    Url = string.Empty,
                    Category = CategoryType.Series,
                    IsFavorite = _favorites.ContainsSeries(g.Key)
                };
            })
            // .OrderBy(c => c.Name) -> default preserves m3u order for latest additions at the top, we just handle Sort options:
            .ToList();

        if (SelectedSortOption == 1) // A-Z
            shows = shows.OrderBy(c => c.Name).ToList();
        else if (SelectedSortOption == 2) // Z-A
            shows = shows.OrderByDescending(c => c.Name).ToList();
        // else: option 0 = Más recientes = original M3U order (newest first)

        if (!string.IsNullOrWhiteSpace(SearchText))
            shows = shows.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();

        FilteredSeries = new ObservableCollection<Channel>(shows);
    }

    private void DisplaySeriesSeasons()
    {
        if (_currentSeriesShowName == null) return;

        var episodes = _seriesCatalog!.EpisodesOf(_currentSeriesShowName);

        // Show-level fallback logo: first episode of this show that has a logo
        var showLogo = SeriesCatalog.FirstLogo(episodes);

        var seasonItems = _seriesCatalog.SeasonsOf(_currentSeriesShowName)
            .Select(s =>
            {
                var seasonEps = episodes.Where(e => e.Season == s).ToList();
                var seasonLogo = SeriesCatalog.FirstLogo(seasonEps);
                return new Channel
                {
                    Name = $"Temporada {s}",
                    LogoUrl = string.IsNullOrEmpty(seasonLogo) ? showLogo : seasonLogo,
                    GroupTitle = $"{seasonEps.Count} episodios",
                    Url = string.Empty,
                    Category = CategoryType.Series,
                    IsFavorite = _favorites.ContainsSeries(_currentSeriesShowName ?? "")
                };
            })
            .ToList();

        FilteredSeries = new ObservableCollection<Channel>(seasonItems);
    }

    private void DisplaySeriesEpisodes()
    {
        var episodes = _seriesCatalog!.GetEpisodes(_currentSeriesShowName, _currentSeriesSeason);

        // Inherit show-level logo for episodes that have no logo of their own
        var showLogo = _currentSeriesShowName != null
            ? SeriesCatalog.FirstLogo(_seriesCatalog.EpisodesOf(_currentSeriesShowName))
            : string.Empty;

        var channels = episodes.Select(e =>
        {
            if (string.IsNullOrEmpty(e.Channel.LogoUrl) && !string.IsNullOrEmpty(showLogo))
            {
                // Clone to avoid mutating the original channel
                return new Channel
                {
                    Name = e.Channel.Name,
                    Url = e.Channel.Url,
                    LogoUrl = showLogo,
                    GroupTitle = e.Channel.GroupTitle,
                    TvgId = e.Channel.TvgId,
                    TvgName = e.Channel.TvgName,
                    Category = e.Channel.Category,
                    IsFavorite = e.Channel.IsFavorite
                };
            }
            return e.Channel;
        });

        if (!string.IsNullOrWhiteSpace(SearchText))
            channels = channels.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        FilteredSeries = new ObservableCollection<Channel>(channels);
    }

    public async Task HandleSeriesItemClickAsync(Channel item)
    {
        switch (SeriesNavLevel)
        {
            case SeriesNavLevel.Shows:
                await OpenSeriesShowAsync(item);
                return;

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
                if (item.IsPendingSeries)
                    await OpenSeriesShowAsync(item);
                else
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
                if (_seriesCatalog!.SeasonsOf(_currentSeriesShowName).Count > 1 && _currentSeriesSeason > 0)
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

    /// <summary>Abre una serie; si es de Xtream y aún no tiene episodios, primero los pide al servidor.</summary>
    [RelayCommand]
    private async Task OpenSeriesShowAsync(Channel show)
    {
        if (await EnsureSeriesEpisodesAsync(show.Name))
            NavigateToSeriesShow(show.Name);
    }

    private async Task<bool> EnsureSeriesEpisodesAsync(string showName)
    {
        var pending = _seriesCatalog?.PendingSeries(showName);
        if (pending == null || _xtream == null)
            return true;

        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            Player.StatusText = $"Cargando episodios de {showName}...";

            if (!_xtreamEpisodesCache.TryGetValue(pending.SeriesId, out var episodes))
            {
                episodes = await _xtream.LoadSeriesEpisodesAsync(pending);
                _xtreamEpisodesCache[pending.SeriesId] = episodes;
            }

            if (episodes.Count == 0)
            {
                Player.StatusText = $"{showName} no tiene episodios disponibles";
                return false;
            }

            ReplacePendingSeries(pending, episodes);
            Player.StatusText = $"{showName}: {episodes.Count} episodios";
            return true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            Player.StatusText = "No se pudieron cargar los episodios";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Pone los episodios en el lugar de la serie pendiente dentro de la categoría y rearma las vistas.</summary>
    private void ReplacePendingSeries(Channel pending, List<Channel> episodes)
    {
        foreach (var group in _fullSeriesCategory?.Groups ?? [])
        {
            var index = group.Channels.IndexOf(pending);
            if (index < 0) continue;

            group.Channels.RemoveAt(index);
            for (var i = 0; i < episodes.Count; i++)
                group.Channels.Insert(index + i, episodes[i]);
        }

        ApplyCategoryFilters();
    }

    public void NavigateToSeriesShow(string showName)
    {
        if (_seriesCatalog == null) return;

        _currentSeriesShowName = showName;
        var seasons = _seriesCatalog.SeasonsOf(showName);

        if (seasons.Count <= 1)
        {
            _currentSeriesSeason = seasons.FirstOrDefault();
            SeriesNavLevel = SeriesNavLevel.Episodes;
        }
        else
        {
            SeriesNavLevel = SeriesNavLevel.Seasons;
        }
        SeriesBreadcrumb = showName;

        SearchText = string.Empty;
        RefreshSeriesDisplay();
    }

    #endregion

    #region Playback

    [RelayCommand]
    private void PlayMovie(Channel channel) => PlayChannel(channel);

    [RelayCommand]
    private Task ClickSeriesItemAsync(Channel channel) => HandleSeriesItemClickAsync(channel);

    /// <summary>Reproduce un canal o una entrada del historial (favoritos, recientes, «Continuar viendo»).</summary>
    [RelayCommand]
    private void PlayItem(object? item)
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
        Epg.ShowChannel(channel);
        IsViewingFullList = false;
    }

    private Channel? GetNextEpisode()
        => SelectedChannel == null ? null : _seriesCatalog?.GetNextEpisode(SelectedChannel.Url);

    private void HandleEndReached()
    {
        var nextEpisode = GetNextEpisode();
        if (nextEpisode != null)
        {
            SaveCurrentWatchPosition();
            SelectedChannel = nextEpisode;
            AddToWatchHistory(nextEpisode);
            Player.PlayNextChannel(nextEpisode);
        }
        else
        {
            Player.HasMedia = false;
            Player.StatusText = "Reproducción finalizada";
        }
    }

    [RelayCommand]
    private void SkipToNextEpisode()
    {
        var nextEpisode = GetNextEpisode();
        if (nextEpisode != null)
        {
            SaveCurrentWatchPosition();
            SelectedChannel = nextEpisode;
            AddToWatchHistory(nextEpisode);
            Player.PlayNextChannel(nextEpisode);
        }
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
            IsFavorite = _favorites.Contains(entry.Url)
        };
        SelectedChannel = channel;
        _currentWatchEntry = entry;
        _history.Touch(entry);

        if (entry.Category != CategoryType.LiveTV && entry.PositionMs > 0)
            Player.PlayChannelFromPosition(channel, entry.PositionMs);
        else
            Player.PlayChannel(channel);

        Epg.ShowChannel(channel);
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

            channel.IsFavorite = _favorites.ToggleSeries(showName);
        }
        else
        {
            channel.IsFavorite = !channel.IsFavorite;
            _favorites.Set(channel.Url, channel.IsFavorite);
        }

        RefreshFavoriteLists();
    }

    private void MarkFavoriteChannels()
    {
        foreach (var channel in GetAllChannels())
            channel.IsFavorite = _favorites.Contains(channel.Url);
    }

    private void RefreshFavoriteLists()
    {
        var favChannels = GetAllChannels().Where(c => c.IsFavorite).ToList();
        LiveTvFavorites = new(favChannels.Where(c => c.Category == CategoryType.LiveTV));
        MovieFavorites = new(favChannels.Where(c => c.Category == CategoryType.Movies));

        if (_seriesCatalog != null && _favorites.HasSeries)
        {
            SeriesFavorites = new(_seriesCatalog.GroupByShow()
                .Where(g => _favorites.ContainsSeries(g.Key))
                .Select(g => new Channel
                {
                    Name = g.Key,
                    LogoUrl = SeriesCatalog.FirstLogo(g),
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
        _currentWatchEntry = _history.Record(channel);
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

        if (_history.Remove(entry.Url))
        {
            if (_currentWatchEntry?.Url.Equals(entry.Url, StringComparison.OrdinalIgnoreCase) == true)
            {
                _currentWatchEntry = null;
            }
            RefreshHistoryLists();
        }
    }

    private void RefreshHistoryLists()
    {
        RecentLiveTv = new(_history.Recent(CategoryType.LiveTV, 10));
        ContinueWatchingMovies = new(_history.ContinueWatching(CategoryType.Movies, 10));
        ContinueWatchingSeries = new(_history.ContinueWatching(CategoryType.Series, 10));

        OnPropertyChanged(nameof(HasRecentLiveTv));
        OnPropertyChanged(nameof(HasContinueWatchingMovies));
        OnPropertyChanged(nameof(HasContinueWatchingSeries));
        OnPropertyChanged(nameof(RecentLiveTvPreview));
        OnPropertyChanged(nameof(ContinueWatchingMoviesPreview));
        OnPropertyChanged(nameof(ContinueWatchingSeriesPreview));
    }

    #endregion

    #region EPG

    private async Task LoadEpgAsync(string url)
    {
        if (await Epg.LoadAsync(url) && SelectedChannel != null)
            Epg.ShowChannel(SelectedChannel);
    }

    public void RefreshEpgDisplay() => Epg.Refresh(SelectedChannel);

    #endregion

    #region Settings

    public void LoadSettings(AppSettings settings)
    {
        Sources = new ObservableCollection<PlaylistSource>(settings.Sources);
        SelectedSource = Sources.FirstOrDefault(s => s.Id == settings.ActiveSourceId) ?? Sources.FirstOrDefault();
        Player.Volume = settings.Volume;
        Player.IsMuted = settings.IsMuted;

        if (Enum.TryParse<ContentTab>(settings.ActiveTab, out var tab))
            ActiveTab = tab;

        _favorites = new Favorites(settings.FavoriteUrls, settings.FavoriteSeriesNames ?? []);
        _history = new WatchHistory(settings.WatchHistory ?? []);
        Player.MaxReconnectAttempts = settings.MaxReconnectAttempts;
        HideAdultContent = settings.HideAdultContent;
        RefreshHistoryLists();
    }

    public void SaveToSettings(AppSettings settings)
    {
        SaveCurrentWatchPosition();
        settings.Sources = Sources.ToList();
        settings.ActiveSourceId = (_loadedSource ?? SelectedSource)?.Id ?? string.Empty;
        // Ya migradas a Sources; no se vuelven a escribir.
        settings.LastPlaylistUrl = string.Empty;
        settings.RecentPlaylistUrls = [];
        settings.Volume = Player.Volume;
        settings.IsMuted = Player.IsMuted;
        settings.ActiveTab = ActiveTab.ToString();
        settings.FavoriteUrls = _favorites.Urls;
        settings.FavoriteSeriesNames = _favorites.SeriesNames;
        settings.WatchHistory = _history.Entries.ToList();
        settings.HideAdultContent = HideAdultContent;
    }

    public void ClearWatchHistory()
    {
        _history.Clear();
        _currentWatchEntry = null;
        RefreshHistoryLists();
    }

    public void ClearFavorites()
    {
        _favorites.Clear();
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

    #endregion
}
