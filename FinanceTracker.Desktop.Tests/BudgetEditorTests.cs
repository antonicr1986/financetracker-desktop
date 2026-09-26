using System.Net;
using System.Net.Http;
using System.Text;
using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class BudgetFormTests
{
    [Fact]
    public void MonthsAround_GivesTwelveEachSide_CrossingYears()
    {
        var months = Budgets.MonthsAround(new MonthKey(2026, 2));

        Assert.Equal(25, months.Count);
        Assert.Equal(new MonthKey(2025, 2), months[0]);
        Assert.Equal(new MonthKey(2026, 2), months[12]);
        Assert.Equal(new MonthKey(2027, 2), months[24]);
        Assert.Contains(new MonthKey(2025, 12), months); // diciembre -> enero sin saltos
        Assert.Contains(new MonthKey(2026, 1), months);
    }

    [Fact]
    public void Validate_ReportsTheFirstProblem()
    {
        Assert.Equal(BudgetFormProblem.MissingName, BudgetForm.Validate(" ", "10"));
        Assert.Equal(BudgetFormProblem.NameTooLong, BudgetForm.Validate(new string('a', 101), "10"));
        Assert.Equal(BudgetFormProblem.InvalidAmount, BudgetForm.Validate("Casa", "mucho"));
        Assert.Equal(BudgetFormProblem.AmountNotPositive, BudgetForm.Validate("Casa", "0"));
        Assert.Null(BudgetForm.Validate("Casa", "450,50"));
    }
}

public class BudgetEditorViewModelTests
{
    private static readonly List<CategoryDto> Categories =
    [
        new(1, "Supermercado", Expense),
        new(2, "Ocio", Expense),
        new(3, "Nómina", Income),
    ];

    private static readonly MonthKey September = new(2026, 9);

    private static BudgetEditorViewModel New(FakeTransactionService api, FakeDialogService? dialogs = null) =>
        new(api, dialogs ?? new FakeDialogService(), Categories, Es, September);

    [Fact]
    public void New_StartsAsAnExpenseForTheViewedMonth_WithAllCategories()
    {
        var vm = New(new FakeTransactionService());

        Assert.False(vm.IsEdit);
        Assert.True(vm.IsExpense);
        Assert.Equal(September, vm.SelectedMonth?.Key);
        Assert.Null(vm.SelectedCategory?.Id);
        Assert.Equal(EsText("budgets.allCategories"), vm.SelectedCategory?.Name);
        // "Todas" y las de gasto, ordenadas; nunca las de ingreso.
        Assert.Equal(new[] { EsText("budgets.allCategories"), "Ocio", "Supermercado" },
            vm.Categories.Select(c => c.Name));
    }

    [Fact]
    public async Task Save_New_SendsMonthYearTypeAndANullCategoryForAll()
    {
        var api = new FakeTransactionService();
        var vm = New(api);
        bool? closed = null;
        vm.CloseRequested += (_, changed) => closed = changed;
        vm.Name = "  Casa ";
        vm.AmountText = "450,5";
        vm.SelectedMonth = vm.Months.Single(m => m.Key == new MonthKey(2026, 10));

        await vm.SaveCommand.ExecuteAsync(null);

        var input = Assert.Single(api.CreatedBudgets);
        Assert.Equal("Casa", input.Name);
        Assert.Equal(450.5m, input.Amount);
        Assert.Equal(10, input.Month);
        Assert.Equal(2026, input.Year);
        Assert.Equal(Expense, input.Type);
        Assert.Null(input.CategoryId);
        Assert.Equal(new MonthKey(2026, 10), vm.SavedMonth);
        Assert.True(closed);
    }

    [Fact]
    public void SwitchingToIncome_OffersIncomeCategories_AndFallsBackToAll()
    {
        var vm = New(new FakeTransactionService());
        vm.SelectedCategory = vm.Categories.Single(c => c.Name == "Ocio");

        vm.IsIncome = true;

        Assert.Equal(new[] { EsText("budgets.allCategories"), "Nómina" }, vm.Categories.Select(c => c.Name));
        Assert.Null(vm.SelectedCategory?.Id); // "Ocio" es de gasto: vuelve a "Todas"
    }

    [Fact]
    public async Task Edit_PrefillsAndUpdatesById()
    {
        var api = new FakeTransactionService();
        var existing = new BudgetDto(7, "Ocio del mes", 200, 50, 150, 25, 8, 2026, Expense, 2, "Ocio");
        var vm = new BudgetEditorViewModel(api, new FakeDialogService(), Categories, Es, September, existing);

        Assert.True(vm.IsEdit);
        Assert.Equal(EsText("budgets.editTitle"), vm.Title);
        Assert.Equal("Ocio del mes", vm.Name);
        Assert.Equal("200", vm.AmountText);
        Assert.Equal(new MonthKey(2026, 8), vm.SelectedMonth?.Key); // su mes, no el del panel
        Assert.Equal("Ocio", vm.SelectedCategory?.Name);

        vm.AmountText = "250";
        await vm.SaveCommand.ExecuteAsync(null);

        var (id, input) = Assert.Single(api.UpdatedBudgets);
        Assert.Equal(7, id);
        Assert.Equal(250m, input.Amount);
        Assert.Equal(2, input.CategoryId);
    }

    [Fact]
    public async Task Save_Invalid_ShowsTheProblemWithoutCallingTheApi()
    {
        var api = new FakeTransactionService();
        var vm = New(api);
        vm.AmountText = "100";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(EsText("errors.writeBudgetName"), vm.ErrorMessage);
        Assert.Empty(api.CreatedBudgets);
    }

