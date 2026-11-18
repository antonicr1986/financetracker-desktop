using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Token de dispositivo de confianza del MFA ("no volver a pedir el codigo en
/// este dispositivo durante 30 dias"). Aparte de la sesion a proposito: cerrar
/// sesion no lo borra, porque lo que es de confianza es el equipo. Lo borran
/// "Olvidar todos los dispositivos" y desactivar el MFA.
/// </summary>
public interface ITrustedDeviceStore
{
    string? Load();

    /// <summary>null lo borra.</summary>
    void Save(string? token);
}

/// <summary>
/// Cifrado con DPAPI, como la sesion (ver DpapiSessionStore), en
/// %LocalAppData%\FinanceTracker\trusted-device.dat.
/// </summary>
public class DpapiTrustedDeviceStore(string? path = null) : ITrustedDeviceStore
{
    private static readonly byte[] Entropy = "FinanceTracker.Desktop.TrustedDevice.v1"u8.ToArray();

    private readonly string path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FinanceTracker", "trusted-device.dat");

    public string? Load()
    {
        try
        {
            if (!File.Exists(path)) return null;
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception e) when (e is CryptographicException or IOException or UnauthorizedAccessException)
        {
            // Roto o de otra cuenta: como si no hubiera, y se pedira el codigo.
            Save(null);
            return null;
        }
    }

    public void Save(string? token)
    {
        try
        {
            if (token is null)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), Entropy, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(path, bytes);
        }
        catch (Exception e) when (e is CryptographicException or IOException or UnauthorizedAccessException)
        {
            // Si no se puede guardar, simplemente se volvera a pedir el codigo.
        }
    }
}

/// <summary>En memoria, para los tests.</summary>
public class MemoryTrustedDeviceStore : ITrustedDeviceStore
{
    private string? token;

    public string? Load() => token;

    public void Save(string? value) => token = value;
}
