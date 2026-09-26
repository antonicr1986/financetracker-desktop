using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

public class LoginViewModelTests
{
    /// <summary>Sustituye a la API: devuelve lo que le digamos, sin red.</summary>
    private class FakeAuthService : IAuthService
    {
        public Func<string, string, LoginResponse> Handler { get; set; } =
            (_, _) => throw new InvalidOperationException("No configurado");

        public int Calls { get; private set; }

        public Task<LoginResponse> LoginAsync(string email, string password)
        {
            Calls++;
            return Task.FromResult(Handler(email, password));
        }
    }

    private static LoginResponse Success(string email) =>
        new("token-123", DateTime.UtcNow.AddHours(1), new UserInfo(1, "Antonio", email));

    [Fact]
    public async Task Login_WithEmptyFields_ShowsErrorAndDoesNotCallApi()
    {
        var auth = new FakeAuthService();
        var vm = new LoginViewModel(auth, new Session());

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Escribe el email y la contraseña.", vm.ErrorMessage);
        Assert.Equal(0, auth.Calls);
    }

    [Fact]
    public async Task Login_Success_StartsSessionAndRaisesEvent()
    {
        var auth = new FakeAuthService { Handler = (email, _) => Success(email) };
        var session = new Session();
        var vm = new LoginViewModel(auth, session) { Email = "  antonio@example.com ", Password = "secret" };
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
        var vm = new LoginViewModel(auth, session) { Email = "a@b.com", Password = "bad" };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("Email o contraseña incorrectos.", vm.ErrorMessage);
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
        var vm = new LoginViewModel(auth, new Session()) { Email = "a@b.com", Password = "x" };

        await vm.LoginCommand.ExecuteAsync(null);

        Assert.Equal("No se pudo conectar con el servidor. Revisa tu conexión.", vm.ErrorMessage);
    }
}
