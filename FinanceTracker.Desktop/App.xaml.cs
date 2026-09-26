using System.Windows;
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
    private readonly Session session = new();
    private readonly ApiClient api;

    public App()
    {
        api = new ApiClient(session);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShowLogin();
    }

    private void ShowLogin(string? message = null)
    {
        var viewModel = new LoginViewModel(api, session);
        if (message is not null) viewModel.ErrorMessage = message;

        var window = new LoginWindow(viewModel);
        viewModel.LoggedIn += (_, _) =>
        {
            ShowMain();
            window.Close();
        };
        MainWindow = window;
        window.Show();
    }

    private void ShowMain()
    {
        var viewModel = new MainViewModel(session, api);
        var window = new Views.MainWindow(viewModel);

        viewModel.LoggedOut += (_, _) =>
        {
            ShowLogin();
            window.Close();
        };

        // Un 401 en el panel no es una contrasena incorrecta: habia un token y
        // la API lo ha rechazado (caduca a los 60 minutos).
        viewModel.SessionExpired += (_, _) =>
        {
            ShowLogin("Tu sesión ha caducado. Vuelve a iniciar sesión.");
            window.Close();
        };

        MainWindow = window;
        window.Show();
        viewModel.LoadCommand.Execute(null);
    }
}
