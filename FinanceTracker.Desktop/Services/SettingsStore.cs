using System.IO;
using System.Text.Json;

namespace FinanceTracker.Desktop.Services;

/// <summary>Preferencias del usuario que sobreviven a cerrar la aplicacion.</summary>
public class AppSettings
{
    /// <summary>"light", "dark" o null (sin elegir: sigue a Windows).</summary>
    public string? Theme { get; set; }

    /// <summary>"es", "en" o null (sin elegir: el idioma de Windows).</summary>
    public string? Language { get; set; }
}

public interface ISettingsStore
{
    AppSettings Load();
    void Save(AppSettings settings);
}

/// <summary>
/// Guarda las preferencias en %LocalAppData%\FinanceTracker\settings.json, la
/// carpeta que Windows reserva para los datos de cada aplicacion y usuario.
/// Separado de la sesion: cerrar sesion no debe reiniciar el tema.
/// </summary>
public class SettingsStore(string? path = null) : ISettingsStore
{
    private readonly string path = path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FinanceTracker", "settings.json");

    public AppSettings Load()
    {
        try
        {
            return File.Exists(path)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path)) ?? new AppSettings()
                : new AppSettings();
        }
        catch (Exception e) when (e is IOException or JsonException or UnauthorizedAccessException)
        {
            // Un archivo roto o ilegible no debe impedir abrir la aplicacion.
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // Si no se puede guardar, el tema dura lo que dure la sesion.
        }
    }
}
