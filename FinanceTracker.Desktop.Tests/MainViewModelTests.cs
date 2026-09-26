using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class MainViewModelTests
{
    private class FakeTransactionService : ITransactionService
    {
        public Func<List<TransactionDto>> Handler { get; set; } = () => [];

        public Task<List<TransactionDto>> GetAllTransactionsAsync() => Task.FromResult(Handler());
    }

    private static List<TransactionDto> SampleData() =>
    [
        Tx(1, "2026-08-10T00:00:00", 50, Expense, "Agosto"),
        Tx(2, "2026-09-01T00:00:00", 1000, Income, "Nómina"),
        Tx(3, "2026-09-12T00:00:00", 250, Expense, "Compra"),
    ];

    [Fact]
    public void Greeting_UsesUserName()
    {
        var vm = new MainViewModel(LoggedInSession(), new FakeTransactionService());

        Assert.Equal("Hola, Antonio", vm.Greeting);
    }

    [Fact]
    public async Task Load_SelectsNewestMonthAndShowsItsTotals()
    {
        var vm = new MainViewModel(LoggedInSession(), new FakeTransactionService { Handler = SampleData });

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Months.Count);
        Assert.Equal("Septiembre 2026", vm.SelectedMonth?.Label);
        Assert.Equal(new[] { "Compra", "Nómina" }, vm.Transactions.Select(t => t.Description));
        Assert.Equal(1000m.ToString("C", MainViewModel.Culture), vm.IncomeText);
        Assert.Equal(250m.ToString("C", MainViewModel.Culture), vm.ExpenseText);
        Assert.Equal(750m.ToString("C", MainViewModel.Culture), vm.BalanceText);
        Assert.True(vm.HasData);
    }

    [Fact]
    public async Task ChangingMonth_UpdatesListAndTotals()
    {
        var vm = new MainViewModel(LoggedInSession(), new FakeTransactionService { Handler = SampleData });
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SelectedMonth = vm.Months.Single(m => m.Label == "Agosto 2026");

        Assert.Equal(new[] { "Agosto" }, vm.Transactions.Select(t => t.Description));
        Assert.True(vm.IsBalanceNegative);
    }

    [Fact]
    public async Task Load_WithNoTransactions_ShowsEmptyState()
    {
        var vm = new MainViewModel(LoggedInSession(), new FakeTransactionService());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.IsEmpty);
        Assert.False(vm.HasData);
    }

    [Fact]
    public async Task Load_NetworkError_ShowsErrorAndKeepsSession()
    {
        var session = LoggedInSession();
        var service = new FakeTransactionService
        {
            Handler = () => throw new ApiException(ApiException.NetworkError)
        };
        var vm = new MainViewModel(session, service);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.True(session.IsActive);
        Assert.False(vm.IsLoading);
    }

    [Fact]
    public async Task Load_SessionExpired_ClearsSessionAndRaisesEvent()
    {
        var session = LoggedInSession();
        var service = new FakeTransactionService
        {
            Handler = () => throw new ApiException(ApiException.SessionExpired)
        };
        var vm = new MainViewModel(session, service);
        var raised = false;
        vm.SessionExpired += (_, _) => raised = true;

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.True(raised);
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Logout_ClearsSessionAndRaisesEvent()
    {
        var session = LoggedInSession();
        var vm = new MainViewModel(session, new FakeTransactionService());
        var raised = false;
        vm.LoggedOut += (_, _) => raised = true;

        vm.LogoutCommand.Execute(null);

        Assert.False(session.IsActive);
        Assert.True(raised);
    }
}
