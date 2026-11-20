using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;

namespace FinanceTracker.Desktop.ViewModels;

public partial class LoginViewModel : ObservableObject, IDisposable
{
    public const string DemoEmail = "demo@financetracker.app";
    public const string DemoPassword = "Demo1234!"; // gitleaks:allow — cuenta demo publica a proposito

    private readonly IAuthService auth;
    private readonly Session session;
    private readonly Localizer localizer;
    private readonly IGoogleSignIn? googleSignIn;

    // El error se guarda como "como escribirlo", no como texto: si se cambia de
    // idioma con un error en pantalla, se vuelve a escribir en el nuevo.
    private Func<string>? error;

    public LoginViewModel(IAuthService auth, Session session, Localizer localizer, TimeSpan? wakingNoticeDelay = null,
        IGoogleSignIn? googleSignIn = null)
    {
        this.auth = auth;
        this.session = session;
        this.localizer = localizer;
        this.googleSignIn = googleSignIn;
        WakingNotice = new WakingNotice(wakingNoticeDelay);
        localizer.Changed += OnLanguageChanged;
    }

    /// <summary>"El servidor esta despertando" si entrar tarda mas de unos segundos.</summary>
    public WakingNotice WakingNotice { get; }

    [ObservableProperty]
    private string email = "";

    // La PasswordBox de WPF no admite binding (por seguridad), asi que esta
    // propiedad la rellena el code-behind de LoginWindow.
    public string Password { get; set; } = "";

    [ObservableProperty]
    private string errorMessage = "";

    /// <summary>
    /// true cuando la contraseña ya es correcta y falta el codigo del MFA: la
    /// ventana cambia el formulario por el del codigo.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordStep))]
    private bool isMfaStep;

    /// <summary>
    /// true cuando la contraseña es correcta pero el correo esta sin verificar:
    /// falta el codigo de 6 digitos que la API ha enviado al correo.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPasswordStep))]
    private bool isEmailStep;

    public bool IsPasswordStep => !IsMfaStep && !IsEmailStep;

    [ObservableProperty]
    private string emailCode = "";

    /// <summary>"Te hemos enviado un codigo a ..." (se reescribe al cambiar de idioma).</summary>
    [ObservableProperty]
    private string emailStepBody = "";

    /// <summary>Resultado de "Reenviar codigo".</summary>
    [ObservableProperty]
    private string resendMessage = "";

    /// <summary>Token temporal del paso del correo (30 minutos) y correo al que se envio.</summary>
    private string? verificationToken;
    private string verificationEmail = "";

    [ObservableProperty]
    private string mfaCode = "";

    /// <summary>"No volver a pedir el codigo en este equipo durante 30 dias". Desmarcada por defecto.</summary>
    [ObservableProperty]
    private bool rememberDevice;

    /// <summary>Token temporal del segundo paso (5 minutos).</summary>
    private string? mfaToken;

    // NotifyCanExecuteChangedFor: cada vez que cambia IsBusy, los botones
    // vuelven a preguntar si pueden ejecutarse, y se deshabilitan mientras se espera.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoginDemoCommand))]
    [NotifyCanExecuteChangedFor(nameof(LoginGoogleCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyMfaCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerifyEmailCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResendEmailCodeCommand))]
    private bool isBusy;

    /// <summary>Se lanza al iniciar sesion. La ventana lo escucha para navegar.</summary>
    public event EventHandler? LoggedIn;

    /// <summary>"¿No tienes cuenta? Crear una": ir al registro.</summary>
    public event EventHandler? RegisterRequested;

    [RelayCommand]
    private void GoToRegister() => RegisterRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Al volver del panel porque el token caduco.</summary>
    public void ShowSessionExpired() => SetError(() => localizer.ApiError(ApiException.SessionExpired));

    private void SetError(Func<string>? text)
    {
        error = text;
        ErrorMessage = text?.Invoke() ?? "";
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        SetError(error);
        if (IsEmailStep) EmailStepBody = localizer.T("emailVerification.body", verificationEmail);
    }

    /// <summary>
    /// Pasa directamente al paso del codigo del correo. Lo usa el registro:
    /// la cuenta ya esta creada y solo falta verificarla.
    /// </summary>
    public void StartEmailVerification(string token, string email)
    {
        verificationToken = token;
        verificationEmail = email;
        EmailStepBody = localizer.T("emailVerification.body", email);
        EmailCode = "";
        ResendMessage = "";
        IsMfaStep = false;
        IsEmailStep = true;
    }

