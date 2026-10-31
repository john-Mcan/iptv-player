using System.Text.Json;
using System.Text.Json.Serialization;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

/// <summary>Lee y guarda la configuración como JSON en %APPDATA%\IPTVPlayer\settings.json.</summary>
public static class SettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IPTVPlayer");

    public static readonly string DefaultPath = Path.Combine(SettingsDir, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // WindowLeft y WindowTop valen NaN hasta que la ventana se cierra por primera vez.
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
    };

    public static AppSettings Load() => Load(DefaultPath);

    public static void Save(AppSettings settings) => Save(settings, DefaultPath);

    /// <summary>Lee la configuración; si el archivo no existe o está dañado, devuelve los valores por defecto.</summary>
    public static AppSettings Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AppSettings();

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            MigrateLegacyPlaylists(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>
    /// Convierte las URLs de listas que guardaban las versiones anteriores (última y recientes)
    /// en fuentes M3U, y deja como activa la última que se cargó.
    /// </summary>
    internal static void MigrateLegacyPlaylists(AppSettings settings)
    {
        if (settings.Sources.Count > 0)
            return;

        var urls = new[] { settings.LastPlaylistUrl }
            .Concat(settings.RecentPlaylistUrls ?? [])
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var url in urls)
        {
            var name = PlaylistSource.SuggestName(url);
            var duplicates = settings.Sources.Count(s =>
                s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)
                || s.Name.StartsWith(name + " (", StringComparison.OrdinalIgnoreCase));
            settings.Sources.Add(new PlaylistSource
            {
                Name = duplicates == 0 ? name : $"{name} ({duplicates + 1})",
                Type = PlaylistSourceType.M3U,
                Url = url
            });
        }

        var last = settings.Sources.FirstOrDefault(s => s.Url.Equals(settings.LastPlaylistUrl.Trim(), StringComparison.OrdinalIgnoreCase));
        settings.ActiveSourceId = last?.Id ?? string.Empty;
    }

    public static void Save(AppSettings settings, string path)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(path, json);
        }
        catch
        {
            // Non-critical — silently ignore write failures
        }
    }
}
