using FinanceTracker.Desktop.ViewModels;

namespace FinanceTracker.Desktop.Tests;

public class MainViewModelTests
{
    [Fact]
    public void SayHello_UpdatesMessageWithClickCount()
    {
        var vm = new MainViewModel();

        vm.SayHelloCommand.Execute(null);
        vm.SayHelloCommand.Execute(null);

        Assert.Equal("Has pulsado 2 veces", vm.Message);
    }
}
