using System.Security.Cryptography;
using System.Text;

namespace IPTVPlayer.Services;

/// <summary>
/// Cifra datos sensibles (contraseñas de Xtream) antes de guardarlos en settings.json, con DPAPI:
/// solo el mismo usuario de Windows, en el mismo equipo, puede descifrarlos.
/// </summary>
public static class SecretProtector
{
    private static readonly byte[] Entropy = "Chucao.PlaylistSource"u8.ToArray();

    public static string Protect(string value)
    {
        if (string.IsNullOrEmpty(value) || !OperatingSystem.IsWindows())
            return string.Empty;

        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>Descifra lo que produjo <see cref="Protect"/>; si no se puede (otro usuario, dato dañado), devuelve vacío.</summary>
    public static string Unprotect(string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue) || !OperatingSystem.IsWindows())
            return string.Empty;

        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(protectedValue), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return string.Empty;
        }
    }
}
