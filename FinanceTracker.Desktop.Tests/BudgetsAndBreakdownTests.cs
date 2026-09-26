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

internal static class BudgetData
{
    public static BudgetDto Budget(int id, int year, int month, decimal amount, decimal spent,
        string? category = "Supermercado", decimal? percentage = null) =>
        new(id, $"Presupuesto {id}", amount, spent, amount - spent,
            percentage ?? (amount == 0 ? 0 : spent / amount * 100),
            month, year, Expense, category is null ? null : 1, category);
}

public class BudgetsTests
{
    [Fact]
    public void OfMonth_KeepsOnlyThatMonth_InApiOrder()
    {
        var all = new[]
        {
            BudgetData.Budget(1, 2026, 9, 100, 10),
            BudgetData.Budget(2, 2026, 8, 100, 10),
            BudgetData.Budget(3, 2025, 9, 100, 10), // mismo mes, otro año
            BudgetData.Budget(4, 2026, 9, 100, 10),
        };

        Assert.Equal(new[] { 1, 4 }, Budgets.OfMonth(all, new MonthKey(2026, 9)).Select(b => b.Id));
    }

    [Theory]
    [InlineData(79.4, 79)]
    [InlineData(79.5, 80)] // como la web; Math.Round por defecto daria 80, pero 80,5 daria 80
    [InlineData(80.5, 81)]
    public void Percentage_RoundsHalfAwayFromZero(double usage, int expected)
    {
        var budget = BudgetData.Budget(1, 2026, 9, 100, 0, percentage: (decimal)usage);

        Assert.Equal(expected, Budgets.Percentage(budget));
    }

    [Theory]
    [InlineData(0, BudgetTone.OnTrack)]
    [InlineData(79, BudgetTone.OnTrack)]
    [InlineData(80, BudgetTone.Warning)]
    [InlineData(99, BudgetTone.Warning)]
    [InlineData(100, BudgetTone.Over)]
    [InlineData(250, BudgetTone.Over)]
    public void Tone_ChangesAt80And100(int percentage, BudgetTone expected)
    {
        Assert.Equal(expected, Budgets.Tone(percentage));
    }

    [Fact]
    public void WithinLimit_CountsThoseNotOverspent_IncludingExactlyAtTheLimit()
    {
        var all = new[]
        {
            BudgetData.Budget(1, 2026, 9, 100, 50),
            BudgetData.Budget(2, 2026, 9, 100, 100), // justo en el limite: quedan 0
            BudgetData.Budget(3, 2026, 9, 100, 120),
        };

        Assert.Equal(2, Budgets.WithinLimit(all));
    }
}

public class BreakdownTests
{
    [Fact]
    public void Of_SumsOnlyExpensesByCategory_LargestFirst()
    {
        var all = new[]
        {
            Tx(1, "2026-09-01T00:00:00", 50, Expense, category: "Ocio"),
            Tx(2, "2026-09-02T00:00:00", 1000, Income, category: "Nómina"),
            Tx(3, "2026-09-03T00:00:00", 120, Expense, category: "Supermercado"),
            Tx(4, "2026-09-04T00:00:00", 80, Expense, category: "Supermercado"),
            Tx(5, "2026-09-05T00:00:00", 10, Expense, category: ""),
        };

        var totals = Breakdown.Of(all, "Sin categoría");

        Assert.Equal(new[] { "Supermercado", "Ocio", "Sin categoría" }, totals.Select(t => t.Name));
        Assert.Equal(new[] { 200m, 50m, 10m }, totals.Select(t => t.Amount));
    }

    [Theory]
    [InlineData(200, 200, 100)]
    [InlineData(50, 200, 25)]
    [InlineData(10, 0, 0)] // sin gastos: nada que repartir
    public void BarPercent_IsRelativeToTheLargest(double amount, double largest, int expected)
    {
        Assert.Equal(expected, Breakdown.BarPercent((decimal)amount, (decimal)largest));
    }
}

public class DashboardSectionsTests
{
    private static MainViewModel Vm(FakeTransactionService api, Localizer? localizer = null) =>
        new(LoggedInSession(), api, api, api, new FakeDialogService(), localizer ?? Es);

