using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class TransactionEditorViewModelTests
{
    private static readonly List<CategoryDto> Categories =
    [
        new(1, "Supermercado", Expense),
        new(2, "Nómina", Income),
        new(3, "Alquiler", Expense),
    ];

    private static readonly DateTime Today = new(2026, 9, 26);

    private static TransactionEditorViewModel New(FakeTransactionService api, FakeDialogService? dialogs = null) =>
        new(api, dialogs ?? new FakeDialogService(), Categories, today: Today);

    private static TransactionEditorViewModel Edit(FakeTransactionService api, TransactionDto existing,
        FakeDialogService? dialogs = null) =>
        new(api, dialogs ?? new FakeDialogService(), Categories, existing);

    [Fact]
    public void New_StartsAsAnExpenseOfTodayWithExpenseCategoriesOnly()
    {
        var vm = New(new FakeTransactionService());

        Assert.False(vm.IsEdit);
        Assert.True(vm.IsExpense);
        Assert.Equal(Today, vm.Date);
        Assert.Equal(new[] { "Alquiler", "Supermercado" }, vm.Categories.Select(c => c.Name)); // ordenadas
        Assert.Equal("Alquiler", vm.SelectedCategory?.Name);
    }

    [Fact]
    public void SwitchingToIncome_ShowsOnlyIncomeCategories()
    {
        var vm = New(new FakeTransactionService());

        vm.IsIncome = true;

        Assert.Equal(new[] { "Nómina" }, vm.Categories.Select(c => c.Name));
        Assert.Equal("Nómina", vm.SelectedCategory?.Name);
    }

    [Fact]
    public async Task Save_New_SendsTheParsedInputAndRequestsClose()
    {
        var api = new FakeTransactionService();
        var vm = New(api);
        bool? closed = null;
        vm.CloseRequested += (_, changed) => closed = changed;
        vm.Description = "  Compra semanal ";
        vm.AmountText = "45,30";
        vm.SelectedCategory = vm.Categories.Single(c => c.Name == "Supermercado");

        await vm.SaveCommand.ExecuteAsync(null);

        var input = Assert.Single(api.Created);
        Assert.Equal("Compra semanal", input.Description);
        Assert.Equal(45.30m, input.Amount);
        Assert.Equal(Today, input.Date);
        Assert.Equal(DateTimeKind.Unspecified, input.Date.Kind);
        Assert.Equal(Expense, input.Type);
        Assert.Equal(1, input.CategoryId);
        Assert.True(closed);
    }

    [Fact]
    public async Task Save_Invalid_ShowsErrorAndDoesNotCallApi()
    {
        var api = new FakeTransactionService();
        var vm = New(api);
        vm.Description = "Cena";
        vm.AmountText = "0";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("El importe tiene que ser mayor que cero.", vm.ErrorMessage);
        Assert.Empty(api.Created);
    }

    [Fact]
    public async Task Edit_PrefillsTheFormAndUpdatesById()
    {
        var api = new FakeTransactionService();
        var existing = new TransactionDto(7, "Nómina septiembre", 1500.50m, new DateTime(2026, 9, 1), Income, 2, "Nómina");
        var vm = Edit(api, existing);

        Assert.True(vm.IsEdit);
        Assert.Equal("Editar movimiento", vm.Title);
        Assert.Equal("1500,5", vm.AmountText);
        Assert.True(vm.IsIncome);
        Assert.Equal("Nómina", vm.SelectedCategory?.Name);

        vm.AmountText = "1600";
        await vm.SaveCommand.ExecuteAsync(null);

        var (id, input) = Assert.Single(api.Updated);
        Assert.Equal(7, id);
        Assert.Equal(1600m, input.Amount);
        Assert.Empty(api.Created);
    }

    [Fact]
    public async Task Delete_AsksFirstAndDoesNothingIfTheUserSaysNo()
    {
        var api = new FakeTransactionService();
        var dialogs = new FakeDialogService { ConfirmAnswer = false };
        var vm = Edit(api, Tx(7, "2026-09-01T00:00:00", 10, Expense), dialogs);

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Empty(api.Deleted);
    }

    [Fact]
    public async Task Delete_Confirmed_DeletesAndRequestsClose()
    {
        var api = new FakeTransactionService();
        var vm = Edit(api, Tx(7, "2026-09-01T00:00:00", 10, Expense));
        bool? closed = null;
        vm.CloseRequested += (_, changed) => closed = changed;

        await vm.DeleteCommand.ExecuteAsync(null);

        Assert.Equal(new[] { 7 }, api.Deleted);
        Assert.True(closed);
    }

    [Fact]
    public async Task Save_CategoryTypeMismatch_ShowsMessageAndStaysOpen()
    {
        var api = new FakeTransactionService { WriteError = new ApiException(ApiException.CategoryTypeMismatch) };
        var vm = New(api);
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;
        vm.Description = "Cena";
        vm.AmountText = "20";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("La categoría no es del tipo elegido.", vm.ErrorMessage);
        Assert.False(closed);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Save_SessionExpired_FlagsItAndCloses()
    {
        var api = new FakeTransactionService { WriteError = new ApiException(ApiException.SessionExpired) };
        var vm = New(api);
        bool? closed = null;
        vm.CloseRequested += (_, changed) => closed = changed;
        vm.Description = "Cena";
        vm.AmountText = "20";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.SessionHasExpired);
        Assert.False(closed);
    }
}
