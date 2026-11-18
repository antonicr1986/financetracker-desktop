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

    public bool IsPasswordStep => !IsMfaStep;

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

    private void OnLanguageChanged(object? sender, EventArgs e) => SetError(error);

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
            var login = await signIn();

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
        SetError(null);
    }

    /// <summary>Deja de escuchar al Localizer, que vive mas que esta pantalla.</summary>
    public void Dispose()
    {
        localizer.Changed -= OnLanguageChanged;
        WakingNotice.Stop();
    }
}