    [Fact]
    public async Task Budgets_ShowTheSelectedMonthWithTextsAndTone()
    {
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 90, Expense)],
            Budgets =
            [
                BudgetData.Budget(1, 2026, 9, 100, 90),                   // 90 %: ambar
                BudgetData.Budget(2, 2026, 9, 50, 60, category: null),   // 120 %: rojo, todas las categorias
                BudgetData.Budget(3, 2026, 8, 100, 10),                  // otro mes
            ],
        };
        var vm = Vm(api);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(2, vm.BudgetRows.Count);
        Assert.Equal(Es.T("budgets.summary", 1, 2), vm.BudgetsSummary);

        var warning = vm.BudgetRows[0];
        Assert.Equal(BudgetTone.Warning, warning.Tone);
        Assert.Equal("90 %", warning.PercentText);
        Assert.Equal(Es.T("budgets.remaining", 10m.ToString("C", Es.Culture)), warning.StatusText);

        var over = vm.BudgetRows[1];
        Assert.Equal(BudgetTone.Over, over.Tone);
        Assert.Equal(100, over.BarValue); // la barra no pasa de llena
        Assert.Equal(EsText("budgets.allCategories"), over.CategoryText);
        Assert.Equal(Es.T("budgets.exceeded", 10m.ToString("C", Es.Culture)), over.StatusText);
    }

    [Fact]
    public async Task Breakdown_ShowsTheMonthsExpensesAndTheLargestInTheSummary()
    {
        var api = new FakeTransactionService
        {
            Handler = () =>
            [
                Tx(1, "2026-09-10T00:00:00", 300, Expense, category: "Alquiler"),
                Tx(2, "2026-09-11T00:00:00", 150, Expense, category: "Ocio"),
                Tx(3, "2026-08-11T00:00:00", 999, Expense, category: "Otro mes"),
            ],
        };
        var vm = Vm(api);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Alquiler", "Ocio" }, vm.BreakdownRows.Select(r => r.Name));
        Assert.Equal(new[] { 100, 50 }, vm.BreakdownRows.Select(r => r.BarValue));
        Assert.Equal(Es.T("dashboard.byCategorySummary", "Alquiler", 300m.ToString("C", Es.Culture)), vm.BreakdownSummary);
    }

    [Fact]
    public async Task WithoutBudgetsOrExpenses_TheSummariesSaySo()
    {
        var api = new FakeTransactionService { Handler = () => [Tx(1, "2026-09-10T00:00:00", 1000, Income)] };
        var vm = Vm(api);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.False(vm.HasBudgets);
        Assert.False(vm.HasExpenses);
        Assert.Equal(EsText("budgets.none"), vm.BudgetsSummary);
        Assert.Equal(EsText("dashboard.noExpenses"), vm.BreakdownSummary);
    }

    [Fact]
    public void Sections_StartCollapsed_TheFirstTime()
    {
        var vm = new MainViewModel(LoggedInSession(), new FakeTransactionService(), new FakeTransactionService(),
            new FakeTransactionService(), new FakeDialogService(), Es, new MemorySettingsStore());

        Assert.False(vm.IsBudgetsExpanded);
        Assert.False(vm.IsBreakdownExpanded);
    }

    [Fact]
    public void Sections_RememberHowTheUserLeftThem()
    {
        var store = new MemorySettingsStore();
        var api = new FakeTransactionService();
        var first = new MainViewModel(LoggedInSession(), api, api, api, new FakeDialogService(), Es, store);

        first.ToggleBudgetsCommand.Execute(null); // abre presupuestos, el desglose sigue plegado

        // Otra sesion (otra ventana del panel) con los mismos ajustes.
        var second = new MainViewModel(LoggedInSession(), api, api, api, new FakeDialogService(), Es, store);
        Assert.True(second.IsBudgetsExpanded);
        Assert.False(second.IsBreakdownExpanded);
    }

    [Fact]
    public void TogglingASection_KeepsTheOtherSettings()
    {
        var store = new MemorySettingsStore();
        store.Save(new AppSettings { Theme = "dark", Language = "en" });
        var api = new FakeTransactionService();
        var vm = new MainViewModel(LoggedInSession(), api, api, api, new FakeDialogService(), Es, store);

        vm.ToggleBreakdownCommand.Execute(null);

        var saved = store.Load();
        Assert.Equal("dark", saved.Theme);
        Assert.Equal("en", saved.Language);
        Assert.True(saved.BreakdownExpanded);
    }

    [Fact]
    public async Task SwitchingLanguage_RewritesBudgetTexts()
    {
        var localizer = Es;
        var api = new FakeTransactionService
        {
            Handler = () => [Tx(1, "2026-09-10T00:00:00", 10, Expense)],
            Budgets = [BudgetData.Budget(1, 2026, 9, 100, 10, category: null)],
        };
        var vm = Vm(api, localizer);
        await vm.LoadCommand.ExecuteAsync(null);

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal(Strings.En["budgets.allCategories"], vm.BudgetRows[0].CategoryText);
        Assert.Equal("€90.00 left", vm.BudgetRows[0].StatusText);
    }
}

public class BudgetsApiTests
{
    private class Handler(string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public async Task GetBudgets_ReadsAPlainArray_WithANullCategory()
    {
        var handler = new Handler("""
            [{"id":1,"name":"Casa","amount":500,"spentAmount":520.5,"remainingAmount":-20.5,"usagePercentage":104.1,
              "month":9,"year":2026,"type":"Expense","categoryId":null,"categoryName":null}]
            """);
        var api = new ApiClient(LoggedInSession(), new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

        var budgets = await api.GetBudgetsAsync();

        var budget = Assert.Single(budgets);
        Assert.Null(budget.CategoryId);
        Assert.Equal(-20.5m, budget.RemainingAmount);
        Assert.Equal("/api/Budgets", handler.Request!.RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization?.Scheme);
    }
}
