using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Services;
using FinanceTracker.Desktop.ViewModels;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class MonthlyTrendViewModelTests
{
    // Diciembre 2025: 1000 de ingresos y 50,25 de gastos.
    // Enero 2026: 1200 de ingresos y 399,75 de gastos. Tope del eje: 2000.
    private static FakeTransactionService TwoMonths() => new()
    {
        Handler = () =>
        [
            Tx(1, "2025-12-05T00:00:00", 1000, Income),
            Tx(2, "2025-12-31T00:00:00", 50.25m, Expense),
            Tx(3, "2026-01-20T00:00:00", 1200, Income),
            Tx(4, "2026-01-10T00:00:00", 300, Expense),
            Tx(5, "2026-01-25T00:00:00", 99.75m, Expense),
        ],
    };

    private static MainViewModel Vm(FakeTransactionService api, Localizer? localizer = null,
        ISettingsStore? store = null) =>
        new(LoggedInSession(), api, api, api, new FakeDialogService(), localizer ?? Es, store);

    [Fact]
    public async Task Trend_ShowsEveryMonth_WithBarsScaledToTheAxis()
    {
        var vm = Vm(TwoMonths());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(new[] { "Dic 25", "Ene 26" }, vm.TrendBars.Select(b => b.Label));
        Assert.Equal(new[] { 50, 60 }, vm.TrendBars.Select(b => b.IncomeHeight));   // 1000 y 1200 de 2000
        Assert.Equal(new[] { 3, 20 }, vm.TrendBars.Select(b => b.ExpenseHeight));   // 50,25 y 399,75 de 2000
        Assert.Equal(new[]
        {
            2000m.ToString("C0", Es.Culture),
            1000m.ToString("C0", Es.Culture),
            0m.ToString("C0", Es.Culture),
        }, vm.TrendTicks);
        Assert.Equal("Dic 25 – Ene 26", vm.TrendSummary);
    }

    [Fact]
    public async Task Trend_HighlightsTheSelectedMonth_AndFollowsTheSelector()
    {
        var vm = Vm(TwoMonths());
        await vm.LoadCommand.ExecuteAsync(null);

        // Al cargar se elige el mes mas reciente.
        Assert.Equal(new[] { false, true }, vm.TrendBars.Select(b => b.IsSelected));

        vm.SelectedMonth = vm.Months.Single(m => m.Key == new Domain.MonthKey(2025, 12));

        Assert.Equal(new[] { true, false }, vm.TrendBars.Select(b => b.IsSelected));
    }

    [Fact]
    public async Task Trend_TooltipShowsMonthAndExactAmounts()
    {
        var vm = Vm(TwoMonths());

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(
            $"Diciembre 2025\n" +
            $"{EsText("dashboard.income")}: {1000m.ToString("C", Es.Culture)}\n" +
            $"{EsText("dashboard.expenses")}: {50.25m.ToString("C", Es.Culture)}",
            vm.TrendBars[0].Tooltip);
    }

    [Fact]
    public void Trend_StartsCollapsed_AndRemembersItsState()
    {
        var store = new MemorySettingsStore();
        var api = new FakeTransactionService();
        var first = Vm(api, store: store);
        Assert.False(first.IsTrendExpanded);

        first.ToggleTrendCommand.Execute(null);

        var second = Vm(api, store: store);
        Assert.True(second.IsTrendExpanded);
        Assert.False(second.IsBreakdownExpanded);
    }

    [Fact]
    public async Task Trend_AxisIgnoresMonthsOutsideTheChart()
    {
        // 13 meses: el primero (fuera del grafico) con 9000; el resto, 1000.
        var api = new FakeTransactionService
        {
            Handler = () => Enumerable.Range(0, 13)
                .Select(i => Tx(i + 1, new DateTime(2025, 1, 15).AddMonths(i).ToString("s"),
                    i == 0 ? 9000 : 1000, Income))
                .ToList(),
        };
        var vm = Vm(api);

        await vm.LoadCommand.ExecuteAsync(null);

        Assert.Equal(12, vm.TrendBars.Count);
        Assert.Equal(1000m.ToString("C0", Es.Culture), vm.TrendTicks[0]);
        Assert.Equal("Feb 25 – Ene 26", vm.TrendSummary);
    }

    [Fact]
    public async Task SwitchingLanguage_RewritesTrendLabels()
    {
        var localizer = Es;
        var vm = Vm(TwoMonths(), localizer);
        await vm.LoadCommand.ExecuteAsync(null);

        localizer.SetLanguage(AppLanguage.En);

        Assert.Equal(new[] { "Dec 25", "Jan 26" }, vm.TrendBars.Select(b => b.Label));
        Assert.Equal("Dec 25 – Jan 26", vm.TrendSummary);
        Assert.StartsWith("December 2025\nIncome: €1,000.00", vm.TrendBars[0].Tooltip);
    }
}
