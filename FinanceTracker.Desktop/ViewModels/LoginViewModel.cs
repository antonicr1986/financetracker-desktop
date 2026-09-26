using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

public partial class LoginViewModel(IAuthService auth, Session session) : ObservableObject
{
    public const string DemoEmail = "demo@financetracker.app";
    public const string DemoPassword = "Demo1234!"; // gitleaks:allow — cuenta demo publica a proposito

    [ObservableProperty]
    private string email = "";

    // La PasswordBox de WPF no admite binding (por seguridad), asi que esta
    // propiedad la rellena el code-behind de LoginWindow.
    public string Password { get; set; } = "";

    [ObservableProperty]
    private string errorMessage = "";

    // NotifyCanExecuteChangedFor: cada vez que cambia IsBusy, el boton vuelve
    // a preguntar si puede ejecutarse, y se deshabilita mientras se espera.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private bool isBusy;

    /// <summary>Se lanza al iniciar sesion. La ventana lo escucha para navegar.</summary>
    public event EventHandler? LoggedIn;

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = "";

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            ErrorMessage = "Escribe el email y la contraseña.";
            return;
        }

        IsBusy = true;
        try
        {
            var login = await auth.LoginAsync(Email.Trim(), Password);
            session.Start(login);
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e)
        {
            ErrorMessage = e.Code switch
            {
                ApiException.InvalidCredentials => "Email o contraseña incorrectos.",
                ApiException.NetworkError => "No se pudo conectar con el servidor. Revisa tu conexión.",
                _ => "Algo ha fallado. Inténtalo de nuevo.",
            };
        }
        finally
        {
            IsBusy = false;
        }
    }
}
