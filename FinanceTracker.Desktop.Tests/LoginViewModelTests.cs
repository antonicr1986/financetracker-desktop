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

        public Task<LoginResponse> LoginAsync(string email, string password)
        {
            Calls++;
            LastEmail = email;
            LastPassword = password;
            return Task.FromResult(Handler(email, password));
        }
    }

    private static LoginResponse Success(string email) =>
        new("token-123", DateTime.UtcNow.AddHours(1), new UserInfo(1, "Antonio", email));

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
