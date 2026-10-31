using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "IPTVPlayer.Tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SaveYLoad_ConservanLaConfiguracionPorDefecto()
    {
        // Una instalación nueva no tiene posición de ventana: WindowLeft y WindowTop valen NaN.
        SettingsService.Save(new AppSettings(), SettingsPath);

        Assert.True(File.Exists(SettingsPath));
        var loaded = SettingsService.Load(SettingsPath);
        Assert.True(double.IsNaN(loaded.WindowLeft));
        Assert.True(double.IsNaN(loaded.WindowTop));
        Assert.Equal(80, loaded.Volume);
    }
}
