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
    private readonly ApiClient api = new();
    private readonly Session session = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShowLogin();
    }

    private void ShowLogin()
    {
        var viewModel = new LoginViewModel(api, session);
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
        var viewModel = new MainViewModel(session);
        var window = new Views.MainWindow(viewModel);
        viewModel.LoggedOut += (_, _) =>
        {
            ShowLogin();
            window.Close();
        };
        MainWindow = window;
        window.Show();
    }
}
