using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.Localization;

/// <summary>
/// Que idioma toca, como en la web: el elegido y, si no hay, el del sistema
/// (español si Windows esta en español, ingles en cualquier otro caso).
/// </summary>
public class LanguagePreference(ISettingsStore store, Func<string> systemLanguage)
{
    public AppLanguage? Chosen => store.Load().Language switch
    {
        "es" => AppLanguage.Es,
        "en" => AppLanguage.En,
        _ => null,
    };

    public AppLanguage Effective =>
        Chosen ?? (systemLanguage().StartsWith("es", StringComparison.OrdinalIgnoreCase) ? AppLanguage.Es : AppLanguage.En);

    public void Save(AppLanguage language)
    {
        var settings = store.Load();
        settings.Language = language == AppLanguage.Es ? "es" : "en";
        store.Save(settings);
    }
}