    [Fact]
    public async Task Delete_AsksWithTheThemedDialog_AndDoesNothingOnNo()
    {
        var api = new FakeTransactionService();
        var dialogs = new FakeDialogService { ConfirmAnswer = false };
        var existing = new BudgetDto(7, "Casa", 200, 0, 200, 0, 9, 2026, Expense, null, null);
        var vm = new BudgetEditorViewModel(api, dialogs, Categories, Es, September, existing);

        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Empty(api.DeletedBudgets);
        Assert.Equal(EsText("dialog.deleteYes"), dialogs.LastConfirmText);

        dialogs.ConfirmAnswer = true;
        await vm.DeleteCommand.ExecuteAsync(null);
        Assert.Equal(new[] { 7 }, api.DeletedBudgets);
    }

    [Theory]
    [InlineData(ApiException.CategoryTypeMismatch, "errors.budgetCategoryInvalid")]
    [InlineData(ApiException.CategoryNotFound, "errors.budgetCategoryInvalid")]
    [InlineData(ApiException.NotFound, "errors.budgetGone")]
    [InlineData(ApiException.Unknown, "errors.saveBudgetFailed")]
    public async Task Save_ApiErrors_AreExplained_AndTheDialogStaysOpen(string code, string key)
    {
        var api = new FakeTransactionService { BudgetError = new ApiException(code) };
        var vm = New(api);
        var closed = false;
        vm.CloseRequested += (_, _) => closed = true;
        vm.Name = "Casa";
        vm.AmountText = "100";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(EsText(key), vm.ErrorMessage);
        Assert.False(closed);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public void SwitchingLanguage_RewritesMonthsAndAllCategories_KeepingTheSelection()
    {
        var localizer = Es;
        var vm = new BudgetEditorViewModel(new FakeTransactionService(), new FakeDialogService(), Categories,
            localizer, September);
        vm.SelectedCategory = vm.Categories.Single(c => c.Name == "Ocio");

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal("September 2026", vm.SelectedMonth?.Label);
        Assert.Equal(Strings.En["budgets.allCategories"], vm.Categories[0].Name);
        Assert.Equal("Ocio", vm.SelectedCategory?.Name);
        Assert.Equal(Strings.En["budgets.dialogTitle"], vm.Title);
    }
}

public class BudgetsOnTheDashboardTests
{
    [Fact]
    public async Task SavingABudget_ReloadsJumpsToItsMonthAndOpensTheSection()
    {
        var api = new FakeTransactionService { Handler = () => [Tx(1, "2026-09-10T00:00:00", 10, Expense)] };
        var dialogs = new FakeDialogService();
        var store = new MemorySettingsStore();
        var vm = new MainViewModel(LoggedInSession(), api, api, api, dialogs, Es, store);
        await vm.LoadCommand.ExecuteAsync(null);
        Assert.False(vm.IsBudgetsExpanded);

        dialogs.BudgetEditor = editor =>
        {
            editor.Name = "Casa";
            editor.AmountText = "300";
            editor.SaveCommand.Execute(null);
            return true;
        };
        await vm.NewBudgetCommand.ExecuteAsync(null);

        Assert.Single(api.CreatedBudgets);
        Assert.Equal(new MonthKey(2026, 9), dialogs.LastBudgetEditor!.SelectedMonth?.Key); // el que se veia
        Assert.Equal(2, api.LoadCount);
        Assert.True(vm.IsBudgetsExpanded);
        Assert.True(store.Load().BudgetsExpanded);
    }

    [Fact]
    public async Task ClickingABudget_OpensItPrefilled_AndCancellingDoesNotReload()
    {
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 10, Expense)],
            Budgets = [new BudgetDto(7, "Casa", 200, 10, 190, 5, 9, 2026, Expense, null, null)],
        };
        var dialogs = new FakeDialogService();
        var vm = new MainViewModel(LoggedInSession(), api, api, api, dialogs, Es, new MemorySettingsStore());
        await vm.LoadCommand.ExecuteAsync(null);

        await vm.EditBudgetCommand.ExecuteAsync(vm.BudgetRows.Single());

        Assert.True(dialogs.LastBudgetEditor?.IsEdit);
        Assert.Equal("Casa", dialogs.LastBudgetEditor?.Name);
        Assert.Equal(1, api.LoadCount);
    }
}

public class BudgetsWriteApiTests
{
    private class LambdaHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(HttpMethod Method, string Path, string? Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Calls.Add((request.Method, request.RequestUri!.AbsolutePath, body));
            return respond(request);
        }
    }

    private static readonly BudgetInput Input = new("Casa", 300m, 9, 2026, Expense, null);

    [Fact]
    public async Task CreateUpdateDelete_HitTheRightEndpoints_WithTheBodyTheApiExpects()
    {
        var handler = new LambdaHandler(request => request.Method == HttpMethod.Post
            ? new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(
                    """{"id":9,"name":"Casa","amount":300,"spentAmount":0,"remainingAmount":300,"usagePercentage":0,"month":9,"year":2026,"type":"Expense","categoryId":null,"categoryName":null}""",
                    Encoding.UTF8, "application/json"),
            }
            : new HttpResponseMessage(HttpStatusCode.NoContent));
        var api = new ApiClient(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        var created = await api.CreateBudgetAsync(Input);
        await api.UpdateBudgetAsync(9, Input);
        await api.DeleteBudgetAsync(9);

        Assert.Equal(9, created.Id);
        const string json = """{"name":"Casa","amount":300,"month":9,"year":2026,"type":"Expense","categoryId":null}""";
        Assert.Equal((HttpMethod.Post, "/api/Budgets", json), handler.Calls[0]);
        Assert.Equal((HttpMethod.Put, "/api/Budgets/9", json), handler.Calls[1]);
        Assert.Equal((HttpMethod.Delete, "/api/Budgets/9", (string?)null), handler.Calls[2]);
    }
}
