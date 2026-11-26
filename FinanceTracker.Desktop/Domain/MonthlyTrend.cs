using FinanceTracker.Desktop.Models;

namespace FinanceTracker.Desktop.Domain;

/// <summary>Ingresos y gastos de un mes, para el grafico de evolucion mensual.</summary>
public record MonthlyPoint(MonthKey Month, decimal Income, decimal Expense);

/// <summary>
/// Evolucion mensual de los ultimos meses, como el grafico de la web. C# puro:
/// la vista solo dibuja lo que se calcula aqui.
/// </summary>
public static class MonthlyTrend
{
    /// <summary>
    /// Meses que caben en el grafico. Con mas, a lo ancho minimo de la ventana
    /// las barras y las etiquetas del eje se pisan.
    /// </summary>
    public const int MaxMonths = 12;

    /// <summary>
    /// Un punto por mes, del mas antiguo al mas reciente, de los ultimos
    /// <see cref="MaxMonths"/> meses hasta el mas reciente con datos. Los meses
    /// sin movimientos salen a 0: si no, dos meses separados se verian seguidos
    /// en el grafico.
    /// </summary>
    public static List<MonthlyPoint> Series(IEnumerable<TransactionDto> transactions)
    {
        var byMonth = transactions
            .GroupBy(t => MonthKey.Of(t.Date))
            .ToDictionary(g => g.Key, g => Months.Summary(g));
        if (byMonth.Count == 0)
            return [];

        var points = new List<MonthlyPoint>();
        var last = byMonth.Keys.Max();
        for (var month = byMonth.Keys.Min(); month.CompareTo(last) <= 0; month = Next(month))
        {
            var summary = byMonth.GetValueOrDefault(month) ?? new MonthSummary(0, 0);
            points.Add(new MonthlyPoint(month, summary.Income, summary.Expense));
        }
        return points.TakeLast(MaxMonths).ToList();
    }

    private static MonthKey Next(MonthKey month) =>
        month.Month == 12 ? new MonthKey(month.Year + 1, 1) : new MonthKey(month.Year, month.Month + 1);

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
