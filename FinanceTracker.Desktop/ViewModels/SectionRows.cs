using FinanceTracker.Desktop.Domain;
using FinanceTracker.Desktop.Localization;
using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.ViewModels;

/// <summary>
/// Un presupuesto listo para pintar, como TransactionRow: textos ya
/// formateados en el idioma, y el tono para el color. El XAML no calcula nada.
/// </summary>
public record BudgetRow(
    BudgetDto Source,
    string Name,
    string CategoryText,
    string PercentText,
    int BarValue,
    string SpentOfText,
    string StatusText,
    BudgetTone Tone)
{
    public static BudgetRow From(BudgetDto budget, Localizer localizer)
    {
        var culture = localizer.Culture;
        var percent = Budgets.Percentage(budget);
        string Money(decimal amount) => amount.ToString("C", culture);

        return new BudgetRow(
            budget,
            budget.Name,
            budget.CategoryName ?? localizer.T("budgets.allCategories"),
            $"{percent} %",
            // La barra no pasa de llena aunque el gasto si pase del limite.
            Math.Clamp(percent, 0, 100),
            localizer.T("budgets.spentOf", Money(budget.SpentAmount), Money(budget.Amount)),
            budget.RemainingAmount >= 0
                ? localizer.T("budgets.remaining", Money(budget.RemainingAmount))
                : localizer.T("budgets.exceeded", Money(-budget.RemainingAmount)),
            Budgets.Tone(percent));
    }
}

/// <summary>Una categoria del desglose, lista para pintar.</summary>
public record BreakdownRow(string Name, string AmountText, int BarValue);
