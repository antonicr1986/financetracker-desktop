using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace FinanceTracker.Desktop.Services;

/// <summary>
/// Aplica el tema en WPF: cambia el diccionario de colores de la aplicacion y
/// la barra de titulo de cada ventana. La decision de que tema toca esta en
/// ThemePreference; aqui solo se pinta.
/// </summary>
public class ThemeManager
{
    private readonly ThemePreference preference;

    public ThemeManager(ThemePreference preference)
    {
        this.preference = preference;

        // Cada ventana que se abra, en cuanto se cargue, recibe su barra de titulo.
        EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
            new RoutedEventHandler((sender, _) => ApplyTitleBar((Window)sender)));

        // Si el usuario no ha elegido y cambia el tema de Windows, se le sigue.
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && preference.Chosen is null)
                Application.Current.Dispatcher.Invoke(() => Apply(preference.Effective));
        };
    }

    public AppTheme Current { get; private set; }

    public void ApplyInitial() => Apply(preference.Effective);

    public void Toggle() => Apply(preference.Toggle());

    private void Apply(AppTheme theme)
    {
        Current = theme;

        // App.xaml tiene dos diccionarios: [0] los colores y [1] los controles.
        // Solo se sustituye el primero; todo lo que use DynamicResource se repinta.
        var file = theme == AppTheme.Dark ? "Dark" : "Light";
        Application.Current.Resources.MergedDictionaries[0] = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/Themes/{file}.xaml"),
        };

        foreach (Window window in Application.Current.Windows)
            ApplyTitleBar(window);
    }

    /// <summary>Lee el ajuste "Modo de aplicacion" de Windows (Personalizacion > Colores).</summary>
    public static bool SystemPrefersDark()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
    }

    // La barra de titulo no es de WPF, la pinta Windows. Para ponerla oscura
    // hay que pedirselo a Windows directamente (Windows 10 20H1 y posteriores).
    private const int DwmUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private void ApplyTitleBar(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var dark = Current == AppTheme.Dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DwmUseImmersiveDarkMode, ref dark, sizeof(int));
    }
}
