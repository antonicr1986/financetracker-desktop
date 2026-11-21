using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

public enum ForgotPasswordStep
{
    Email,
    Code,
    Done
}

/// <summary>
/// "¿Has olvidado tu contraseña?", como en la web y Android: primero el correo,
/// despues el codigo que llega con la contraseña nueva. No inicia sesion: la
/// API cierra todas al cambiarla y se vuelve al acceso.
/// </summary>
public partial class ForgotPasswordViewModel : ObservableObject, IDisposable
{
    private readonly IAuthService auth;
    private readonly Localizer localizer;

    // Como escribir el error y el aviso actuales: se reescriben al cambiar de idioma.
    private Func<string>? error;
    private Func<string>? status;

    /// <summary>Correo al que se pidio el codigo.</summary>
    private string sentTo = "";

    public ForgotPasswordViewModel(IAuthService auth, Localizer localizer)
    {
        this.auth = auth;
        this.localizer = localizer;
        localizer.Changed += OnLanguageChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEmailStep))]
    [NotifyPropertyChangedFor(nameof(IsCodeStep))]
    [NotifyPropertyChangedFor(nameof(IsDone))]
    [NotifyPropertyChangedFor(nameof(ShowSignInLink))]
    private ForgotPasswordStep step = ForgotPasswordStep.Email;

    public bool IsEmailStep => Step == ForgotPasswordStep.Email;
    public bool IsCodeStep => Step == ForgotPasswordStep.Code;
    public bool IsDone => Step == ForgotPasswordStep.Done;

    /// <summary>El enlace "Iniciar sesion" bajo la tarjeta; al terminar ya lo es el boton principal.</summary>
    public bool ShowSignInLink => !IsDone;

    [ObservableProperty] private string email = "";
    [ObservableProperty] private string code = "";

    // Las PasswordBox no admiten binding: las rellena el code-behind.
    public string Password { get; set; } = "";
    public string Confirmation { get; set; } = "";

    [ObservableProperty] private string errorMessage = "";
    [ObservableProperty] private string statusMessage = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendCodeCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResendCodeCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCommand))]
    private bool isBusy;

    /// <summary>"Iniciar sesion": volver al acceso.</summary>
    public event EventHandler? SignInRequested;

    [RelayCommand]
    private void GoToSignIn() => SignInRequested?.Invoke(this, EventArgs.Empty);

    private bool CanSend() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendCodeAsync()
    {
        SetError(null);
        var typed = Email.Trim();

        if (!RegistrationForm.IsValidEmail(typed))
        {
            SetError(() => localizer.T("errors.invalidEmail"));
            return;
        }

        await RequestCodeAsync(typed);
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private Task ResendCodeAsync()
    {
        SetError(null);
        return RequestCodeAsync(sentTo);
    }

    private async Task RequestCodeAsync(string to)
    {
        IsBusy = true;
        try
        {
            await auth.ForgotPasswordAsync(to);
            sentTo = to;
            SetStatus(() => localizer.T("passwordReset.codeSent", to));
            Step = ForgotPasswordStep.Code;
        }
        catch (ApiException e)
        {
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task ResetAsync()
    {
        SetError(null);
        var typedCode = Code.Trim();

        // Las mismas reglas que la API (ResetPasswordDto): se evita una
        // peticion para un error evidente.
        string? problem =
            typedCode.Length != 6 || !typedCode.All(char.IsAsciiDigit) ? "emailVerification.codeFormat"
            : Password.Length < RegistrationForm.MinPasswordLength ? "errors.passwordTooShort"
            : Password != Confirmation ? "errors.passwordsDontMatch"
            : null;

        if (problem is { } key)
        {
            SetError(() => localizer.T(key));
            return;
        }

        IsBusy = true;
        try
        {
            await auth.ResetPasswordAsync(sentTo, typedCode, Password);
            SetStatus(null);
            Step = ForgotPasswordStep.Done;
        }
        catch (ApiException e)
        {
            // Codigo gastado o caducado: se borra para escribir el de un correo nuevo.
            if (e.Code == ApiException.ResetCodeExpired) Code = "";
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToEmail()
    {
        Code = "";
        SetError(null);
        SetStatus(null);
        Step = ForgotPasswordStep.Email;
    }

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    private void SetStatus(Func<string>? text)
    {
        status = text;
        StatusMessage = text?.Invoke() ?? "";
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        SetError(error);
        SetStatus(status);
    }

    /// <summary>Deja de escuchar al Localizer, que vive mas que esta pantalla.</summary>
    public void Dispose() => localizer.Changed -= OnLanguageChanged;
}
