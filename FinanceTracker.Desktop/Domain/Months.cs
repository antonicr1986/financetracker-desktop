using System.Globalization;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Un mes concreto. Se ordena por año y luego por mes.</summary>
public readonly record struct MonthKey(int Year, int Month) : IComparable<MonthKey>
{
    public static MonthKey Of(DateTime date) => new(date.Year, date.Month);

    public int CompareTo(MonthKey other) =>
        Year != other.Year ? Year.CompareTo(other.Year) : Month.CompareTo(other.Month);

    /// <summary>"septiembre 2026" -> "Septiembre 2026".</summary>
    public string Label(CultureInfo culture)
    {
        var text = new DateTime(Year, Month, 1).ToString("MMMM yyyy", culture);
        return culture.TextInfo.ToTitleCase(text);
    }
}

public record MonthSummary(decimal Income, decimal Expense)
{
    public decimal Balance => Income - Expense;
}

/// <summary>
/// Agrupacion por meses y totales, calculados en el cliente sobre todos los
/// movimientos, como en la web y en Android. Sin WPF: se prueba como codigo normal.
/// </summary>
public static class Months
{
    /// <summary>Meses con datos, del mas reciente al mas antiguo.</summary>
    public static List<MonthKey> Available(IEnumerable<TransactionDto> transactions) =>
        transactions.Select(t => MonthKey.Of(t.Date)).Distinct().OrderDescending().ToList();

    /// <summary>Movimientos de un mes, del mas reciente al mas antiguo.</summary>
    public static List<TransactionDto> Of(IEnumerable<TransactionDto> transactions, MonthKey month) =>
        transactions.Where(t => MonthKey.Of(t.Date) == month)
                    .OrderByDescending(t => t.Date)
                    .ThenByDescending(t => t.Id)
                    .ToList();

    public static MonthSummary Summary(IEnumerable<TransactionDto> transactions)
    {
        var list = transactions.ToList();
        return new MonthSummary(
            Income: list.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount),
            Expense: list.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount));
    }
}
