using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Domain;

public record CategoryTotal(string Name, decimal Amount);

/// <summary>Gastos por categoria, como la web y Android. C# puro.</summary>
public static class Breakdown
{
    /// <summary>
    /// Solo gastos, sumados por categoria, de mayor a menor. La etiqueta de
    /// "sin categoria" la pasa quien llama, ya traducida: aqui no hay idioma.
    /// </summary>
    public static List<CategoryTotal> Of(IEnumerable<TransactionDto> transactions, string noCategoryLabel) =>
        transactions
            .Where(t => t.Type == TransactionType.Expense)
            .GroupBy(t => string.IsNullOrWhiteSpace(t.CategoryName) ? noCategoryLabel : t.CategoryName)
            .Select(g => new CategoryTotal(g.Key, g.Sum(t => t.Amount)))
            .OrderByDescending(c => c.Amount)
            .ThenBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    /// <summary>
    /// Largo de la barra, de 0 a 100: la categoria mayor llega al 100 % y las
    /// demas en proporcion a ella. Es un reparto, no un limite.
    /// </summary>
    public static int BarPercent(decimal amount, decimal largest) =>
        largest <= 0 ? 0 : (int)Math.Round(amount / largest * 100, MidpointRounding.AwayFromZero);
}
