using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Como va un presupuesto. Mismos cortes que la web y Android: 80 % y 100 %.</summary>
public enum BudgetTone
{
    OnTrack,
    Warning,
    Over,
}

/// <summary>
/// Presupuestos del panel, en C# puro. Las cifras las da la API; aqui solo se
/// elige cuales se ven y como se pinta cada uno.
/// </summary>
public static class Budgets
{
    /// <summary>Los del mes que se ve, en el orden en que llegan de la API.</summary>
    public static List<BudgetDto> OfMonth(IEnumerable<BudgetDto> budgets, MonthKey month) =>
        budgets.Where(b => b.Year == month.Year && b.Month == month.Month).ToList();

    /// <summary>
    /// Porcentaje redondeado como lo escribe la web: 79,5 es 80. Math.Round por
    /// defecto redondea al par (79,5 -> 80, pero 80,5 -> 80), por eso AwayFromZero.
    /// </summary>
    public static int Percentage(BudgetDto budget) =>
        (int)Math.Round(budget.UsagePercentage, MidpointRounding.AwayFromZero);

    public static BudgetTone Tone(int percentage) => percentage switch
    {
        >= 100 => BudgetTone.Over,
        >= 80 => BudgetTone.Warning,
        _ => BudgetTone.OnTrack,
    };

    /// <summary>Cuantos siguen dentro del limite, para "1 de 2 dentro del límite".</summary>
    public static int WithinLimit(IEnumerable<BudgetDto> budgets) => budgets.Count(b => b.RemainingAmount >= 0);
}