    private bool CanLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        SetError(null);

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            SetError(() => localizer.T("errors.fillCredentials"));
            return;
        }

        await SignInAsync(Email.Trim(), Password);
    }

    /// <summary>
    /// Un clic y dentro, como en Android. No toca los campos del formulario:
    /// entra directamente con las credenciales publicas de la demo.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginDemoAsync()
    {
        SetError(null);
        await SignInAsync(DemoEmail, DemoPassword);
    }

    /// <summary>
    /// Abre el navegador para elegir la cuenta de Google y entra con ella.
    /// Cancelar o cualquier fallo deja a la persona aqui con un aviso.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginGoogleAsync()
    {
        SetError(null);
        if (googleSignIn is null) return;

        await EnterAsync(async () =>
        {
            var idToken = await googleSignIn.GetIdTokenAsync();
            return await auth.GoogleLoginAsync(idToken, localizer.Language == AppLanguage.En ? "en" : "es");
        });
    }

    private Task SignInAsync(string email, string password) =>
        EnterAsync(() => auth.LoginAsync(email, password));

    private async Task EnterAsync(Func<Task<LoginResponse>> signIn)
    {
        IsBusy = true;
        WakingNotice.Start();
        try
        {
            Continue(await signIn());
        }
        catch (ApiException e)
        {
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            WakingNotice.Stop();
            IsBusy = false;
        }
    }

    /// <summary>
    /// Lo que toca tras una respuesta del login: el codigo del correo, el del
    /// MFA o la sesion.
    /// </summary>
    private void Continue(LoginResponse login)
    {
        if (login.EmailVerificationRequired && login.VerificationToken is not null)
        {
            StartEmailVerification(login.VerificationToken, login.Email ?? Email.Trim());
            return;
        }

        IsEmailStep = false;

        // Con MFA el login no abre la sesion: falta el codigo.
        if (login.MfaRequired && login.MfaToken is not null)
        {
            mfaToken = login.MfaToken;
            MfaCode = "";
            IsMfaStep = true;
            return;
        }

        session.Start(login);
        LoggedIn?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task VerifyEmailAsync()
    {
        SetError(null);
        ResendMessage = "";
        var code = EmailCode.Trim();
        if (verificationToken is null) return;

        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
        {
            SetError(() => localizer.T("emailVerification.codeFormat"));
            return;
        }

        IsBusy = true;
        try
        {
            Continue(await auth.VerifyEmailAsync(verificationToken, code));
        }
        catch (ApiException e)
        {
            // Han pasado los 30 minutos: vuelta a la contraseña.
            if (e.Code == ApiException.InvalidVerificationToken) BackToPassword();
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task ResendEmailCodeAsync()
    {
        SetError(null);
        ResendMessage = "";
        if (verificationToken is null) return;

        IsBusy = true;
        try
        {
            await auth.ResendEmailCodeAsync(verificationToken);
            ResendMessage = localizer.T("emailVerification.resent");
        }
        catch (ApiException e)
        {
            if (e.Code == ApiException.InvalidVerificationToken) BackToPassword();
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task VerifyMfaAsync()
    {
        SetError(null);
        var code = MfaCode.Trim();
        if (mfaToken is null || code.Length == 0) return;

        IsBusy = true;
        try
        {
            var login = await auth.VerifyMfaAsync(mfaToken, code, RememberDevice);
            session.Start(login);
            LoggedIn?.Invoke(this, EventArgs.Empty);
        }
        catch (ApiException e)
        {
            // Han pasado los 5 minutos: vuelta a la contraseña.
            if (e.Code == ApiException.InvalidMfaToken) BackToPassword();
            SetError(() => localizer.ApiError(e.Code));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void BackToPassword()
    {
        mfaToken = null;
        MfaCode = "";
        IsMfaStep = false;
        verificationToken = null;
        EmailCode = "";
        ResendMessage = "";
        IsEmailStep = false;
        SetError(null);
    }

    /// <summary>Deja de escuchar al Localizer, que vive mas que esta pantalla.</summary>
    public void Dispose()
    {
        localizer.Changed -= OnLanguageChanged;
        WakingNotice.Stop();
    }
}
