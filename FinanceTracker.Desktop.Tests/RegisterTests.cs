using System.Net;
using System.Net.Http;
using System.Text;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class RegistrationFormTests
{
    [Fact]
    public void Validate_ReportsTheFirstProblem_InTheAndroidOrder()
    {
        Assert.Equal(RegistrationProblem.MissingFields, RegistrationForm.Validate("", "a@b.com", "123456", "123456"));
        Assert.Equal(RegistrationProblem.MissingFields, RegistrationForm.Validate("Ana", "", "123456", "123456"));
        Assert.Equal(RegistrationProblem.InvalidEmail, RegistrationForm.Validate("Ana", "ana@correo", "12", "34"));
        Assert.Equal(RegistrationProblem.PasswordTooShort, RegistrationForm.Validate("Ana", "ana@correo.com", "12345", "12345"));
        Assert.Equal(RegistrationProblem.PasswordsDontMatch, RegistrationForm.Validate("Ana", "ana@correo.com", "123456", "1234567"));
        Assert.Null(RegistrationForm.Validate("Ana", "ana@correo.com", "123456", "123456"));
    }

    [Theory]
    [InlineData("ana@correo.com", true)]
    [InlineData("ana.lopez+ft@sub.correo.es", true)]
    [InlineData("ana@correo", false)]
    [InlineData("ana correo@x.com", false)]
    [InlineData("@correo.com", false)]
    public void Email_NeedsSomethingAtSomethingDotSomething(string email, bool valid)
    {
        var problem = RegistrationForm.Validate("Ana", email, "123456", "123456");

        Assert.Equal(valid, problem is null);
    }
}

public class RegisterViewModelTests
{
    /// <summary>API falsa de acceso: apunta el orden de las llamadas.</summary>
    private class FakeAuth : IAuthService
    {
        public List<string> Calls { get; } = [];
        public ApiException? RegisterError { get; set; }
        public ApiException? LoginError { get; set; }

        public Task<UserInfo> RegisterAsync(string name, string email, string password)
        {
            Calls.Add($"register {name} {email} {password}");
            if (RegisterError is not null) throw RegisterError;
            return Task.FromResult(new UserInfo(1, name, email));
        }

        public Task<LoginResponse> LoginAsync(string email, string password)
        {
            Calls.Add($"login {email} {password}");
            if (LoginError is not null) throw LoginError;
            return Task.FromResult(new LoginResponse("token", DateTime.UtcNow.AddHours(1), new UserInfo(1, "Ana", email)));
        }
    }

    private static RegisterViewModel Filled(FakeAuth auth, Session? session = null, Localizer? localizer = null) =>
        new(auth, session ?? new Session(), localizer ?? Es)
        {
            Name = "  Ana ",
            Email = " ana@correo.com ",
            Password = "secreto1",
            Confirmation = "secreto1",
        };

    [Fact]
    public async Task Register_CreatesTheAccountThenSignsIn_BecauseTheApiReturnsNoToken()
    {
        var auth = new FakeAuth();
        var session = new Session();
        var vm = Filled(auth, session);
        var raised = false;
        vm.LoggedIn += (_, _) => raised = true;

        await vm.RegisterCommand.ExecuteAsync(null);

        Assert.Equal(new[]
        {
            "register Ana ana@correo.com secreto1", // nombre y correo recortados
            "login ana@correo.com secreto1",
        }, auth.Calls);
        Assert.True(session.IsActive);
        Assert.True(raised);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Register_Invalid_ShowsTheProblemWithoutCallingTheApi()
    {
        var auth = new FakeAuth();
        var vm = Filled(auth);
        vm.Confirmation = "otra";

        await vm.RegisterCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.passwordsDontMatch"), vm.ErrorMessage);
        Assert.Empty(auth.Calls);
    }

    [Fact]
    public async Task Register_WithAnEmailInUse_SaysSo_AndDoesNotSignIn()
    {
        var auth = new FakeAuth { RegisterError = new ApiException(ApiException.EmailAlreadyExists) };
        var session = new Session();
        var vm = Filled(auth, session);

        await vm.RegisterCommand.ExecuteAsync(null);

        Assert.Equal(EsText("apiError.email_already_exists"), vm.ErrorMessage);
        Assert.Single(auth.Calls); // no llega a intentar el login
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task Register_WithAnUnknownApiError_UsesTheGenericMessage()
    {
        var auth = new FakeAuth { RegisterError = new ApiException(ApiException.Unknown) };
        var vm = Filled(auth);

        await vm.RegisterCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.createAccountFailed"), vm.ErrorMessage);
    }

    [Fact]
    public async Task AnError_IsRewrittenWhenTheLanguageChanges()
    {
        var localizer = Es;
        var vm = Filled(new FakeAuth(), localizer: localizer);
        vm.Password = "123";
        vm.Confirmation = "123";
        await vm.RegisterCommand.ExecuteAsync(null);

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal(Strings.En["errors.passwordTooShort"], vm.ErrorMessage);
    }

    [Fact]
    public void TheLinks_AskToSwitchScreens()
    {
        var register = new RegisterViewModel(new FakeAuth(), new Session(), Es);
        var toSignIn = false;
        register.SignInRequested += (_, _) => toSignIn = true;
        register.GoToSignInCommand.Execute(null);

        var login = new LoginViewModel(new FakeAuth(), new Session(), Es);
        var toRegister = false;
        login.RegisterRequested += (_, _) => toRegister = true;
        login.GoToRegisterCommand.Execute(null);

        Assert.True(toSignIn);
        Assert.True(toRegister);
    }
}

public class RegisterApiTests
{
    private class LambdaHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public HttpRequestMessage? Last { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Last = request;
            Body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            return respond(request);
        }
    }

    private static ApiClient Client(HttpMessageHandler handler) =>
        new(new Session(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task Register_PostsNameEmailAndPassword_WithoutAToken()
    {
        var handler = new LambdaHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"id":5,"name":"Ana","email":"ana@correo.com"}""", Encoding.UTF8, "application/json"),
        });

        var user = await Client(handler).RegisterAsync("Ana", "ana@correo.com", "secreto1");

        Assert.Equal(HttpMethod.Post, handler.Last!.Method);
        Assert.Equal("/api/Users/register", handler.Last.RequestUri!.AbsolutePath);
        Assert.Null(handler.Last.Headers.Authorization);
        Assert.Equal("""{"name":"Ana","email":"ana@correo.com","password":"secreto1"}""", handler.Body);
        Assert.Equal(5, user.Id);
    }

    [Fact]
    public async Task Register_WithAnEmailInUse_KeepsTheApiCode()
    {
        var handler = new LambdaHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"code":"email_already_exists","detail":"..."}""", Encoding.UTF8, "application/json"),
        });

        var e = await Assert.ThrowsAsync<ApiException>(() => Client(handler).RegisterAsync("Ana", "ana@correo.com", "secreto1"));

        Assert.Equal(ApiException.EmailAlreadyExists, e.Code);
    }
}
