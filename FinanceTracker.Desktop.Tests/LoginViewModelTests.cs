using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class LoginViewModelTests
{
    /// <summary>Sustituye a la API: devuelve lo que le digamos, sin red.</summary>
    private class FakeAuthService : IAuthService
    {
        public Func<string, string, LoginResponse> Handler { get; set; } =
            (_, _) => throw new InvalidOperationException("No configurado");

        public int Calls { get; private set; }
        public string? LastEmail { get; private set; }
        public string? LastPassword { get; private set; }

        public Task<UserInfo> RegisterAsync(string name, string email, string password, string language) =>
            throw new InvalidOperationException("El login no registra");

        public string? LastGoogleIdToken { get; private set; }
        public string? LastGoogleLanguage { get; private set; }

        public Task<LoginResponse> GoogleLoginAsync(string idToken, string language)
        {
            LastGoogleIdToken = idToken;
            LastGoogleLanguage = language;
            return Task.FromResult(Success("maria@gmail.com"));
        }

        public Func<string, string, LoginResponse> MfaHandler { get; set; } =
            (_, _) => throw new InvalidOperationException("No configurado");

        public string? LastMfaToken { get; private set; }
        public bool LastRememberDevice { get; private set; }

        public Task<LoginResponse> VerifyMfaAsync(string mfaToken, string code, bool rememberDevice = false)
        {
            LastMfaToken = mfaToken;
            LastRememberDevice = rememberDevice;
            return Task.FromResult(MfaHandler(mfaToken, code));
        }

        public Func<string, string, LoginResponse> EmailHandler { get; set; } =
            (_, _) => throw new InvalidOperationException("No configurado");

        public int Resends { get; private set; }

        public Task<LoginResponse> VerifyEmailAsync(string verificationToken, string code) =>
            Task.FromResult(EmailHandler(verificationToken, code));

        public Task ResendEmailCodeAsync(string verificationToken)
        {
            Resends++;
            return Task.CompletedTask;
        }

        public Task<LoginResponse> LoginAsync(string email, string password)
        {
            Calls++;
            LastEmail = email;
            LastPassword = password;
            return Task.FromResult(Handler(email, password));
        }
    }

    /// <summary>Sustituye al navegador: devuelve el token o falla como si se cancelara.</summary>
    private class FakeGoogleSignIn(string? idToken) : IGoogleSignIn
    {
        public Task<string> GetIdTokenAsync() =>
            idToken is null
                ? throw new ApiException(ApiException.GoogleSignInFailed)
                : Task.FromResult(idToken);
    }

    [Fact]
    public async Task LoginWithGoogle_SendsTokenAndLanguage_AndStartsSession()
    {
        var auth = new FakeAuthService();
        var session = new Session();
        var vm = new LoginViewModel(auth, session, En, googleSignIn: new FakeGoogleSignIn("google-id-token"));
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginGoogleCommand.ExecuteAsync(null);

        Assert.Equal("google-id-token", auth.LastGoogleIdToken);
        Assert.Equal("en", auth.LastGoogleLanguage);
        Assert.Equal("maria@gmail.com", session.User?.Email);
        Assert.True(raised);
    }

    [Fact]
    public async Task LoginWithGoogle_Cancelled_ShowsErrorAndStaysOnLogin()
    {
        var session = new Session();
        var vm = new LoginViewModel(new FakeAuthService(), session, Es, googleSignIn: new FakeGoogleSignIn(null));
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginGoogleCommand.ExecuteAsync(null);

        Assert.Equal("No se ha podido iniciar sesión con Google.", vm.ErrorMessage);
        Assert.Null(session.Token);
        Assert.False(raised);
        Assert.False(vm.IsBusy);
    }

    private static LoginResponse Success(string email) =>
        new("token-123", DateTime.UtcNow.AddHours(1), new UserInfo(1, "Antonio", email));

    // Lo que responde la API a una cuenta con MFA: ni token ni usuario.
    private static LoginResponse MfaChallenge() =>
        new(null!, DateTime.UtcNow.AddMinutes(5), null!, MfaRequired: true, MfaToken: "mfa-token");

    // Lo que responde la API a una cuenta con el correo sin verificar.
    private static LoginResponse EmailChallenge() =>
        new(null!, DateTime.UtcNow.AddMinutes(30), null!, EmailVerificationRequired: true,
            VerificationToken: "verify-token", Email: "ana@correo.com");

    [Fact]
    public async Task Login_WithAnUnverifiedEmail_AsksForTheEmailCode()
    {
        var auth = new FakeAuthService { Handler = (_, _) => EmailChallenge() };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "ana@correo.com", Password = "una-clave-larga" };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.True(vm.IsEmailStep);
        Assert.False(vm.IsPasswordStep);
        Assert.Contains("ana@correo.com", vm.EmailStepBody);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task VerifyEmail_WithTheRightCode_StartsTheSession()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => EmailChallenge(),
            EmailHandler = (token, code) => token == "verify-token" && code == "123456"
                ? Success("ana@correo.com")
                : throw new ApiException("invalid_email_code"),
        };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        var loggedIn = false;
        vm.LoggedIn += (_, _) => loggedIn = true;
        await vm.LoginCommand.ExecuteAsync(null);

        vm.EmailCode = "123456";
        await vm.VerifyEmailCommand.ExecuteAsync(null);

        Assert.True(loggedIn);
        Assert.True(session.IsActive);
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12a456")]
    public async Task VerifyEmail_ChecksTheFormat_BeforeCallingTheApi(string code)
    {
        var auth = new FakeAuthService { Handler = (_, _) => EmailChallenge() };
        var vm = new LoginViewModel(auth, new Session(), Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        await vm.LoginCommand.ExecuteAsync(null);

        vm.EmailCode = code;
        await vm.VerifyEmailCommand.ExecuteAsync(null);

        Assert.Equal("Escribe los 6 dígitos del código.", vm.ErrorMessage);
        Assert.True(vm.IsEmailStep);
    }

    [Fact]
    public async Task VerifyEmail_WithAnExpiredStep_GoesBackToThePassword()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => EmailChallenge(),
            EmailHandler = (_, _) => throw new ApiException(ApiException.InvalidVerificationToken),
        };
        var vm = new LoginViewModel(auth, new Session(), Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        await vm.LoginCommand.ExecuteAsync(null);

        vm.EmailCode = "123456";
        await vm.VerifyEmailCommand.ExecuteAsync(null);

        Assert.True(vm.IsPasswordStep);
        Assert.NotEqual("", vm.ErrorMessage);
    }

    [Fact]
    public async Task ResendEmailCode_AsksTheApi_AndSaysSo()
    {
        var auth = new FakeAuthService { Handler = (_, _) => EmailChallenge() };
        var vm = new LoginViewModel(auth, new Session(), Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        await vm.LoginCommand.ExecuteAsync(null);

        await vm.ResendEmailCodeCommand.ExecuteAsync(null);

        Assert.Equal(1, auth.Resends);
        Assert.NotEqual("", vm.ResendMessage);
    }

    [Fact]
    public async Task Login_WithMfa_AsksForTheCode_AndDoesNotStartTheSession()
    {
        var auth = new FakeAuthService { Handler = (_, _) => MfaChallenge() };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.True(vm.IsMfaStep);
        Assert.False(vm.IsPasswordStep);
        Assert.Null(session.Token);
        Assert.False(raised);
    }

    [Fact]
    public async Task VerifyMfa_WithAValidCode_StartsTheSession()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => MfaChallenge(),
            MfaHandler = (_, _) => Success("ana@correo.com"),
        };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "ana@correo.com", Password = "una-clave-larga" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginCommand.ExecuteAsync(null);
        vm.MfaCode = " 123456 ";
        vm.RememberDevice = true;
        await vm.VerifyMfaCommand.ExecuteAsync(null);

        Assert.Equal("mfa-token", auth.LastMfaToken);
        Assert.True(auth.LastRememberDevice);
        Assert.Equal("token-123", session.Token);
        Assert.True(raised);
    }

    [Fact]
    public async Task VerifyMfa_WithAnExpiredStep_GoesBackToThePassword()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => MfaChallenge(),
            MfaHandler = (_, _) => throw new ApiException(ApiException.InvalidMfaToken),
        };
        var vm = new LoginViewModel(auth, new Session(), Es) { Email = "ana@correo.com", Password = "una-clave-larga" };

        await vm.LoginCommand.ExecuteAsync(null);
        vm.MfaCode = "123456";
        await vm.VerifyMfaCommand.ExecuteAsync(null);

        Assert.False(vm.IsMfaStep);
        Assert.Equal("Ha pasado demasiado tiempo. Vuelve a introducir tu contraseña.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Login_WithEmptyFields_ShowsErrorAndDoesNotCallApi()
    {
        var auth = new FakeAuthService();
        var vm = new LoginViewModel(auth, new Session(), Es);

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.fillCredentials"), vm.ErrorMessage);
        Assert.Equal(0, auth.Calls);
    }

    [Fact]
    public async Task Login_Success_StartsSessionAndRaisesEvent()
    {
        var auth = new FakeAuthService { Handler = (email, _) => Success(email) };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "  antonio@example.com ", Password = "secret" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.True(raised);
        Assert.Equal("token-123", session.Token);
        Assert.Equal("antonio@example.com", session.User?.Email); // email recortado
        Assert.Equal("", vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShowsMessage()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => throw new ApiException(ApiException.InvalidCredentials)
        };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "a@b.com", Password = "bad" };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(EsText("apiError.invalid_credentials"), vm.ErrorMessage);
        Assert.False(session.IsActive);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Login_NetworkError_ShowsConnectionMessage()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => throw new ApiException(ApiException.NetworkError)
        };
        var vm = new LoginViewModel(auth, new Session(), Es) { Email = "a@b.com", Password = "x" };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal(EsText("apiError.network_error"), vm.ErrorMessage);
    }

    [Fact]
    public async Task LoginDemo_UsesTheDemoCredentialsAndIgnoresTheForm()
    {
        var auth = new FakeAuthService { Handler = (email, _) => Success(email) };
        var session = new Session();
        var vm = new LoginViewModel(auth, session, Es) { Email = "otro@example.com", Password = "lo-que-sea" };
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.LoginDemoCommand.ExecuteAsync(null);

        Assert.Equal(LoginViewModel.DemoEmail, auth.LastEmail);
        Assert.Equal(LoginViewModel.DemoPassword, auth.LastPassword);
        Assert.Equal("otro@example.com", vm.Email); // el formulario no se toca
        Assert.True(raised);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task LoginDemo_WorksWithAnEmptyForm_AndReportsErrors()
    {
        var auth = new FakeAuthService
        {
            Handler = (_, _) => throw new ApiException(ApiException.NetworkError)
        };
        var vm = new LoginViewModel(auth, new Session(), Es);

        await vm.LoginDemoCommand.ExecuteAsync(null);

        Assert.Equal(1, auth.Calls); // no exige rellenar los campos
        Assert.Equal(EsText("apiError.network_error"), vm.ErrorMessage);
        Assert.False(vm.IsBusy);
    }
}
