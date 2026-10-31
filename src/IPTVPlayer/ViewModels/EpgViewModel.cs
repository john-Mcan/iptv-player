using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.ViewModels;

/// <summary>Guía de programación del canal en reproducción: próximos programas, el actual y el siguiente.</summary>
public partial class EpgViewModel : ObservableObject
{
    private readonly EpgService _service;
    private readonly Action<string> _reportStatus;

    public EpgViewModel(Action<string> reportStatus) : this(new EpgService(), reportStatus)
    {
    }

    public EpgViewModel(EpgService service, Action<string> reportStatus)
    {
        _service = service;
        _reportStatus = reportStatus;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasEpgData))]
    private ObservableCollection<EpgProgram> _currentEpgProgrammes = new();

    [ObservableProperty]
    private EpgProgram? _currentProgram;

    [ObservableProperty]
    private EpgProgram? _nextProgram;

    [ObservableProperty]
    private bool _isEpgLoading;

    public bool HasEpgData => CurrentEpgProgrammes.Count > 0;

    /// <summary>Descarga la guía de <paramref name="url"/> y devuelve si se pudo cargar.</summary>
    public async Task<bool> LoadAsync(string url)
    {
        try
        {
            IsEpgLoading = true;
            _reportStatus("Cargando EPG...");
            await _service.LoadAsync(url);
            _reportStatus("EPG cargada");
            return true;
        }
        catch
        {
            _reportStatus("No se pudo cargar EPG");
            return false;
        }
        finally
        {
            IsEpgLoading = false;
        }
    }

    /// <summary>Muestra la programación de <paramref name="channel"/>, vinculada por tvg-id o, si no tiene, por tvg-name.</summary>
    public void ShowChannel(Channel? channel)
    {
        if (channel == null || !_service.IsLoaded)
        {
            CurrentEpgProgrammes = new();
            CurrentProgram = null;
            NextProgram = null;
            return;
        }

        var tvgId = !string.IsNullOrEmpty(channel.TvgId) ? channel.TvgId : channel.TvgName;
        CurrentEpgProgrammes = new(_service.GetProgramsForChannel(tvgId));
        CurrentProgram = _service.GetCurrentProgram(tvgId);
        NextProgram = _service.GetNextProgram(tvgId);
    }

    /// <summary>Vuelve a calcular lo que está al aire; se llama cada minuto.</summary>
    public void Refresh(Channel? channel)
    {
        if (channel != null && _service.IsLoaded)
            ShowChannel(channel);
    }
}
