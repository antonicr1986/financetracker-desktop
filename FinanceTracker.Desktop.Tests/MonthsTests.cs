using FinanceTracker.Desktop.Domain;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class MonthsTests
{
    [Fact]
    public void Available_ReturnsDistinctMonthsNewestFirst_AcrossYears()
    {
        var all = new[]
        {
            Tx(1, "2025-12-31T00:00:00", 10, Expense),
            Tx(2, "2026-09-01T00:00:00", 10, Expense),
            Tx(3, "2026-01-15T00:00:00", 10, Income),
            Tx(4, "2026-09-20T00:00:00", 10, Income),
        };

        var months = Months.Available(all);

        Assert.Equal(new[] { new MonthKey(2026, 9), new MonthKey(2026, 1), new MonthKey(2025, 12) }, months);
    }

    [Fact]
    public void FirstDayOfMonth_StaysInItsOwnMonth()
    {
        // La fecha llega sin zona horaria: no debe moverse al mes anterior.
        var tx = Tx(1, "2026-09-01T00:00:00", 10, Expense);

        Assert.Equal(new MonthKey(2026, 9), MonthKey.Of(tx.Date));
    }

    [Fact]
    public void Of_FiltersByMonthAndSortsNewestFirst()
    {
        var all = new[]
        {
            Tx(1, "2026-09-05T00:00:00", 10, Expense),
            Tx(2, "2026-08-30T00:00:00", 10, Expense),
            Tx(3, "2026-09-20T00:00:00", 10, Income),
        };

        var ofMonth = Months.Of(all, new MonthKey(2026, 9));

        Assert.Equal(new[] { 3, 1 }, ofMonth.Select(t => t.Id));
    }

    [Fact]
    public void Summary_AddsIncomeAndExpenseAndComputesBalance()
    {
        var all = new[]
        {
            Tx(1, "2026-09-01T00:00:00", 1500.50m, Income),
            Tx(2, "2026-09-02T00:00:00", 200.25m, Expense),
            Tx(3, "2026-09-03T00:00:00", 99.75m, Expense),
        };

        var summary = Months.Summary(all);

        Assert.Equal(1500.50m, summary.Income);
        Assert.Equal(300.00m, summary.Expense);
        Assert.Equal(1200.50m, summary.Balance);
    }

    [Fact]
    public void Label_IsCapitalisedInSpanish()
    {
        var label = new MonthKey(2026, 9).Label(System.Globalization.CultureInfo.GetCultureInfo("es-ES"));

        Assert.Equal("Septiembre 2026", label);
    }
}
