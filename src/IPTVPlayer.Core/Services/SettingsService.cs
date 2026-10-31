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
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
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
