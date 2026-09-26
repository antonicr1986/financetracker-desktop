using System.Globalization;
using System.Windows;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using FinanceTracker.Desktop.Views;

namespace FinanceTracker.Desktop;

/// <summary>
/// Punto de entrada. Crea los servicios una sola vez y decide que ventana
/// mostrar. Los ViewModels no abren ventanas: avisan con un evento y es aqui
/// donde se navega.
/// </summary>
public partial class App : Application
{
    // La sesion se guarda cifrada entre ejecuciones (DPAPI): no hay que volver
    // a iniciar sesion cada vez que se abre la aplicacion.
    private readonly Session session = new(new DpapiSessionStore());
    private readonly ApiClient api;
    private readonly DialogService dialogs = new();
    private readonly Localizer localizer = new();
    private readonly SettingsStore settings = new();

    /// <summary>Tema de la aplicacion. Estatico porque es uno para todas las ventanas.</summary>
    public static ThemeManager Theme { get; private set; } = null!;

    /// <summary>Idioma de la aplicacion. Lo usa el selector de la barra superior.</summary>
    public static LanguageManager Languages { get; private set; } = null!;

    public App()
    {
        api = new ApiClient(session);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Antes de abrir ninguna ventana, para que la primera ya salga con su
        // tema y su idioma.
        Theme = new ThemeManager(new ThemePreference(settings, ThemeManager.SystemPrefersDark));
        Theme.ApplyInitial();

        // El idioma del sistema se lee antes de que LanguageManager cambie la
        // cultura del proceso.
        var systemLanguage = CultureInfo.CurrentUICulture.Name;
        Languages = new LanguageManager(localizer, new LanguagePreference(settings, () => systemLanguage));
        Languages.ApplyInitial();

        // Con una sesion guardada y valida, directo al panel. Si el token ya
        // caduco, al login con el aviso, como cuando caduca con la app abierta.
        switch (session.TryRestore(DateTime.UtcNow))
        {
            case SessionRestore.Restored:
                ShowMain();
                break;
            case SessionRestore.Expired:
                ShowLogin(sessionExpired: true);
                break;
            default:
                ShowLogin();
                break;
        }
    }

    private void ShowLogin(bool sessionExpired = false)
    {
        var viewModel = new LoginViewModel(api, session, localizer);
        if (sessionExpired) viewModel.ShowSessionExpired();

        var window = new LoginWindow(viewModel);
        window.Closed += (_, _) => viewModel.Dispose();
        viewModel.LoggedIn += (_, _) =>
        {
            ShowMain();
            window.Close();
        };
        viewModel.RegisterRequested += (_, _) =>
        {
            ShowRegister();
            window.Close();
        };
        MainWindow = window;
        window.Show();
    }

    private void ShowRegister()
    {
        var viewModel = new RegisterViewModel(api, session, localizer);
        var window = new RegisterWindow(viewModel);
        window.Closed += (_, _) => viewModel.Dispose();

        // Cuenta creada y sesion iniciada: directo al panel, como la web.
        viewModel.LoggedIn += (_, _) =>
        {
            ShowMain();
            window.Close();
        };
        viewModel.SignInRequested += (_, _) =>
        {
            ShowLogin();
            window.Close();
        };
        MainWindow = window;
        window.Show();
    }

    private void ShowMain()
    {
        var viewModel = new MainViewModel(session, api, api, api, dialogs, localizer, settings);
        var window = new Views.MainWindow(viewModel);
        window.Closed += (_, _) => viewModel.Dispose();

        viewModel.LoggedOut += (_, _) =>
        {
            ShowLogin();
            window.Close();
        };

        // Un 401 en el panel no es una contrasena incorrecta: habia un token y
        // la API lo ha rechazado (caduca a los 60 minutos).
        viewModel.SessionExpired += (_, _) =>
        {
            ShowLogin(sessionExpired: true);
            window.Close();
        };

        MainWindow = window;
        window.Show();
        viewModel.LoadCommand.Execute(null);
    }
}
