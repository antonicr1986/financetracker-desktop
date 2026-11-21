using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class ForgotPasswordViewModelTests
{
    /// <summary>API falsa de acceso: apunta lo que se le pide.</summary>
    private class FakeAuth : IAuthService
    {
        public List<string> Requested { get; } = [];
        public List<(string Email, string Code, string Password)> Resets { get; } = [];
        public ApiException? ResetError { get; set; }

        public Task ForgotPasswordAsync(string email)
        {
            Requested.Add(email);
            return Task.CompletedTask;
        }

        public Task ResetPasswordAsync(string email, string code, string newPassword)
        {
            Resets.Add((email, code, newPassword));
            return ResetError is null ? Task.CompletedTask : Task.FromException(ResetError);
        }

        public Task<LoginResponse> LoginAsync(string email, string password) => throw new NotSupportedException();
        public Task<UserInfo> RegisterAsync(string name, string email, string password, string language) => throw new NotSupportedException();
        public Task<LoginResponse> GoogleLoginAsync(string idToken, string language) => throw new NotSupportedException();
        public Task<LoginResponse> VerifyMfaAsync(string mfaToken, string code, bool rememberDevice = false) => throw new NotSupportedException();
        public Task<LoginResponse> VerifyEmailAsync(string verificationToken, string code) => throw new NotSupportedException();
        public Task ResendEmailCodeAsync(string verificationToken) => throw new NotSupportedException();
    }

    private const string NewPassword = "la-nueva-larga"; // gitleaks:allow — dato de prueba

    private static async Task<ForgotPasswordViewModel> AtCodeStep(FakeAuth auth, Localizer? localizer = null)
    {
        var vm = new ForgotPasswordViewModel(auth, localizer ?? Es) { Email = " ana@correo.com " };
        await vm.SendCodeCommand.ExecuteAsync(null);
        return vm;
    }

    private static void Fill(ForgotPasswordViewModel vm, string code, string password, string? confirmation = null)
    {
        vm.Code = code;
        vm.Password = password;
        vm.Confirmation = confirmation ?? password;
    }

    [Fact]
    public async Task SendCode_AsksTheApi_AndMovesToTheCodeStep()
    {
        var auth = new FakeAuth();

        var vm = await AtCodeStep(auth);

        Assert.Equal(["ana@correo.com"], auth.Requested);
        Assert.True(vm.IsCodeStep);
        Assert.Contains("ana@correo.com", vm.StatusMessage);
    }

    [Fact]
    public async Task SendCode_WithABrokenEmail_StaysAndSaysWhy()
    {
        var auth = new FakeAuth();
        var vm = new ForgotPasswordViewModel(auth, Es) { Email = "ana@correo" };

        await vm.SendCodeCommand.ExecuteAsync(null);

        Assert.Empty(auth.Requested);
        Assert.True(vm.IsEmailStep);
        Assert.Equal("Introduce un correo válido.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Reset_WithEverythingRight_ChangesThePassword_AndFinishes()
    {
        var auth = new FakeAuth();
        var vm = await AtCodeStep(auth);
        Fill(vm, "123456", NewPassword);

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.Equal(("ana@correo.com", "123456", NewPassword), Assert.Single(auth.Resets));
        Assert.True(vm.IsDone);
        Assert.False(vm.ShowSignInLink);
    }

    [Theory]
    [InlineData("12345", NewPassword, NewPassword, "Escribe los 6 dígitos del código.")]
    [InlineData("12a456", NewPassword, NewPassword, "Escribe los 6 dígitos del código.")]
    [InlineData("123456", "corta", "corta", "La contraseña debe tener al menos 12 caracteres.")]
    [InlineData("123456", NewPassword, "otra-distinta-larga", "Las dos contraseñas no coinciden.")]
    public async Task Reset_ChecksTheForm_BeforeCallingTheApi(string code, string password, string confirmation, string message)
    {
        var auth = new FakeAuth();
        var vm = await AtCodeStep(auth);
        Fill(vm, code, password, confirmation);

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.Empty(auth.Resets);
        Assert.Equal(message, vm.ErrorMessage);
        Assert.True(vm.IsCodeStep);
    }

    [Fact]
    public async Task Reset_WithAnExpiredCode_ClearsIt_AndAsksForANewOne()
    {
        var auth = new FakeAuth { ResetError = new ApiException(ApiException.ResetCodeExpired) };
        var vm = await AtCodeStep(auth);
        Fill(vm, "123456", NewPassword);

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.Equal("", vm.Code);
        Assert.Contains("Pide uno nuevo", vm.ErrorMessage);
        Assert.True(vm.IsCodeStep);
    }

    [Fact]
    public async Task Reset_WithAWrongCode_KeepsItToFix()
    {
        var auth = new FakeAuth { ResetError = new ApiException("invalid_reset_code") };
        var vm = await AtCodeStep(auth);
        Fill(vm, "123456", NewPassword);

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.Equal("123456", vm.Code);
        Assert.Equal("El código no es correcto.", vm.ErrorMessage);
    }

    [Fact]
    public async Task Resend_AsksAgain_ForTheSameEmail()
    {
        var auth = new FakeAuth();
        var vm = await AtCodeStep(auth);

        await vm.ResendCodeCommand.ExecuteAsync(null);

        Assert.Equal(["ana@correo.com", "ana@correo.com"], auth.Requested);
    }

    [Fact]
    public async Task BackToEmail_ForgetsTheCode()
    {
        var vm = await AtCodeStep(new FakeAuth());
        vm.Code = "123456";

        vm.BackToEmailCommand.Execute(null);

        Assert.True(vm.IsEmailStep);
        Assert.Equal("", vm.Code);
        Assert.Equal("", vm.StatusMessage);
    }

    [Fact]
    public async Task ChangingLanguage_RewritesTheMessages()
    {
        var localizer = Es;
        var vm = await AtCodeStep(new FakeAuth(), localizer);
        Fill(vm, "1", NewPassword);
        await vm.ResetCommand.ExecuteAsync(null);

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal("Enter the 6 digits of the code.", vm.ErrorMessage);
        Assert.StartsWith("If ana@correo.com", vm.StatusMessage);
    }

    [Fact]
    public void SignIn_RaisesItsEvent()
    {
        var vm = new ForgotPasswordViewModel(new FakeAuth(), Es);
        var raised = false;
        vm.SignInRequested += (_, _) => raised = true;

        vm.GoToSignInCommand.Execute(null);

        Assert.True(raised);
    }

    [Theory]
    [InlineData("ana@correo.com", true)]
    [InlineData("ana@correo", false)]
    [InlineData("ana correo@x.com", false)]
    public void EmailCheck_IsTheRegistrationOne(string email, bool valid)
    {
        Assert.Equal(valid, RegistrationForm.IsValidEmail(email));
    }
}
