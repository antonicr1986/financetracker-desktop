using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

public class MainViewModelTests
{
    private static Session LoggedInSession()
    {
        var session = new Session();
        session.Start(new LoginResponse("token", DateTime.UtcNow.AddHours(1),
            new UserInfo(1, "Antonio", "antonio@example.com")));
        return session;
    }

    [Fact]
    public void Greeting_UsesUserName()
    {
        var vm = new MainViewModel(LoggedInSession());

        Assert.Equal("Hola, Antonio", vm.Greeting);
    }

    [Fact]
    public void Logout_ClearsSessionAndRaisesEvent()
    {
        var session = LoggedInSession();
        var vm = new MainViewModel(session);
        var raised = false;
        vm.LoggedOut += (_, _) => raised = true;

        vm.LogoutCommand.Execute(null);

        Assert.False(session.IsActive);
        Assert.True(raised);
    }
}
