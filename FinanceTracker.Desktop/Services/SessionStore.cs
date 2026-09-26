using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Services;

/// <summary>Donde se guarda la sesion entre una ejecucion y la siguiente.</summary>
public interface ISessionStore
{
    LoginResponse? Load();
    void Save(LoginResponse login);
    void Clear();
}

/// <summary>
/// Guarda la sesion cifrada con DPAPI (ProtectedData) en
/// %LocalAppData%\FinanceTracker\session.dat.
///
/// DPAPI es el mecanismo de Windows para guardar secretos de una aplicacion:
/// cifra con una clave ligada a la cuenta de Windows del usuario, que Windows
/// guarda por el. Copiar el archivo a otro equipo o a otra cuenta no sirve:
/// alli no se puede descifrar. No hay ninguna clave en el codigo.
/// </summary>
public class DpapiSessionStore(string? path = null) : ISessionStore
{
    // "Entropia" adicional: otra aplicacion del mismo usuario que llamara a
    // DPAPI con el mismo archivo tampoco podria descifrarlo sin este valor.
    private static readonly byte[] Entropy = "FinanceTracker.Desktop.Session.v1"u8.ToArray();

    private readonly string path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FinanceTracker", "session.dat");

    public LoginResponse? Load()
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = ProtectedData.Unprotect(File.ReadAllBytes(path), Entropy, DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<LoginResponse>(json);
        }
        catch (Exception e) when (e is CryptographicException or JsonException or IOException or UnauthorizedAccessException)
        {
            // Roto, de otra cuenta o de otra version: como si no hubiera sesion.
            Clear();
            return null;
        }
    }

    public void Save(LoginResponse login)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var json = JsonSerializer.SerializeToUtf8Bytes(login);
            File.WriteAllBytes(path, ProtectedData.Protect(json, Entropy, DataProtectionScope.CurrentUser));
        }
        catch (Exception e) when (e is CryptographicException or IOException or UnauthorizedAccessException)
        {
            // Si no se puede guardar, la sesion dura lo que dure la aplicacion.
        }
    }

    public void Clear()
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Nada mas que hacer: el token caduca solo a los 60 minutos.
        }
    }
}
