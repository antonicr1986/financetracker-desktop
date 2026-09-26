namespace FinanceTracker.Desktop.Services;

public enum AppTheme
{
    Light,
    Dark,
}

/// <summary>
/// Que tema toca, sin nada de WPF, para poder probarlo. Igual que Android:
/// mientras el usuario no elige, se sigue al tema de Windows; en cuanto pulsa
/// el boton, su eleccion se guarda y manda.
/// </summary>
public class ThemePreference(ISettingsStore store, Func<bool> systemPrefersDark)
{
    /// <summary>La elegida por el usuario, o null si nunca ha pulsado el boton.</summary>
    public AppTheme? Chosen => store.Load().Theme switch
    {
        "light" => AppTheme.Light,
        "dark" => AppTheme.Dark,
        _ => null,
    };

    public AppTheme Effective => Chosen ?? (systemPrefersDark() ? AppTheme.Dark : AppTheme.Light);

    /// <summary>Pasa al otro tema y lo guarda. Devuelve el nuevo.</summary>
    public AppTheme Toggle()
    {
        var next = Effective == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        var settings = store.Load();
        settings.Theme = next == AppTheme.Dark ? "dark" : "light";
        store.Save(settings);
        return next;
    }
}
