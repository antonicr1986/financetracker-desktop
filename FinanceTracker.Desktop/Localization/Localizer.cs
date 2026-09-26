using System.Globalization;

namespace FinanceTracker.Desktop.Localization;

public enum AppLanguage
{
    Es,
    En,
}

/// <summary>
/// El idioma en uso: da los textos y el formato de importes y fechas. Sin WPF,
/// para poder probarlo; los ViewModels lo reciben en el constructor y se
/// suscriben a Changed para repintar sus textos.
/// </summary>
public class Localizer(AppLanguage language = AppLanguage.Es)
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-ES");

    // en-GB y no en-US, como la web: fechas dia/mes. Pero en-GB pondria libras,
    // asi que se clona y se fija el euro: "€1,234.56".
    private static readonly CultureInfo English = CreateEnglish();

    private static CultureInfo CreateEnglish()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-GB").Clone();
        culture.NumberFormat.CurrencySymbol = "€";
        return CultureInfo.ReadOnly(culture);
    }

    public AppLanguage Language { get; private set; } = language;

    public CultureInfo Culture => Language == AppLanguage.Es ? Spanish : English;

    public IReadOnlyDictionary<string, string> Texts => Language == AppLanguage.Es ? Strings.Es : Strings.En;

    /// <summary>Se lanza al cambiar de idioma, despues de cambiarlo.</summary>
    public event EventHandler? Changed;

    public void SetLanguage(AppLanguage value)
    {
        if (value == Language) return;
        Language = value;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Texto de una clave. Si faltara, se ve la clave: fallo visible, no silencioso.</summary>
    public string T(string key) => Texts.TryGetValue(key, out var text) ? text : key;

    /// <summary>Texto con huecos {0}, {1}… rellenados con el formato del idioma.</summary>
    public string T(string key, params object[] args) => string.Format(Culture, T(key), args);

    /// <summary>Mensaje para un codigo de error de la API.</summary>
    public string ApiError(string code) =>
        Texts.TryGetValue("apiError." + code, out var text) ? text : T("errors.unknown");
}
