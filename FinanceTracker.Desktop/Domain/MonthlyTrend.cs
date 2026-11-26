using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Ingresos y gastos de un mes, para el grafico de evolucion mensual.</summary>
public record MonthlyPoint(MonthKey Month, decimal Income, decimal Expense);

/// <summary>
/// Evolucion mensual de todo el historico, como el grafico de la web. C# puro:
/// la vista solo dibuja lo que se calcula aqui.
/// </summary>
public static class MonthlyTrend
{
    /// <summary>Un punto por mes con datos, del mas antiguo al mas reciente.</summary>
    public static List<MonthlyPoint> Series(IEnumerable<TransactionDto> transactions)
    {
        var list = transactions.ToList();
        return Months.Available(list)
            .Order()
            .Select(month =>
            {
                var summary = Months.Summary(Months.Of(list, month));
                return new MonthlyPoint(month, summary.Income, summary.Expense);
            })
            .ToList();
    }

    /// <summary>Tope del eje: el valor redondeado hacia arriba a un numero "redondo".</summary>
    public static decimal AxisMax(decimal value)
    {
        if (value <= 0)
            return 0;

        // Potencia de 10 del primer digito (1234 -> 1000), con decimal y sin
        // logaritmos: asi 1000 no se queda en 999,99 por un redondeo de double.
        var magnitude = 1m;
        while (magnitude * 10 <= value)
            magnitude *= 10;
        while (magnitude > value)
            magnitude /= 10;

        return Math.Ceiling(value / magnitude) * magnitude;
    }
}
