using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Crear cuenta, como en la web y Android: se valida en el cliente, se registra
/// y, como la API devuelve el usuario y no un token, se inicia sesion con las
/// mismas credenciales para entrar directamente al panel.
/// </summary>
public partial class RegisterViewModel : ObservableObject, IDisposable
{
    private readonly IAuthService auth;
    private readonly Session session;
    private readonly Localizer localizer;

    /// <summary>Como escribir el error actual; ver LoginViewModel.</summary>
    private Func<string>? error;

    public RegisterViewModel(IAuthService auth, Session session, Localizer localizer)
    {
        this.auth = auth;
        this.session = session;
        this.localizer = localizer;
        localizer.Changed += OnLanguageChanged;
    }

    [ObservableProperty] private string name = "";
    [ObservableProperty] private string email = "";

    // Las PasswordBox no admiten binding: las rellena el code-behind.
    public string Password { get; set; } = "";
    public string Confirmation { get; set; } = "";

    [ObservableProperty] private string errorMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool isBusy;

    /// <summary>Cuenta creada y sesion iniciada: la ventana navega al panel.</summary>
    public event EventHandler? LoggedIn;

    /// <summary>"¿Ya tienes cuenta? Entrar": volver al login.</summary>
    public event EventHandler? SignInRequested;

    [RelayCommand]
    private void GoToSignIn() => SignInRequested?.Invoke(this, EventArgs.Empty);

    private bool CanRegister() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        SetError(null);

        var trimmedName = Name.Trim();
        var trimmedEmail = Email.Trim();

        var problem = RegistrationForm.Validate(trimmedName, trimmedEmail, Password, Confirmation);
        if (problem is not null)
        {
            SetError(() => localizer.T(problem switch
            {
                RegistrationProblem.MissingFields => "errors.fillAll",
                RegistrationProblem.InvalidEmail => "errors.invalidEmail",
                RegistrationProblem.PasswordTooShort => "errors.passwordTooShort",
                _ => "errors.passwordsDontMatch",
            }));
            return;
        }

        IsBusy = true;
        try
        {
            await auth.RegisterAsync(trimmedName, trimmedEmail, Password);

            // La API no devuelve token al registrarse: se entra con lo mismo.
            var login = await auth.LoginAsync(trimmedEmail, Password);
            session.Start(login);
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e)
        {
            SetError(() => e.Code is ApiException.EmailAlreadyExists or ApiException.NetworkError
                ? localizer.ApiError(e.Code)
                : localizer.T("errors.createAccountFailed"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => SetError(error);

    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}
