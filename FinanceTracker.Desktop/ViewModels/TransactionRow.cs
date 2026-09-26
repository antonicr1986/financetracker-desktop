using System.Globalization;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Un movimiento ya preparado para pintarse: textos formateados y si es
/// ingreso, para el color. Asi el XAML no necesita convertidores.
/// </summary>
public record TransactionRow(string Description, string Category, string DateText, string AmountText, bool IsIncome)
{
    public static TransactionRow From(TransactionDto t, CultureInfo culture)
    {
        var isIncome = t.Type == TransactionType.Income;
        var sign = isIncome ? "+" : "−";
        return new TransactionRow(
            t.Description,
            t.CategoryName,
            t.Date.ToString("d MMM yyyy", culture),
            sign + t.Amount.ToString("C", culture),
            isIncome);
    }
}
