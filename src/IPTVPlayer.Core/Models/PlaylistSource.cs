using System.Text.Json.Serialization;
using IPTVPlayer.Services;

namespace IPTVPlayer.Models;

public enum PlaylistSourceType
{
    M3U,
    Xtream
}

/// <summary>
/// Una fuente de contenido guardada: una lista M3U (URL o archivo local) o una cuenta Xtream Codes.
/// </summary>
public class PlaylistSource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public PlaylistSourceType Type { get; set; } = PlaylistSourceType.M3U;

    /// <summary>M3U: URL o ruta del archivo. Xtream: URL del servidor, por ejemplo http://servidor:8080.</summary>
    public string Url { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    /// <summary>Contraseña de Xtream en claro. No se guarda: en disco va <see cref="ProtectedPassword"/>.</summary>
    [JsonIgnore]
    public string Password { get; set; } = string.Empty;

    /// <summary>La contraseña cifrada para el usuario actual de Windows (DPAPI); es lo que se guarda en settings.json.</summary>
    public string ProtectedPassword
    {
        get => SecretProtector.Protect(Password);
        set => Password = SecretProtector.Unprotect(value);
    }

    [JsonIgnore]
    public bool IsXtream => Type == PlaylistSourceType.Xtream;

    public override string ToString() => Name;

    /// <summary>Un nombre legible para una URL o ruta: el dominio sin «www.» o el nombre del archivo.</summary>
    public static string SuggestName(string urlOrPath)
    {
        var value = urlOrPath.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            var host = uri.Host;
            return host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;
        }

        var fileName = Path.GetFileNameWithoutExtension(value);
        return string.IsNullOrWhiteSpace(fileName) ? value : fileName;
    }
}
