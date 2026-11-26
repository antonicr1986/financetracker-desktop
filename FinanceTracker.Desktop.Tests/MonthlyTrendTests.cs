using FinanceTracker.Desktop.Domain;
using static FinanceTracker.Desktop.Models.TransactionType;
using static FinanceTracker.Desktop.Tests.TestData;

namespace FinanceTracker.Desktop.Tests;

public class MonthlyTrendTests
{
    [Fact]
    public void Series_HasIncomeAndExpensePerMonth_OldestFirst_AcrossYears()
    {
        var all = new[]
        {
            Tx(1, "2026-01-10T00:00:00", 300, Expense),
            Tx(2, "2025-12-05T00:00:00", 1000, Income),
            Tx(3, "2026-01-20T00:00:00", 1200, Income),
            Tx(4, "2025-12-31T00:00:00", 50.25m, Expense),
            Tx(5, "2026-01-25T00:00:00", 99.75m, Expense),
        };

        var series = MonthlyTrend.Series(all);

        Assert.Equal(new[]
        {
            new MonthlyPoint(new MonthKey(2025, 12), 1000, 50.25m),
            new MonthlyPoint(new MonthKey(2026, 1), 1200, 399.75m),
        }, series);
    }

    [Fact]
    public void Series_FillsMonthsWithoutTransactionsWithZero()
    {
        var all = new[]
        {
            Tx(1, "2025-11-03T00:00:00", 500, Income),
            Tx(2, "2026-02-14T00:00:00", 80, Expense),
        };

        var series = MonthlyTrend.Series(all);

        Assert.Equal(new[]
        {
            new MonthlyPoint(new MonthKey(2025, 11), 500, 0),
            new MonthlyPoint(new MonthKey(2025, 12), 0, 0),
            new MonthlyPoint(new MonthKey(2026, 1), 0, 0),
            new MonthlyPoint(new MonthKey(2026, 2), 0, 80),
        }, series);
    }

    [Fact]
    public void Series_IsEmpty_WithoutTransactions()
    {
        Assert.Empty(MonthlyTrend.Series([]));
    }

    [Theory]
    [InlineData(1234, 2000)]
    [InlineData(870, 900)]
    [InlineData(100, 100)]
    [InlineData(0, 0)]
    public void AxisMax_RoundsUpToANiceNumber(double largest, double expected)
    {
        Assert.Equal((decimal)expected, MonthlyTrend.AxisMax((decimal)largest));
    }
}
