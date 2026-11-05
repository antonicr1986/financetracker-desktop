using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class WakingNoticeTests
{
    private static readonly TimeSpan ShortDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>Espera a que se cumpla la condicion, con un limite generoso para CI.</summary>
    private static async Task<bool> EventuallyAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (condition()) return true;
            await Task.Delay(10);
        }
        return condition();
    }

    /// <summary>Un login que no termina hasta que el test lo suelta.</summary>
    private class SlowAuthService : IAuthService
    {
        private readonly TaskCompletionSource<LoginResponse> gate = new();

        public void Release(string email) => gate.SetResult(
            new LoginResponse("token-123", DateTime.UtcNow.AddHours(1), new UserInfo(1, "Antonio", email)));

        public Task<LoginResponse> LoginAsync(string email, string password) => gate.Task;

        public Task<LoginResponse> GoogleLoginAsync(string idToken, string language) =>
            throw new InvalidOperationException("Sin usar en estas pruebas");

        public Task<UserInfo> RegisterAsync(string name, string email, string password, string language) =>
            throw new InvalidOperationException("El login no registra");
    }

    [Fact]
    public void DefaultDelay_IsTheSameAsWebAndAndroid()
    {
        Assert.Equal(TimeSpan.FromSeconds(4), WakingNotice.DefaultDelay);
    }

    [Fact]
    public async Task Start_ShowsTheNoticeOnlyAfterTheDelay()
    {
        var notice = new WakingNotice(TimeSpan.FromMilliseconds(300));

        notice.Start();

        Assert.False(notice.IsVisible); // no aparece de golpe
        Assert.True(await EventuallyAsync(() => notice.IsVisible));
    }

    [Fact]
    public async Task Stop_BeforeTheDelay_NeverShowsTheNotice()
    {
        var notice = new WakingNotice(ShortDelay);

        notice.Start();
        notice.Stop();
        await Task.Delay(ShortDelay * 4);

        Assert.False(notice.IsVisible);
    }

    [Fact]
    public async Task Stop_HidesAVisibleNotice()
    {
        var notice = new WakingNotice(ShortDelay);
        notice.Start();
        Assert.True(await EventuallyAsync(() => notice.IsVisible));

        notice.Stop();

        Assert.False(notice.IsVisible);
    }

    [Fact]
    public async Task SlowLogin_ShowsTheNotice_AndHidesItWhenSignedIn()
    {
        var auth = new SlowAuthService();
        var vm = new LoginViewModel(auth, new Session(), Es, ShortDelay)
        {
            Email = "antonio@example.com",
            Password = "secret",
        };

        var login = vm.LoginCommand.ExecuteAsync(null);
        Assert.True(await EventuallyAsync(() => vm.WakingNotice.IsVisible));

        auth.Release("antonio@example.com");
        await login;

        Assert.False(vm.WakingNotice.IsVisible);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task FastLogin_NeverShowsTheNotice()
    {
        var auth = new SlowAuthService();
        auth.Release("antonio@example.com"); // la API ya estaba despierta
        var vm = new LoginViewModel(auth, new Session(), Es, ShortDelay)
        {
            Email = "antonio@example.com",
            Password = "secret",
        };

        await vm.LoginCommand.ExecuteAsync(null);
        await Task.Delay(ShortDelay * 4);

        Assert.False(vm.WakingNotice.IsVisible);
    }
}
