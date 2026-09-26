using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace FinanceTracker.Desktop.Localization;

/// <summary>
/// Aplica el idioma en WPF, igual que ThemeManager aplica el tema: convierte
/// los textos del idioma en un diccionario de recursos y lo pone en el hueco
/// [2] de App.xaml. Todo lo que use {DynamicResource clave} cambia al momento.
/// </summary>
public class LanguageManager
{
    private readonly Localizer localizer;
    private readonly LanguagePreference preference;

    public LanguageManager(Localizer localizer, LanguagePreference preference)
    {
        this.localizer = localizer;
        this.preference = preference;

        // Cada ventana nueva recibe el idioma: lo usan el DatePicker y el
        // calendario para los nombres de meses y dias.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => ApplyTo((Window)sender)));
    }

    public Localizer Localizer => localizer;

    public void ApplyInitial()
    {
        localizer.SetLanguage(preference.Effective);
        Apply();
    }

    public void Set(AppLanguage language)
    {
        preference.Save(language);
        localizer.SetLanguage(language);
        Apply();
    }

    private void Apply()
    {
        var dictionary = new ResourceDictionary();
        foreach (var (key, text) in localizer.Texts)
            dictionary[key] = text;

        // [0] colores, [1] controles, [2] textos.
        Application.Current.Resources.MergedDictionaries[2] = dictionary;

        // Formato por defecto de lo que WPF formatea solo (el texto del DatePicker).
        CultureInfo.CurrentCulture = localizer.Culture;
        CultureInfo.CurrentUICulture = localizer.Culture;
        CultureInfo.DefaultThreadCurrentCulture = localizer.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = localizer.Culture;

        foreach (Window window in Application.Current.Windows)
            ApplyTo(window);
    }

    private void ApplyTo(Window window) =>
        window.Language = XmlLanguage.GetLanguage(localizer.Culture.IetfLanguageTag);
}
