using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class MainViewModelTests
{
    private static MainViewModel Vm(FakeTransactionService api, FakeDialogService? dialogs = null,
        Session? session = null) =>
        new(session ?? LoggedInSession(), api, api, dialogs ?? new FakeDialogService(), Es);

    private static List<TransactionDto> SampleData() =>
    [
        Tx(1, "2026-08-10T00:00:00", 50, Expense, "Agosto"),
        Tx(2, "2026-09-01T00:00:00", 1000, Income, "Nómina"),
        Tx(3, "2026-09-12T00:00:00", 250, Expense, "Compra"),
    ];

    [Fact]
    public void Greeting_UsesUserName()
    {
        var vm = Vm(new FakeTransactionService());

        Assert.Equal("Hola, Antonio", vm.Greeting);
    }

    [Fact]
    public async Task Load_SelectsNewestMonthAndShowsItsTotals()
    {
        var vm = Vm(new FakeTransactionService { Handler = SampleData });

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.Months.Count);
        Assert.Equal("Septiembre 2026", vm.SelectedMonth?.Label);
        Assert.Equal(new[] { "Compra", "Nómina" }, vm.Transactions.Select(t => t.Description));
        Assert.Equal(1000m.ToString("C", Es.Culture), vm.IncomeText);
        Assert.Equal(250m.ToString("C", Es.Culture), vm.ExpenseText);
        Assert.Equal(750m.ToString("C", Es.Culture), vm.BalanceText);
        Assert.True(vm.HasData);
    }

    [Fact]
    public async Task ChangingMonth_UpdatesListAndTotals()
    {
        var vm = Vm(new FakeTransactionService { Handler = SampleData });
        await vm.LoadCommand.ExecuteAsync(null);

        vm.SelectedMonth = vm.Months.Single(m => m.Label == "Agosto 2026");

        Assert.Equal(new[] { "Agosto" }, vm.Transactions.Select(t => t.Description));
        Assert.True(vm.IsBalanceNegative);
    }

    [Fact]
    public async Task Load_WithNoTransactions_ShowsEmptyState()
    {
        var vm = Vm(new FakeTransactionService());

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
        var vm = Vm(service, session: session);

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
        var vm = Vm(service, session: session);
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
        var vm = Vm(new FakeTransactionService(), session: session);
        var raised = false;
        vm.LoggedOut += (_, _) => raised = true;

        vm.LogoutCommand.Execute(null);

        Assert.False(session.IsActive);
        Assert.True(raised);
    }

    [Fact]
    public async Task SavingANewTransaction_ReloadsAndShowsItsMonth()
    {
        var api = new FakeTransactionService { Handler = SampleData };
        var dialogs = new FakeDialogService();
        var vm = Vm(api, dialogs);
        await vm.LoadCommand.ExecuteAsync(null);

        // El "usuario" guarda un movimiento de agosto estando en septiembre.
        dialogs.Editor = editor =>
        {
            editor.Description = "Cena";
            editor.AmountText = "30";
            editor.Date = new DateTime(2026, 8, 20);
            editor.SaveCommand.Execute(null);
            return true;
        };
        api.Categories = [new CategoryDto(1, "Comida", Expense)];
        await vm.LoadCommand.ExecuteAsync(null); // recoge la categoria nueva

        await vm.NewTransactionCommand.ExecuteAsync(null);

        Assert.Single(api.Created);
        Assert.Equal(3, api.LoadCount);
        Assert.Equal("Agosto 2026", vm.SelectedMonth?.Label);
    }

    [Fact]
    public async Task CancellingTheEditor_DoesNotReload()
    {
        var api = new FakeTransactionService { Handler = SampleData };
        var vm = Vm(api);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.NewTransactionCommand.ExecuteAsync(null);

        Assert.Equal(1, api.LoadCount);
    }

    [Fact]
    public async Task EditingARow_OpensTheEditorPrefilled()
    {
        var api = new FakeTransactionService { Handler = SampleData };
        var dialogs = new FakeDialogService();
        var vm = Vm(api, dialogs);
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.EditTransactionCommand.ExecuteAsync(vm.Transactions.First());

        Assert.True(dialogs.LastEditor?.IsEdit);
        Assert.Equal("Compra", dialogs.LastEditor?.Description);
    }
}
