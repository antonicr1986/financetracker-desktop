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
        public int ChangePasswordCalls { get; private set; }
        public Exception? ChangePasswordError { get; set; }

        public Task<LoginResponse> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            ChangePasswordCalls++;
            return ChangePasswordError is null
                ? Task.FromResult(new LoginResponse("new-jwt", DateTime.UtcNow.AddHours(1), new UserInfo(7, "Ana", "ana@correo.com")))
                : Task.FromException<LoginResponse>(ChangePasswordError);
        }

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

    private static MfaViewModel PasswordForm(FakeMfaService mfa, Session session, string current, string next, string confirmation)
    {
        var vm = new MfaViewModel(mfa, session, new Localizer(AppLanguage.Es), new FakeDialogService());
        vm.StartChangePasswordCommand.Execute(null);
        vm.CurrentPassword = current;
        vm.NewPassword = next;
        vm.PasswordConfirmation = confirmation;
        return vm;
    }

    [Fact]
    public async Task ChangePassword_SavesTheNewToken_AndGoesBackToIdle()
    {
        var mfa = new FakeMfaService();
        var session = ActiveSession();
        var vm = PasswordForm(mfa, session, "la-actual-larga", "la-nueva-larga", "la-nueva-larga");

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("new-jwt", session.Token);
        Assert.True(vm.IsIdle);
        Assert.NotEqual("", vm.PasswordMessage);
        Assert.Equal("", vm.NewPassword);
    }

    [Theory]
    [InlineData("", "la-nueva-larga", "la-nueva-larga")]
    [InlineData("la-actual-larga", "corta", "corta")]
    [InlineData("la-actual-larga", "la-nueva-larga", "otra-distinta")]
    public async Task ChangePassword_ChecksTheFormBeforeCallingTheApi(string current, string next, string confirmation)
    {
        var mfa = new FakeMfaService();
        var vm = PasswordForm(mfa, ActiveSession(), current, next, confirmation);

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal(0, mfa.ChangePasswordCalls);
        Assert.NotEqual("", vm.ErrorMessage);
        Assert.True(vm.IsChangingPassword);
    }

    [Fact]
    public async Task ChangePassword_ShowsTheApiError_AndKeepsTheFormOpen()
    {
        var mfa = new FakeMfaService { ChangePasswordError = new ApiException("invalid_current_password") };
        var session = ActiveSession();
        var vm = PasswordForm(mfa, session, "mal-mal-mal", "la-nueva-larga", "la-nueva-larga");

        await vm.ChangePasswordCommand.ExecuteAsync(null);

        Assert.Equal("La contraseña actual no es correcta.", vm.ErrorMessage);
        Assert.True(vm.IsChangingPassword);
        Assert.Equal("jwt", session.Token);
    }

    private static MfaViewModel VmFor(UserInfo user)
    {
        var session = new Session();
        session.Start(new LoginResponse("jwt", DateTime.UtcNow.AddHours(1), user));
        return new MfaViewModel(new FakeMfaService(), session, new Localizer(AppLanguage.Es), new FakeDialogService());
    }

    [Fact]
    public void AccountWithoutPassword_CannotEnableMfaNorChangeThePassword()
    {
        var vm = VmFor(new UserInfo(7, "Ana", "ana@gmail.com", MfaEnabled: false, HasPassword: false));

        Assert.False(vm.ShowAction);
        Assert.True(vm.ShowMfaUnavailable);
        Assert.False(vm.HasPassword);
        Assert.Contains("Google", vm.PasswordDescription);
    }

    [Fact]
    public void AccountWithoutPassword_CanStillDisableAnMfaItAlreadyHas()
    {
        var vm = VmFor(new UserInfo(7, "Ana", "ana@gmail.com", MfaEnabled: true, HasPassword: false));

        Assert.True(vm.ShowAction);
        Assert.False(vm.ShowMfaUnavailable);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(null)]
    public void AccountWithPasswordOrUnknown_ShowsEverything(bool? hasPassword)
    {
        var vm = VmFor(new UserInfo(7, "Ana", "ana@correo.com", HasPassword: hasPassword));

        Assert.True(vm.ShowAction);
        Assert.False(vm.ShowMfaUnavailable);
        Assert.True(vm.HasPassword);
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
