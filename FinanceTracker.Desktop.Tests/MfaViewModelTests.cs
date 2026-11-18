using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

public class MfaViewModelTests
{
    private class FakeMfaService : IMfaService
    {
        public int RevokeCalls { get; private set; }
        public Exception? RevokeError { get; set; }

        public Task<MfaSetupResponse> SetupMfaAsync() => throw new NotSupportedException();
        public Task<MfaRecoveryCodesResponse> ConfirmMfaAsync(string code) => throw new NotSupportedException();
        public Task DisableMfaAsync(string code) => throw new NotSupportedException();
        public Task ForgetDevicesAsync() => throw new NotSupportedException();

        public Task RevokeSessionsAsync()
        {
            RevokeCalls++;
            return RevokeError is null ? Task.CompletedTask : Task.FromException(RevokeError);
        }
    }

    private static Session ActiveSession()
    {
        var session = new Session();
        session.Start(new LoginResponse("jwt", DateTime.UtcNow.AddHours(1), new UserInfo(7, "Ana", "ana@correo.com")));
        return session;
    }

    [Fact]
    public async Task SignOutEverywhere_DoesNothing_WhenTheUserCancels()
    {
        var mfa = new FakeMfaService();
        var session = ActiveSession();
        var vm = new MfaViewModel(mfa, session, new Localizer(AppLanguage.Es), new FakeDialogService { ConfirmAnswer = false });

        await vm.SignOutEverywhereCommand.ExecuteAsync(null);

        Assert.Equal(0, mfa.RevokeCalls);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task SignOutEverywhere_RevokesAndClearsTheLocalSession()
    {
        var mfa = new FakeMfaService();
        var session = ActiveSession();
        var vm = new MfaViewModel(mfa, session, new Localizer(AppLanguage.Es), new FakeDialogService());
        var signedOut = false;
        vm.SignedOut += (_, _) => signedOut = true;

        await vm.SignOutEverywhereCommand.ExecuteAsync(null);

        Assert.Equal(1, mfa.RevokeCalls);
        Assert.False(session.IsActive);
        Assert.True(signedOut);
    }

    [Fact]
    public async Task SignOutEverywhere_KeepsTheSession_WhenTheApiFails()
    {
        var mfa = new FakeMfaService { RevokeError = new ApiException(ApiException.Unknown) };
        var session = ActiveSession();
        var vm = new MfaViewModel(mfa, session, new Localizer(AppLanguage.Es), new FakeDialogService());

        await vm.SignOutEverywhereCommand.ExecuteAsync(null);

        Assert.True(session.IsActive);
        Assert.NotEqual("", vm.ErrorMessage);
    }
}
